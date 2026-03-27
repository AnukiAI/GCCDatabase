/**
 * Accessibility Enhancements for GCC Database Solution
 * Provides additional ARIA support, keyboard navigation, and screen reader improvements
 * Version: 1.0.0
 * WCAG 2.1 Level AA Compliance
 */

(function() {
    'use strict';

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAccessibility);
    } else {
        initAccessibility();
    }

    function initAccessibility() {
        console.log('Accessibility enhancements initializing...');

        // Core enhancements
        enhanceFilterPanels();
        enhanceSortDropdown();
        enhanceModalFocusTrap();
        enhanceViewToggle();
        enhanceActionButtons();
        enhanceLoadingStates();

        console.log('Accessibility enhancements initialized');
    }

    /**
     * Enhance filter panels with proper ARIA expanded states
     */
    function enhanceFilterPanels() {
        const filterHeaders = document.querySelectorAll('.filter-header');

        filterHeaders.forEach(header => {
            // Set initial aria-expanded based on content visibility
            const contentId = header.getAttribute('aria-controls');
            if (contentId) {
                const content = document.getElementById(contentId);
                const isExpanded = content && !content.classList.contains('collapsed');
                header.setAttribute('aria-expanded', isExpanded);
            }

            // Update aria-expanded on click
            header.addEventListener('click', function() {
                const contentId = this.getAttribute('aria-controls');
                if (contentId) {
                    const content = document.getElementById(contentId);
                    if (content) {
                        const isNowExpanded = !content.classList.contains('collapsed');
                        this.setAttribute('aria-expanded', isNowExpanded);

                        // Announce to screen readers
                        announceToScreenReader(
                            `${this.textContent.trim()} filter ${isNowExpanded ? 'expanded' : 'collapsed'}`
                        );
                    }
                }
            });

            // Add keyboard support
            header.addEventListener('keydown', function(e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    this.click();
                }
            });
        });
    }

    /**
     * Enhance sort dropdown with full keyboard navigation
     */
    function enhanceSortDropdown() {
        const dropdown = document.getElementById('sortDropdown');
        if (!dropdown) return;

        const button = dropdown.querySelector('.sort-dropdown-button');
        const content = dropdown.querySelector('.sort-dropdown-content');
        const options = content ? Array.from(content.querySelectorAll('.sort-option')) : [];

        if (!button || !content) return;

        let focusedIndex = -1;

        // Button click handler
        button.addEventListener('click', function() {
            const isExpanded = this.getAttribute('aria-expanded') === 'true';
            this.setAttribute('aria-expanded', !isExpanded);
            content.style.display = isExpanded ? 'none' : 'block';

            if (!isExpanded && options.length > 0) {
                // Focus first option when opening
                focusedIndex = 0;
                options[0].focus();
                announceToScreenReader('Sort options menu opened. Use arrow keys to navigate.');
            }
        });

        // Keyboard navigation in dropdown
        content.addEventListener('keydown', function(e) {
            switch(e.key) {
                case 'ArrowDown':
                    e.preventDefault();
                    focusedIndex = Math.min(focusedIndex + 1, options.length - 1);
                    options[focusedIndex].focus();
                    break;

                case 'ArrowUp':
                    e.preventDefault();
                    focusedIndex = Math.max(focusedIndex - 1, 0);
                    options[focusedIndex].focus();
                    break;

                case 'Home':
                    e.preventDefault();
                    focusedIndex = 0;
                    options[focusedIndex].focus();
                    break;

                case 'End':
                    e.preventDefault();
                    focusedIndex = options.length - 1;
                    options[focusedIndex].focus();
                    break;

                case 'Enter':
                case ' ':
                    e.preventDefault();
                    if (focusedIndex >= 0) {
                        options[focusedIndex].click();
                    }
                    break;

                case 'Escape':
                    e.preventDefault();
                    button.setAttribute('aria-expanded', 'false');
                    content.style.display = 'none';
                    button.focus();
                    announceToScreenReader('Sort menu closed');
                    break;
            }
        });

        // Make options focusable
        options.forEach((option, index) => {
            option.setAttribute('tabindex', '0');
            option.setAttribute('role', 'option');

            option.addEventListener('click', function() {
                const sortLabel = this.textContent.trim();
                button.setAttribute('aria-label', `Sort certificates. Current sort: ${sortLabel}`);
                announceToScreenReader(`Sorting by ${sortLabel}`);
            });
        });

        // Close on outside click
        document.addEventListener('click', function(e) {
            if (!dropdown.contains(e.target)) {
                button.setAttribute('aria-expanded', 'false');
                content.style.display = 'none';
            }
        });
    }

    /**
     * Enhance modals with proper focus trap
     */
    function enhanceModalFocusTrap() {
        // Listen for modal opens
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1 && node.classList &&
                        (node.classList.contains('admin-modal-overlay') || node.classList.contains('gcc-modal-overlay'))) {
                        setupFocusTrap(node);
                    }
                });
            });
        });

        observer.observe(document.body, { childList: true, subtree: true });

        // Also check for existing modals
        document.querySelectorAll('.admin-modal-overlay, .gcc-modal-overlay').forEach(setupFocusTrap);
    }

    function setupFocusTrap(modal) {
        const focusableElements = modal.querySelectorAll(
            'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"]):not([disabled])'
        );

        if (focusableElements.length === 0) return;

        const firstFocusable = focusableElements[0];
        const lastFocusable = focusableElements[focusableElements.length - 1];

        // Store original focus
        const previouslyFocused = document.activeElement;

        // Focus first element
        setTimeout(() => firstFocusable.focus(), 100);

        // Trap Tab key
        const trapFocus = function(e) {
            if (e.key === 'Tab') {
                if (e.shiftKey) {
                    // Shift+Tab
                    if (document.activeElement === firstFocusable) {
                        e.preventDefault();
                        lastFocusable.focus();
                    }
                } else {
                    // Tab
                    if (document.activeElement === lastFocusable) {
                        e.preventDefault();
                        firstFocusable.focus();
                    }
                }
            }
        };

        modal.addEventListener('keydown', trapFocus);

        // Store cleanup function
        modal._focusTrapCleanup = function() {
            modal.removeEventListener('keydown', trapFocus);
            if (previouslyFocused && previouslyFocused.focus) {
                previouslyFocused.focus();
            }
        };

        // Clean up when modal is removed
        const modalObserver = new MutationObserver(function(mutations) {
            if (!document.body.contains(modal)) {
                if (modal._focusTrapCleanup) {
                    modal._focusTrapCleanup();
                }
                modalObserver.disconnect();
            }
        });

        modalObserver.observe(document.body, { childList: true, subtree: true });
    }

    /**
     * Enhance view toggle with proper ARIA states
     */
    function enhanceViewToggle() {
        const viewButtons = document.querySelectorAll('.toggle-btn[data-view]');

        // Set initial aria-pressed states if not already set
        viewButtons.forEach(button => {
            if (!button.hasAttribute('aria-pressed')) {
                const isActive = button.classList.contains('active');
                button.setAttribute('aria-pressed', isActive);
            }
        });

        viewButtons.forEach(button => {
            button.addEventListener('click', function() {
                const view = this.getAttribute('data-view');

                // Update all buttons
                viewButtons.forEach(btn => {
                    const isPressed = btn === this;
                    btn.setAttribute('aria-pressed', isPressed);
                    btn.classList.toggle('active', isPressed);
                });

                // Announce change
                announceToScreenReader(`Switched to ${view} view`);
            });
        });
    }

    /**
     * Enhance action buttons (Edit/Delete) with better labels
     */
    function enhanceActionButtons() {
        // Edit buttons
        document.querySelectorAll('.action-btn.edit, button.edit').forEach(button => {
            if (!button.getAttribute('aria-label')) {
                const row = button.closest('tr, .grid-row');
                const itemName = getItemName(row);
                button.setAttribute('aria-label', `Edit ${itemName}`);
            }
        });

        // Delete buttons
        document.querySelectorAll('.action-btn.delete, button.delete').forEach(button => {
            if (!button.getAttribute('aria-label')) {
                const row = button.closest('tr, .grid-row');
                const itemName = getItemName(row);
                button.setAttribute('aria-label', `Delete ${itemName}`);
            }
        });

        // View buttons
        document.querySelectorAll('.action-btn.view, button.view').forEach(button => {
            if (!button.getAttribute('aria-label')) {
                const row = button.closest('tr, .grid-row');
                const itemName = getItemName(row);
                button.setAttribute('aria-label', `View details for ${itemName}`);
            }
        });
    }

    function getItemName(row) {
        if (!row) return 'item';

        // Try to find name in common locations
        const nameCell = row.querySelector('td:first-child, .grid-cell:first-child, [data-name]');
        if (nameCell) {
            return nameCell.textContent.trim() || 'item';
        }

        return 'item';
    }

    /**
     * Enhance loading states with proper ARIA announcements
     */
    function enhanceLoadingStates() {
        const loadingIndicator = document.getElementById('loadingIndicator');
        if (!loadingIndicator) return;

        // Watch for visibility changes
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.type === 'attributes' && mutation.attributeName === 'style') {
                    const isVisible = loadingIndicator.style.display !== 'none';
                    loadingIndicator.setAttribute('aria-busy', isVisible);

                    if (isVisible) {
                        announceToScreenReader('Loading more certificates');
                    }
                }
            });
        });

        observer.observe(loadingIndicator, { attributes: true });
    }

    /**
     * Announce message to screen readers
     */
    function announceToScreenReader(message, priority = 'polite') {
        // Use AdminCore if available
        if (window.AdminCore && window.AdminCore.announceToScreenReader) {
            window.AdminCore.announceToScreenReader(message);
            return;
        }

        // Fallback: create live region if it doesn't exist
        let liveRegion = document.getElementById('a11y-live-region');
        if (!liveRegion) {
            liveRegion = document.createElement('div');
            liveRegion.id = 'a11y-live-region';
            liveRegion.className = 'admin-sr-only';
            liveRegion.setAttribute('aria-live', priority);
            liveRegion.setAttribute('aria-atomic', 'true');
            document.body.appendChild(liveRegion);
        }

        // Update content
        liveRegion.textContent = message;

        // Clear after announcement
        setTimeout(() => {
            liveRegion.textContent = '';
        }, 1000);
    }

    /**
     * Utility: Check if element is visible
     */
    function isVisible(element) {
        return element.offsetWidth > 0 && element.offsetHeight > 0 &&
               window.getComputedStyle(element).visibility !== 'hidden';
    }

    // Export utilities for use by other scripts
    window.AccessibilityEnhancements = {
        announceToScreenReader,
        setupFocusTrap,
        isVisible
    };

})();
