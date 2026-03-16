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
        debounceTimer: null,
        currentView: 'tile', // Track current view: 'tile' or 'list'
        initialized: false
    };

    function initialize() {
        loadFilterOptions();
        loadInitialCertificates();
        setupEventListeners();
        setupInfiniteScroll();
        setupUrlParameters();
        initializeAutocomplete();
        initStickyTable();
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

                // DEBUG: Log the first certificate to verify API response structure
                if (data.certificates && data.certificates.length > 0) {
                    console.log('DEBUG - First certificate from API:', JSON.stringify(data.certificates[0], null, 2));
                    console.log('DEBUG - certificateNumber value:', data.certificates[0].certificateNumber);
                    console.log('DEBUG - id value:', data.certificates[0].id);
                }

                if (append) {
                    state.certificates = [...state.certificates, ...data.certificates];
                } else {
                    state.certificates = data.certificates;
                }

                // DEBUG: Verify certificates are stored
                console.log('DEBUG loadCertificates: Stored', state.certificates.length, 'certificates in state');

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

        // Remove skeleton placeholders on first render
        removeSkeleton();

        // Use tracked view state instead of CSS class check
        if (state.currentView === 'list') {
            populateTableView();
        } else {
            if (append) {
                const html = state.certificates.map(cert => createCertificateCard(cert)).join('');
                grid.insertAdjacentHTML('beforeend', html);
            } else {
                renderCertificatesTiles();
            }
            animateCards();
        }
    }

    // Render certificates in tile format (default)
    function renderCertificatesTiles() {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) {
            console.error('ERROR: certificatesGrid element not found!');
            return;
        }

        // Ensure grid is not hidden (CSS handles display: grid via !important)
        grid.classList.remove('hidden-for-table');

        const html = state.certificates.map(cert => createCertificateCard(cert)).join('');
        console.log('DEBUG renderCertificatesTiles: HTML length:', html.length);
        console.log('DEBUG renderCertificatesTiles: Certificates count:', state.certificates.length);

        grid.innerHTML = html;

        console.log('DEBUG renderCertificatesTiles: Children count:', grid.children.length);
        if (grid.children.length === 0 && state.certificates.length > 0) {
            console.error('ERROR: No children after setting innerHTML!');
            console.log('HTML was:', html.substring(0, 1000));
        }

        // Force all cards visible immediately
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
            <tr class="certificate-row" data-id="${cert.id}" onclick="viewCertificateDetails('${cert.certificateNumber}')">
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
                    <button class="btn-icon" title="View Details" aria-label="View details for certificate ${escapeHtml(cert.certificateNumber)}" onclick="event.stopPropagation(); viewCertificateDetails('${cert.certificateNumber}')">
                        <i class="fas fa-eye" aria-hidden="true"></i>
                    </button>
                    <button class="btn-icon" title="Edit Certificate" aria-label="Edit certificate ${escapeHtml(cert.certificateNumber)}" onclick="event.stopPropagation(); editCertificate('${cert.certificateNumber}')">
                        <i class="fas fa-edit" aria-hidden="true"></i>
                    </button>
                    <button class="btn-icon" title="Download PDF" aria-label="Download PDF for certificate ${escapeHtml(cert.certificateNumber)}" onclick="event.stopPropagation(); downloadCertificate('${cert.certificateNumber}')">
                        <i class="fas fa-download" aria-hidden="true"></i>
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
                 onclick="viewCertificateDetails('${cert.certificateNumber}')">
                <div class="card-header">
                    <div class="header-row">
                        <div class="certificate-category">${escapeHtml(cert.certificationType)}</div>
                        <div class="certificate-id">${escapeHtml(cert.certificateNumber)}</div>
                    </div>
                    <div class="company-name">${escapeHtml(cert.companyName)}</div>
                    <div class="status-row">
                        <span class="status-label">Status:</span> ${statusBadge}
                    </div>
                    ${cert.partner ? `<div class="status-row">
                        <span class="partner-label">Partner:</span> ${partnerBadge}
                    </div>` : ''}
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
                    <div class="action-icon download" title="Download PDF" role="button" tabindex="0" aria-label="Download PDF for certificate ${escapeHtml(cert.certificateNumber)}" onclick="event.stopPropagation(); downloadCertificate('${cert.certificateNumber}')" onkeypress="if(event.key==='Enter'||event.key===' '){event.stopPropagation(); downloadCertificate('${cert.certificateNumber}')}">
                        <i class="fas fa-download" aria-hidden="true"></i>
                    </div>
                    <div class="action-icon external" title="View Details" role="button" tabindex="0" aria-label="View details for certificate ${escapeHtml(cert.certificateNumber)}" onclick="event.stopPropagation(); viewCertificate('${cert.certificateNumber}')" onkeypress="if(event.key==='Enter'||event.key===' '){event.stopPropagation(); viewCertificate('${cert.certificateNumber}')}">
                        <i class="fas fa-external-link-alt" aria-hidden="true"></i>
                    </div>
                    <div class="action-icon edit" title="Edit Certificate" role="button" tabindex="0" aria-label="Edit certificate ${escapeHtml(cert.certificateNumber)}" onclick="event.stopPropagation(); editCertificate('${cert.certificateNumber}')" onkeypress="if(event.key==='Enter'||event.key===' '){event.stopPropagation(); editCertificate('${cert.certificateNumber}')}">
                        <i class="fas fa-edit" aria-hidden="true"></i>
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

        // Sidebar collapse/expand buttons
        setupSidebarToggle();

        // Table column sort headers
        setupTableColumnSort();

        attachFilterEventListeners();
    }

    // Setup sidebar collapse/expand functionality
    function setupSidebarToggle() {
        const toggleTab = document.getElementById('sidebarToggleTab');
        const collapseBtn = document.getElementById('sidebarCollapseBtn');

        if (toggleTab) {
            toggleTab.addEventListener('click', () => {
                expandSidebar();
            });
        }

        if (collapseBtn) {
            collapseBtn.addEventListener('click', () => {
                collapseSidebar();
            });
        }
    }

    // Collapse the sidebar (show edge tab)
    function collapseSidebar() {
        const sidebar = document.getElementById('filterSidebar');
        const toggleTab = document.getElementById('sidebarToggleTab');
        const mainContainer = document.querySelector('.main-container');

        if (sidebar) sidebar.classList.add('collapsed');
        if (toggleTab) toggleTab.classList.add('visible');
        if (mainContainer) mainContainer.classList.add('sidebar-collapsed');
    }

    // Expand the sidebar (hide edge tab)
    function expandSidebar() {
        const sidebar = document.getElementById('filterSidebar');
        const toggleTab = document.getElementById('sidebarToggleTab');
        const mainContainer = document.querySelector('.main-container');

        if (sidebar) sidebar.classList.remove('collapsed');
        if (toggleTab) toggleTab.classList.remove('visible');
        if (mainContainer) mainContainer.classList.remove('sidebar-collapsed');
    }

    // Setup table column sort click handlers
    function setupTableColumnSort() {
        document.querySelectorAll('.sticky-header-cell[data-sort-column]').forEach(header => {
            header.style.cursor = 'pointer';
            header.addEventListener('click', () => {
                const column = header.dataset.sortColumn;
                // Map table column names to API sort values
                const sortMap = {
                    'certificate': 'cert-number-newest',
                    'certificationType': 'cert-type',
                    'status': 'status',
                    'effectiveDate': 'newest',
                    'expirationDate': 'expiring',
                    'company': 'company-az',
                    'productName': 'product-az',
                    'productCategory': 'product-type',
                    'productType': 'product-type',
                    'location': 'location-az',
                    'country': 'country',
                    'partner': 'partner'
                };

                const sortValue = sortMap[column] || 'newest';

                // Toggle ascending/descending
                const currentDir = header.dataset.sortDir || 'asc';
                const newDir = currentDir === 'asc' ? 'desc' : 'asc';
                header.dataset.sortDir = newDir;

                // Map to reverse sort if descending
                const reverseSortMap = {
                    'cert-number-newest': 'cert-number-oldest',
                    'cert-type': 'cert-type-za',
                    'newest': 'oldest-effective',
                    'expiring': 'longest-validity',
                    'company-az': 'company-za',
                    'product-az': 'product-za',
                    'location-az': 'location-za',
                    'country': 'country-za'
                };

                const finalSort = newDir === 'desc' ? (reverseSortMap[sortValue] || sortValue) : sortValue;

                // Update sort icon
                document.querySelectorAll('.sticky-header-cell .sort-icon').forEach(icon => {
                    icon.textContent = '▼';
                });
                const sortIcon = header.querySelector('.sort-icon');
                if (sortIcon) {
                    sortIcon.textContent = newDir === 'asc' ? '▲' : '▼';
                }

                state.filters.sortBy = finalSort;
                loadInitialCertificates();
            });
        });
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
            'additional': 'tags',
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
                        // Use selected autocomplete item
                        const selectedItem = items[selectedIndex];
                        searchInput.value = selectedItem.dataset.value;
                        state.filters.search = selectedItem.dataset.value;
                        autocompleteContainer.style.display = 'none';
                        loadInitialCertificates();
                        updateUrl();
                    } else if (searchInput.value.trim()) {
                        // No autocomplete selected, but there's text - use it for search
                        state.filters.search = searchInput.value.trim();
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

            // Update aria-expanded for accessibility
            const button = dropdown.querySelector('.sort-dropdown-button');
            if (button) {
                button.setAttribute('aria-expanded', !isOpen);
            }

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
            // Update aria-expanded for accessibility
            const button = dropdown.querySelector('.sort-dropdown-button');
            if (button) {
                button.setAttribute('aria-expanded', 'false');
            }
            document.removeEventListener('click', closeSortDropdownOutside);
        }
    }

    // Select sort option
    function selectSort(value, label) {
        state.filters.sortBy = value;
        document.getElementById('sortLabel').textContent = label;
        const dropdown = document.getElementById('sortDropdown');
        dropdown.classList.remove('open');
        // Update aria-expanded for accessibility
        const button = dropdown.querySelector('.sort-dropdown-button');
        if (button) {
            button.setAttribute('aria-expanded', 'false');
        }

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

    // Certificate actions - uses certificate NUMBER (not ID) per requirement
    window.viewCertificateDetails = function(certificateNumber) {
        if (!certificateNumber) {
            console.error('Certificate number is missing');
            showToast('Error: Certificate number not found', 'error');
            return;
        }
        // Encode URI to handle certificate numbers with special chars like "24-0366"
        window.location.href = `/Certificate/CertificateInternalView/${encodeURIComponent(certificateNumber)}`;
    };

    function viewCertificate(certificateNumber) {
        if (!certificateNumber) {
            console.error('Certificate number is missing');
            return;
        }
        // Encode URI to handle certificate numbers with special chars like "24-0366"
        window.open(`/Certificate/CertificateInternalView/${encodeURIComponent(certificateNumber)}`, '_blank');
    }

    function editCertificate(certificateNumber) {
        // Use certificate number (e.g., "24-1605") instead of ID for URL
        window.location.href = `/Certificate/Edit/${encodeURIComponent(certificateNumber)}`;
    }

    window.viewCertificate = viewCertificate;
    window.editCertificate = editCertificate;

    // Download certificate - uses the main CertificateController endpoint which handles
    // both uploaded files and PDF generation fallback
    function downloadCertificate(certificateNumber) {
        try {
            if (!certificateNumber) {
                showToast('Certificate number is required', 'error');
                return;
            }
            // Use the main Download endpoint which accepts certificate number
            window.open(`/Certificate/Download/${encodeURIComponent(certificateNumber)}`, '_blank');
            console.log('Certificate download started for:', certificateNumber);
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

    function removeSkeleton() {
        const grid = document.getElementById('certificatesGrid');
        if (grid) {
            grid.querySelectorAll('.skeleton-card').forEach(el => el.remove());
        }
    }

    function showLoadingIndicator() {
        const grid = document.getElementById('certificatesGrid');
        if (!grid) return;
        if (grid.querySelector('.skeleton-card')) return;

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
        // Immediately make all cards visible - no animation delays
        const cards = document.querySelectorAll('.certificate-card');
        cards.forEach((card) => {
            card.classList.add('animated', 'fadeInUp');
            // Force visibility in case CSS isn't applying
            card.style.opacity = '1';
            card.style.visibility = 'visible';
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

    async function handleViewTypeChange(viewType) {
        // Update active button state
        document.querySelectorAll('.toggle-btn[data-view]').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-view="${viewType}"]`).classList.add('active');

        // Track current view state
        state.currentView = viewType;

        if (viewType === 'list') {
            // Show table view within same page
            await showTableView();
        } else {
            // Show tile view
            showTileView();
        }
        // Sidebar state is preserved - user controls collapse/expand manually
    }

    // Show table view
    async function showTableView() {
        const tileView = document.getElementById('certificatesGrid');
        const tableView = document.getElementById('tableView');

        if (tileView) tileView.classList.add('hidden-for-table');
        if (tableView) {
            tableView.classList.add('active');
            tableView.style.display = 'block';

            // If certificates aren't loaded yet, load them first
            if (!state.certificates || state.certificates.length === 0) {
                console.log('DEBUG showTableView: No certificates in state, loading...');
                await loadCertificates();
            }

            populateTableView();
        }

        // Add class to body for wider layout in table view
        document.body.classList.add('table-view-active');

        // Bulk actions bar will handle Download Selected button visibility
    }

    // Show tile view
    function showTileView() {
        const tileView = document.getElementById('certificatesGrid');
        const tableView = document.getElementById('tableView');

        if (tableView) {
            tableView.classList.remove('active');
            tableView.style.display = 'none';
        }
        if (tileView) tileView.classList.remove('hidden-for-table');

        // Remove wider layout class when switching back to tile view
        document.body.classList.remove('table-view-active');

        // Bulk actions bar is hidden in tile view, so Download Selected is not visible
    }

    // Populate table view with certificates
    function populateTableView() {
        const gridBody = document.getElementById('gridBody');
        if (!gridBody) {
            console.error('DEBUG populateTableView: gridBody element not found!');
            return;
        }

        // Clear existing content
        gridBody.innerHTML = '';

        // DEBUG: Log state to trace the issue
        console.log('DEBUG populateTableView: state.certificates length =', state.certificates ? state.certificates.length : 'undefined');
        console.log('DEBUG populateTableView: state.totalCount =', state.totalCount);

        // Use current certificates data
        if (state.certificates && state.certificates.length > 0) {
            console.log('DEBUG populateTableView: Rendering', state.certificates.length, 'rows');
            renderTableRows(state.certificates);
        } else {
            console.log('DEBUG populateTableView: No certificates in state, showing empty message');
            gridBody.innerHTML = '<div class="no-results">No certificates found</div>';
        }

        // Re-attach select all handler after table is populated
        setupSelectAllHandler();

        // Attach change listeners to individual checkboxes
        document.querySelectorAll('.row-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', updateBulkActionsBar);
        });

        // Sync scroll sizes after content is rendered
        requestAnimationFrame(syncScrollSizes);

        // Update table results count
        const tableResultsCount = document.getElementById('tableResultsCount');
        if (tableResultsCount) {
            const count = state.totalCount || state.certificates.length;
            tableResultsCount.innerHTML = `Showing <strong>${count}</strong> certificates`;
        }

        // Re-apply column visibility from localStorage
        applyColumnVisibility();
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

    // Create table row HTML for a certificate (flex-based for sticky header layout)
    function createTableRow(cert) {
        const effectiveDate = cert.effectiveDate ? new Date(cert.effectiveDate).toLocaleDateString() : 'N/A';
        const expirationDate = cert.expirationDate ? new Date(cert.expirationDate).toLocaleDateString() : 'N/A';
        const location = formatLocation(cert.location, cert.country);
        const statusBadge = getStatusBadge(cert.status);

        const certNumber = cert.certificateNumber || '';
        const certType = cert.certificationType || '';
        const company = cert.companyName || '';
        const contact = cert.companyContact || '';
        const product = cert.productName || '';
        const category = cert.productCategory || '';
        const type = cert.productType || '';
        const subcategory = cert.productSubcategory || '';
        const country = cert.country || '';
        const extTags = cert.externalTags || '';
        const intTags = cert.internalTags || '';
        const asin = cert.asin || '';
        const upc = cert.upc || '';
        const gtin = cert.gtin || '';
        const ean = cert.ean || '';

        const countIds = (val) => {
            if (!val || typeof val !== 'string' || val.trim() === '') return 0;
            return val.split(',').map(s => s.trim()).filter(s => s.length > 0).length;
        };

        const asinCount = countIds(asin);
        const upcCount = countIds(upc);
        const gtinCount = countIds(gtin);
        const eanCount = countIds(ean);

        return `
            <div class="table-body-row" data-cert-id="${cert.id}" data-cert-number="${escapeHtml(certNumber)}" role="row" onclick="viewCertificateDetails('${certNumber}')">
                <div class="table-body-cell tc-select" role="gridcell">
                    <input type="checkbox" class="row-checkbox" data-cert-id="${cert.id}" aria-label="Select certificate ${escapeHtml(certNumber)}" onclick="event.stopPropagation()">
                </div>
                <div class="table-body-cell tc-actions" role="gridcell">
                    <div class="action-buttons">
                        <button class="action-btn edit-btn" onclick="event.stopPropagation(); editCertificate('${certNumber}')" title="Edit Certificate" aria-label="Edit certificate ${escapeHtml(certNumber)}">
                            <i class="fas fa-edit" aria-hidden="true"></i>
                        </button>
                        <button class="action-btn download-btn" onclick="event.stopPropagation(); downloadCertificate('${certNumber}')" title="Download PDF" aria-label="Download PDF for certificate ${escapeHtml(certNumber)}">
                            <i class="fas fa-download" aria-hidden="true"></i>
                        </button>
                        <button class="action-btn view-btn" onclick="event.stopPropagation(); viewCertificateDetails('${certNumber}')" title="View Details" aria-label="View details for certificate ${escapeHtml(certNumber)}">
                            <i class="fas fa-external-link-alt" aria-hidden="true"></i>
                        </button>
                    </div>
                </div>
                <div class="table-body-cell tc-cert" title="${escapeHtml(certNumber)}" role="gridcell">${escapeHtml(certNumber)}</div>
                <div class="table-body-cell tc-type" title="${escapeHtml(certType)}" role="gridcell">${escapeHtml(certType)}</div>
                <div class="table-body-cell tc-status" role="gridcell">${statusBadge}</div>
                <div class="table-body-cell tc-eff" role="gridcell">${effectiveDate}</div>
                <div class="table-body-cell tc-exp" role="gridcell">${expirationDate}</div>
                <div class="table-body-cell tc-company" title="${escapeHtml(company)}" role="gridcell">${escapeHtml(company)}</div>
                <div class="table-body-cell tc-contact" title="${escapeHtml(contact)}" role="gridcell">${escapeHtml(contact)}</div>
                <div class="table-body-cell tc-product" title="${escapeHtml(product)}" role="gridcell">${escapeHtml(product)}</div>
                <div class="table-body-cell tc-prodcat" title="${escapeHtml(category)}" role="gridcell">${escapeHtml(category)}</div>
                <div class="table-body-cell tc-prodtype" title="${escapeHtml(type)}" role="gridcell">${escapeHtml(type)}</div>
                <div class="table-body-cell tc-prodsub" title="${escapeHtml(subcategory)}" role="gridcell">${escapeHtml(subcategory)}</div>
                <div class="table-body-cell tc-location" title="${location}" role="gridcell">${location}</div>
                <div class="table-body-cell tc-country" title="${escapeHtml(country)}" role="gridcell">${escapeHtml(country)}</div>
                <div class="table-body-cell tc-exttag" title="${escapeHtml(extTags)}" role="gridcell">${escapeHtml(extTags)}</div>
                <div class="table-body-cell tc-inttag" title="${escapeHtml(intTags)}" role="gridcell">${escapeHtml(intTags)}</div>
                <div class="table-body-cell tc-partner" title="${escapeHtml(cert.partner || '')}" role="gridcell">${escapeHtml(cert.partner || '')}</div>
                <div class="table-body-cell tc-asin" data-ids="${escapeHtml(asin)}" title="Click to copy ASINs" role="gridcell">${asinCount > 0 ? `<span class="id-count-badge" onclick="event.stopPropagation(); copyIdsToClipboard('${escapeHtml(asin)}', 'ASIN')">${asinCount}</span>` : ''}</div>
                <div class="table-body-cell tc-upc" data-ids="${escapeHtml(upc)}" title="Click to copy UPCs" role="gridcell">${upcCount > 0 ? `<span class="id-count-badge" onclick="event.stopPropagation(); copyIdsToClipboard('${escapeHtml(upc)}', 'UPC')">${upcCount}</span>` : ''}</div>
                <div class="table-body-cell tc-gtin" data-ids="${escapeHtml(gtin)}" title="Click to copy GTINs" role="gridcell">${gtinCount > 0 ? `<span class="id-count-badge" onclick="event.stopPropagation(); copyIdsToClipboard('${escapeHtml(gtin)}', 'GTIN')">${gtinCount}</span>` : ''}</div>
                <div class="table-body-cell tc-ean" data-ids="${escapeHtml(ean)}" title="Click to copy EANs" role="gridcell">${eanCount > 0 ? `<span class="id-count-badge" onclick="event.stopPropagation(); copyIdsToClipboard('${escapeHtml(ean)}', 'EAN')">${eanCount}</span>` : ''}</div>
            </div>
        `;
    }

    // ================================================================
    // STICKY TABLE: Scroll sync, sticky shadow, column toggle
    // ================================================================

    const STORAGE_KEY = 'gcc-cert-columns-internal';

    // All toggleable column keys (Select and Actions are locked)
    const COLUMN_KEYS = [
        'cert', 'type', 'status', 'eff', 'exp',
        'company', 'contact', 'product', 'prodcat', 'prodtype', 'prodsub',
        'location', 'country', 'exttag', 'inttag', 'partner',
        'asin', 'upc', 'gtin', 'ean'
    ];

    function initStickyTable() {
        const stickyBlock = document.getElementById('stickyTableBlock');
        const scrollbarRow = document.getElementById('stickyScrollbar');
        const headerRow = document.getElementById('stickyHeaderRow');
        const bodyWrap = document.getElementById('tableBodyWrap');
        const scrollbarSizer = document.querySelector('#stickyScrollbar .scrollbar-sizer');

        if (!stickyBlock || !scrollbarRow || !headerRow || !bodyWrap || !scrollbarSizer) return;

        // 3-way scroll sync
        let syncing = false;

        scrollbarRow.addEventListener('scroll', function () {
            if (syncing) return;
            syncing = true;
            headerRow.scrollLeft = scrollbarRow.scrollLeft;
            bodyWrap.scrollLeft = scrollbarRow.scrollLeft;
            syncing = false;
        });

        bodyWrap.addEventListener('scroll', function () {
            if (syncing) return;
            syncing = true;
            scrollbarRow.scrollLeft = bodyWrap.scrollLeft;
            headerRow.scrollLeft = bodyWrap.scrollLeft;
            syncing = false;
        });

        // Sticky shadow detection
        window.addEventListener('scroll', function () {
            stickyBlock.classList.toggle('stuck', stickyBlock.getBoundingClientRect().top <= 0);
        }, { passive: true });

        // Sync sizes on window resize
        window.addEventListener('resize', syncScrollSizes);

        // Column toggle dropdown
        setupColumnToggle();

        // Load saved column visibility
        loadColumnVisibility();
    }

    function syncScrollSizes() {
        const bodyInner = document.getElementById('gridBody');
        const scrollbarSizer = document.querySelector('#stickyScrollbar .scrollbar-sizer');
        const headerInner = document.querySelector('#stickyHeaderRow .sticky-header-inner');

        if (!bodyInner || !scrollbarSizer) return;

        const w = bodyInner.scrollWidth;
        scrollbarSizer.style.width = w + 'px';
        if (headerInner) {
            headerInner.style.minWidth = w + 'px';
        }
    }

    // --- Column Toggle ---

    function setupColumnToggle() {
        const toggleBtn = document.getElementById('colToggleBtn');
        const dropdown = document.getElementById('colToggleDropdown');
        const resetBtn = document.getElementById('colResetBtn');

        if (!toggleBtn || !dropdown) return;

        // Toggle dropdown open/close
        toggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            dropdown.classList.toggle('open');
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', function (e) {
            if (!e.target.closest('.col-toggle-wrap')) {
                dropdown.classList.remove('open');
            }
        });

        // Reset all columns to visible
        if (resetBtn) {
            resetBtn.addEventListener('click', function (e) {
                e.preventDefault();
                resetAllColumns();
            });
        }

        // Toggle switch change handlers
        dropdown.querySelectorAll('.ct-switch input[type="checkbox"]').forEach(function (cb) {
            cb.addEventListener('change', function () {
                const colKey = this.dataset.col;
                if (colKey) {
                    toggleColumn(colKey, this.checked);
                }
            });
        });
    }

    function toggleColumn(key, show) {
        // key is the full class name e.g. "tc-cert"
        document.querySelectorAll('.' + key).forEach(function (el) {
            el.classList.toggle('col-hidden', !show);
        });
        updateColumnCount();
        saveColumnVisibility();
        requestAnimationFrame(syncScrollSizes);
    }

    function updateColumnCount() {
        const badge = document.getElementById('colCount');
        if (!badge) return;
        let n = 0;
        const dropdown = document.getElementById('colToggleDropdown');
        if (dropdown) {
            dropdown.querySelectorAll('.ct-row:not(.ct-locked) input[type="checkbox"]').forEach(function (cb) {
                if (cb.checked) n++;
            });
        }
        badge.textContent = n;
    }

    function resetAllColumns() {
        const dropdown = document.getElementById('colToggleDropdown');
        if (dropdown) {
            dropdown.querySelectorAll('.ct-row:not(.ct-locked) input[type="checkbox"]').forEach(function (cb) {
                cb.checked = true;
            });
        }
        document.querySelectorAll('.col-hidden').forEach(function (el) {
            el.classList.remove('col-hidden');
        });
        updateColumnCount();
        saveColumnVisibility();
        requestAnimationFrame(syncScrollSizes);
    }

    function saveColumnVisibility() {
        const hidden = [];
        const dropdown = document.getElementById('colToggleDropdown');
        if (dropdown) {
            dropdown.querySelectorAll('.ct-row:not(.ct-locked) input[type="checkbox"]').forEach(function (cb) {
                if (!cb.checked && cb.dataset.col) {
                    hidden.push(cb.dataset.col);
                }
            });
        }
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(hidden));
        } catch (e) {
            // localStorage not available
        }
    }

    function loadColumnVisibility() {
        try {
            const saved = localStorage.getItem(STORAGE_KEY);
            if (!saved) return;
            const hidden = JSON.parse(saved);
            if (!Array.isArray(hidden) || hidden.length === 0) return;

            const dropdown = document.getElementById('colToggleDropdown');
            hidden.forEach(function (key) {
                // Uncheck the toggle switch
                if (dropdown) {
                    const cb = dropdown.querySelector(`input[data-col="${key}"]`);
                    if (cb) cb.checked = false;
                }
                // Hide the column cells (key is full class e.g. "tc-cert")
                document.querySelectorAll('.' + key).forEach(function (el) {
                    el.classList.add('col-hidden');
                });
            });
            updateColumnCount();
        } catch (e) {
            // Invalid data or localStorage not available
        }
    }

    function applyColumnVisibility() {
        // Re-apply hidden columns to newly rendered rows
        try {
            const saved = localStorage.getItem(STORAGE_KEY);
            if (!saved) return;
            const hidden = JSON.parse(saved);
            if (!Array.isArray(hidden)) return;
            hidden.forEach(function (key) {
                document.querySelectorAll('.' + key).forEach(function (el) {
                    el.classList.add('col-hidden');
                });
            });
        } catch (e) {
            // Ignore
        }
    }

    function showToast(message, type = 'success') {
        if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
            AdminCore.showNotification(message, type);
        } else {
            console.warn(`[showToast] ${type}: ${message}`);
        }
    }

    function showError(message) {
        showToast(message, 'error');
    }

    // Copy product IDs to clipboard as newline-separated values (excel-friendly column)
    function copyIdsToClipboard(idsString, idType) {
        if (!idsString) return;
        // Convert comma-separated to newline-separated for excel column paste
        const ids = idsString.split(',').map(s => s.trim()).filter(s => s.length > 0);
        const excelText = ids.join('\n');

        navigator.clipboard.writeText(excelText).then(() => {
            showToast(`${ids.length} ${idType}(s) copied - paste into Excel column`, 'success');
        }).catch(() => {
            // Fallback for older browsers
            const textarea = document.createElement('textarea');
            textarea.value = excelText;
            document.body.appendChild(textarea);
            textarea.select();
            document.execCommand('copy');
            document.body.removeChild(textarea);
            showToast(`${ids.length} ${idType}(s) copied - paste into Excel column`, 'success');
        });
    }

    // Export functions for global access
    window.copyIdsToClipboard = copyIdsToClipboard;
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

    // Initialize the app when DOM is ready (single initialization only)
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            if (!state.initialized) {
                state.initialized = true;
                initialize();
            }
        });
    } else {
        if (!state.initialized) {
            state.initialized = true;
            initialize();
        }
    }
})();