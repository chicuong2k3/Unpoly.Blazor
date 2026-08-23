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

// Called by [up-on-accepted] on the "add an option" subinteraction. A named function again:
// these attributes hold ONE expression, so a multi-statement body silently does nothing.
// 📖 https://unpoly.com/subinteractions (Adding options to an existing select)
window.addCollectionOption = function (value) {
    const select = document.querySelector('.collection-select');
    if (!select || select.querySelector(`option[value="${value.slug}"]`)) return;

    const option = new Option(value.name, value.slug, true, true);
    select.add(option);
};

// ---------------------------------------------------------------- destructors
// Three ways to clean up, all of which must run when the element is destroyed.
// 📖 https://unpoly.com/enhancing-elements (Alternative ways to register destructors)
window.__destroyed = [];

up.compiler('.probe-destructor', function (element) {
    const kind = element.dataset.kind;
    const note = () => window.__destroyed.push(kind);

    // Returning an array registers several destructors at once.
    if (kind === 'array') return [note, () => {}];

    // up.destructor() registers one without returning it, which suits code that decides
    // to clean up somewhere other than the compiler's return statement.
    if (kind === 'register') { up.destructor(element, note); return; }

    return note;
});

// ---------------------------------------------------------------- data
// Every shape of data arrives in the same second argument: data-* attributes, [up-data],
// and any other attribute read off the element itself.
// 📖 https://unpoly.com/data
up.compiler('.probe-data', function (element, data) {
    console.log('[probe-data]', {
        fromDataAttributes: { role: element.dataset.role, count: element.dataset.count },
        fromUpData: data,
        arbitraryAttribute: element.getAttribute('up-data'),
    });
    element.title = JSON.stringify(data);
});

// ---------------------------------------------------------------- render pass
// A compiler can ask about the pass that inserted it -- whether this is a revalidation,
// for instance, which is otherwise invisible.
// 📖 https://unpoly.com/enhancing-elements (Accessing information about the render pass)
up.compiler('[data-gallery]', function (element, data, meta) {
    element.dataset.revalidating = String(!!meta?.revalidating);
    element.dataset.layerMode = meta?.layer?.mode ?? 'root';
});

// ---------------------------------------------------------------- assets
// There is no default behaviour: the app decides what a new version means.
// 📖 https://unpoly.com/handling-asset-changes
up.on('up:assets:changed', function (event) {
    window.__assetsChanged = (window.__assetsChanged ?? 0) + 1;

    const bar = document.createElement('p');
    bar.className = 'flash assets-changed';
    bar.textContent = 'Có phiên bản mới — tải lại khi thuận tiện.';
    document.querySelector('[up-flashes]')?.appendChild(bar);

    // "Reloading at the next opportunity" is deliberately NOT wired up automatically here.
    // Hijacking the next up:link:follow turns every subsequent navigation into a full page
    // load, which broke overlays across the whole sample. The button on the flash is the
    // opt-in version; a real app decides its own moment.
    bar.append(' ');
    const reload = document.createElement('button');
    reload.type = 'button';
    reload.className = 'btn-quiet';
    reload.textContent = 'Tải lại ngay';
    reload.onclick = () => location.reload();
    bar.appendChild(reload);
});

// ---------------------------------------------------------------- fragment hooks
// Preventing a render pass, and inspecting a loaded response before it is used.
// 📖 https://unpoly.com/render-lifecycle
up.on('up:fragment:loaded', function (event) {
    window.__lastStatus = event.response.status;

    // A response can be refused entirely -- the server said 418 and we want none of it.
    if (event.response.header('X-Lab-Refuse')) event.preventDefault();
});

up.on('up:fragment:inserted', function (event) {
    window.__inserted = (window.__inserted ?? 0) + 1;
});

// ---------------------------------------------------------------- motion
// A custom transition: two animations run together, old element out, new element in.
// 📖 https://unpoly.com/predefined-transitions (Custom transitions)
up.transition('lab-slide', function (oldElement, newElement, options) {
    return Promise.all([
        up.animate(oldElement, { opacity: 1, transform: 'translateX(0)' },
                                { ...options, to: { opacity: 0, transform: 'translateX(-40px)' } }),
        up.animate(newElement, { opacity: 0, transform: 'translateX(40px)' },
                                { ...options, to: { opacity: 1, transform: 'translateX(0)' } }),
    ]);
});

// ---------------------------------------------------------------- events
// The client-side twin of the server's UpEmit: same event bus, either end can raise one.
up.on('lab:pinged', function (event) {
    window.__pinged = (window.__pinged ?? 0) + 1;
    console.log('[lab:pinged]', event.from ?? event.detail);
});

// ---------------------------------------------------------------- previews
// A preview mutates the DOM immediately, BEFORE the server answers, and Unpoly reverts it
// when the response arrives. The server plays no part in any of this.
// 📖 https://unpoly.com/previews
up.preview('lab-skeleton', function (preview) {
    window.__previewRan = (window.__previewRan ?? 0) + 1;

    // Prefer additive changes: they revert cleanly whatever the outcome.
    preview.insert(preview.fragment, 'beforeend',
        '<span class="meta lab-skeleton"> đang tải…</span>');
    preview.addClass(preview.fragment, 'is-previewing');
});

// Preview parameters, and several previews chained on one element.
up.preview('lab-dim', function (preview, { amount }) {
    preview.setStyle(preview.fragment, { opacity: String(amount ?? 0.4) });
});

// ---------------------------------------------------------------- optimistic rendering
// Render the expected result at once, then let the server confirm or replace it. The
// template is embedded in the response so the client can clone it without a request.
// 📖 https://unpoly.com/optimistic-rendering
up.preview('lab-optimistic', function (preview) {
    const template = document.querySelector('#optimistic-row');
    if (!template) return;

    const row = up.element.createFromHTML(template.innerHTML.trim());
    row.classList.add('is-optimistic');
    preview.insert(document.querySelector('.picked-list'), 'beforeend', row);
});

// ---------------------------------------------------------------- events, framework, log
// up.event.build() makes an event without emitting it; halt() stops one dead.
window.labBuildEvent = function () {
    const event = up.event.build('lab:pinged', { from: 'build' });
    up.emit(event);
    return event.type;
};

up.on('lab:halt-me', function (event) { up.event.halt(event); window.__halted = true; });

// up:framework:booted fires once, after Unpoly has started.
up.on('up:framework:booted', function () { window.__booted = true; });

// Logging is configurable, not just on or off.
window.labLogConfig = function () {
    up.log.enable();
    return JSON.stringify(up.log.config);
};
