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
            isOpen ? close(panel, btn) : open(panel, btn);
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
    };
})();
