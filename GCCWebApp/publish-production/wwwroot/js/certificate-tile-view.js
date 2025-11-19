// Certificate Tile View JavaScript
(function () {
    'use strict';

    // State management
    const state = {
        certificates: [],
        filters: {
            search: '',
            certType: [],
            productCategory: [],
            company: [],
            country: [],
            tags: [],
            sortBy: 'newest'
        },
        loadedCount: 0,
        batchSize: 20,
        totalCount: 0,
        isLoading: false,
        hasMore: true,
        filterOptions: {},
        debounceTimer: null,
        // Determine API endpoints based on current page
        isInternalView: window.location.pathname.includes('TileViewInternal'),
        apiBaseUrl: window.location.pathname.includes('TileViewInternal') ? '/api/certificate-tiles-internal' : '/api/certificate-tiles',
        // Retry mechanism
        retryCount: 0,
        maxRetries: 3,
        retryDelay: 1000 // Start with 1 second
    };

    function initialize() {
        loadFilterOptions();
        loadInitialCertificates();
        setupEventListeners();
        setupInfiniteScroll();
        setupUrlParameters();
        initializeAutocomplete();
    }

    // Load filter options from API
    async function loadFilterOptions() {
        try {
            const response = await fetch(`${state.apiBaseUrl}/filters`);
            if (response.ok) {
                state.filterOptions = await response.json();
                populateFilterOptions();
            }
        } catch (error) {
            console.error('Error loading filter options:', error);
        }
    }

    // Populate filter dropdowns dynamically
    function populateFilterOptions() {
        // Populate certification types
        const certTypeList = document.querySelector('#cert-type-content ul');
        if (certTypeList && state.filterOptions.certificationTypes) {
            certTypeList.innerHTML = state.filterOptions.certificationTypes.map(type => {
                const typeName = typeof type === 'string' ? type : type.name;
                const typeLevel = typeof type === 'string' ? 1 : (type.level || 1);
                return `
                <li class="filter-item filter-level-${typeLevel}">
                    <label>
                        <input type="checkbox" name="cert-type" value="${escapeHtml(typeName)}">
                        ${escapeHtml(typeName)}
                    </label>
                </li>
            `}).join('');
        }

        // Populate product categories
        const productCategoryList = document.querySelector('#product-category-content ul');
        if (productCategoryList && state.filterOptions.productCategories) {
            productCategoryList.innerHTML = state.filterOptions.productCategories.map(category => `
                <li class="filter-item">
                    <label>
                        <input type="checkbox" name="product-category" value="${escapeHtml(category)}">
                        ${escapeHtml(category)}
                    </label>
                </li>
            `).join('');
        }

        // Populate companies
        const companyList = document.querySelector('#company-content ul');
        if (companyList && state.filterOptions.companies) {
            companyList.innerHTML = state.filterOptions.companies.map(company => `
                <li class="filter-item">
                    <label>
                        <input type="checkbox" name="company" value="${company.id}">
                        ${escapeHtml(company.name)}
                    </label>
                </li>
            `).join('');
        }

        // Populate countries
        const countryList = document.querySelector('#countries-content ul');
        if (countryList && state.filterOptions.countries) {
            countryList.innerHTML = state.filterOptions.countries.map(country => `
                <li class="filter-item">
                    <label>
                        <input type="checkbox" name="countries" value="${escapeHtml(country)}">
                        ${escapeHtml(country)}
                    </label>
                </li>
            `).join('');
        }

        // Populate additional tags (APR, LEED, etc.)
        const additionalList = document.querySelector('#additional-content ul');
        if (additionalList && state.filterOptions.additionalTags) {
            additionalList.innerHTML = state.filterOptions.additionalTags.map(tag => `
                <li class="filter-item">
                    <label>
                        <input type="checkbox" name="additional" value="${escapeHtml(tag)}">
                        ${escapeHtml(tag)}
                    </label>
                </li>
            `).join('');
        }

        // Re-attach event listeners to new checkboxes
        attachFilterEventListeners();
    }

    // Load initial certificates
    async function loadInitialCertificates() {
        state.loadedCount = 0;
        state.certificates = [];
        state.hasMore = true;
        await loadCertificates();
    }

    // Load certificates from API
    async function loadCertificates(append = false) {
        if (state.isLoading) return;

        state.isLoading = true;
        showLoadingIndicator();

        console.log('Loading certificates from:', `${state.apiBaseUrl}?${buildApiParams()}`);

        try {
            // Check network connectivity first
            if (!navigator.onLine) {
                throw new Error('No network connection');
            }

            const params = buildApiParams();
            const response = await fetch(`${state.apiBaseUrl}?${params}`, {
                signal: AbortSignal.timeout(30000) // 30 second timeout
            });

            console.log('API Response status:', response.status, response.ok);

            if (response.ok) {
                const data = await response.json();

                console.log('API data received:', data);

                if (append) {
                    state.certificates = [...state.certificates, ...data.certificates];
                    state.loadedCount += data.certificates.length;
                } else {
                    state.certificates = data.certificates;
                    state.loadedCount = data.certificates.length;
                }

                state.totalCount = data.totalCount;
                state.hasMore = state.loadedCount < data.totalCount;

                // Reset retry count on success
                state.retryCount = 0;

                renderCertificates(append);
                updateResultsCount();

                // Show end of results indicator if no more certificates
                if (!state.hasMore && state.certificates.length > 0) {
                    showEndOfResults();
                }
            } else if (response.status >= 500) {
                // Server error - retry
                throw new Error(`Server error: ${response.status}`);
            } else {
                console.error('API response not OK:', response.status, response.statusText);
                showError(`Failed to load certificates: ${response.statusText}`);
            }
        } catch (error) {
            console.error('Error loading certificates:', error);

            // Retry logic for network errors
            if (state.retryCount < state.maxRetries && (error.name === 'TypeError' || error.name === 'AbortError' || error.message.includes('Server error'))) {
                state.retryCount++;
                const delay = state.retryDelay * Math.pow(2, state.retryCount - 1); // Exponential backoff

                console.log(`Retrying in ${delay}ms (attempt ${state.retryCount}/${state.maxRetries})...`);
                showError(`Connection failed. Retrying in ${delay / 1000} seconds... (${state.retryCount}/${state.maxRetries})`);

                setTimeout(() => {
                    state.isLoading = false;
                    loadCertificates(append);
                }, delay);

                return; // Don't hide loading indicator, will retry
            } else {
                // Max retries reached or non-retryable error
                state.retryCount = 0;
                const errorMessage = error.message.includes('No network connection')
                    ? 'No network connection. Please check your internet and try again.'
                    : 'Failed to load certificates. Please try again.';
                showError(errorMessage);
                showRetryButton(append);
            }
        } finally {
            state.isLoading = false;
            hideLoadingIndicator();
        }
    }

    // Build API query parameters
    function buildApiParams() {
        const params = new URLSearchParams();

        if (state.filters.search) {
            params.append('search', state.filters.search);
        }

        if (state.filters.certType.length > 0) {
            params.append('certType', state.filters.certType.join(','));
        }

        if (state.filters.productCategory.length > 0) {
            params.append('productCategory', state.filters.productCategory.join(','));
        }

        if (state.filters.company.length > 0) {
            params.append('company', state.filters.company.join(','));
        }

        if (state.filters.country.length > 0) {
            params.append('country', state.filters.country.join(','));
        }

        if (state.filters.tags.length > 0) {
            params.append('tags', state.filters.tags.join(','));
        }

        params.append('sortBy', state.filters.sortBy);
        params.append('offset', state.loadedCount);
        params.append('limit', state.batchSize);

        return params.toString();
    }

    // Render certificates to the grid
    function renderCertificates(append = false) {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) {
            console.error('Certificate grid not found!');
            return;
        }

        console.log('Rendering certificates:', state.certificates.length, 'append:', append);

        // Handle empty state
        if (state.certificates.length === 0 && !append) {
            showEmptyState();
            return;
        }

        const html = state.certificates.map(cert => createCertificateCard(cert)).join('');

        if (append) {
            // Remove loading/end messages before appending
            const loadingContainer = document.getElementById('loadingContainer');
            const endOfResults = document.getElementById('endOfResults');
            const retryContainer = document.getElementById('retryContainer');
            if (loadingContainer) loadingContainer.remove();
            if (endOfResults) endOfResults.remove();
            if (retryContainer) retryContainer.remove();

            grid.insertAdjacentHTML('beforeend', html);
        } else {
            // Clear empty state if it exists
            const emptyState = document.getElementById('emptyState');
            if (emptyState) emptyState.remove();

            grid.innerHTML = html;
        }

        // Re-add loading/end containers if they were removed
        if (append) {
            grid.insertAdjacentHTML('afterend', `
                <div class="loading-container" id="loadingContainer" style="display: none;">
                    <div class="loading-spinner">
                        <i class="fas fa-spinner fa-spin"></i>
                        <span>Loading certificates...</span>
                    </div>
                </div>
                <div class="end-of-results" id="endOfResults" style="display: none;">
                    <div class="end-message">
                        <i class="fas fa-check-circle"></i>
                        <span>You've reached the end of the results</span>
                    </div>
                </div>
            `);
        }

        // Animate new cards
        animateCards();

        // Also update table view if it's currently visible
        const tableView = document.getElementById('tableView');
        if (tableView && tableView.style.display !== 'none') {
            renderTableRows(state.certificates);
        }
    }

    // Create certificate card HTML
    function createCertificateCard(cert) {
        const certificationPeriod = formatCertificationPeriod(cert.effectiveDate, cert.expirationDate);
        const location = formatLocation(cert.location, cert.country);
        const formattedCertNumber = formatCertificateNumber(cert.certificateNumber || cert.number, cert.effectiveDate);

        return `
            <div class="certificate-card"
                 data-id="${cert.id}"
                 data-type="${escapeHtml(cert.certificationType)}"
                 data-company="${cert.companyName}"
                 data-country="${cert.country}">
                <div class="card-header">
                    <div class="header-row">
                        <div class="certificate-category">${escapeHtml(cert.certificationType)}</div>
                        <div class="certificate-id" onclick="viewCertificateDetails('${cert.id}')" style="cursor: pointer;">${escapeHtml(formattedCertNumber)}</div>
                    </div>
                    <div class="company-name" onclick="viewCertificateDetails('${cert.id}')" style="cursor: pointer;">${escapeHtml(cert.companyName)}</div>
                </div>
                <div class="card-body" onclick="viewCertificateDetails('${cert.id}')" style="cursor: pointer;">
                    <div class="product-info">
                        <div class="product-name">${escapeHtml(cert.productName)}</div>
                        <div class="product-type">${escapeHtml(cert.productType || '')}</div>
                    </div>
                    <div class="certificate-details">
                        <div class="certificate-period">
                            <strong>Certification Period:</strong>
                            ${certificationPeriod}
                        </div>
                        <div class="certificate-location">
                            <i class="fas fa-map-marker-alt"></i>
                            ${location}
                        </div>
                    </div>
                    ${cert.externalTags ? `
                        <div class="certificate-tags">
                            <span class="tag">${escapeHtml(cert.externalTags)}</span>
                        </div>
                    ` : ''}
                </div>
                <div class="card-actions">
                    <div class="action-icon download" title="Download PDF" onclick="event.stopPropagation(); downloadCertificate('${cert.id}')">
                        <i class="fas fa-download"></i>
                    </div>
                    <div class="action-icon external" title="View Details" onclick="event.stopPropagation(); viewCertificate('${cert.certificateNumber || cert.number || cert.id}')">
                        <i class="fas fa-external-link-alt"></i>
                    </div>
                </div>
            </div>
        `;
    }

    // Setup event listeners
    function setupEventListeners() {
        // Search input handled in initializeAutocomplete()
        // Do not set up conflicting listeners here

        // Clear filters button
        const clearFiltersBtn = document.querySelector('.clear-filters');
        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener('click', (e) => {
                e.preventDefault();
                clearAllFilters();
            });
        }

        // Sort dropdown
        const sortDropdownBtn = document.getElementById('sortDropdownBtn');
        if (sortDropdownBtn) {
            sortDropdownBtn.addEventListener('click', toggleSortDropdown);
        }

        // Sort options
        document.querySelectorAll('.sort-option').forEach(option => {
            option.addEventListener('click', () => {
                const sortValue = option.dataset.sort;
                const label = option.textContent.trim();
                selectSort(sortValue, label);
            });
        });

        // Filter toggles
        document.querySelectorAll('.filter-header').forEach(header => {
            header.addEventListener('click', () => {
                const filterId = header.dataset.filter;
                if (filterId) toggleFilter(filterId);
            });
        });
        
        // View toggle buttons
        setupViewToggleButtons();

        attachFilterEventListeners();
    }
    
    // Setup view toggle buttons
    function setupViewToggleButtons() {
        // View type toggle (Tile/Table)
        document.querySelectorAll('.toggle-btn[data-view]').forEach(btn => {
            btn.addEventListener('click', () => {
                const viewType = btn.dataset.view;
                handleViewTypeChange(viewType);
            });
        });
        
        // Access type toggle (External/Internal)
        document.querySelectorAll('.access-toggle-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const accessType = btn.dataset.access;
                handleAccessTypeChange(accessType);
            });
        });
    }
    
    // Handle view type change (Tile/List)
    function handleViewTypeChange(viewType) {
        // Update active state
        document.querySelectorAll('.toggle-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-view="${viewType}"]`).classList.add('active');

        if (viewType === 'list') {
            // Show table view within same page
            showTableView();
        } else {
            // Show tile view
            showTileView();
        }
    }

    // Show table view
    function showTableView() {
        const tileView = document.getElementById('certificatesGrid');
        const tableView = document.getElementById('tableView');

        if (tileView) tileView.style.display = 'none';
        if (tableView) {
            tableView.style.display = 'block';
            populateTableView();
        }
    }

    // Show tile view
    function showTileView() {
        const tileView = document.getElementById('certificatesGrid');
        const tableView = document.getElementById('tableView');

        if (tableView) tableView.style.display = 'none';
        if (tileView) tileView.style.display = 'grid';
    }
    
    // Handle access type change (External/Internal)
    function handleAccessTypeChange(accessType) {
        // Update active state
        document.querySelectorAll('.access-toggle-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-access="${accessType}"]`).classList.add('active');
        
        if (accessType === 'internal') {
            // Redirect to internal tile view
            window.location.href = '/Certificate/CertificateTileViewInternal';
        } else {
            // Redirect to external tile view  
            window.location.href = '/Certificate/CertificateTileViewExternal';
        }
    }

    // Attach event listeners to filter checkboxes
    function attachFilterEventListeners() {
        document.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
            checkbox.addEventListener('change', handleFilterChange);
        });
    }

    // Handle filter checkbox changes
    function handleFilterChange(e) {
        const filterName = e.target.name;
        const value = e.target.value;
        const filterMap = {
            'cert-type': 'certType',
            'product-cat': 'productCategory',
            'company': 'company',
            'countries': 'country',
            'cert-tags': 'tags'
        };

        const filterKey = filterMap[filterName];
        if (!filterKey) return;

        if (e.target.checked) {
            if (!state.filters[filterKey].includes(value)) {
                state.filters[filterKey].push(value);
            }
        } else {
            const index = state.filters[filterKey].indexOf(value);
            if (index > -1) {
                state.filters[filterKey].splice(index, 1);
            }
        }

        loadInitialCertificates();
        updateUrl();
    }

    // Setup infinite scroll
    function setupInfiniteScroll() {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;

        const observer = new IntersectionObserver(
            (entries) => {
                const lastEntry = entries[entries.length - 1];
                if (lastEntry.isIntersecting && !state.isLoading && state.hasMore) {
                    loadCertificates(true);
                }
            },
            {
                rootMargin: '200px'
            }
        );

        // Create sentinel element for infinite scroll
        const sentinel = document.createElement('div');
        sentinel.id = 'scroll-sentinel';
        sentinel.style.height = '1px';
        grid.parentElement.appendChild(sentinel);
        observer.observe(sentinel);
    }

    // Initialize autocomplete
    function initializeAutocomplete() {
        const searchInput = document.querySelector('.search-input');
        if (!searchInput) return;

        const autocompleteContainer = document.createElement('div');
        autocompleteContainer.className = 'autocomplete-dropdown';
        searchInput.parentElement.appendChild(autocompleteContainer);

        // Single event listener for both search and autocomplete
        searchInput.addEventListener('input', async (e) => {
            const term = e.target.value;
            
            // Clear existing debounce timer
            clearTimeout(state.debounceTimer);
            
            // Handle autocomplete suggestions
            if (term.length >= 2) {
                try {
                    const response = await fetch(`${state.apiBaseUrl}/autocomplete?term=${encodeURIComponent(term)}`);
                    if (response.ok) {
                        const data = await response.json();
                        renderAutocomplete(data.suggestions, autocompleteContainer, searchInput);
                    }
                } catch (error) {
                    console.error('Error fetching autocomplete:', error);
                    autocompleteContainer.style.display = 'none';
                }
            } else {
                autocompleteContainer.style.display = 'none';
            }
            
            // Debounced search execution
            state.debounceTimer = setTimeout(() => {
                state.filters.search = term;
                updateUrl(); // Update URL before loading to preserve state
                loadInitialCertificates();
            }, 300);
        });

        // Close autocomplete on outside click
        document.addEventListener('click', (e) => {
            if (!searchInput.contains(e.target) && !autocompleteContainer.contains(e.target)) {
                autocompleteContainer.style.display = 'none';
            }
        });
        
        // Handle keyboard navigation
        searchInput.addEventListener('keydown', (e) => {
            const items = autocompleteContainer.querySelectorAll('.autocomplete-item');
            let selectedIndex = Array.from(items).findIndex(item => item.classList.contains('selected'));
            
            switch (e.key) {
                case 'ArrowDown':
                    e.preventDefault();
                    if (selectedIndex < items.length - 1) {
                        if (selectedIndex >= 0) items[selectedIndex].classList.remove('selected');
                        items[selectedIndex + 1].classList.add('selected');
                    }
                    break;
                case 'ArrowUp':
                    e.preventDefault();
                    if (selectedIndex > 0) {
                        items[selectedIndex].classList.remove('selected');
                        items[selectedIndex - 1].classList.add('selected');
                    }
                    break;
                case 'Enter':
                    e.preventDefault();
                    if (selectedIndex >= 0) {
                        const selectedItem = items[selectedIndex];
                        searchInput.value = selectedItem.dataset.value;
                        state.filters.search = selectedItem.dataset.value;
                        autocompleteContainer.style.display = 'none';
                        updateUrl(); // Update URL before loading to preserve state
                        loadInitialCertificates();
                    }
                    break;
                case 'Escape':
                    autocompleteContainer.style.display = 'none';
                    break;
            }
        });
    }

    // Render autocomplete suggestions
    function renderAutocomplete(suggestions, container, input) {
        if (!suggestions || suggestions.length === 0) {
            container.style.display = 'none';
            return;
        }

        container.innerHTML = suggestions.map((s, index) => `
            <div class="autocomplete-item ${index === 0 ? 'selected' : ''}" data-value="${escapeHtml(s.value)}">
                <span class="autocomplete-label">${escapeHtml(s.label)}</span>
                <span class="autocomplete-type">${escapeHtml(s.type)}</span>
            </div>
        `).join('');

        container.style.display = 'block';

        // Add click handlers
        container.querySelectorAll('.autocomplete-item').forEach(item => {
            item.addEventListener('click', () => {
                input.value = item.dataset.value;
                state.filters.search = item.dataset.value;
                container.style.display = 'none';
                clearTimeout(state.debounceTimer); // Cancel pending search
                updateUrl(); // Update URL before loading to preserve state
                loadInitialCertificates();
            });
            
            // Add hover handlers for better UX
            item.addEventListener('mouseenter', () => {
                container.querySelectorAll('.autocomplete-item').forEach(i => i.classList.remove('selected'));
                item.classList.add('selected');
            });
        });
    }

    // Setup URL parameters
    function setupUrlParameters() {
        const params = new URLSearchParams(window.location.search);
        
        if (params.has('search')) {
            state.filters.search = params.get('search');
            const searchInput = document.querySelector('.search-input');
            if (searchInput) searchInput.value = state.filters.search;
        }
        
        if (params.has('certType')) {
            state.filters.certType = params.get('certType').split(',');
            updateCheckboxes('cert-type', state.filters.certType);
        }
        
        if (params.has('category')) {
            state.filters.productCategory = params.get('category').split(',');
            updateCheckboxes('product-cat', state.filters.productCategory);
        }
        
        if (params.has('company')) {
            state.filters.company = params.get('company').split(',');
            updateCheckboxes('company', state.filters.company);
        }
        
        if (params.has('country')) {
            state.filters.country = params.get('country').split(',');
            updateCheckboxes('countries', state.filters.country);
        }
        
        if (params.has('sort')) {
            state.filters.sortBy = params.get('sort');
            updateSortLabel(state.filters.sortBy);
        }
    }

    // Update URL with current filters
    function updateUrl() {
        const params = new URLSearchParams();
        
        if (state.filters.search) {
            params.set('search', state.filters.search);
        }
        
        if (state.filters.certType.length > 0) {
            params.set('certType', state.filters.certType.join(','));
        }
        
        if (state.filters.productCategory.length > 0) {
            params.set('category', state.filters.productCategory.join(','));
        }
        
        if (state.filters.company.length > 0) {
            params.set('company', state.filters.company.join(','));
        }
        
        if (state.filters.country.length > 0) {
            params.set('country', state.filters.country.join(','));
        }
        
        if (state.filters.sortBy !== 'newest') {
            params.set('sort', state.filters.sortBy);
        }
        
        const newUrl = params.toString() 
            ? `${window.location.pathname}?${params.toString()}`
            : window.location.pathname;
        
        window.history.replaceState({}, '', newUrl);
    }

    // Update checkboxes based on URL parameters
    function updateCheckboxes(name, values) {
        document.querySelectorAll(`input[name="${name}"]`).forEach(checkbox => {
            checkbox.checked = values.includes(checkbox.value);
        });
    }

    // Update sort label
    function updateSortLabel(sortValue) {
        const sortLabels = {
            'newest': 'Newest Effective Date',
            'oldest-effective': 'Oldest Effective Date',
            'expiring': 'Expiring Soon',
            'company-az': 'Company A-Z',
            'product-az': 'Product Name A-Z'
        };
        
        const label = sortLabels[sortValue] || 'Default';
        const sortLabel = document.getElementById('sortLabel');
        if (sortLabel) sortLabel.textContent = label;
    }

    // Clear all filters
    function clearAllFilters() {
        state.filters = {
            search: '',
            certType: [],
            productCategory: [],
            company: [],
            country: [],
            tags: [],
            sortBy: 'newest'
        };
        
        // Clear UI
        document.querySelector('.search-input').value = '';
        document.querySelectorAll('input[type="checkbox"]').forEach(cb => cb.checked = false);
        document.getElementById('sortLabel').textContent = 'Default';
        
        loadInitialCertificates();
        updateUrl();
        showToast('All filters cleared', 'info');
    }

    // Toggle sort dropdown
    function toggleSortDropdown() {
        const dropdown = document.getElementById('sortDropdown');
        if (dropdown) {
            dropdown.classList.toggle('open');
        }
    }

    // Select sort option
    function selectSort(value, label) {
        state.filters.sortBy = value;
        document.getElementById('sortLabel').textContent = label;
        document.getElementById('sortDropdown').classList.remove('open');
        
        loadInitialCertificates();
        updateUrl();
        showToast(`Sorted by ${label}`, 'info');
    }

    // Toggle filter panel
    function toggleFilter(filterId) {
        const content = document.getElementById(filterId + '-content');
        const header = content.previousElementSibling;
        const icon = header.querySelector('i');
        
        if (content.style.display === 'none') {
            content.style.display = 'block';
            icon.className = 'fas fa-chevron-up';
        } else {
            content.style.display = 'none';
            icon.className = 'fas fa-chevron-down';
        }
    }

    // Update results count
    function updateResultsCount() {
        const resultsCount = document.getElementById('resultsCount');
        const announcement = document.getElementById('searchAnnouncement');

        if (resultsCount) {
            const countText = `${state.totalCount} Result${state.totalCount !== 1 ? 's' : ''}`;
            resultsCount.textContent = countText;

            // Announce to screen readers
            if (announcement) {
                announcement.textContent = `Search updated. ${countText} found.`;
            }
        }
    }

    // View certificate details (main card click - new tab per requirement)
    window.viewCertificateDetails = function(id) {
        if (!id) {
            console.error('Certificate ID is missing');
            showToast('Error: Certificate ID not found', 'error');
            return;
        }
        console.log('Opening certificate in new tab:', id);
        // Encode URI to handle certificate numbers with special chars like "24-0366"
        window.open(`/Certificate/CertificateSingleView/${encodeURIComponent(id)}`, '_blank');
    };

    // View certificate (external link - new tab)
    function viewCertificate(id) {
        if (!id) {
            console.error('Certificate ID is missing');
            showToast('Error: Certificate ID not found', 'error');
            return;
        }
        console.log('Opening certificate in new tab:', id);
        // Encode URI to handle certificate numbers with special chars like "24-0366"
        window.open(`/Certificate/CertificateSingleView/${encodeURIComponent(id)}`, '_blank');
    }
    
    // Make it globally accessible
    window.viewCertificate = viewCertificate;

    // Download certificate
    async function downloadCertificate(id) {
        try {
            const response = await fetch(`/Certificate/Download/${id}`);

            if (response.ok) {
                const blob = await response.blob();

                // Extract filename from Content-Disposition header or use certificate number
                let filename = `Certificate_${id}.pdf`; // Fallback
                const contentDisposition = response.headers.get('Content-Disposition');
                if (contentDisposition) {
                    const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
                    if (filenameMatch && filenameMatch[1]) {
                        filename = filenameMatch[1].replace(/['"]/g, '');
                    }
                }

                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = filename;
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                document.body.removeChild(a);
                showToast('Certificate download started', 'success');
            } else {
                showToast('Certificate PDF not available', 'error');
            }
        } catch (error) {
            console.error('Error downloading certificate:', error);
            showToast('Failed to download certificate', 'error');
        }
    }
    
    // Make it globally accessible
    window.downloadCertificate = downloadCertificate;

    // Utility functions
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text || '';
        return div.innerHTML;
    }

    function formatCertificationPeriod(effectiveDate, expirationDate) {
        const formatDate = (dateStr) => {
            if (!dateStr) return 'N/A';
            const date = new Date(dateStr);
            return date.toLocaleDateString('en-US', { 
                year: 'numeric', 
                month: 'numeric', 
                day: 'numeric' 
            });
        };
        
        return `${formatDate(effectiveDate)} - ${formatDate(expirationDate)}`;
    }

    function formatLocation(location, country) {
        const parts = [];
        if (location) parts.push(location);
        if (country) parts.push(country);
        return parts.join(', ') || 'N/A';
    }

    // Format certificate number to YY-#### format
    function formatCertificateNumber(certificateNumber, effectiveDate) {
        if (!certificateNumber) return 'N/A';

        // If already in correct format, return as is
        if (/^\d{2}-\d{4}$/.test(certificateNumber)) {
            return certificateNumber;
        }

        // Try to extract year from effective date
        let year = '';
        if (effectiveDate) {
            const date = new Date(effectiveDate);
            if (!isNaN(date.getTime())) {
                year = date.getFullYear().toString().slice(-2);
            }
        }

        // If we can't get year, use current year
        if (!year) {
            year = new Date().getFullYear().toString().slice(-2);
        }

        // If certificate number is just a number, format it as YY-####
        if (/^\d+$/.test(certificateNumber)) {
            const paddedNumber = certificateNumber.padStart(4, '0');
            return `${year}-${paddedNumber}`;
        }

        // If it's something else, try to extract numbers and format
        const numbers = certificateNumber.replace(/\D/g, '');
        if (numbers) {
            const paddedNumber = numbers.slice(-4).padStart(4, '0');
            return `${year}-${paddedNumber}`;
        }

        return certificateNumber; // Return as-is if we can't format it
    }

    function showLoadingIndicator() {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;
        
        const loader = document.createElement('div');
        loader.className = 'loading-indicator';
        loader.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Loading certificates...';
        loader.id = 'loading-indicator';
        
        if (!document.getElementById('loading-indicator')) {
            grid.parentElement.appendChild(loader);
        }
    }

    function hideLoadingIndicator() {
        const loader = document.getElementById('loading-indicator');
        if (loader) {
            loader.remove();
        }
    }

    function animateCards() {
        const cards = document.querySelectorAll('.certificate-card:not(.animated)');
        cards.forEach((card, index) => {
            setTimeout(() => {
                card.classList.add('animated', 'fadeInUp');
            }, index * 50);
        });
    }

    function showToast(message, type = 'success') {
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
            <span>${message}</span>
        `;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);
        
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                if (document.body.contains(toast)) {
                    document.body.removeChild(toast);
                }
            }, 300);
        }, 3000);
    }

    function showError(message) {
        showToast(message, 'error');
    }

    // Show empty state when no certificates found
    function showEmptyState() {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;

        const hasActiveFilters = state.filters.search ||
                                 state.filters.certType.length > 0 ||
                                 state.filters.productCategory.length > 0 ||
                                 state.filters.company.length > 0 ||
                                 state.filters.country.length > 0 ||
                                 state.filters.tags.length > 0;

        const emptyStateHtml = `
            <div id="emptyState" class="empty-state" style="
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                padding: 60px 20px;
                text-align: center;
                grid-column: 1 / -1;
                background: #f8f9fa;
                border-radius: 8px;
                margin: 20px 0;
            ">
                <i class="fas fa-search" style="font-size: 64px; color: #ccc; margin-bottom: 20px;"></i>
                <h3 style="color: #333; margin-bottom: 10px;">No certificates found</h3>
                <p style="color: #666; margin-bottom: 20px;">
                    ${hasActiveFilters
                        ? 'Try adjusting your filters or search terms to find what you\'re looking for.'
                        : 'There are no certificates available at this time.'}
                </p>
                ${hasActiveFilters ? `
                    <button onclick="certificateTileView.clearAllFilters()" class="btn btn-primary" style="
                        padding: 10px 24px;
                        background: #007bff;
                        color: white;
                        border: none;
                        border-radius: 4px;
                        cursor: pointer;
                        font-size: 14px;
                    ">
                        Clear All Filters
                    </button>
                ` : ''}
            </div>
        `;

        grid.innerHTML = emptyStateHtml;
        updateResultsCount();
    }

    // Show end of results indicator
    function showEndOfResults() {
        const endOfResults = document.getElementById('endOfResults');
        if (endOfResults) {
            endOfResults.style.display = 'block';
        }
    }

    // Show retry button after failed load
    function showRetryButton(append) {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;

        const retryHtml = `
            <div id="retryContainer" class="retry-container" style="
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                padding: 40px 20px;
                text-align: center;
                grid-column: 1 / -1;
            ">
                <i class="fas fa-exclamation-triangle" style="font-size: 48px; color: #dc3545; margin-bottom: 16px;"></i>
                <p style="color: #666; margin-bottom: 20px;">Failed to load certificates. Please try again.</p>
                <button onclick="certificateTileView.retryLoad(${append})" class="btn btn-primary" style="
                    padding: 10px 24px;
                    background: #007bff;
                    color: white;
                    border: none;
                    border-radius: 4px;
                    cursor: pointer;
                    font-size: 14px;
                ">
                    <i class="fas fa-redo"></i> Retry
                </button>
            </div>
        `;

        if (append) {
            grid.insertAdjacentHTML('afterend', retryHtml);
        } else {
            grid.innerHTML = retryHtml;
        }
    }

    // Retry loading certificates
    function retryLoad(append) {
        const retryContainer = document.getElementById('retryContainer');
        if (retryContainer) retryContainer.remove();

        state.retryCount = 0; // Reset retry count
        loadCertificates(append);
    }

    function copyToClipboard(text) {
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.style.position = 'fixed';
        textarea.style.opacity = '0';
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand('copy');
        document.body.removeChild(textarea);
    }

    // Populate table view with certificates
    function populateTableView() {
        const gridBody = document.getElementById('gridBody');
        if (!gridBody) return;

        // Clear existing content
        gridBody.innerHTML = '';

        // Show loading state
        gridBody.innerHTML = '<div class="loading-state">Loading certificates...</div>';

        // Use current certificates data
        if (state.certificates && state.certificates.length > 0) {
            renderTableRows(state.certificates);
        } else {
            // If no data, fetch fresh data
            loadCertificates();
        }
    }

    // Render table rows for certificates
    function renderTableRows(certificates) {
        const gridBody = document.getElementById('gridBody');
        if (!gridBody) return;

        if (!certificates || certificates.length === 0) {
            gridBody.innerHTML = '<div class="no-results">No certificates found</div>';
            return;
        }

        gridBody.innerHTML = certificates.map(cert => createTableRow(cert)).join('');
    }

    // Create table row HTML for a certificate
    function createTableRow(cert) {
        const effectiveDate = cert.effectiveDate ? new Date(cert.effectiveDate).toLocaleDateString() : 'N/A';
        const expirationDate = cert.expirationDate ? new Date(cert.expirationDate).toLocaleDateString() : 'N/A';
        const location = formatLocation(cert.location, cert.country);

        return `
            <div class="grid-row" data-cert-id="${cert.id}">
                <div class="grid-cell actions-column">
                    <div class="action-buttons">
                        <button class="action-btn edit-btn" onclick="editCertificate('${cert.id}')" title="Edit Certificate">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="action-btn download-btn" onclick="downloadCertificate('${cert.id}')" title="Download PDF">
                            <i class="fas fa-download"></i>
                        </button>
                        <button class="action-btn view-btn" onclick="viewCertificateDetails('${cert.id}')" title="View Details">
                            <i class="fas fa-external-link-alt"></i>
                        </button>
                    </div>
                </div>
                <div class="grid-cell">${escapeHtml(cert.certificateNumber || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.certificationType || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.csfAttributes || '')}</div>
                <div class="grid-cell">
                    <span class="status-badge status-${(cert.status || '').toLowerCase().replace(/\s+/g, '-')}">${escapeHtml(cert.status || '')}</span>
                </div>
                <div class="grid-cell">${effectiveDate}</div>
                <div class="grid-cell">${expirationDate}</div>
                <div class="grid-cell">${escapeHtml(cert.companyName || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.companyContact || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.productName || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.productCategory || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.productType || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.productSubcategory || '')}</div>
                <div class="grid-cell">${location}</div>
                <div class="grid-cell">${escapeHtml(cert.country || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.externalTags || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.internalTags || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.asin || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.upc || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.gtin || '')}</div>
                <div class="grid-cell">${escapeHtml(cert.ean || '')}</div>
            </div>
        `;
    }

    // Table action handlers simplified - checkboxes removed

    // Edit certificate function
    window.editCertificate = function(id) {
        if (!id) {
            console.error('Certificate ID is missing');
            showToast('Error: Certificate ID not found', 'error');
            return;
        }
        console.log('Editing certificate:', id);
        window.location.href = `/Certificate/Edit/${id}`;
    };

    // Export functions for global access as object as well
    window.certificateTileView = {
        viewCertificate: viewCertificate,
        downloadCertificate: downloadCertificate,
        clearAllFilters: clearAllFilters,
        editCertificate: editCertificate,
        populateTableView: populateTableView,
        retryLoad: retryLoad
    };

    // Initialize the app when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }
})();