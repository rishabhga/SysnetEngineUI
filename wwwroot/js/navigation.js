/**
 * SYSNET Navigation System — Single Source of Truth
 * Handles: Sidebar toggle, sub-menus, breadcrumbs, back button, overlay, state persistence
 * IMPORTANT: This is the ONLY file that should control sidebar/navigation behavior.
 */
(function () {
    "use strict";

    // ── Guard against double-init ──
    if (window.__SYSNET_NAV_INIT) return;
    window.__SYSNET_NAV_INIT = true;

    var STORAGE_KEY = "sysnet.sidebar.collapsed";
    var SCROLL_KEY  = "sysnet.scroll.";
    var FILTER_KEY  = "sysnet.filters.";

    function getSidebar()  { return document.getElementById("app-sidebar"); }
    function getOverlay()  { return document.getElementById("sidebarOverlay"); }
    function isMobile()    { return window.innerWidth <= 768; }

    // ═════════════════════════════════════════
    //  SIDEBAR STATE MANAGEMENT
    // ═════════════════════════════════════════

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
            // Show overlay on mobile when sidebar opens
            if (isMobile() && overlay) {
                overlay.classList.add("active");
            }
        }

        // Only persist desktop state
        if (!isMobile()) {
            localStorage.setItem(STORAGE_KEY, isCollapsed ? "1" : "0");
        }

        // Trigger resize for any charts/tables that need reflow
        try { window.dispatchEvent(new Event("resize")); } catch (e) {}
    }

    function initSidebarState() {
        var sidebar = getSidebar();
        if (!sidebar) return;

        if (isMobile()) {
            // Mobile: sidebar starts collapsed (hidden)
            setCollapsed(true);
        } else {
            // Desktop: restore saved state
            var saved = localStorage.getItem(STORAGE_KEY);
            setCollapsed(saved === "1");
        }
    }

    // ═════════════════════════════════════════
    //  BREADCRUMBS WITH BACK BUTTON
    // ═════════════════════════════════════════

    function initBreadcrumbs() {
        var breadcrumbs = document.getElementById("breadcrumbs");
        if (!breadcrumbs) return;

        var html = "";

        // ── Back Button ──
        if (window.history.length > 1) {
            html += '<button class="breadcrumb-back-btn" onclick="window.history.back()" title="Go back">';
            html += '<i class="fas fa-arrow-left"></i>';
            html += '</button>';
        }

        // ── Home Link ──
        html += '<a href="/" class="breadcrumb-link">Home</a>';

        var path = window.location.pathname.toLowerCase();
        
        // ── Dashboard Logic ──
        if (path.indexOf("computersummary/deshboad") !== -1) {
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

        // ── Standard Logic ──
        var segments = window.location.pathname.split("/").filter(Boolean);
        if (segments.length > 0) {
            var lastSegment = segments[segments.length - 1];
            if (lastSegment.toLowerCase() === "index") {
                lastSegment = segments[segments.length - 2] || "Home";
            }
            
            var label = decodeURIComponent(lastSegment).split("-").join(" ").split("_").join(" ");
            var capitalizedLabel = label.charAt(0).toUpperCase() + label.slice(1);
            
            html += ' <i class="fas fa-chevron-right breadcrumb-separator"></i> ';
            html += '<span class="breadcrumb-current">' + capitalizedLabel + '</span>';
        }

        breadcrumbs.innerHTML = html;
    }

    // ═════════════════════════════════════════
    //  MENU HANDLING (Event Delegation)
    // ═════════════════════════════════════════

    function initMenus() {
        // Single delegated handler for group triggers
        document.addEventListener("click", function (e) {
            var trigger = e.target.closest(".group-trigger");
            if (!trigger) return;

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

        // Auto-expand active group on page load
        var activeSubLink = document.querySelector(".sidebar-sub-link.active");
        if (activeSubLink) {
            var parentGroup = activeSubLink.closest(".nav-group");
            if (parentGroup) parentGroup.classList.add("open");
        }
    }

    // ═════════════════════════════════════════
    //  SIDEBAR TOGGLE (Event Delegation)
    // ═════════════════════════════════════════

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

        // Overlay click → close sidebar
        var overlay = getOverlay();
        if (overlay) {
            overlay.addEventListener("click", function () {
                setCollapsed(true);
            });
        }

        // Handle window resize (mobile ↔ desktop transitions)
        var resizeTimer;
        window.addEventListener("resize", function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                var sidebar = getSidebar();
                var overlayEl = getOverlay();
                if (!sidebar) return;

                if (isMobile()) {
                    // If transitioning to mobile and sidebar is open, keep overlay in sync
                    if (!sidebar.classList.contains("collapsed") && overlayEl) {
                        overlayEl.classList.add("active");
                    }
                } else {
                    // Desktop: remove overlay
                    if (overlayEl) overlayEl.classList.remove("active");
                }
            }, 150);
        });
    }

    // ═════════════════════════════════════════
    //  STATE PERSISTENCE
    // ═════════════════════════════════════════

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
                    // Small delay to let content render first
                    setTimeout(function () { mainContent.scrollTop = pos; }, 100);
                }
            }
        } catch (e) {}
    }

    function initStatePersistence() {
        // Save scroll position before navigating away
        window.addEventListener("beforeunload", saveScrollPosition);

        // Save on any internal link click
        document.addEventListener("click", function (e) {
            var link = e.target.closest("a[href]");
            if (link && link.hostname === window.location.hostname) {
                saveScrollPosition();
            }
        });

        // Restore scroll position on load
        restoreScrollPosition();

        // Expose filter save/restore utilities globally
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

    // ═════════════════════════════════════════
    //  LIFECYCLE
    // ═════════════════════════════════════════

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
