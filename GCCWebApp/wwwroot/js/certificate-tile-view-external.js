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
        debounceTimer: null,
        currentView: 'tile'
    };

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', initialize);

    async function initialize() {
        setupUrlParameters(); // Parse URL params into state.filters first

        // Load filters and certificates in parallel for faster initial render
        await Promise.all([
            loadFilterOptions(),
            loadInitialCertificates()
        ]);

        restoreFilterCheckboxes(); // Apply state.filters to the now-populated checkboxes
        setupEventListeners();
        setupInfiniteScroll();
        initializeAutocomplete();
        initStickyTable();
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

        // Populate hierarchical product categories (Level 1 -> Level 2 -> Level 3)
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
                    // Deduplicate by ID to prevent showing the same cert twice
                    const existingIds = new Set(state.certificates.map(c => c.id));
                    const uniqueNew = data.certificates.filter(c => !existingIds.has(c.id));
                    state.certificates = [...state.certificates, ...uniqueNew];
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

    // Render certificates based on current view mode
    function renderCertificates(append = false) {
        if (state.currentView === 'list') {
            renderCertificatesTable();
        } else {
            const grid = document.getElementById('certificatesGrid');
            if (!grid) return;
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
                 onclick="viewCertificateDetails('${cert.certificateNumber}')">
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
                    <div class="action-icon download" title="Download PDF" onclick="event.stopPropagation(); downloadCertificate('${cert.certificateNumber}')">
                        <i class="fas fa-download"></i>
                    </div>
                    <div class="action-icon external" title="View Details" onclick="event.stopPropagation(); viewCertificate('${cert.certificateNumber}')">
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

    // Render certificates in table format (flex-based rows into #gridBody)
    function renderCertificatesTable() {
        const gridBody = document.getElementById('gridBody');
        if (!gridBody) return;

        if (!state.certificates || state.certificates.length === 0) {
            gridBody.innerHTML = '<div class="no-results">No certificates found</div>';
            return;
        }

        gridBody.innerHTML = state.certificates.map(cert => createCertificateTableRow(cert)).join('');

        // Sync scroll sizes after rendering
        requestAnimationFrame(syncScrollSizes);

        // Update table results count
        const tableResultsCount = document.getElementById('tableResultsCount');
        if (tableResultsCount) {
            const count = state.totalCount || state.certificates.length;
            tableResultsCount.innerHTML = `Showing <strong>${count}</strong> certificates`;
        }

        // Setup select all and row checkbox handlers
        setupSelectAllHandler();
        document.querySelectorAll('.row-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', updateBulkActionsBar);
        });
    }

    // Parse date string as local time to avoid UTC timezone shift
    // Without this, "2026-04-04" is parsed as UTC midnight, which rolls back a day in US timezones
    function parseLocalDate(dateStr) {
        if (!dateStr) return null;
        if (/^\d{4}-\d{2}-\d{2}$/.test(dateStr)) {
            return new Date(dateStr + 'T00:00:00');
        }
        return new Date(dateStr);
    }

    // Create certificate table row (flex-based for sticky header layout)
    function createCertificateTableRow(cert) {
        const effectiveDate = cert.effectiveDate ? parseLocalDate(cert.effectiveDate).toLocaleDateString() : 'N/A';
        const expirationDate = cert.expirationDate ? parseLocalDate(cert.expirationDate).toLocaleDateString() : 'N/A';
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
                <div class="table-body-cell tc-partner" title="${escapeHtml(cert.partner || '')}" role="gridcell">${escapeHtml(cert.partner || '')}</div>
                <div class="table-body-cell tc-asin" data-ids="${escapeHtml(asin)}" title="Click to copy ASINs" role="gridcell">${asinCount > 0 ? `<span class="id-count-badge" onclick="event.stopPropagation(); copyIdsToClipboard('${escapeHtml(asin)}', 'ASIN')">${asinCount}</span>` : ''}</div>
                <div class="table-body-cell tc-upc" data-ids="${escapeHtml(upc)}" title="Click to copy UPCs" role="gridcell">${upcCount > 0 ? `<span class="id-count-badge" onclick="event.stopPropagation(); copyIdsToClipboard('${escapeHtml(upc)}', 'UPC')">${upcCount}</span>` : ''}</div>
                <div class="table-body-cell tc-gtin" data-ids="${escapeHtml(gtin)}" title="Click to copy GTINs" role="gridcell">${gtinCount > 0 ? `<span class="id-count-badge" onclick="event.stopPropagation(); copyIdsToClipboard('${escapeHtml(gtin)}', 'GTIN')">${gtinCount}</span>` : ''}</div>
                <div class="table-body-cell tc-ean" data-ids="${escapeHtml(ean)}" title="Click to copy EANs" role="gridcell">${eanCount > 0 ? `<span class="id-count-badge" onclick="event.stopPropagation(); copyIdsToClipboard('${escapeHtml(ean)}', 'EAN')">${eanCount}</span>` : ''}</div>
            </div>
        `;
    }

    // Setup select all handler for table view
    function setupSelectAllHandler() {
        const selectAllCheckbox = document.getElementById('selectAll');
        if (selectAllCheckbox) {
            selectAllCheckbox.addEventListener('change', function () {
                const checkboxes = document.querySelectorAll('.row-checkbox');
                checkboxes.forEach(cb => { cb.checked = selectAllCheckbox.checked; });
                updateBulkActionsBar();
            });
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

        const selectAllCheckbox = document.getElementById('selectAll');
        const allCheckboxes = document.querySelectorAll('.row-checkbox');
        if (selectAllCheckbox && allCheckboxes.length > 0) {
            selectAllCheckbox.checked = selectedCheckboxes.length === allCheckboxes.length;
            selectAllCheckbox.indeterminate = selectedCheckboxes.length > 0 && selectedCheckboxes.length < allCheckboxes.length;
        }
    }

    // Copy product IDs to clipboard
    function copyIdsToClipboard(idsString, idType) {
        if (!idsString) return;
        const ids = idsString.split(',').map(s => s.trim()).filter(s => s.length > 0);
        const excelText = ids.join('\n');
        navigator.clipboard.writeText(excelText).then(() => {
            showToast(`${ids.length} ${idType}(s) copied - paste into Excel column`, 'success');
        }).catch(() => {
            const textarea = document.createElement('textarea');
            textarea.value = excelText;
            document.body.appendChild(textarea);
            textarea.select();
            document.execCommand('copy');
            document.body.removeChild(textarea);
            showToast(`${ids.length} ${idType}(s) copied - paste into Excel column`, 'success');
        });
    }

    // Setup event listeners
    function setupEventListeners() {
        // Mobile filter toggle
        const mobileFilterToggle = document.getElementById('mobileFilterToggle');
        if (mobileFilterToggle) {
            mobileFilterToggle.addEventListener('click', () => {
                const sidebar = document.querySelector('.sidebar');
                sidebar.classList.toggle('mobile-open');
                mobileFilterToggle.classList.toggle('open');
                const span = mobileFilterToggle.querySelector('span');
                if (sidebar.classList.contains('mobile-open')) {
                    span.textContent = 'Hide Filters & Search';
                } else {
                    span.textContent = 'Show Filters & Search';
                }
            });
        }

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

        // Filter header collapse/expand toggles
        document.querySelectorAll('.filter-header').forEach(header => {
            header.addEventListener('click', () => {
                const filterId = header.dataset.filter;
                if (filterId) toggleFilter(filterId);
            });
        });
    }

    function toggleFilter(filterId) {
        const content = document.getElementById(filterId + '-content');
        if (!content) return;
        const header = content.previousElementSibling;
        const icon = header ? header.querySelector('i') : null;

        const isCollapsed = content.classList.toggle('filter-collapsed');
        if (icon) {
            icon.className = isCollapsed ? 'fas fa-chevron-down' : 'fas fa-chevron-up';
        }
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

        if (params.has('partner')) {
            state.filters.partner = params.get('partner').split(',');
        }

        if (params.has('certType')) {
            state.filters.certType = params.get('certType').split(',');
        }

        if (params.has('category')) {
            state.filters.productCategory = params.get('category').split(',');
        }

        if (params.has('company')) {
            state.filters.company = params.get('company').split(',');
        }

        if (params.has('country')) {
            state.filters.country = params.get('country').split(',');
        }

        if (params.has('sort')) {
            state.filters.sortBy = params.get('sort');
        }
    }

    // Restore checkbox states and UI after filter options have been populated
    function restoreFilterCheckboxes() {
        if (state.filters.partner && state.filters.partner.length > 0) {
            updateCheckboxes('partner', state.filters.partner);
        }
        if (state.filters.certType.length > 0) {
            updateCheckboxes('cert-type', state.filters.certType);
        }
        if (state.filters.productCategory.length > 0) {
            updateCheckboxes('product-cat', state.filters.productCategory);
        }
        if (state.filters.company.length > 0) {
            updateCheckboxes('company', state.filters.company);
        }
        if (state.filters.country.length > 0) {
            updateCheckboxes('countries', state.filters.country);
        }
        if (state.filters.sortBy !== 'newest') {
            updateSortLabel(state.filters.sortBy);
        }
        renderActiveFilterTags();
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
    // Use certificate number for navigation (more user-friendly URLs)
    window.viewCertificateDetails = function(certificateNumber) {
        if (!certificateNumber) {
            console.error('Certificate number is missing');
            showToast('Error: Certificate number not found', 'error');
            return;
        }
        // Encode URI to handle certificate numbers with special chars like "24-0366"
        window.location.href = `/Certificate/CertificateExternalView/${encodeURIComponent(certificateNumber)}`;
    };

    function viewCertificate(certificateNumber) {
        if (!certificateNumber) {
            console.error('Certificate number is missing');
            return;
        }
        // Encode URI to handle certificate numbers with special chars like "24-0366"
        window.open(`/Certificate/CertificateExternalView/${encodeURIComponent(certificateNumber)}`, '_blank');
    }

    window.viewCertificate = viewCertificate;

    // Download certificate (External view) - uses the main CertificateController endpoint
    // which handles both uploaded files and PDF generation fallback
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
            const date = parseLocalDate(dateStr);
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
        cards.forEach((card) => {
            card.classList.add('animated', 'fadeInUp');
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

        state.currentView = viewType;

        const tileView = document.getElementById('certificatesGrid');
        const tableView = document.getElementById('tableView');

        if (viewType === 'list') {
            // Show table view, hide tiles
            if (tileView) tileView.classList.add('hidden-for-table');
            if (tableView) {
                tableView.style.display = 'block';
                renderCertificatesTable();
            }
            document.body.classList.add('table-view-active');
        } else {
            // Show tile view, hide table
            state.currentView = 'tile';
            if (tableView) tableView.style.display = 'none';
            if (tileView) tileView.classList.remove('hidden-for-table');
            document.body.classList.remove('table-view-active');
            renderCertificatesTiles();
        }
    }

    // ================================================================
    // STICKY TABLE: Scroll sync + sticky shadow (no column toggle for external)
    // ================================================================

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

        // Setup sort headers
        setupTableColumnSort();
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

    // Setup table column sort click handlers for external view
    function setupTableColumnSort() {
        document.querySelectorAll('.sticky-header-cell[data-sort-column]').forEach(header => {
            header.style.cursor = 'pointer';
            header.addEventListener('click', () => {
                const column = header.dataset.sortColumn;
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

                const currentDir = header.dataset.sortDir || 'asc';
                const newDir = currentDir === 'asc' ? 'desc' : 'asc';
                header.dataset.sortDir = newDir;

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

                // Update sort icons
                document.querySelectorAll('.sticky-header-cell .sort-icon').forEach(icon => {
                    icon.textContent = '\u25BC';
                });
                const sortIcon = header.querySelector('.sort-icon');
                if (sortIcon) {
                    sortIcon.textContent = newDir === 'asc' ? '\u25B2' : '\u25BC';
                }

                state.filters.sortBy = finalSort;
                loadInitialCertificates();
            });
        });
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

    // Export functions for global access
    window.copyIdsToClipboard = copyIdsToClipboard;

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