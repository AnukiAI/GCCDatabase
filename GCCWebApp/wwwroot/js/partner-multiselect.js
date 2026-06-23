/**
 * Partner(s) multi-select dropdown.
 *
 * Replaces the native `<select multiple size="4">` (Ctrl-click) Partner(s) control on the
 * Certificate Create/Edit forms with a dropdown that looks like the Company dropdown
 * (createCustomDropdown in certificate-inline-create.js) but:
 *   - uses CHECKBOXES (multi-select) instead of the single-select radio circles, and
 *   - has NO "Add New" footer — partners cannot be created from the cert form.
 *
 * It does NOT change the data contract: the underlying `<select#partnerSelect>` keeps the
 * authoritative selected state, and toggling an option dispatches a `change` event so the
 * existing CR-171 inline sync continues to mirror the selection into the hidden
 * `PartnerIds` CSV that the POST handler consumes. Edit pre-selection (server-rendered
 * `selected` options) is honored automatically.
 */
(function () {
    'use strict';

    function initPartnerMultiselect(selectId, hiddenId) {
        const select = document.getElementById(selectId);
        if (!select || select.dataset.checkboxDropdownInit) return;
        select.dataset.checkboxDropdownInit = 'true';

        const formGroup = select.closest('.form-group');
        if (!formGroup) return;

        // --- Wrapper ---
        const wrapper = document.createElement('div');
        wrapper.className = 'partner-multiselect-wrapper';
        wrapper.style.cssText = 'position: relative; width: 100%;';

        // --- Trigger (shows selected names / placeholder) ---
        const trigger = document.createElement('div');
        trigger.className = 'partner-multiselect-trigger';
        trigger.setAttribute('role', 'button');
        trigger.setAttribute('tabindex', '0');
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-expanded', 'false');
        trigger.setAttribute('aria-controls', selectId + '-partner-panel');
        trigger.setAttribute('aria-label', 'Partner(s)');
        trigger.style.cssText = `
            display: flex; justify-content: space-between; align-items: center;
            padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px;
            background: white; cursor: pointer; font-size: 14px; min-height: 40px;
            transition: border-color 0.2s; gap: 10px;
        `;
        trigger.innerHTML = `
            <span class="partner-multiselect-selected" style="color: #6c757d; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">Select partner(s)...</span>
            <i class="fas fa-chevron-down" style="color: #6c757d; font-size: 12px; transition: transform 0.2s; flex-shrink: 0;"></i>
        `;

        // --- Panel ---
        const panel = document.createElement('div');
        panel.className = 'partner-multiselect-panel';
        panel.id = selectId + '-partner-panel';
        panel.setAttribute('role', 'listbox');
        panel.setAttribute('aria-multiselectable', 'true');
        panel.style.cssText = `
            position: absolute; top: 100%; left: 0; right: 0; z-index: 1000;
            background: white; border: 1px solid #ced4da; border-top: none;
            border-radius: 0 0 4px 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            display: none; max-height: 350px;
        `;

        // --- Search ---
        const searchContainer = document.createElement('div');
        searchContainer.style.cssText = 'padding: 8px 12px; border-bottom: 1px solid #e9ecef;';
        const searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.className = 'partner-multiselect-search';
        searchInput.placeholder = 'Search partners...';
        searchInput.setAttribute('autocomplete', 'off');
        searchInput.setAttribute('aria-label', 'Search partners');
        searchInput.style.cssText = `
            width: 100%; padding: 8px 12px; border: 1px solid #ced4da; border-radius: 4px;
            font-size: 14px; outline: none;
        `;
        searchInput.onfocus = () => (searchInput.style.borderColor = '#92C83E');
        searchInput.onblur = () => (searchInput.style.borderColor = '#ced4da');
        searchInput.onclick = (e) => e.stopPropagation();
        searchInput.addEventListener('input', () => buildOptions(searchInput.value));
        // Escape from the search box closes the panel and returns focus to the trigger
        // (ARIA listbox pattern — focus must not be lost when the panel collapses).
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();
                closeDropdown();
                trigger.focus();
            }
        });
        searchContainer.appendChild(searchInput);

        // --- Options (scrollable) ---
        const optionsContainer = document.createElement('div');
        optionsContainer.className = 'partner-multiselect-options';
        optionsContainer.style.cssText = 'max-height: 280px; overflow-y: auto; padding: 8px 0;';

        function buildOptions(filter) {
            optionsContainer.innerHTML = '';
            const filterLower = (filter || '').toLowerCase();
            let matchCount = 0;

            Array.from(select.options).forEach((opt) => {
                if (!opt.value) return; // skip any placeholder
                if (filterLower && opt.textContent.toLowerCase().indexOf(filterLower) === -1) return;

                matchCount++;
                const row = document.createElement('div');
                row.className = 'partner-multiselect-option';
                row.setAttribute('role', 'option');
                row.setAttribute('aria-selected', opt.selected ? 'true' : 'false');
                row.dataset.value = opt.value;
                row.style.cssText = `
                    display: flex; align-items: center; gap: 10px;
                    padding: 10px 16px; cursor: pointer; transition: background 0.15s;
                `;

                const checked = opt.selected;
                row.innerHTML = `
                    <span class="partner-multiselect-box" style="width: 18px; height: 18px; border: 2px solid ${checked ? '#92C83E' : '#ced4da'};
                        background: ${checked ? '#92C83E' : 'transparent'}; border-radius: 4px; display: flex;
                        align-items: center; justify-content: center; flex-shrink: 0;">
                        ${checked ? '<i class="fas fa-check" style="color: #fff; font-size: 11px;"></i>' : ''}
                    </span>
                    <span style="color: #333;">${opt.textContent}</span>
                `;

                row.onmouseenter = () => (row.style.backgroundColor = '#f8f9fa');
                row.onmouseleave = () => (row.style.backgroundColor = 'transparent');
                row.onclick = (e) => {
                    e.stopPropagation();
                    toggleOption(opt);
                };

                optionsContainer.appendChild(row);
            });

            if (matchCount === 0) {
                const noResult = document.createElement('div');
                noResult.setAttribute('role', 'option');
                noResult.setAttribute('aria-selected', 'false');
                noResult.setAttribute('aria-disabled', 'true');
                noResult.style.cssText = 'padding: 10px 16px; color: #6c757d; font-style: italic; font-size: 14px;';
                noResult.textContent = filterLower ? `No partner found matching "${filter}"` : 'No partners available';
                optionsContainer.appendChild(noResult);
            }
        }

        // Toggle a single option; keep the panel OPEN (multi-select) and notify listeners.
        function toggleOption(opt) {
            opt.selected = !opt.selected;
            // Fire change so the CR-171 inline sync updates the hidden PartnerIds CSV.
            select.dispatchEvent(new Event('change', { bubbles: true }));
            updateTrigger();
            buildOptions(searchInput.value); // re-render to flip the checkbox
        }

        function updateTrigger() {
            const names = Array.from(select.selectedOptions).map((o) => o.textContent);
            const span = trigger.querySelector('.partner-multiselect-selected');
            if (names.length > 0) {
                span.textContent = names.join(', ');
                span.style.color = '#333';
                span.title = names.join(', ');
                trigger.setAttribute('aria-label', 'Partner(s): ' + names.join(', '));
            } else {
                span.textContent = 'Select partner(s)...';
                span.style.color = '#6c757d';
                span.title = '';
                trigger.setAttribute('aria-label', 'Partner(s)');
            }
        }

        // Assemble panel
        panel.appendChild(searchContainer);
        panel.appendChild(optionsContainer);

        // Hide the native select but keep it for state (and for the inline sync to read).
        select.style.position = 'absolute';
        select.style.opacity = '0';
        select.style.height = '0';
        select.style.width = '0';
        select.style.overflow = 'hidden';
        select.style.pointerEvents = 'none';
        select.setAttribute('tabindex', '-1');
        select.setAttribute('aria-hidden', 'true');

        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(trigger);
        wrapper.appendChild(panel);
        wrapper.appendChild(select);

        // Open/close
        let isOpen = false;
        function openDropdown() {
            panel.style.display = 'block';
            trigger.style.borderColor = '#92C83E';
            trigger.style.borderRadius = '4px 4px 0 0';
            trigger.querySelector('i').style.transform = 'rotate(180deg)';
            trigger.setAttribute('aria-expanded', 'true');
            isOpen = true;
            searchInput.value = '';
            buildOptions();
            setTimeout(() => searchInput.focus(), 50);
        }
        function closeDropdown() {
            panel.style.display = 'none';
            trigger.style.borderColor = '#ced4da';
            trigger.style.borderRadius = '4px';
            trigger.querySelector('i').style.transform = 'rotate(0deg)';
            trigger.setAttribute('aria-expanded', 'false');
            isOpen = false;
        }
        trigger.onclick = (e) => {
            e.stopPropagation();
            isOpen ? closeDropdown() : openDropdown();
        };
        trigger.onkeydown = (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                isOpen ? closeDropdown() : openDropdown();
            } else if (e.key === 'Escape' && isOpen) {
                closeDropdown();
            }
        };

        // Close on outside click
        document.addEventListener('click', (e) => {
            if (!wrapper.contains(e.target)) closeDropdown();
        });

        // Initial paint
        updateTrigger();
    }

    window.initPartnerMultiselect = initPartnerMultiselect;

    function boot() {
        initPartnerMultiselect('partnerSelect', 'partnersHidden');
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();
