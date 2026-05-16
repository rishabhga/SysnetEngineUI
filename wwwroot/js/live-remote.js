const RemoteSession = (function () {
    'use strict';

    const CONFIG = {
        REQUEST_TIMEOUT: 60,          
        MIN_FRESH_IMAGES: 3,           
        MAX_NO_IMAGE_COUNT: 15,       
        POLL_INTERVAL: 200,            
        DENIAL_CHECK_INTERVAL: 2000,   
        MOUSE_THROTTLE: 100         
    };

    let domain = '';
    let apiBase = '';
    let storageKey = '';
    let currentState = 'idle';  
    let isPolling = false;
    let isCheckingDenial = false;
    let noImageCount = 0;
    let freshImageCount = 0;
    let lastImageHash = '';
    let lastMouseSend = 0;

    let elements = {};

    function init() {
        if (typeof REMOTE_CONFIG !== 'undefined') {
            domain = REMOTE_CONFIG.domain || '';
            apiBase = REMOTE_CONFIG.apiBase || '/ComputerSummary';
        }

        storageKey = `remote_session_${domain}`;
        cacheElements();
        setupEventListeners();
        restoreSessionState();
    }
    function cacheElements() {
        elements = {
            mainBtn: document.getElementById('mainBtn'),
            mainIcon: document.getElementById('mainIcon'),
            mainText: document.getElementById('mainText'),
            waitingState: document.getElementById('waitingState'),
            waitingIcon: document.getElementById('waitingIcon'),
            waitingTitle: document.getElementById('waitingTitle'),
            waitingMessage: document.getElementById('waitingMessage'),
            countdownContainer: document.getElementById('countdownContainer'),
            countdownTimer: document.getElementById('countdownTimer'),
            screenContainer: document.getElementById('screenContainer'),
            remoteImage: document.getElementById('remoteImage'),
            statusDot: document.getElementById('statusDot'),
            statusText: document.getElementById('statusText'),
            controlsHint: document.getElementById('controlsHint'),
            lastUpdateTime: document.getElementById('lastUpdateTime'),
            lastUpdateValue: document.getElementById('lastUpdateValue'),
            stopModal: document.getElementById('stopModal'),
            timeoutModal: document.getElementById('timeoutModal'),
            timeoutModalIcon: document.getElementById('timeoutModalIcon'),
            timeoutModalTitle: document.getElementById('timeoutModalTitle'),
            timeoutModalMessage: document.getElementById('timeoutModalMessage'),
            expiredModal: document.getElementById('expiredModal'),
            fullscreenIcon: document.getElementById('fullscreenIcon'),
            fullscreenText: document.getElementById('fullscreenText')
        };
    }
    function setupEventListeners() {
        if (elements.remoteImage) {
            elements.remoteImage.addEventListener('mousemove', handleMouseMove);
            elements.remoteImage.addEventListener('click', handleLeftClick);
            elements.remoteImage.addEventListener('contextmenu', handleRightClick);
        }
        document.addEventListener('keydown', handleKeyPress);
        document.addEventListener('fullscreenchange', handleFullscreenChange);
        window.addEventListener('beforeunload', cleanup);
    }

    function saveSessionState() {
        const sessionData = {
            state: currentState,
            timestamp: Date.now(),
            remainingSeconds: remainingSeconds,
            domain: domain
        };
        sessionStorage.setItem(storageKey, JSON.stringify(sessionData));
    }
    function restoreSessionState() {
        const savedData = sessionStorage.getItem(storageKey);

        if (savedData) {
            try {
                const session = JSON.parse(savedData);
                const elapsed = Math.floor((Date.now() - session.timestamp) / 1000);

                if (session.domain === domain) {
                    if (session.state === 'waiting' || session.state === 'requesting') {
                        const timeLeft = (session.remainingSeconds || CONFIG.REQUEST_TIMEOUT) - elapsed;

                        if (timeLeft > 0) {
                            currentState = 'waiting';
                            remainingSeconds = timeLeft;
                            showWaitingUI();
                            startPolling();
                            startDenialCheck();
                            return;
                        }
                    } else if (session.state === 'connected') {
                        currentState = 'waiting';
                        remainingSeconds = 30;
                        showReconnectingUI();
                        startPolling();
                        return;
                    }
                }
            } catch (e) {
                console.error('Error restoring session:', e);
            }
        }

        clearSessionState();
        resetToIdleState();
    }
    function clearSessionState() {
        sessionStorage.removeItem(storageKey);
    function handleMainAction() {
        if (currentState === 'idle') {
            requestAccess();
        } else if (currentState === 'connected') {
            openStopModal();
        }
    }
    function requestAccess() {
        if (currentState === 'requesting' || currentState === 'waiting') return;
        currentState = 'requesting';
        noImageCount = 0;
        freshImageCount = 0;
        lastImageHash = '';
        remainingSeconds = CONFIG.REQUEST_TIMEOUT;
        saveSessionState();
        setButtonState('sending');
        updateWaitingState('Sending Request...', 'Contacting remote device', 'fa-paper-plane animate-pulse');
        updateStatus('yellow', 'Requesting...');
        fetch(`${apiBase}/RemoteAccess?domain=${domain}`)
            .then(response => {
                currentState = 'waiting';
                saveSessionState();

                updateWaitingState('Request Sent!', 'Waiting for user to accept...', 'fa-clock text-green-400 animate-pulse');
                updateStatus('orange', 'Waiting for approval');

                elements.mainText.textContent = 'Waiting...';
                startPolling();
                startDenialCheck();
            })
            .catch(error => {
                console.error('Request error:', error);
                updateWaitingState('Request Failed', 'Could not contact device.', 'fa-times-circle text-red-400');
                updateStatus('red', 'Failed');
                clearSessionState();
                resetToIdleState();

            });
    }
    function confirmRemoteStop() {
        closeStopModal();
        setButtonState('stopping');
        fetch(`${apiBase}/Livestop?domain=${domain}`)
            .then(r => r.json())
            .then(() => {
                stopAllIntervals();
                clearSessionState();
                resetToIdleState();
            })
            .catch(err => {
                console.error('Stop error:', err);
                stopAllIntervals();
                clearSessionState();
                resetToIdleState();
            });
    }

    function startPolling() {
        if (isPolling) return;
        isPolling = true;
        pollNext();
    }

    function pollNext() {
        if (!isPolling || currentState === 'idle') {
            isPolling = false;
            return;
        }

        fetch(`${apiBase}/Remotemonitoring?domain=${domain}&_t=${Date.now()}`)
            .then(r => r.json())
            .then(data => {
                handlePollingResponse(data);
                if (isPolling) pollNext();
            })
            .catch(err => {
                handlePollingError(err);
                if (isPolling) pollNext();
            });
    }

    function stopPolling() {
        isPolling = false;
    }

    function handlePollingResponse(data) {
        const imageData = data.image || data.ImageBase64 || data.imageBase64;
        if (imageData && imageData.length > 100) {
            const currentHash = getImageHash(imageData);

            if (currentHash !== lastImageHash) {
                freshImageCount++;
                lastImageHash = currentHash;
                noImageCount = 0;
            }
            if (currentState === 'waiting' && freshImageCount >= CONFIG.MIN_FRESH_IMAGES) {
                currentState = 'connected';
                stopCountdown();
                stopDenialCheck();
                saveSessionState();
                showRemoteScreen();
                setButtonState('stop');
            }
            if (currentState === 'connected') {
                elements.remoteImage.src = "data:image/jpeg;base64," + imageData;
                updateLastUpdateTime();
            }
        } else {
            noImageCount++;

            if (currentState === 'connected' && noImageCount >= CONFIG.MAX_NO_IMAGE_COUNT) {
                handleDisconnect();
            }
        }
    }
    function handlePollingError(error) {
        console.error('Polling error:', error);
        noImageCount++;

        if (currentState === 'connected' && noImageCount >= CONFIG.MAX_NO_IMAGE_COUNT) {
            handleDisconnect();
        }
    }
    function getImageHash(base64Data) {
        if (!base64Data || base64Data.length < 200) return '';
        return base64Data.substring(0, 100) + base64Data.substring(base64Data.length - 100);
    }

 


    function startDenialCheck() {
        if (isCheckingDenial) return;
        isCheckingDenial = true;
        checkNextDenial();
    }

    function checkNextDenial() {
        if (!isCheckingDenial || currentState !== 'waiting') {
            isCheckingDenial = false;
            return;
        }

        fetch(`${apiBase}/CheckAccessStatus?domain=${domain}&_t=${Date.now()}`)
            .then(r => r.json())
            .then(res => {
                if (res.denied === true) {
                    handleDenial();
                } else if (isCheckingDenial) {
                    checkNextDenial();
                }
            })
            .catch(() => {
                if (isCheckingDenial) checkNextDenial();
            });
    }

    function stopDenialCheck() {
        isCheckingDenial = false;
    }

    function handleDenial() {
        stopAllIntervals();
        clearSessionState();
        showTimeoutModal('Access Denied', 'The remote user has denied your access request.', 'fa-ban', 'bg-red-500');
        resetToIdleState();
    }

    function handleDisconnect() {
        stopAllIntervals();
        clearSessionState();
        elements.expiredModal.classList.remove('hidden');
        resetToIdleState();
    }

    function setButtonState(state) {
        const btn = elements.mainBtn;
        const icon = elements.mainIcon;
        const text = elements.mainText;
        btn.classList.remove('from-orange-500', 'to-red-600', 'from-red-500', 'to-red-700', 'from-gray-500', 'to-gray-600');
        icon.classList.remove('fa-satellite-dish', 'fa-power-off', 'fa-spinner', 'fa-spin');
        switch (state) {
            case 'idle':
                btn.disabled = false;
                btn.classList.add('from-orange-500', 'to-red-600');
                icon.classList.add('fa-satellite-dish');
                text.textContent = 'Request Access';
                break;
            case 'sending':
                btn.disabled = true;
                btn.classList.add('from-gray-500', 'to-gray-600');
                icon.classList.add('fa-spinner', 'fa-spin');
                text.textContent = 'Sending...';
                break;
            case 'waiting':
                btn.disabled = true;
                btn.classList.add('from-gray-500', 'to-gray-600');
                icon.classList.add('fa-spinner', 'fa-spin');
                text.textContent = 'Waiting...';
                break;
            case 'stop':
                btn.disabled = false;
                btn.classList.add('from-red-500', 'to-red-700');
                icon.classList.add('fa-power-off');
                text.textContent = 'Stop Remote';
                break;
            case 'stopping':
                btn.disabled = true;
                btn.classList.add('from-gray-500', 'to-gray-600');
                icon.classList.add('fa-spinner', 'fa-spin');
                text.textContent = 'Stopping...';
                break;
        }
    }
    function showWaitingUI() {
        setButtonState('waiting');
        updateWaitingState('Request Pending', 'Waiting for user to accept on remote device...', 'fa-clock text-green-400 animate-pulse');
        updateStatus('orange', 'Waiting for approval');
    }
    function showReconnectingUI() {
        setButtonState('waiting');
        elements.mainText.textContent = 'Reconnecting...';
        updateWaitingState('Reconnecting...', 'Attempting to restore session', 'fa-sync text-cyan-400 animate-spin');
        updateStatus('yellow', 'Reconnecting...');
    }
    function showRemoteScreen() {
        elements.waitingState.style.display = 'none';
        elements.remoteImage.style.display = 'block';
        elements.controlsHint.style.display = 'block';
        elements.lastUpdateTime.style.display = 'block';
        updateStatus('green', 'Connected');
    }
    function updateWaitingState(title, message, iconClass) {
        elements.waitingTitle.textContent = title;
        elements.waitingMessage.textContent = message;
        elements.waitingIcon.className = 'fas ' + iconClass + ' text-3xl';
    }
    function updateStatus(color, text) {
        const colors = {
            gray: { dot: 'bg-gray-400', text: 'text-gray-600', pulse: false },
            yellow: { dot: 'bg-yellow-500', text: 'text-yellow-600', pulse: true },
            orange: { dot: 'bg-orange-500', text: 'text-orange-600', pulse: true },
            green: { dot: 'bg-green-500', text: 'text-green-600', pulse: true },
            red: { dot: 'bg-red-500', text: 'text-red-600', pulse: false }
        };
        const c = colors[color] || colors.gray;
        elements.statusDot.className = `w-3 h-3 ${c.dot} rounded-full${c.pulse ? ' animate-pulse' : ''}`;
        elements.statusText.textContent = text;
        elements.statusText.className = `text-sm font-bold ${c.text}`;
    }
    function updateLastUpdateTime() {
        elements.lastUpdateValue.textContent = new Date().toLocaleTimeString();
    }
    function resetToIdleState() {
        currentState = 'idle';
        noImageCount = 0;
        freshImageCount = 0;
        lastImageHash = '';
        remainingSeconds = CONFIG.REQUEST_TIMEOUT;
        stopAllIntervals();
        setButtonState('idle');
        elements.waitingState.style.display = 'flex';
        elements.countdownContainer.classList.add('hidden');
        updateWaitingState('Ready to Connect', 'Click "Request Access" button above to start', 'fa-hand-pointer');
        elements.remoteImage.style.display = 'none';
        elements.remoteImage.src = 'data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==';
        updateStatus('gray', 'Not Started');
        elements.controlsHint.style.display = 'none';
        elements.lastUpdateTime.style.display = 'none';
    }

    function openStopModal() {
        elements.stopModal.classList.remove('hidden');
    }
    function closeStopModal() {
        elements.stopModal.classList.add('hidden');
    }
    function showTimeoutModal(title, message, icon, bgClass) {
        elements.timeoutModalTitle.textContent = title;
        elements.timeoutModalMessage.textContent = message;
        elements.timeoutModalIcon.className = `w-12 h-12 ${bgClass} rounded-xl flex items-center justify-center shadow-lg`;
        elements.timeoutModalIcon.innerHTML = `<i class="fas ${icon} text-white text-2xl"></i>`;
        elements.timeoutModal.classList.remove('hidden');
    }
    function closeTimeoutModal() {
        elements.timeoutModal.classList.add('hidden');
    }
    function closeExpiredModal() {
        elements.expiredModal.classList.add('hidden');
    }

    function handleMouseMove(e) {
        if (currentState !== 'connected') return;

        const now = Date.now();
        if (now - lastMouseSend < CONFIG.MOUSE_THROTTLE) return;
        lastMouseSend = now;
        const rect = elements.remoteImage.getBoundingClientRect();
        const x = ((e.clientX - rect.left) / elements.remoteImage.clientWidth) * 100;
        const y = ((e.clientY - rect.top) / elements.remoteImage.clientHeight) * 100;
        fetch(`${apiBase}/SendMouseMove?domain=${domain}&x=${x}&y=${y}`).catch(() => { });
    }
    function handleLeftClick() {
        if (currentState === 'connected') {
            fetch(`${apiBase}/SendLeftClick?domain=${domain}`).catch(() => { });
        }
    }
    function handleRightClick(e) {
        e.preventDefault();
        if (currentState === 'connected') {
            fetch(`${apiBase}/SendRightClick?domain=${domain}`).catch(() => { });
        }
    }
    function handleKeyPress(e) {
        if (currentState === 'connected') {
            fetch(`${apiBase}/SendKeyPress?domain=${domain}&key=${encodeURIComponent(e.key)}`).catch(() => { });
        }
    }

    function toggleFullscreen() {
        if (!document.fullscreenElement) {
            elements.screenContainer.requestFullscreen()
                .then(() => {
                    elements.fullscreenIcon.classList.replace('fa-expand', 'fa-compress');
                    elements.fullscreenText.textContent = 'Exit Fullscreen';
                })
                .catch(err => console.error('Fullscreen error:', err));
        } else {
            document.exitFullscreen()
                .then(() => {
                    elements.fullscreenIcon.classList.replace('fa-compress', 'fa-expand');
                    elements.fullscreenText.textContent = 'Fullscreen';
                });
        }
    }
    function handleFullscreenChange() {
        if (!document.fullscreenElement) {
            elements.fullscreenIcon.classList.replace('fa-compress', 'fa-expand');
            elements.fullscreenText.textContent = 'Fullscreen';
        }
    }

    function stopAllIntervals() {
        stopPolling();
        stopDenialCheck();
    }
    function cleanup() {
        stopAllIntervals();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    return {
        handleMainAction,
        requestAccess,
        confirmRemoteStop,
        openStopModal,
        closeStopModal,
        closeTimeoutModal,
        closeExpiredModal,
        toggleFullscreen
    };
})();