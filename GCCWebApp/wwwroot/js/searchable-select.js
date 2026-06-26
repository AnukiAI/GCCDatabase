/**
 * Searchable Select - Converts a <select> into a type-ahead searchable dropdown.
 * Usage: new SearchableSelect('#countrySelect', { placeholder: 'Search countries...' });
 *
 * Supports dynamic option refresh:
 *   var ss = new SearchableSelect('#ProductId', { placeholder: 'Search products...' });
 *   // After options change on the underlying <select>:
 *   ss.refresh();
 */
(function () {
    'use strict';

    window.SearchableSelect = function (selectSelector, options) {
        var select = document.querySelector(selectSelector);
        if (!select) return;

        options = options || {};
        var placeholder = options.placeholder || 'Type to search...';
        var noResultsText = options.noResultsText || 'No matches found';

        var allOptions = [];
        var activeIndex = -1;

        // Store reference on the select element for external access
        select._searchableSelect = this;

        function loadOptions() {
            allOptions = [];
            for (var i = 0; i < select.options.length; i++) {
                allOptions.push({
                    value: select.options[i].value,
                    text: select.options[i].text,
                    selected: select.options[i].selected
                });
            }
        }

        loadOptions();

        // Hide original select
        select.style.display = 'none';

        // Create wrapper
        var wrapper = document.createElement('div');
        wrapper.className = 'searchable-select-wrapper';
        wrapper.style.position = 'relative';
        select.parentNode.insertBefore(wrapper, select.nextSibling);

        // Create text input
        var input = document.createElement('input');
        input.type = 'text';
        input.className = 'form-control';
        input.placeholder = placeholder;
        input.setAttribute('autocomplete', 'off');
        input.setAttribute('role', 'combobox');
        input.setAttribute('aria-expanded', 'false');
        input.setAttribute('aria-haspopup', 'listbox');
        input.setAttribute('aria-label', select.getAttribute('aria-label') || placeholder);

        // Store reference on the original select for external code that needs to clear/set the input
        select.autocompleteInput = input;

        // Set initial value if something is selected
        var currentVal = select.value;
        if (currentVal) {
            for (var j = 0; j < allOptions.length; j++) {
                if (allOptions[j].value === currentVal) {
                    input.value = allOptions[j].text;
                    break;
                }
            }
        }

        wrapper.appendChild(input);

        // Create dropdown list
        var dropdown = document.createElement('div');
        dropdown.className = 'searchable-select-dropdown';
        dropdown.setAttribute('role', 'listbox');
        dropdown.style.cssText = 'display:none; position:absolute; z-index:1050; width:100%; max-height:250px; overflow-y:auto; background:#fff; border:1px solid #ced4da; border-top:none; border-radius:0 0 4px 4px; box-shadow:0 4px 8px rgba(0,0,0,0.1);';
        wrapper.appendChild(dropdown);

        function renderOptions(filter) {
            dropdown.innerHTML = '';
            activeIndex = -1;
            var filterLower = (filter || '').toLowerCase();
            var count = 0;

            for (var k = 0; k < allOptions.length; k++) {
                var opt = allOptions[k];
                if (!opt.value) continue; // skip placeholder option
                if (filterLower && opt.text.toLowerCase().indexOf(filterLower) === -1) continue;

                var item = document.createElement('div');
                item.className = 'searchable-select-item';
                item.setAttribute('role', 'option');
                item.setAttribute('data-value', opt.value);
                item.textContent = opt.text;
                item.style.cssText = 'padding:8px 12px; cursor:pointer; font-size:14px;';

                if (opt.value === select.value) {
                    item.style.backgroundColor = '#e9ecef';
                    item.style.fontWeight = '600';
                }

                item.addEventListener('mouseenter', function () {
                    clearActive();
                    this.style.backgroundColor = '#0d6efd';
                    this.style.color = '#fff';
                });
                item.addEventListener('mouseleave', function () {
                    this.style.backgroundColor = this.getAttribute('data-value') === select.value ? '#e9ecef' : '';
                    this.style.color = '';
                    this.style.fontWeight = this.getAttribute('data-value') === select.value ? '600' : '';
                });

                (function (itm, val, txt) {
                    itm.addEventListener('click', function (e) {
                        e.preventDefault();
                        e.stopPropagation();
                        selectOption(val, txt);
                    });
                })(item, opt.value, opt.text);

                dropdown.appendChild(item);
                count++;
            }

            if (count === 0) {
                var noResult = document.createElement('div');
                noResult.style.cssText = 'padding:8px 12px; color:#6c757d; font-style:italic; font-size:14px;';
                noResult.textContent = noResultsText;
                dropdown.appendChild(noResult);
            }
        }

        function selectOption(value, text) {
            select.value = value;
            input.value = text;
            hideDropdown();
            // Trigger change event on the original select
            var event = new Event('change', { bubbles: true });
            select.dispatchEvent(event);
        }

        // Commit the typed text to the underlying <select> when it unambiguously
        // identifies an option: an exact (case-insensitive) text match, or — when the
        // typed filter narrows the list to exactly one option — that lone option.
        // Returns true if a value was committed.
        //
        // Without this, the value only committed on a mouse click of a dropdown row;
        // typing a type name then pressing Enter/Tab or clicking away left the <select>
        // empty, which posted an empty value for [Required] fields (e.g. Certification
        // Type → ".NET: The value '' is invalid").
        function commitTypedValue() {
            var typed = (input.value || '').trim();
            if (!typed) { return false; }
            var typedLower = typed.toLowerCase();
            // 1) exact text match
            for (var a = 0; a < allOptions.length; a++) {
                if (allOptions[a].value && allOptions[a].text.toLowerCase() === typedLower) {
                    selectOption(allOptions[a].value, allOptions[a].text);
                    return true;
                }
            }
            // 2) unique partial match — the filter resolves to exactly one real option
            var matches = allOptions.filter(function (o) {
                return o.value && o.text.toLowerCase().indexOf(typedLower) !== -1;
            });
            if (matches.length === 1) {
                selectOption(matches[0].value, matches[0].text);
                return true;
            }
            return false;
        }

        function showDropdown() {
            dropdown.style.display = 'block';
            input.setAttribute('aria-expanded', 'true');
        }

        function hideDropdown() {
            dropdown.style.display = 'none';
            input.setAttribute('aria-expanded', 'false');
            activeIndex = -1;
        }

        function clearActive() {
            var items = dropdown.querySelectorAll('.searchable-select-item');
            for (var m = 0; m < items.length; m++) {
                items[m].style.backgroundColor = items[m].getAttribute('data-value') === select.value ? '#e9ecef' : '';
                items[m].style.color = '';
                items[m].style.fontWeight = items[m].getAttribute('data-value') === select.value ? '600' : '';
            }
        }

        function setActiveByIndex(idx) {
            var items = dropdown.querySelectorAll('.searchable-select-item');
            if (items.length === 0) return;
            clearActive();
            if (idx < 0) idx = items.length - 1;
            if (idx >= items.length) idx = 0;
            activeIndex = idx;
            items[activeIndex].style.backgroundColor = '#0d6efd';
            items[activeIndex].style.color = '#fff';
            items[activeIndex].scrollIntoView({ block: 'nearest' });
        }

        // Input events
        input.addEventListener('input', function () {
            renderOptions(this.value);
            showDropdown();

            // If input is cleared, clear the select value
            if (!this.value.trim()) {
                select.value = '';
            }
        });

        input.addEventListener('focus', function () {
            // Show the full option list on focus — typing re-filters via the
            // 'input' handler below. Filtering by the current input value here
            // collapses the dropdown to only the already-selected item, which
            // breaks the "click to see my other choices" gesture.
            renderOptions('');
            showDropdown();
            this.select(); // text stays highlighted so typing replaces cleanly
        });

        input.addEventListener('keydown', function (e) {
            var items = dropdown.querySelectorAll('.searchable-select-item');
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                if (dropdown.style.display === 'none') {
                    renderOptions(this.value);
                    showDropdown();
                }
                setActiveByIndex(activeIndex + 1);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                setActiveByIndex(activeIndex - 1);
            } else if (e.key === 'Enter') {
                e.preventDefault();
                if (activeIndex >= 0 && activeIndex < items.length) {
                    var sel = items[activeIndex];
                    selectOption(sel.getAttribute('data-value'), sel.textContent);
                } else {
                    // No row arrow-highlighted: commit the typed text if it resolves
                    // to a single option (the common type-then-Enter gesture).
                    commitTypedValue();
                    hideDropdown();
                }
            } else if (e.key === 'Escape') {
                hideDropdown();
                // Restore to current select value
                if (select.value) {
                    for (var n = 0; n < allOptions.length; n++) {
                        if (allOptions[n].value === select.value) {
                            input.value = allOptions[n].text;
                            break;
                        }
                    }
                } else {
                    input.value = '';
                }
            } else if (e.key === 'Tab') {
                // Commit the typed match before focus leaves (so Tab doesn't discard it).
                commitTypedValue();
                hideDropdown();
            }
        });

        // Close dropdown on outside click
        document.addEventListener('click', function (e) {
            if (!wrapper.contains(e.target)) {
                // Clicking away commits an unambiguous typed match rather than
                // silently discarding it (which posted an empty value).
                if (!select.value && input.value && input.value.trim()) {
                    commitTypedValue();
                }
                hideDropdown();
                // Restore display to match select value
                if (select.value) {
                    for (var p = 0; p < allOptions.length; p++) {
                        if (allOptions[p].value === select.value) {
                            input.value = allOptions[p].text;
                            break;
                        }
                    }
                } else {
                    input.value = '';
                }
            }
        });

        /**
         * Refresh options from the underlying <select> element.
         * Call after dynamically adding/removing <option> elements.
         * Optionally pass preselectValue to auto-select and display that option.
         */
        this.refresh = function (preselectValue) {
            loadOptions();
            if (preselectValue) {
                select.value = preselectValue;
                for (var r = 0; r < allOptions.length; r++) {
                    if (allOptions[r].value === String(preselectValue)) {
                        input.value = allOptions[r].text;
                        break;
                    }
                }
            } else if (select.value) {
                // Keep current selection display in sync
                for (var s = 0; s < allOptions.length; s++) {
                    if (allOptions[s].value === select.value) {
                        input.value = allOptions[s].text;
                        break;
                    }
                }
            } else {
                input.value = '';
            }
        };

        /** Clear the input and select value */
        this.clear = function () {
            select.value = '';
            input.value = '';
        };
    };
})();
