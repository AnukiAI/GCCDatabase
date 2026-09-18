/*
 * CR-053 (QC #267) --- paging inside the certificate preview popup.
 *
 * WHAT THIS EXISTS FOR
 * The client asked to "page through" a multi-page certificate from inside the preview popup.
 * CR-052 answered the literal request with a "View all pages" link that opens the PDF in a new
 * browser tab; the client came back and said she meant paging, in place. This is that.
 *
 * ONE FILE, FOUR VIEWS, DELIBERATELY.
 * Edit, CertificateInternalView, CertificateExternalView and CertificateSingleView all host
 * #imageZoomModal. CR-048's lesson was that four hand-copied blocks drift apart; the CR-052 rework
 * had to be applied four times for exactly that reason. So the markup is built here, once, at
 * runtime, and each view only has to load this script and expose the certificate Id.
 *
 * Create.cshtml is NOT in scope and cannot be: there is no saved certificate, no Id and no stored
 * PDF to render pages from. It keeps its blob-URL "All pages" link.
 *
 * TWO THINGS THAT WOULD OTHERWISE BE BUGS
 * 1. #imageZoomModal carries onclick="closeImageZoom()" on the CONTAINER, so any click inside it
 *    closes the popup. Every control here calls stopPropagation() or the popup shuts the instant
 *    you try to turn a page. (This is the same trap CR-052's rework hit with its link.)
 * 2. The image element is shared with the stored thumbnail. Page 0 must therefore restore the
 *    ORIGINAL src rather than request ?page=0, so reopening a preview costs nothing and no PDF is
 *    opened at all for the common case.
 */
(function () {
    'use strict';

    var MODAL_ID = 'imageZoomModal';
    var IMG_ID = 'zoomedImage';

    var state = {
        certId: null,
        pageCount: 0,
        current: 0,
        originalSrc: null,
        wired: false,
        countPromise: null
    };

    function el(id) { return document.getElementById(id); }

    function pageUrl(page) {
        return '/Certificate/ViewCertificateImage?id=' + encodeURIComponent(state.certId) +
               '&page=' + encodeURIComponent(page);
    }

    /* The pager only ever appears for a document we have actually confirmed is multi-page.
       0 pages means unreadable (encrypted - DEF-400 - or missing from disk) and 1 means there is
       nothing to page to. Both hide the control rather than showing a dead one. */
    function fetchPageCount() {
        if (state.countPromise) return state.countPromise;

        state.countPromise = fetch('/Certificate/CertificatePageCount?id=' + encodeURIComponent(state.certId), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' } // else a 401 arrives as login HTML at HTTP 200
        })
            .then(function (r) { return r.ok ? r.json() : { pages: 0 }; })
            .then(function (d) { return (d && typeof d.pages === 'number') ? d.pages : 0; })
            .catch(function () { return 0; });

        return state.countPromise;
    }

    function buildPager() {
        var modal = el(MODAL_ID);
        if (!modal || el('zoomPager')) return el('zoomPager');

        var bar = document.createElement('div');
        bar.id = 'zoomPager';
        bar.className = 'zoom-pager';
        bar.setAttribute('role', 'group');
        bar.setAttribute('aria-label', 'Certificate pages');
        bar.hidden = true;
        bar.innerHTML =
            '<button type="button" class="zoom-pager-btn" id="zoomPagerPrev" aria-label="Previous page">' +
            '<i class="fas fa-chevron-left" aria-hidden="true"></i></button>' +
            '<span class="zoom-pager-label" id="zoomPagerLabel" aria-live="polite"></span>' +
            '<button type="button" class="zoom-pager-btn" id="zoomPagerNext" aria-label="Next page">' +
            '<i class="fas fa-chevron-right" aria-hidden="true"></i></button>';

        /* Insert above the existing "View all pages" link so the pager reads as the primary control
           and the link as the way out to the full document. */
        var allPages = modal.querySelector('.zoom-allpages');
        if (allPages) modal.insertBefore(bar, allPages);
        else modal.appendChild(bar);

        bar.addEventListener('click', function (e) { e.stopPropagation(); });
        el('zoomPagerPrev').addEventListener('click', function (e) { e.stopPropagation(); go(state.current - 1); });
        el('zoomPagerNext').addEventListener('click', function (e) { e.stopPropagation(); go(state.current + 1); });

        return bar;
    }

    function render() {
        var bar = el('zoomPager');
        if (!bar) return;

        if (state.pageCount < 2) { bar.hidden = true; return; }

        bar.hidden = false;
        el('zoomPagerLabel').textContent = 'Page ' + (state.current + 1) + ' of ' + state.pageCount;
        el('zoomPagerPrev').disabled = state.current <= 0;
        el('zoomPagerNext').disabled = state.current >= state.pageCount - 1;
    }

    function go(page) {
        if (page < 0 || page >= state.pageCount || page === state.current) return;

        var img = el(IMG_ID);
        if (!img) return;

        state.current = page;
        img.classList.add('zoom-page-loading');

        /* Page 0 is the stored thumbnail: restore the src the view already rendered rather than
           asking the server to re-render page 0 from the PDF. */
        var next = (page === 0 && state.originalSrc) ? state.originalSrc : pageUrl(page);

        var probe = new Image();
        probe.onload = function () {
            img.src = next;
            img.classList.remove('zoom-page-loading');
        };
        probe.onerror = function () {
            /* A page that will not render (404 from the bounds check, or an unreadable PDF) must
               not leave a broken image behind. Stay where we were and stop offering that direction. */
            img.classList.remove('zoom-page-loading');
            state.pageCount = state.current;   // treat as the end of the document
            state.current = Math.max(0, state.current - 1);
            render();
        };
        probe.src = next;

        render();
    }

    function isOpen() {
        var modal = el(MODAL_ID);
        return !!modal && modal.style.display === 'block';
    }

    // DEF-434: never take Left/Right away from a control that needs them. This listener is bound to
    // `document`, so while the popup is open it was calling preventDefault() on EVERY arrow press —
    // including ones aimed at a form field behind the overlay. Reproduced on /Certificate/Edit: with
    // the caret at the end of a populated input, ArrowLeft left the caret where it was and turned
    // the popup's page instead. Edit is the only host view with form fields, so it was the only one
    // affected, but the fix belongs here rather than there.
    function isTextEntry(node) {
        if (!node) return false;
        var tag = (node.tagName || '').toUpperCase();
        return tag === 'INPUT' || tag === 'SELECT' || tag === 'TEXTAREA' || node.isContentEditable === true;
    }

    function onKey(e) {
        if (!isOpen() || state.pageCount < 2) return;
        if (isTextEntry(e.target)) return;

        if (e.key === 'ArrowRight') { e.preventDefault(); go(state.current + 1); }
        else if (e.key === 'ArrowLeft') { e.preventDefault(); go(state.current - 1); }
    }

    function onOpened() {
        var img = el(IMG_ID);
        state.originalSrc = (img && img.src) ? img.src : null;
        state.current = 0;

        buildPager();
        render();

        fetchPageCount().then(function (n) {
            state.pageCount = n;
            render();
        });
    }

    /* WHY AN OBSERVER AND NOT A WRAPPER AROUND openImageZoom().
       Each view declares `function openImageZoom(...)` in its own inline <script>. A function
       DECLARATION is hoisted and assigned when that script runs, so wrapping window.openImageZoom
       from here would work or be silently clobbered depending purely on whether this file loads
       before or after that block - a load-order dependency across four views, which is the kind of
       thing that works on three pages and quietly fails on the fourth. Watching the modal's own
       style attribute has no such dependency: whoever opens it, however they open it, we see it. */
    function watchModal() {
        if (state.wired) return;
        state.wired = true;

        var modal = el(MODAL_ID);
        var wasOpen = isOpen();

        new MutationObserver(function () {
            var open = isOpen();
            if (open && !wasOpen) onOpened();
            wasOpen = open;
        }).observe(modal, { attributes: true, attributeFilter: ['style'] });

        if (wasOpen) onOpened();

        document.addEventListener('keydown', onKey);
    }

    function init() {
        var modal = el(MODAL_ID);
        if (!modal) return;

        var idAttr = modal.getAttribute('data-certificate-id');
        var parsed = parseInt(idAttr, 10);
        if (!idAttr || isNaN(parsed) || parsed <= 0) return; // no Id -> no paging (Create, or no cert)

        state.certId = parsed;
        watchModal();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
