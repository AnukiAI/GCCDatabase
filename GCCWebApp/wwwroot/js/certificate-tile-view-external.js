// Certificate Tile View External JavaScript
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
            partner: [],
            sortBy: 'newest'
        },
        currentPage: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
        isLoading: false,
        hasMore: true,
        filterOptions: {},
        debounceTimer: null
    };

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', initialize);

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
            const response = await fetch('/api/certificate-tiles-external/filters');
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
            certTypeList.innerHTML = state.filterOptions.certificationTypes.map(type => `
                <li class="filter-item">
                    <label>
                        <input type="checkbox" name="cert-type" value="${escapeHtml(type)}">
                        ${escapeHtml(type)}
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

        // Populate partners (External specific)
        const partnerList = document.querySelector('#partner-content ul');
        if (partnerList && state.filterOptions.partners) {
            partnerList.innerHTML = state.filterOptions.partners.map(partner => `
                <li class="filter-item">
                    <label>
                        <input type="checkbox" name="partner" value="${escapeHtml(partner)}">
                        ${escapeHtml(partner)}
                    </label>
                </li>
            `).join('');
        }

        // Re-attach event listeners to new checkboxes
        attachFilterEventListeners();
    }

    // Load initial certificates
    async function loadInitialCertificates() {
        state.currentPage = 1;
        state.certificates = [];
        await loadCertificates();
    }

    // Load certificates from API
    async function loadCertificates(append = false) {
        if (state.isLoading) return;

        state.isLoading = true;
        showLoadingIndicator();

        try {
            const params = buildApiParams();
            const response = await fetch(`/api/certificate-tiles-external?${params}`);
            
            if (response.ok) {
                const data = await response.json();
                
                if (append) {
                    state.certificates = [...state.certificates, ...data.certificates];
                } else {
                    state.certificates = data.certificates;
                }
                
                state.totalCount = data.totalCount;
                state.totalPages = data.totalPages;
                state.hasMore = data.hasMore;
                
                renderCertificates(append);
                updateResultsCount();
            } else if (response.status === 403) {
                showError('Access denied. You may not have permission to view these certificates.');
            }
        } catch (error) {
            console.error('Error loading certificates:', error);
            showError('Failed to load certificates. Please try again.');
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
        
        if (state.filters.partner.length > 0) {
            params.append('partner', state.filters.partner.join(','));
        }
        
        params.append('sortBy', state.filters.sortBy);
        params.append('page', state.currentPage);
        params.append('pageSize', state.pageSize);
        
        return params.toString();
    }

    // Render certificates to the grid
    function renderCertificates(append = false) {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;

        // Check if we're in table view or tile view
        const isTableView = grid.classList.contains('table-view');
        
        if (isTableView) {
            renderCertificatesTable();
        } else {
            if (append) {
                const html = state.certificates.map(cert => createCertificateCard(cert)).join('');
                grid.insertAdjacentHTML('beforeend', html);
            } else {
                renderCertificatesTiles();
            }
        }
    }

    // Create certificate card HTML (External view - simplified)
    function createCertificateCard(cert) {
        const certificationPeriod = formatCertificationPeriod(cert.effectiveDate, cert.expirationDate);
        const location = formatLocation(cert.location, cert.country);
        const partnerBadge = cert.partner ? `<span class="partner-badge external">${escapeHtml(cert.partner)}</span>` : '';
        
        return `
            <div class="certificate-card external-card" 
                 data-id="${cert.id}"
                 data-type="${escapeHtml(cert.certificationType)}" 
                 data-company="${cert.companyName}" 
                 data-country="${cert.country}"
                 onclick="viewCertificateDetails('${cert.id}')">
                <div class="card-header">
                    <div class="header-row">
                        <div class="certificate-category">${escapeHtml(cert.certificationType)}</div>
                        <div class="certificate-id">${escapeHtml(cert.certificateNumber)}</div>
                    </div>
                    <div class="company-name">${escapeHtml(cert.companyName)}</div>
                    ${partnerBadge ? `<div class="partner-row">${partnerBadge}</div>` : ''}
                </div>
                <div class="card-body">
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
                            <span class="tag external">${escapeHtml(cert.externalTags)}</span>
                        </div>
                    ` : ''}
                </div>
                <div class="card-actions">
                    <div class="action-icon download" title="Download PDF" onclick="event.stopPropagation(); downloadCertificate('${cert.id}')">
                        <i class="fas fa-download"></i>
                    </div>
                    <div class="action-icon external" title="View Details" onclick="event.stopPropagation(); viewCertificate('${cert.id}')">
                        <i class="fas fa-external-link-alt"></i>
                    </div>
                </div>
            </div>
        `;
    }

    // Render certificates in tile format (default)
    function renderCertificatesTiles() {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;

        grid.innerHTML = state.certificates.map(cert => createCertificateCard(cert)).join('');
        animateCards();
    }

    // Render certificates in table format
    function renderCertificatesTable() {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;

        const tableHtml = `
            <div class="table-container">
                <table class="certificates-table">
                    <thead>
                        <tr>
                            <th>Certificate #</th>
                            <th>Company</th>
                            <th>Product</th>
                            <th>Type</th>
                            <th>Effective Date</th>
                            <th>Expiration Date</th>
                            <th>Location</th>
                            <th>Partner</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${state.certificates.map(cert => createCertificateTableRow(cert)).join('')}
                    </tbody>
                </table>
            </div>
        `;
        
        grid.innerHTML = tableHtml;
    }

    // Create certificate table row
    function createCertificateTableRow(cert) {
        const partnerBadge = cert.partner ? `<span class="partner-badge external">${escapeHtml(cert.partner)}</span>` : '';
        const location = formatLocation(cert.location, cert.country);
        
        return `
            <tr class="certificate-row" data-id="${cert.id}" onclick="viewCertificateDetails('${cert.id}')">
                <td class="cert-number">${escapeHtml(cert.certificateNumber)}</td>
                <td class="company-name">${escapeHtml(cert.companyName)}</td>
                <td class="product-name">${escapeHtml(cert.productName)}</td>
                <td class="cert-type">${escapeHtml(cert.certificationType)}</td>
                <td class="effective-date">${formatDate(cert.effectiveDate)}</td>
                <td class="expiration-date">${formatDate(cert.expirationDate)}</td>
                <td class="location">${location}</td>
                <td class="partner">${partnerBadge}</td>
                <td class="actions">
                    <button class="btn-icon" title="View Details" onclick="event.stopPropagation(); viewCertificateDetails('${cert.id}')">
                        <i class="fas fa-eye"></i>
                    </button>
                    <button class="btn-icon" title="Download PDF" onclick="event.stopPropagation(); downloadCertificate('${cert.id}')">
                        <i class="fas fa-download"></i>
                    </button>
                </td>
            </tr>
        `;
    }

    // Setup event listeners
    function setupEventListeners() {
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

        // Toggle buttons
        setupToggleButtons();

        attachFilterEventListeners();
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
            'cert-tags': 'tags',
            'partner': 'partner'
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
                    state.currentPage++;
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
                    const response = await fetch(`/api/certificate-tiles-external/autocomplete?term=${encodeURIComponent(term)}`);
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
                loadInitialCertificates();
                updateUrl();
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
                        loadInitialCertificates();
                        updateUrl();
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
                clearTimeout(state.debounceTimer);
                loadInitialCertificates();
                updateUrl();
            });
            
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
        
        if (params.has('partner')) {
            state.filters.partner = params.get('partner').split(',');
            updateCheckboxes('partner', state.filters.partner);
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
        
        if (state.filters.search) params.set('search', state.filters.search);
        if (state.filters.certType.length > 0) params.set('certType', state.filters.certType.join(','));
        if (state.filters.productCategory.length > 0) params.set('category', state.filters.productCategory.join(','));
        if (state.filters.company.length > 0) params.set('company', state.filters.company.join(','));
        if (state.filters.country.length > 0) params.set('country', state.filters.country.join(','));
        if (state.filters.tags.length > 0) params.set('tags', state.filters.tags.join(','));
        if (state.filters.partner.length > 0) params.set('partner', state.filters.partner.join(','));
        if (state.filters.sortBy !== 'newest') params.set('sort', state.filters.sortBy);
        
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
            'product-az': 'Product Name A-Z',
            'partner': 'Partner A-Z'
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
            partner: [],
            sortBy: 'newest'
        };
        
        // Clear UI
        document.querySelector('.search-input').value = '';
        document.querySelectorAll('input[type="checkbox"]').forEach(cb => cb.checked = false);
        document.getElementById('sortLabel').textContent = 'Newest Effective Date';
        
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

    // Update results count
    function updateResultsCount() {
        const resultsCount = document.getElementById('resultsCount');
        if (resultsCount) {
            resultsCount.textContent = `${state.totalCount} Result${state.totalCount !== 1 ? 's' : ''}`;
        }
    }

    // Certificate actions (External view - limited actions)
    window.viewCertificateDetails = function(id) {
        if (!id) {
            console.error('Certificate ID is missing');
            showToast('Error: Certificate ID not found', 'error');
            return;
        }
        window.location.href = `/Certificate/CertificateExternalView/${id}`;
    };

    function viewCertificate(id) {
        // Encode URI to handle certificate numbers with special chars like "24-0366"
        window.open(`/Certificate/CertificateExternalView/${encodeURIComponent(id)}`, '_blank');
    }

    window.viewCertificate = viewCertificate;

    // Download certificate (External view - with access control)
    async function downloadCertificate(id) {
        try {
            const response = await fetch(`/api/certificate-tiles-external/${id}/download`);

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
            } else if (response.status === 403) {
                showToast('You do not have permission to download this certificate', 'error');
            } else if (response.status === 404) {
                showToast('Certificate PDF not available', 'error');
            } else {
                showToast('Failed to download certificate', 'error');
            }
        } catch (error) {
            console.error('Error downloading certificate:', error);
            showToast('Failed to download certificate', 'error');
        }
    }
    
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

    // Setup toggle buttons functionality
    function setupToggleButtons() {
        // View type toggle buttons (Tile/Table)
        document.querySelectorAll('.toggle-btn[data-view]').forEach(btn => {
            btn.addEventListener('click', () => {
                const viewType = btn.dataset.view;
                handleViewTypeChange(viewType);
            });
        });
    }

    function handleViewTypeChange(viewType) {
        // Update active button state
        document.querySelectorAll('.toggle-btn[data-view]').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-view="${viewType}"]`).classList.add('active');

        // Get the certificates grid container
        const gridContainer = document.getElementById('certificatesGrid');
        if (!gridContainer) return;

        if (viewType === 'table') {
            // Switch to table view
            gridContainer.classList.add('table-view');
            gridContainer.classList.remove('tile-view');
            renderCertificatesTable();
        } else {
            // Switch to tile view (default)
            gridContainer.classList.remove('table-view');
            gridContainer.classList.add('tile-view');
            renderCertificatesTiles();
        }
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

    // Export functions for global access
    window.certificateTileViewExternal = {
        viewCertificate: viewCertificate,
        downloadCertificate: downloadCertificate,
        clearAllFilters: clearAllFilters
    };

    // Initialize the app when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }
})();