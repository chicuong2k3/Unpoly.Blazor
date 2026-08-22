// Application scripts live in <head> with [defer], never in <body>.
//
// A script in the body re-executes every time that region is swapped: the function is
// redefined on every render, and anything with a side effect runs again.
// 📖 https://unpoly.com/legacy-scripts

// Compilers run once per matching element -- on page load AND for every fragment inserted
// later. That is the whole reason they exist instead of DOMContentLoaded.
// 📖 https://unpoly.com/enhancing-elements
up.compiler('[data-gallery]', function (element, data) {
    const instance = window.Gallery.init(element, data);

    // The timer is a GLOBAL effect: it outlives the element. Returning a destructor is how
    // Unpoly is told to stop it when the fragment is swapped away. Without this, every swap
    // leaves another timer running against detached DOM.
    // 📖 https://unpoly.com/enhancing-elements (Cleaning up after yourself)
    return () => instance.destroy();
});

// Moved out of ProductDetail's body for the reason above. Called by [up-on-accepted],
// which is evaluated as a single expression -- hence a named function.
window.onSizeChosen = function (value) {
    document.querySelector('.chosen-size').textContent = value.size;
    document.querySelector('.add-to-cart').disabled = false;
};
