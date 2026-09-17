(function () {
    "use strict";
    if (window.__SYSNET_NAV_INIT) return;
    window.__SYSNET_NAV_INIT = true;
    
    window.navigateToEncryptedDashboard = async function(comId, companyName, groupId, groupName, locationId, locationName) {
        const payload = {
            CompanyId: comId || 0, CompanyName: companyName || '',
            GroupId: groupId || 0, GroupName: groupName || '',
            LocationId: locationId || 0, LocationName: locationName || ''
        };
        try {
            const response = await fetch('/Home/EncryptContext', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const data = await response.json();
            if (data.success && data.token) {
                window.location.href = '/ComputerSummary/Deshboad?token=' + encodeURIComponent(data.token);
            } else {
                console.error('Failed to encrypt context');
            }
        } catch (e) {
            console.error(e);
        }
    };

    window.navigateToEncrypted = async function(urlPath, params) {
        try {
            const response = await fetch('/Home/EncryptDict', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(params)
            });
            const data = await response.json();
            if (data.success && data.q) {
                window.location.href = urlPath + '?q=' + encodeURIComponent(data.q);
            } else {
                console.error('Failed to encrypt dictionary params');
            }
        } catch (e) {
            console.error(e);
        }
    };

    var STORAGE_KEY = "sysnet.sidebar.collapsed";
    var SCROLL_KEY  = "sysnet.scroll.";
    var FILTER_KEY  = "sysnet.filters.";
    function getSidebar()  { return document.getElementById("app-sidebar"); }
    function getOverlay()  { return document.getElementById("sidebarOverlay"); }
    function isMobile()    { return window.innerWidth <= 768; }
    function setCollapsed(isCollapsed) {
        var sidebar = getSidebar();
        var overlay = getOverlay();
        if (!sidebar) return;

        if (isCollapsed) {
            sidebar.classList.add("collapsed");
            document.documentElement.classList.add("sidebar-collapsed");
            if (overlay) overlay.classList.remove("active");
        } else {
            sidebar.classList.remove("collapsed");
            document.documentElement.classList.remove("sidebar-collapsed");
            if (isMobile() && overlay) {
                overlay.classList.add("active");
            }
        }

        if (!isMobile()) {
            localStorage.setItem(STORAGE_KEY, isCollapsed ? "1" : "0");
        }

        try { window.dispatchEvent(new Event("resize")); } catch (e) {}
    }

    function initSidebarState() {
        var sidebar = getSidebar();
        if (!sidebar) return;

        if (isMobile()) {
            setCollapsed(true);
        } else {
            var saved = localStorage.getItem(STORAGE_KEY);
            setCollapsed(saved === "1");
        }
    }

    window.sysnetGoBackToTickets = function (e) {
        if (e) {
            if (e.preventDefault) e.preventDefault();
            if (e.stopPropagation) e.stopPropagation();
        }
        var ref = (document.referrer || "").toLowerCase();
        if (window.history.length > 1 && ref && ref.indexOf("/servicedesk/tickets") !== -1) {
            window.history.back();
            return false;
        }
        window.location.replace("/ServiceDesk/Tickets");
        return false;
    };

    window.sysnetGoBack = function (fallbackUrl) {
        var path = (window.location.pathname || "").toLowerCase();
        var ref = (document.referrer || "").toLowerCase();

        // 1. Details or CreateTicket: return to Tickets cleanly
        if (path.indexOf("/servicedesk/details") !== -1 || path.indexOf("/servicedesk/createticket") !== -1) {
            window.sysnetGoBackToTickets();
            return;
        }

        // 2. Tickets page: Back means Dashboard / Service Desk parent, never loop into child
        if (path.indexOf("/servicedesk/tickets") !== -1) {
            if (ref && (ref.indexOf("/servicedesk/details") !== -1 || ref.indexOf("/servicedesk/createticket") !== -1)) {
                // Referrer was child view! History.back() would take us right back into child!
                window.location.href = "/ServiceDesk";
                return;
            }
            if (window.history.length > 1 && ref && ref.indexOf(window.location.host.toLowerCase()) !== -1) {
                window.history.back();
                return;
            }
            window.location.href = "/ServiceDesk";
            return;
        }

        // 3. Other Service Desk subpages (Reports, SLA, Parts, Settings)
        if (path.indexOf("/servicedesk") !== -1 && path !== "/servicedesk" && path !== "/servicedesk/index" && path !== "/servicedesk/dashboard") {
            if (window.history.length > 1 && ref && ref.indexOf(window.location.host.toLowerCase()) !== -1 && ref.indexOf(path) === -1) {
                window.history.back();
                return;
            }
            window.location.href = "/ServiceDesk";
            return;
        }

        if (fallbackUrl) {
            window.location.href = fallbackUrl;
            return;
        }

        // 4. General fallback
        if (window.history.length > 1 && ref && ref.indexOf(window.location.host.toLowerCase()) !== -1) {
            window.history.back();
        } else {
            window.location.href = "/";
        }
    };

    function initBreadcrumbs() {
        var breadcrumbs = document.getElementById("breadcrumbs");
        if (!breadcrumbs) return;

        var html = "";
        var currentPath = (window.location.pathname || "").toLowerCase();

        // Show back button on all non-root pages or when history exists
        if (window.history.length > 1 || (currentPath !== "/" && currentPath !== "/home" && currentPath !== "/home/index")) {
            html += '<button type="button" class="breadcrumb-back-btn" onclick="window.sysnetGoBack()" title="Go back">';
            html += '<i class="fas fa-arrow-left"></i>';
            html += '</button>';
        }

        html += '<a href="/" class="breadcrumb-link"><i class="fas fa-home text-[11px] mr-1"></i>Home</a>';

        // Context check for computersummary/deshboad
        if (currentPath.indexOf("computersummary/deshboad") !== -1) {
            var ctx = window.SYSNET_CONTEXT || {};
            if (ctx.company) {
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<a href="/Companies/Companies" class="breadcrumb-link">' + ctx.company + '</a>';
            }
            if (ctx.group) {
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<span class="breadcrumb-current">' + ctx.group + '</span>';
            }
            breadcrumbs.innerHTML = html;
            return;
        }

        // Special dedicated handling for Service Desk module
        if (currentPath.indexOf("/servicedesk") !== -1) {
            var isRoot = currentPath === "/servicedesk" || currentPath === "/servicedesk/" || currentPath === "/servicedesk/index" || currentPath === "/servicedesk/dashboard";
            
            html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
            if (isRoot) {
                html += '<span class="breadcrumb-current"><i class="fas fa-headset text-[11px] mr-1 text-cyan-600"></i>Service Desk</span>';
            } else {
                html += '<a href="/ServiceDesk" class="breadcrumb-link"><i class="fas fa-headset text-[11px] mr-1"></i>Service Desk</a>';
            }

            if (currentPath.indexOf("/servicedesk/tickets") !== -1) {
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<span class="breadcrumb-current"><i class="fas fa-ticket-alt text-[10px] mr-1"></i>All Tickets</span>';
            } else if (currentPath.indexOf("/servicedesk/createticket") !== -1) {
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<a href="/ServiceDesk/Tickets" onclick="window.sysnetGoBackToTickets(event)" class="breadcrumb-link">Tickets</a>';
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<span class="breadcrumb-current">New Request</span>';
            } else if (currentPath.indexOf("/servicedesk/details") !== -1) {
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<a href="/ServiceDesk/Tickets" onclick="window.sysnetGoBackToTickets(event)" class="breadcrumb-link">Tickets</a>';
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                
                var ticketNoEl = document.querySelector("#headerStatusBadge")?.previousElementSibling;
                var ticketNo = ticketNoEl ? ticketNoEl.textContent.trim() : "";
                html += '<span class="breadcrumb-current">' + (ticketNo ? ('Ticket ' + ticketNo) : 'Ticket Details') + '</span>';
            } else if (currentPath.indexOf("/servicedesk/reports") !== -1) {
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<span class="breadcrumb-current">Reports & Analytics</span>';
            } else if (currentPath.indexOf("/servicedesk/slamanagement") !== -1) {
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<span class="breadcrumb-current">SLA Policies</span>';
            } else if (currentPath.indexOf("/servicedesk/partsinventory") !== -1) {
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<span class="breadcrumb-current">Parts Inventory</span>';
            } else if (currentPath.indexOf("/servicedesk/adminsettings") !== -1) {
                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                html += '<span class="breadcrumb-current">Admin Settings</span>';
            }
            breadcrumbs.innerHTML = html;
            return;
        }

        // Generic fallback for all other modules with camelCase spacing
        var segments = window.location.pathname.split("/").filter(Boolean);
        if (segments.length > 0) {
            for (var i = 0; i < segments.length; i++) {
                var seg = segments[i];
                if (seg.toLowerCase() === "index" && i > 0) continue;

                var clean = decodeURIComponent(seg)
                    .replace(/([a-z])([A-Z])/g, '$1 $2')
                    .replace(/[-_]+/g, ' ')
                    .trim();
                var label = clean.charAt(0).toUpperCase() + clean.slice(1);

                html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
                if (i === segments.length - 1 || (i === segments.length - 2 && segments[segments.length - 1].toLowerCase() === "index")) {
                    html += '<span class="breadcrumb-current">' + label + '</span>';
                    break;
                } else {
                    var segPath = "/" + segments.slice(0, i + 1).join("/");
                    html += '<a href="' + segPath + '" class="breadcrumb-link">' + label + '</a>';
                }
            }
        }

        breadcrumbs.innerHTML = html;
    }


    function initMenus() {
        document.addEventListener("click", function (e) {
            var trigger = e.target.closest(".group-trigger");
            if (!trigger) {
                var link = e.target.closest(".sidebar-link, .sidebar-sub-link");
                if (link && !link.classList.contains("group-trigger")) {
                    setCollapsed(true);
                }
                return;
            }

            e.preventDefault();
            e.stopPropagation();
            var group = trigger.closest(".nav-group");
            if (!group) return;

            var sidebar = getSidebar();
            if (sidebar && sidebar.classList.contains("collapsed")) {
                setCollapsed(false);
                setTimeout(function () { group.classList.add("open"); }, 150);
                return;
            }

            group.classList.toggle("open");
        });

        var activeSubLink = document.querySelector(".sidebar-sub-link.active");
        if (activeSubLink) {
            var parentGroup = activeSubLink.closest(".nav-group");
            if (parentGroup) parentGroup.classList.add("open");
        }
    }

    function initDelegation() {
        document.addEventListener("click", function (e) {
            var toggleBtn = e.target.closest("#sidebarToggle");
            if (toggleBtn) {
                e.preventDefault();
                e.stopPropagation();
                var sidebar = getSidebar();
                if (sidebar) {
                    var isNowCollapsed = !sidebar.classList.contains("collapsed");
                    setCollapsed(isNowCollapsed);
                }
                return;
            }
        });

        var overlay = getOverlay();
        if (overlay) {
            overlay.addEventListener("click", function () {
                setCollapsed(true);
            });
        }
        var resizeTimer;
        window.addEventListener("resize", function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                var sidebar = getSidebar();
                var overlayEl = getOverlay();
                if (!sidebar) return;

                if (isMobile()) {
                    if (!sidebar.classList.contains("collapsed") && overlayEl) {
                        overlayEl.classList.add("active");
                    }
                } else {
                    if (overlayEl) overlayEl.classList.remove("active");
                }
            }, 150);
        });
    }

    function getRouteKey() {
        return window.location.pathname + window.location.search;
    }

    function saveScrollPosition() {
        var mainContent = document.getElementById("mainContent");
        if (mainContent) {
            try {
                sessionStorage.setItem(SCROLL_KEY + getRouteKey(), mainContent.scrollTop.toString());
            } catch (e) {}
        }
    }

    function restoreScrollPosition() {
        var mainContent = document.getElementById("mainContent");
        if (!mainContent) return;

        try {
            var saved = sessionStorage.getItem(SCROLL_KEY + getRouteKey());
            if (saved) {
                var pos = parseInt(saved, 10);
                if (!isNaN(pos)) {
                    setTimeout(function () { mainContent.scrollTop = pos; }, 100);
                }
            }
        } catch (e) {}
    }

    function initStatePersistence() {
        window.addEventListener("beforeunload", saveScrollPosition);
        document.addEventListener("click", function (e) {
            var link = e.target.closest("a[href]");
            if (link && link.hostname === window.location.hostname) {
                saveScrollPosition();
            }
        });

        restoreScrollPosition();
        window.SYSNET_STATE = {
            saveFilter: function (key, value) {
                try { sessionStorage.setItem(FILTER_KEY + getRouteKey() + "." + key, JSON.stringify(value)); } catch (e) {}
            },
            getFilter: function (key) {
                try {
                    var val = sessionStorage.getItem(FILTER_KEY + getRouteKey() + "." + key);
                    return val ? JSON.parse(val) : null;
                } catch (e) { return null; }
            },
            clearFilters: function () {
                try {
                    var prefix = FILTER_KEY + getRouteKey();
                    Object.keys(sessionStorage).forEach(function (k) {
                        if (k.indexOf(prefix) === 0) sessionStorage.removeItem(k);
                    });
                } catch (e) {}
            }
        };
    }

    function init() {
        initSidebarState();
        initDelegation();
        initMenus();
        initBreadcrumbs();
        initStatePersistence();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
