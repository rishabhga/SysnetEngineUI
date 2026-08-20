var domaindata = "";
var actualDomainName = "";
var dataTables = {};
var tableRegistry = {};

const flexRender = (row, ...fields) => {
    let val = undefined;
    for (const field of fields) {
        if (row[field] !== undefined && row[field] !== null) { val = row[field]; break; }
        const camel = field.charAt(0).toLowerCase() + field.slice(1);
        if (row[camel] !== undefined && row[camel] !== null) { val = row[camel]; break; }
    }
    if (val === undefined || val === null || val === '') return "N/A";
    if (typeof val === 'string' && val.match(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/)) {
        try {
            const date = new Date(val);
            if (!isNaN(date.getTime())) {
                return date.toLocaleString(undefined, {
                    year: 'numeric', month: 'short', day: 'numeric',
                    hour: '2-digit', minute: '2-digit'
                });
            }
        } catch (e) { }
    }
    return val;
};

const commonLogColumns = [
    { data: null, render: (row) => flexRender(row, 'FieldName') },
    { data: null, render: (row) => flexRender(row, 'PreviousValue') },
    { data: null, render: (row) => flexRender(row, 'ChangedValue') },
    { data: null, render: (row) => flexRender(row, 'ChangeDate', 'ChangeDateTime') }
];

function diskFreshnessBadge(dateVal, opts) {
    opts = opts || {};
    const staleHours = opts.staleHours ?? 24;
    const veryStaleHours = opts.veryStaleHours ?? 24 * 7;
    const label = opts.label || 'Captured';

    if (!dateVal) {
        return '<span class="disk-fresh-badge disk-fresh-unknown"><i class="fas fa-question-circle"></i> No scan data yet</span>';
    }
    let d;
    try { d = new Date(dateVal); } catch (e) { d = null; }
    if (!d || isNaN(d.getTime())) {
        return '<span class="disk-fresh-badge disk-fresh-unknown"><i class="fas fa-question-circle"></i> Unknown time</span>';
    }

    const ageMs = Date.now() - d.getTime();
    const ageHours = ageMs / 36e5;
    let cls = 'disk-fresh-ok';
    let icon = 'fa-check-circle';
    if (ageHours >= veryStaleHours) { cls = 'disk-fresh-stale'; icon = 'fa-exclamation-triangle'; }
    else if (ageHours >= staleHours) { cls = 'disk-fresh-warn'; icon = 'fa-clock'; }

    let rel;
    if (ageHours < 1) rel = Math.max(1, Math.round(ageMs / 60000)) + ' min ago';
    else if (ageHours < 48) rel = Math.round(ageHours) + ' hr ago';
    else rel = Math.round(ageHours / 24) + ' days ago';

    const abs = d.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
    const staleNote = cls !== 'disk-fresh-ok' ? ' &middot; may be from a previous audit' : '';

    return `<span class="disk-fresh-badge ${cls}" title="${abs}"><i class="fas ${icon}"></i> ${label}: ${rel}${staleNote}</span>`;
}

(function initDiskSubtabs() {
    $(document).on('click', '.disk-subtab-btn', function () {
        const target = $(this).data('subtab');
        if ($(this).hasClass('active')) return;

        $('.disk-subtab-btn').removeClass('active');
        $(this).addClass('active');

        $('.disk-subtab-panel').removeClass('active').hide();
        const $panel = $('#diskSubtab' + target.charAt(0).toUpperCase() + target.slice(1));
        $panel.show().addClass('active');

        if (target === 'deep') {
            ensureDeepAuditDataLoaded(false);
            setTimeout(function () {
                if ($.fn.DataTable.isDataTable('#diskSmartAttributesTable')) $('#diskSmartAttributesTable').DataTable().columns.adjust();
                if ($.fn.DataTable.isDataTable('#diskBenchmarkTable')) $('#diskBenchmarkTable').DataTable().columns.adjust();
            }, 50);
        } else if (target === 'overview') {
            setTimeout(function () {
                if (window.diskHealthChartInstance) window.diskHealthChartInstance.resize();
                if (window.diskUsageChartInstance) window.diskUsageChartInstance.resize();
                if (window.diskTrendChartInstance) window.diskTrendChartInstance.resize();
            }, 50);
        }
    });
})();

(function injectDiskAnimCss() {
    if (document.getElementById('disk-audit-anim-css')) return;
    const style = document.createElement('style');
    style.id = 'disk-audit-anim-css';
    style.textContent = `
        /* Base Stagger Animations */
        @keyframes diskPanelFadeIn { from { opacity:0; transform:translateY(10px); } to { opacity:1; transform:translateY(0); } }
        .disk-anim-in { animation: diskPanelFadeIn .45s cubic-bezier(0.16, 1, 0.3, 1) both; }
        
        .disk-stagger-1 { animation-delay: 0.05s; }
        .disk-stagger-2 { animation-delay: 0.10s; }
        .disk-stagger-3 { animation-delay: 0.15s; }
        .disk-stagger-4 { animation-delay: 0.20s; }
        .disk-stagger-5 { animation-delay: 0.25s; }
        .disk-stagger-6 { animation-delay: 0.30s; }

        .disk-stale-banner { display:flex; align-items:center; gap:10px; background:rgba(255,251,235,0.85); backdrop-filter:blur(8px); border:1px solid rgba(253,230,138,0.5); border-radius:var(--radius-md); padding:10px 14px; margin-bottom:14px; animation: diskPanelFadeIn .35s ease both; }

        /* Modern scanning state for Quick/Deep Audit */
        @keyframes diskScanPulse { 0%,100% { opacity:1; transform:scale(1); } 50% { opacity:.6; transform:scale(0.95); } }
        @keyframes diskScanShimmer { 0% { background-position: -400px 0; } 100% { background-position: 400px 0; } }
        @keyframes diskScanBarMove { 0% { transform: translateX(-100%); } 100% { transform: translateX(340%); } }
        .disk-scan-shell { position:relative; overflow:hidden; border:1px solid rgba(226,232,240,0.5) !important; background:linear-gradient(to right, #f8fafc, #f1f5f9) !important; box-shadow:inset 0 2px 4px rgba(0,0,0,0.02); }
        .disk-scan-icon { animation: diskScanPulse 1.8s cubic-bezier(0.4, 0, 0.6, 1) infinite; }
        .disk-scan-track { position:relative; height:6px; border-radius:999px; background:var(--slate-200); overflow:hidden; margin:14px auto 0; max-width:260px; box-shadow:inset 0 1px 2px rgba(0,0,0,0.1); }
        .disk-scan-track::after {
            content:''; position:absolute; top:0; left:0; height:100%; width:30%; border-radius:999px;
            background:linear-gradient(90deg, transparent, #0ea5e9, #0284c7, transparent);
            animation: diskScanBarMove 1.4s ease-in-out infinite;
        }
        .disk-scan-shimmer-text {
            background:linear-gradient(90deg, var(--slate-500) 0%, var(--slate-500) 40%, #0284c7 50%, var(--slate-500) 60%, var(--slate-500) 100%);
            background-size: 800px 100%;
            -webkit-background-clip:text; background-clip:text; color:transparent;
            animation: diskScanShimmer 2.2s linear infinite;
        }

        /* Audit buttons: soft glow pulse */
        @keyframes diskBtnGlow { 0%,100% { box-shadow:0 4px 12px rgba(14,165,233,.3); transform:scale(1); } 50% { box-shadow:0 6px 24px rgba(14,165,233,.6); transform:scale(1.02); } }
        #btnAuditHardDiskQuick.disk-audit-running, #btnAuditHardDiskDeep.disk-audit-running {
            animation: diskBtnGlow 1.4s cubic-bezier(0.4, 0, 0.6, 1) infinite;
        }

        /* Benchmark & SMART DataTable polish */
        #diskBenchmarkTable.dataTable, #diskSmartAttributesTable.dataTable { border-collapse:separate !important; border-spacing:0; }
        #diskBenchmarkTable.dataTable thead th, #diskSmartAttributesTable.dataTable thead th {
            font-size:.68rem; text-transform:uppercase; letter-spacing:.04em; color:var(--slate-500);
            border-bottom:2px solid var(--slate-200); padding:12px 14px; background:var(--slate-50);
        }
        #diskBenchmarkTable.dataTable tbody td, #diskSmartAttributesTable.dataTable tbody td {
            font-size:.8rem; padding:12px 14px; border-bottom:1px solid var(--slate-100); color:var(--slate-700);
            transition: background-color .2s ease, transform .2s ease;
        }
        #diskBenchmarkTable.dataTable tbody tr:hover td, #diskSmartAttributesTable.dataTable tbody tr:hover td { background:#f0fdfa; }
        #diskBenchmarkTable.dataTable tbody tr:nth-child(even) td, #diskSmartAttributesTable.dataTable tbody tr:nth-child(even) td { background:rgba(248,250,252,0.6); }

        /* Grouped sub-tabs */
        .disk-subtab-nav {
            display:flex; gap:8px; margin:18px 0 24px; border-bottom:1px solid var(--slate-200);
            overflow-x:auto; padding-bottom:2px;
        }
        .disk-subtab-btn {
            display:flex; align-items:center; gap:8px; white-space:nowrap;
            padding:12px 20px; border:none; background:transparent; cursor:pointer;
            font-size:.85rem; font-weight:700; color:var(--slate-400);
            border-bottom:3px solid transparent; margin-bottom:-3px;
            transition: all .2s cubic-bezier(0.4, 0, 0.2, 1);
            position:relative;
        }
        .disk-subtab-btn::after {
            content:''; position:absolute; bottom:-3px; left:50%; width:0%; height:3px;
            background:var(--primary); transition:all .2s cubic-bezier(0.4, 0, 0.2, 1); transform:translateX(-50%);
            border-radius:3px 3px 0 0;
        }
        .disk-subtab-btn i { font-size:.85rem; transition:transform .2s; }
        .disk-subtab-btn:hover { color:var(--slate-700); background:rgba(241,245,249,0.5); border-radius:8px 8px 0 0; }
        .disk-subtab-btn:hover i { transform:translateY(-1px); }
        .disk-subtab-btn.active {
            color:var(--primary);
            background:linear-gradient(180deg, transparent 50%, rgba(14,165,233,.05) 100%);
            border-radius:8px 8px 0 0;
        }
        .disk-subtab-btn.active::after { width:100%; }
        .disk-subtab-panel { display:none; }
        .disk-subtab-panel.active { display:block; animation: diskPanelFadeIn .4s cubic-bezier(0.16, 1, 0.3, 1) both; }

        /* Skeleton shimmer */
        @keyframes diskSkeletonShimmer { 0% { background-position:-450px 0; } 100% { background-position:450px 0; } }
        .disk-skeleton-block {
            border-radius:8px; height:16px; margin-bottom:8px;
            background:linear-gradient(90deg, var(--slate-100) 25%, var(--slate-200) 37%, var(--slate-100) 63%);
            background-size:800px 100%;
            animation: diskSkeletonShimmer 1.4s linear infinite;
        }
        .disk-skeleton-card {
            background:#fff; border:1px solid var(--slate-200); border-radius:var(--radius-md);
            padding:16px 18px; box-shadow:var(--shadow-sm);
        }

        /* Glassmorphic & Modern Cards */
        .disk-modern-card {
            background:rgba(255,255,255,0.7); backdrop-filter:blur(12px);
            border:1px solid rgba(226,232,240,0.8); border-radius:var(--radius-lg);
            box-shadow:0 4px 6px -1px rgba(0,0,0,0.02), 0 2px 4px -1px rgba(0,0,0,0.01);
            transition: all .25s cubic-bezier(0.4, 0, 0.2, 1);
            position:relative; overflow:hidden;
        }
        .disk-modern-card::before {
            content:''; position:absolute; top:0; left:0; right:0; height:100%;
            background:linear-gradient(135deg, rgba(255,255,255,0.4) 0%, rgba(255,255,255,0) 100%);
            pointer-events:none;
        }
        .disk-modern-card:hover {
            transform: translateY(-4px) scale(1.01);
            box-shadow: 0 12px 30px -4px rgba(15,23,42,.1), 0 4px 12px -2px rgba(15,23,42,.05);
            border-color: #7dd3fc;
            background:rgba(255,255,255,0.9);
        }

        /* Chart Containers */
        .disk-chart-container {
            position:relative; width:100%; height:100%; min-height:220px;
            display:flex; align-items:center; justify-content:center;
        }
        .disk-gauge-val {
            position:absolute; top:50%; left:50%; transform:translate(-50%,-50%);
            text-align:center;
        }

        /* Hero: pulsing icon ring + subtle animated gradient sheen */
        @keyframes diskHeroIconPulse { 0%,100% { box-shadow:0 0 0 0 rgba(13,148,136,.4); } 50% { box-shadow:0 0 0 12px rgba(13,148,136,0); } }
        @keyframes diskHeroSheen { 0% { background-position: 0% 50%; } 100% { background-position: 200% 50%; } }
        #HardDiskHw .cpu-hero { position:relative; overflow:hidden; border-radius:var(--radius-lg); box-shadow:var(--shadow-md); }
        #HardDiskHw .cpu-hero::before {
            content:''; position:absolute; inset:0; pointer-events:none;
            background:linear-gradient(120deg, transparent 20%, rgba(255,255,255,.4) 50%, transparent 80%);
            background-size:200% 100%;
            animation: diskHeroSheen 8s linear infinite;
        }
        #HardDiskHw .cpu-chip-icon { animation: diskHeroIconPulse 2.4s cubic-bezier(0.4, 0, 0.6, 1) infinite; }

        .disk-count-val { transition: color .4s ease; }
        
        /* Glass banner for risk */
        .disk-risk-banner {
            backdrop-filter: blur(8px);
            animation: diskPanelFadeIn 0.5s cubic-bezier(0.16, 1, 0.3, 1) both;
            box-shadow: 0 8px 24px -4px rgba(0,0,0,0.05);
        }
    `;
    document.head.appendChild(style);
})();

function deepStalenessBanner(deepDateVal, sectionLabel) {
    if (!deepDateVal) return '';
    let deepTime;
    try { deepTime = new Date(deepDateVal); } catch (e) { return ''; }
    if (isNaN(deepTime.getTime())) return '';

    const quickTime = window.lastQuickAuditTime ? new Date(window.lastQuickAuditTime) : null;
    const isOlderThanQuick = quickTime && !isNaN(quickTime.getTime()) && deepTime < quickTime;
    const ageHours = (Date.now() - deepTime.getTime()) / 36e5;

    if (ageHours < 24 && !isOlderThanQuick) return '';

    const when = deepTime.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
    return `<div class="disk-stale-banner">
        <i class="fas fa-triangle-exclamation" style="color:#d97706;font-size:1rem;flex-shrink:0;"></i>
        <div style="font-size:.78rem;color:#92400e;line-height:1.4;">
            <strong>${sectionLabel} data is not current.</strong> Last captured ${when}.
            Quick Audit does not refresh this section — run <strong>Deep Audit (DST)</strong> above to update it.
        </div>
    </div>`;
}

function setDiskTabDot(dotId, dateVal, opts) {
    opts = opts || {};
    const staleHours = opts.staleHours ?? 24;
    const veryStaleHours = opts.veryStaleHours ?? 24 * 7;
    const $dot = $('#' + dotId);
    if (!$dot.length) return;

    if (!dateVal) { $dot.css('background', 'var(--slate-300)'); return; }
    const d = new Date(dateVal);
    if (isNaN(d.getTime())) { $dot.css('background', 'var(--slate-300)'); return; }

    const ageHours = (Date.now() - d.getTime()) / 36e5;
    let color = '#22c55e';
    if (ageHours >= veryStaleHours) color = '#ef4444';
    else if (ageHours >= staleHours) color = '#f59e0b';
    $dot.css('background', color);
}

window.sysAlert = function (msg, type) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            text: msg,
            icon: type || 'info',
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3500,
            background: '#1e293b',
            color: '#ffffff'
        });
    } else {
        alert(msg);
    }
};

$(document).ready(function () {
    initTabStyles();

    domaindata = $('#domainid').val();
    actualDomainName = $('#domainName').val() || domaindata;

    if (!domaindata) {
        console.error("Domain ID not found â€” skipping data load");
        return;
    }

    loadSummaryData();
    loadOSDetails();
    loadDeviceDetails();
    loadLogicalDrivesDashboard();

    setTimeout(function () {
        initializeAllTables();
    }, 500);
});

function lazyLoadTabData(tabId) {
    switch (tabId) {
        case '#Hardware':
            loadBiosDetails();
            loadBatteryDetails();
            loadMonitorDetails();
            loadProcessorDetails(false);
            loadNetworkAdapters();
            loadKeyboardDetails();
            loadMotherboardDetails();
            loadMemoryDetails();
            loadHardDiskDetails();
            break;
        case '#Battery':
            checkBatteryReportExists();
            loadBatteryDetails();
            break;
        case '#Restriction':
            loadRestrictionData();
            break;
    }
}
function initTabStyles() {
    var loadedTabs = { '#Summary': true };

    $(document).on('click', '#mainTabList .main-tab a', function (e) {
        e.preventDefault();
        var $li = $(this).closest('li');
        var target = $(this).attr('href');
        if (!target) return;

        $('#mainTabList .main-tab').removeClass('active');
        $li.addClass('active');

        if ($li.length && $li[0].scrollIntoView) {
            $li[0].scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
        }

        $('#mainTabContent').children('.tab-pane').removeClass('active');
        $(target).addClass('active');

        if (!loadedTabs[target]) {
            loadedTabs[target] = true;
            lazyLoadTabData(target);
        }

        let g_SubTabMap = {};
        window.componentScores = { processor: 100, disk: 100, motherboard: 100, memory: 100 };

        window.updateSystemHealth = function () {
        };



        var firstSubTabMap = {
            '#Hardware': '#hardwareSubTabs .hardware-tab:first-child a',
            '#System': '#systemSubTabs .system-tab:first-child a',
            '#Software': '#softwareSubTabs .software-tab:first-child a',
            '#Security': '#securitySubTabs .security-tab:first-child a',
            '#History': '#historySubTabs .history-tab:first-child a'
        };
        if (firstSubTabMap[target]) {
            var $firstSubTab = $(firstSubTabMap[target]);
            if ($firstSubTab.length) {
                var $activeSubPane = $(target).find('.tab-pane.active');
                if ($activeSubPane.length === 0) {
                    $firstSubTab.trigger('click');
                }
            }
        }
        setTimeout(function () {
            $(window).trigger('resize');
            if ($.fn.DataTable) {
                $.each($.fn.dataTable.tables({ visible: true, api: true }), function () {
                    this.columns.adjust();
                });
            }
        }, 150);
    });

    var subTabSelector = [
        '.system-tab a',
        '.hardware-tab a',
        '.software-tab a',
        '.security-tab a',
        '.patch-sub-tab a',
        '.usb-tab a',
        '.history-tab a',
        '.updatelog-tab a',
        '.restriction-tab a',
        '.usbaudit-tab a'
    ].join(', ');

    $(document).on('click', subTabSelector, function (e) {
        e.preventDefault();
        var $li = $(this).closest('li');
        var target = $(this).attr('href');
        if (!target) return;
        $li.siblings().removeClass('active');
        $li.addClass('active');

        if ($li.length && $li[0].scrollIntoView) {
            $li[0].scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
        }

        var $paneParent = $(target).parent();
        $paneParent.children('.tab-pane').removeClass('active');
        $(target).addClass('active');

        setTimeout(function () {
            $(window).trigger('resize');
            if ($.fn.DataTable) {
                $(target).find('table.dataTable').each(function () {
                    if ($.fn.DataTable.isDataTable(this)) {
                        $(this).DataTable().columns.adjust();
                    }
                });
            }
        }, 150);
    });
}


function initializeAllTables() {
    initTable('#servicesTable', `/ComputerSummary/services?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'DisplayName') },
        {
            data: null, render: (row) => {
                const st = (row.startupType || row.StartupType || '').toString();
                let color = '#64748b', bg = '#f1f5f9';
                const stl = st.toLowerCase();
                if (stl === 'automatic' || stl === 'auto') { color = '#166534'; bg = '#dcfce7'; }
                else if (stl === 'manual') { color = '#92400e'; bg = '#fef3c7'; }
                else if (stl === 'disabled') { color = '#991b1b'; bg = '#fee2e2'; }
                return `<span style="display:inline-block;font-size:.74rem;font-weight:700;padding:2px 8px;border-radius:999px;background:${bg};color:${color};white-space:nowrap;">${st || 'Unknown'}</span>`;
            }
        },
        {
            data: null, render: (row) => {
                const state = (row.state || row.State || row.status || row.Status || '').toString();
                const running = state.toLowerCase() === 'running';
                const dot = `<span style="display:inline-block;width:7px;height:7px;border-radius:50%;background:${running ? '#22c55e' : '#94a3b8'};margin-right:5px;flex-shrink:0;"></span>`;
                return `<span style="display:inline-flex;align-items:center;font-size:.78rem;font-weight:600;color:${running ? '#166534' : '#475569'};white-space:nowrap;">${dot}${state || 'Unknown'}</span>`;
            }
        },
        { data: null, render: (row) => flexRender(row, 'LogonName') },
        { data: null, render: (row) => flexRender(row, 'DateTime') },
        {
            data: null, render: (row) => {
                const sName = (row.displayName || row.DisplayName || '').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
                const state = (row.state || row.State || row.status || row.Status || '').toLowerCase();
                const stype = (row.startupType || row.StartupType || '').toLowerCase();
                const disabled = stype === 'disabled';

                let btns = '';
                if (disabled) {
                    btns = `<span style="font-size:.72rem;color:#94a3b8;font-weight:600;padding:3px 8px;background:#f1f5f9;border-radius:6px;">Disabled</span>`;
                } else if (state === 'running') {
                    btns = `
                        <button data-svc="${sName}" data-action="stop" class="svc-btn svc-stop" title="Stop ${sName}"><i class="fas fa-stop"></i></button>
                        <button data-svc="${sName}" data-action="restart" class="svc-btn svc-restart" title="Restart ${sName}"><i class="fas fa-redo"></i></button>`;
                } else {
                    btns = `
                        <button data-svc="${sName}" data-action="start" class="svc-btn svc-start" title="Start ${sName}"><i class="fas fa-play"></i></button>`;
                }

                return `<div style="display:flex;align-items:center;gap:5px;">${btns}</div>`;
            }
        }
    ]);

    if (!document.getElementById('svcBtnStyle')) {
        const s = document.createElement('style');
        s.id = 'svcBtnStyle';
        s.textContent = `
            .svc-btn { display:inline-flex;align-items:center;justify-content:center;width:30px;height:30px;border:none;border-radius:7px;cursor:pointer;font-size:.72rem;transition:filter .15s,transform .1s;flex-shrink:0; }
            .svc-btn:hover { filter:brightness(.88);transform:scale(1.08); }
            .svc-btn:active { transform:scale(.96); }
            .svc-stop    { background:#fee2e2;color:#991b1b; }
            .svc-restart { background:#dbeafe;color:#1d4ed8; }
            .svc-start   { background:#dcfce7;color:#166534; }
            .svc-btn.loading { opacity:.35;pointer-events:none; }
            .svc-processing { display:inline-flex;align-items:center;gap:5px;font-size:.72rem;font-weight:600;color:var(--cyan,#06b6d4);white-space:nowrap; }
        `;
        document.head.appendChild(s);
    }

    $(document).off('click.svc').on('click.svc', '.svc-btn', function () {
        const $btn = $(this);
        const $row = $btn.closest('tr');
        const serviceName = $btn.data('svc');
        const action = $btn.data('action');

        if (!serviceName) {
            sysAlert('Service name could not be read. Try refreshing the page.', 'error');
            return;
        }

        const actionLabel = action.charAt(0).toUpperCase() + action.slice(1);
        if (!confirm(`${actionLabel} the service "${serviceName}"?`)) return;

        const $actionCell = $btn.closest('div');
        $actionCell.find('.svc-btn').addClass('loading');

        const $spinner = $(`<span class="svc-processing"><i class="fas fa-circle-notch fa-spin"></i> Processing...</span>`);
        $actionCell.append($spinner);

        sysAlert(`Sending ${actionLabel} command to "${serviceName}"...`, 'info');

        $.ajax({
            url: `/ComputerSummary/ControlService`,
            type: 'POST',
            timeout: 0,
            data: {
                domain: actualDomainName,
                serviceName: serviceName,
                action: action
            },
            success: function (res) {
                if (res && res.success) {
                    sysAlert(res.message || `"${serviceName}" — ${actionLabel} completed.`, 'success');
                    if ($.fn.DataTable.isDataTable('#servicesTable')) {
                        $('#servicesTable').DataTable().ajax.reload(null, false);
                    }
                } else {
                    sysAlert(res && res.message ? res.message : `Command sent but state not confirmed yet.`, 'warning');
                    $spinner.remove();
                    $actionCell.find('.svc-btn').removeClass('loading');
                }
            },
            error: function () {
                sysAlert(`Could not reach the device. It may be offline.`, 'error');
                $spinner.remove();
                $actionCell.find('.svc-btn').removeClass('loading');
            }
        });
    });

    initTable('#usersTable', `/ComputerSummary/users?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'UserName') },
        { data: null, render: (row) => flexRender(row, 'DomainName') },
        { data: null, render: (row) => flexRender(row, 'FullName') },
        { data: null, render: (row) => flexRender(row, 'AccountType') },
        { data: null, render: (row) => flexRender(row, 'Status') },
        { data: null, render: (row) => flexRender(row, 'LastLogin', 'LastLogin', 'Last Login') }
    ]);

    initTable('#groupsTable', `/ComputerSummary/groups?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'Description') },
        { data: null, render: (row) => flexRender(row, 'SID', 'Sid') },
        { data: null, render: (row) => flexRender(row, 'Status') }
    ]);

    window.expandedDriversGroups = window.expandedDriversGroups || {};

    function getDeviceIcon(category) {
        let icon = 'fa-microchip';
        if (!category) return icon;
        const g = category.toLowerCase();

        if (g.includes('computer')) icon = 'fa-desktop';
        else if (g.includes('display') || g.includes('monitor')) icon = 'fa-tv';
        else if (g.includes('audio') || g.includes('sound') || g.includes('media')) icon = 'fa-volume-up';
        else if (g.includes('disk') || g.includes('volume') || g.includes('drive')) icon = 'fa-hdd';
        else if (g.includes('mouse') || g.includes('pointing')) icon = 'fa-mouse';
        else if (g.includes('keyboard')) icon = 'fa-keyboard';
        else if (g.includes('print') || g.includes('fax')) icon = 'fa-print';
        else if (g.includes('net') || g.includes('wan') || g.includes('wi-fi') || g.includes('bluetooth')) icon = 'fa-network-wired';
        else if (g.includes('usb')) icon = 'fa-usb';
        else if (g.includes('processor') || g.includes('cpu')) icon = 'fa-microchip';
        else if (g.includes('hid') || g.includes('human interface')) icon = 'fa-gamepad';
        else if (g.includes('storage') || g.includes('scsi')) icon = 'fa-database';
        else if (g.includes('system') || g.includes('board') || g.includes('ide') || g.includes('hdc')) icon = 'fa-microchip';

        return icon;
    }

    initTable('#driversTable', `/ComputerSummary/drivers?domain=${domaindata}`, [
        { data: function (row) { return row.Category || row.category || 'Other Devices'; }, visible: false },
        {
            data: null,
            render: function (data, type, row) {
                let icon = getDeviceIcon(row.Category || row.category);
                return `<div class="flex items-center relative pl-6">
                            <div class="absolute left-2 top-0 bottom-0 w-px bg-slate-300"></div>
                            <div class="absolute left-2 top-1/2 w-3 h-px bg-slate-300"></div>
                            <i class="fas ${icon} mr-2 text-slate-500 w-4 text-center z-10 bg-white"></i>
                            <span class="z-10 truncate" title="${flexRender(row, 'DeviceName', 'Name')}">${flexRender(row, 'DeviceName', 'Name')}</span>
                        </div>`;
            }
        },
        { data: null, render: (row) => flexRender(row, 'Manufacturer', 'Description') },
        { data: null, render: (row) => flexRender(row, 'Status') },
        { data: null, render: (row) => flexRender(row, 'DateTime') }
    ], {
        order: [[0, 'asc']],
        paging: false,
        rowGroup: {
            dataSrc: function (row) {
                return row.Category || row.category || 'Other Devices';
            },
            startRender: function (rows, group) {
                let icon = getDeviceIcon(group);
                var expanded = !!window.expandedDriversGroups[group];

                rows.nodes().each(function (r) {
                    r.style.display = expanded ? '' : 'none';
                });

                return $('<tr class="cursor-pointer group-header hover:bg-slate-50 transition-colors" data-name="' + group + '"/>')
                    .append('<td colspan="5" class="bg-white font-bold text-slate-800 border-y border-slate-200 py-2 px-2 shadow-sm">' +
                        '<i class="fas fa-chevron-' + (expanded ? 'down' : 'right') + ' mr-2 w-4 text-center text-slate-400 text-xs"></i>' +
                        '<i class="fas ' + icon + ' mr-2 text-cyan-600"></i>' +
                        group +
                        ' <span class="text-xs text-slate-500 font-normal ml-2">(' + rows.count() + ')</span></td>');
            }
        }
    });

    $('#driversTable tbody').off('click', 'tr.group-header').on('click', 'tr.group-header', function () {
        var name = $(this).data('name');
        if (name) {
            window.expandedDriversGroups[name] = !window.expandedDriversGroups[name];
            $('#driversTable').DataTable().draw(false);
        }
    });


    initTable('#pointingTable', `/ComputerSummary/PointingDevices?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'Description', 'Name') },
        { data: null, render: (row) => flexRender(row, 'NumberOfButtons') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#printersTable', `/ComputerSummary/Printers?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'PrinterName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'PortName') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#soundTable', `/ComputerSummary/Sound?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#videoTable', `/ComputerSummary/VideoControllers?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'AdapterName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'AdapterRAM', 'Ram') },
        { data: null, render: (row) => flexRender(row, 'HorizontalResolution', 'Resolution') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#usbTable', `/ComputerSummary/USBControllers?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'HardwareName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'DeviceStatus', 'Status') }
    ]);

    initTable('#desktopAppsTable', `/ComputerSummary/DesktopApps?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name', 'SoftwareName') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer', 'Publisher') },
        { data: null, render: (row) => flexRender(row, 'InstalledDate', 'DateTime') },
        {
            data: null,
            render: function (data, type, row) {
                const name = flexRender(row, 'Name', 'SoftwareName');
                return '<button data-software-name="' + escapeHtml(name) + '" class="btn-uninstall px-2 py-1 bg-red-500 text-white text-xs rounded-lg hover:bg-red-600 transition">' +
                    '<i class="fas fa-trash"></i> Uninstall</button>';
            }
        }
    ]);

    initTable('#storeAppsTable', `/ComputerSummary/MicrosoftstoreApps?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name', 'DisplayName') },
        { data: null, render: (row) => flexRender(row, 'PackageFullName') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') }
    ]);

    initTable('#installersTable', `/ComputerSummary/InstallationSoft?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'SoftwareName', 'FileName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        {
            data: null,
            render: function (data, type, row) {
                const fileName = flexRender(row, 'FileName', 'Name');
                return '<button data-file-name="' + escapeHtml(fileName) + '" class="btn-install px-2 py-1 bg-green-500 text-white text-xs rounded-lg hover:bg-green-600 transition">' +
                    '<i class="fas fa-download"></i> Install</button>';
            }
        }
    ]);

    initTable('#antivirusTable', `/ComputerSummary/Antivirus?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'ProtectionStatus', 'Status') },
        { data: null, render: (row) => flexRender(row, 'LicenseStatus') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') }
    ]);

    initTable('#firewallTable', `/ComputerSummary/Firewall?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        { data: null, render: (row) => flexRender(row, 'ProtectionStatus', 'Status') },
        { data: null, render: (row) => flexRender(row, 'Version') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') }
    ]);

    initTable('#windowUpdateTable', `/ComputerSummary/Missingpatchwindow?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'PatchId', 'Id') },
        { data: null, render: (row) => flexRender(row, 'Bulletin') },
        { data: null, render: (row) => flexRender(row, 'PatchName', 'Name') },
        {
            data: null,
            render: function (data, type, row) {
                const desc = flexRender(row, 'PatchDescription', 'Description');
                if (desc && desc !== 'N/A' && desc.length > 50) {
                    return '<span title="' + escapeHtml(desc) + '">' + escapeHtml(desc.substring(0, 50)) + '...</span>';
                }
                return escapeHtml(desc) || 'N/A';
            }
        },
        { data: null, render: (row) => flexRender(row, 'Severity') },
        { data: null, render: (row) => flexRender(row, 'DateTime') }
    ]);

    initTable('#thirdPartyTable', `/ComputerSummary/Missingpatch?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'PatchName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'Vendor', 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'CurrentVersion') },
        { data: null, render: (row) => flexRender(row, 'AvailableVersion') },
        { data: null, render: (row) => flexRender(row, 'Severity') },
        {
            data: null,
            render: function (data, type, row) {
                const id = flexRender(row, 'PatchId', 'Id');
                const name = flexRender(row, 'PatchName', 'Name');
                return '<button data-patch-id="' + escapeHtml(id) + '" data-patch-name="' + escapeHtml(name) + '" class="btn-patch px-2 py-1 bg-blue-500 text-white text-xs rounded-lg hover:bg-blue-600 transition">' +
                    '<i class="fas fa-download"></i> Update</button>';
            }
        }
    ]);

    initTable('#hotfixTable', `/ComputerSummary/Hotfix?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'HotFixID', 'HotfixId') },
        { data: null, render: (row) => flexRender(row, 'Caption', 'Title') },
        { data: null, render: (row) => flexRender(row, 'InstalledOn') },
        { data: null, render: (row) => flexRender(row, 'InstalledBy') }
    ]);

    initTable('#usbAuditTable', `/ComputerSummary/UsbDeviceAudit?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'DeviceName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'UserName') },
        { data: null, render: (row) => flexRender(row, 'DeviceType') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'ConnectedTime', 'DateTime') },
        { data: null, render: (row) => flexRender(row, 'UsageDuration', 'Duration') }
    ]);

    initTable('#auditHistoryTable', `/ComputerSummary/AuditHistory?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'DeviceName', 'Name') },
        { data: null, render: (row) => flexRender(row, 'UserName', 'Username') },
        { data: null, render: (row) => flexRender(row, 'DeviceType') },
        { data: null, render: (row) => flexRender(row, 'Manufacturer') },
        { data: null, render: (row) => flexRender(row, 'ConnectedTime', 'DateTime') },
        { data: null, render: (row) => flexRender(row, 'UsageDuration', 'Duration') }
    ]);

    initTable('#loginHistoryTable', `/ComputerSummary/LoginHistory?domain=${domaindata}`, [
        { data: null, render: (row) => flexRender(row, 'UserName', 'Username') },
        { data: null, render: (row) => flexRender(row, 'LogonTime') },
        { data: null, render: (row) => flexRender(row, 'LogoffTime') },
        { data: null, render: (row) => flexRender(row, 'DateTime') }
    ]);

    initTable('#summaryLogTable', `/ComputerSummary/SummaryUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#osSummaryLogTable', `/ComputerSummary/OSSummaryUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#userLogTable', `/ComputerSummary/WindowsUserChangeAuditUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#groupLogTable', `/ComputerSummary/WindowsGroupChangeAuditUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#batteryLogTable', `/ComputerSummary/BatteryLog?domain=${domaindata}`, commonLogColumns);
    initTable('#biosLogTable', `/ComputerSummary/BiosSummaryChageUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#desktopAppsLogTable', `/ComputerSummary/DesktopAppsChangeAuditUpdateLog?domain=${domaindata}`, commonLogColumns);
    initTable('#antivirusLogTable', `/ComputerSummary/AntivirusChangeAuditUpdateLog?domain=${domaindata}`, commonLogColumns);

    $(document).on('click', '.btn-uninstall', function () {
        var name = $(this).data('software-name');
        uninstallSoftware(name);
    });

    $(document).on('click', '.btn-install', function () {
        var fileName = $(this).data('file-name');
        installSoftware(fileName);
    });

    $(document).on('click', '.btn-patch', function () {
        var patchId = $(this).data('patch-id');
        var patchName = $(this).data('patch-name');
        installPatch(patchId, patchName);
    });
}

function initTable(selector, url, columns, options) {
    options = options || {};
    dataTables[selector] = { url: url, columns: columns, options: options };
    tableRegistry[selector] = { url: url, columns: columns, options: options };

    if ($.fn.DataTable.isDataTable(selector)) {
        $(selector).DataTable().destroy();
    }

    var dtConfig = {
        ajax: {
            url: url,
            type: "GET",
            dataSrc: function (json) {
                if (Array.isArray(json)) return json;
                if (json && typeof json === 'object' && Array.isArray(json.data)) return json.data;
                return [];
            },
            error: function (xhr) {
                console.error("DataTable error for " + selector + ":", xhr.status);
                $(selector + ' tbody').html('<tr><td colspan="100" class="text-center py-8 text-slate-400"><i class="fas fa-exclamation-circle text-2xl mb-2 block"></i>Failed to load data</td></tr>');
            }
        },
        columns: columns,
        responsive: true,
        pageLength: 10,
        processing: true,
        language: {
            processing: '<div class="cs-dt-processing"><i class="fas fa-circle-notch fa-spin"></i> Loading data...</div>',
            search: "",
            searchPlaceholder: "Search records...",
            lengthMenu: "_MENU_ per page",
            info: "Showing _START_ to _END_ of _TOTAL_",
            emptyTable: '<div class="text-center py-8 text-slate-400"><i class="fas fa-inbox text-2xl mb-2 block"></i>No records found</div>',
            zeroRecords: '<div class="text-center py-8 text-slate-400"><i class="fas fa-search text-2xl mb-2 block"></i>No matching records found</div>',
            paginate: {
                previous: '<i class="fas fa-chevron-left"></i>',
                next: '<i class="fas fa-chevron-right"></i>'
            }
        },
        dom: '<"cs-dt-top"lf>rt<"cs-dt-bottom"ip>',
        drawCallback: function () {
        },
        initComplete: function () {
            var $wrapper = $(selector).closest('.dataTables_wrapper');
            $wrapper.find('.dataTables_filter').addClass('cs-dt-search-wrapper');
            $wrapper.find('.dataTables_filter input').addClass('cs-dt-search-input');
            if ($wrapper.find('.dataTables_filter i.fa-search').length === 0) {
                $wrapper.find('.dataTables_filter').prepend('<i class="fas fa-search cs-dt-search-icon"></i>');
            }
            $wrapper.find('.dataTables_length select').addClass('cs-dt-length-select');
        }
    };

    if (options) {
        Object.assign(dtConfig, options);
    }

    $(selector).DataTable(dtConfig);
}

function initTablesInPane(paneId) {
    $(paneId).find('table').each(function () {
        var tableId = '#' + $(this).attr('id');
        if (!$.fn.DataTable.isDataTable(tableId) && tableRegistry[tableId]) {
            initTable(tableId, tableRegistry[tableId].url, tableRegistry[tableId].columns, tableRegistry[tableId].options);
        }
    });
}

let memUsageChartInstance = null;
let memCapacityChartInstance = null;
let _fullMemHistory = [];
let _fullCpuHistory = [];

function formatChartLabel(dt) {
    if (isNaN(dt.getTime())) return '';
    var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return months[dt.getMonth()] + ' ' + dt.getDate() + ' ' + String(dt.getHours()).padStart(2, '0') + ':' + String(dt.getMinutes()).padStart(2, '0');
}

function chartScaleXOptions() {
    return {
        x: {
            ticks: { maxRotation: 45, minRotation: 25, font: { size: 9 }, autoSkip: true, maxTicksLimit: 12 }
        }
    };
}

function filterByRange(history, range, dateKey, startDate, endDate) {
    if (!history || !history.length || range === 'all') return history;
    var now = new Date();
    var cutoffStart, cutoffEnd;
    if (range === 'today') {
        cutoffStart = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        cutoffEnd = now;
    } else if (range === '7d') {
        cutoffStart = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        cutoffEnd = now;
    } else if (range === '30d') {
        cutoffStart = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
        cutoffEnd = now;
    } else if (range === '90d') {
        cutoffStart = new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000);
        cutoffEnd = now;
    } else if (range === 'custom' && startDate && endDate) {
        cutoffStart = new Date(startDate);
        cutoffEnd = new Date(endDate);
        cutoffEnd.setHours(23, 59, 59, 999);
    } else {
        return history;
    }
    return history.filter(function (h) {
        var val = h[dateKey] || h['DateTime'] || h['dateTime'];
        var dt = new Date(val);
        return !isNaN(dt.getTime()) && dt >= cutoffStart && dt <= cutoffEnd;
    });
}

function setActiveFilter(container, btn) {
    $(container).find('.chart-filter-btn').removeClass('active');
    if (btn) $(btn).addClass('active');
}

function filterMemChart(range, btn) {
    setActiveFilter('#memChartFilters', btn);
    if (range === 'custom') {
        var s = $('#memDateStart').val(), e = $('#memDateEnd').val();
        if (!s || !e) return;
        var filtered = filterByRange(_fullMemHistory, 'custom', 'dateTime', s, e);
        renderMemoryTrendChart(filtered, true);
    } else {
        $('#memDateStart').val(''); $('#memDateEnd').val('');
        var filtered = filterByRange(_fullMemHistory, range, 'dateTime');
        renderMemoryTrendChart(filtered, true);
    }
}

function filterCpuChart(range, btn) {
    setActiveFilter('#cpuChartFilters', btn);
    if (range === 'custom') {
        var s = $('#cpuDateStart').val(), e = $('#cpuDateEnd').val();
        if (!s || !e) return;
        var filtered = filterByRange(_fullCpuHistory, 'custom', 'dateTime', s, e);
        renderProcessorTrendCharts(filtered, true);
    } else {
        $('#cpuDateStart').val(''); $('#cpuDateEnd').val('');
        var filtered = filterByRange(_fullCpuHistory, range, 'dateTime');
        renderProcessorTrendCharts(filtered, true);
    }
}

function renderMemoryTrendChart(history, skipStore) {
    if (!history || !history.length) return;
    if (!skipStore) _fullMemHistory = history;

    var labels = history.map(function (h) {
        return formatChartLabel(new Date(h.dateTime || h.DateTime));
    });

    var usageCanvas = document.getElementById('memUsageTrendChart');
    if (usageCanvas) {
        if (memUsageChartInstance) memUsageChartInstance.destroy();
        memUsageChartInstance = new Chart(usageCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Usage %', data: history.map(h => parseFloat(h.usagePercent || h.UsagePercent) || 0), borderColor: '#0ea5e9', backgroundColor: 'rgba(14, 165, 233, .1)', borderWidth: 2, fill: true, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { mode: 'index', intersect: false } },
                scales: Object.assign({ y: { title: { display: true, text: '%' }, min: 0, max: 100 } }, chartScaleXOptions())
            }
        });
    }

    var capacityCanvas = document.getElementById('memCapacityTrendChart');
    if (capacityCanvas) {
        if (memCapacityChartInstance) memCapacityChartInstance.destroy();
        memCapacityChartInstance = new Chart(capacityCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Used (GB)', data: history.map(h => parseFloat(h.usedMemoryGB || h.UsedMemoryGB) || 0), borderColor: '#0ea5e9', borderWidth: 2, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Free (GB)', data: history.map(h => parseFloat(h.freeMemoryGB || h.FreeMemoryGB) || 0), borderColor: '#22c55e', borderWidth: 2, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } }, tooltip: { mode: 'index', intersect: false } },
                scales: Object.assign({ y: { title: { display: true, text: 'GB' }, min: 0 } }, chartScaleXOptions())
            }
        });
    }
}
function computeMemoryHealth(data) {
    var overall = data.healthScore ?? data.HealthScore ?? 100;

    window.componentScores = window.componentScores || { processor: 100, disk: 100, motherboard: 100, memory: 100 };
    window.componentScores.memory = overall;
    if (typeof window.updateSystemHealth === 'function') window.updateSystemHealth();

    var usagePct = parseFloat(data.usagePercent || data.UsagePercent || 0);
    var freeGB = parseFloat(data.freeMemoryGB || data.FreeMemoryGB || 0);
    var totalGB = parseFloat(data.installedMemoryGB || data.InstalledMemoryGB || 0);
    var modules = data.memoryModules || data.MemoryModules || [];
    var totalSlots = parseInt(data.totalSlots || data.TotalSlots || (modules.length > 0 ? Math.max(2, modules.length) : 2)) || 2;
    var usedSlots = parseInt(data.usedSlots || data.UsedSlots || modules.length) || modules.length;
    var emptySlots = Math.max(0, totalSlots - usedSlots);

    var usageScore = 100;
    if (usagePct > 90) usageScore = Math.max(20, Math.round(100 - (usagePct - 90) * 5));
    else if (usagePct > 75) usageScore = Math.round(100 - (usagePct - 75) * 2.5);
    else if (usagePct > 60) usageScore = Math.round(100 - (usagePct - 60) * 1.2);

    var configScore = overall;
    var issues = [];

    var rawIssues = data.healthIssues || data.HealthIssues;
    if (rawIssues && Array.isArray(rawIssues) && rawIssues.length > 0) {
        issues = rawIssues.slice();
    } else {
        if (usagePct > 90) {
            issues.push("Critical Memory Load: High RAM utilization at " + usagePct.toFixed(1) + "%. System may experience performance throttling.");
        } else if (usagePct > 75) {
            issues.push("Elevated Memory Usage: Memory utilization is high at " + usagePct.toFixed(1) + "%.");
        }

        if (freeGB > 0 && freeGB < 1.5 && totalGB >= 4) {
            issues.push("Low Free Memory: Only " + freeGB.toFixed(2) + " GB of available memory remains.");
        }

        if (modules.length >= 2) {
            var speeds = modules.map(m => m.speedMHz || m.SpeedMHz || m.configuredClockSpeedMHz || m.ConfiguredClockSpeedMHz || 0).filter(s => s > 0);
            var uniqueSpeeds = Array.from(new Set(speeds));
            if (uniqueSpeeds.length > 1) {
                issues.push("RAM Speed Mismatch: Modules operate at different speeds (" + uniqueSpeeds.join(" MHz, ") + " MHz). System will run at lowest clock speed.");
            }
        }

        if (emptySlots > 0) {
            issues.push("Expansion Capacity: " + emptySlots + " unpopulated RAM slot(s) available for RAM expansion.");
        }
    }

    var backendLevel = (data.healthLevel || data.HealthLevel || 'HEALTHY').toUpperCase();
    var status = data.healthLevel || data.HealthLevel || 'Healthy';

    var color = '#10b981';
    if (backendLevel === 'CRITICAL') color = '#ef4444';
    else if (backendLevel === 'WARNING') color = '#f59e0b';

    var channelMode = (usedSlots >= 2) ? 'Dual Channel' : 'Single Channel';

    return {
        overall: overall,
        configScore: Math.min(100, Math.max(0, configScore)),
        usageScore: Math.min(100, Math.max(0, usageScore)),
        status: status,
        color: color,
        channelMode: channelMode,
        emptySlots: emptySlots,
        issues: issues
    };
}

function renderMemoryHealth(data) {
    var health = computeMemoryHealth(data);

    $('#memHealthScoreText').text(health.overall + '%');

    var circumference = 283;
    var healthDash = Math.max(0, Math.min(100, health.overall)) / 100 * circumference;
    $('#memHealthCircle').css({
        'stroke-dasharray': healthDash + ', ' + circumference,
        'stroke': health.color,
        'transition': 'stroke-dasharray 1.2s ease-in-out, stroke 1s ease'
    });

    $('#memHealthBadge').text(health.status).css({
        'background': health.color + '22',
        'color': health.color
    });

    $('#memStatusBadge').html('<span class="cpu-live-dot"></span> ' + health.status).css({
        'background': health.color + '22',
        'color': health.color
    });

    $('#memConfigScore').text(health.configScore + '%');
    $('#memUsageScore').text(health.usageScore + '%');
    $('#memChannelMode').text(health.channelMode);
    $('#memEmptySlots').text(health.emptySlots);

    var list = $('#memHealthIssuesList').empty();
    if (!health.issues || health.issues.length === 0) {
        list.append('<li style="color:var(--slate-500);list-style:none;margin-left:-18px;"><i class="fas fa-check-circle" style="color:#22c55e;margin-right:6px;"></i>No diagnostic issues found. Memory is operating normally.</li>');
    } else {
        health.issues.forEach(function (issue) {
            list.append('<li style="margin-bottom:4px;"><i class="fas fa-info-circle" style="color:var(--slate-400);margin-right:6px;"></i>' + escapeHtml(issue) + '</li>');
        });
    }
}

function hasRealMemoryData(data) {
    if (!data) return false;
    var installed = data.installedMemoryGB || data.InstalledMemoryGB || 0;
    var modules = data.memoryModules || data.MemoryModules || [];
    var dt = data.dateTime || data.DateTime;
    return installed > 0 || modules.length > 0 || !!dt;
}

function renderMemoryPanel(data) {
    renderMemoryHeaderOnly(data);

    let usagePct = (data.usagePercent || data.UsagePercent || 0);
    $('#memUtilizationText').text(usagePct.toFixed(1) + '%');

    var usedGB = parseFloat(data.usedMemoryGB || data.UsedMemoryGB || 0);
    var freeGB = parseFloat(data.freeMemoryGB || data.FreeMemoryGB || 0);
    var totalGB = parseFloat(data.installedMemoryGB || data.InstalledMemoryGB || 0);
    $('#memUsedGBText').text(usedGB.toFixed(1));
    $('#memFreeGBText').text(freeGB.toFixed(1));
    $('#memTotalGBText').text(totalGB.toFixed(1));

    let utilLevel = (data.UsageLevel || data.usageLevel || 'Normal').toUpperCase();
    let utilColor = '#3b82f6';
    if (utilLevel === 'CRITICAL') utilColor = '#ef4444';
    else if (utilLevel === 'HIGH') utilColor = '#f59e0b';
    else if (utilLevel === 'WARNING') utilColor = '#0ea5e9';

    var circumference = 283;
    var utilDash = Math.max(0, Math.min(100, usagePct)) / 100 * circumference;
    $('#memUtilizationCircle').css({
        'stroke-dasharray': utilDash + ', ' + circumference,
        'stroke': utilColor,
        'transition': 'stroke-dasharray 1.2s ease-in-out, stroke 1s ease'
    });

    renderMemoryHealth(data);

    var tbody = $('#memoryModulesTable tbody').empty();
    var modules = data.memoryModules || data.MemoryModules || [];
    if (modules.length === 0) {
        tbody.append(`<tr><td colspan="6" style="padding:15px;text-align:center;color:#94a3b8;">No modules found</td></tr>`);
    } else {
        modules.forEach(m => {
            tbody.append(`
                <tr style="border-bottom:1px solid var(--slate-100);">
                    <td style="padding:10px 12px;font-family:var(--font-mono);font-size:0.8rem;color:var(--slate-700);">${m.deviceLocator || m.DeviceLocator || 'N/A'}</td>
                    <td style="padding:10px 12px;color:var(--slate-800);">${m.manufacturer || m.Manufacturer || 'N/A'}</td>
                    <td style="padding:10px 12px;color:var(--slate-800);font-weight:600;">${(m.capacityGB || m.CapacityGB || 0).toFixed(2)} GB</td>
                    <td style="padding:10px 12px;color:var(--slate-600);">${m.speedMHz || m.SpeedMHz || m.configuredClockSpeedMHz || m.ConfiguredClockSpeedMHz || 0} MHz</td>
                    <td style="padding:10px 12px;color:var(--slate-600);"><span style="background:var(--slate-100);padding:2px 6px;border-radius:4px;font-size:0.75rem;">${decodeMap(MEM_TYPE_MAP, m.memoryType || m.MemoryType)}</span></td>
                    <td style="padding:10px 12px;color:var(--slate-600);">${decodeMap(MEM_FORM_FACTOR_MAP, m.formFactor || m.FormFactor)}</td>
                </tr>
            `);
        });
    }

    $('#memAuditPlaceholder').hide();
    $('#memAuditLoading').hide();
    $('#memAuditGate').show();

    $.get(`/ComputerSummary/MemoryHistory?domain=${domaindata}`, function (history) {
        if (history && history.length > 0) {
            $('#memTrendNoData').hide();
            $('#memTrendChartWrap').show();
            renderMemoryTrendChart(history);
        } else {
            $('#memTrendChartWrap').hide();
            $('#memTrendNoData').show();
        }
    });
}

function loadMemoryDetails() {
    $.get(`/ComputerSummary/MemorySummary?domain=${domaindata}`, function (data) {
        if (hasRealMemoryData(data)) {
            renderMemoryHeaderOnly(data);
        }
    });
}

function renderMemoryHeaderOnly(data) {
    $('#memTotalCapacity').text((data.installedMemoryGB || data.InstalledMemoryGB || 0).toFixed(2) + ' GB Installed');
    $('#memUsageBadge').html(`<i class="fas fa-chart-pie"></i> ${(data.usagePercent || data.UsagePercent || 0).toFixed(1)}% Used`);

    let totalSlots = data.totalSlots || data.TotalSlots || 0;
    let usedSlots = data.usedSlots || data.UsedSlots || 0;
    $('#memSlotsBadge').html(`<i class="fas fa-grip-horizontal"></i> ${usedSlots}/${totalSlots} Slots`);

    let dt = data.dateTime || data.DateTime;
    if (dt) {
        var d = new Date(dt);
        if (!isNaN(d.getTime())) $('#memLastUpdated').text('Last audit: ' + d.toLocaleString());
    }

    $('#memInstalled').text((data.installedMemoryGB || data.InstalledMemoryGB || 0).toFixed(2) + ' GB');
    $('#memMaxSupported').text((data.maximumSupportedMemoryGB || data.MaximumSupportedMemoryGB || 0).toFixed(2) + ' GB');
    $('#memUsed').text((data.usedMemoryGB || data.UsedMemoryGB || 0).toFixed(2) + ' GB');
    $('#memFree').text((data.freeMemoryGB || data.FreeMemoryGB || 0).toFixed(2) + ' GB');
    $('#memTotalSlots').text(totalSlots);
    $('#memUsedSlots').text(usedSlots);
}

function escapeHtml(str) {
    if (str === null || str === undefined) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function loadSummaryData() {
    $.get(`/ComputerSummary/Summary?domain=${domaindata}`, function (data) {
        $("#tdTotalHardware").text(data.totalHardware || data.TotalHardware || 0);
        $("#tdTotalSoftware").text(data.totalSoftware || data.TotalSoftware || 0);
        $("#tdCommercialSoftware").text(data.commercialSoftware || data.CommercialSoftware || 0);
        $("#tdNonCommercialSoftware").text(data.nonCommercialSoftware || data.NonCommercialSoftware || 0);
        $("#tdProhibitedSoftware").text(data.prohibitedSoftware || data.ProhibitedSoftware || 0);
        $("#tdMissingPatches").text(data.missingPatches || data.MissingPatches || 0);
    }).fail(function () { console.error("Failed to load summary data"); });
}


function loadOSDetails() {
    $.get(`/ComputerSummary/OSSummary?domain=${domaindata}`, function (data) {
        $('#tdOperatingSystem').text(data.operatingSystem || data.OperatingSystem || 'N/A');
        $('#tdOSVersion').text(data.osVersion || data.OSVersion || 'N/A');
        $('#tdRegisteredTo').text(data.registeredTo || data.RegisteredTo || 'N/A');
        $('#tdProductID').text(data.productID || data.ProductID || 'N/A');
        $('#tdLicenseType').text(data.licenseType || data.LicenseType || 'N/A');
        $('#tdSystemDrive').text(data.systemDrive || data.SystemDrive || 'N/A');
        $('#tdOSCDKey').text(data.oscdKey || data.OSCDKey || 'N/A');
        $('#tdOSBuildNumber').text(data.osBuildNumber || data.OSBuildNumber || 'N/A');
    }).fail(function () { console.error("Failed to load OS details"); });
}

function loadDeviceDetails() {
    $.get(`/ComputerSummary/DeviceSummary?domain=${domaindata}`, function (data) {
        $('#tdDeviceManufacturer').text(data.deviceManufacturer || data.DeviceManufacturer || 'N/A');
        $('#tdDeviceModel').text(data.deviceModel || data.DeviceModel || 'N/A');
        $('#tdDeviceType').text(data.deviceType || data.DeviceType || 'N/A');
        $('#tdProcessor').text(data.processor || data.Processor || 'N/A');
        $('#tdMemory').text(data.memory || data.Memory || 'N/A');
        $('#tdSerialNumber').text(data.serialNumber || data.SerialNumber || 'N/A');
        $('#tdProcessorArchitecture').text(data.processorArchitecture || data.ProcessorArchitecture || 'N/A');
        $('#tdUDID').text(data.udid || data.UDID || 'N/A');
        $('#tdBatteryLevel').text(data.batteryLevel || data.BatteryLevel || 'N/A');
    }).fail(function () { console.error("Failed to load device details"); });
}

var _logicalDrivesCache = null;

function loadLogicalDrivesDashboard() {
    $.get(`/ComputerSummary/LocalDisk?domain=${domaindata}`, function (res) {
        var data = (res && res.data) ? res.data : res;
        _logicalDrivesCache = data;
        _renderSummaryDrives(data);
    }).fail(function () {
        $('#summaryLogicalDrivesContainer').html(
            '<div style="text-align:center; padding: 20px; color: var(--red); font-weight: 500; font-size: 0.9rem; grid-column: 1 / -1;">' +
            '<i class="fas fa-exclamation-triangle text-2xl mb-2 block"></i>Failed to load drive details</div>'
        );
    });
}

function loadHwPartitions() {
    if (_logicalDrivesCache !== null) {
        _renderHwPartitions(_logicalDrivesCache);
        return;
    }
    $.get(`/ComputerSummary/LocalDisk?domain=${domaindata}`, function (res) {
        var data = (res && res.data) ? res.data : res;
        _logicalDrivesCache = data; _renderHwPartitions(data);
    }).fail(function () {
        $('#hwPartitionsContainer').html(
            '<div style="padding:16px;text-align:center;color:var(--red);font-size:.83rem;">' +
            '<i class="fas fa-exclamation-triangle"></i> Failed to load partition data</div>'
        );
    });
}

function _renderSummaryDrives(data) {
    var container = $('#summaryLogicalDrivesContainer').empty();
    if (!data || !data.length) {
        container.html('<div style="text-align:center; padding: 20px; color: var(--slate-400); font-weight: 500; font-size: 0.9rem; grid-column: 1 / -1;"><i class="fas fa-inbox text-2xl mb-2 block"></i>No logical drives found</div>');
        return;
    }
    data.forEach(function (d) {
        var driveLetter = d.driveLetter || d.DriveLetter || d.Name || 'Unknown';
        var fileSystem = d.fileSystem || d.FileSystem || 'Unknown';
        var total = parseFloat(d.totalCapacity || d.Size || 0).toFixed(2);
        var free = parseFloat(d.freeSpace || d.FreeSpace || 0).toFixed(2);
        var usagePct = parseFloat(d.usagePercentage || d.Usage || 0);

        var usageLevel = (d.UsageLevel || d.usageLevel || 'Healthy').toUpperCase();
        var barColor = 'var(--cyan)';
        if (usageLevel === 'CRITICAL') barColor = 'var(--red)';
        else if (usageLevel === 'WARNING') barColor = 'var(--amber)';

        var icon = driveLetter.includes('C:')
            ? '<i class="fab fa-windows" style="color:var(--cyan); font-size: 1.15rem;"></i>'
            : '<i class="fas fa-hdd" style="color:var(--slate-400); font-size: 1.15rem;"></i>';

        var dashLen = (usagePct / 100) * 100;

        container.append(
            '<div class="cs-drive-card" style="background: var(--white); border: 1px solid var(--slate-200); border-radius: var(--radius-md); padding: 10px 14px; display: flex; gap: 14px; align-items: center; box-shadow: var(--shadow-sm); cursor: default;">' +
            '<div style="position:relative; width: 54px; height: 54px; flex-shrink: 0;">' +
            '<svg viewBox="0 0 36 36" style="width:100%; height:100%; transform: rotate(-90deg);">' +
            '<path stroke="#a7f3d0" stroke-width="3" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />' +
            '<path stroke="' + barColor + '" stroke-width="3" stroke-dasharray="' + dashLen + ', 100" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" style="stroke-linecap: round; transition: stroke-dasharray 1s ease-in-out;" />' +
            '</svg>' +
            '<div style="position:absolute; inset:0; display:flex; align-items:center; justify-content:center; font-size:0.75rem; font-weight:800; color:var(--slate-800);">' + usagePct.toFixed(0) + '%</div>' +
            '</div>' +
            '<div style="flex: 1; min-width: 0; display: flex; flex-direction: column; justify-content: center;">' +
            '<div style="display:flex; justify-content:space-between; align-items:center; margin-bottom: 2px;">' +
            '<div style="font-weight: 800; color: var(--slate-800); font-size: 0.95rem; display: flex; align-items: center; gap: 8px;">' + icon + ' ' + escapeHtml(driveLetter) + '</div>' +
            '<div style="font-size: 0.65rem; color: var(--slate-500); font-weight: 700; background: var(--slate-100); padding: 2px 5px; border-radius: 4px; letter-spacing: 0.5px;">' + escapeHtml(fileSystem) + '</div>' +
            '</div>' +
            '<div style="font-size: 0.8rem; color: var(--slate-500); font-weight: 500;">' +
            '<span style="color:#10b981; font-weight:700;">' + free + ' GB</span> free of ' + total + ' GB' +
            '</div>' +
            '</div>' +
            '</div>'
        );
    });
}

function _renderHwPartitions(data) {
    var container = $('#hwPartitionsContainer');
    if (!container.length) return;

    if (!data || !data.length) {
        container.html('<div style="padding:16px;text-align:center;color:var(--slate-400);font-size:.83rem;">No partition data available</div>');
        return;
    }

    function _driveStyle(letter, driveType) {
        var l = (letter || '').toUpperCase();
        var t = (driveType || '').toLowerCase();
        if (l.startsWith('C')) return { bg: '#eff6ff', iconBg: '#3b82f6', icon: 'fab fa-windows', label: 'System', labelBg: '#dbeafe', labelColor: '#1d4ed8' };
        if (t.includes('cd') || t.includes('dvd') || t.includes('optical') || t.includes('removable'))
            return { bg: '#faf5ff', iconBg: '#8b5cf6', icon: 'fas fa-compact-disc', label: 'Optical', labelBg: '#ede9fe', labelColor: '#6d28d9' };
        if (t.includes('network') || t.includes('remote'))
            return { bg: '#ecfdf5', iconBg: '#10b981', icon: 'fas fa-network-wired', label: 'Network', labelBg: '#d1fae5', labelColor: '#065f46' };
        if (t.includes('usb') || t.includes('removable'))
            return { bg: '#fff7ed', iconBg: '#f97316', icon: 'fas fa-usb', label: 'Removable', labelBg: '#fed7aa', labelColor: '#c2410c' };
        return { bg: '#f8fafc', iconBg: '#475569', icon: 'fas fa-hdd', label: 'Data', labelBg: '#e2e8f0', labelColor: '#334155' };
    }

    var cardsHtml = '<div class="partition-grid">';

    data.forEach(function (d) {
        var letter = d.driveLetter || d.DriveLetter || d.Name || '?:';
        var fs = d.fileSystem || d.FileSystem || 'N/A';
        var label = d.volumeName || d.VolumeName || d.VolumeLabel || '';
        var driveType = d.driveType || d.DriveType || '';
        var total = parseFloat(d.totalCapacity || d.Size || 0);
        var free = parseFloat(d.freeSpace || d.FreeSpace || 0);
        var used = parseFloat(d.usedSpace || d.UsedSpace || (total - free) || 0);
        var pct = d.UsagePercentage || d.usagePercentage || 0;
        var usageLvl = (d.UsageLevel || d.usageLevel || 'Healthy').toUpperCase();

        var style = _driveStyle(letter, driveType);

        var barColor = '#22c55e';
        if (usageLvl === 'CRITICAL') barColor = '#ef4444';
        else if (usageLvl === 'WARNING') barColor = '#f59e0b';

        var displayName = escapeHtml(letter) + (label ? ' â€” ' + escapeHtml(label) : '');

        var isOptical = (driveType || '').toLowerCase().includes('cd') ||
            (driveType || '').toLowerCase().includes('dvd') ||
            (driveType || '').toLowerCase().includes('optical');
        var noMedia = (isOptical && total === 0);

        var barSection = noMedia
            ? '<div style="font-size:.72rem;color:var(--slate-400);padding:4px 0;font-style:italic;">No media inserted</div>'
            : ('<div class="partition-bar-wrap">' +
                '<div class="partition-bar-track">' +
                '<div class="partition-bar-fill" style="width:' + pct.toFixed(1) + '%;background:' + barColor + ';"></div>' +
                '</div>' +
                '<div class="partition-bar-labels">' +
                '<span style="color:' + barColor + ';">' + pct.toFixed(1) + '% used</span>' +
                '<span style="color:#22c55e;">' + free.toFixed(2) + ' GB free</span>' +
                '</div>' +
                '</div>');

        var statsSection = noMedia ? '' :
            '<div class="partition-stats">' +
            '<div class="partition-stat"><div class="partition-stat-val">' + total.toFixed(1) + ' GB</div><div class="partition-stat-lbl">Total</div></div>' +
            '<div class="partition-stat" style="border-left:1px solid var(--slate-100);border-right:1px solid var(--slate-100);">' +
            '<div class="partition-stat-val" style="color:#f59e0b;">' + used.toFixed(1) + ' GB</div><div class="partition-stat-lbl">Used</div></div>' +
            '<div class="partition-stat"><div class="partition-stat-val" style="color:#22c55e;">' + free.toFixed(1) + ' GB</div><div class="partition-stat-lbl">Free</div></div>' +
            '</div>';

        cardsHtml +=
            '<div class="partition-card" style="background:' + style.bg + ';">' +

            '<div class="partition-card-header">' +
            '<div class="partition-drive-badge">' +
            '<div class="partition-drive-icon" style="background:' + style.iconBg + ';color:#fff;">' +
            '<i class="' + style.icon + '"></i>' +
            '</div>' +
            '<div>' +
            '<div class="partition-drive-letter">' + displayName + '</div>' +
            '</div>' +
            '</div>' +
            '<span class="partition-type-badge" style="background:' + style.labelBg + ';color:' + style.labelColor + ';">' + style.label + '</span>' +
            '</div>' +

            barSection +
            statsSection +

            '</div>';
    });

    cardsHtml += '</div>';
    container.html(cardsHtml);
}

function loadBiosDetails() {
    $.get(`/ComputerSummary/BIOS?domain=${domaindata}`, function (data) {
        $('#biosManufacturer').text(data.manufacturer || 'N/A');
        $('#biosVersion').text(data.version || 'N/A');
        $('#biosSmbiosVersion').text(data.smBiosVersion || data.smbiosVersion || 'N/A');
        $('#biosReleaseDate').text(data.releaseDate || 'N/A');
        $('#biosStatus').text(data.status || 'N/A');
        $('#biosDescription').text(data.description || 'N/A');
    }).fail(function () { console.error("Failed to load BIOS details"); });
}


function loadMonitorDetails() {
    $.get(`/ComputerSummary/Monitor?domain=${domaindata}`, function (data) {
        $('#monitorManufacturer').text(data.manufacturer || 'N/A');
        $('#monitorType').text(data.monitorType || 'N/A');
        $('#monitorResolution').text((data.screenWidth || 'N/A') + ' x ' + (data.screenHeight || 'N/A'));
        $('#monitorSerial').text(data.serialNumber || 'N/A');
        $('#monitorSize').text(data.monitorSize || 'N/A');
        $('#monitorStatus').text(data.deviceStatus || 'N/A');
    }).fail(function () { console.error("Failed to load monitor details"); });
}

const CPU_ARCH_MAP = { '0': 'x86', '1': 'MIPS', '2': 'Alpha', '3': 'PowerPC', '5': 'ARM', '6': 'Itanium-based (IA-64)', '9': 'x64', '12': 'ARM64' };
const CPU_TYPE_MAP = { '1': 'Other', '2': 'Unknown', '3': 'Central Processor', '4': 'Math Processor', '5': 'DSP Processor', '6': 'Video Processor' };
const CPU_STATUS_MAP = { '0': 'Unknown', '1': 'CPU Enabled', '2': 'Disabled by User (BIOS)', '3': 'Disabled by BIOS (Error)', '4': 'CPU Idle', '5': 'Other', '6': 'Reserved', '7': 'Other' };
const CPU_SOCKET_MAP = {
    '4': 'ZIF Socket', '20': 'LGA771', '21': 'LGA775', '25': 'LGA1366', '27': 'AM3', '29': 'LGA1156',
    '36': 'LGA1155', '37': 'LGA1356', '38': 'LGA2011', '41': 'FM1', '42': 'FM2', '45': 'LGA1150',
    '49': 'AM4', '50': 'LGA1151', '54': 'LGA3647', '57': 'LGA2066', '62': 'LGA1200', '64': 'LGA1700',
    '73': 'AM5', '84': 'LGA1851'
};

const MEM_TYPE_MAP = {
    '0': 'Unknown', '1': 'Other', '2': 'DRAM', '3': 'SDRAM', '4': 'Cache DRAM', '5': 'EDO', '6': 'EDRAM', '7': 'VRAM', '8': 'SRAM', '9': 'RAM',
    '10': 'ROM', '11': 'Flash', '12': 'EEPROM', '13': 'FEPROM', '14': 'EPROM', '15': 'CDRAM', '16': '3DRAM', '17': 'SDRAM', '18': 'SGRAM',
    '19': 'RDRAM', '20': 'DDR', '21': 'DDR2', '22': 'DDR2 FB-DIMM', '24': 'DDR3', '25': 'FBD2', '26': 'DDR4', '34': 'DDR5', '35': 'LPDDR5'
};

const MEM_FORM_FACTOR_MAP = {
    '0': 'Unknown', '1': 'Other', '2': 'SIP', '3': 'DIP', '4': 'ZIP', '5': 'SOJ', '6': 'Proprietary', '7': 'SIMM', '8': 'DIMM', '9': 'TSOP',
    '10': 'PGA', '11': 'RIMM', '12': 'SODIMM', '13': 'SRIMM', '14': 'SMD', '15': 'SSMP', '16': 'QFP', '17': 'TQFP', '18': 'SOIC',
    '19': 'LCC', '20': 'PLCC', '21': 'BGA', '22': 'FPBGA', '23': 'LGA', '24': 'FB-DIMM'
};

function decodeMap(map, code) {
    if (code === null || code === undefined) return 'N/A';
    return map[String(code)] || code || 'N/A';
}

function cpuDecode(map, code) {
    return decodeMap(map, code);
}

function cpuDecodeVoltage(raw) {
    var v = parseInt(raw, 10);
    if (isNaN(v) || v === 0) return 'N/A';
    if (v & 0x80) return ((v & 0x7f) / 10).toFixed(1) + ' V';
    return 'Index ' + v;
}

function cpuTempColor(t) {
    if (t === undefined || t === null || isNaN(t) || t <= 0) return '#94a3b8';
    if (t < 50) return '#22c55e';
    if (t < 70) return '#f59e0b';
    return '#ef4444';
}

function cpuVal(d, ...keys) {
    if (!d) return undefined;
    var origKeys = Object.keys(d);
    var lower = origKeys.map(function (k) { return k.toLowerCase(); });
    for (var i = 1; i < arguments.length; i++) {
        var key = arguments[i];
        var idx = lower.indexOf(key.toLowerCase());
        if (idx > -1) {
            var actualKey = origKeys[idx];
            if (d[actualKey] !== undefined && d[actualKey] !== null && d[actualKey] !== '') {
                return d[actualKey];
            }
        }
    }
    return undefined;
}

function loadProcessorDetails(includeAuditSections) {
    $.get(`/ComputerSummary/Processors?domain=${domaindata}`, function (data) {
        if (!data) return;
        renderProcessorHero(data);
        renderProcessorSpecs(data);
        renderProcessorCache(data);

        if (includeAuditSections || window.hasLiveAuditOccurred) {
            $('#cpuHealthPlaceholder').hide();
            $.get(`/ComputerSummary/ProcessorHistory?domain=${domaindata}&count=1`, function (hist) {
                if (hist && hist.length > 0) {
                    renderProcessorThermal(hist[0]);
                } else {
                    $('#cpuHealthPlaceholder').show();
                    $('#cpuHealthSection').hide();
                }
            });
            $.get(`/ComputerSummary/ProcessorHistory?domain=${domaindata}&count=30`, function (hist) {
                if (hist && hist.length > 1) {
                    renderProcessorTrendCharts(hist);
                } else {
                    $('#cpuTrendPlaceholder').show();
                    $('#cpuTrendSection').hide();
                }
            });
        } else {
            $('#cpuHealthPlaceholder').show();
            $('#cpuHealthSection').hide();
            $('#cpuTrendPlaceholder').show();
            $('#cpuTrendSection').hide();
        }
    }).fail(function () { console.error("Failed to load processor details"); });
}

function renderProcessorHero(d) {
    $('#cpuName').text(cpuVal(d, 'description', 'Description', 'name', 'Name', 'caption', 'Caption') || cpuVal(d, 'manufacturer', 'Manufacturer') || 'Unknown Processor');
    $('#cpuManufacturerBadge').html('<i class="fas fa-industry"></i> ' + (cpuVal(d, 'manufacturer', 'Manufacturer') || 'N/A'));

    var status = cpuVal(d, 'status', 'Status', 'cpuStatus', 'CpuStatus', 'deviceStatus', 'DeviceStatus') || 'Unknown';
    var isOk = String(status).toUpperCase() === 'OK';
    var $badge = $('#cpuStatusBadge');
    $badge.toggleClass('is-down', !isOk);
    $badge.html('<span class="cpu-live-dot"></span> ' + (isOk ? 'Operational' : status));

    var dt = cpuVal(d, 'dateTime', 'DateTime');
    if (dt) {
        var date = new Date(dt);
        if (!isNaN(date.getTime())) {
            $('#cpuLastUpdated').text('Synced ' + date.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }));
        }
    }
}

function renderProcessorSpecs(d) {
    var cores = cpuVal(d, 'cores', 'Cores', 'numberOfCores', 'NumberOfCores') || 0;
    var threads = cpuVal(d, 'logicalProcessors', 'LogicalProcessors') || cores;
    $('#cpuCoresThreads').text(cores + ' Cores' + (threads > cores ? ' / ' + threads + ' Threads' : ''));

    // Helper: convert MHz to GHz string (e.g. 3101 → "3.10 GHz")
    function mhzToGHz(mhz) {
        var val = parseFloat(mhz);
        if (!val || isNaN(val)) return null;
        return (val / 1000).toFixed(2) + ' GHz';
    }

    // Base Clock — already stored in GHz in ProcessorInfo model
    var baseGHz = parseFloat(cpuVal(d, 'baseSpeedGHz', 'BaseSpeedGHz')) || 0;
    if (baseGHz > 0) {
        $('#cpuBaseClock').text(baseGHz.toFixed(2) + ' GHz');
    } else {
        // Fallback: derive from MaxClockSpeedMHz
        var maxMHz = parseFloat(cpuVal(d, 'maxClockSpeedMHz', 'MaxClockSpeedMHz')) || 0;
        if (maxMHz > 0) {
            $('#cpuBaseClock').text((maxMHz / 1000).toFixed(2) + ' GHz');
        } else {
            $('#cpuBaseClock').text('--');
        }
    }

    // Processor Speed (Current Clock) — stored in MHz, display as GHz
    var clockMHz = parseFloat(cpuVal(d, 'currentClockSpeedMHz', 'CurrentClockSpeedMHz', 'processorSpeed', 'ProcessorSpeed')) || 0;
    $('#cpuCurrentClock').text(clockMHz > 0 ? (clockMHz / 1000).toFixed(2) + ' GHz' : '--');

    // Max Clock — stored in MHz, display as GHz
    var maxClockMHz = parseFloat(cpuVal(d, 'maxClockSpeedMHz', 'MaxClockSpeedMHz')) || 0;
    $('#cpuMaxClock').text(maxClockMHz > 0 ? (maxClockMHz / 1000).toFixed(2) + ' GHz' : '--');

    // Bus Speed — stored in MHz, display as MHz but formatted neatly
    var busMHz = parseFloat(cpuVal(d, 'busSpeedMHz', 'BusSpeedMHz', 'extClockMHz', 'ExtClockMHz', 'extClock', 'ExtClock')) || 0;
    $('#cpuBusSpeed').text(busMHz > 0 ? busMHz.toFixed(1) + ' MHz' : '--');

    $('#cpuSocket').text(cpuVal(d, 'socketDesignation', 'SocketDesignation') || 'N/A');

    $('#cpuUpgradeMethod').text(cpuVal(d, 'upgradeMethod', 'UpgradeMethod') || '--');

    var addrWidth = cpuVal(d, 'addressWidth', 'AddressWidth') || '--';
    var dataWidth = cpuVal(d, 'dataWidth', 'DataWidth') || '--';
    $('#cpuWidth').text(addrWidth + '-bit / ' + dataWidth + '-bit');

    $('#cpuVoltage').text(cpuDecodeVoltage(cpuVal(d, 'voltage', 'Voltage')));

    $('#cpuProcessorId').text(cpuVal(d, 'processorId', 'ProcessorId') || '--');
}

function renderProcessorCache(d) {
    var l1 = parseInt(cpuVal(d, 'l1CacheKB', 'L1CacheKB') || 0);
    var l2 = parseInt(cpuVal(d, 'l2CacheKB', 'L2CacheKB') || 0);
    var l3 = parseInt(cpuVal(d, 'l3CacheKB', 'L3CacheKB') || 0);

    if (!l1 && !l2 && !l3) {
        $('#cpuCacheContainer').hide();
        $('#cpuCacheTitle').hide();
        return;
    }

    $('#cpuCacheContainer').show();
    $('#cpuCacheTitle').show();

    var max = Math.max(l1, l2, l3) || 1;

    var setCache = function (sel, val) {
        var pct = (val / max) * 100;
        var label = val + ' KB';
        if (val >= 1024) { label = (val / 1024).toFixed(1) + ' MB'; }

        var $row = $('#cpuCacheContainer .cpu-cache-row').filter(function () { return $(this).find('.cpu-cache-label').text() === sel; });
        if ($row.length) {
            $row.find('.cpu-cache-fill').css('width', pct + '%').css('background', val > 0 ? 'var(--cyan)' : 'var(--slate-200)');
            $row.find('.cpu-cache-value').text(val > 0 ? label : '--');
        }
    };

    setCache('L1', l1);
    setCache('L2', l2);
    setCache('L3', l3);
}

function renderProcessorThermal(d) {
    var pkgTemp = parseFloat(cpuVal(d, 'cpuPackageTemperature', 'CpuPackageTemperature', 'packageTemperature', 'PackageTemperature', 'cpuTemperature', 'CpuTemperature')) || 0;
    var pkgPower = parseFloat(cpuVal(d, 'cpuPackagePower', 'CpuPackagePower', 'packagePower', 'PackagePower', 'powerDraw', 'PowerDraw')) || 0;

    $('#cpuHealthPlaceholder').hide();
    $('#cpuHealthSection').show();

    var healthScore = d.HealthScore || d.healthScore || 100;
    var healthStatus = d.HealthLevel || d.healthLevel || d.HealthStatus || d.healthStatus || 'Healthy';
    var healthColor;
    var upperLevel = healthStatus.toUpperCase();
    if (upperLevel === 'CRITICAL') healthColor = '#ef4444';
    else if (upperLevel === 'WARNING') healthColor = '#f59e0b';
    else healthColor = '#22c55e';

    window.componentScores = window.componentScores || { processor: 100, disk: 100, motherboard: 100, memory: 100 };
    window.componentScores.processor = healthScore;
    if (typeof window.updateSystemHealth === 'function') window.updateSystemHealth();


    var circumference = 2 * Math.PI * 45;
    var healthDash = (healthScore / 100) * circumference;
    $('#cpuHealthCircle').css({ stroke: healthColor, 'stroke-dasharray': healthDash + ', ' + circumference, transition: 'stroke-dasharray 1.5s ease, stroke 1s ease' });
    $('#cpuHealthScoreText').text(healthScore + '%');
    $('#cpuHealthBadge').html('<i class="fas fa-' + (healthScore >= 80 ? 'check-circle' : healthScore >= 55 ? 'exclamation-triangle' : 'fire') + '"></i> ' + healthStatus)
        .css({ background: healthColor + '22', color: healthColor });

    var tempDash = Math.min(100, (pkgTemp / 100) * 100) / 100 * circumference;
    var tempColor = pkgTemp < 60 ? '#22c55e' : pkgTemp < 75 ? '#f59e0b' : '#ef4444';
    $('#cpuPackageTempCircle').css({ stroke: tempColor, 'stroke-dasharray': tempDash + ', ' + circumference });
    $('#cpuPackageTempText').text(pkgTemp.toFixed(0) + '\u00B0C');
    $('#cpuPackagePowerText').text('Power draw: ' + (pkgPower > 0 ? pkgPower.toFixed(1) + ' W' : 'N/A'));
    $('#cpuHealthStatus').html('<i class="fas fa-thermometer-half"></i> ' + healthStatus).css('color', healthColor);

    $('#cpuHealthTemp').text(pkgTemp.toFixed(0) + '\u00B0C').css('color', tempColor);
    var cores = parseInt(cpuVal(d, 'cores', 'Cores', 'numberOfCores', 'NumberOfCores')) || 0;
    var threads = parseInt(cpuVal(d, 'logicalProcessors', 'LogicalProcessors', 'numberOfLogicalProcessors', 'NumberOfLogicalProcessors')) || 0;
    $('#cpuHealthCores').text(cores > 0 ? cores + 'C / ' + threads + 'T' : '--');
    var maxMHz = parseFloat(cpuVal(d, 'maxClockSpeedMHz', 'MaxClockSpeedMHz', 'maxClockSpeed', 'MaxClockSpeed')) || 0;
    $('#cpuHealthClock').text(maxMHz > 0 ? (maxMHz / 1000).toFixed(2) + ' GHz' : '--');
    $('#cpuHealthPower').text(pkgPower > 0 ? pkgPower.toFixed(1) + ' W' : 'N/A');

    var maxTemp = 100;
    var coreReadings = [
        { label: 'Core 0', value: parseFloat(cpuVal(d, 'core0Temp', 'Core0Temp', 'coreTemp0', 'CoreTemp0')) || 0 },
        { label: 'Core 1', value: parseFloat(cpuVal(d, 'core1Temp', 'Core1Temp', 'coreTemp1', 'CoreTemp1')) || 0 },
        { label: 'Core 2', value: parseFloat(cpuVal(d, 'core2Temp', 'Core2Temp', 'coreTemp2', 'CoreTemp2')) || 0 },
        { label: 'Core 3', value: parseFloat(cpuVal(d, 'core3Temp', 'Core3Temp', 'coreTemp3', 'CoreTemp3')) || 0 }
    ].filter(function (c) { return c.value > 0; });   // only show cores with real data

    var coreHtml = '';
    if (coreReadings.length > 0) {
        coreReadings.forEach(function (c) {
            var cColor = c.value < 60 ? '#22c55e' : c.value < 75 ? '#f59e0b' : '#ef4444';
            var cDash = Math.min(100, (c.value / maxTemp) * 100);
            coreHtml += '<div class="cpu-core-gauge">' +
                '<svg viewBox="0 0 36 36" style="width:56px;height:56px;transform:rotate(-90deg);">' +
                '<path stroke="#e2e8f0" stroke-width="3.2" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />' +
                '<path stroke="' + cColor + '" stroke-width="3.2" stroke-dasharray="' + cDash + ', 100" fill="none" stroke-linecap="round" style="transition:stroke-dasharray 1s ease;" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />' +
                '</svg>' +
                '<div class="cpu-core-gauge-label">' + c.label + '</div>' +
                '<div class="cpu-core-gauge-value" style="color:' + cColor + ';font-weight:700;">' + c.value.toFixed(0) + '\u00B0C</div>' +
                '</div>';
        });
    } else {
        coreHtml = '<div style="color:var(--slate-400);font-size:.8rem;">Per-core temperature not reported by this device.</div>';
    }
    $('#cpuCoreGaugeContainer').html(coreHtml);

    $('#cpuStatusBadge').html('<span class="cpu-live-dot"></span> ' + healthStatus)
        .css({ background: healthColor + '22', color: healthColor })
        .removeClass('is-down')
        .toggleClass('is-down', healthScore < 40);

    var findings = d.Issues || d.issues || [];
    if (findings.length === 0) findings.push('No thermal issues detected — processor is operating within healthy temperature ranges.');

    var list = $('#cpuHealthIssuesList').empty();
    findings.forEach(function (f) { list.append('<li>' + escapeHtml(f) + '</li>'); });
}

let cpuTempChartInstance = null;
let cpuClockChartInstance = null;

function renderProcessorTrendCharts(history, skipStore) {
    if (!history || !history.length) {
        $('#cpuTrendSection').hide();
        $('#cpuTrendPlaceholder').show();
        $('#cpuTrendPlaceholderText').html(skipStore ? 'No samples found in the selected date range.' : 'Run <strong>Audit Processor</strong> above to see temperature and clock trend data.');
        return;
    }
    if (!skipStore) _fullCpuHistory = history;

    var hasRealTempData = history.some(function (h) {
        return (parseFloat(cpuVal(h, 'cpuPackageTemperature', 'CpuPackageTemperature', 'packageTemperature', 'PackageTemperature')) || 0) > 0;
    });
    if (!hasRealTempData) {
        $('#cpuTrendSection').hide();
        $('#cpuTrendPlaceholder').show();
        $('#cpuTrendPlaceholderText').html(skipStore ? 'No temperature samples in this date range.' : 'Run <strong>Audit Processor</strong> above to see temperature and clock trend data.');
        return;
    }

    $('#cpuTrendPlaceholder').hide();
    $('#cpuTrendSection').show();

    var labels = history.map(function (h) {
        return formatChartLabel(new Date(cpuVal(h, 'dateTime', 'DateTime')));
    });

    var tempCanvas = document.getElementById('cpuTempTrendChart');
    if (tempCanvas) {
        if (cpuTempChartInstance) cpuTempChartInstance.destroy();
        cpuTempChartInstance = new Chart(tempCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Package', data: history.map(h => parseFloat(cpuVal(h, 'cpuPackageTemperature', 'CpuPackageTemperature', 'packageTemperature', 'PackageTemperature')) || 0), borderColor: '#ef4444', backgroundColor: 'rgba(239,68,68,.08)', borderWidth: 2, fill: true, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Core 0', data: history.map(h => parseFloat(cpuVal(h, 'core0Temp', 'Core0Temp', 'coreTemp0', 'CoreTemp0')) || 0), borderColor: '#0ea5e9', borderWidth: 1.5, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Core 1', data: history.map(h => parseFloat(cpuVal(h, 'core1Temp', 'Core1Temp', 'coreTemp1', 'CoreTemp1')) || 0), borderColor: '#22c55e', borderWidth: 1.5, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Core 2', data: history.map(h => parseFloat(cpuVal(h, 'core2Temp', 'Core2Temp', 'coreTemp2', 'CoreTemp2')) || 0), borderColor: '#f59e0b', borderWidth: 1.5, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Core 3', data: history.map(h => parseFloat(cpuVal(h, 'core3Temp', 'Core3Temp', 'coreTemp3', 'CoreTemp3')) || 0), borderColor: '#a855f7', borderWidth: 1.5, fill: false, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } }, tooltip: { mode: 'index', intersect: false } },
                scales: Object.assign({ y: { title: { display: true, text: 'Â°C' } } }, chartScaleXOptions())
            }
        });
    }

    var clockCanvas = document.getElementById('cpuClockTrendChart');
    if (clockCanvas) {
        if (cpuClockChartInstance) cpuClockChartInstance.destroy();
        cpuClockChartInstance = new Chart(clockCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Current Clock (GHz)', data: history.map(h => { var v = parseFloat(cpuVal(h, 'currentClockSpeedMHz', 'CurrentClockSpeedMHz', 'currentClockSpeed', 'CurrentClockSpeed')) || 0; return v > 0 ? (v / 1000) : 0; }), borderColor: '#0ea5e9', backgroundColor: 'rgba(14,165,233,.08)', borderWidth: 2, fill: true, tension: 0.3, pointRadius: 2, pointHoverRadius: 5 },
                    { label: 'Bus Speed (MHz)', data: history.map(h => parseFloat(cpuVal(h, 'busSpeedMHz', 'BusSpeedMHz', 'extClock', 'ExtClock')) || 0), borderColor: '#cbd5e1', borderWidth: 1.5, borderDash: [5, 5], fill: false, pointRadius: 0 }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 10 } } }, tooltip: { mode: 'index', intersect: false } },
                scales: Object.assign({ y: { title: { display: true, text: 'GHz' } } }, chartScaleXOptions())
            }
        });
    }
}

function loadNetworkAdapters() {
    $.get(`/ComputerSummary/NetworkAdapters?domain=${domaindata}`, function (data) {
        var container = $('#networkContainer').empty();
        if (data && data.length) {
            data.forEach(function (a) {
                container.append(
                    '<div class="bg-gray-50 rounded-lg border border-gray-100 overflow-hidden hover:shadow-md transition-shadow">' +
                    '<div class="px-4 py-3 bg-white border-b border-gray-100"><h4 class="font-semibold text-gray-800 text-sm flex items-center gap-2">' +
                    '<i class="fas fa-network-wired text-blue-500"></i> ' + escapeHtml(a.description || 'Network Adapter') + '</h4></div>' +
                    '<div class="p-4 space-y-2 text-sm">' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">Manufacturer:</span><span class="text-gray-700">' + escapeHtml(a.manufacturer || 'N/A') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">MAC Address:</span><span class="font-mono text-gray-700">' + escapeHtml(a.macAddress || 'N/A') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">IP Address:</span><span class="font-mono text-gray-700">' + escapeHtml(a.ipAddress || 'N/A') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">DHCP:</span><span>' + (a.dhcpEnabled ? '<i class="fas fa-check-circle text-green-500"></i> Yes' : '<i class="fas fa-times-circle text-red-400"></i> No') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">Status:</span><span class="' + (a.deviceStatus === 'OK' ? 'text-green-600' : 'text-red-500') + '">' + escapeHtml(a.deviceStatus || 'N/A') + '</span></div>' +
                    '</div></div>'
                );
            });
        } else {
            container.html('<div class="text-center py-8 text-gray-400"><i class="fas fa-network-wired text-4xl mb-3 block"></i>No network adapters found</div>');
        }
    }).fail(function () {
        $('#networkContainer').html('<div class="text-center py-8 text-red-400">Failed to load network adapters</div>');
    });
}

function loadKeyboardDetails() {
    $.get(`/ComputerSummary/Keyboard?domain=${domaindata}`, function (data) {
        var container = $('#keyboardContainer').empty();
        if (data && data.length) {
            data.forEach(function (k) {
                container.append(
                    '<div class="bg-gray-50 rounded-lg border border-gray-100 p-4 hover:shadow-md transition-shadow">' +
                    '<div class="flex items-center gap-3 mb-3"><i class="fas fa-keyboard text-gray-500 text-xl"></i>' +
                    '<h4 class="font-semibold text-gray-800">' + escapeHtml(k.manufacturer || 'Keyboard') + '</h4></div>' +
                    '<div class="space-y-1 text-sm">' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">Description:</span><span class="text-gray-700">' + escapeHtml(k.description || 'N/A') + '</span></div>' +
                    '<div class="flex justify-between flex-wrap"><span class="text-gray-500">Status:</span><span class="' + (k.deviceStatus === 'OK' ? 'text-green-600' : 'text-red-500') + '">' + escapeHtml(k.deviceStatus || 'N/A') + '</span></div>' +
                    '</div></div>'
                );
            });
        } else {
            container.html('<div class="text-center py-8 text-gray-400"><i class="fas fa-keyboard text-4xl mb-3 block"></i>No keyboard devices found</div>');
        }
    }).fail(function () {
        $('#keyboardContainer').html('<div class="text-center py-8 text-red-400">Failed to load keyboard details</div>');
    });
}

function loadMotherboardDetails() {
    $.get(`/ComputerSummary/Motherboard?domain=${domaindata}`, function (data) {
        var container = $('#motherboardContainer').empty();
        if (data && data.length) {
            data.forEach(function (mb) {
                container.append(
                    '<div class="bg-gray-50 rounded-lg border border-gray-100 p-4 hover:shadow-md transition-shadow">' +
                    '<div class="flex items-center gap-3 mb-3"><i class="fas fa-microchip text-blue-500 text-xl"></i>' +
                    '<h4 class="font-semibold text-gray-800">' + escapeHtml(mb.manufacturer || 'Motherboard') + '</h4></div>' +
                    '<div class="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">' +
                    '<div><span class="text-gray-500">Model:</span> <span class="text-gray-700">' + escapeHtml(mb.model || 'N/A') + '</span></div>' +
                    '<div><span class="text-gray-500">Version:</span> <span class="text-gray-700">' + escapeHtml(mb.version || 'N/A') + '</span></div>' +
                    '<div><span class="text-gray-500">Serial:</span> <span class="font-mono text-gray-700">' + escapeHtml(mb.serialNumber || 'N/A') + '</span></div>' +
                    '<div><span class="text-gray-500">Status:</span> <span class="' + (mb.deviceStatus === 'OK' ? 'text-green-600' : 'text-red-500') + '">' + escapeHtml(mb.deviceStatus || 'N/A') + '</span></div>' +
                    '</div></div>'
                );
            });
        } else {
            container.html('<div class="text-center py-8 text-gray-400"><i class="fas fa-microchip text-4xl mb-3 block"></i>No motherboard information found</div>');
        }
    }).fail(function () {
        $('#motherboardContainer').html('<div class="text-center py-8 text-red-400">Failed to load motherboard details</div>');
    });
}


let mbChartInstances = {};

function loadMotherboardHealthLatest() {
    $('#mbAuditLoading').hide();
    $.get(`/ComputerSummary/MotherboardHealthLatest?domain=${domaindata}`, function (data) {
        if (data && (data.health || data.Health)) {
            renderMotherboardAudit(data);
            $('#mbAuditPlaceholder').hide();
            $('#mbAuditGate').show();
        } else {
            $('#mbAuditGate').hide();
            $('#mbAuditPlaceholder').show();
        }
    }).fail(function () {
        $('#mbAuditGate').hide();
        $('#mbAuditPlaceholder').show();
    });
}

$(document).on('click', '#btnAuditMotherboard', function (e) {
    e.preventDefault();
    window.hasLiveAuditOccurred = true;
    let btn = $(this);
    let originalText = btn.html();
    btn.html('<i class="fas fa-circle-notch fa-spin"></i> Processing...');
    btn.prop('disabled', true);
    btn.css('opacity', '0.7');

    $('#mbAuditPlaceholder').hide();
    $('#mbAuditGate').hide();
    $('#mbAuditLoading').show();

    sysAlert('Motherboard audit requested. This can take a little while — please wait...', 'info');

    $.ajax({
        url: '/ComputerSummary/AuditMotherboard?domain=' + encodeURIComponent(domaindata) + '&hostName=' + encodeURIComponent(actualDomainName),
        type: 'POST',
        timeout: 90000,
        success: function (res) {
            $('#mbAuditLoading').hide();
            if (res && res.success && res.data) {
                renderMotherboardAudit(res.data);
                loadMotherboardHealthHistory();
                $('#mbAuditGate').show();
                sysAlert(res.message || 'Motherboard audit completed!', 'success');
            } else {
                $('#mbAuditPlaceholder').show();
                sysAlert(res && res.message ? res.message : 'Motherboard audit failed.', 'error');
            }
        },
        error: function (xhr, status) {
            $('#mbAuditLoading').hide();
            $('#mbAuditPlaceholder').show();
            let msg = status === 'timeout' ? 'Motherboard audit timed out. The device may still be processing.' : 'Connection error while requesting motherboard audit.';
            sysAlert(msg, 'error');
        },
        complete: function () {
            btn.html(originalText);
            btn.prop('disabled', false);
            btn.css('opacity', '1');
        }
    });
});

function renderMotherboardAudit(data) {
    const health = data.health || data.Health;
    const cpu = data.cpu || data.Cpu;
    if (!health) return;

    const val = (obj, a, b) => (obj[a] ?? obj[b] ?? 0);
    const score = val(health, 'healthScore', 'HealthScore');
    const status = health.status ?? health.Status ?? 'Unknown';

    window.componentScores = window.componentScores || { processor: 100, disk: 100, motherboard: 100, memory: 100 };
    window.componentScores.motherboard = score;
    if (typeof window.updateSystemHealth === 'function') window.updateSystemHealth();


    const circumference = 283;
    const dash = (score / 100) * circumference;
    const upperStatus = (status || '').toUpperCase();
    const color = upperStatus === 'CRITICAL' ? '#ef4444' : upperStatus === 'WARNING' ? '#f59e0b' : '#22c55e';
    $('#mbHealthCircle').attr('stroke-dasharray', `${dash},${circumference}`).attr('stroke', color);
    $('#mbHealthPercentText').text(score + '%');

    const badgeBg = upperStatus === 'CRITICAL' ? '#fee2e2' : upperStatus === 'WARNING' ? '#fef3c7' : '#dcfce7';
    const badgeColor = upperStatus === 'CRITICAL' ? '#b91c1c' : upperStatus === 'WARNING' ? '#b45309' : '#15803d';
    $('#mbStatusBadge').css({ background: badgeBg, color: badgeColor })
        .html(`<span class="cpu-live-dot" style="background:${badgeColor};"></span> ${status}`);

    $('#mbBoardTemp').text(fmtMb1(val(health, 'motherboardTemperature', 'MotherboardTemperature')));
    $('#mbCpuTemp').text(fmtMb1(val(health, 'cpuTemperature', 'CpuTemperature')));
    $('#mbVolt12').text(fmtMb2(val(health, 'voltage12V', 'Voltage12V')));
    $('#mbVolt5').text(fmtMb2(val(health, 'voltage5V', 'Voltage5V')));
    $('#mbVolt33').text(fmtMb2(val(health, 'voltage3V3', 'Voltage3V3')));
    $('#mbFanRpm').text(Math.round(val(health, 'fanRPM', 'FanRPM')));
    $('#mbWheaErrors').text(val(health, 'wheaErrors', 'WheaErrors'));

    const auditDate = health.auditDate ?? health.AuditDate;
    $('#mbLastAudit').text(auditDate ? new Date(auditDate).toLocaleString() : '--');

    const issues = health.issues ?? health.Issues ?? [];
    if (issues.length > 0) {
        $('#mbIssuesList').html(issues.map(i => `<li>${escapeHtml(i)}</li>`).join(''));
        $('#mbIssuesWrap').show();
    } else {
        $('#mbIssuesWrap').hide();
    }

    if (cpu) {
        const cval = (a, b) => (cpu[a] ?? cpu[b] ?? 0);
        const cores = [
            { load: cval('cpuCore1Load', 'CpuCore1Load'), temp: cval('cpuCore1Temp', 'CpuCore1Temp'), clock: cval('cpuCore1Clock', 'CpuCore1Clock') },
            { load: cval('cpuCore2Load', 'CpuCore2Load'), temp: cval('cpuCore2Temp', 'CpuCore2Temp'), clock: cval('cpuCore2Clock', 'CpuCore2Clock') },
            { load: cval('cpuCore3Load', 'CpuCore3Load'), temp: cval('cpuCore3Temp', 'CpuCore3Temp'), clock: cval('cpuCore3Clock', 'CpuCore3Clock') },
            { load: cval('cpuCore4Load', 'CpuCore4Load'), temp: cval('cpuCore4Temp', 'CpuCore4Temp'), clock: cval('cpuCore4Clock', 'CpuCore4Clock') },
        ];
        const rows = cores.map((c, idx) =>
            `<tr style="border-bottom:1px solid var(--slate-100);">
                <td style="padding:6px 8px;">Core ${idx + 1}</td>
                <td style="padding:6px 8px;">${fmtMb1(c.load)}%</td>
                <td style="padding:6px 8px;">${fmtMb1(c.temp)}</td>
                <td style="padding:6px 8px;">${Math.round(c.clock)}</td>
            </tr>`
        ).join('');
        $('#mbCpuCoreTableBody').html(rows);
        $('#mbCpuPackagePower').text(fmtMb1(cval('cpuPackagePower', 'CpuPackagePower')));
        $('#mbCpuBusSpeed').text(fmtMb1(cval('busSpeed', 'BusSpeed')));
        $('#mbCpuMaxTemp').text(fmtMb1(cval('coreMaxTemp', 'CoreMaxTemp')));
    }
}

function fmtMb1(n) { return (typeof n === 'number') ? n.toFixed(1) : n; }
function fmtMb2(n) { return (typeof n === 'number') ? n.toFixed(2) : n; }

function loadMotherboardHealthHistory() {
    $.get(`/ComputerSummary/MotherboardHealthHistory?domain=${domaindata}`, function (history) {
        if (!history || history.length === 0) {
            $('#mbHistoryChartWrap').hide();
            $('#mbHistoryEmpty').show();
            $('#mbHistAuditCount').text('0 audits');
            return;
        }
        $('#mbHistoryChartWrap').show();
        $('#mbHistoryEmpty').hide();
        $('#mbHistAuditCount').text(history.length + ' audit' + (history.length === 1 ? '' : 's'));
        renderMotherboardHistoryCharts(history);
    }).fail(function () {
        $('#mbHistoryChartWrap').hide();
        $('#mbHistoryEmpty').show();
    });
}

function renderMotherboardHistoryCharts(history) {
    const val = (obj, a, b) => (obj[a] ?? obj[b] ?? null);
    const labels = history.map(h => {
        const d = val(h, 'auditDate', 'AuditDate');
        return d ? new Date(d).toLocaleDateString() + ' ' + new Date(d).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '';
    });

    drawMbLineChart('mbHistHealthChart', labels, [
        { label: 'Health Score %', data: history.map(h => val(h, 'healthScore', 'HealthScore')), color: '#22c55e' }
    ]);

    drawMbLineChart('mbHistTempChart', labels, [
        { label: 'Board °C', data: history.map(h => val(h, 'motherboardTemperature', 'MotherboardTemperature')), color: '#f59e0b' },
        { label: 'CPU °C', data: history.map(h => val(h, 'cpuTemperature', 'CpuTemperature')), color: '#ef4444' }
    ]);

    drawMbLineChart('mbHistVoltageChart', labels, [
        { label: '12V', data: history.map(h => val(h, 'voltage12V', 'Voltage12V')), color: '#3b82f6' },
        { label: '5V', data: history.map(h => val(h, 'voltage5V', 'Voltage5V')), color: '#8b5cf6' },
        { label: '3.3V', data: history.map(h => val(h, 'voltage3V3', 'Voltage3V3')), color: '#06b6d4' }
    ]);

    drawMbLineChart('mbHistFanChart', labels, [
        { label: 'Fan RPM', data: history.map(h => val(h, 'fanRPM', 'FanRPM')), color: '#0ea5e9' }
    ]);
}

function drawMbLineChart(canvasId, labels, series) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    if (mbChartInstances[canvasId]) {
        mbChartInstances[canvasId].destroy();
    }
    mbChartInstances[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: series.map(s => ({
                label: s.label,
                data: s.data,
                borderColor: s.color,
                backgroundColor: s.color + '22',
                borderWidth: 2,
                tension: 0.3,
                pointRadius: 2,
                fill: series.length === 1
            }))
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: series.length > 1, labels: { boxWidth: 10, font: { size: 10 } } } },
            scales: {
                x: { ticks: { font: { size: 9 } } },
                y: { ticks: { font: { size: 9 } }, beginAtZero: false }
            }
        }
    });
}

function loadRestrictionData() {
    $.get(`/ComputerSummary/RestrictionOnDevice?domain=${domaindata}`, function (data) {
        $('#tdIsCameraEnabled').html(boolToIcon(data.isCameraEnabled));
        $('#tdIsTelemetryEnabled').html(boolToIcon(data.isTelemetryEnabled));
        $('#tdCanModifyDateTime').html(boolToIcon(data.canModifyDateTime));
        $('#tdIsBluetoothEnabled').html(boolToIcon(data.isBluetoothEnabled));
    });

    $.get(`/ComputerSummary/RestrictionOnNetwork?domain=${domaindata}`, function (data) {
        $('#tdInternetSharing').html(boolToIcon(data.internetSharing));
        $('#tdVPN').html(boolToIcon(data.vpn));
        $('#tdWiFi').html(boolToIcon(data.wiFi));
        $('#tdAllowWiFiConfiguration').html(boolToIcon(data.allowWiFiConfiguration));
    });

    $.get(`/ComputerSummary/bluetootdetailsdata?domain=${domaindata}`, function (data) {
        $('#tdBluetooth').html(boolToIcon(data.bluetooth));
        $('#tdBluetoothdiscovery').html(boolToIcon(data.bluetoothdiscovery));
        $('#tdBluetoothprepairing').html(boolToIcon(data.bluetoothprepairing));
        $('#tdBluetoothservicesadvertising').html(boolToIcon(data.bluetoothservicesadvertising));
    });

    $.get(`/ComputerSummary/SecurityPrivacyDetails?domain=${domaindata}`, function (data) {
        $('#tdLocationServices').html(boolToIcon(data.locationServices));
        $('#tdIsMicrosoftAccountConnected').html(boolToIcon(data.isMicrosoftAccountConnected));
        $('#tdCanAddNonMicrosoftAccounts').html(boolToIcon(data.canAddNonMicrosoftAccounts));
        $('#tdCanResetDevice').html(boolToIcon(data.canResetDevice));
    });

    $.get(`/ComputerSummary/ApplicationSettings?domain=${domaindata}`, function (data) {
        $('#tdInstallNonStoreApps').html(boolToIcon(data.installNonStoreApps));
        $('#tdInstallAppsOnlyInDeviceMemory').html(boolToIcon(data.installAppsOnlyInDeviceMemory));
        $('#tdStoreAppDataOnlyInDeviceMemory').html(boolToIcon(data.storeAppDataOnlyInDeviceMemory));
        $('#tdAutoUpdateStoreApps').html(boolToIcon(data.autoUpdateStoreApps));
    });

    $.get(`/ComputerSummary/SocialSearchSettings?domain=${domaindata}`, function (data) {
        $('#tdCortanaEnabled').html(boolToIcon(data.cortanaEnabled));
        $('#tdSyncSettingsEnabled').html(boolToIcon(data.syncSettingsEnabled));
        $('#tdSearchLocationEnabled').html(boolToIcon(data.searchLocationEnabled));
    });
}

function boolToIcon(value) {
    if (value === true || value === 'true' || value === 'True' || value === 'Enabled') {
        return '<i class="fas fa-check-circle text-green-500 text-xl"></i>';
    }
    return '<i class="fas fa-times-circle text-red-400 text-xl"></i>';
}

function uninstallSoftware(softwareName) {
    if (!softwareName || softwareName === 'N/A') return;

    Swal.fire({
        title: 'Confirm Uninstall',
        text: 'Are you sure you want to uninstall ' + softwareName + '?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        confirmButtonText: 'Yes, Uninstall',
        cancelButtonText: 'Cancel'
    }).then(function (result) {
        if (!result.isConfirmed) return;

        sysAlert('Sending uninstall commandï¿½', 'info');

        $.ajax({
            url: '/ComputerSummary/Uninstallsoftware?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ softwareName: softwareName }),
            success: function (res) {
                if (res && res.status === 'success') {
                    sysAlert('Uninstall command sent. Checking statusï¿½', 'info');
                    pollUninstallStatus(softwareName, 0);
                } else {
                    sysAlert('Uninstall failed: ' + (res && res.message ? res.message : 'Unknown error'), 'error');
                }
            },
            error: function (xhr) {
                sysAlert('Failed to send uninstall command (HTTP ' + xhr.status + ')', 'error');
            }
        });
    });
}

function pollUninstallStatus(softwareName, attempt) {
    if (attempt > 200) {
        sysAlert('Uninstall status unknown - check the device manually.', 'warning');
        reloadTable('#desktopAppsTable');
        return;
    }
    setTimeout(function () {
        $.get('/ComputerSummary/Uninstallsoftwarestatus', {
            softwareName: softwareName,
            domain: actualDomainName
        }, function (res) {
            if (res && res.status === 'success') {
                sysAlert(softwareName + ' uninstalled successfully.', 'success');
                reloadTable('#desktopAppsTable');
            } else if (res && res.status === 'Failed') {
                sysAlert('Uninstall failed on device: ' + (res.message || ''), 'error');
                reloadTable('#desktopAppsTable');
            } else {
                pollUninstallStatus(softwareName, attempt + 1);
            }
        }).fail(function () {
            pollUninstallStatus(softwareName, attempt + 1);
        });
    }, 3000);
}

function installSoftware(fileName) {
    if (!fileName || fileName === 'N/A') return;

    Swal.fire({
        title: 'Confirm Installation',
        text: 'Are you sure you want to install ' + fileName + '?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#10b981',
        confirmButtonText: 'Yes, Install',
        cancelButtonText: 'Cancel'
    }).then(function (result) {
        if (!result.isConfirmed) return;

        sysAlert('Sending install commandï¿½', 'info');

        $.ajax({
            url: '/ComputerSummary/PatchUpdate?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ softwareName: fileName }),
            success: function (res) {
                if (res && res.status === 'success') {
                    sysAlert('Install command sent. Checking statusï¿½', 'info');
                    pollInstallStatus(fileName, 0);
                } else {
                    sysAlert('Install failed: ' + (res && res.message ? res.message : 'Unknown error'), 'error');
                }
            },
            error: function (xhr) {
                sysAlert('Failed to send install command (HTTP ' + xhr.status + ')', 'error');
            }
        });
    });
}

function pollInstallStatus(fileName, attempt) {
    if (attempt > 150) {
        sysAlert('Install status unknown - check the device manually.', 'warning');
        reloadTable('#installersTable');
        reloadTable('#desktopAppsTable');
        return;
    }
    setTimeout(function () {
        $.get('/ComputerSummary/installsoftwarestatus', {
            softwareName: fileName,
            domain: actualDomainName
        }, function (res) {
            if (res && res.status === 'success') {
                sysAlert(fileName + ' installed successfully.', 'success');
                reloadTable('#installersTable');
                reloadTable('#desktopAppsTable');
            } else if (res && res.status === 'Failed') {
                sysAlert('Install failed on device: ' + (res.message || ''), 'error');
                reloadTable('#installersTable');
            } else {
                pollInstallStatus(fileName, attempt + 1);
            }
        }).fail(function () {
            pollInstallStatus(fileName, attempt + 1);
        });
    }, 4000);
}

function installPatch(patchId, patchName) {
    if (!patchId || patchId === 'N/A') return;

    var displayName = patchName || patchId;

    Swal.fire({
        title: 'Confirm Update',
        text: 'Are you sure you want to install: ' + displayName + '?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#3b82f6',
        confirmButtonText: 'Yes, Install',
        cancelButtonText: 'Cancel'
    }).then(function (result) {
        if (!result.isConfirmed) return;

        sysAlert('Sending patch commandï¿½', 'info');
        $.ajax({
            url: '/ComputerSummary/PatchUpdate?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ softwareName: displayName }),
            success: function (res) {
                if (res && res.status === 'success') {
                    sysAlert('Patch command sent. Checking statusï¿½', 'info');
                    pollInstallStatus(displayName, 0);
                } else {
                    sysAlert('Patch failed: ' + (res && res.message ? res.message : 'Unknown error'), 'error');
                }
            },
            error: function (xhr) {
                sysAlert('Failed to send patch command (HTTP ' + xhr.status + ')', 'error');
            }
        });
    });
}

function reloadTable(selector) {
    if ($.fn.DataTable.isDataTable(selector)) {
        $(selector).DataTable().ajax.reload(null, false);
    }
}

function refreshSoftwareTable() {
    reloadTable('#desktopAppsTable');
}

function applyUsbBlock() {
    if (!domaindata) return;
    Swal.fire({
        title: 'Block USB?',
        text: 'This will disable USB ports on ' + domaindata,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        confirmButtonText: 'Yes, Block'
    }).then(function (result) {
        if (!result.isConfirmed) return;
        $.ajax({
            url: '/ComputerSummary/BlockUsb?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            success: function (res) {
                if (res && res.success) sysAlert('USB Blocked successfully', 'success');
                else sysAlert(res.message || 'Block failed', 'error');
            },
            error: function () { sysAlert('Connection error', 'error'); }
        });
    });
}

function applyUsbUnblock() {
    if (!domaindata) return;
    Swal.fire({
        title: 'Unblock USB?',
        text: 'This will enable USB ports on ' + domaindata,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#10b981',
        confirmButtonText: 'Yes, Unblock'
    }).then(function (result) {
        if (!result.isConfirmed) return;
        $.ajax({
            url: '/ComputerSummary/UnblockUsb?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            success: function (res) {
                if (res && res.success) sysAlert('USB Unblocked successfully', 'success');
                else sysAlert(res.message || 'Unblock failed', 'error');
            },
        });
    });
}

function checkBatteryReportExists() {
    $.get(`/ComputerSummary/BatteryReportExists?domain=${actualDomainName}`, function (res) {
        if (res && res.exists) {
            $('#btnViewBatteryReport').show();
        } else {
            $('#btnViewBatteryReport').hide();
        }
    });
}

function loadBatteryDetails() {
    $.get(`/ComputerSummary/Battery?domain=${actualDomainName}`, function (data) {
        if (!data) return;

        var mfr = data.manufacturer || data.Manufacturer || '';
        var status = data.status || data.Status || '';
        var battName = data.batteryName || data.BatteryName || '';
        var serial = data.serialNumber || data.SerialNumber || '';
        var chemistry = data.chemistry || data.Chemistry || '';
        var cycleCount = data.cycleCount || data.CycleCount || 0;
        var desc = data.description || data.Description || '';
        var systemType = data.systemType || data.SystemType || '';
        var noDataValues = ['No Battery', 'Not found', 'Unknow Manufacturer', '', null, undefined];
        var mfrReal = mfr && !noDataValues.includes(mfr);
        var nameReal = battName && !noDataValues.includes(battName);
        var serialReal = serial && !noDataValues.includes(serial) && serial !== 'Not found';
        var chemReal = chemistry && !noDataValues.includes(chemistry);
        var isBatteryDevice = mfrReal || nameReal || serialReal || chemReal;

        var isDesktop = !isBatteryDevice && (
            (systemType && systemType.toLowerCase() === 'desktop') ||
            mfr === 'No Battery'
        );

        if (isDesktop) {
            $('#batteryNoBatteryState').show();
            $('#batteryContentWrap').hide();
            $('#batteryActionButtons').hide();
            return;
        }

        $('#batteryNoBatteryState').hide();
        $('#batteryContentWrap').show();
        $('#batteryActionButtons').show();

        if (isBatteryDevice) {
            $('#batteryManufacturer').text(mfr || 'N/A');
            $('#batteryStatus').text(status || 'N/A');
            $('#batteryNameDb').text(battName || 'N/A');
            $('#batterySerialDb').text(serial || 'N/A');
            $('#batteryChemistryDb').text(chemistry || 'N/A');
            $('#batteryCycleCountDb').text(cycleCount > 0 ? cycleCount : 'N/A');
            $('#batteryDescription').text(desc || 'N/A');
        } else {
            $('#batteryManufacturer').text('No Battery');
            $('#batteryStatus').text('N/A');
            $('#batteryNameDb').text('N/A');
            $('#batterySerialDb').text('N/A');
            $('#batteryChemistryDb').text('N/A');
            $('#batteryCycleCountDb').text('N/A');
            $('#batteryDescription').text('Desktop — no battery installed');
        }

        let scanDate = data.scanDate || data.ScanDate || data.dateTime || data.DateTime;
        if (scanDate) {
            try {
                let d = new Date(scanDate);
                $('#batteryLastAuditDb').text(d.toLocaleString(undefined, {
                    year: 'numeric', month: 'short', day: 'numeric',
                    hour: '2-digit', minute: '2-digit'
                }));
            } catch (e) { $('#batteryLastAuditDb').text(scanDate); }
        }

        let lvl = data.batteryPercentage || data.BatteryPercentage || 0;
        let charging = data.isCharging || data.IsCharging || false;
        if (lvl > 0) {
            var lvlStatus = (data.BatteryLevelStatus || data.batteryLevelStatus || 'Healthy').toUpperCase();
            var lvlColor = lvlStatus === 'CRITICAL' ? '#ef4444' : (lvlStatus === 'WARNING' ? '#f59e0b' : '#22c55e');
            $('#batteryLevel').html(`<div style="display:flex;align-items:center;gap:5px;">
                    <span style="font-size:1rem;font-weight:700;color:${lvlColor};">${lvl}%</span>
                    <i class="fas ${charging ? 'fa-bolt' : 'fa-battery-three-quarters'}" style="color:${charging ? '#22c55e' : lvlColor};font-size:.78rem;"></i>
                </div>
                <div style="font-size:.65rem;color:var(--slate-500);margin-top:1px;">${charging ? 'Charging' : 'Discharging'}</div>`);
        } else {
            $('#batteryLevel').text('N/A');
        }
        $('#batteryHistoryChartLoading').hide();
        $('#batteryHistoryChartContainer').hide();
    }).fail(function () { console.error("Failed to load battery details"); });

    checkBatteryReportExists();
}

function renderBatteryAuditPanel(metrics) {
    if (!metrics) return;

    var mfr = metrics.manufacturer || metrics.Manufacturer || '';
    var battName = metrics.batteryName || metrics.BatteryName || '';
    var serial = metrics.serialNumber || metrics.SerialNumber || '';
    var chemistry = metrics.chemistry || metrics.Chemistry || '';
    var cycleCount = parseInt(metrics.cycleCount || metrics.CycleCount) || 0;
    var status = metrics.status || metrics.Status || 'Unknown';
    var designCap = parseInt(metrics.designCapacity || metrics.DesignCapacity) || 0;
    var fullCap = parseInt(metrics.fullChargeCapacity || metrics.FullChargeCapacity) || 0;
    var wearRate = metrics.wearRatePerMonth !== undefined ? metrics.wearRatePerMonth
        : (metrics.WearRatePerMonth !== undefined ? metrics.WearRatePerMonth : '--');
    var remaining = metrics.estimatedRemainingMonths !== undefined ? metrics.estimatedRemainingMonths
        : (metrics.EstimatedRemainingMonths !== undefined ? metrics.EstimatedRemainingMonths : undefined);
    var healthPercent = parseFloat(
        metrics.healthPercentage !== undefined ? metrics.healthPercentage :
            (metrics.HealthPercentage !== undefined ? metrics.HealthPercentage :
                (metrics.batteryHealthPercent !== undefined ? metrics.batteryHealthPercent :
                    (metrics.BatteryHealthPercent !== undefined ? metrics.BatteryHealthPercent : 0)))
    ) || 0;

    var hasValidHealthData = (healthPercent > 0 || designCap > 0);

    if (!hasValidHealthData && !mfr) return;

    $('#batteryAuditPlaceholder').hide();
    $('#batteryAuditGate').show();
    $('#batteryAuditLoading').hide();
    $('#batteryAuditResults').css('display', 'flex');

    if (mfr) $('#batteryManufacturer').text(mfr);
    if (battName) $('#batteryNameDb').text(battName);
    if (serial) $('#batterySerialDb').text(serial);
    if (chemistry) $('#batteryChemistryDb').text(chemistry);
    if (cycleCount > 0) $('#batteryCycleCountDb').text(cycleCount);
    var auditDateRaw = metrics.scanDate || metrics.ScanDate || metrics.dateTime || metrics.DateTime;
    if (auditDateRaw) {
        try {
            var auditD = new Date(auditDateRaw);
            $('#batteryLastAuditDb').text(auditD.toLocaleString(undefined, {
                year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
            }));
        } catch (e) { $('#batteryLastAuditDb').text(String(auditDateRaw)); }
    }

    $('#auditHealthPercentText').text(hasValidHealthData ? healthPercent.toFixed(1) + '%' : 'N/A');
    $('#auditDesignCap').text(designCap > 0 ? designCap.toLocaleString() : '--');
    $('#auditFullCap').text(fullCap > 0 ? fullCap.toLocaleString() : '--');
    $('#auditCycleCount').text(cycleCount > 0 ? cycleCount : '--');
    $('#auditWearRate').text(wearRate !== '--' ? Math.abs(wearRate).toFixed(1) : '--');

    var remainingText = '--';
    if (remaining !== undefined) {
        remainingText = (remaining === 999 || remaining > 120) ? 'Healthy' : remaining;
    }
    $('#auditRemainingLife').text(remainingText);

    const circle = $('#auditHealthCircle');
    var circumference = 2 * Math.PI * 45;
    var dashLen = hasValidHealthData ? (healthPercent / 100) * circumference : 0;
    circle.css('stroke-dasharray', dashLen + ', ' + circumference);

    let color = '#4ade80';
    let icon = '<i class="fas fa-check-circle" style="color:#4ade80;"></i>';

    var backendHealthLevel = (metrics.healthLevel || metrics.HealthLevel || '').toUpperCase();

    if (!hasValidHealthData || backendHealthLevel === 'UNKNOWN' || backendHealthLevel === '') {
        color = '#94a3b8'; icon = '<i class="fas fa-question-circle" style="color:#94a3b8;"></i>';
        status = 'No Data';
    } else if (backendHealthLevel === 'CRITICAL') {
        color = '#ef4444'; icon = '<i class="fas fa-times-circle" style="color:#ef4444;"></i>';
        status = status || 'Critical';
    } else if (backendHealthLevel === 'WARNING') {
        color = '#f59e0b'; icon = '<i class="fas fa-exclamation-triangle" style="color:#f59e0b;"></i>';
        status = status || 'Aging';
    }

    circle.css('stroke', color);
    $('#auditStatus').html(icon + ' <span style="color:' + color + ';">' + status + '</span>');
    $('#batteryStatus').text(status);

    var liveLvl = metrics.liveBatteryLevel !== undefined ? metrics.liveBatteryLevel
        : (metrics.LiveBatteryLevel !== undefined ? metrics.LiveBatteryLevel
            : (metrics.batteryPercentage !== undefined ? metrics.batteryPercentage
                : (metrics.BatteryPercentage !== undefined ? metrics.BatteryPercentage : null)));
    var liveDetails = metrics.liveBatteryDetails || metrics.LiveBatteryDetails || '';
    var liveCharging = metrics.isCharging !== undefined ? metrics.isCharging
        : (metrics.IsCharging !== undefined ? metrics.IsCharging : null);
    if (!liveDetails && liveCharging !== null) {
        liveDetails = liveCharging ? 'AC' : 'Battery';
    }

    if (liveLvl !== null && liveLvl !== undefined) {
        $('#liveBatteryCard').css('display', 'flex');
        $('#liveBatteryLevelText').text(liveLvl + '%');

        let liveColor = '#10b981';
        if (liveLvl <= 20) liveColor = '#ef4444';
        else if (liveLvl <= 50) liveColor = '#f59e0b';

        setTimeout(() => {
            $('#liveBatteryFill').css({ 'width': liveLvl + '%', 'background': liveColor });
        }, 100);

        if (liveDetails.toLowerCase().includes('ac') || liveDetails.toLowerCase().includes('charge')) {
            $('#liveBatteryLightning').show();
            liveDetails = 'Charging (' + liveDetails + ')';
        } else {
            $('#liveBatteryLightning').hide();
            if (liveDetails.toLowerCase() === 'battery') liveDetails = 'Discharging';
        }
        $('#liveBatteryDetailsText').text(liveDetails || 'Unknown');

        if (liveLvl > 0) {
            var auditLvlColor = liveLvl <= 20 ? '#ef4444' : (liveLvl <= 40 ? '#f59e0b' : '#22c55e');
            var isChargingNow = liveDetails.toLowerCase().indexOf('charg') !== -1;
            $('#batteryLevel').html(`
            <div style="display:flex;align-items:center;gap:5px;">
                <span style="font-size:1rem;font-weight:700;color:${auditLvlColor};">${liveLvl}%</span>
                <i class="fas ${isChargingNow ? 'fa-bolt' : 'fa-battery-three-quarters'}" style="color:${isChargingNow ? '#22c55e' : auditLvlColor};font-size:.78rem;"></i>
            </div>
            <div style="font-size:.65rem;color:var(--slate-500);margin-top:1px;">${liveDetails || (isChargingNow ? 'Charging' : 'Discharging')}</div>`);
        }
    } else {
        $('#liveBatteryCard').hide();
    }

    renderCapacityTrendChart(metrics.capacityHistory || metrics.CapacityHistory);
    renderBatteryUsageTables(
        metrics.batteryUsage || metrics.BatteryUsage,
        metrics.usageHistory || metrics.UsageHistory
    );
    checkBatteryReportExists();
}

let battHistHealthChart = null;
let battHistCapacityChart = null;
let battHistCycleChart = null;
let battHistLiveChart = null;

function loadBatteryHistoryCharts() {
    $('#batteryHistoryChartLoading').show();
    $('#batteryHistoryChartWrap').hide();
    $('#batteryHistoryNoData').hide();
    $('#batteryHistoryChartContainer').show();

    $.get(`/ComputerSummary/GetBatteryHistory?domain=${actualDomainName}`, function (rows) {
        $('#batteryHistoryChartLoading').hide();

        var validRows = (rows || []).filter(function (r) {
            var health = parseFloat(r.batteryHealthPercent || r.BatteryHealthPercent) || 0;
            var cap = parseInt(r.fullChargeCapacity || r.FullChargeCapacity) || 0;
            return health > 0 || cap > 0;
        });

        if (!validRows || validRows.length === 0) {
            $('#batteryHistoryChartContainer').hide();
            return;
        }

        var rows = validRows;
        rows.sort((a, b) => new Date(a.scanDate || a.ScanDate) - new Date(b.scanDate || b.ScanDate));

        const labels = rows.map(r => {
            let d = new Date(r.scanDate || r.ScanDate);
            return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: '2-digit' });
        });

        const healthData = rows.map(r => parseFloat(r.batteryHealthPercent || r.BatteryHealthPercent) || null);
        const capacityData = rows.map(r => parseInt(r.fullChargeCapacity || r.FullChargeCapacity) || null);
        const cycleData = rows.map(r => parseInt(r.cycleCount || r.CycleCount) || null);
        const liveData = rows.map(r => parseInt(r.batteryPercentage || r.BatteryPercentage) || null);

        $('#battHistAuditCount').text(rows.length + ' audit' + (rows.length === 1 ? '' : 's'));
        let firstDate = new Date(rows[0].scanDate || rows[0].ScanDate);
        let lastDate = new Date(rows[rows.length - 1].scanDate || rows[rows.length - 1].ScanDate);
        $('#battHistFirstAudit').text('First: ' + firstDate.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' }));
        $('#battHistLastAudit').text('Latest: ' + lastDate.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' }));

        const sharedXAxis = { ticks: { font: { size: 9 }, maxRotation: 45 } };
        const sharedOptions = {
            responsive: true, maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: { x: sharedXAxis }
        };

        if (battHistHealthChart) battHistHealthChart.destroy();
        battHistHealthChart = new Chart(
            document.getElementById('battHistHealthChart').getContext('2d'), {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Health %',
                    data: healthData,
                    borderColor: '#22c55e',
                    backgroundColor: 'rgba(34,197,94,0.12)',
                    borderWidth: 2.5, fill: true, tension: 0.3,
                    pointRadius: rows.length > 20 ? 2 : 4,
                    pointBackgroundColor: healthData.map(v =>
                        v >= 80 ? '#22c55e' : v >= 60 ? '#f59e0b' : '#ef4444'
                    )
                }]
            },
            options: {
                ...sharedOptions,
                scales: {
                    x: sharedXAxis,
                    y: { min: 0, max: 100, ticks: { callback: v => v + '%', font: { size: 10 } } }
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => `Health: ${ctx.raw}%`,
                            afterLabel: ctx => {
                                let r = rows[ctx.dataIndex];
                                let status = r.status || r.Status || '';
                                return status ? 'Status: ' + status : '';
                            }
                        }
                    }
                }
            }
        });

        if (battHistCapacityChart) battHistCapacityChart.destroy();
        battHistCapacityChart = new Chart(
            document.getElementById('battHistCapacityChart').getContext('2d'), {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Full Charge (mWh)',
                    data: capacityData,
                    backgroundColor: 'rgba(14,165,233,0.7)',
                    borderColor: '#0ea5e9',
                    borderWidth: 1,
                    borderRadius: 3
                }, {
                    label: 'Design (mWh)',
                    data: rows.map(r => parseInt(r.designCapacity || r.DesignCapacity) || null),
                    type: 'line',
                    borderColor: '#94a3b8',
                    borderWidth: 1.5,
                    borderDash: [4, 4],
                    fill: false,
                    pointRadius: 0
                }]
            },
            options: {
                ...sharedOptions,
                plugins: { legend: { display: true, position: 'bottom', labels: { font: { size: 10 } } } },
                scales: {
                    x: sharedXAxis,
                    y: { ticks: { font: { size: 10 } }, beginAtZero: false }
                }
            }
        });

        if (battHistCycleChart) battHistCycleChart.destroy();
        battHistCycleChart = new Chart(
            document.getElementById('battHistCycleChart').getContext('2d'), {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Cycle Count',
                    data: cycleData,
                    borderColor: '#a855f7',
                    backgroundColor: 'rgba(168,85,247,0.1)',
                    borderWidth: 2, fill: true, tension: 0.3,
                    pointRadius: rows.length > 20 ? 2 : 4
                }]
            },
            options: {
                ...sharedOptions,
                scales: {
                    x: sharedXAxis,
                    y: { ticks: { font: { size: 10 } }, beginAtZero: true }
                }
            }
        });

        if (battHistLiveChart) battHistLiveChart.destroy();
        battHistLiveChart = new Chart(
            document.getElementById('battHistLiveChart').getContext('2d'), {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Battery % at Audit',
                    data: liveData,
                    backgroundColor: liveData.map(v =>
                        !v ? '#e2e8f0' :
                            v <= 20 ? 'rgba(239,68,68,0.7)' :
                                v <= 50 ? 'rgba(245,158,11,0.7)' :
                                    'rgba(16,185,129,0.7)'
                    ),
                    borderRadius: 3
                }]
            },
            options: {
                ...sharedOptions,
                scales: {
                    x: sharedXAxis,
                    y: { min: 0, max: 100, ticks: { callback: v => v + '%', font: { size: 10 } } }
                }
            }
        });

        $('#batteryHistoryChartWrap').show();

    }).fail(function () {
        $('#batteryHistoryChartLoading').hide();
        $('#batteryHistoryChartContainer').hide();
    });
}

let battDrainChart = null;
let battUsageHistChart = null;

function parseDurationToMinutes(durStr) {
    if (!durStr || durStr === '-' || durStr === '--') return null;
    const parts = durStr.trim().split(':').map(Number);
    if (parts.length === 3) return parts[0] * 60 + parts[1] + parts[2] / 60;
    if (parts.length === 2) return parts[0] * 60 + parts[1];
    return null;
}

function renderBatteryUsageTables(batteryUsage, usageHistory) {
    const container = $('#usageHistoryContainer');
    let hasData = false;

    const drainCanvas = document.getElementById('battDrainChartCanvas');
    if (drainCanvas && batteryUsage && batteryUsage.length > 0) {
        hasData = true;
        const drainLabels = batteryUsage.map(r => r.startTime ? r.startTime.split(' ').slice(-1)[0] : '-');
        const drainMins = batteryUsage.map(r => parseDurationToMinutes(r.duration));
        const drainEnergy = batteryUsage.map(r => {
            if (!r.energyDrained || r.energyDrained === '-' || r.energyDrained === '- -') return null;
            const m = r.energyDrained.match(/(\d[\d,]*)\s*mWh/i);
            return m ? parseInt(m[1].replace(/,/g, '')) : null;
        });

        if (battDrainChart) battDrainChart.destroy();
        battDrainChart = new Chart(drainCanvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: drainLabels,
                datasets: [
                    {
                        label: 'Duration (min)',
                        data: drainMins,
                        backgroundColor: 'rgba(14,165,233,0.7)',
                        borderColor: '#0ea5e9',
                        borderWidth: 1,
                        borderRadius: 3,
                        yAxisID: 'yDur'
                    },
                    {
                        label: 'Energy Drained (mWh)',
                        data: drainEnergy,
                        type: 'line',
                        borderColor: '#f59e0b',
                        backgroundColor: 'rgba(245,158,11,0.12)',
                        borderWidth: 2,
                        fill: false,
                        tension: 0.3,
                        pointRadius: 3,
                        yAxisID: 'yEng'
                    }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { font: { size: 10 } } } },
                scales: {
                    x: { ticks: { font: { size: 9 }, maxRotation: 45 } },
                    yDur: { position: 'left', ticks: { font: { size: 9 } }, title: { display: true, text: 'Duration (min)', font: { size: 9 } } },
                    yEng: { position: 'right', ticks: { font: { size: 9 } }, title: { display: true, text: 'mWh', font: { size: 9 } }, grid: { drawOnChartArea: false } }
                }
            }
        });
    }

    const histCanvas = document.getElementById('battUsageHistChartCanvas');
    if (histCanvas && usageHistory && usageHistory.length > 0) {
        hasData = true;
        const histLabels = usageHistory.map(r => r.period ? r.period.split(' - ').pop() : '-');
        const batMins = usageHistory.map(r => parseDurationToMinutes(r.batteryActive));
        const acMins = usageHistory.map(r => parseDurationToMinutes(r.acActive));

        if (battUsageHistChart) battUsageHistChart.destroy();
        battUsageHistChart = new Chart(histCanvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: histLabels,
                datasets: [
                    {
                        label: 'Battery Active (min)',
                        data: batMins,
                        backgroundColor: 'rgba(168,85,247,0.7)',
                        borderColor: '#a855f7',
                        borderWidth: 1,
                        borderRadius: 3
                    },
                    {
                        label: 'AC Active (min)',
                        data: acMins,
                        backgroundColor: 'rgba(34,197,94,0.7)',
                        borderColor: '#22c55e',
                        borderWidth: 1,
                        borderRadius: 3
                    }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { font: { size: 10 } } } },
                scales: {
                    x: { stacked: false, ticks: { font: { size: 9 }, maxRotation: 45 } },
                    y: { ticks: { font: { size: 9 } }, title: { display: true, text: 'Minutes', font: { size: 9 } }, beginAtZero: true }
                }
            }
        });
    }

    container.toggle(hasData);
}
let capacityChartInstance = null;
function renderCapacityTrendChart(history) {
    const container = $('#capacityChartContainer');
    if (!history || history.length < 2) { container.hide(); return; }
    container.show();

    const canvas = document.getElementById('capacityTrendChart');
    if (!canvas) return;
    if (capacityChartInstance) capacityChartInstance.destroy();

    capacityChartInstance = new Chart(canvas.getContext('2d'), {
        type: 'line',
        data: {
            labels: history.map(h => h.period || h.Period || ''),
            datasets: [
                {
                    label: 'Full Charge Capacity (mWh)',
                    data: history.map(h => h.fullChargeCapacity ?? h.FullChargeCapacity ?? null),
                    borderColor: '#0ea5e9',
                    backgroundColor: 'rgba(14,165,233,0.1)',
                    borderWidth: 2, fill: true, tension: 0.3
                },
                {
                    label: 'Design Capacity (mWh)',
                    data: history.map(h => h.designCapacity ?? h.DesignCapacity ?? null),
                    borderColor: '#cbd5e1', borderWidth: 2,
                    borderDash: [5, 5], fill: false, pointRadius: 0
                }
            ]
        },
        options: {
            responsive: true, maintainAspectRatio: false,
            plugins: { legend: { position: 'bottom', labels: { font: { size: 10 } } } },
            scales: {
                x: { ticks: { font: { size: 9 }, maxRotation: 45 } },
                y: { ticks: { font: { size: 10 } }, beginAtZero: false }
            }
        }
    });
}

function animateDiskCountUp($el, target, suffix) {
    suffix = suffix || '%';
    const start = parseFloat($el.text()) || 0;
    const end = parseFloat(target) || 0;
    const duration = 600;
    const startTime = performance.now();
    function tick(now) {
        const progress = Math.min(1, (now - startTime) / duration);
        const eased = 1 - Math.pow(1 - progress, 3);
        const val = start + (end - start) * eased;
        $el.text(val.toFixed(0) + suffix);
        if (progress < 1) requestAnimationFrame(tick);
    }
    requestAnimationFrame(tick);
}

function diskSkeletonCards(count, height) {
    let html = '';
    for (let i = 0; i < count; i++) {
        html += `<div class="disk-skeleton-card"><div class="disk-skeleton-block" style="width:${60 + (i % 3) * 10}%;"></div><div class="disk-skeleton-block" style="height:${height || 22}px;width:90%;margin-bottom:0;"></div></div>`;
    }
    return html;
}

// Deep Audit subtab (S.M.A.R.T. full detail + DST report + benchmark) is fetched
// lazily: it's the heaviest set of calls (GetSmartData, GetHardDiskSmartAttributes,
// GetDeepDiskReport, GetHardDiskBenchmark) and most visits never open that subtab.
// `loadedForSerial` remembers which disk's data is currently in the DOM so re-clicking
// the tab, or coming back to a disk already fetched, doesn't re-hit the server.
window.diskDeepDataState = { loadedForSerial: undefined };

function showDeepAuditLoadingState() {
    $('#diskSmartSummaryContainer').html(diskSkeletonCards(2, 18));
    $('#diskDstResults').hide();
    $('#diskDstPlaceholder').show().html(
        '<i class="fas fa-circle-notch fa-spin" style="font-size:1.4rem;display:block;margin-bottom:8px;color:#8b5cf6;"></i>Loading deep audit results&hellip;'
    );
}

// Call whenever the Deep Audit subtab becomes visible, whenever the audited disk
// changes, or after a Quick/Deep audit completes. `force` re-fetches even if this
// disk's data already appears to be loaded (used after a fresh audit finishes).
function ensureDeepAuditDataLoaded(force) {
    const serial = window.currentDiskSerial || null;
    if (!force && window.diskDeepDataState.loadedForSerial === serial) return;
    window.diskDeepDataState.loadedForSerial = serial;
    showDeepAuditLoadingState();
    loadSmartDataDetails();
    loadDeepDiskReportDetails();
}

// Disk switched (or first audit gate open) — the previously loaded Deep Audit data,
// if any, belongs to a different disk/state now, so mark it stale. It will be
// re-fetched the next time the Deep Audit subtab is opened, not eagerly.
function invalidateDeepAuditData() {
    window.diskDeepDataState.loadedForSerial = undefined;
}

function loadHardDiskDetails() {
    // Show a shimmering skeleton immediately so the tab never looks empty/frozen
    // while the initial /HardDisk fetch is in flight.
    $('#diskUsageContainer').html(diskSkeletonCards(1, 30));
    $('#diskSpecsContainer').html(`<div class="cs-info-grid" style="grid-template-columns:repeat(auto-fill,minmax(130px,1fr));">${diskSkeletonCards(6, 16)}</div>`);

    $.get(`/ComputerSummary/HardDisk?domain=${domaindata}`, function (disks) {
        if (!disks || !Array.isArray(disks) || disks.length === 0) {
            $('#diskHeroName').text('No disks found');
            return;
        }
        var hasRealData = disks.some(function (d) {
            var model = d.Model || d.model || '';
            var health = d.HealthStatus || d.healthStatus || '';
            var temp = parseFloat(d.Temperature || d.temperature) || 0;
            var cap = parseFloat(d.TotalCapacity || d.totalCapacity) || 0;
            return cap > 0 && model && model !== 'Unknown' && model !== '';
        });

        if (hasRealData) {
            renderHardDiskDashboard(disks, false);
            loadHwPartitions();
            loadHardDiskHistoryChart();
            loadDiskInfoDetails();
            invalidateDeepAuditData();
            // Deep Audit subtab (SMART full detail / DST / benchmark) loads lazily —
            // see ensureDeepAuditDataLoaded(), triggered from the subtab click handler.
        } else {
            var d = disks[0];
            var model = d.Model || d.model || 'Unknown Disk';
            $('#diskHeroName').text(model);
        }
    }).fail(function () {
        $('#diskHeroName').text('Failed to load disk data');
    });
}

function renderDiskHeroOnly(disks) {
    const d = disks[0];
    const totalCap = parseFloat(d.TotalCapacity || d.totalCapacity || 0).toFixed(1);
    $('#diskHeroName').text(d.Model || d.model || 'Unknown Disk');
    $('#diskHeroCapacity').html('<i class="fas fa-database"></i> ' + totalCap + ' GB Total');
    $('#diskHeroInterface').html('<i class="fas fa-plug"></i> ' + (d.InterfaceType || d.interfaceType || 'N/A'));
    const powHrs = d.PowerOnHours || d.powerOnHours || 0;
    $('#diskHeroPowerOn').html('<i class="fas fa-clock"></i> ' + Number(powHrs).toLocaleString() + ' hrs powered');

    const backendLevel = (d.HealthLevel || d.healthLevel || 'HEALTHY').toUpperCase();
    let healthColor = '#10b981', healthText = d.HealthLevel || d.healthLevel || 'Healthy';
    if (backendLevel === 'CRITICAL') {
        healthColor = '#ef4444';
        $('#diskHealthBadge').addClass('is-down');
    } else if (backendLevel === 'WARNING') {
        healthColor = '#f59e0b';
    }
    $('#diskHealthBadge').css('color', healthColor)
        .html('<span class="cpu-live-dot" style="background:' + healthColor + ';"></span> ' + healthText);

    const dt = d.DateTime || d.dateTime;
    if (dt) {
        try {
            const parsed = new Date(dt);
            if (!isNaN(parsed)) $('#diskLastUpdated').text('Updated: ' + parsed.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }));
        } catch (e) { }
    }
}

function renderHardDiskDashboard(disks, openGate = true) {
    if (openGate) {
        $('#diskAuditLoading').hide();
        $('#diskAuditPlaceholder').hide();
        $('#diskAuditGate').show();
    }

    renderDiskHeroOnly(disks);

    const d = disks[0];
    window.currentDiskSerial = d.SerialNumber || d.serialNumber || null;


    if (disks.length > 1) {
        $('#diskSelectorWrap').show();
        let tabHtml = '';
        disks.forEach(function (disk, idx) {
            const isActive = idx === 0;
            const cap = parseFloat(disk.TotalCapacity || disk.totalCapacity || 0).toFixed(0);
            tabHtml += `<li style="cursor:pointer;">
                <a class="disk-selector-tab cpu-pill${isActive ? ' active' : ''}"
                   style="${isActive ? 'background:linear-gradient(135deg,var(--primary),#0d9488);color:#fff;border-color:transparent;box-shadow:0 2px 8px rgba(14,165,233,.25);' : 'background:#fff;border-color:var(--slate-200);color:var(--slate-600);'}"
                   data-disk-idx="${idx}">
                   <i class="fas fa-hdd" style="margin-right:4px;"></i>
                               <span style="opacity:.7;font-size:.7rem;margin-left:4px;">${cap} GB &mdash; ${(disk.Model || disk.model || 'Unknown').substring(0, 20)}</span>
                </a></li>`;
        });
        $('#diskSelectorTabs').html(tabHtml);
        $(document).off('click', '.disk-selector-tab').on('click', '.disk-selector-tab', function () {
            const idx = parseInt($(this).data('disk-idx'));
            const clickedDisk = disks[idx];
            $('.disk-selector-tab').css({ background: '#fff', color: 'var(--slate-600)', borderColor: 'var(--slate-200)', boxShadow: 'none' }).removeClass('active');
            $(this).css({ background: 'linear-gradient(135deg,var(--primary),#0d9488)', color: '#fff', borderColor: 'transparent', boxShadow: '0 2px 8px rgba(14,165,233,.25)' }).addClass('active');
            const selCap = parseFloat(clickedDisk.TotalCapacity || clickedDisk.totalCapacity || 0).toFixed(1);
            $('#diskHeroName').text(clickedDisk.Model || clickedDisk.model || 'Unknown Disk');
            $('#diskHeroCapacity').html('<i class="fas fa-database"></i> ' + selCap + ' GB Total');
            $('#diskHeroInterface').html('<i class="fas fa-plug"></i> ' + (clickedDisk.InterfaceType || clickedDisk.interfaceType || 'N/A'));
            $('#diskHeroPowerOn').html('<i class="fas fa-clock"></i> ' + Number(clickedDisk.PowerOnHours || clickedDisk.powerOnHours || 0).toLocaleString() + ' hrs powered');
            renderDiskPanels(clickedDisk);

            // Switching disks means the deep-audit panels (SMART/DST/Benchmark/Disk-Info)
            // need to be re-fetched scoped to THIS disk's serial — otherwise they keep
            // showing whichever disk was audited most recently, regardless of which tab
            // is open (the bug that made GEONIXGOLD256's tab show the WDC drive's data).
            window.currentDiskSerial = clickedDisk.SerialNumber || clickedDisk.serialNumber || null;
            loadDiskInfoDetails();
            invalidateDeepAuditData();
            // Only re-fetch Deep Audit data immediately if that subtab is the one
            // currently on screen; otherwise it loads lazily next time it's opened.
            if ($('.disk-subtab-btn.active').data('subtab') === 'deep') {
                ensureDeepAuditDataLoaded(true);
            }
        });
    }

    renderDiskPanels(d);

}

function calculateDiskRiskScore(d, extra) {
    let score = Number(d.HealthScore ?? d.healthScore ?? 100);
    const reasons = [];

    if (extra.predictFail) { score -= 30; reasons.push('Drive is reporting an imminent failure prediction'); }

    if (extra.wearVal !== null && extra.wearVal !== undefined) {
        if (extra.wearVal < 20) { score -= 20; reasons.push('Wear level critically low (' + extra.wearVal + '%)'); }
        else if (extra.wearVal < 50) { score -= 10; reasons.push('Wear level degraded (' + extra.wearVal + '%)'); }
    }

    if (extra.tempVal !== null && extra.tempVal !== undefined) {
        if (extra.tempVal >= 60) { score -= 15; reasons.push('Operating temperature very high (' + extra.tempVal + '°C)'); }
        else if (extra.tempVal >= 50) { score -= 5; reasons.push('Operating temperature elevated (' + extra.tempVal + '°C)'); }
    }

    if (extra.reallocSectors > 0) { score -= 15; reasons.push(extra.reallocSectors + ' reallocated sector(s) detected'); }
    if (extra.pendingSectors > 0) { score -= 15; reasons.push(extra.pendingSectors + ' pending sector(s) awaiting reallocation'); }
    if (extra.uncorrSectors > 0) { score -= 20; reasons.push(extra.uncorrSectors + ' uncorrectable sector(s) detected'); }
    if (extra.readErr > 0 || extra.writeErr > 0) { score -= 10; reasons.push('Read/write errors present (' + (extra.readErr + extra.writeErr) + ' total)'); }

    score = Math.max(0, Math.min(100, score));

    let level = 'Low Risk', color = '#22c55e';
    if (score < 50) { level = 'High Risk'; color = '#ef4444'; }
    else if (score < 80) { level = 'Moderate Risk'; color = '#f59e0b'; }

    return { score, level, color, reasons };
}

function renderDiskRiskBanner(d, extra) {
    const risk = calculateDiskRiskScore(d, extra);
    const reasonsHtml = risk.reasons.length
        ? risk.reasons.map(r => `<li style="margin-bottom:2px;">${r}</li>`).join('')
        : '<li>No elevated risk factors detected.</li>';

    const html = `
        <div style="background:#fff;border:1px solid ${risk.color}33;border-left:4px solid ${risk.color};border-radius:var(--radius-md);padding:14px 18px;box-shadow:var(--shadow-sm);margin-bottom:14px;display:flex;gap:16px;align-items:center;flex-wrap:wrap;">
            <div style="min-width:110px;">
                <div style="font-size:.78rem;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:#0f172a;">Reliability Risk</div>
                <div style="font-size:1.3rem;font-weight:800;color:${risk.color};">${risk.score}<span style="font-size:.7rem;font-weight:600;color:#475569;"> /100</span></div>
                <div style="font-size:.72rem;font-weight:700;color:${risk.color};">${risk.level}</div>
            </div>
            <ul style="flex:1;min-width:220px;margin:0;padding-left:18px;font-size:.74rem;color:var(--slate-600);">
                ${reasonsHtml}
            </ul>
        </div>`;
    $('#diskRiskBannerContainer').html(html);
}

function renderDiskPanels(d) {
    window.lastQuickAuditTime = d.DateTime || d.dateTime || window.lastQuickAuditTime;
    const totalCap = parseFloat(d.TotalCapacity || d.totalCapacity || 0);
    const usedGB = parseFloat(d.UsedSpaceGB || d.usedSpaceGB || 0);
    const freeGB = parseFloat(d.FreeSpaceGB || d.freeSpaceGB || 0);
    const usedPct = d.UsedPercent || d.usedPercent || 0;
    const usageLvl = (d.UsageLevel || d.usageLevel || 'Normal').toUpperCase();
    let barColor = '#22c55e';
    if (usageLvl === 'CRITICAL') barColor = '#ef4444';
    else if (usageLvl === 'HIGH') barColor = '#f59e0b';

    const wear = d.Wear || d.wear;
    const temp = d.Temperature || d.temperature;
    const predictFail = d.PredictFailure || d.predictFailure || false;
    const readErr = Number(d.ReadErrorsTotal || d.readErrorsTotal || 0);
    const writeErr = Number(d.WriteErrorsTotal || d.writeErrorsTotal || 0);
    const readCorr = Number(d.ReadErrorsCorrected || d.readErrorsCorrected || 0);
    const auditType = d.AuditType || d.auditType || 'Quick';
    const deviceId = d.DeviceId ?? d.deviceId;
    const lastScanned = d.DateTime || d.dateTime;

    let healthScore = d.HealthScore ?? d.healthScore ?? 100;

    window.componentScores = window.componentScores || { processor: 100, disk: 100, motherboard: 100, memory: 100 };
    window.componentScores.disk = healthScore;
    if (typeof window.updateSystemHealth === 'function') window.updateSystemHealth();


    let healthScoreColor = '#22c55e';
    let healthScoreText = d.healthLevel || d.HealthLevel || d.HealthStatus || 'Healthy';

    if (healthScoreText.toUpperCase() === 'CRITICAL') { healthScoreColor = '#ef4444'; }
    else if (healthScoreText.toUpperCase() === 'WARNING') { healthScoreColor = '#f59e0b'; }
    else if (healthScoreText.toUpperCase() === 'HEALTHY') { healthScoreColor = '#10b981'; }

    $('#diskHealthStatusLabel').text(healthScoreText).css('color', healthScoreColor);
    let usageScoreText = d.UsageLevel || d.usageLevel || 'Normal';
    $('#diskUsageStatusLabel').text(usageScoreText).css('color', barColor);

    if (typeof window.diskHealthChartInstance !== 'undefined' && window.diskHealthChartInstance) window.diskHealthChartInstance.destroy();
    const hlCanvas = document.getElementById('diskHealthChartCanvas');
    if (hlCanvas) {
        window.diskHealthChartInstance = new Chart(hlCanvas.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: ['Health Score', ''],
                datasets: [{
                    data: [healthScore, Math.max(0, 100 - healthScore)],
                    backgroundColor: [healthScoreColor, '#e2e8f0'],
                    borderWidth: 0,
                    cutout: '75%'
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { enabled: false } }
            }
        });
        animateDiskCountUp($('#diskHealthScoreLabel').addClass('disk-count-val').css('color', healthScoreColor), healthScore, '%');
    }

    if (typeof window.diskUsageChartInstance !== 'undefined' && window.diskUsageChartInstance) window.diskUsageChartInstance.destroy();
    const usCanvas = document.getElementById('diskUsageChartCanvas');
    if (usCanvas) {
        window.diskUsageChartInstance = new Chart(usCanvas.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: ['Used', 'Free'],
                datasets: [{
                    data: [usedPct, Math.max(0, 100 - usedPct)],
                    backgroundColor: [barColor, '#e2e8f0'],
                    borderWidth: 0,
                    cutout: '75%'
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { enabled: false } }
            }
        });
        animateDiskCountUp($('#diskUsageScoreLabel').addClass('disk-count-val').css('color', barColor), usedPct, '%');
    }

    const usageHtml = `
        <div class="disk-modern-card disk-anim-in" style="padding:16px 18px;">
            <div style="display:flex;justify-content:space-between;align-items:flex-start;flex-wrap:wrap;gap:4px;margin-bottom:10px;">
                <span style="font-size:.82rem;font-weight:800;color:#0f172a;flex:1;min-width:0;word-break:break-word;">${d.Model || d.model || 'Disk'}</span>
                <span style="font-size:.82rem;color:#334155;white-space:nowrap;">${usedGB.toFixed(2)} GB used of ${totalCap.toFixed(2)} GB</span>
            </div>
            <div style="height:12px;border-radius:6px;background:var(--slate-100);overflow:hidden;">
                <div style="height:100%;width:0%;background:${barColor};border-radius:6px;transition:width 1s cubic-bezier(.22,1,.36,1);" data-target-width="${usedPct.toFixed(1)}"></div>
            </div>
            <div style="display:flex;justify-content:space-between;margin-top:6px;">
                <span style="font-size:.78rem;color:${barColor};font-weight:700;">${usedPct.toFixed(1)}% Used</span>
                <span style="font-size:.78rem;color:#22c55e;font-weight:700;">${freeGB.toFixed(2)} GB Free</span>
            </div>
        </div>`;
    $('#diskUsageContainer').html(usageHtml);
    // Animate the bar filling in from 0 on the next frame (CSS transition needs the
    // width change to happen after initial paint, not in the same synchronous .html() call).
    requestAnimationFrame(() => {
        $('#diskUsageContainer [data-target-width]').each(function () {
            $(this).css('width', $(this).data('target-width') + '%');
        });
    });

    window.lastQuickDiskData = d;
    renderDiskRiskBanner(d, { wearVal: wear, tempVal: temp, predictFail, readErr, writeErr, reallocSectors: 0, pendingSectors: 0, uncorrSectors: 0 });
    const specsHtml = `
        <div class="cs-info-grid disk-anim-in" style="grid-template-columns:repeat(auto-fill,minmax(130px,1fr));">
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Model</div><div class="cs-info-box-value accent" style="font-size:.8rem;">${d.Model || d.model || 'N/A'}</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Manufacturer</div><div class="cs-info-box-value">${d.Manufacturer || d.manufacturer || 'N/A'}</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Serial Number</div><div class="cs-info-box-value" style="font-family:var(--font-mono);font-size:.72rem;">${d.SerialNumber || d.serialNumber || 'N/A'}</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Firmware</div><div class="cs-info-box-value" style="font-family:var(--font-mono);font-size:.74rem;">${d.FirmwareVersion || d.firmwareVersion || 'N/A'}</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Interface</div><div class="cs-info-box-value">${d.InterfaceType || d.interfaceType || 'N/A'}</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Description</div><div class="cs-info-box-value" style="font-size:.75rem;font-weight:500;">${d.Description || d.description || 'N/A'}</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Total Capacity</div><div class="cs-info-box-value accent">${parseFloat(d.TotalCapacity || d.totalCapacity || 0).toFixed(2)} GB</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Used Space</div><div class="cs-info-box-value amber">${usedGB.toFixed(2)} GB</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Free Space</div><div class="cs-info-box-value green">${freeGB.toFixed(2)} GB</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Power-On Hours</div><div class="cs-info-box-value">${Number(d.PowerOnHours || d.powerOnHours || 0).toLocaleString()} hrs</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Health Status</div><div class="cs-info-box-value" style="color:${(d.HealthStatus || '').toUpperCase() === 'HEALTHY' ? '#22c55e' : '#f59e0b'};">${d.HealthStatus || d.healthStatus || 'N/A'}</div></div>
            <div class="cs-info-box disk-modern-card"><div class="cs-info-box-label">Predict Failure</div><div class="cs-info-box-value" style="color:${(d.PredictFailure || d.predictFailure) ? '#ef4444' : '#22c55e'};">${(d.PredictFailure || d.predictFailure) ? 'Yes <i class="fas fa-exclamation-triangle"></i>' : 'No'}</div></div>
        </div>`;
    $('#diskSpecsContainer').html(specsHtml);

    const wearVal = (wear !== undefined && wear !== null) ? wear : null;
    const tempVal = (temp !== undefined && temp !== null) ? temp : null;
    const wearPct = wearVal !== null ? Math.min(100, wearVal) : 0;
    const tempNorm = tempVal !== null ? Math.min(100, (tempVal / 70) * 100) : 0;

    let wearColor = '#22c55e';
    if (wearPct >= 80) wearColor = '#ef4444';
    else if (wearPct >= 50) wearColor = '#f59e0b';

    let tempColor = '#22c55e';
    if (tempVal >= 55) tempColor = '#ef4444';
    else if (tempVal >= 45) tempColor = '#f59e0b';

    const smartHtml = `
        <div style="background:#fff;border:1px solid var(--slate-200);border-radius:var(--radius-md);padding:18px;box-shadow:var(--shadow-sm);">
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:16px;">

                ${wearVal !== null ? `
                <div>
                    <div style="font-size:.82rem;font-weight:700;color:#0f172a;margin-bottom:8px;"><i class="fas fa-tools" style="color:#f59e0b;margin-right:5px;"></i>Wear Level</div>
                    <div style="display:flex;align-items:center;gap:12px;">
                        <div style="flex:1;height:10px;border-radius:6px;background:var(--slate-100);overflow:hidden;">
                            <div style="height:100%;width:${wearPct}%;background:${wearColor};border-radius:6px;transition:width 1s;"></div>
                        </div>
                        <span style="font-size:.8rem;font-weight:800;color:${wearColor};min-width:36px;text-align:right;">${wearPct}%</span>
                    </div>
                </div>` : ''}

                ${tempVal !== null ? `
                <div>
                    <div style="font-size:.74rem;font-weight:700;color:var(--slate-600);margin-bottom:8px;"><i class="fas fa-thermometer-half" style="color:${tempColor};margin-right:5px;"></i>Temperature</div>
                    <div style="display:flex;align-items:center;gap:12px;">
                        <div style="flex:1;height:10px;border-radius:6px;background:var(--slate-100);overflow:hidden;">
                            <div style="height:100%;width:${tempNorm.toFixed(1)}%;background:${tempColor};border-radius:6px;transition:width 1s;"></div>
                        </div>
                        <span style="font-size:.8rem;font-weight:800;color:${tempColor};min-width:40px;text-align:right;">${tempVal}°C</span>
                    </div>
                </div>` : ''}

            </div>

            <div style="margin-top:16px;border-top:1px solid var(--slate-100);padding-top:14px;">
                <div style="font-size:.72rem;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:#0f172a;margin-bottom:10px;">Error Counters (Quick Audit)</div>
                <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(130px,1fr));gap:8px;">
                    ${_smartMetric('Read Errors (Total)', readErr, readErr > 0 ? '#ef4444' : '#22c55e', 'fas fa-times-circle')}
                    ${_smartMetric('Write Errors (Total)', writeErr, writeErr > 0 ? '#ef4444' : '#22c55e', 'fas fa-times-circle')}
                    ${_smartMetric('Read Errors (Corrected)', readCorr, readCorr > 0 ? '#f59e0b' : '#22c55e', 'fas fa-check-circle')}
                </div>
                <div style="font-size:.68rem;color:#475569;margin-top:6px;">Sector-level counters (reallocated / pending / uncorrectable / CRC) require Deep Audit - see S.M.A.R.T. section below once available.</div>
            </div>

            <div style="margin-top:16px;border-top:1px solid var(--slate-100);padding-top:14px;">
                <div style="font-size:.72rem;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:#0f172a;margin-bottom:10px;">Device Metadata</div>
                <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:8px;">
                    ${_smartMetric('Device ID', deviceId ?? 'N/A', '#64748b', 'fas fa-hashtag', true)}
                    ${_smartMetric('Last Scanned', lastScanned ? new Date(lastScanned).toLocaleString() : 'N/A', '#64748b', 'fas fa-history', true)}
                </div>
            </div>

            <div style="margin-top:12px;display:flex;align-items:center;gap:8px;flex-wrap:wrap;">
                <span style="font-size:.7rem;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:#0f172a;">Audit Type:</span>
                <span style="font-size:.72rem;font-weight:800;padding:3px 10px;border-radius:999px;background:${auditType === 'Deep' ? '#ede9fe' : '#ecfdf5'};color:${auditType === 'Deep' ? '#7c3aed' : '#059669'};">${auditType}</span>
                ${diskFreshnessBadge(lastScanned, { label: 'Quick data' })}
            </div>
        </div>`;
    $('#diskSmartContainer').html(smartHtml);

    // Draw Temperature Gauge Chart if canvas exists
    const tempCanvas = document.getElementById('diskTemperatureChartCanvas');
    if (tempCanvas && tempVal !== null) {
        if (window.diskTempChartInstance) window.diskTempChartInstance.destroy();

        // Gradient color for gauge based on temp
        let tempChartColor = '#22c55e'; // Green
        if (tempVal >= 55) tempChartColor = '#ef4444'; // Red
        else if (tempVal >= 45) tempChartColor = '#f59e0b'; // Orange

        window.diskTempChartInstance = new Chart(tempCanvas.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: ['Current Temp', ''],
                datasets: [{
                    data: [tempVal, Math.max(0, 100 - tempVal)],
                    backgroundColor: [tempChartColor, 'rgba(226,232,240,0.4)'],
                    borderWidth: 0,
                    circumference: 180,
                    rotation: -90,
                    cutout: '80%'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { enabled: false } }
            },
            plugins: [{
                id: 'textCenter',
                beforeDraw: function (chart) {
                    var width = chart.width, height = chart.height, ctx = chart.ctx;
                    ctx.restore();
                    var fontSize = (height / 80).toFixed(2);
                    ctx.font = "800 " + fontSize + "em 'Inter', sans-serif";
                    ctx.textBaseline = "middle";
                    ctx.fillStyle = tempChartColor;
                    var text = tempVal + "°C",
                        textX = Math.round((width - ctx.measureText(text).width) / 2),
                        textY = height - (height * 0.15); // Adjust Y to sit properly in semi-circle
                    ctx.fillText(text, textX, textY);

                    ctx.font = "600 " + (fontSize * 0.35).toFixed(2) + "em 'Inter', sans-serif";
                    ctx.fillStyle = '#64748b';
                    var subText = "Temperature",
                        subX = Math.round((width - ctx.measureText(subText).width) / 2),
                        subY = textY + (height * 0.2);
                    ctx.fillText(subText, subX, subY);
                    ctx.save();
                }
            }]
        });
    }

    $('#diskSmartPlaceholder').hide();
    $('#diskAuditResults').show();
    setDiskTabDot('dotSmartQuick', lastScanned);

}

function _smartMetric(label, value, color, icon = null, isText = true) {
    const display = isText ? value : (Number(value) || 0).toLocaleString();
    const iconHtml = icon ? `<i class="${icon}" style="font-size:.9rem;margin-right:6px;color:${color};"></i>` : '';
    return `<div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:6px;padding:12px;display:flex;flex-direction:column;justify-content:center;">
                <div style="font-size:.75rem;font-weight:800;text-transform:uppercase;color:#0f172a;margin-bottom:4px;letter-spacing:0.02em;">
                    ${iconHtml}${label}
                </div>
                <div style="font-size:1.1rem;font-weight:900;color:${color};">
                    ${display}
                </div>
            </div>`;
}

function _smartStatus(status) {
    if (!status) return '--';
    const s = String(status).trim().toUpperCase();
    if (['OK', 'GOOD', 'HEALTHY'].includes(s)) {
        return '<span style="font-weight:800;color:#22c55e;"><i class="fas fa-check-circle" style="margin-right:4px;"></i>' + status + '</span>';
    } else if (['WARNING', 'DEGRADED', 'PRED FAIL'].includes(s)) {
        return '<span style="font-weight:800;color:#f59e0b;"><i class="fas fa-exclamation-triangle" style="margin-right:4px;"></i>' + status + '</span>';
    } else if (['BAD', 'FAIL', 'FAILED', 'ERROR'].includes(s)) {
        return '<span style="font-weight:800;color:#ef4444;"><i class="fas fa-times-circle" style="margin-right:4px;"></i>' + status + '</span>';
    } else {
        const ok = ['OK', 'GOOD', 'HEALTHY'].includes(String(status).toUpperCase());
        return '<span style="font-weight:800;color:' + (ok ? '#22c55e' : '#ef4444') + ';">' + status + '</span>';
    }
}

function loadSmartDataDetails() {
    const serialParam = window.currentDiskSerial ? `&serial=${encodeURIComponent(window.currentDiskSerial)}` : '';
    $.get(`/ComputerSummary/GetSmartData?domain=${domaindata}${serialParam}`, function (smart) {
        if (!smart || smart.success === false) {
            handleSmartDataFallback();
            return;
        }
        renderSmartDataPanel(smart);
    }).fail(function () {
        handleSmartDataFallback();
    });
    loadSmartAttributesTable();
}

function handleSmartDataFallback() {
    if (window.lastQuickDiskData) {
        const d = window.lastQuickDiskData;
        const healthScore = Number(d.HealthScore ?? d.healthScore) || 0;
        const wear = Number(d.Wear ?? d.wear) || 0;
        const usedPct = Number(d.UsedPercent ?? d.usedPercent) || 0;
        const fallbackSmart = {
            smartSupported: true,
            smartEnabled: true,
            smartPassed: (d.HealthStatus || d.healthStatus || 'HEALTHY').toUpperCase() !== 'CRITICAL',
            healthPercentage: healthScore || 100,
            wearLevel: wear,
            lifeRemaining: Math.max(0, 100 - wear),
            percentageUsed: usedPct,
            reallocatedSectorCount: 0,
            pendingSectorCount: 0,
            uncorrectableSectorCount: 0,
            crcErrorCount: 0,
            temperature: Number(d.Temperature ?? d.temperature) || 0,
            powerOnHours: Number(d.PowerOnHours ?? d.powerOnHours) || 0,
            model: d.Model || d.model || 'Disk Telemetry',
            serialNumber: d.SerialNumber || d.serialNumber || 'N/A',
            computerName: domaindata
        };
        renderSmartDataPanel(fallbackSmart);
    } else {
        $('#diskSmartSummaryContainer').html(
            '<div class="disk-modern-card" style="padding:16px;margin-bottom:16px;text-align:center;color:var(--slate-400);"><i class="fas fa-info-circle" style="font-size:1.4rem;display:block;margin-bottom:8px;color:var(--slate-300);"></i>No standalone S.M.A.R.T. summary telemetry recorded for this drive.</div>'
        );
    }
}

// Raw SMART attribute rows, straight off SmartAttributeModel — same initTable +
// flexRender pattern as the benchmark table, instead of a hand-built .html() block.
function loadSmartAttributesTable() {
    const serialParam = window.currentDiskSerial ? `&serial=${encodeURIComponent(window.currentDiskSerial)}` : '';
    initTable('#diskSmartAttributesTable', `/ComputerSummary/GetHardDiskSmartAttributes?domain=${domaindata}${serialParam}`, [
        { data: null, render: (row) => flexRender(row, 'Name') },
        {
            data: null, render: (row) => {
                const val = flexRender(row, 'CurrentValue');
                const pct = Math.min(100, Math.max(0, (Number(val) || 0) / 2.55));
                return `<div style="display:flex;align-items:center;gap:8px;">
                        <div style="flex:1;height:6px;background:var(--slate-100);border-radius:3px;overflow:hidden;"><div style="height:100%;width:${pct}%;background:#0ea5e9;"></div></div>
                        <span style="min-width:24px;text-align:right;">${val}</span>
                    </div>`;
            }
        },
        { data: null, render: (row) => flexRender(row, 'WorstValue') },
        {
            data: null, render: (row) => {
                const val = flexRender(row, 'Threshold');
                const pct = Math.min(100, Math.max(0, (Number(val) || 0) / 2.55));
                return `<div style="display:flex;align-items:center;gap:8px;">
                        <div style="flex:1;height:6px;background:var(--slate-100);border-radius:3px;overflow:hidden;"><div style="height:100%;width:${pct}%;background:#f59e0b;"></div></div>
                        <span style="min-width:24px;text-align:right;">${val}</span>
                    </div>`;
            }
        },
        { data: null, render: (row) => flexRender(row, 'RawValue') },
        {
            data: null, render: (row) => _smartStatus(row.Status || row.status)
        }
    ]);
}

function renderSmartDataPanel(smart) {
    const g = (camel, pascal, fallback) => smart[camel] ?? smart[pascal] ?? fallback;

    const smartSupported = g('smartSupported', 'SmartSupported', false);
    const smartEnabled = g('smartEnabled', 'SmartEnabled', false);
    const smartPassed = g('smartPassed', 'SmartPassed', false);

    const _safeNum = (val) => {
        if (val === null || val === undefined) return 0;
        const parsed = parseFloat(String(val).replace(/[^\d.-]/g, ''));
        return isNaN(parsed) ? 0 : parsed;
    };

    const healthPct = _safeNum(g('healthPercentage', 'HealthPercentage', 0));
    const wearLevel = _safeNum(g('wearLevel', 'WearLevel', 0));
    const lifeRemaining = _safeNum(g('lifeRemaining', 'LifeRemaining', 0));
    const pctUsed = _safeNum(g('percentageUsed', 'PercentageUsed', 0));
    const reallocSectors = _safeNum(g('reallocatedSectorCount', 'ReallocatedSectorCount', 0));
    const pendingSectors = _safeNum(g('pendingSectorCount', 'PendingSectorCount', 0));
    const uncorrSectors = _safeNum(g('uncorrectableSectorCount', 'UncorrectableSectorCount', 0));
    const crcErrors = _safeNum(g('crcErrorCount', 'CRCErrorCount', 0));
    const temp = _safeNum(g('temperature', 'Temperature', 0));
    const minTemp = g('minimumTemperature', 'MinimumTemperature', null);
    const maxTemp = g('maximumTemperature', 'MaximumTemperature', null);
    const lifeMinTemp = g('lifetimeMinimumTemperature', 'LifetimeMinimumTemperature', null);
    const lifeMaxTemp = g('lifetimeMaximumTemperature', 'LifetimeMaximumTemperature', null);
    const powerOnHours = g('powerOnHours', 'PowerOnHours', 0);
    const powerCycles = g('powerCycles', 'PowerCycles', 0);
    const dataRead = g('totalDataRead', 'TotalDataRead', 0);
    const dataWritten = g('totalDataWritten', 'TotalDataWritten', 0);
    const readCmds = g('readCommands', 'ReadCommands', 0);
    const writeCmds = g('writeCommands', 'WriteCommands', 0);
    const rotationRate = g('rotationRate', 'RotationRate', 'N/A');
    const scanTime = g('scanTime', 'ScanTime', null);
    const model = g('model', 'Model', 'N/A');
    const serial = g('serialNumber', 'SerialNumber', 'N/A');
    const computerName = g('computerName', 'ComputerName', 'N/A');
    if (window.lastQuickDiskData) {
        const d = window.lastQuickDiskData;
        renderDiskRiskBanner(d, {
            wearVal: d.Wear ?? d.wear,
            tempVal: d.Temperature ?? d.temperature,
            predictFail: d.PredictFailure ?? d.predictFailure ?? false,
            readErr: Number(d.ReadErrorsTotal ?? d.readErrorsTotal ?? 0),
            writeErr: Number(d.WriteErrorsTotal ?? d.writeErrorsTotal ?? 0),
            reallocSectors, pendingSectors, uncorrSectors
        });
    }

    const staleBanner = deepStalenessBanner(scanTime, 'S.M.A.R.T. Full Detail (Deep Audit)');

    const summaryHtml = `
        ${staleBanner}
        <div class="disk-anim-in disk-modern-card" style="padding:20px;margin-bottom:20px;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);">
            <div style="display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:6px;margin-bottom:8px;border-bottom:1px solid #e2e8f0;padding-bottom:10px;">
                <div style="font-size:1rem;font-weight:800;color:#0f172a;">
                    <i class="fas fa-heartbeat" style="color:var(--cyan);margin-right:8px;"></i>S.M.A.R.T. Summary <span style="font-weight:600;color:#475569;">(Deep Audit)</span>
                </div>
                ${diskFreshnessBadge(scanTime, { label: 'Deep scan' })}
            </div>
            <div style="font-size:.85rem;font-weight:600;color:#334155;margin-bottom:16px;">${model} &middot; ${serial} &middot; ${computerName}</div>

            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:20px;margin-bottom:24px;">
                <!-- Health & Wear Charts -->
                <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;">
                    <div style="font-size:.85rem;font-weight:800;text-transform:uppercase;color:#0f172a;margin-bottom:12px;text-align:center;">Health & Wear</div>
                    <div style="display:flex;justify-content:space-around;align-items:center;margin-bottom:16px;">
                        <div style="width:110px;height:110px;position:relative;">
                            <canvas id="daHealthChart"></canvas>
                            <div style="position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;">
                                <span style="font-size:1.2rem;font-weight:900;color:#0f172a;">${healthPct}%</span>
                                <span style="font-size:.65rem;font-weight:800;color:#64748b;">HEALTH</span>
                            </div>
                        </div>
                        <div style="width:110px;height:110px;position:relative;">
                            <canvas id="daWearChart"></canvas>
                            <div style="position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;">
                                <span style="font-size:1.2rem;font-weight:900;color:#0f172a;">${wearLevel}%</span>
                                <span style="font-size:.65rem;font-weight:800;color:#64748b;">WEAR</span>
                            </div>
                        </div>
                    </div>
                    <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;">
                        ${_smartMetric('Health %', healthPct + '%', healthPct >= 80 ? '#22c55e' : (healthPct >= 50 ? '#f59e0b' : '#ef4444'))}
                        ${_smartMetric('Wear Level', wearLevel + '%', wearLevel < 50 ? '#22c55e' : (wearLevel < 80 ? '#f59e0b' : '#ef4444'))}
                        ${_smartMetric('Life Remaining', lifeRemaining + '%', lifeRemaining >= 50 ? '#22c55e' : (lifeRemaining >= 20 ? '#f59e0b' : '#ef4444'))}
                        ${_smartMetric('Percentage Used', pctUsed + '%', pctUsed < 50 ? '#22c55e' : (pctUsed < 80 ? '#f59e0b' : '#ef4444'))}
                    </div>
                </div>

                <!-- Sector Errors Chart -->
                <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;">
                    <div style="font-size:.85rem;font-weight:800;text-transform:uppercase;color:#0f172a;margin-bottom:12px;text-align:center;">Sector Errors</div>
                    <div style="height:120px;margin-bottom:16px;"><canvas id="daSectorErrorsChart"></canvas></div>
                    <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;">
                        ${_smartMetric('Reallocated Sectors', reallocSectors, reallocSectors > 0 ? '#ef4444' : '#22c55e', 'fas fa-exclamation-triangle')}
                        ${_smartMetric('Pending Sectors', pendingSectors, pendingSectors > 0 ? '#ef4444' : '#22c55e', 'fas fa-exclamation-circle')}
                        ${_smartMetric('Uncorrectable Sectors', uncorrSectors, uncorrSectors > 0 ? '#ef4444' : '#22c55e', 'fas fa-skull-crossbones')}
                        ${_smartMetric('CRC Errors', crcErrors, crcErrors > 0 ? '#ef4444' : '#22c55e', 'fas fa-wifi')}
                    </div>
                </div>
            </div>

            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:20px;margin-bottom:24px;">
                <!-- Temperature Gauge -->
                <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;">
                    <div style="font-size:.85rem;font-weight:800;text-transform:uppercase;color:#0f172a;margin-bottom:12px;text-align:center;">Temperature</div>
                    <div style="height:100px;position:relative;margin-bottom:24px;">
                        <canvas id="daTempGauge"></canvas>
                        <div style="position:absolute;bottom:0;width:100%;text-align:center;">
                            <span style="font-size:1.5rem;font-weight:900;color:#0f172a;">${temp}°C</span>
                        </div>
                    </div>
                    <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;">
                        ${_smartMetric('Current', temp + '°C', temp >= 55 ? '#ef4444' : (temp >= 45 ? '#f59e0b' : '#22c55e'), 'fas fa-thermometer-half', true)}
                        ${minTemp !== null ? _smartMetric('Min (Session)', minTemp + '°C', '#0f172a', 'fas fa-thermometer-empty', true) : ''}
                        ${maxTemp !== null ? _smartMetric('Max (Session)', maxTemp + '°C', '#0f172a', 'fas fa-thermometer-full', true) : ''}
                        ${lifeMaxTemp !== null ? _smartMetric('Max (Lifetime)', lifeMaxTemp + '°C', '#0f172a', 'fas fa-thermometer-full', true) : ''}
                    </div>
                </div>

                <!-- Data Read vs Written -->
                <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;">
                    <div style="font-size:.85rem;font-weight:800;text-transform:uppercase;color:#0f172a;margin-bottom:12px;text-align:center;">Data I/O Lifetime</div>
                    <div style="height:120px;margin-bottom:16px;"><canvas id="daDataIOChart"></canvas></div>
                    <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;">
                        ${_smartMetric('Total Data Read', Number(dataRead).toLocaleString() + ' GB', '#0f172a', 'fas fa-download', true)}
                        ${_smartMetric('Total Data Written', Number(dataWritten).toLocaleString() + ' GB', '#0f172a', 'fas fa-upload', true)}
                        ${_smartMetric('Scan Time', scanTime ? new Date(scanTime).toLocaleString() : 'N/A', '#0f172a', 'fas fa-calendar', true)}
                    </div>
                </div>
            </div>

            <div style="font-size:.85rem;font-weight:800;text-transform:uppercase;color:#0f172a;margin-bottom:12px;border-bottom:2px solid #e2e8f0;padding-bottom:6px;">Status Metrics</div>
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px;margin-bottom:24px;">
                ${_smartMetric('SMART Supported', smartSupported ? 'Yes' : 'No', smartSupported ? '#22c55e' : '#ef4444', 'fas fa-check-circle', true)}
                ${_smartMetric('SMART Enabled', smartEnabled ? 'Yes' : 'No', smartEnabled ? '#22c55e' : '#ef4444', 'fas fa-toggle-on', true)}
                ${_smartMetric('SMART Passed', smartPassed ? 'Yes' : 'No', smartPassed ? '#22c55e' : '#ef4444', 'fas fa-shield-alt', true)}
                ${_smartMetric('Rotation Rate', rotationRate, '#0f172a', 'fas fa-sync', true)}
            </div>

            <div style="font-size:.85rem;font-weight:800;text-transform:uppercase;color:#0f172a;margin-bottom:12px;border-bottom:2px solid #e2e8f0;padding-bottom:6px;">Usage & Endurance</div>
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px;">
                ${_smartMetric('Power-On Hours', Number(powerOnHours).toLocaleString(), '#0f172a', 'fas fa-clock', true)}
                ${_smartMetric('Power Cycles', Number(powerCycles).toLocaleString(), '#0f172a', 'fas fa-power-off', true)}
                ${_smartMetric('Read Commands', Number(readCmds).toLocaleString(), '#0f172a', 'fas fa-arrow-down', true)}
                ${_smartMetric('Write Commands', Number(writeCmds).toLocaleString(), '#0f172a', 'fas fa-arrow-up', true)}
            </div>
        </div>`;

    $('#diskSmartSummaryContainer').html(summaryHtml);
    setDiskTabDot('dotSmartFull', scanTime);

    // Initialize the new Deep Audit charts
    setTimeout(() => {
        const createDoughnut = (id, value, color) => {
            const ctx = document.getElementById(id);
            if (!ctx) return;
            new Chart(ctx, {
                type: 'doughnut',
                data: { datasets: [{ data: [value, Math.max(0, 100 - value)], backgroundColor: [color, '#e2e8f0'], borderWidth: 0, cutout: '80%', borderRadius: [4, 0] }] },
                options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false }, tooltip: { enabled: false } }, animation: { animateScale: true } }
            });
        };
        createDoughnut('daHealthChart', healthPct, healthPct >= 80 ? '#22c55e' : (healthPct >= 50 ? '#f59e0b' : '#ef4444'));
        createDoughnut('daWearChart', wearLevel, wearLevel < 50 ? '#22c55e' : (wearLevel < 80 ? '#f59e0b' : '#ef4444'));

        const sectorCtx = document.getElementById('daSectorErrorsChart');
        if (sectorCtx) {
            new Chart(sectorCtx, {
                type: 'bar',
                data: {
                    labels: ['Reallocated', 'Pending', 'Uncorrectable', 'CRC Errors'],
                    datasets: [{
                        data: [reallocSectors, pendingSectors, uncorrSectors, crcErrors],
                        backgroundColor: (ctx) => ctx.raw > 0 ? '#ef4444' : '#22c55e',
                        borderRadius: 4
                    }]
                },
                options: {
                    indexAxis: 'y', responsive: true, maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: { x: { beginAtZero: true, grid: { color: '#f1f5f9' }, ticks: { font: { weight: 'bold' } } }, y: { grid: { display: false }, ticks: { font: { weight: 'bold', color: '#334155' } } } }
                }
            });
        }

        const tempCtx = document.getElementById('daTempGauge');
        if (tempCtx) {
            new Chart(tempCtx, {
                type: 'doughnut',
                data: { datasets: [{ data: [temp, Math.max(0, 100 - temp)], backgroundColor: [temp >= 55 ? '#ef4444' : (temp >= 45 ? '#f59e0b' : '#3b82f6'), '#e2e8f0'], borderWidth: 0, cutout: '75%', borderRadius: 4, circumference: 180, rotation: 270 }] },
                options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false }, tooltip: { enabled: false } }, animation: { animateScale: true } }
            });
        }

        const ioCtx = document.getElementById('daDataIOChart');
        if (ioCtx) {
            new Chart(ioCtx, {
                type: 'bar',
                data: {
                    labels: ['Data Read (GB)', 'Data Written (GB)'],
                    datasets: [{
                        data: [dataRead, dataWritten],
                        backgroundColor: ['#8b5cf6', '#0ea5e9'],
                        borderRadius: 4
                    }]
                },
                options: {
                    indexAxis: 'y', responsive: true, maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: { x: { beginAtZero: true, grid: { color: '#f1f5f9' }, ticks: { font: { weight: 'bold' } } }, y: { grid: { display: false }, ticks: { font: { weight: 'bold', color: '#334155' } } } }
                }
            });
        }
    }, 100);
}

function loadDeepDiskReportDetails() {
    const serialParam = window.currentDiskSerial ? `&serial=${encodeURIComponent(window.currentDiskSerial)}` : '';
    $.get(`/ComputerSummary/GetDeepDiskReport?domain=${domaindata}${serialParam}`, function (report) {
        if (!report || report.success === false) return;
        renderDeepDiskReportPanel(report);
    });
    loadBenchmarkTable();
}

// Benchmark results, straight off the DiskTestResult model — same DataTable
// pattern used everywhere else on this page (initTable + flexRender), so it
// no longer depends on the hand-built cards/chart matching field names correctly.
function loadBenchmarkTable() {
    const serialParam = window.currentDiskSerial ? `&serial=${encodeURIComponent(window.currentDiskSerial)}` : '';
    initTable('#diskBenchmarkTable', `/ComputerSummary/GetHardDiskBenchmark?domain=${domaindata}${serialParam}`, [
        { data: null, render: (row) => flexRender(row, 'TestName') },
        {
            data: null, render: (row) => {
                const success = row.Success ?? row.success;
                const status = row.Status || row.status;
                const ok = success !== false;
                return '<span style="font-weight:700;color:' + (ok ? '#22c55e' : '#ef4444') + ';"><i class="fas ' +
                    (ok ? 'fa-check-circle' : 'fa-times-circle') + '"></i> ' + (status || (ok ? 'Passed' : 'Failed')) + '</span>';
            }
        },
        {
            data: null, render: (row) => {
                const speed = row.SpeedMBps ?? row.speedMBps;
                return (speed !== undefined && speed !== null) ? Number(speed).toFixed(1) : 'N/A';
            }
        },
        {
            data: null, render: (row) => {
                const dur = row.DurationSeconds ?? row.durationSeconds;
                return (dur !== undefined && dur !== null) ? Number(dur).toFixed(1) : 'N/A';
            }
        },
        {
            data: null, render: (row) => {
                const processed = Number(row.ProcessedBytes ?? row.processedBytes ?? 0);
                const total = Number(row.TotalBytes ?? row.totalBytes ?? 0);
                const bytes = processed || total;
                return bytes ? (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB' : 'N/A';
            }
        },
        { data: null, render: (row) => flexRender(row, 'ErrorCount') }
    ]);
}

function renderDeepDiskReportPanel(report) {
    if (!report) return;

    const dst = report.dstResult || report.DstResult;

    const overallStatus = report.overallStatus || report.OverallStatus || 'Unknown';
    const errMsg = report.errorMessage || report.ErrorMessage;
    const statusColor = overallStatus.toUpperCase() === 'PASSED' || overallStatus.toUpperCase() === 'HEALTHY' ? '#22c55e' : (overallStatus.toUpperCase() === 'WARNING' ? '#f59e0b' : '#ef4444');

    const deepDateForStaleness = report.endTime || report.EndTime || report.startTime || report.StartTime;
    const staleBanner = deepStalenessBanner(deepDateForStaleness, 'Deep Scan (SMART / Benchmark / DST)');

    const summaryHeaderHtml = `
        ${staleBanner}
        <div class="disk-anim-in disk-modern-card" style="padding:16px;margin-bottom:16px;">
            <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:10px;flex-wrap:wrap;gap:6px;">
                <span style="font-size:.82rem;font-weight:700;color:var(--slate-800);"><i class="fas fa-clipboard-check" style="color:var(--primary);margin-right:6px;"></i>Deep Scan Summary</span>
                <div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">
                    ${diskFreshnessBadge(report.endTime || report.EndTime || report.startTime || report.StartTime, { label: 'Deep scan' })}
                    <span style="font-size:.72rem;font-weight:800;padding:3px 10px;border-radius:999px;background:${statusColor}22;color:${statusColor};">${overallStatus}</span>
                </div>
            </div>
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(130px,1fr));gap:8px;">
                ${_smartMetric('Disk Index', report.diskIndex ?? report.DiskIndex ?? 'N/A', '#64748b', 'fas fa-hashtag', true)}
                ${_smartMetric('Model', report.model || report.Model || 'N/A', '#64748b', 'fas fa-hdd', true)}
                ${_smartMetric('Drive Letter', report.driveLetter || report.DriveLetter || 'N/A', '#64748b', 'fas fa-folder', true)}
                ${_smartMetric('Media Type', report.mediaType || report.MediaType || 'N/A', '#64748b', 'fas fa-compact-disc', true)}
                ${_smartMetric('SSD', (report.isSSD ?? report.IsSSD) ? 'Yes' : 'No', (report.isSSD ?? report.IsSSD) ? '#22c55e' : '#64748b', 'fas fa-bolt', true)}
                ${_smartMetric('NVMe', (report.isNVMe ?? report.IsNVMe) ? 'Yes' : 'No', (report.isNVMe ?? report.IsNVMe) ? '#8b5cf6' : '#64748b', 'fas fa-microchip', true)}
                ${_smartMetric('Duration', (report.durationMinutes ?? report.DurationMinutes ?? 0).toFixed(1) + ' min', '#64748b', 'fas fa-stopwatch', true)}
                ${_smartMetric('Started', (report.startTime || report.StartTime) ? new Date(report.startTime || report.StartTime).toLocaleString() : 'N/A', '#64748b', 'fas fa-play', true)}
                ${_smartMetric('Ended', (report.endTime || report.EndTime) ? new Date(report.endTime || report.EndTime).toLocaleString() : 'N/A', '#64748b', 'fas fa-flag-checkered', true)}
            </div>
            ${errMsg && errMsg !== 'No error' ? `<div style="margin-top:10px;font-size:.74rem;color:#ef4444;background:#fef2f2;padding:8px 12px;border-radius:6px;">${errMsg}</div>` : ''}
        </div>`;

    let dstHtml = '';
    if (dst) {
        const dstStart = dst.startTime || dst.StartTime;
        const dstEnd = dst.endTime || dst.EndTime;
        const dstErrMsg = dst.errorMessage || dst.ErrorMessage;

        const endDate = dstEnd ? new Date(dstEnd) : null;
        const startDate = dstStart ? new Date(dstStart) : null;
        const hasValidEndDate = endDate && endDate.getFullYear() > 1 && (!startDate || endDate >= startDate);

        const rawStatus = dst.status || dst.Status || '';
        const statusUpper = rawStatus.toUpperCase();
        const hasPassedStatus = ['PASSED', 'COMPLETED', 'PASS', 'OK', 'DONE', 'FAILED', 'FAIL'].includes(statusUpper);
        const allSubTestsDone = [dst.sequentialReadSuccess ?? dst.SequentialReadSuccess, dst.sequentialWriteSuccess ?? dst.SequentialWriteSuccess, dst.randomReadSuccess ?? dst.RandomReadSuccess, dst.randomWriteSuccess ?? dst.RandomWriteSuccess, dst.surfaceReadSuccess ?? dst.SurfaceReadSuccess].every(v => v !== undefined && v !== null);

        const isUnfinished = !hasValidEndDate && !hasPassedStatus && !allSubTestsDone;

        const displayStatus = isUnfinished ? 'In Progress' : (rawStatus || 'Passed');
        let stColor = isUnfinished ? '#0ea5e9' : (displayStatus.toUpperCase() === 'PASSED' || displayStatus.toUpperCase() === 'COMPLETED' ? '#22c55e' : '#ef4444');

        const durationDisplay = isUnfinished ? 'In progress…' : (Number(dst.durationMinutes || dst.DurationMinutes || 0).toFixed(1) + ' mins');
        const endedDisplay = isUnfinished ? 'Still running' : (endDate ? endDate.toLocaleString() : 'N/A');

        const subTests = [
            { label: 'Sequential Read', ok: dst.sequentialReadSuccess ?? dst.SequentialReadSuccess },
            { label: 'Sequential Write', ok: dst.sequentialWriteSuccess ?? dst.SequentialWriteSuccess },
            { label: 'Random Read', ok: dst.randomReadSuccess ?? dst.RandomReadSuccess },
            { label: 'Random Write', ok: dst.randomWriteSuccess ?? dst.RandomWriteSuccess },
            { label: 'Surface Read', ok: dst.surfaceReadSuccess ?? dst.SurfaceReadSuccess }
        ];
        const subTestHtml = isUnfinished
            ? '<div style="font-size:.76rem;color:var(--slate-400);grid-column:1/-1;">Sub-test results will appear once the self-test finishes.</div>'
            : subTests.map(t => `
            <div style="display:flex;align-items:center;justify-content:space-between;padding:6px 10px;background:var(--slate-50);border-radius:6px;">
                <span style="font-size:.74rem;color:#0f172a;">${t.label}</span>
                <span style="font-size:.82rem;font-weight:700;color:${t.ok ? '#22c55e' : '#ef4444'};"><i class="fas ${t.ok ? 'fa-check-circle' : 'fa-times-circle'}"></i> ${t.ok ? 'Pass' : 'Fail'}</span>
            </div>`).join('');

        dstHtml = `
            <div class="disk-anim-in disk-modern-card" style="padding:16px;position:relative;">
                <div style="position:absolute;top:16px;right:16px;opacity:0.15;pointer-events:none;transform:rotate(15deg);">
                    <i class="fas ${displayStatus.toUpperCase() === 'PASSED' ? 'fa-check-double' : 'fa-exclamation-triangle'}" style="font-size:6rem;color:${stColor};"></i>
                </div>
                <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:10px;position:relative;z-index:1;">
                    <span style="font-size:.82rem;font-weight:700;color:var(--slate-800);"><i class="fas fa-microscope" style="color:#8b5cf6;margin-right:6px;"></i>Drive Self-Test (DST) Report</span>
                    <span style="font-size:.85rem;font-weight:800;color:${stColor};border:2px solid ${stColor};padding:4px 12px;border-radius:4px;text-transform:uppercase;letter-spacing:1px;box-shadow:0 0 10px ${stColor}33;">${isUnfinished ? '<i class="fas fa-circle-notch fa-spin"></i> ' : ''}${displayStatus}</span>
                </div>
                ${isUnfinished ? `<div style="font-size:.74rem;color:#0369a1;background:#f0f9ff;padding:8px 12px;border-radius:6px;margin-bottom:12px;">This drive self-test hasn't finished on the device yet. Results below are from the last completed test.</div>` : ''}
                <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:8px;font-size:.76rem;margin-bottom:12px;position:relative;z-index:1;">
                    <div style="background:rgba(248,250,252,0.8);padding:8px 12px;border-radius:6px;border:1px solid var(--slate-100);">Test Type: <strong>${dst.testType || dst.TestType || 'Full DST'}</strong></div>
                    <div style="background:rgba(248,250,252,0.8);padding:8px 12px;border-radius:6px;border:1px solid var(--slate-100);">Duration: <strong>${durationDisplay}</strong></div>
                    <div style="background:rgba(248,250,252,0.8);padding:8px 12px;border-radius:6px;border:1px solid var(--slate-100);">Total Errors: <strong style="color:${(dst.totalErrors || dst.TotalErrors) > 0 ? '#ef4444' : '#22c55e'};">${dst.totalErrors ?? dst.TotalErrors ?? 0}</strong></div>
                    <div style="background:rgba(248,250,252,0.8);padding:8px 12px;border-radius:6px;border:1px solid var(--slate-100);">Started: <strong>${dstStart ? new Date(dstStart).toLocaleString() : 'N/A'}</strong></div>
                    <div style="background:rgba(248,250,252,0.8);padding:8px 12px;border-radius:6px;border:1px solid var(--slate-100);">Ended: <strong>${endedDisplay}</strong></div>
                </div>
                <div style="font-size:.7rem;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:#0f172a;margin-bottom:8px;position:relative;z-index:1;">Sub-Test Breakdown</div>
                <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:8px;margin-bottom:${dstErrMsg && dstErrMsg !== 'No error' ? '10px' : '0'};position:relative;z-index:1;">
                    ${subTestHtml}
                </div>
                ${dstErrMsg && dstErrMsg !== 'No error' ? `<div style="font-size:.74rem;color:#ef4444;background:#fef2f2;padding:8px 12px;border-radius:6px;position:relative;z-index:1;">${dstErrMsg}</div>` : ''}
            </div>`;
    }

    const nvme = report.nvmeResult || report.NvmeResult;
    const nvmeHtml = (report.isNVMe || report.IsNVMe) ? _nvmeCard(nvme) : '';

    $('#diskDstContainer').html(summaryHeaderHtml + nvmeHtml + dstHtml);
    $('#diskDstPlaceholder').hide();
    $('#diskDstResults').show();
    setDiskTabDot('dotBenchmark', report.endTime || report.EndTime || report.startTime || report.StartTime);

    // Render benchmark results directly from report object if present
    const testDefs = [
        { name: 'Sequential Read', obj: report.sequentialRead || report.SequentialRead },
        { name: 'Sequential Write', obj: report.sequentialWrite || report.SequentialWrite },
        { name: 'Random Read', obj: report.randomRead || report.RandomRead },
        { name: 'Random Write', obj: report.randomWrite || report.RandomWrite },
        { name: 'Surface Read', obj: report.surfaceRead || report.SurfaceRead }
    ];

    const benchmarkData = testDefs.map(t => {
        if (!t.obj) return null;
        return {
            TestName: t.obj.testName || t.obj.TestName || t.name,
            Success: t.obj.success ?? t.obj.Success ?? true,
            Status: t.obj.status || t.obj.Status || 'Passed',
            SpeedMBps: t.obj.speedMBps ?? t.obj.SpeedMBps ?? 0,
            DurationSeconds: t.obj.durationSeconds ?? t.obj.DurationSeconds ?? 0,
            ProcessedBytes: t.obj.processedBytes ?? t.obj.ProcessedBytes ?? t.obj.totalBytes ?? t.obj.TotalBytes ?? 0,
            TotalBytes: t.obj.totalBytes ?? t.obj.TotalBytes ?? 0,
            ErrorCount: t.obj.errorCount ?? t.obj.ErrorCount ?? 0
        };
    }).filter(Boolean);

    if (benchmarkData.length > 0) {
        // Draw Radar Chart
        const radarCanvas = document.getElementById('diskBenchmarkRadarCanvas');
        if (radarCanvas) {
            radarCanvas.style.display = 'block';
            if (window.diskBenchmarkRadarInstance) window.diskBenchmarkRadarInstance.destroy();

            const labels = benchmarkData.map(d => d.TestName.replace('Sequential', 'Seq.').replace('Random', 'Rand.'));
            const speeds = benchmarkData.map(d => d.SpeedMBps || 0);

            // Calculate a "Performance Score" based on max speeds (Assuming max ~3000MB/s for a good NVMe, though scales dynamically)
            const maxSpeed = Math.max(...speeds, 1);
            const perfScore = Math.min(100, Math.round((maxSpeed / 3000) * 100));
            const perfColor = perfScore > 80 ? '#22c55e' : (perfScore > 50 ? '#f59e0b' : '#ef4444');

            window.diskBenchmarkRadarInstance = new Chart(radarCanvas.getContext('2d'), {
                type: 'radar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'Speed (MB/s)',
                        data: speeds,
                        backgroundColor: 'rgba(139, 92, 246, 0.2)',
                        borderColor: '#8b5cf6',
                        pointBackgroundColor: '#fff',
                        pointBorderColor: '#8b5cf6',
                        pointHoverBackgroundColor: '#8b5cf6',
                        pointHoverBorderColor: '#fff',
                        borderWidth: 2,
                        fill: true
                    }]
                },
                options: {
                    responsive: false,
                    maintainAspectRatio: false,
                    scales: {
                        r: {
                            angleLines: { color: 'rgba(226, 232, 240, 0.5)' },
                            grid: { color: 'rgba(226, 232, 240, 0.5)' },
                            pointLabels: { font: { size: 10, family: "'Inter', sans-serif" }, color: '#64748b' },
                            ticks: { display: false }
                        }
                    },
                    plugins: { legend: { display: false }, tooltip: { enabled: true } }
                },
                plugins: [{
                    id: 'textCenterRadar',
                    beforeDraw: function (chart) {
                        var width = chart.width, height = chart.height, ctx = chart.ctx;
                        ctx.restore();
                        var fontSize = (height / 120).toFixed(2);
                        ctx.font = "800 " + fontSize + "em 'Inter', sans-serif";
                        ctx.textBaseline = "middle";
                        ctx.fillStyle = perfColor;
                        var text = perfScore,
                            textX = Math.round((width - ctx.measureText(text).width) / 2),
                            textY = height / 2;
                        ctx.fillText(text, textX, textY);

                        ctx.font = "700 " + (fontSize * 0.35).toFixed(2) + "em 'Inter', sans-serif";
                        ctx.fillStyle = '#64748b';
                        var subText = "SCORE",
                            subX = Math.round((width - ctx.measureText(subText).width) / 2),
                            subY = textY + (height * 0.12);
                        ctx.fillText(subText, subX, subY);
                        ctx.save();
                    }
                }]
            });
        }

        initLocalTable('#diskBenchmarkTable', benchmarkData, [
            { data: null, render: (row) => flexRender(row, 'TestName') },
            {
                data: null, render: (row) => {
                    const success = row.Success ?? row.success;
                    const status = row.Status || row.status;
                    const ok = success !== false;
                    return '<span style="font-weight:700;color:' + (ok ? '#22c55e' : '#ef4444') + ';"><i class="fas ' +
                        (ok ? 'fa-check-circle' : 'fa-times-circle') + '"></i> ' + (status || (ok ? 'Passed' : 'Failed')) + '</span>';
                }
            },
            {
                data: null, render: (row) => {
                    const speed = row.SpeedMBps ?? row.speedMBps;
                    return (speed !== undefined && speed !== null) ? Number(speed).toFixed(1) : 'N/A';
                }
            },
            {
                data: null, render: (row) => {
                    const dur = row.DurationSeconds ?? row.durationSeconds;
                    return (dur !== undefined && dur !== null) ? Number(dur).toFixed(1) : 'N/A';
                }
            },
            {
                data: null, render: (row) => {
                    const processed = Number(row.ProcessedBytes ?? row.processedBytes ?? 0);
                    const total = Number(row.TotalBytes ?? row.totalBytes ?? 0);
                    const bytes = processed || total;
                    return bytes ? (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB' : 'N/A';
                }
            },
            { data: null, render: (row) => flexRender(row, 'ErrorCount') }
        ]);
    }
}

function _nvmeCard(nvme) {
    if (!nvme) return '';
    const g = (camel, pascal, fallback) => nvme[camel] ?? nvme[pascal] ?? fallback;

    const temp = g('temperature', 'Temperature', null);
    const wear = g('percentageUsed', 'PercentageUsed', 0);
    const spare = g('availableSpare', 'AvailableSpare', 0);
    const spareThresh = g('availableSpareThreshold', 'AvailableSpareThreshold', 10);
    const critWarn = g('criticalWarning', 'CriticalWarning', null);
    const mediaErr = Number(g('mediaErrors', 'MediaErrors', 0));
    const errorLogEntries = Number(g('errorLogEntries', 'ErrorLogEntries', 0));
    const unsafeShutdowns = Number(g('unsafeShutdowns', 'UnsafeShutdowns', 0));
    const dataRead = Number(g('dataUnitsRead', 'DataUnitsRead', 0));
    const dataWritten = Number(g('dataUnitsWritten', 'DataUnitsWritten', 0));
    const hostReadCmds = Number(g('hostReadCommands', 'HostReadCommands', 0));
    const hostWriteCmds = Number(g('hostWriteCommands', 'HostWriteCommands', 0));
    const firmware = g('firmwareRevision', 'FirmwareRevision', 'N/A');
    const busType = g('busType', 'BusType', 'N/A');
    const pnpId = g('pnpDeviceId', 'PnpDeviceId', null);
    const isHealthy = (!critWarn || critWarn.toString().toUpperCase() === 'NONE' || critWarn === '0') && mediaErr === 0;

    return `
        <div class="disk-anim-in disk-modern-card" style="padding:16px;margin-bottom:16px;">
            <div style="font-size:.82rem;font-weight:700;color:var(--slate-800);margin-bottom:4px;display:flex;justify-content:space-between;align-items:center;">
                <span><i class="fas fa-microchip" style="color:#8b5cf6;margin-right:6px;"></i>NVMe Health Report</span>
                <span style="font-size:.72rem;font-weight:800;color:${isHealthy ? '#22c55e' : '#ef4444'};">${isHealthy ? 'Healthy' : 'Attention Needed'}</span>
            </div>
            <div style="font-size:.7rem;color:var(--slate-400);margin-bottom:10px;">Firmware ${firmware} &middot; ${busType}${pnpId ? ' &middot; ' + pnpId : ''}</div>

            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:10px;margin-bottom:12px;">
                ${_smartMetric('Temperature', (temp ?? 'N/A') + '°C', temp >= 70 ? '#ef4444' : (temp >= 55 ? '#f59e0b' : '#22c55e'), 'fas fa-thermometer-half', true)}
                ${_smartMetric('Percentage Used', wear + '%', wear >= 80 ? '#ef4444' : (wear >= 50 ? '#f59e0b' : '#22c55e'), 'fas fa-battery-half', true)}
                ${_smartMetric('Available Spare', spare + '%', spare <= spareThresh ? '#ef4444' : '#22c55e', 'fas fa-life-ring', true)}
                ${_smartMetric('Media Errors', mediaErr, mediaErr > 0 ? '#ef4444' : '#22c55e', 'fas fa-exclamation-triangle')}
                ${_smartMetric('Power On Hours', Number(g('powerOnHours', 'PowerOnHours', 0)).toLocaleString(), '#64748b', 'fas fa-clock', true)}
                ${_smartMetric('Critical Warning', critWarn || 'None', isHealthy ? '#22c55e' : '#ef4444', 'fas fa-flag', true)}
            </div>

            <div style="font-size:.7rem;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:var(--slate-500);margin-bottom:8px;">Endurance &amp; Reliability Counters</div>
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:10px;">
                ${_smartMetric('Power Cycles', Number(g('powerCycles', 'PowerCycles', 0)).toLocaleString(), '#64748b', 'fas fa-power-off', true)}
                ${_smartMetric('Unsafe Shutdowns', unsafeShutdowns.toLocaleString(), unsafeShutdowns > 0 ? '#f59e0b' : '#22c55e', 'fas fa-plug')}
                ${_smartMetric('Error Log Entries', errorLogEntries.toLocaleString(), errorLogEntries > 0 ? '#ef4444' : '#22c55e', 'fas fa-file-alt')}
                ${_smartMetric('Data Units Read', dataRead.toLocaleString(), '#64748b', 'fas fa-download', true)}
                ${_smartMetric('Data Units Written', dataWritten.toLocaleString(), '#64748b', 'fas fa-upload', true)}
                ${_smartMetric('Host Read Cmds', hostReadCmds.toLocaleString(), '#64748b', 'fas fa-arrow-down', true)}
                ${_smartMetric('Host Write Cmds', hostWriteCmds.toLocaleString(), '#64748b', 'fas fa-arrow-up', true)}
            </div>
        </div>`;
}
function loadHardDiskHistoryChart() {
    $.get(`/ComputerSummary/GetHardDiskHistory?domain=${domaindata}&take=20`, function (history) {
        if (!history || !Array.isArray(history) || history.length === 0) return;
        const chronological = history.slice().reverse(); // oldest -> newest
        renderHardDiskTrendChart(chronological);
        renderStorageForecast(chronological);
    });
}

function renderHardDiskTrendChart(history) {
    const canvas = document.getElementById('diskTrendChartCanvas');
    if (!canvas) return;
    if (window.diskTrendChartInstance) window.diskTrendChartInstance.destroy();

    const labels = history.map(h => {
        const dt = new Date(h.dateTime || h.DateTime);
        return isNaN(dt) ? '' : dt.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }) + ' ' + dt.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
    });

    window.diskTrendChartInstance = new Chart(canvas.getContext('2d'), {
        type: 'line',
        data: {
            labels,
            datasets: [
                {
                    label: 'Health Score (%)',
                    data: history.map(h => h.healthScore ?? h.HealthScore ?? null),
                    borderColor: '#10b981', backgroundColor: 'rgba(16,185,129,.1)',
                    borderWidth: 2, fill: true, tension: 0.3, yAxisID: 'y'
                },
                {
                    label: 'Used (%)',
                    data: history.map(h => h.usedPercent ?? h.UsedPercent ?? null),
                    borderColor: '#0ea5e9', backgroundColor: 'rgba(14,165,233,.08)',
                    borderWidth: 2, fill: true, tension: 0.3, yAxisID: 'y'
                },
                {
                    label: 'Temperature (°C)',
                    data: history.map(h => h.temperature ?? h.Temperature ?? null),
                    borderColor: '#f59e0b', borderWidth: 2, borderDash: [4, 4],
                    fill: false, tension: 0.3, yAxisID: 'y1', pointRadius: 2
                }
            ]
        },
        options: {
            responsive: true, maintainAspectRatio: false,
            plugins: { legend: { position: 'bottom', labels: { font: { size: 10 } } } },
            scales: {
                x: { ticks: { font: { size: 9 }, maxRotation: 45 } },
                y: { position: 'left', min: 0, max: 100, ticks: { font: { size: 10 } }, title: { display: true, text: '%', font: { size: 9 } } },
                y1: { position: 'right', grid: { drawOnChartArea: false }, ticks: { font: { size: 10 } }, title: { display: true, text: '°C', font: { size: 9 } } }
            }
        }
    });
}

// Simple linear regression of UsedSpaceGB over time across recent snapshots,
// projected forward to estimate when the drive will run out of free space.
// Needs at least 3 data points with actual variation to produce a meaningful estimate.
function renderStorageForecast(history) {
    const el = document.getElementById('diskForecastContainer');
    if (!el) return;

    const points = history
        .map(h => ({
            t: new Date(h.dateTime || h.DateTime).getTime(),
            used: parseFloat(h.usedSpaceGB ?? h.UsedSpaceGB ?? 0),
            total: parseFloat(h.totalCapacity ?? h.TotalCapacity ?? 0)
        }))
        .filter(p => !isNaN(p.t) && !isNaN(p.used));

    if (points.length < 3) {
        el.innerHTML = `<div style="font-size:.76rem;color:var(--slate-400);padding:8px 0;">Not enough historical scans yet to forecast storage growth. Run a few more audits over time.</div>`;
        return;
    }

    const n = points.length;
    const meanT = points.reduce((s, p) => s + p.t, 0) / n;
    const meanU = points.reduce((s, p) => s + p.used, 0) / n;
    let num = 0, den = 0;
    points.forEach(p => { num += (p.t - meanT) * (p.used - meanU); den += (p.t - meanT) * (p.t - meanT); });
    const slopePerMs = den !== 0 ? num / den : 0;
    const slopePerDay = slopePerMs * 1000 * 60 * 60 * 24;

    const latest = points[points.length - 1];
    let forecastHtml = '';

    if (slopePerDay <= 0.001) {
        forecastHtml = `<div style="font-size:.78rem;color:#22c55e;font-weight:600;"><i class="fas fa-check-circle"></i> Storage usage is flat or shrinking - no fill-up forecast needed.</div>`;
    } else {
        const remainingGB = Math.max(0, latest.total - latest.used);
        const daysToFull = remainingGB / slopePerDay;
        const color = daysToFull < 14 ? '#ef4444' : (daysToFull < 45 ? '#f59e0b' : '#22c55e');
        forecastHtml = `
            <div style="display:flex;align-items:center;gap:14px;flex-wrap:wrap;">
                <div>
                    <div style="font-size:.66rem;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:var(--slate-400);">Projected Time to Full</div>
                    <div style="font-size:1.15rem;font-weight:800;color:${color};">${daysToFull >= 999 ? '999+' : daysToFull.toFixed(0)} days</div>
                </div>
                <div style="font-size:.74rem;color:var(--slate-500);">
                    Growing ~${slopePerDay.toFixed(2)} GB/day based on the last ${n} scans.
                </div>
            </div>`;
    }

    el.innerHTML = `
        <div style="background:#fff;border:1px solid var(--slate-200);border-radius:var(--radius-md);padding:14px 18px;box-shadow:var(--shadow-sm);">
            <div style="font-size:.8rem;font-weight:700;color:var(--slate-800);margin-bottom:8px;"><i class="fas fa-chart-line" style="color:var(--primary);margin-right:6px;"></i>Storage Growth Forecast</div>
            ${forecastHtml}
        </div>`;
}

function loadDiskInfoDetails() {
    $.get(`/ComputerSummary/GetDiskInfo?domain=${domaindata}`, function (infoList) {
        if (!infoList || !Array.isArray(infoList) || infoList.length === 0) return;
        renderDiskInfoPanel(infoList);
    });
}

function renderDiskInfoPanel(infoList) {
    const scanTimes = infoList
        .map(info => info.scanTime || info.ScanTime)
        .filter(Boolean)
        .map(t => new Date(t))
        .filter(d => !isNaN(d.getTime()));
    const latestScan = scanTimes.length ? new Date(Math.max.apply(null, scanTimes)) : null;
    const freshnessHtml = `<div style="margin-bottom:10px;">${diskFreshnessBadge(latestScan, { label: 'Deep scan' })}</div>`;
    const staleBanner = deepStalenessBanner(latestScan, 'Disk & Volume Inventory (Deep Scan)');

    const cards = infoList.map(info => {
        const g = (camel, pascal, fallback) => info[camel] ?? info[pascal] ?? fallback;
        const volumes = info.volumes || info.Volumes || [];
        const volRows = volumes.map(v => {
            const cap = v.capacityGB ?? v.CapacityGB ?? 0;
            const used = v.usedSpaceGB ?? v.UsedSpaceGB ?? 0;
            const pct = cap > 0 ? Math.min(100, (used / cap) * 100) : 0;
            const color = pct >= 90 ? '#ef4444' : (pct >= 75 ? '#f59e0b' : '#22c55e');
            const volName = v.volumeName || v.VolumeName;
            return `<div style="display:flex;align-items:center;gap:10px;padding:8px 0;border-bottom:1px solid var(--slate-100);">
                <span style="font-weight:700;font-size:.8rem;color:var(--slate-700);min-width:36px;">${v.driveLetter || v.DriveLetter || '--'}</span>
                <div style="flex:1;height:8px;border-radius:4px;background:var(--slate-100);overflow:hidden;">
                    <div style="height:100%;width:${pct.toFixed(1)}%;background:${color};"></div>
                </div>
                <span style="font-size:.72rem;color:var(--slate-500);white-space:nowrap;">${used.toFixed(1)} / ${cap.toFixed(1)} GB</span>
                <span style="font-size:.7rem;color:var(--slate-400);min-width:60px;text-align:right;">${v.fileSystem || v.FileSystem || 'N/A'}</span>
                ${volName ? `<span style="font-size:.68rem;color:var(--slate-400);min-width:80px;text-align:right;">${volName}</span>` : ''}
            </div>`;
        }).join('') || '<div style="font-size:.76rem;color:var(--slate-400);">No volumes found on this disk.</div>';

        const firmware = g('firmwareRevision', 'FirmwareRevision', 'N/A');
        const busType = g('busType', 'BusType', 'N/A');
        const driveType = g('driveType', 'DriveType', 'N/A');
        const friendlyOrPnp = g('friendlyName', 'FriendlyName', null) || g('pnpDeviceId', 'PnpDeviceId', null);
        const partitionStyle = g('partitionStyle', 'PartitionStyle', 'N/A');
        const isRemovable = g('isRemovable', 'IsRemovable', false);
        const physSector = g('physicalSectorSize', 'PhysicalSectorSize', null);
        const logSector = g('logicalSectorSize', 'LogicalSectorSize', null);
        const rotationRate = g('rotationRate', 'RotationRate', null);
        const formFactor = g('formFactor', 'FormFactor', null);
        const serial = g('serialNumber', 'SerialNumber', 'N/A');

        return `
        <div class="disk-anim-in disk-modern-card" style="padding:16px;margin-bottom:14px;">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:4px;flex-wrap:wrap;gap:4px;">
                <span style="font-size:.82rem;font-weight:700;color:var(--slate-800);"><i class="fas fa-hdd" style="color:var(--primary);margin-right:6px;"></i>${info.model || info.Model || ('Disk ' + (info.diskIndex ?? info.DiskIndex ?? ''))}</span>
                <span style="font-size:.72rem;color:var(--slate-400);">${(info.capacityGB ?? info.CapacityGB ?? 0).toFixed(0)} GB &middot; ${info.mediaType || info.MediaType || 'N/A'}</span>
            </div>
            <div style="font-size:.7rem;color:var(--slate-400);margin-bottom:10px;">S/N ${serial} &middot; Firmware ${firmware} &middot; ${busType} &middot; ${driveType}${friendlyOrPnp ? ' &middot; ' + friendlyOrPnp : ''}</div>
            <div style="display:flex;flex-wrap:wrap;gap:6px;margin-bottom:10px;">
                <span style="font-size:.66rem;padding:2px 8px;border-radius:999px;background:var(--slate-100);color:var(--slate-500);">Partition Style: ${partitionStyle}</span>
                ${isRemovable ? '<span style="font-size:.66rem;padding:2px 8px;border-radius:999px;background:#fef3c7;color:#b45309;">Removable</span>' : ''}
                ${formFactor ? `<span style="font-size:.66rem;padding:2px 8px;border-radius:999px;background:var(--slate-100);color:var(--slate-500);">${formFactor}</span>` : ''}
                ${rotationRate !== null && rotationRate !== undefined && rotationRate > 0 ? `<span style="font-size:.66rem;padding:2px 8px;border-radius:999px;background:var(--slate-100);color:var(--slate-500);">${rotationRate} RPM</span>` : ''}
                ${physSector ? `<span style="font-size:.66rem;padding:2px 8px;border-radius:999px;background:var(--slate-100);color:var(--slate-500);">Sector: ${physSector}B phys / ${logSector}B log</span>` : ''}
            </div>
            ${volRows}
        </div>`;
    }).join('');

    $('#diskInfoContainer').html(staleBanner + freshnessHtml + cards);
    $('#diskInfoPlaceholder').hide();
    $('#diskInfoResults').show();
    setDiskTabDot('dotVolumes', latestScan);
}

$(document).ready(function () {
    let batteryAuditTimeout;
    let batteryAuditPollInterval;

    $('#btnViewBatteryReport').on('click', function (e) {
        e.preventDefault();
        window.open(`/ComputerSummary/ViewBatteryReport?domain=${encodeURIComponent(actualDomainName)}`, '_blank');
    });

    $('#btnAuditBattery').on('click', function (e) {
        e.preventDefault();
        window.hasLiveAuditOccurred = true;
        $('#batteryAuditPlaceholder').hide();
        $('#batteryAuditGate').show();
        $('#batteryAuditResults').hide();
        $('#batteryAuditLoading').html('<i class="fas fa-circle-notch fa-spin" style="font-size: 1.5rem; color: var(--cyan); margin-bottom: 8px; display: block;"></i> Fetching live diagnostics from device, please wait...');
        $('#batteryAuditLoading').show();

        function triggerBatteryFallback(reasonMsg) {
            if (!$('#batteryAuditLoading').is(':visible')) return;
            $.get(`/ComputerSummary/Battery?domain=${actualDomainName}`, function (data) {
                renderBatteryAuditPanel(data);
                loadBatteryHistoryCharts();
                sysAlert(reasonMsg || 'Live fetch unavailable. Showing last known state.', 'warning');
            });
        }

        sysAlert('Battery audit requested from device. Waiting for data...', 'info');

        $.ajax({
            url: '/ComputerSummary/AuditBattery?domain=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            timeout: 85000,
            success: function (res) {
                if (res && res.success && res.data && res.data.metrics) {
                    renderBatteryAuditPanel(res.data.metrics);
                    loadBatteryHistoryCharts();
                    sysAlert('Battery health data received!', 'success');
                } else {
                    triggerBatteryFallback(res ? res.message : 'Failed to receive audit data, falling back...');
                }
            },
            error: function (xhr, status, error) {
                let msg = status === 'timeout' ? 'Live fetch timed out. Showing last known state.' : 'Connection error while requesting audit, falling back...';
                triggerBatteryFallback(msg);
            }
        });
    });

    $('#btnAuditMemory').on('click', function (e) {
        e.preventDefault();
        window.hasLiveAuditOccurred = true;
        let btn = $(this);
        let originalText = btn.html();
        btn.html('<i class="fas fa-circle-notch fa-spin"></i> Processing...');
        btn.prop('disabled', true);
        btn.css('opacity', '0.7');

        $('#memAuditPlaceholder').hide();
        $('#memAuditGate').hide();
        $('#memAuditLoading').show();

        function triggerMemoryFallback(reasonMsg) {
            $.get(`/ComputerSummary/MemorySummary?domain=${domaindata}`, function (data) {
                if (hasRealMemoryData(data)) {
                    renderMemoryPanel(data);
                    sysAlert(reasonMsg || 'Live fetch unavailable. Showing last known state.', 'warning');
                } else {
                    $('#memAuditLoading').hide();
                    $('#memAuditPlaceholder').show();
                    sysAlert(reasonMsg || 'No memory data available yet for this device.', 'error');
                }
            }).fail(function () {
                $('#memAuditLoading').hide();
                $('#memAuditPlaceholder').show();
                sysAlert(reasonMsg || 'Unable to reach the server. Please try again.', 'error');
            });
        }

        sysAlert('Memory audit requested. This can take a little while on slower devices — please wait...', 'info');

        $.ajax({
            url: '/ComputerSummary/AuditMemory?domain=' + encodeURIComponent(domaindata) + '&hostName=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            timeout: 0,
            success: function (res) {
                if (res && res.success && res.data && res.data.metrics) {
                    renderMemoryPanel(res.data.metrics);
                    sysAlert(res.message || 'Memory audit completed!', 'success');
                } else if (res && res.success) {
                    $.get(`/ComputerSummary/MemorySummary?domain=${domaindata}`, function (data) {
                        if (hasRealMemoryData(data)) renderMemoryPanel(data);
                    });
                    sysAlert(res.message || 'Memory audit completed!', 'success');
                } else {
                    triggerMemoryFallback(res ? res.message : 'Audit did not complete. Showing last known state.');
                }
            },
            error: function (xhr, status) {
                let msg = 'Connection error while requesting audit. Showing last known state.';
                triggerMemoryFallback(msg);
            },
            complete: function () {
                btn.html(originalText);
                btn.prop('disabled', false);
                btn.css('opacity', '1');
            }
        });
    });

    $('#btnAuditProcessor').on('click', function (e) {
        e.preventDefault();
        window.hasLiveAuditOccurred = true;
        let btn = $(this);
        let originalText = btn.html();
        btn.html('<i class="fas fa-circle-notch fa-spin"></i> Processing...');
        btn.prop('disabled', true);
        btn.css('opacity', '0.7');
        sysAlert('Processor audit requested. Waiting for device to respond...', 'info');

        $.ajax({
            url: '/ComputerSummary/AuditProcessor?domain=' + encodeURIComponent(domaindata) + '&hostName=' + encodeURIComponent(actualDomainName),
            type: 'POST',
            timeout: 90000,
            success: function (res) {
                if (res && res.success) {
                    sysAlert(res.message || 'Processor audit completed!', 'success');
                    loadProcessorDetails(true);
                } else {
                    sysAlert(res.message || 'Processor audit failed.', 'error');
                }
            },
            error: function (xhr, status) {
                let msg = status === 'timeout' ? 'Processor audit timed out. The device may still be processing.' : 'Connection error while requesting processor audit.';
                sysAlert(msg, 'error');
            },
            complete: function () {
                btn.html(originalText);
                btn.prop('disabled', false);
                btn.css('opacity', '1');
            }
        });
    });

    $('#btnAuditHardDiskQuick').on('click', function (e) {
        e.preventDefault();
        let btn = $(this);
        let originalText = btn.html();
        window.hasLiveAuditOccurred = true;
        btn.html('<i class="fas fa-circle-notch fa-spin"></i> Processing...');
        btn.prop('disabled', true);
        btn.css('opacity', '0.7');
        btn.addClass('disk-audit-running');
        sysAlert('Hard Disk quick audit requested. Waiting for device to respond...', 'info');

        $('#diskAuditPlaceholder').hide();
        $('#diskAuditGate').hide();
        $('#diskAuditLoading').show();

        $.ajax({
            url: '/ComputerSummary/AuditHardDisk?domain=' + encodeURIComponent(domaindata) + '&hostName=' + encodeURIComponent(actualDomainName) + '&auditType=quick',
            type: 'POST',
            timeout: 90000,
            success: function (res) {
                if (res && res.success) {
                    sysAlert(res.message || 'Hard Disk quick audit completed!', 'success');
                    $.get(`/ComputerSummary/HardDisk?domain=${domaindata}`, function (disks) {
                        if (disks && Array.isArray(disks) && disks.length > 0) {
                            renderHardDiskDashboard(disks);
                            loadHardDiskHistoryChart();
                            loadDiskInfoDetails();
                            // Quick Audit never touches SMART/Deep tables, so there's nothing new
                            // to show there — but the fresh quick timestamp changes whether the
                            // existing deep data now counts as "stale". Invalidate rather than
                            // eagerly re-fetch; it'll recompute next time Deep Audit is opened,
                            // and refresh immediately if that subtab happens to be the open one.
                            invalidateDeepAuditData();
                            if ($('.disk-subtab-btn.active').data('subtab') === 'deep') {
                                ensureDeepAuditDataLoaded(true);
                            }
                        } else {
                            $('#diskAuditLoading').hide();
                            $('#diskAuditPlaceholder').show();
                            sysAlert('No hard disk data available after audit.', 'error');
                        }
                    }).fail(function () {
                        $('#diskAuditLoading').hide();
                        $('#diskAuditPlaceholder').show();
                    });
                } else {
                    $('#diskAuditLoading').hide();
                    $('#diskAuditPlaceholder').show();
                    sysAlert(res.message || 'Hard Disk quick audit failed.', 'error');
                }
            },
            error: function (xhr, status) {
                $('#diskAuditLoading').hide();
                $('#diskAuditPlaceholder').show();
                let msg = status === 'timeout' ? 'Hard Disk audit timed out. The device may still be processing.' : 'Connection error while requesting hard disk audit.';
                sysAlert(msg, 'error');
            },
            complete: function () {
                btn.html(originalText);
                btn.prop('disabled', false);
                btn.css('opacity', '1');
                btn.removeClass('disk-audit-running');
            }
        });
    });

    function setDeepAuditPendingUI(message) {
        $('#diskDstResults').hide();
        $('#diskDstPlaceholder').show().html(
            '<i class="fas fa-circle-notch fa-spin" style="font-size:1.4rem;display:block;margin-bottom:8px;color:#8b5cf6;"></i>' + message
        );
        $('#diskSmartSummaryContainer').html(
            '<div style="padding:20px;background:var(--slate-50);border:1px dashed var(--slate-200);border-radius:var(--radius-md);text-align:center;color:var(--slate-400);font-size:.82rem;">' +
            '<i class="fas fa-circle-notch fa-spin" style="font-size:1.2rem;display:block;margin-bottom:8px;color:var(--cyan);"></i>' + message + '</div>'
        );
        $('#diskInfoResults').hide();
        $('#diskInfoPlaceholder').show().html(
            '<i class="fas fa-circle-notch fa-spin" style="font-size:1.4rem;display:block;margin-bottom:8px;color:var(--primary);"></i>' + message
        );
    }

    function resetDeepAuditButton(btn, originalText) {
        btn.html(originalText);
        btn.prop('disabled', false);
        btn.css('opacity', '1');
        btn.removeClass('disk-audit-running');
    }

    function refreshQuickDiskSection() {
        $.get(`/ComputerSummary/HardDisk?domain=${domaindata}`, function (disks) {
            if (disks && Array.isArray(disks) && disks.length > 0) {
                // openGate=true: this is the only place that turns real, freshly-fetched
                // disk data into visible UI after a Deep Audit run, so it must be able to
                // reveal the gate itself (and hide the loading/placeholder states) even if
                // the user never ran a Quick Audit before — not just when the gate already
                // happens to be open.
                renderHardDiskDashboard(disks, true);
                loadHwPartitions();
            } else {
                // No data came back (e.g. Deep Audit was the very first audit run and the
                // device hasn't reported anything yet) — fall back to the placeholder
                // instead of leaving the loading spinner or an empty gate on screen.
                $('#diskAuditLoading').hide();
                $('#diskAuditGate').hide();
                $('#diskAuditPlaceholder').show();
            }
        }).fail(function () {
            $('#diskAuditLoading').hide();
            $('#diskAuditGate').hide();
            $('#diskAuditPlaceholder').show();
        });
    }

    function pollDeepAuditCompletion(baselineDate, attempt, btn, originalText) {
        const MAX_ATTEMPTS = 40;      // ~10 minutes total at 15s intervals
        const POLL_INTERVAL_MS = 15000;

        $.get(`/ComputerSummary/GetDeepDiskReport?domain=${domaindata}`, function (report) {
            const endRaw = report && report.success !== false ? (report.endTime || report.EndTime) : null;
            const endDate = endRaw ? new Date(endRaw) : null;
            const isFreshAndFinished = endDate && endDate.getFullYear() > 1 && (!baselineDate || endDate > baselineDate);

            if (isFreshAndFinished) {
                sysAlert('Deep Audit (DST) completed! Loading fresh diagnostics...', 'success');
                refreshQuickDiskSection();
                loadDiskInfoDetails();
                loadHardDiskHistoryChart();
                // The whole point of running Deep Audit was to see this data, so switch
                // to that subtab now and load it — rather than fetching it into a panel
                // the user may not currently be looking at.
                invalidateDeepAuditData();
                const $deepTabBtn = $('.disk-subtab-btn[data-subtab="deep"]');
                if ($deepTabBtn.hasClass('active')) {
                    ensureDeepAuditDataLoaded(true);
                } else {
                    $deepTabBtn.trigger('click'); // click handler calls ensureDeepAuditDataLoaded()
                }
                resetDeepAuditButton(btn, originalText);
                return;
            }

            if (attempt >= MAX_ATTEMPTS) {
                sysAlert('Deep Audit is taking longer than expected and may still be running on the device. Showing the last completed results — refresh later to check again.', 'warning');
                refreshQuickDiskSection();
                loadDiskInfoDetails();
                invalidateDeepAuditData();
                resetDeepAuditButton(btn, originalText);
                return;
            }

            setTimeout(function () { pollDeepAuditCompletion(baselineDate, attempt + 1, btn, originalText); }, POLL_INTERVAL_MS);
        }).fail(function () {
            if (attempt >= MAX_ATTEMPTS) {
                resetDeepAuditButton(btn, originalText);
                return;
            }
            setTimeout(function () { pollDeepAuditCompletion(baselineDate, attempt + 1, btn, originalText); }, POLL_INTERVAL_MS);
        });
    }

    $('#btnAuditHardDiskDeep').on('click', function (e) {
        e.preventDefault();
        let btn = $(this);
        let originalText = btn.html();
        btn.html('<i class="fas fa-circle-notch fa-spin"></i> Running DST...');
        btn.prop('disabled', true);
        btn.css('opacity', '0.7');
        btn.addClass('disk-audit-running');
        window.hasLiveAuditOccurred = true;
        sysAlert('Deep Audit (DST) requested. This is a thorough self-test and can take several minutes — sending command to device...', 'info');

        // Only keep the gate open right away if it was already populated by a previous
        // audit. If this is the very first audit on the device, the gate has nothing in
        // it yet (hero/risk/usage all show placeholder dashes) — show the loading state
        // instead, same as Quick Audit, and let refreshQuickDiskSection() open the gate
        // once real data actually exists.
        const diskGateAlreadyPopulated = $('#diskAuditGate').is(':visible');
        $('#diskAuditPlaceholder').hide();
        if (diskGateAlreadyPopulated) {
            $('#diskAuditGate').show();
        } else {
            $('#diskAuditGate').hide();
            $('#diskAuditLoading').show();
        }

        function proceedWithBaseline(baselineDate) {
            setDeepAuditPendingUI('Deep Audit (DST) running on the device. This can take several minutes for a full self-test…');

            $.ajax({
                url: '/ComputerSummary/AuditHardDisk?domain=' + encodeURIComponent(domaindata) + '&hostName=' + encodeURIComponent(actualDomainName) + '&auditType=deep',
                type: 'POST',
                timeout: 15000,
                success: function (res) {
                    if (res && res.success) {
                        sysAlert(res.message || 'Deep Audit (DST) started on the device. This will refresh automatically once it finishes.', 'success');
                        pollDeepAuditCompletion(baselineDate, 0, btn, originalText);
                    } else {
                        sysAlert(res.message || 'Failed to initiate Deep Audit.', 'error');
                        refreshQuickDiskSection();
                        loadDiskInfoDetails();
                        invalidateDeepAuditData();
                        if ($('.disk-subtab-btn.active').data('subtab') === 'deep') ensureDeepAuditDataLoaded(true);
                        resetDeepAuditButton(btn, originalText);
                    }
                },
                error: function () {
                    sysAlert('Connection error while starting Deep Audit. Please try again.', 'error');
                    refreshQuickDiskSection();
                    loadDiskInfoDetails();
                    invalidateDeepAuditData();
                    if ($('.disk-subtab-btn.active').data('subtab') === 'deep') ensureDeepAuditDataLoaded(true);
                    resetDeepAuditButton(btn, originalText);
                }
            });
        }

        $.get(`/ComputerSummary/GetDeepDiskReport?domain=${domaindata}`)
            .done(function (existing) {
                const baselineRaw = existing && existing.success !== false ? (existing.endTime || existing.EndTime) : null;
                proceedWithBaseline(baselineRaw ? new Date(baselineRaw) : null);
            })
            .fail(function () {
                proceedWithBaseline(null);
            });
    });

    checkBatteryReportExists();
    $(document).on('click', '#diskDataTabBar .disk-data-tab', function () {
        const key = $(this).data('disktab');
        if ($(this).hasClass('active')) return;

        $('#diskDataTabBar .disk-data-tab').removeClass('active');
        $(this).addClass('active');

        const $panels = $('#diskDataTabPanels .disk-data-panel');
        const $current = $panels.filter('.active');
        const $next = $panels.filter('[data-diskpanel="' + key + '"]');
        if (!$next.length || $current.is($next)) return;

        $current.removeClass('disk-panel-visible');
        setTimeout(function () {
            $current.removeClass('active');
            $next.addClass('active');
            void $next[0].offsetHeight;
            $next.addClass('disk-panel-visible');

            setTimeout(function () {
                $(window).trigger('resize');
                if (window.diskTrendChartInstance) window.diskTrendChartInstance.resize();
                if (window.diskHealthChartInstance) window.diskHealthChartInstance.resize();
                if (window.diskUsageChartInstance) window.diskUsageChartInstance.resize();
            }, 60);
        }, 180);
    });
});
(function () {
    var CS_NAV_IMAGES = {
        '#System': '/images/systeminfo/system.png',
        '#Hardware': '/images/systeminfo/hardware.png',
        '#Software': '/images/systeminfo/software.png',
        '#security': '/images/systeminfo/security.png',
        '#PatchManagment': '/images/systeminfo/patchManagment.png',
        '#Restriction': '/images/systeminfo/restriction.png',
        '#UsbAudit': '/images/systeminfo/usbAudit.png',
        '#History': '/images/systeminfo/history.png',
        '#Updatelogs': '/images/systeminfo/updatelogs.png',

        '#BatteryHw': '',
        '#BIOSHw': '',
        '#HardDiskHw': '',
        '#KeyboardHw': '',
        '#MonitorHw': '',
        '#MotherboardHw': '',
        '#NetworkHw': '',
        '#MemoryHw': '',
        '#PointingHw': '',
        '#PrintersHw': '',
        '#ProcessorsHw': '',
        '#SoundHw': '',
        '#VideoHw': '',
        '#USBHw': '',

        '#ServicesSys': '/images/systeminfo/ServicesSys.png',
        '#UsersSys': '',
        '#GroupsSys': '',
        '#DriversSys': '',
        '#SharesSys': '',

        '#DesktopAppsSw': '',
        '#StoreAppsSw': '',
        '#InstallersSw': '',

        '#AntivirusSec': '',
        '#FirewallSec': '',
        '#BitLockerSec': '',
        '#ExternalDevSec': '',

        '#WindowsListTab': '',
        '#ThirdPartyTab': '',
        '#HotfixTab': '',

        '#AuditHistory': '',
        '#LoginHistory': '',

        '#SummaryLog': '',
        '#SystemLogs': '',
        '#HardwareLogs': '',
        '#SoftwareLogs': '',
        '#SecurityLogs': '',

        '#RestrictionOverview': '',
        '#UsbAuditOverview': ''
    };

    function cardHtml(target, iconClass, label, size) {
        var sizeClass = size === 'sm' ? ' cs-nav-card-sm' : '';
        var imgUrl = CS_NAV_IMAGES[target];
        var hasImgClass = imgUrl ? ' cs-nav-card-has-img' : '';
        var imgInner = imgUrl
            ? '<img src="' + imgUrl + '" alt="' + label + '" onerror="this.closest(\'.cs-nav-card-img\').classList.remove(\'cs-img-box\');this.parentElement.innerHTML=\'<i class=&quot;' + (iconClass || 'fas fa-square') + '&quot;></i>\';">'
            : '<i class="' + (iconClass || 'fas fa-square') + '"></i>';
        return '<div class="cs-nav-card' + sizeClass + hasImgClass + '" data-target="' + target + '">' +
            '<div class="cs-nav-card-img' + (imgUrl ? ' cs-img-box' : '') + '">' + imgInner + '</div>' +
            '<div class="cs-nav-card-label">' + label + '</div>' +
            '<i class="fas fa-chevron-right cs-nav-card-arrow"></i>' +
            '</div>';
    }

    function linkParts($a) {
        var target = $a.attr('href');
        var iconClass = $a.find('i').first().attr('class');
        var label = $.trim($a.clone().children().remove().end().text());
        return { target: target, iconClass: iconClass, label: label };
    }

    function buildMainCardGrid() {
        var $mainList = $('#mainTabList');
        var $grid = $('#mainTabCardGridInner');
        if (!$mainList.length || !$grid.length) return;

        $mainList.find('li.main-tab').each(function () {
            var $a = $(this).find('a').first();
            var parts = linkParts($a);
            if (!parts.target || parts.target === '#Summary') return;

            var $card = $(cardHtml(parts.target, parts.iconClass, parts.label));
            $card.on('click', function () {
                openMainSection(parts.target);
            });
            $grid.append($card);
        });
    }

    function buildSubCardGrids() {
        $('#mainTabContent > .tab-pane').each(function () {
            var $pane = $(this);
            var $subBar = $pane.find('> .cs-card > .cs-subtab-bar').first();
            var $innerContent = $pane.find('> .cs-card > .tab-content').first();
            if (!$subBar.length || !$innerContent.length) return;

            var $wrap = $('<div class="cs-subtab-card-grid-wrap"></div>');
            var $backBtn = $('<button type="button" class="cs-back-btn"><i class="fas fa-arrow-left"></i> Back to System Info</button>');
            $backBtn.on('click', function () { openMainCardGrid(); });
            var titleText = $.trim($pane.closest('.tab-pane').length ? ($mainLabelFor($pane.attr('id')) || '') : '');
            var $header = $('<div class="cs-subtab-card-grid-header"></div>').append($backBtn);
            if (titleText) {
                $header.append('<h3 class="cs-subtab-card-grid-title">' + titleText + '</h3>');
            }

            var numItems = $subBar.find('li').length;
            $pane.data('csNumItems', numItems);

            var $subGrid = null;
            if (numItems > 1) {
                $subGrid = $('<div class="cs-card-grid cs-card-grid-sm"></div>');
                $subBar.find('li').each(function () {
                    var $a = $(this).find('a').first();
                    var parts = linkParts($a);
                    if (!parts.target) return;

                    var $card = $(cardHtml(parts.target, parts.iconClass, parts.label || 'Overview', 'sm'));
                    $card.on('click', function () {
                        $a.trigger('click');
                        showSubContent($pane);
                    });
                    $subGrid.append($card);
                });
                $wrap.append($header).append($subGrid);
            } else {
                $wrap.append($header);
                $subBar.find('li a').first().trigger('click');
            }

            $subBar.before($wrap);
            $subBar.css('display', 'none');

            var $backToCards = null;
            if (numItems > 1) {
                $backToCards = $('<button type="button" class="cs-back-btn cs-sub-back-btn"><i class="fas fa-th-large"></i> Back to categories</button>');
                $backToCards.on('click', function () { showSubCards($pane); });
                $innerContent.before($backToCards);
            }

            $pane.data('csCardWrap', $wrap);
            $pane.data('csInnerContent', $innerContent);
            if ($backToCards) $pane.data('csBackToCards', $backToCards);
            $pane.data('csSubBar', $subBar);
            if ($subGrid) $pane.data('csSubGrid', $subGrid);
            $pane.data('csHeader', $header);
        });
    }

    function $mainLabelFor(paneId) {
        var $link = $('#mainTabList a[href="#' + paneId + '"]');
        if (!$link.length) return '';
        return $.trim($link.clone().children().remove().end().text());
    }

    var currentSection = '#System';

    function hideAll() {
        $('#summaryDashboardContainer').hide();
        $('#csEntryBanner').hide();
        $('#systemInfoHeader').hide();
        $('#mainTabCardGrid').hide();
        $('#tabViewContainer').hide();
    }

    function goToSummary() {
        hideAll();
        window.isAppCardView = false;
        updateToggleButton(false);

        $('#csEntryBanner').show();
        $('#summaryDashboardContainer').show();

        $('#mainTabContent > .tab-pane').removeClass('active');
        $('#mainTabList .main-tab').removeClass('active');
        $('#mainTabList .main-tab[data-target="#System"]').addClass('active');
        currentSection = '#System';

        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function goToCardGrid() {
        hideAll();
        window.isAppCardView = true;
        updateToggleButton(true);

        $('#systemInfoHeader').css('display', 'flex');
        $('#mainTabCardGrid').show();
        $('#mainTabCardGridInner').show();

        if ($('#mainTabCardGrid')[0] && $('#mainTabCardGrid')[0].scrollIntoView) {
            $('#mainTabCardGrid')[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    function goToSection(target, forceTabView) {
        hideAll();
        var $link = $('#mainTabList a[href="' + target + '"]');
        if (!$link.length) return;

        currentSection = target;

        $('#mainTabList .main-tab').removeClass('active');
        $link.closest('li').addClass('active');

        $('#mainTabContent > .tab-pane').removeClass('active');
        $(target).addClass('active');

        $('#systemInfoHeader').css('display', 'flex');
        $('#tabViewContainer').show();

        var $pane = $(target);

        if (window.isAppCardView && !forceTabView) {
            $('#tabViewContainer > .cs-tab-bar').hide();
            updateToggleButton(true);
            enableCardModeForPane($pane);
        } else {
            window.isAppCardView = false;
            $('#tabViewContainer > .cs-tab-bar').show();
            updateToggleButton(false);
            enableTabModeForAllPanes();
        }

        if ($link.length) $link.trigger('click');

        if ($pane[0] && $pane[0].scrollIntoView) {
            $pane[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    function enableCardModeForPane($pane) {
        var $wrap = $pane.data('csCardWrap');
        var $inner = $pane.data('csInnerContent');
        var $backToCards = $pane.data('csBackToCards');
        var $subBar = $pane.data('csSubBar');
        var $subGrid = $pane.data('csSubGrid');
        var numItems = $pane.data('csNumItems') || 0;

        if ($subBar) $subBar.css('display', 'none');
        if ($wrap && $wrap.length) $wrap.show();
        if ($subGrid) $subGrid.show();

        if (numItems > 1) {
            if ($inner && $inner.length) $inner.hide();
            if ($backToCards && $backToCards.length) $backToCards.hide();
        } else {
            if ($inner && $inner.length) $inner.show();
        }
    }

    function enableTabModeForAllPanes() {
        $('#mainTabContent > .tab-pane').each(function () {
            var $pane = $(this);
            var $wrap = $pane.data('csCardWrap');
            var $subBar = $pane.data('csSubBar');
            var $backToCards = $pane.data('csBackToCards');
            var $innerContent = $pane.data('csInnerContent');
            var $subGrid = $pane.data('csSubGrid');

            if ($wrap) $wrap.hide();
            if ($subGrid) $subGrid.hide();
            if ($backToCards) $backToCards.hide();
            if ($subBar) $subBar.css('display', 'flex');
            if ($innerContent) $innerContent.show();

            if ($subBar && $subBar.find('li.active').length === 0) {
                $subBar.find('li a').first().trigger('click');
            }
        });
    }

    function showSubCards($pane) {
        enableCardModeForPane($pane);
    }

    function showSubContent($pane) {
        var $wrap = $pane.data('csCardWrap');
        var $inner = $pane.data('csInnerContent');
        var $backToCards = $pane.data('csBackToCards');
        if ($wrap && $wrap.length) $wrap.hide();
        if ($inner && $inner.length) $inner.show();
        if ($backToCards && $backToCards.length) $backToCards.show();

        setTimeout(function () {
            $(window).trigger('resize');
            if ($.fn.DataTable) {
                $inner.find('table.dataTable').each(function () {
                    if ($.fn.DataTable.isDataTable(this)) {
                        $(this).DataTable().columns.adjust();
                    }
                });
            }
        }, 150);
    }


    function updateToggleButton(isCardView) {
        var $btn = $('#btnUniversalViewToggle');
        if (isCardView) {
            $btn.html('<i class="fas fa-list"></i> <span>Tab View</span>');
        } else {
            $btn.html('<i class="fas fa-th-large"></i> <span>Card View</span>');
        }
    }

    function openMainCardGrid() {
        goToCardGrid();
    }

    function openMainSection(target) {
        goToSection(target);
    }
    function backToSummary() {
        goToSummary();
    }
    function toggleView() {
        if (window.isAppCardView) {
            var activeSection = currentSection || '#System';
            window.isAppCardView = false;
            goToSection(activeSection, true);
        } else {
            window.isAppCardView = true;
            goToCardGrid();
        }
    }


    $(document).ready(function () {
        buildMainCardGrid();
        buildSubCardGrids();

        window.isAppCardView = false;

        $('#btnOpenSystemInfo').on('click', function () { openMainCardGrid(); });
        $('#btnBackToSummary').on('click', function () { backToSummary(); });
        $('#btnUniversalViewToggle').on('click', function () { toggleView(); });
    });

})();