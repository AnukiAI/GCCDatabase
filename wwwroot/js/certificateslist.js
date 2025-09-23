/**
 * Certificate List JavaScript
 * Handles interactive functionality for the certificate list page
 * Following GreenCircle Design Guide patterns
 */

class CertificateList {
    constructor() {
        // Infinite scroll state
        this.currentPage = 1;
        this.pageSize = 20;
        this.isLoading = false;
        this.hasMoreResults = true;
        this.totalLoadedCertificates = 0;
        this.lastRequestId = null;

        // Filter and search state
        this.currentFilters = {};
        this.currentSearchTerm = '';
        this.currentSort = 'default';

        // Determine API endpoints based on current page
        this.isInternalView = window.location.pathname.includes('TileViewInternal');
        this.apiBaseUrl = this.isInternalView ? '/api/certificate-tiles-internal' : '/api/certificate-tiles';

        this.init();
    }

    init() {
        this.setupEventListeners();
        this.setupFilters();
        this.setupSearch();
        this.setupSort();
        this.setupMobileFilters();
        this.setupInfiniteScroll();
        this.loadInitialCertificates();
    }

    setupEventListeners() {
        // Use event delegation for all clickable elements
        document.addEventListener('click', (e) => {
            this.handleGlobalClick(e);
        });

        // Certificate card actions
        this.setupCertificateActions();
    }

    handleGlobalClick(e) {
        const target = e.target;
        const closestButton = target.closest('button');
        const closestClickable = target.closest('[data-action]') || target.closest('.filter-header') || target.closest('.sort-option');

        // Handle mobile filter toggle
        if (target.closest('.mobile-filter-toggle')) {
            e.preventDefault();
            this.toggleMobileFilters();
            return;
        }

        // Handle clear filters
        if (target.closest('.clear-filters')) {
            e.preventDefault();
            this.clearAllFilters();
            return;
        }

        // Handle filter header toggles
        if (target.closest('.filter-header')) {
            e.preventDefault();
            this.toggleFilter(target.closest('.filter-header'));
            return;
        }

        // Handle sort dropdown toggle
        if (target.closest('.sort-dropdown-button')) {
            e.preventDefault();
            e.stopPropagation();
            this.toggleSort();
            return;
        }

        // Handle sort options
        if (target.closest('.sort-option')) {
            e.preventDefault();
            this.selectSort(target.closest('.sort-option'));
            return;
        }

        // Handle view toggle buttons
        if (target.closest('.toggle-btn')) {
            e.preventDefault();
            const viewType = target.closest('.toggle-btn').getAttribute('data-view');
            if (viewType) {
                this.toggleView(viewType);
            }
            return;
        }

        // Handle certificate action buttons
        if (target.closest('.action-icon')) {
            e.preventDefault();
            this.handleCertificateAction(target.closest('.action-icon'));
            return;
        }

        // Handle certificate action buttons with data attributes
        if (target.closest('.certificate-action-button[data-action]')) {
            e.preventDefault();
            this.handleCertificateAction(target.closest('.certificate-action-button[data-action]'));
            return;
        }

        // Handle bulk actions
        if (target.closest('[data-action="clear-selection"]')) {
            e.preventDefault();
            this.clearSelection();
            return;
        }

        if (target.closest('[data-action="bulk-download"]')) {
            e.preventDefault();
            this.bulkDownload();
            return;
        }

        if (target.closest('[data-action="bulk-export"]')) {
            e.preventDefault();
            this.bulkExport();
            return;
        }

        // Handle table sorting
        if (target.closest('[data-sort-column]')) {
            e.preventDefault();
            const column = target.closest('[data-sort-column]').getAttribute('data-sort-column');
            this.sortTable(column);
            return;
        }

        // Handle select all checkbox
        if (target.id === 'selectAll') {
            this.toggleSelectAll();
            return;
        }

        // Close sort dropdown when clicking outside
        const sortDropdown = document.querySelector('.sort-dropdown.open');
        if (sortDropdown && !sortDropdown.contains(target)) {
            sortDropdown.classList.remove('open');
        }
    }

    setupFilters() {
        // Filter panel toggles
        const filterHeaders = document.querySelectorAll('.filter-header');
        filterHeaders.forEach(header => {
            header.addEventListener('click', () => {
                const panel = header.parentElement;
                panel.classList.toggle('open');

                // Store filter state in localStorage for persistence
                const filterId = header.dataset.filter;
                if (filterId) {
                    const isOpen = panel.classList.contains('open');
                    localStorage.setItem(`filter_${filterId}_open`, isOpen);
                }
            });

            // Restore filter panel states from localStorage
            const filterId = header.dataset.filter;
            if (filterId) {
                const isOpen = localStorage.getItem(`filter_${filterId}_open`) === 'true';
                if (isOpen) {
                    header.parentElement.classList.add('open');
                }
            }
        });

        // Filter tree toggles for hierarchical categories
        const treeToggles = document.querySelectorAll('.tree-toggle');
        treeToggles.forEach(toggle => {
            toggle.addEventListener('click', (e) => {
                e.stopPropagation();
                const treeItem = toggle.closest('.filter-tree-item');
                treeItem.classList.toggle('expanded');
            });
        });

        // Filter checkboxes - using broader selectors to catch all variations including External view's data attributes
        const filterCheckboxes = document.querySelectorAll('.filter-item input[type="checkbox"], .filter-option input[type="checkbox"], input[type="checkbox"][name^="cert-"], input[type="checkbox"][name^="product-"], input[type="checkbox"][name^="company"], input[type="checkbox"][name^="country"], input[type="checkbox"][name^="tag"], input[type="checkbox"][data-type], input[type="checkbox"][data-category], input[type="checkbox"][data-country], .filter-tree input[type="checkbox"]');
        filterCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                this.handleFilterChange(e.target);
            });
        });

        // Filter search inputs
        const filterSearchInputs = document.querySelectorAll('.filter-search-input');
        filterSearchInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                this.handleFilterSearch(e.target);
            });
        });
    }

    setupSearch() {
        const searchInput = document.getElementById('searchInput');
        const searchBtn = document.querySelector('.search-btn');
        const autocompleteDropdown = document.getElementById('autocompleteDropdown');
        const autocompleteList = document.getElementById('autocompleteList');

        this.selectedSuggestionIndex = -1;
        this.currentSuggestions = [];

        if (searchInput && autocompleteDropdown && autocompleteList) {
            // Debounced autocomplete
            let autocompleteTimeout;
            searchInput.addEventListener('input', (e) => {
                const searchTerm = e.target.value.trim();

                clearTimeout(autocompleteTimeout);

                if (searchTerm.length >= 2) {
                    autocompleteTimeout = setTimeout(() => {
                        this.fetchAutocompleteSuggestions(searchTerm);
                    }, 300);
                } else {
                    this.hideAutocompleteDropdown();
                }
            });

            // Keyboard navigation
            searchInput.addEventListener('keydown', (e) => {
                if (this.isAutocompleteVisible()) {
                    this.handleAutocompleteKeyNavigation(e);
                }
            });

            // Search on Enter key
            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    if (this.selectedSuggestionIndex >= 0 && this.currentSuggestions[this.selectedSuggestionIndex]) {
                        this.selectSuggestion(this.currentSuggestions[this.selectedSuggestionIndex]);
                    } else {
                        this.performSearch(e.target.value);
                    }
                }
            });

            // Hide dropdown when clicking outside
            document.addEventListener('click', (e) => {
                if (!searchInput.contains(e.target) && !autocompleteDropdown.contains(e.target)) {
                    this.hideAutocompleteDropdown();
                }
            });

            // Handle blur to hide dropdown with slight delay for click handling
            searchInput.addEventListener('blur', () => {
                setTimeout(() => {
                    this.hideAutocompleteDropdown();
                }, 150);
            });
        }

        if (searchBtn) {
            searchBtn.addEventListener('click', (e) => {
                e.preventDefault();
                const searchValue = searchInput ? searchInput.value : '';
                this.performSearch(searchValue);
            });
        }
    }

    setupSort() {
        // Sort functionality handled in event listeners
        this.currentSort = 'default';
    }

    setupMobileFilters() {
        const mobileFilterToggle = document.getElementById('mobileFilterToggle');
        const filtersSidebar = document.querySelector('.filters-sidebar');

        if (mobileFilterToggle && filtersSidebar) {
            mobileFilterToggle.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleMobileFilters();
            });

            // Create overlay for mobile filters
            const overlay = document.createElement('div');
            overlay.className = 'filter-overlay';
            overlay.addEventListener('click', () => {
                this.closeMobileFilters();
            });
            document.body.appendChild(overlay);

            // Close mobile filters on window resize if screen becomes large
            window.addEventListener('resize', () => {
                if (window.innerWidth > 768) {
                    this.closeMobileFilters();
                }
            });
        }
    }

    setupInfiniteScroll() {
        // Create intersection observer for infinite scroll
        const options = {
            root: null,
            rootMargin: '100px',
            threshold: 0.1
        };

        this.intersectionObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && this.hasMoreResults && !this.isLoading) {
                    this.loadMoreCertificates();
                }
            });
        }, options);

        // Observe the loading container
        const loadingContainer = document.getElementById('loadingContainer');
        if (loadingContainer) {
            this.intersectionObserver.observe(loadingContainer);
        }

        // Add scroll event listener as fallback
        window.addEventListener('scroll', this.throttle(() => {
            if (this.shouldLoadMore()) {
                this.loadMoreCertificates();
            }
        }, 100));
    }

    throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }

    shouldLoadMore() {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        const windowHeight = window.innerHeight;
        const documentHeight = document.documentElement.scrollHeight;

        return (scrollTop + windowHeight >= documentHeight - 500) &&
               this.hasMoreResults &&
               !this.isLoading;
    }

    setupCertificateActions() {
        const certificateCards = document.querySelectorAll('.certificate-card');

        certificateCards.forEach(card => {
            // Download button
            const downloadBtn = card.querySelector('button[title="Download Certificate"]');
            if (downloadBtn) {
                downloadBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.downloadCertificate(card);
                });
            }

            // View details button
            const viewBtn = card.querySelector('button[title="View Details"]');
            if (viewBtn) {
                viewBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.viewCertificateDetails(card);
                });
            }

            // Expand button
            const expandBtn = card.querySelector('button[title="Expand"]');
            if (expandBtn) {
                expandBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.expandCertificate(card);
                });
            }

            // Card hover effects
            card.addEventListener('mouseenter', () => {
                card.style.transform = 'translateY(-4px)';
            });

            card.addEventListener('mouseleave', () => {
                card.style.transform = 'translateY(0)';
            });
        });
    }

    // Sort handling
    handleSortChange(sortOption) {
        const sortValue = sortOption.dataset.sort;
        const sortText = sortOption.textContent.trim();

        // Update button text
        const sortBtn = document.getElementById('sortDropdownBtn');
        if (sortBtn) {
            sortBtn.querySelector('span').textContent = sortText;
        }

        // Close dropdown
        const sortDropdown = sortOption.closest('.sort-dropdown');
        if (sortDropdown) {
            sortDropdown.classList.remove('open');
        }

        // Apply sort (in MVC, this would trigger a server request)
        this.applySorting(sortValue);
        this.currentSort = sortValue;

        // Show loading state
        this.showLoadingState();

        // Simulate API call delay
        setTimeout(() => {
            this.hideLoadingState();
            this.showToast(`Sorted by ${sortText}`, 'success');
        }, 500);
    }

    // Filter handling
    handleFilterChange(checkbox) {
        const filterValue = checkbox.value;
        const filterName = checkbox.name || checkbox.getAttribute('data-filter-type') || 'unknown';
        const isChecked = checkbox.checked;

        console.log(`Filter ${filterName}=${filterValue} ${isChecked ? 'checked' : 'unchecked'}`);

        // Update current filters state
        if (!this.currentFilters[filterName]) {
            this.currentFilters[filterName] = [];
        }

        if (isChecked) {
            if (!this.currentFilters[filterName].includes(filterValue)) {
                this.currentFilters[filterName].push(filterValue);
            }
        } else {
            this.currentFilters[filterName] = this.currentFilters[filterName].filter(val => val !== filterValue);
            if (this.currentFilters[filterName].length === 0) {
                delete this.currentFilters[filterName];
            }
        }

        // Reload certificates with new filters
        this.loadInitialCertificates();
    }

    handleFilterSearch(searchInput) {
        const searchTerm = searchInput.value.toLowerCase();
        const filterPanel = searchInput.closest('.filter-panel');
        const filterOptions = filterPanel.querySelectorAll('.filter-option');

        filterOptions.forEach(option => {
            const optionText = option.textContent.toLowerCase();
            const shouldShow = optionText.includes(searchTerm);
            option.style.display = shouldShow ? 'flex' : 'none';
        });
    }

    clearAllFilters() {
        console.log('Clearing all filters...');

        // Reset internal state
        this.currentFilters = {};
        this.currentSearchTerm = '';
        this.currentSort = 'default';

        // Uncheck all filter checkboxes - including those with data attributes (External view) and name attributes (Internal view)
        const filterCheckboxes = document.querySelectorAll('.filter-item input[type="checkbox"], .filter-option input[type="checkbox"], input[type="checkbox"][name^="cert-"], input[type="checkbox"][name^="product-"], input[type="checkbox"][name^="company"], input[type="checkbox"][name^="countries"], input[type="checkbox"][name^="additional"], input[type="checkbox"][data-type], input[type="checkbox"][data-category], input[type="checkbox"][data-country], .filter-tree input[type="checkbox"]');
        console.log(`Found ${filterCheckboxes.length} checkboxes to clear`);

        filterCheckboxes.forEach(checkbox => {
            checkbox.checked = false;
        });

        // Clear the main search input
        const searchInput = document.querySelector('.search-input');
        if (searchInput) {
            searchInput.value = '';
        }

        // Clear any filter-specific search inputs
        const filterSearchInputs = document.querySelectorAll('.filter-search-input');
        filterSearchInputs.forEach(input => {
            input.value = '';
            this.handleFilterSearch(input);
        });

        // Reset sort to default
        const sortLabel = document.getElementById('sortLabel');
        if (sortLabel) {
            sortLabel.textContent = 'Default';
        }

        // Close any open filter panels for mobile
        const filterPanels = document.querySelectorAll('.filter-panel.open');
        filterPanels.forEach(panel => {
            panel.classList.remove('open');
        });

        // Reload certificates with cleared filters
        this.loadInitialCertificates();

        this.showToast('All filters cleared', 'info');
        console.log('All filters cleared successfully');
    }

    showAllCertificates() {
        console.log('Showing all certificate cards...');
        // Find all certificate cards and make them visible
        const certificateCards = document.querySelectorAll('.certificate-card');
        certificateCards.forEach(card => {
            card.style.display = ''; // Reset to default display
        });
        console.log(`Showed ${certificateCards.length} certificate cards`);
    }

    // Search handling
    performSearch(searchTerm) {
        console.log('Searching for:', searchTerm);

        this.currentSearchTerm = searchTerm.trim();

        // Reload certificates with new search term
        this.loadInitialCertificates();

        if (this.currentSearchTerm) {
            this.showToast(`Searching for "${this.currentSearchTerm}"`, 'info');
        }
    }

    // Sorting
    applySorting(sortValue) {
        console.log('Applying sort:', sortValue);
        this.currentSort = sortValue;

        // Reload certificates with new sort
        this.loadInitialCertificates();
    }

    // New methods to handle inline functions
    toggleFilter(filterHeader) {
        const panel = filterHeader.parentElement;
        panel.classList.toggle('open');

        // Store filter state in localStorage for persistence
        const filterId = filterHeader.dataset.filter;
        if (filterId) {
            const isOpen = panel.classList.contains('open');
            localStorage.setItem(`filter_${filterId}_open`, isOpen);
        }
    }

    toggleSort() {
        const sortDropdown = document.getElementById('sortDropdown');
        if (sortDropdown) {
            sortDropdown.classList.toggle('open');
        }
    }

    selectSort(sortOption) {
        const sortValue = sortOption.getAttribute('data-sort') || sortOption.textContent.trim();
        const sortText = sortOption.textContent.trim();

        // Update button text
        const sortLabel = document.getElementById('sortLabel');
        if (sortLabel) {
            sortLabel.textContent = sortText;
        }

        // Close dropdown
        const sortDropdown = document.getElementById('sortDropdown');
        if (sortDropdown) {
            sortDropdown.classList.remove('open');
        }

        // Apply sort
        this.applySorting(sortValue);
        this.currentSort = sortValue;

        // Show loading state
        this.showLoadingState();

        // Simulate API call delay
        setTimeout(() => {
            this.hideLoadingState();
            this.showToast(`Sorted by ${sortText}`, 'success');
        }, 500);
    }

    toggleView(viewType) {
        // Remove active class from all toggle buttons
        const toggleButtons = document.querySelectorAll('.toggle-btn');
        toggleButtons.forEach(btn => btn.classList.remove('active'));

        // Add active class to clicked button
        const clickedButton = document.querySelector(`[data-view="${viewType}"]`);
        if (clickedButton) {
            clickedButton.classList.add('active');
        }

        // Update view
        console.log(`Switching to ${viewType} view`);
        this.showToast(`Switched to ${viewType} view`, 'info');
    }

    handleCertificateAction(actionElement) {
        const action = actionElement.getAttribute('data-action');
        const certificateNumber = actionElement.getAttribute('data-cert');

        if (!certificateNumber) {
            // Fallback to old method if data-cert is not available
            const certificateCard = actionElement.closest('.certificate-card');
            const fallbackNumber = this.getCertificateNumber(certificateCard);
            
            if (actionElement.classList.contains('edit')) {
                this.editCertificate(fallbackNumber);
            } else if (actionElement.classList.contains('download')) {
                this.downloadCertificate(fallbackNumber);
            } else if (actionElement.classList.contains('external')) {
                this.viewCertificate(fallbackNumber);
            }
            return;
        }

        switch (action) {
            case 'edit':
                this.editCertificate(certificateNumber);
                break;
            case 'download':
                this.downloadCertificate(certificateNumber);
                break;
            case 'view':
                this.viewCertificate(certificateNumber);
                break;
            default:
                console.warn('Unknown certificate action:', action);
        }
    }

    getCertificateNumber(certificateCard) {
        const certificateNumberElement = certificateCard.querySelector('.certificate-number .value');
        return certificateNumberElement ? certificateNumberElement.textContent.trim() : 'Unknown';
    }

    editCertificate(certificateNumber) {
        console.log('Editing certificate:', certificateNumber);
        // In MVC implementation, this would navigate to edit page
        window.location.href = `/Certificate/Edit/${certificateNumber}`;
    }

    downloadCertificate(certificateNumber) {
        console.log('Downloading certificate:', certificateNumber);
        // In MVC implementation, this would trigger a download
        window.location.href = `/Certificate/Download/${certificateNumber}`;
    }

    viewCertificate(certificateNumber) {
        console.log('Viewing certificate:', certificateNumber);
        // In MVC implementation, this would navigate to details page
        window.location.href = `/Certificate/Details/${certificateNumber}`;
    }

    // Mobile filter controls
    toggleMobileFilters() {
        const filtersSidebar = document.querySelector('.sidebar');
        const overlay = document.querySelector('.filter-overlay') || this.createFilterOverlay();

        if (filtersSidebar) {
            const isOpen = filtersSidebar.classList.contains('mobile-open');

            if (isOpen) {
                this.closeMobileFilters();
            } else {
                this.openMobileFilters();
            }
        }
    }

    createFilterOverlay() {
        const overlay = document.createElement('div');
        overlay.className = 'filter-overlay';
        overlay.addEventListener('click', () => {
            this.closeMobileFilters();
        });
        document.body.appendChild(overlay);
        return overlay;
    }

    openMobileFilters() {
        const filtersSidebar = document.querySelector('.sidebar');
        const overlay = document.querySelector('.filter-overlay');

        if (filtersSidebar) {
            filtersSidebar.classList.add('mobile-open');
            document.body.style.overflow = 'hidden';
        }
        if (overlay) {
            overlay.classList.add('show');
        }
    }

    closeMobileFilters() {
        const filtersSidebar = document.querySelector('.sidebar');
        const overlay = document.querySelector('.filter-overlay');

        if (filtersSidebar) {
            filtersSidebar.classList.remove('mobile-open');
            document.body.style.overflow = '';
        }
        if (overlay) {
            overlay.classList.remove('show');
        }
    }

    // Certificate actions
    downloadCertificate(card) {
        const certificateNumber = card.querySelector('.certificate-number .value')?.textContent;
        console.log('Downloading certificate:', certificateNumber);

        // In MVC implementation, this would trigger a download
        this.showToast(`Downloading certificate ${certificateNumber}`, 'success');
    }

    viewCertificateDetails(card) {
        const certificateNumber = card.querySelector('.certificate-number .value')?.textContent;
        console.log('Viewing certificate details:', certificateNumber);

        // In MVC implementation, this would navigate to a details page
        this.showToast(`Viewing details for certificate ${certificateNumber}`, 'info');
    }

    expandCertificate(card) {
        const certificateNumber = card.querySelector('.certificate-number .value')?.textContent;
        console.log('Expanding certificate:', certificateNumber);

        // Toggle expanded state
        card.classList.toggle('expanded');
        this.showToast(`Certificate ${certificateNumber} expanded`, 'info');
    }

    // Utility functions
    updateResultsCount() {
        const resultsCountElement = document.getElementById('resultsCount');
        if (resultsCountElement) {
            // Count visible certificate cards
            const visibleCards = document.querySelectorAll('.certificate-card:not([style*="display: none"])');
            const count = visibleCards.length;
            // Always include "Results" text
            resultsCountElement.textContent = `${count} Results`;
        }
    }

    showLoadingState() {
        const loadingContainer = document.getElementById('loadingContainer');
        if (loadingContainer) {
            loadingContainer.style.display = 'block';
        }

        const certificatesGrid = document.getElementById('certificatesGrid');
        if (certificatesGrid) {
            certificatesGrid.classList.add('loading');
        }
    }

    hideLoadingState() {
        const loadingContainer = document.getElementById('loadingContainer');
        if (loadingContainer) {
            loadingContainer.style.display = 'none';
        }

        const certificatesGrid = document.getElementById('certificatesGrid');
        if (certificatesGrid) {
            certificatesGrid.classList.remove('loading');
        }
    }

    showToast(message, type = 'success') {
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;

        const iconMap = {
            success: 'check-circle',
            error: 'exclamation-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };

        toast.innerHTML = `
            <i class="fas fa-${iconMap[type]}"></i>
            ${message}
        `;

        // Add to page
        document.body.appendChild(toast);

        // Show with animation
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        // Auto remove after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                if (document.body.contains(toast)) {
                    document.body.removeChild(toast);
                }
            }, 300);
        }, 3000);
    }

    // Infinite scroll core methods
    async loadInitialCertificates() {
        console.log('Loading initial certificates...');
        this.resetScrollState();
        await this.loadMoreCertificates(true);
    }

    async loadMoreCertificates(isInitial = false) {
        if (this.isLoading || (!this.hasMoreResults && !isInitial)) {
            return;
        }

        this.isLoading = true;
        this.showLoadingState();

        try {
            const requestId = Date.now().toString();
            this.lastRequestId = requestId;

            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                searchTerm: this.currentSearchTerm,
                sort: this.currentSort,
                ...this.currentFilters
            });

            const response = await fetch(`${this.apiBaseUrl}?${params}`);

            // Check if this is still the latest request
            if (this.lastRequestId !== requestId) {
                console.log('Request superseded, ignoring response');
                return;
            }

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();

            if (data.success && data.certificates) {
                if (isInitial) {
                    this.clearCertificatesGrid();
                }

                this.appendCertificates(data.certificates);
                this.updatePagingState(data);
                this.updateResultsCount(data.totalCount || this.totalLoadedCertificates);

                console.log(`Loaded page ${this.currentPage}, ${data.certificates.length} certificates`);
                this.currentPage++;
            } else {
                console.error('Failed to load certificates:', data.message);
                this.hasMoreResults = false;
            }

        } catch (error) {
            console.error('Error loading certificates:', error);
            this.showToast('Error loading certificates. Please try again.', 'error');
            this.hasMoreResults = false;
        } finally {
            this.isLoading = false;
            this.hideLoadingState();
        }
    }

    resetScrollState() {
        this.currentPage = 1;
        this.hasMoreResults = true;
        this.totalLoadedCertificates = 0;
        this.isLoading = false;
    }

    clearCertificatesGrid() {
        const grid = document.getElementById('certificatesGrid');
        if (grid) {
            // Keep loading and end-of-results elements, remove certificate cards
            const loadingContainer = document.getElementById('loadingContainer');
            const endOfResults = document.getElementById('endOfResults');

            grid.innerHTML = '';

            if (loadingContainer) grid.appendChild(loadingContainer);
            if (endOfResults) grid.appendChild(endOfResults);
        }
    }

    appendCertificates(certificates) {
        const grid = document.getElementById('certificatesGrid');
        const loadingContainer = document.getElementById('loadingContainer');

        if (grid && certificates) {
            certificates.forEach(cert => {
                const cardElement = this.createCertificateCard(cert);
                // Insert before loading container
                if (loadingContainer) {
                    grid.insertBefore(cardElement, loadingContainer);
                } else {
                    grid.appendChild(cardElement);
                }
            });

            this.totalLoadedCertificates += certificates.length;
            this.setupCertificateActions();
        }
    }

    updatePagingState(data) {
        this.hasMoreResults = data.hasMore ||
                           (data.totalCount && this.totalLoadedCertificates < data.totalCount) ||
                           (data.certificates && data.certificates.length === this.pageSize);

        const endOfResults = document.getElementById('endOfResults');
        if (endOfResults) {
            endOfResults.style.display = this.hasMoreResults ? 'none' : 'block';
        }
    }

    // Public API methods for MVC integration
    updateCertificates(certificates) {
        // Method to update the certificates grid with new data from MVC controller
        this.clearCertificatesGrid();
        if (certificates) {
            this.appendCertificates(certificates);
            this.totalLoadedCertificates = certificates.length;
            this.updateResultsCount(certificates.length);
        }
    }

    createCertificateCard(certificate) {
        // Helper method to create certificate card HTML from data
        const card = document.createElement('div');
        card.className = 'certificate-card';

        // Add data attributes for filtering
        if (certificate.certType) card.setAttribute('data-type', certificate.certType);
        if (certificate.companySlug) card.setAttribute('data-company', certificate.companySlug);
        if (certificate.country) card.setAttribute('data-country', certificate.country);
        if (certificate.category) card.setAttribute('data-category', certificate.category);
        if (certificate.effectiveDate) card.setAttribute('data-date', certificate.effectiveDate);

        card.innerHTML = `
            <div class="card-header">
                <div class="header-row">
                    <div class="certificate-category">${this.escapeHtml(certificate.certificationType || certificate.certType || 'CERTIFIED SUSTAINABILITY FACTS')}</div>
                    <div class="certificate-id">${this.escapeHtml(certificate.certificateNumber || certificate.id || '')}</div>
                </div>
                <div class="company-name">${this.escapeHtml(certificate.companyName || '')}</div>
            </div>
            <div class="card-body">
                <div class="product-info">
                    <div class="product-name">${this.escapeHtml(certificate.productName || '')}</div>
                    <div class="product-type">${this.escapeHtml(certificate.productType || certificate.category || '')}</div>
                </div>
                <div class="certificate-details">
                    <div class="certificate-period">
                        <strong>Certification Period:</strong>
                        ${this.formatDateRange(certificate.effectiveDate, certificate.expirationDate)}
                    </div>
                    <div class="certificate-location">
                        <i class="fas fa-map-marker-alt"></i>
                        ${this.escapeHtml(certificate.location || certificate.manufacturingLocation || '')}
                    </div>
                </div>
            </div>
            <div class="card-actions">
                <div class="action-icon download" title="Download PDF" data-action="download" data-cert="${this.escapeHtml(certificate.certificateNumber || certificate.id || '')}">
                    <i class="fas fa-download"></i>
                </div>
                <div class="action-icon external" title="View Details" data-action="view" data-cert="${this.escapeHtml(certificate.certificateNumber || certificate.id || '')}">
                    <i class="fas fa-external-link-alt"></i>
                </div>
            </div>
        `;

        return card;
    }

    formatDateRange(startDate, endDate) {
        if (!startDate || !endDate) return 'Date not available';

        const formatDate = (dateStr) => {
            const date = new Date(dateStr);
            return date.toLocaleDateString('en-US', { month: 'numeric', day: 'numeric', year: 'numeric' });
        };

        return `${formatDate(startDate)} - ${formatDate(endDate)}`;
    }

    getFilterState() {
        // Method to get current filter state for MVC form submission
        const filterState = {};

        const filterCheckboxes = document.querySelectorAll('.filter-option input[type="checkbox"]:checked');
        filterCheckboxes.forEach(checkbox => {
            const filterPanel = checkbox.closest('.filter-panel');
            const filterType = filterPanel.querySelector('.filter-header h4').textContent.trim();

            if (!filterState[filterType]) {
                filterState[filterType] = [];
            }
            filterState[filterType].push(checkbox.value);
        });

        return filterState;
    }

    getCurrentSort() {
        // Method to get current sort for MVC form submission
        return this.currentSort || 'default';
    }

    getCurrentSearch() {
        // Method to get current search term for MVC form submission
        const searchInput = document.getElementById('searchInput');
        return searchInput ? searchInput.value : '';
    }

    // New methods for table functionality and bulk actions
    clearSelection() {
        console.log('Clearing selection...');
        const checkboxes = document.querySelectorAll('input[type="checkbox"]:checked');
        checkboxes.forEach(checkbox => {
            if (checkbox.id !== 'selectAll') {
                checkbox.checked = false;
            }
        });
        
        // Update select all checkbox
        const selectAllCheckbox = document.getElementById('selectAll');
        if (selectAllCheckbox) {
            selectAllCheckbox.checked = false;
        }
        
        // Update selected count
        this.updateSelectedCount();
        this.showToast('Selection cleared', 'info');
    }

    bulkDownload() {
        const selectedCertificates = this.getSelectedCertificates();
        console.log('Bulk downloading certificates:', selectedCertificates);
        
        if (selectedCertificates.length === 0) {
            this.showToast('Please select certificates to download', 'warning');
            return;
        }
        
        // In MVC implementation, this would trigger bulk download
        this.showToast(`Downloading ${selectedCertificates.length} certificates`, 'success');
        
        // Simulate download process
        selectedCertificates.forEach((certId, index) => {
            setTimeout(() => {
                console.log(`Downloading certificate ${certId}`);
                window.location.href = `/Certificate/Download/${certId}`;
            }, index * 500); // Stagger downloads
        });
    }

    bulkExport() {
        const selectedCertificates = this.getSelectedCertificates();
        console.log('Bulk exporting certificates:', selectedCertificates);
        
        if (selectedCertificates.length === 0) {
            this.showToast('Please select certificates to export', 'warning');
            return;
        }
        
        // In MVC implementation, this would trigger bulk export
        this.showToast(`Exporting ${selectedCertificates.length} certificates to Excel`, 'success');
        
        // Simulate export process
        const exportData = selectedCertificates.join(',');
        window.location.href = `/Certificate/Export?certificates=${exportData}&format=excel`;
    }

    toggleSelectAll() {
        const selectAllCheckbox = document.getElementById('selectAll');
        const isChecked = selectAllCheckbox.checked;
        
        console.log('Toggle select all:', isChecked);
        
        // Select/deselect all visible certificate checkboxes
        const certificateCheckboxes = document.querySelectorAll('.certificate-checkbox');
        certificateCheckboxes.forEach(checkbox => {
            checkbox.checked = isChecked;
        });
        
        this.updateSelectedCount();
        
        const message = isChecked ? 'All certificates selected' : 'All certificates deselected';
        this.showToast(message, 'info');
    }

    sortTable(column) {
        console.log('Sorting table by column:', column);
        
        // Add visual indicator to column header
        const headers = document.querySelectorAll('.grid-header-cell');
        headers.forEach(header => {
            const sortIndicator = header.querySelector('.sort-indicator');
            if (sortIndicator) {
                sortIndicator.className = 'fas fa-sort sort-indicator';
            }
        });
        
        // Update the clicked column's sort indicator
        const clickedHeader = document.querySelector(`[data-sort-column="${column}"]`);
        if (clickedHeader) {
            const sortIndicator = clickedHeader.querySelector('.sort-indicator');
            if (sortIndicator) {
                // Toggle sort direction
                const currentSort = clickedHeader.dataset.sortDirection || 'none';
                let newSort = 'asc';
                
                if (currentSort === 'asc') {
                    newSort = 'desc';
                    sortIndicator.className = 'fas fa-sort-down sort-indicator';
                } else if (currentSort === 'desc') {
                    newSort = 'asc';
                    sortIndicator.className = 'fas fa-sort-up sort-indicator';
                } else {
                    newSort = 'asc';
                    sortIndicator.className = 'fas fa-sort-up sort-indicator';
                }
                
                clickedHeader.dataset.sortDirection = newSort;
            }
        }
        
        // Show loading state
        this.showLoadingState();
        
        // In MVC implementation, this would trigger a server request
        setTimeout(() => {
            this.hideLoadingState();
            this.showToast(`Sorted by ${column} (${clickedHeader.dataset.sortDirection})`, 'success');
        }, 500);
    }

    getSelectedCertificates() {
        const selectedCheckboxes = document.querySelectorAll('.certificate-checkbox:checked');
        const certificateIds = Array.from(selectedCheckboxes).map(checkbox => {
            // Extract certificate ID from checkbox value or data attribute
            return checkbox.value || checkbox.dataset.certificateId || checkbox.closest('.certificate-row')?.dataset.certificateId;
        }).filter(id => id); // Remove any null/undefined values
        
        return certificateIds;
    }

    updateSelectedCount() {
        const selectedCount = this.getSelectedCertificates().length;
        const countElement = document.getElementById('selectedCount');
        if (countElement) {
            countElement.textContent = `${selectedCount} certificates selected`;
        }
        
        // Show/hide bulk actions based on selection
        const bulkActionsPanel = document.querySelector('.bulk-actions-panel');
        if (bulkActionsPanel) {
            bulkActionsPanel.style.display = selectedCount > 0 ? 'flex' : 'none';
        }
    }

    // Autocomplete functionality
    async fetchAutocompleteSuggestions(searchTerm) {
        try {
            const response = await fetch(`${this.apiBaseUrl}/autocomplete?term=${encodeURIComponent(searchTerm)}`);
            if (response.ok) {
                const data = await response.json();
                this.currentSuggestions = data.suggestions || [];
                this.displayAutocompleteSuggestions(this.currentSuggestions);
            } else {
                this.hideAutocompleteDropdown();
            }
        } catch (error) {
            console.error('Error fetching autocomplete suggestions:', error);
            this.hideAutocompleteDropdown();
        }
    }

    displayAutocompleteSuggestions(suggestions) {
        const autocompleteList = document.getElementById('autocompleteList');
        const autocompleteDropdown = document.getElementById('autocompleteDropdown');

        if (!autocompleteList || !autocompleteDropdown) return;

        // Clear previous suggestions
        autocompleteList.innerHTML = '';
        this.selectedSuggestionIndex = -1;

        if (suggestions.length === 0) {
            this.hideAutocompleteDropdown();
            return;
        }

        // Create suggestion items
        suggestions.forEach((suggestion, index) => {
            const li = document.createElement('li');
            li.className = 'autocomplete-item';
            li.innerHTML = `
                <div class="suggestion-content">
                    <span class="suggestion-text">${this.escapeHtml(suggestion.label)}</span>
                    <span class="suggestion-type">${this.escapeHtml(suggestion.type)}</span>
                </div>
            `;

            // Add click handler
            li.addEventListener('click', () => {
                this.selectSuggestion(suggestion);
            });

            // Add mouseover handler for keyboard navigation
            li.addEventListener('mouseover', () => {
                this.selectedSuggestionIndex = index;
                this.updateSuggestionHighlight();
            });

            autocompleteList.appendChild(li);
        });

        // Show dropdown
        autocompleteDropdown.style.display = 'block';
    }

    selectSuggestion(suggestion) {
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.value = suggestion.value;
            this.hideAutocompleteDropdown();
            this.performSearch(suggestion.value);
        }
    }

    hideAutocompleteDropdown() {
        const autocompleteDropdown = document.getElementById('autocompleteDropdown');
        if (autocompleteDropdown) {
            autocompleteDropdown.style.display = 'none';
        }
        this.currentSuggestions = [];
        this.selectedSuggestionIndex = -1;
    }

    isAutocompleteVisible() {
        const autocompleteDropdown = document.getElementById('autocompleteDropdown');
        return autocompleteDropdown && autocompleteDropdown.style.display !== 'none';
    }

    handleAutocompleteKeyNavigation(e) {
        const maxIndex = this.currentSuggestions.length - 1;

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.selectedSuggestionIndex = Math.min(this.selectedSuggestionIndex + 1, maxIndex);
                this.updateSuggestionHighlight();
                break;
            case 'ArrowUp':
                e.preventDefault();
                this.selectedSuggestionIndex = Math.max(this.selectedSuggestionIndex - 1, -1);
                this.updateSuggestionHighlight();
                break;
            case 'Escape':
                e.preventDefault();
                this.hideAutocompleteDropdown();
                break;
        }
    }

    updateSuggestionHighlight() {
        const suggestionItems = document.querySelectorAll('.autocomplete-item');

        suggestionItems.forEach((item, index) => {
            if (index === this.selectedSuggestionIndex) {
                item.classList.add('highlighted');
            } else {
                item.classList.remove('highlighted');
            }
        });
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Toast styles that need to be added to CSS
const toastStyles = `
.toast {
    position: fixed;
    top: 20px;
    right: 20px;
    background-color: var(--success);
    color: white;
    padding: 15px 20px;
    border-radius: 6px;
    box-shadow: var(--shadow);
    z-index: 9999;
    transition: var(--transition);
    transform: translateX(100%);
    opacity: 0;
    display: flex;
    align-items: center;
    gap: 10px;
    max-width: 300px;
}

.toast.show {
    transform: translateX(0);
    opacity: 1;
}

.toast.error {
    background-color: var(--danger);
}

.toast.warning {
    background-color: #FFE9A3;
    color: var(--blue-dark);
}

.toast.info {
    background-color: #336699;
    color: white;
}
`;

// Add toast styles to page if they don't exist
if (!document.querySelector('style[data-toast-styles]')) {
    const styleElement = document.createElement('style');
    styleElement.setAttribute('data-toast-styles', 'true');
    styleElement.textContent = toastStyles;
    document.head.appendChild(styleElement);
}

// Initialize the certificate list when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.certificateList = new CertificateList();
});

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CertificateList;
}