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

    function loadNotificationCount() {
        if (typeof $ === "undefined") return;
        
        const ctx = window.SYSNET_CONTEXT || {};
        let url = "/Home/GetNotificationCount";
        let params = {};

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
                    count = data.filter(n => !n.isRead && !n.IsRead).length;
                } else {
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

        params._ = new Date().getTime();

        console.log("Loading notifications for context:", ctx.isDashboard ? "Dashboard" : "General", params);

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
                } else if (data && (data.data || data.Data)) {
                    items = data.data || data.Data;
                }

                if (items.length === 0 && url.includes("GetNotificationsByLocation")) {
                    console.log("Location notifications empty, falling back to general...");
                    url = "/Home/GetNotifications";
                    params = {};
                    $.ajax({
                        url: url,
                        success: function(fallbackData) {
                            var fallbackItems = Array.isArray(fallbackData) ? fallbackData : (fallbackData.items || fallbackData.data || []);
                            renderNotificationList(fallbackItems);
                        }
                    });
                    return;
                }

                renderNotificationList(items);
            },
            error: function (xhr) {
                console.error("Notification load error:", xhr.status, xhr.responseText);
                listEl.innerHTML = '<div style="text-align:center;padding:2rem;color:#ef4444;"><i class="fas fa-exclamation-circle" style="font-size:1.5rem;display:block;margin-bottom:0.5rem;"></i><span style="font-size:11px;">Error loading notifications</span></div>';
            }
        });

        function renderNotificationList(items) {
            if (!items || !items.length) {
                listEl.innerHTML = '<div style="text-align:center;padding:2rem;color:#94a3b8;"><i class="fas fa-check-circle" style="color:#22c55e;font-size:1.5rem;display:block;margin-bottom:0.5rem;"></i><span style="font-size:11px;">No new notifications</span></div>';
                return;
            }

            var html = items.slice(0, 20).map(function (n) {
                var id = n.id || n.Id;
                var msg = n.message || n.Message || "";
                var source = n.machineId || n.MachineId || "System";
                var isRead = n.isRead || n.IsRead;
                var type = (n.msNotificationType || n.MSNotificationType || "INFO").toUpperCase();
                var time = n.createdAt || n.CreatedAt || "";
                
                var iconClass = "fa-info-circle";
                var iconColor = "#3b82f6";
                var bgColor = isRead ? "#fff" : "#f8fafc";
                var borderLeft = "none";

                if (type.includes("CRIT") || type.includes("ALERT")) { 
                    iconClass = "fa-exclamation-circle"; 
                    iconColor = "#ef4444"; 
                    if (!isRead) { bgColor = "#fef2f2"; borderLeft = "3px solid #ef4444"; }
                }
                else if (type.includes("WARN")) { 
                    iconClass = "fa-exclamation-triangle"; 
                    iconColor = "#f59e0b"; 
                    if (!isRead) { bgColor = "#fffbeb"; borderLeft = "3px solid #f59e0b"; }
                }
                
                var formattedTime = "";
                if (time) {
                    var d = new Date(time);
                    if (!isNaN(d.getTime())) {
                        formattedTime = d.toLocaleTimeString([], {hour:"2-digit", minute:"2-digit"});
                        var now = new Date();
                        if (d.toDateString() !== now.toDateString()) {
                            formattedTime = d.toLocaleDateString([], {month:'short', day:'numeric'}) + " " + formattedTime;
                        }
                    }
                }

                var downloadBtn = "";
                if (msg.includes("[DOWNLOAD_LINK:")) {
                    var parts = msg.split("[DOWNLOAD_LINK:");
                    var urlPart = parts[1].split("]")[0];
                    msg = parts[0];
                    downloadBtn = '<div style="margin-top:6px;"><a href="' + urlPart + '" class="notif-download-link" style="display:inline-block;padding:3px 8px;background:#0ea5e9;color:#fff;border-radius:4px;font-size:9px;font-weight:600;text-decoration:none;"><i class="fas fa-download" style="margin-right:4px;"></i>Download EXE</a></div>';
                }

                return '<div class="notification-item" data-id="' + id + '" style="padding:12px 14px;border-bottom:1px solid #f1f5f9;display:flex;gap:12px;cursor:pointer;transition:all 0.2s;' + 
                    (isRead ? 'opacity:0.7;' : 'background:' + bgColor + ';border-left:' + borderLeft + ';') + '">' +
                    '<div style="margin-top:2px;"><i class="fas ' + iconClass + '" style="color:' + iconColor + ';font-size:13px;"></i></div>' +
                    '<div style="flex:1;min-width:0;">' +
                    '<div style="display:flex;justify-content:between;align-items:start;margin-bottom:2px;">' +
                    '<span style="font-size:9px;font-weight:700;color:' + iconColor + ';text-transform:uppercase;letter-spacing:0.02em;">' + type + '</span>' +
                    '<span style="margin-left:auto;font-size:9px;color:#94a3b8;">' + formattedTime + '</span>' +
                    '</div>' +
                    '<p style="font-size:11px;font-weight:600;color:#334155;margin:0;line-height:1.4;">' + msg + '</p>' +
                    downloadBtn +
                    '<div style="margin-top:4px;font-size:9px;color:#64748b;display:flex;align-items:center;gap:4px;">' +
                    '<i class="fas fa-desktop" style="font-size:8px;opacity:0.7;"></i>' +
                    '<span style="max-width:120px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">' + source + '</span>' +
                    (!isRead ? '<span style="margin-left:auto;width:6px;height:6px;background:#3b82f6;border-radius:50%;"></span>' : '') +
                    '</div></div></div>';
            }).join("");

            listEl.innerHTML = html;
            
            var notifItems = listEl.querySelectorAll('.notification-item');
            notifItems.forEach(function(item) {
                item.addEventListener('click', function(e) {
                    if (e.target.closest('a')) return;
                    
                    var id = this.getAttribute('data-id');
                    var isRead = this.style.opacity === '0.7';
                    
                    if (!isRead) {
                        this.style.opacity = '0.7';
                        this.style.background = '#fff';
                        this.style.borderLeft = 'none';
                        $.post("/ComputerSummary/MarkNotificationRead", { id: id }, function() {
                            loadNotificationCount();
                        });
                    }
                });
            });
        }
    };

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
        loadNotificationCount();
        setInterval(loadNotificationCount, 60000);

        $(document).on('keyup', '#globalSearchInput', function () {
            var term = $(this).val().toLowerCase().trim();
            var $cards = $('.device-card, .bg-white.rounded-lg.shadow-md.border');

            if ($cards.length === 0) {
                $cards = $('.grid > div').filter(function() {
                    return $(this).hasClass('bg-white') || $(this).hasClass('device-card');
                });
            }

            $cards.each(function () {
                var $card = $(this);
                var text = $card.text().toLowerCase();
                if (text.indexOf(term) > -1) {
                    $card.show();
                } else {
                    $card.hide();
                }
            });
        });
    };
})();
