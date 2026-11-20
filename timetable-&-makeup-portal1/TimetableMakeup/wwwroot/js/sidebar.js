// Helper functions for responsive sidebar behavior (desktop collapse & mobile off-canvas)
window.portalSidebar = {
    isMobileView: function () {
        return window.innerWidth < 992;
    },

    // Closes mobile drawer automatically when resizing up to desktop
    registerResizeListener: function (dotNetHelper) {
        if (this._resizeHandler) {
            window.removeEventListener('resize', this._resizeHandler);
        }

        this._resizeHandler = function () {
            if (window.innerWidth >= 992 && dotNetHelper) {
                dotNetHelper.invokeMethodAsync('CloseMobileSidebar');
            }
        };

        window.addEventListener('resize', this._resizeHandler);
    }
};
