// Certificate Tile View Internal JavaScript
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
            status: [],
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
            const response = await fetch('/api/certificate-tiles-internal/filters');
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

        // Populate hierarchical product categories
        const productCategoryList = document.querySelector('#product-cat-content ul');
        if (productCategoryList && state.filterOptions.productCategoriesHierarchy) {
            let html = '';

            state.filterOptions.productCategoriesHierarchy.forEach(category => {
                // Level 1: Category
                html += `
                    <li class="filter-item filter-level-1">
                        <label>
                            <input type="checkbox" name="product-cat" value="${escapeHtml(category.name)}">
                            ${escapeHtml(category.name)}
                        </label>
                    </li>`;

                // Level 2: Types
                if (category.types && category.types.length > 0) {
                    category.types.forEach(type => {
                        html += `
                            <li class="filter-item filter-level-2">
                                <label>
                                    <input type="checkbox" name="product-cat" value="${escapeHtml(type.name)}">
                                    ${escapeHtml(type.name)}
                                </label>
                            </li>`;

                        // Level 3: Subcategories
                        if (type.subcategories && type.subcategories.length > 0) {
                            type.subcategories.forEach(subcat => {
                                html += `
                                    <li class="filter-item filter-level-3">
                                        <label>
                                            <input type="checkbox" name="product-cat" value="${escapeHtml(subcat.name)}">
                                            ${escapeHtml(subcat.name)}
                                        </label>
                                    </li>`;
                            });
                        }
                    });
                }
            });

            productCategoryList.innerHTML = html;
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

        // Populate statuses (Internal specific)
        const statusList = document.querySelector('#status-content ul');
        if (statusList && state.filterOptions.statuses) {
            statusList.innerHTML = state.filterOptions.statuses.map(status => `
                <li class="filter-item">
                    <label>
                        <input type="checkbox" name="status" value="${escapeHtml(status)}">
                        ${escapeHtml(status)}
                    </label>
                </li>
            `).join('');
        }

        // Populate partners (Internal specific)
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
            const response = await fetch(`/api/certificate-tiles-internal?${params}`);
            
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
        
        if (state.filters.status.length > 0) {
            params.append('status', state.filters.status.join(','));
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

        // Animate new cards if in tile view
        if (!isTableView) {
            animateCards();
        }
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
                            <th>Status</th>
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

    // Create certificate table row for internal view
    function createCertificateTableRow(cert) {
        const statusBadge = getStatusBadge(cert.status);
        const partnerBadge = cert.partner ? `<span class="partner-badge internal">${escapeHtml(cert.partner)}</span>` : '';
        const location = formatLocation(cert.location, cert.country);
        
        return `
            <tr class="certificate-row" data-id="${cert.id}" onclick="viewCertificateDetails('${cert.id}')">
                <td class="cert-number">${escapeHtml(cert.certificateNumber)}</td>
                <td class="company-name">${escapeHtml(cert.companyName)}</td>
                <td class="product-name">${escapeHtml(cert.productName)}</td>
                <td class="cert-type">${escapeHtml(cert.certificationType)}</td>
                <td class="status">${statusBadge}</td>
                <td class="effective-date">${formatDate(cert.effectiveDate)}</td>
                <td class="expiration-date">${formatDate(cert.expirationDate)}</td>
                <td class="location">${location}</td>
                <td class="partner">${partnerBadge}</td>
                <td class="actions">
                    <button class="btn-icon" title="View Details" onclick="event.stopPropagation(); viewCertificateDetails('${cert.id}')">
                        <i class="fas fa-eye"></i>
                    </button>
                    <button class="btn-icon" title="Edit Certificate" onclick="event.stopPropagation(); editCertificate('${cert.id}')">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn-icon" title="Download PDF" onclick="event.stopPropagation(); downloadCertificate('${cert.id}')">
                        <i class="fas fa-download"></i>
                    </button>
                </td>
            </tr>
        `;
    }

    // Create certificate card HTML (Enhanced for internal view)
    function createCertificateCard(cert) {
        const certificationPeriod = formatCertificationPeriod(cert.effectiveDate, cert.expirationDate);
        const location = formatLocation(cert.location, cert.country);
        const statusBadge = getStatusBadge(cert.status);
        const partnerBadge = cert.partner ? `<span class="partner-badge">${escapeHtml(cert.partner)}</span>` : '';
        
        return `
            <div class="certificate-card internal-card" 
                 data-id="${cert.id}"
                 data-type="${escapeHtml(cert.certificationType)}" 
                 data-company="${cert.companyName}" 
                 data-country="${cert.country}"
                 data-status="${cert.status}"
                 onclick="viewCertificateDetails('${cert.id}')">
                <div class="card-header">
                    <div class="header-row">
                        <div class="certificate-category">${escapeHtml(cert.certificationType)}</div>
                        <div class="certificate-id">${escapeHtml(cert.certificateNumber)}</div>
                    </div>
                    <div class="company-name">${escapeHtml(cert.companyName)}</div>
                    <div class="status-row">
                        ${statusBadge}
                        ${partnerBadge}
                    </div>
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
                        ${cert.createdDate ? `
                            <div class="certificate-dates">
                                <small class="text-muted">
                                    Created: ${formatDate(cert.createdDate)} | Modified: ${formatDate(cert.modifiedDate)}
                                </small>
                            </div>
                        ` : ''}
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
                    <div class="action-icon external" title="View Details" onclick="event.stopPropagation(); viewCertificate('${cert.id}')">
                        <i class="fas fa-external-link-alt"></i>
                    </div>
                    <div class="action-icon edit" title="Edit Certificate" onclick="event.stopPropagation(); editCertificate('${cert.id}')">
                        <i class="fas fa-edit"></i>
                    </div>
                </div>
            </div>
        `;
    }

    // Get status badge HTML
    function getStatusBadge(status) {
        const statusClasses = {
            'Active': 'status-active',
            'Valid': 'status-valid',
            'Expired': 'status-expired',
            'Pending': 'status-pending',
            'Revoked': 'status-revoked',
            'Draft': 'status-draft'
        };
        
        const cssClass = statusClasses[status] || 'status-default';
        return `<span class="status-badge ${cssClass}">${escapeHtml(status)}</span>`;
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

        // Sort dropdown - target the button inside the sortDropdown container
        const sortDropdown = document.getElementById('sortDropdown');
        const sortDropdownBtn = sortDropdown?.querySelector('.sort-dropdown-button');
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

        // Export and download buttons
        setupExportDownloadButtons();

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
            'status': 'status',
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
                    const response = await fetch(`/api/certificate-tiles-internal/autocomplete?term=${encodeURIComponent(term)}`);
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
        
        if (params.has('status')) {
            state.filters.status = params.get('status').split(',');
            updateCheckboxes('status', state.filters.status);
        }
        
        if (params.has('partner')) {
            state.filters.partner = params.get('partner').split(',');
            updateCheckboxes('partner', state.filters.partner);
        }
        
        // ... other URL parameter handling similar to public view
    }

    // Update URL with current filters
    function updateUrl() {
        const params = new URLSearchParams();
        
        if (state.filters.search) params.set('search', state.filters.search);
        if (state.filters.status.length > 0) params.set('status', state.filters.status.join(','));
        if (state.filters.partner.length > 0) params.set('partner', state.filters.partner.join(','));
        // ... other parameters
        
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

    // Clear all filters
    function clearAllFilters() {
        state.filters = {
            search: '',
            certType: [],
            productCategory: [],
            company: [],
            country: [],
            tags: [],
            status: [],
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
    function toggleSortDropdown(event) {
        event?.stopPropagation();
        const dropdown = document.getElementById('sortDropdown');
        if (dropdown) {
            const isOpen = dropdown.classList.contains('open');
            dropdown.classList.toggle('open');

            // Add/remove click outside listener
            if (!isOpen) {
                setTimeout(() => {
                    document.addEventListener('click', closeSortDropdownOutside);
                }, 0);
            } else {
                document.removeEventListener('click', closeSortDropdownOutside);
            }
        }
    }

    // Close sort dropdown when clicking outside
    function closeSortDropdownOutside(event) {
        const dropdown = document.getElementById('sortDropdown');
        if (dropdown && !dropdown.contains(event.target)) {
            dropdown.classList.remove('open');
            document.removeEventListener('click', closeSortDropdownOutside);
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

    // Certificate actions
    window.viewCertificateDetails = function(id) {
        if (!id) {
            console.error('Certificate ID is missing');
            showToast('Error: Certificate ID not found', 'error');
            return;
        }
        window.location.href = `/Certificate/CertificateInternalView/${id}`;
    };

    function viewCertificate(id) {
        // Encode URI to handle certificate numbers with special chars like "24-0366"
        window.open(`/Certificate/CertificateInternalView/${encodeURIComponent(id)}`, '_blank');
    }

    function editCertificate(id) {
        window.location.href = `/Certificate/Edit/${id}`;
    }

    window.viewCertificate = viewCertificate;
    window.editCertificate = editCertificate;

    // Download certificate
    async function downloadCertificate(id) {
        try {
            const response = await fetch(`/api/certificate-tiles-internal/${id}/download`);

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

    function formatDate(dateStr) {
        if (!dateStr) return 'N/A';
        const date = new Date(dateStr);
        return date.toLocaleDateString('en-US', { 
            year: 'numeric', 
            month: 'short', 
            day: 'numeric' 
        });
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

        // Setup select all checkbox handler
        setupSelectAllHandler();
    }

    // Setup select all checkbox handler
    function setupSelectAllHandler() {
        const selectAllCheckbox = document.getElementById('selectAll');
        if (selectAllCheckbox) {
            selectAllCheckbox.addEventListener('change', function() {
                const isChecked = this.checked;
                const rowCheckboxes = document.querySelectorAll('.row-checkbox');
                rowCheckboxes.forEach(checkbox => {
                    checkbox.checked = isChecked;
                });
                updateBulkActionsBar();
            });
        }
    }

    // Setup export and download button handlers
    function setupExportDownloadButtons() {
        const downloadSelectedBtn = document.getElementById('downloadSelectedBtn');
        const exportAllBtn = document.getElementById('exportAllBtn');

        if (downloadSelectedBtn) {
            downloadSelectedBtn.addEventListener('click', handleDownloadSelected);
        }

        if (exportAllBtn) {
            exportAllBtn.addEventListener('click', handleExportAll);
        }

        // Setup bulk action buttons (clear selection only)
        setupBulkActionButtons();
    }

    // Setup bulk action buttons in the bulk actions bar
    function setupBulkActionButtons() {
        const clearSelectionBtn = document.querySelector('[data-action="clear-selection"]');

        if (clearSelectionBtn) {
            clearSelectionBtn.addEventListener('click', () => {
                document.querySelectorAll('.row-checkbox').forEach(cb => cb.checked = false);
                const selectAllCheckbox = document.getElementById('selectAll');
                if (selectAllCheckbox) selectAllCheckbox.checked = false;
                updateBulkActionsBar();
                updateDownloadSelectedButton();
            });
        }
    }

    // Handle export selected certificates to Excel
    async function handleExportSelected() {
        // Check if we're in table view and have selections
        const selectedCheckboxes = document.querySelectorAll('.row-checkbox:checked');

        if (selectedCheckboxes.length === 0) {
            showToast('Please switch to table view and select certificates to export', 'warning');
            return;
        }

        const selectedIds = Array.from(selectedCheckboxes).map(cb => cb.dataset.certId);

        try {
            showToast(`Exporting ${selectedIds.length} certificate(s)...`, 'info');

            // Build export URL with selected IDs as query params
            const params = new URLSearchParams();
            params.append('format', 'excel');

            // For now, export with current filters (selected export for specific IDs would need a new endpoint)
            // This is a simplified version - you may want to add a dedicated endpoint for selected exports
            const url = `/Certificate/ExportCertificates?${params.toString()}`;

            window.location.href = url;

            setTimeout(() => {
                showToast(`Export started for ${selectedIds.length} certificate(s)`, 'success');
            }, 1000);
        } catch (error) {
            console.error('Error exporting certificates:', error);
            showToast('Failed to export certificates', 'error');
        }
    }

    // Handle export all certificates to Excel
    async function handleExportAll() {
        try {
            showToast('Preparing export...', 'info');

            // Build export URL with current filters
            const params = new URLSearchParams();
            params.append('format', 'excel');

            // Add filter parameters
            if (state.filters.search) {
                params.append('searchTerm', state.filters.search);
            }

            if (state.filters.certType.length > 0) {
                state.filters.certType.forEach(type => params.append('certificationTypes', type));
            }

            if (state.filters.country.length > 0) {
                state.filters.country.forEach(country => params.append('countries', country));
            }

            if (state.filters.status.length > 0) {
                state.filters.status.forEach(status => params.append('statuses', status));
            }

            params.append('sort', state.filters.sortBy);

            const url = `/Certificate/ExportCertificates?${params.toString()}`;

            // Open in new window to trigger download
            window.location.href = url;

            setTimeout(() => {
                showToast(`Exporting ${state.totalCount} certificate(s) to Excel`, 'success');
            }, 1000);
        } catch (error) {
            console.error('Error exporting certificates:', error);
            showToast('Failed to export certificates', 'error');
        }
    }

    // Handle download selected certificates as ZIP file
    async function handleDownloadSelected() {
        const selectedCheckboxes = document.querySelectorAll('.row-checkbox:checked');

        if (selectedCheckboxes.length === 0) {
            showToast('Please switch to table view and select certificates to download', 'warning');
            return;
        }

        const selectedIds = Array.from(selectedCheckboxes).map(cb => parseInt(cb.dataset.certId));
        showToast(`Preparing ZIP file with ${selectedIds.length} certificate(s)...`, 'info');

        try {
            // Call the backend ZIP endpoint
            const response = await fetch('/Certificate/DownloadSelectedZip', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/zip'
                },
                body: JSON.stringify(selectedIds)
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || `HTTP error! status: ${response.status}`);
            }

            // Get the ZIP file blob
            const blob = await response.blob();

            // Create a download link
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;

            // Get filename from Content-Disposition header or use default
            const contentDisposition = response.headers.get('Content-Disposition');
            let filename = `certificates_${new Date().toISOString().split('T')[0]}.zip`;
            if (contentDisposition) {
                const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
                if (filenameMatch && filenameMatch[1]) {
                    filename = filenameMatch[1].replace(/['"]/g, '');
                }
            }

            a.download = filename;
            document.body.appendChild(a);
            a.click();

            // Cleanup
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);

            // Show success notification
            showToast(`Successfully downloaded ${selectedIds.length} certificate(s) as ZIP`, 'success');
        } catch (error) {
            console.error('ZIP download error:', error);
            showToast(`Failed to download certificates: ${error.message}`, 'error');
        }
    }

    function handleViewTypeChange(viewType) {
        // Update active button state
        document.querySelectorAll('.toggle-btn[data-view]').forEach(btn => {
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

        // Bulk actions bar will handle Download Selected button visibility
    }

    // Show tile view
    function showTileView() {
        const tileView = document.getElementById('certificatesGrid');
        const tableView = document.getElementById('tableView');

        if (tableView) tableView.style.display = 'none';
        if (tileView) tileView.style.display = 'grid';

        // Bulk actions bar is hidden in tile view, so Download Selected is not visible
    }

    // Populate table view with certificates
    function populateTableView() {
        const gridBody = document.getElementById('gridBody');
        if (!gridBody) return;

        // Clear existing content
        gridBody.innerHTML = '';

        // Use current certificates data
        if (state.certificates && state.certificates.length > 0) {
            renderTableRows(state.certificates);
        } else {
            gridBody.innerHTML = '<div class="no-results">No certificates found</div>';
        }

        // Re-attach select all handler after table is populated
        setupSelectAllHandler();

        // Attach change listeners to individual checkboxes
        document.querySelectorAll('.row-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', updateBulkActionsBar);
        });
    }

    // Update Download Selected button state
    function updateDownloadSelectedButton() {
        const selectedCheckboxes = document.querySelectorAll('.row-checkbox:checked');
        const downloadSelectedBtn = document.getElementById('downloadSelectedBtn');

        if (downloadSelectedBtn) {
            if (selectedCheckboxes.length > 0) {
                downloadSelectedBtn.disabled = false;
                downloadSelectedBtn.title = `Download ${selectedCheckboxes.length} selected certificate${selectedCheckboxes.length !== 1 ? 's' : ''}`;
            } else {
                downloadSelectedBtn.disabled = true;
                downloadSelectedBtn.title = 'Select certificates in table view to download';
            }
        }
    }

    // Update bulk actions bar based on selection
    function updateBulkActionsBar() {
        const selectedCheckboxes = document.querySelectorAll('.row-checkbox:checked');
        const bulkActionsBar = document.getElementById('bulkActionsBar');
        const selectedCount = document.getElementById('selectedCount');

        if (bulkActionsBar && selectedCount) {
            if (selectedCheckboxes.length > 0) {
                bulkActionsBar.style.display = 'flex';
                selectedCount.textContent = `${selectedCheckboxes.length} certificate${selectedCheckboxes.length !== 1 ? 's' : ''} selected`;
            } else {
                bulkActionsBar.style.display = 'none';
            }
        }

        // Update select all checkbox state
        const selectAllCheckbox = document.getElementById('selectAll');
        const allCheckboxes = document.querySelectorAll('.row-checkbox');
        if (selectAllCheckbox && allCheckboxes.length > 0) {
            selectAllCheckbox.checked = selectedCheckboxes.length === allCheckboxes.length;
            selectAllCheckbox.indeterminate = selectedCheckboxes.length > 0 && selectedCheckboxes.length < allCheckboxes.length;
        }

        // Update Download Selected button state
        updateDownloadSelectedButton();
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
        const statusBadge = getStatusBadge(cert.status);

        return `
            <div class="grid-row" data-cert-id="${cert.id}">
                <div class="grid-cell select-cell">
                    <input type="checkbox" class="row-checkbox" data-cert-id="${cert.id}">
                </div>
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
                <div class="grid-cell">${statusBadge}</div>
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
    window.viewCertificate = viewCertificate;
    window.viewCertificateDetails = viewCertificateDetails;
    window.editCertificate = editCertificate;
    window.downloadCertificate = downloadCertificate;

    window.certificateTileViewInternal = {
        viewCertificate: viewCertificate,
        viewCertificateDetails: viewCertificateDetails,
        editCertificate: editCertificate,
        downloadCertificate: downloadCertificate,
        clearAllFilters: clearAllFilters,
        populateTableView: populateTableView
    };

    // Initialize the app when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }
})();