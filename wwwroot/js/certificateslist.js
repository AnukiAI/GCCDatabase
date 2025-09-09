/**
 * Certificate List JavaScript
 * Handles interactive functionality for the certificate list page
 * Following GreenCircle Design Guide patterns
 */

class CertificateList {
    constructor() {
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.setupFilters();
        this.setupSearch();
        this.setupSort();
        this.setupMobileFilters();
        this.setupPagination();
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

        if (searchInput) {
            // Debounced search
            let searchTimeout;
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    this.performSearch(e.target.value);
                }, 300);
            });

            // Search on Enter key
            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.performSearch(e.target.value);
                }
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

    setupPagination() {
        // Pagination will be handled by MVC backend
        // This is just for client-side interactions
        const paginationButtons = document.querySelectorAll('.pagination .btn');
        paginationButtons.forEach(btn => {
            btn.addEventListener('click', (e) => {
                if (!btn.disabled) {
                    e.preventDefault();
                    // In MVC implementation, this would trigger a page change
                    console.log('Pagination clicked:', btn.textContent.trim());
                }
            });
        });
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
        const isChecked = checkbox.checked;

        // In MVC implementation, this would update the filter state and trigger a request
        console.log(`Filter ${filterValue} ${isChecked ? 'checked' : 'unchecked'}`);

        // Update results count (simulated)
        this.updateResultsCount();

        // Apply filters with loading state
        this.showLoadingState();
        setTimeout(() => {
            this.hideLoadingState();
        }, 300);
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
        
        // Uncheck all filter checkboxes - including those with data attributes (External view) and name attributes (Internal view)
        const filterCheckboxes = document.querySelectorAll('.filter-item input[type="checkbox"], .filter-option input[type="checkbox"], input[type="checkbox"][name^="cert-"], input[type="checkbox"][name^="product-"], input[type="checkbox"][name^="company"], input[type="checkbox"][name^="countries"], input[type="checkbox"][name^="additional"], input[type="checkbox"][data-type], input[type="checkbox"][data-category], input[type="checkbox"][data-country], .filter-tree input[type="checkbox"]');
        console.log(`Found ${filterCheckboxes.length} checkboxes to clear`);
        
        filterCheckboxes.forEach(checkbox => {
            if (checkbox.checked) {
                const identifier = checkbox.name || checkbox.id || checkbox.dataset.type || checkbox.dataset.category || checkbox.dataset.country;
                console.log(`Unchecking: ${identifier}`);
                checkbox.checked = false;
                // Trigger change event to update any dependent logic
                const event = new Event('change', { bubbles: true });
                checkbox.dispatchEvent(event);
            }
        });

        // Clear the main search input
        const searchInput = document.querySelector('.search-input');
        if (searchInput && searchInput.value) {
            console.log(`Clearing main search input: ${searchInput.value}`);
            searchInput.value = '';
            // Trigger input event to update search
            const event = new Event('input', { bubbles: true });
            searchInput.dispatchEvent(event);
        }

        // Clear any filter-specific search inputs
        const filterSearchInputs = document.querySelectorAll('.filter-search-input');
        filterSearchInputs.forEach(input => {
            if (input.value) {
                console.log(`Clearing filter search input: ${input.value}`);
                input.value = '';
                // Trigger search to show all options
                this.handleFilterSearch(input);
            }
        });

        // Reset sort to default
        const sortLabel = document.getElementById('sortLabel');
        if (sortLabel) {
            sortLabel.textContent = 'Default';
        }

        // Reset the sort dropdown value
        this.currentSort = 'default';

        // Close any open filter panels for mobile
        const filterPanels = document.querySelectorAll('.filter-panel.open');
        filterPanels.forEach(panel => {
            panel.classList.remove('open');
        });

        // Show all certificate cards after clearing filters
        this.showAllCertificates();
        
        // Update results count to show all
        const resultsCountElement = document.getElementById('resultsCount');
        if (resultsCountElement) {
            const allCards = document.querySelectorAll('.certificate-card');
            resultsCountElement.textContent = `${allCards.length} Results`;
        }
        
        // If this is a dynamic page, reload without filters
        if (window.location.search) {
            window.location.href = window.location.pathname;
        }
        
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

        // Show loading state
        this.showLoadingState();

        // In MVC implementation, this would trigger a server request
        // Simulate API delay
        setTimeout(() => {
            this.hideLoadingState();

            if (searchTerm.trim()) {
                this.showToast(`Searching for "${searchTerm}"`, 'info');
            }

            // Update results count (simulated)
            this.updateResultsCount();
        }, 500);
    }

    // Sorting
    applySorting(sortValue) {
        console.log('Applying sort:', sortValue);
        // In MVC implementation, this would trigger a server request with sort parameters
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
        const certificatesGrid = document.getElementById('certificatesGrid');
        if (certificatesGrid) {
            certificatesGrid.classList.add('loading');
        }
    }

    hideLoadingState() {
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

    // Public API methods for MVC integration
    updateCertificates(certificates) {
        // Method to update the certificates grid with new data from MVC controller
        const grid = document.getElementById('certificatesGrid');
        if (grid && certificates) {
            // Clear existing cards
            grid.innerHTML = '';

            // Add new certificate cards
            certificates.forEach(cert => {
                const cardElement = this.createCertificateCard(cert);
                grid.appendChild(cardElement);
            });

            // Re-setup event listeners for new cards
            this.setupCertificateActions();
        }
    }

    createCertificateCard(certificate) {
        // Helper method to create certificate card HTML from data
        const card = document.createElement('article');
        card.className = 'certificate-card';

        card.innerHTML = `
            <header class="card-header">
                <h3>${certificate.certificationType}</h3>
                <span class="badge badge-primary">${certificate.badgeText}</span>
            </header>
            <div class="certificate-single-container">
                <div class="certificate-number">
                    <span class="label">Certificate #:</span>
                    <span class="value">${certificate.number}</span>
                </div>
                <h4 class="company-name">${certificate.companyName}</h4>
                <h5 class="product-name">${certificate.productName}</h5>
                <div class="product-category">
                    <span class="tag-type External">${certificate.category}</span>
                </div>
                <div class="certification-period">
                    <div class="period-label">Certification Period:</div>
                    <div class="period-dates">${certificate.effectiveDate} - ${certificate.expirationDate}</div>
                </div>
                <div class="certificate-location">
                    <i class="fas fa-map-marker-alt"></i>
                    <span>${certificate.location}</span>
                </div>
            </div>
            <footer class="certificate-actions">
                <button class="btn btn-outline btn-sm" title="Download Certificate">
                    <i class="fas fa-download"></i>
                </button>
                <button class="btn btn-outline btn-sm" title="View Details">
                    <i class="fas fa-eye"></i>
                </button>
                <button class="btn btn-outline btn-sm" title="Expand">
                    <i class="fas fa-expand"></i>
                </button>
            </footer>
        `;

        return card;
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