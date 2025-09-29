/**
 * Certificate Tile View External - Main JavaScript
 * Implements interactive functionality for External users viewing certificates
 */

class CertificateTileViewExternal {
    constructor() {
        this.service = new CertificateExternalService();
        this.debounceTimer = null;
        this.init();
    }

    async init() {
        // Load initial state from URL
        this.loadUrlState();
        
        // Setup all event listeners
        this.setupEventListeners();
        
        // Load filter options
        await this.loadFilterOptions();
        
        // Load initial certificates
        await this.loadCertificates();
        
        // Setup lazy loading
        this.setupLazyLoading();
        
        // Setup autocomplete
        this.setupAutocomplete();
    }

    /**
     * Load state from URL parameters
     */
    loadUrlState() {
        const params = new URLSearchParams(window.location.search);
        const state = {};
        
        params.forEach((value, key) => {
            state[key] = value;
        });
        
        if (Object.keys(state).length > 0) {
            this.service.loadFromUrlState(state);
            this.updateUIFromFilters();
        }
    }

    /**
     * Update URL with current state
     */
    updateUrl() {
        const state = this.service.getUrlState();
        const params = new URLSearchParams(state);
        const newUrl = params.toString() 
            ? `${window.location.pathname}?${params.toString()}`
            : window.location.pathname;
        
        window.history.replaceState(state, '', newUrl);
    }

    /**
     * Setup all event listeners
     */
    setupEventListeners() {
        // Search input
        const searchInput = document.querySelector('.search-input');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => this.handleSearch(e.target.value));
            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    this.performSearch(e.target.value);
                }
            });
        }

        // Clear filters
        const clearFilters = document.querySelector('.clear-filters');
        if (clearFilters) {
            clearFilters.addEventListener('click', (e) => {
                e.preventDefault();
                this.clearAllFilters();
            });
        }

        // Filter checkboxes
        document.querySelectorAll('.filter-content input[type="checkbox"]').forEach(checkbox => {
            checkbox.addEventListener('change', () => this.handleFilterChange());
        });

        // Filter panel toggles
        document.querySelectorAll('.filter-header').forEach(header => {
            header.addEventListener('click', (e) => this.toggleFilterPanel(e.currentTarget));
        });

        // Sort dropdown
        const sortButton = document.getElementById('sortDropdownBtn');
        if (sortButton) {
            sortButton.addEventListener('click', () => this.toggleSortDropdown());
        }

        // Sort options
        document.querySelectorAll('.sort-option').forEach(option => {
            option.addEventListener('click', (e) => this.handleSortChange(e.currentTarget));
        });

        // View toggle buttons
        document.querySelectorAll('.toggle-btn[data-view]').forEach(btn => {
            btn.addEventListener('click', (e) => this.handleViewToggle(e.currentTarget));
        });

        // Mobile filter toggle
        const mobileToggle = document.querySelector('.mobile-filter-toggle');
        if (mobileToggle) {
            mobileToggle.addEventListener('click', () => this.toggleMobileFilters());
        }

        // Export button
        const exportBtn = document.querySelector('.export-btn');
        if (exportBtn) {
            exportBtn.addEventListener('click', () => this.showExportModal());
        }

        // Close dropdowns when clicking outside
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.sort-dropdown')) {
                this.closeSortDropdown();
            }
            if (!e.target.closest('.autocomplete-container')) {
                this.closeAutocomplete();
            }
        });
    }

    /**
     * Setup lazy loading with Intersection Observer
     */
    setupLazyLoading() {
        const loadingIndicator = document.getElementById('loadingIndicator');
        if (!loadingIndicator) return;

        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting && !this.service.isLoading && this.service.hasMore) {
                        this.loadMoreCertificates();
                    }
                });
            },
            {
                root: null,
                rootMargin: '100px',
                threshold: 0.1
            }
        );

        observer.observe(loadingIndicator);
    }

    /**
     * Setup autocomplete functionality
     */
    setupAutocomplete() {
        const searchInput = document.querySelector('.search-input');
        if (!searchInput) return;

        // Create autocomplete dropdown if it doesn't exist
        let autocompleteDropdown = document.getElementById('autocompleteDropdown');
        if (!autocompleteDropdown) {
            autocompleteDropdown = document.createElement('div');
            autocompleteDropdown.id = 'autocompleteDropdown';
            autocompleteDropdown.className = 'autocomplete-dropdown';
            autocompleteDropdown.style.display = 'none';
            searchInput.parentElement.appendChild(autocompleteDropdown);
        }

        // Handle keyboard navigation
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp' || e.key === 'Escape') {
                this.handleAutocompleteNavigation(e);
            }
        });
    }

    /**
     * Load filter options from API
     */
    async loadFilterOptions() {
        try {
            const options = await this.service.getFilterOptions();
            if (!options) return;

            // Populate certification types
            this.populateFilterOptions('cert-type-content', options.certificationTypes, 'certType');
            
            // Populate product categories
            this.populateFilterOptions('product-cat-content', options.productCategories, 'productCategory');
            
            // Populate countries
            this.populateFilterOptions('location-content', options.countries, 'country');
            
            // Populate partners if available
            if (options.partners) {
                this.populateFilterOptions('partner-content', options.partners, 'partner');
            }

        } catch (error) {
            console.error('Error loading filter options:', error);
        }
    }

    /**
     * Populate filter checkboxes dynamically
     */
    populateFilterOptions(containerId, options, dataAttribute) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const ul = container.querySelector('.filter-tree');
        if (!ul) return;

        // Clear existing options
        ul.innerHTML = '';

        // Add new options
        options.forEach(option => {
            const li = document.createElement('li');
            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.id = `${dataAttribute}-${option.id || option}`;
            checkbox.setAttribute(`data-${dataAttribute}`, option.id || option);
            
            const label = document.createElement('label');
            label.htmlFor = checkbox.id;
            label.textContent = option.name || option;
            
            li.appendChild(checkbox);
            li.appendChild(label);
            ul.appendChild(li);

            // Add event listener
            checkbox.addEventListener('change', () => this.handleFilterChange());
        });
    }

    /**
     * Load certificates from API
     */
    async loadCertificates() {
        try {
            this.showLoadingState();
            
            const result = await this.service.loadCertificates(false);
            if (result) {
                this.renderCertificates(result.certificates);
                this.updateResultsCount(result.totalCount);
                this.updateUrl();
            }
        } catch (error) {
            console.error('Error loading certificates:', error);
            this.showErrorState('Failed to load certificates. Please try again.');
        } finally {
            this.hideLoadingState();
        }
    }

    /**
     * Load more certificates (lazy loading)
     */
    async loadMoreCertificates() {
        try {
            const loadingIndicator = document.getElementById('loadingIndicator');
            if (loadingIndicator) {
                loadingIndicator.style.display = 'flex';
            }

            const result = await this.service.loadMore();
            if (result && result.certificates.length > 0) {
                this.appendCertificates(result.certificates.slice(-this.service.pageSize));
            }
        } catch (error) {
            console.error('Error loading more certificates:', error);
        } finally {
            const loadingIndicator = document.getElementById('loadingIndicator');
            if (loadingIndicator) {
                loadingIndicator.style.display = 'none';
            }
        }
    }

    /**
     * Render certificates to the grid
     */
    renderCertificates(certificates) {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;

        // Clear existing certificates
        grid.innerHTML = '';

        if (certificates.length === 0) {
            this.showNoResultsMessage();
            return;
        }

        certificates.forEach(cert => {
            grid.appendChild(this.createCertificateCard(cert));
        });
    }

    /**
     * Append certificates to the grid (for lazy loading)
     */
    appendCertificates(certificates) {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;

        certificates.forEach(cert => {
            grid.appendChild(this.createCertificateCard(cert));
        });
    }

    /**
     * Create a certificate card element
     */
    createCertificateCard(cert) {
        const card = document.createElement('div');
        card.className = 'certificate-card';
        card.setAttribute('data-id', cert.id);
        
        // Format dates
        const effectiveDate = new Date(cert.effectiveDate).toLocaleDateString();
        const expirationDate = new Date(cert.expirationDate).toLocaleDateString();
        
        card.innerHTML = `
            <div class="card-header">
                <div class="header-row">
                    <div class="certificate-category">${cert.certificationType || 'Certificate'}</div>
                    <div class="certificate-id">${cert.certificateNumber}</div>
                </div>
                <div class="company-name">${cert.companyName}</div>
            </div>
            <div class="card-body">
                <div class="product-info">
                    <div class="product-name">${cert.productName}</div>
                    <div class="product-type">${cert.productType || cert.productCategory}</div>
                </div>
                <div class="certificate-details">
                    <div class="certificate-period">
                        <strong>Certification Period:</strong>
                        ${effectiveDate} - ${expirationDate}
                    </div>
                    <div class="certificate-location">
                        <i class="fas fa-map-marker-alt"></i>
                        ${cert.location || ''} ${cert.country || ''}
                    </div>
                </div>
            </div>
            <div class="card-actions">
                <div class="action-icon download" title="Download PDF" data-action="download" data-cert-id="${cert.id}">
                    <i class="fas fa-download"></i>
                </div>
                <div class="action-icon external" title="View Details" data-action="view" data-cert-id="${cert.id}">
                    <i class="fas fa-external-link-alt"></i>
                </div>
            </div>
        `;

        // Add event listeners for actions
        const downloadBtn = card.querySelector('[data-action="download"]');
        const viewBtn = card.querySelector('[data-action="view"]');
        
        if (downloadBtn) {
            downloadBtn.addEventListener('click', () => this.handleDownload(cert.id));
        }
        
        if (viewBtn) {
            viewBtn.addEventListener('click', () => this.handleView(cert.id));
        }

        return card;
    }

    /**
     * Handle search input with debouncing
     */
    handleSearch(value) {
        // Clear existing timer
        if (this.debounceTimer) {
            clearTimeout(this.debounceTimer);
        }

        // Show autocomplete suggestions
        if (value.length >= 2) {
            this.debounceTimer = setTimeout(() => {
                this.showAutocompleteSuggestions(value);
            }, 300);
        } else {
            this.closeAutocomplete();
        }
    }

    /**
     * Perform search immediately
     */
    async performSearch(searchTerm) {
        this.closeAutocomplete();
        await this.service.search(searchTerm);
        await this.loadCertificates();
    }

    /**
     * Show autocomplete suggestions
     */
    async showAutocompleteSuggestions(term) {
        try {
            const result = await this.service.getAutocompleteSuggestions(term);
            const dropdown = document.getElementById('autocompleteDropdown');
            
            if (!dropdown || !result.suggestions || result.suggestions.length === 0) {
                this.closeAutocomplete();
                return;
            }

            // Clear and populate dropdown
            dropdown.innerHTML = '';
            result.suggestions.forEach((suggestion, index) => {
                const item = document.createElement('div');
                item.className = 'autocomplete-item';
                item.setAttribute('data-index', index);
                item.innerHTML = `
                    <span class="suggestion-label">${suggestion.label}</span>
                    <span class="suggestion-type">${suggestion.type}</span>
                `;
                item.addEventListener('click', () => {
                    document.querySelector('.search-input').value = suggestion.value;
                    this.performSearch(suggestion.value);
                });
                dropdown.appendChild(item);
            });

            dropdown.style.display = 'block';
        } catch (error) {
            console.error('Error getting autocomplete suggestions:', error);
        }
    }

    /**
     * Handle filter changes
     */
    async handleFilterChange() {
        // Collect all checked filters
        const filters = {
            certType: [],
            productCategory: [],
            country: [],
            partner: []
        };

        // Get certification types
        document.querySelectorAll('#cert-type-content input:checked').forEach(checkbox => {
            const value = checkbox.getAttribute('data-certType') || checkbox.getAttribute('data-type');
            if (value) filters.certType.push(value);
        });

        // Get product categories
        document.querySelectorAll('#product-cat-content input:checked').forEach(checkbox => {
            const value = checkbox.getAttribute('data-productCategory') || checkbox.getAttribute('data-category');
            if (value) filters.productCategory.push(value);
        });

        // Get countries
        document.querySelectorAll('#location-content input:checked').forEach(checkbox => {
            const value = checkbox.getAttribute('data-country');
            if (value) filters.country.push(value);
        });

        // Get partners
        document.querySelectorAll('#partner-content input:checked').forEach(checkbox => {
            const value = checkbox.getAttribute('data-partner');
            if (value) filters.partner.push(value);
        });

        // Apply filters and reload
        await this.service.applyFilters(filters);
        await this.loadCertificates();
    }

    /**
     * Handle sort change
     */
    async handleSortChange(option) {
        const sortBy = option.getAttribute('data-sort');
        if (!sortBy) return;

        // Update UI
        document.getElementById('sortLabel').textContent = option.textContent.trim();
        this.closeSortDropdown();

        // Apply sort and reload
        await this.service.updateSort(sortBy);
        await this.loadCertificates();
    }

    /**
     * Handle view toggle
     */
    handleViewToggle(button) {
        const viewType = button.getAttribute('data-view');
        if (!viewType) return;

        // Update active state
        document.querySelectorAll('.toggle-btn[data-view]').forEach(btn => {
            btn.classList.remove('active');
        });
        button.classList.add('active');

        // Toggle view (tile vs list)
        const grid = document.getElementById('certificatesGrid');
        if (grid) {
            if (viewType === 'list') {
                grid.classList.add('list-view');
            } else {
                grid.classList.remove('list-view');
            }
        }
    }

    /**
     * Handle certificate download
     */
    async handleDownload(certificateId) {
        try {
            await this.service.downloadCertificate(certificateId);
        } catch (error) {
            console.error('Error downloading certificate:', error);
            alert('Failed to download certificate. Please try again.');
        }
    }

    /**
     * Handle view certificate details
     */
    handleView(certificateId) {
        window.open(`/Certificate/Details/${certificateId}`, '_blank');
    }

    /**
     * Handle export functionality
     */
    showExportModal() {
        const modal = this.createExportFormatModal();
        document.body.appendChild(modal);
        
        // Handle format selection
        modal.querySelectorAll('.export-format-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const format = e.target.closest('.export-format-btn').dataset.format;
                this.closeModal(modal);
                
                try {
                    await this.service.exportCertificates(format);
                } catch (error) {
                    console.error('Export error:', error);
                    alert('Failed to export certificates. Please try again.');
                }
            });
        });
        
        // Handle modal close
        modal.querySelector('.modal-close').addEventListener('click', () => {
            this.closeModal(modal);
        });
        
        // Close modal when clicking overlay
        modal.querySelector('.export-modal-overlay').addEventListener('click', () => {
            this.closeModal(modal);
        });
    }

    createExportFormatModal() {
        const modal = document.createElement('div');
        modal.className = 'export-modal';
        modal.innerHTML = `
            <div class="export-modal-overlay"></div>
            <div class="export-modal-content">
                <div class="export-modal-header">
                    <h3>Export Certificates</h3>
                    <span class="modal-close">&times;</span>
                </div>
                <div class="export-modal-body">
                    <p>Choose export format:</p>
                    <div class="export-format-buttons">
                        <button class="export-format-btn" data-format="csv">
                            <i class="fas fa-file-csv"></i>
                            <div>
                                <span>CSV</span>
                                <small>Comma-separated values</small>
                            </div>
                        </button>
                        <button class="export-format-btn" data-format="excel">
                            <i class="fas fa-file-excel"></i>
                            <div>
                                <span>Excel</span>
                                <small>Microsoft Excel format</small>
                            </div>
                        </button>
                        <button class="export-format-btn" data-format="pdf">
                            <i class="fas fa-file-pdf"></i>
                            <div>
                                <span>PDF</span>
                                <small>Portable Document Format</small>
                            </div>
                        </button>
                    </div>
                </div>
            </div>
        `;
        
        return modal;
    }

    closeModal(modal) {
        if (modal && modal.parentNode) {
            modal.parentNode.removeChild(modal);
        }
    }

    /**
     * Clear all filters
     */
    async clearAllFilters() {
        // Uncheck all checkboxes
        document.querySelectorAll('.filter-content input[type="checkbox"]').forEach(checkbox => {
            checkbox.checked = false;
        });

        // Clear search
        const searchInput = document.querySelector('.search-input');
        if (searchInput) {
            searchInput.value = '';
        }

        // Reset service and reload
        this.service.clearFilters();
        await this.loadCertificates();
    }

    /**
     * Toggle filter panel
     */
    toggleFilterPanel(header) {
        const panel = header.parentElement;
        const content = panel.querySelector('.filter-content');
        const icon = header.querySelector('i');

        if (content.style.display === 'none') {
            content.style.display = 'block';
            icon.classList.remove('fa-chevron-right');
            icon.classList.add('fa-chevron-down');
        } else {
            content.style.display = 'none';
            icon.classList.remove('fa-chevron-down');
            icon.classList.add('fa-chevron-right');
        }
    }

    /**
     * Toggle sort dropdown
     */
    toggleSortDropdown() {
        const dropdown = document.getElementById('sortDropdown');
        if (dropdown) {
            dropdown.classList.toggle('open');
        }
    }

    /**
     * Close sort dropdown
     */
    closeSortDropdown() {
        const dropdown = document.getElementById('sortDropdown');
        if (dropdown) {
            dropdown.classList.remove('open');
        }
    }

    /**
     * Close autocomplete dropdown
     */
    closeAutocomplete() {
        const dropdown = document.getElementById('autocompleteDropdown');
        if (dropdown) {
            dropdown.style.display = 'none';
        }
    }

    /**
     * Handle autocomplete keyboard navigation
     */
    handleAutocompleteNavigation(e) {
        const dropdown = document.getElementById('autocompleteDropdown');
        if (!dropdown || dropdown.style.display === 'none') return;

        const items = dropdown.querySelectorAll('.autocomplete-item');
        const currentActive = dropdown.querySelector('.autocomplete-item.active');
        let currentIndex = currentActive ? parseInt(currentActive.getAttribute('data-index')) : -1;

        if (e.key === 'Escape') {
            this.closeAutocomplete();
            return;
        }

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            currentIndex = (currentIndex + 1) % items.length;
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            currentIndex = currentIndex <= 0 ? items.length - 1 : currentIndex - 1;
        }

        // Update active state
        items.forEach(item => item.classList.remove('active'));
        if (items[currentIndex]) {
            items[currentIndex].classList.add('active');
        }
    }

    /**
     * Toggle mobile filters
     */
    toggleMobileFilters() {
        const sidebar = document.querySelector('.sidebar');
        const toggle = document.querySelector('.mobile-filter-toggle');
        
        if (sidebar) {
            sidebar.classList.toggle('expanded');
            
            if (toggle) {
                const icon = toggle.querySelector('i');
                const text = toggle.querySelector('span');
                
                if (sidebar.classList.contains('expanded')) {
                    icon.classList.remove('fa-chevron-down');
                    icon.classList.add('fa-chevron-up');
                    text.textContent = 'Hide Filters & Search';
                } else {
                    icon.classList.remove('fa-chevron-up');
                    icon.classList.add('fa-chevron-down');
                    text.textContent = 'Show Filters & Search';
                }
            }
        }
    }

    /**
     * Update results count
     */
    updateResultsCount(count) {
        const countElement = document.getElementById('resultsCount');
        if (countElement) {
            countElement.textContent = `Showing ${count} certificate${count !== 1 ? 's' : ''}`;
        }
    }

    /**
     * Update UI from current filters
     */
    updateUIFromFilters() {
        // Update search input
        const searchInput = document.querySelector('.search-input');
        if (searchInput && this.service.filters.search) {
            searchInput.value = this.service.filters.search;
        }

        // Update checkboxes based on filters
        // This would need to be implemented based on actual filter structure
    }

    /**
     * Show loading state
     */
    showLoadingState() {
        const grid = document.getElementById('certificatesGrid');
        if (grid) {
            grid.innerHTML = '<div class="loading-message">Loading certificates...</div>';
        }
    }

    /**
     * Hide loading state
     */
    hideLoadingState() {
        // Loading state is replaced by actual content
    }

    /**
     * Show no results message
     */
    showNoResultsMessage() {
        const grid = document.getElementById('certificatesGrid');
        if (grid) {
            grid.innerHTML = `
                <div class="no-results-message">
                    <i class="fas fa-search fa-3x"></i>
                    <h3>No certificates found</h3>
                    <p>Try adjusting your filters or search terms</p>
                </div>
            `;
        }
    }

    /**
     * Show error state
     */
    showErrorState(message) {
        const grid = document.getElementById('certificatesGrid');
        if (grid) {
            grid.innerHTML = `
                <div class="error-message">
                    <i class="fas fa-exclamation-triangle fa-3x"></i>
                    <h3>Error</h3>
                    <p>${message}</p>
                    <button onclick="location.reload()">Retry</button>
                </div>
            `;
        }
    }
}

// Note: Initialization is handled by the adapter file to prevent duplicate instances
// The adapter will create an instance when DOM is ready