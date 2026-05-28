window.SearchMediator = (function() {
    const init = () => {
        const $searchInput = $('#globalSearchInput');
        if (!$searchInput.length) return;

        $searchInput.on('input', function() {
            const term = $(this).val();
            $(document).trigger('searchEvent', [term]);
            if (typeof window.onGlobalSearch === 'function') {
                window.onGlobalSearch(term);
            }
        });


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
