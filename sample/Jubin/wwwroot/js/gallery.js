// A stand-in for a third-party widget: an imperative init/destroy API that knows nothing
// about Unpoly. This is the shape that breaks under fragment swaps, which is the point.
//
// It keeps a module-level registry so a test can prove the destructor actually ran. A real
// library would not expose that; the leak would just be invisible.
window.Gallery = (function () {
    const live = new Set();

    function init(element, options) {
        const slides = Array.from(element.querySelectorAll('[data-slide]'));
        if (!slides.length) return { destroy() {} };

        let index = 0;
        slides.forEach((s, i) => s.hidden = i !== 0);

        // A global effect: the timer outlives the element unless something stops it.
        // Recorded so a check can read the value the compiler was handed, instead of
        // inferring it from how fast slides move -- a timing-based assertion tests the clock.
        const interval = options?.interval ?? 1500;
        element.dataset.galleryInterval = String(interval);

        const timer = setInterval(() => {
            slides[index].hidden = true;
            index = (index + 1) % slides.length;
            slides[index].hidden = false;
            element.dataset.slideIndex = String(index);
        }, interval);

        const instance = {
            destroy() {
                clearInterval(timer);
                live.delete(instance);
                element.dataset.galleryLive = 'false';
            }
        };

        live.add(instance);
        element.dataset.galleryLive = 'true';
        element.dataset.slideIndex = '0';
        return instance;
    }

    return { init, liveCount: () => live.size };
})();
