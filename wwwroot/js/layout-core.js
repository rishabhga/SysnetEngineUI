(function () {
    if (window.__SYSNET_LAYOUT_CORE_INIT) return;
    window.__SYSNET_LAYOUT_CORE_INIT = true;

    function initHeaderDropdowns() {
        var profileBtn = document.getElementById("profileBtn");
        var profileDrop = document.getElementById("profileDropdown");
        var bellBtn = document.getElementById("notificationBell");
        var notifPanel = document.getElementById("notificationPanel");

        function open(panel, btn) {
            if (!panel) return;
            panel.classList.add("open");
            if (btn) {
                btn.classList.add("open");
                btn.setAttribute("aria-expanded", "true");
            }
        }

        function close(panel, btn) {
            if (!panel) return;
            panel.classList.remove("open");
            if (btn) {
                btn.classList.remove("open");
                btn.setAttribute("aria-expanded", "false");
            }
        }

        function toggle(panel, btn, other, otherBtn) {
            var isOpen = panel && panel.classList.contains("open");
            close(other, otherBtn);
            if (isOpen) {
                close(panel, btn);
            } else {
                open(panel, btn);
                // Load notifications when panel opens
                if (panel === notifPanel) {
                    loadNotificationPanel();
                }
            }
        }

        if (profileBtn) {
            profileBtn.addEventListener("click", function (e) {
                e.stopPropagation();
                toggle(profileDrop, profileBtn, notifPanel, bellBtn);
            });
        }

        if (bellBtn) {
            bellBtn.addEventListener("click", function (e) {
                e.stopPropagation();
                toggle(notifPanel, bellBtn, profileDrop, profileBtn);
            });
        }

        document.addEventListener("click", function (e) {
            if (profileDrop && profileDrop.classList.contains("open")) {
                if (!profileDrop.contains(e.target) && !profileBtn.contains(e.target)) {
                    close(profileDrop, profileBtn);
                }
            }
            if (notifPanel && bellBtn && notifPanel.classList.contains("open")) {
                if (!notifPanel.contains(e.target) && !bellBtn.contains(e.target)) {
                    close(notifPanel, bellBtn);
                }
            }
        });

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") {
                close(profileDrop, profileBtn);
                close(notifPanel, bellBtn);
            }
        });
    }

    // ── Notification Badge & Panel Loading ──
    function loadNotificationCount() {
        if (typeof $ === "undefined") return;
        
        const ctx = window.SYSNET_CONTEXT || {};
        let url = "/Home/GetNotificationCount";
        let params = {};

        // If on dashboard and we have a specific location, use the scoped API
        if (ctx.isDashboard && ctx.locationId > 0 && ctx.locationId !== "0") {
            url = "/ComputerSummary/GetNotificationsByLocation";
            params = { companyId: ctx.companyId, groupId: ctx.groupId, locationId: ctx.locationId };
        }
        params._ = new Date().getTime();

        $.ajax({
            url: url,
            data: params,
            cache: false,
            success: function (data) {
                var count = 0;
                if (Array.isArray(data)) {
                    // If it's the scoped API returning an array, count unread items
                    count = data.filter(n => !n.isRead && !n.IsRead).length;
                } else {
                    // If it's the global API returning a count object
                    count = (data && data.count !== undefined) ? data.count : (data && data.Count !== undefined) ? data.Count : 0;
                }

                var badge = document.getElementById("notificationBadge");
                var label = document.getElementById("notificationCountLabel");
                if (badge) {
                    if (count > 0) {
                        badge.textContent = count > 99 ? "99+" : count;
                        badge.style.display = "flex";
                    } else {
                        badge.style.display = "none";
                    }
                }
                if (label) {
                    label.textContent = count + " new";
                }
            },
            error: function() {
                console.warn("Failed to load notification count");
            }
        });
    }

    function loadNotificationPanel() {
        if (typeof $ === "undefined") return;
        var listEl = document.getElementById("notificationList");
        if (!listEl) return;

        const ctx = window.SYSNET_CONTEXT || {};
        let url = "/Home/GetNotifications";
        let params = {};

        if (ctx.isDashboard && ctx.locationId > 0 && ctx.locationId !== "0") {
            url = "/ComputerSummary/GetNotificationsByLocation";
            params = { companyId: ctx.companyId, groupId: ctx.groupId, locationId: ctx.locationId };
        }

        listEl.innerHTML = '<div style="text-align:center;padding:1.5rem;color:#94a3b8;"><i class="fas fa-spinner fa-spin"></i><br/><span style="font-size:10px;">Loading alerts...</span></div>';

        // Add timestamp to prevent caching issues which caused the "first click" bug
        params._ = new Date().getTime();

        $.ajax({
            url: url,
            data: params,
            cache: false,
            success: function (data) {
                var items = [];
                if (Array.isArray(data)) {
                    items = data;
                } else if (data && (data.items || data.Items)) {
                    items = data.items || data.Items;
                }

            if (!items || !items.length) {
                listEl.innerHTML = '<div style="text-align:center;padding:2rem;color:#94a3b8;"><i class="fas fa-check-circle" style="color:#22c55e;font-size:1.5rem;display:block;margin-bottom:0.5rem;"></i><span style="font-size:11px;">No new notifications</span></div>';
                return;
            }

            var html = items.slice(0, 20).map(function (n) {
                var id = n.id || n.Id;
                var msg = n.message || n.Message || "";
                var source = n.machineId || n.MachineId || "System";
                var isRead = n.isRead || n.IsRead;
                var type = (n.msNotificationType || n.MSNotificationType || "info").toLowerCase();
                var time = n.createdAt || n.CreatedAt || "";
                
                var iconClass = "fa-info-circle";
                var iconColor = "#3b82f6";
                if (type.includes("crit") || type.includes("alert")) { iconClass = "fa-exclamation-circle"; iconColor = "#ef4444"; }
                else if (type.includes("warn")) { iconClass = "fa-exclamation-triangle"; iconColor = "#f59e0b"; }
                
                // Parse hidden download links
                var downloadBtn = "";
                if (msg.includes("[DOWNLOAD_LINK:")) {
                    var parts = msg.split("[DOWNLOAD_LINK:");
                    var urlPart = parts[1].split("]")[0];
                    msg = parts[0];
                    downloadBtn = '<div style="margin-top:6px;"><a href="' + urlPart + '" class="notif-download-link" style="display:inline-block;padding:3px 8px;background:#0ea5e9;color:#fff;border-radius:4px;font-size:9px;font-weight:600;text-decoration:none;"><i class="fas fa-download" style="margin-right:4px;"></i>Download EXE</a></div>';
                }

                return '<div class="notification-item" data-id="' + id + '" style="padding:10px 14px;border-bottom:1px solid #f1f5f9;display:flex;gap:10px;cursor:pointer;' + (isRead ? 'opacity:0.6;' : 'background:#f8fafc;') + '">' +
                    '<i class="fas ' + iconClass + '" style="color:' + iconColor + ';margin-top:3px;font-size:12px;"></i>' +
                    '<div style="flex:1;min-width:0;">' +
                    '<p style="font-size:11px;font-weight:500;color:#334155;margin:0;line-height:1.4;">' + msg + '</p>' +
                    downloadBtn +
                    '<div style="display:flex;justify-content:space-between;margin-top:4px;font-size:9px;color:#94a3b8;">' +
                    '<span style="max-width:100px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">' + source + '</span>' +
                    '<span>' + (time ? new Date(time).toLocaleTimeString([], {hour:"2-digit", minute:"2-digit"}) : "") + '</span>' +
                    '</div></div></div>';
            }).join("");

            listEl.innerHTML = html;
            
            // Add click handlers for unread notifications to mark them read
            var notifItems = listEl.querySelectorAll('.notification-item');
            notifItems.forEach(function(item) {
                item.addEventListener('click', function(e) {
                    if (e.target.closest('a')) return; // Ignore link clicks
                    
                    var id = this.getAttribute('data-id');
                    var isRead = this.style.opacity === '0.6';
                    
                    if (!isRead) {
                        this.style.opacity = '0.6';
                        this.style.background = '#fff';
                        $.post("/ComputerSummary/MarkNotificationRead", { id: id }, function() {
                            loadNotificationCount();
                        });
                    }
                });
            });

        },
        error: function() {
            listEl.innerHTML = '<div style="text-align:center;padding:2rem;color:#ef4444;"><i class="fas fa-exclamation-circle" style="font-size:1.5rem;display:block;margin-bottom:0.5rem;"></i><span style="font-size:11px;">Error loading notifications</span></div>';
        }
        });
    }

    // Make loadNotificationPanel globally available for manual refresh
    window.loadNotificationPanel = loadNotificationPanel;
    window.loadNotificationCount = loadNotificationCount;

    function setupAjaxUnauthorizedHandling() {
        if (typeof $ === "undefined") return;
        $.ajaxSetup({
            error: function (jqXHR) {
                if (jqXHR.status === 401 || jqXHR.status === 403) {
                    if (typeof window.sysAlert === "function") {
                        window.sysAlert("Session expired or unauthorized access. Redirecting...", "error");
                    }
                    setTimeout(function () {
                        window.location.href = "/Auth/Login";
                    }, 1500);
                } else if (jqXHR.status === 0) {
                    if (typeof window.sysAlert === "function") {
                        window.sysAlert("Network error: Please check your connection or VPN.", "error");
                    }
                } else if (jqXHR.status >= 500) {
                    if (typeof window.sysAlert === "function") {
                        window.sysAlert("Server error: High demand or backend issue detected.", "error");
                    }
                }
            }
        });
    }

    window.initLayoutCore = function () {
        initHeaderDropdowns();
        setupAjaxUnauthorizedHandling();

        // Load notification count on page load
        loadNotificationCount();

        // Auto-refresh notification count every 60 seconds
        setInterval(loadNotificationCount, 60000);
    };
})();
