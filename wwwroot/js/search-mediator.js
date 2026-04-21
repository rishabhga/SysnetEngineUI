/**
 * Search Mediator
 * Centralizes search events from the navigation bar to individual page content.
 */

window.SearchMediator = (function() {
    const init = () => {
        const $searchInput = $('#globalSearchInput');
        if (!$searchInput.length) return;

        let debounceTimer;
        $searchInput.on('input', function() {
            clearTimeout(debounceTimer);
            const term = $(this).val();
            
            debounceTimer = setTimeout(() => {
                // Dispatch a custom event that any page can listen to
                $(document).trigger('searchEvent', [term]);
                
                // Also support a legacy global function pattern if needed
                if (typeof window.onGlobalSearch === 'function') {
                    window.onGlobalSearch(term);
                }
                
        console.log('Search dispatched:', term);
            }, 250);
        });

        // Clear search on ESC
        $searchInput.on('keydown', function(e) {
            if (e.key === 'Escape') {
                $(this).val('').trigger('input');
            }
        });
    };

    const updateNavLogo = (logoUrl, companyName) => {
        const $logo = $('#globalHeaderLogo');
        if (!$logo.length) return;

        const defaultLogo = '/images/footer-logo.png';
        const finalUrl = logoUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(companyName || 'C')}&background=00BCEB&color=fff&bold=true`;
        
        $logo.fadeOut(200, function() {
            $(this).attr('src', finalUrl).fadeIn(200);
        });
    };

    const resetNavLogo = () => {
        const $logo = $('#globalHeaderLogo');
        if (!$logo.length) return;

        const defaultLogo = '/images/footer-logo.png';
        $logo.fadeOut(200, function() {
            $(this).attr('src', defaultLogo).fadeIn(200);
        });
    };

    return { init, updateNavLogo, resetNavLogo };
})();

$(document).ready(() => SearchMediator.init());
