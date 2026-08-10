// Product ID Manager JavaScript
// Handles all interactions for the Product ID Manager component

class ProductIdManager {
    constructor(containerId, certificateId, isReadOnly = false) {
        this.container = document.getElementById(containerId) || document.querySelector('.product-id-manager');
        this.certificateId = certificateId;
        this.isReadOnly = isReadOnly;
        this.productIds = [];
        this.filteredProductIds = [];
        this.currentPage = 1;
        // DEF-150: client-side sort state, mirrors Views/Product/Management.cshtml.
        this.sortColumn = null;
        this.sortDirection = 'asc';

        // CR-018: Server-paged mode for Edit (cert exists in DB). Sort, filter,
        // and search round-trip to the server; only one page (≤ pageSize rows)
        // lives in `productIds` at a time. Create page (certId === 0) stays in
        // memory mode unchanged.
        this.serverPaged = !!certificateId && certificateId !== 0 && certificateId !== '0';
        this.pageSize = 50;
        this.totalCount = 0;
        this.totalPages = 0;
        // Race guard — newer fetches invalidate older in-flight ones so a slow
        // page-1 response can't clobber a fast page-2 response if the user
        // clicks fast.
        this.requestVersion = 0;
        // Small client cache keyed by query string. Cleared on every mutation
        // (Add/Edit/Delete/Import) so we never serve stale data after a write.
        this.cache = new Map();
        this.searchDebounceTimer = null;

        // DEF-194: Bulk delete selection state. Set survives re-renders, filter
        // changes, and (in server-paged mode) page changes — so a user can
        // accumulate selections across multiple pages and delete in one shot.
        this.selectedIds = new Set();
        // DEF-194 cross-page: when true, the user has clicked "Select all N
        // product IDs" in the banner. selectedIds is ignored and the bulk
        // delete posts {deleteAll:true} + current filters. Cleared on filter
        // change, on "Clear selection", or when the user unchecks any row.
        this.selectAllAcrossPages = false;

        this.init();
    }

    init() {
        if (!this.container) {
            console.error('Product ID Manager container not found');
            return;
        }

        // Move modals to body so they aren't trapped inside the form's stacking context
        // (Bootstrap backdrop is appended to body at z-index 1050; modals inside
        //  nested positioned containers render behind it)
        const productIdModal = document.getElementById('productIdModal');
        if (productIdModal) document.body.appendChild(productIdModal);
        const importModal = document.getElementById('import-preview-modal');
        if (importModal) document.body.appendChild(importModal);

        this.bindEvents();
        this.initializeSortableHeaders();
        this.loadProductIds();
        this.initializeTypeHelp();
    }

    // DEF-150: Click-to-sort column headers. Toggle asc/desc on repeated click;
    // switching column resets to asc. Icon ▼ default, ▲ asc, ▼ desc.
    initializeSortableHeaders() {
        const table = document.getElementById('productIdsTable');
        if (!table) return;

        const headers = table.querySelectorAll('th[data-sort]');
        headers.forEach(header => {
            header.addEventListener('click', () => {
                const column = header.getAttribute('data-sort');
                if (this.sortColumn === column) {
                    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
                } else {
                    this.sortColumn = column;
                    this.sortDirection = 'asc';
                }

                headers.forEach(h => {
                    h.classList.remove('active-sort');
                    const icon = h.querySelector('.sort-icon');
                    if (icon) icon.textContent = '▼';
                });
                header.classList.add('active-sort');

                const sortIcon = header.querySelector('.sort-icon');
                if (sortIcon) sortIcon.textContent = this.sortDirection === 'asc' ? '▲' : '▼';

                this.currentPage = 1;
                // CR-018: server-paged mode round-trips for the new sort.
                if (this.serverPaged) {
                    this.loadProductIds();
                } else {
                    this.applySort();
                    this.renderTable();
                }
            });
        });
    }

    // CR-018: Build query string from current sort/filter/search state and
    // fetch one page from the API. Honors a request-version race guard and
    // a small client cache.
    async _loadProductIdsPaged() {
        const params = new URLSearchParams();
        params.set('page', String(this.currentPage || 1));
        params.set('pageSize', String(this.pageSize || 50));
        if (this.sortColumn) {
            params.set('sort', this.sortColumn);
            params.set('dir', this.sortDirection || 'asc');
        }
        const search = (document.getElementById('searchProductIds')?.value || '').trim();
        const typeFilter = document.getElementById('filterByType')?.value || '';
        const partnerFilter = document.getElementById('filterByPartner')?.value || '';
        if (search) params.set('search', search);
        if (typeFilter) params.set('type', typeFilter);
        if (partnerFilter) params.set('partner', partnerFilter);

        const cacheKey = params.toString();
        if (this.cache.has(cacheKey)) {
            this._applyServerPage(this.cache.get(cacheKey));
            return;
        }

        const myVersion = ++this.requestVersion;
        this.showLoading(true);
        try {
            const url = `/api/productids/certificate/${this.certificateId}?${cacheKey}`;
            const response = await fetch(url);
            // Race guard — a newer fetch already started; drop this response.
            if (myVersion !== this.requestVersion) return;

            if (!response.ok) {
                this.showError(`Failed to load product IDs (HTTP ${response.status})`);
                return;
            }

            const json = await response.json();
            if (myVersion !== this.requestVersion) return;
            this.cache.set(cacheKey, json);
            this._applyServerPage(json);
        } catch (error) {
            if (myVersion !== this.requestVersion) return;
            console.error('Error loading product IDs:', error);
            this.showError('Error loading product IDs');
        } finally {
            this.showLoading(false);
        }
    }

    _applyServerPage(json) {
        const items = Array.isArray(json.data) ? json.data : [];
        const totalCount = json.totalCount || 0;
        const totalPages = json.totalPages || 0;
        const pageReturned = json.page || this.currentPage || 1;

        // CR-018: empty-page fallback — if a delete or filter change leaves
        // the requested page beyond the new last page, drop to the last
        // existing page and re-fetch. This catches e.g. "delete the only
        // row on page 3" → land on page 2 instead of an empty table.
        if (items.length === 0 && totalCount > 0 && pageReturned > 1) {
            this.currentPage = totalPages > 0 ? totalPages : 1;
            this.loadProductIds();
            return;
        }

        this.productIds = items;
        // In server-paged mode, productIds === filteredProductIds (server has
        // already applied filter+sort+page).
        this.filteredProductIds = items;
        this.totalCount = totalCount;
        this.totalPages = totalPages;
        this.pageSize = json.pageSize || this.pageSize;
        this.currentPage = pageReturned;
        this.renderTable();
        this.updateCount();
    }

    // DEF-150: Apply current sort to filteredProductIds. Numeric for the
    // Created date (timestamp), alpha (locale-aware) for the rest.
    // CR-018: in server-paged mode the server already applied the sort to
    // the page items, so this is a no-op.
    applySort() {
        if (this.serverPaged) return;
        if (!this.sortColumn) return;

        const sortMap = {
            'type': 'productIdType',
            'value': 'productIdValue',
            'name': 'productName',
            'sku': 'productSku',
            'partner': 'partner',
            'created': 'createdAt',
            'createdBy': 'createdByUsername'
        };
        const prop = sortMap[this.sortColumn] || this.sortColumn;
        const dir = this.sortDirection === 'asc' ? 1 : -1;

        this.filteredProductIds.sort((a, b) => {
            let aValue, bValue;

            if (this.sortColumn === 'created') {
                aValue = a.createdAt ? new Date(a.createdAt).getTime() : 0;
                bValue = b.createdAt ? new Date(b.createdAt).getTime() : 0;
                if (isNaN(aValue)) aValue = 0;
                if (isNaN(bValue)) bValue = 0;
                return (aValue - bValue) * dir;
            }

            if (this.sortColumn === 'createdBy') {
                aValue = (a.createdByUser && a.createdByUser.username) || a.createdBy || '';
                bValue = (b.createdByUser && b.createdByUser.username) || b.createdBy || '';
            } else {
                aValue = a[prop] || '';
                bValue = b[prop] || '';
            }

            return aValue.toString().localeCompare(bValue.toString()) * dir;
        });
    }

    bindEvents() {
        // Add Product ID button
        const addBtn = document.getElementById('addProductId');
        if (addBtn) {
            addBtn.addEventListener('click', () => this.showAddModal());
        }

        // Import/Export buttons
        const importBtn = document.getElementById('importProductIds');
        if (importBtn) {
            importBtn.addEventListener('click', () => this.showImportModal());
        }

        const exportBtn = document.getElementById('exportProductIds');
        if (exportBtn) {
            exportBtn.addEventListener('click', () => this.exportProductIds());
        }

        // Filter controls
        document.getElementById('filterByType')?.addEventListener('change', () => this.applyFilters());
        // CR-015: Status filter removed.
        document.getElementById('filterByPartner')?.addEventListener('change', () => this.applyFilters());
        document.getElementById('searchProductIds')?.addEventListener('input', () => this.applyFilters());

        // Modal form submission - bind to Save button click since form is a div
        const saveBtn = document.getElementById('saveProductId');
        if (saveBtn) {
            saveBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.handleFormSubmit(e);
            });
        }

        // Import form submission
        const importForm = document.getElementById('importForm');
        if (importForm) {
            document.getElementById('startImport')?.addEventListener('click', () => this.handleImport());
        }

        // Download sample
        document.getElementById('downloadSample')?.addEventListener('click', () => this.downloadSample());

        // Product ID type change
        document.getElementById('productIdType')?.addEventListener('change', (e) => this.updateTypeHelp(e.target.value));

        // Product ID value validation
        document.getElementById('productIdValue')?.addEventListener('input', (e) => this.validateProductIdValue(e.target));

        // Table row actions (event delegation)
        const tableBody = document.getElementById('productIdsTableBody');
        if (tableBody) {
            tableBody.addEventListener('click', (e) => this.handleTableAction(e));
            // DEF-194: separate change handler for row checkboxes.
            tableBody.addEventListener('change', (e) => {
                if (e.target.classList.contains('bulk-select-checkbox')) {
                    const id = parseInt(e.target.getAttribute('data-product-id'));
                    if (!isNaN(id)) this.handleRowCheckboxToggle(id, e.target.checked);
                }
            });
        }

        // DEF-194: Select-all + bulk-delete controls.
        const selectAll = document.getElementById('selectAllProductIds');
        if (selectAll) {
            selectAll.addEventListener('change', (e) => this.handleSelectAllToggle(e.target.checked));
        }
        const bulkDeleteBtn = document.getElementById('bulkDeleteProductIds');
        if (bulkDeleteBtn) {
            bulkDeleteBtn.addEventListener('click', () => this.handleBulkDelete());
        }

        // DEF-194 cross-page: banner controls.
        const bannerLink = document.getElementById('bulkSelectAllLink');
        if (bannerLink) {
            bannerLink.addEventListener('click', () => this.enableSelectAllAcrossPages());
        }
        const bannerClear = document.getElementById('bulkSelectAllClear');
        if (bannerClear) {
            bannerClear.addEventListener('click', () => this.clearAllSelection());
        }
    }

    async loadProductIds() {
        // Skip loading if certificate ID is 0 (new certificate being created — Create page, in-memory mode)
        if (!this.certificateId || this.certificateId === 0 || this.certificateId === '0') {
            this.productIds = [];
            this.filteredProductIds = [];
            this.currentPage = 1;
            this.renderTable();
            this.updateCount();
            return;
        }

        // CR-018: Server-paged mode (Edit page) — push sort/filter/search to the API.
        if (this.serverPaged) {
            return this._loadProductIdsPaged();
        }

        try {
            this.showLoading(true);
            const response = await fetch(`/api/productids/certificate/${this.certificateId}`);

            if (response.ok) {
                const result = await response.json();
                // Handle both ApiResponse wrapper and raw array format
                this.productIds = result.data || result;
                if (!Array.isArray(this.productIds)) {
                    this.productIds = [];
                }
                this.filteredProductIds = [...this.productIds];
                this.applySort();
                this.currentPage = 1;
                this.renderTable();
                this.updateCount();
            } else {
                console.error('Failed to load product IDs');
                this.showError('Failed to load product IDs');
            }
        } catch (error) {
            console.error('Error loading product IDs:', error);
            this.showError('Error loading product IDs');
        } finally {
            this.showLoading(false);
        }
    }

    renderTable() {
        const tbody = document.getElementById('productIdsTableBody');
        if (!tbody) return;

        // DEF-150: Sort here so every code path that ends in render
        // (loadProductIds, applyFilters, in-memory add/edit/delete) gets a
        // consistent ordering without sprinkling applySort() at every site.
        // No-op in server-paged mode (server already sorted the page).
        this.applySort();

        // DEF-229: in in-memory mode (Create page, legacy non-paged Edit),
        // totalCount is what drives the "Select all N" cross-page banner in
        // updateBulkActionsUI. Without this, totalCount stays at 0 and the
        // banner never appears → select-all caps at the visible 50.
        if (!this.serverPaged) {
            this.totalCount = this.filteredProductIds.length;
        }

        if (this.filteredProductIds.length === 0) {
            tbody.innerHTML = this.getEmptyStateHtml();
            this.renderPagination();
            this.updateBulkActionsUI();
            return;
        }

        // CR-018: in server-paged mode, filteredProductIds IS the page —
        // render it as-is and skip client-side windowing.
        if (this.serverPaged) {
            tbody.innerHTML = this.filteredProductIds.map(productId => this.getProductIdRowHtml(productId)).join('');
            this.renderPagination();
            this.updateBulkActionsUI();
            return;
        }

        const PAGE_SIZE = 50;
        const totalItems = this.filteredProductIds.length;
        const totalPages = Math.ceil(totalItems / PAGE_SIZE);

        if (!this.currentPage || this.currentPage < 1) this.currentPage = 1;
        if (this.currentPage > totalPages) this.currentPage = totalPages;

        // For small lists, show all; for large lists, paginate
        if (totalItems <= PAGE_SIZE) {
            tbody.innerHTML = this.filteredProductIds.map(productId => this.getProductIdRowHtml(productId)).join('');
        } else {
            const start = (this.currentPage - 1) * PAGE_SIZE;
            const pageItems = this.filteredProductIds.slice(start, start + PAGE_SIZE);
            tbody.innerHTML = pageItems.map(productId => this.getProductIdRowHtml(productId)).join('');
        }

        this.renderPagination();
        this.updateBulkActionsUI();
    }

    // DEF-152: Pagination markup mirrors Views/Product/Management.cshtml:740-793
    // (chevron Prev / numbered pages with ellipsis / chevron Next), styled via
    // .product-id-manager .pagination-* in product-id-manager.css. Removes the
    // inline-styled First/Prev/Page X/Next/Last variant that didn't match the
    // rest of the admin app.
    // CR-018: in server-paged mode, totalItems / totalPages come from the API
    // response, not the local filtered array.
    renderPagination() {
        const PAGE_SIZE = this.pageSize || 50;
        const totalItems = this.serverPaged ? this.totalCount : this.filteredProductIds.length;
        const existing = document.getElementById('pid-pagination');

        if (totalItems <= PAGE_SIZE) {
            if (existing) existing.remove();
            return;
        }

        const totalPages = this.serverPaged
            ? (this.totalPages || Math.ceil(totalItems / PAGE_SIZE))
            : Math.ceil(totalItems / PAGE_SIZE);
        const cur = this.currentPage;
        const start = (cur - 1) * PAGE_SIZE + 1;
        const end = Math.min(cur * PAGE_SIZE, totalItems);

        let buttonsHtml = '';

        // Previous chevron
        buttonsHtml += `<button type="button" class="pagination-btn${cur === 1 ? ' disabled' : ''}"
                            onclick="productIdManager.goToPage(${cur - 1})" ${cur === 1 ? 'disabled' : ''}
                            aria-label="Previous page">
                            <i class="fas fa-chevron-left"></i>
                        </button>`;

        // Numbered pages with ellipsis (max 7 visible page-numbers, mirrors Product Management)
        const maxVisible = 7;
        let startPage = 1;
        let endPage = totalPages;

        if (totalPages > maxVisible) {
            const half = Math.floor(maxVisible / 2);
            startPage = Math.max(1, cur - half);
            endPage = Math.min(totalPages, startPage + maxVisible - 1);
            if (endPage - startPage < maxVisible - 1) {
                startPage = Math.max(1, endPage - maxVisible + 1);
            }
        }

        if (startPage > 1) {
            buttonsHtml += `<button type="button" class="pagination-btn" onclick="productIdManager.goToPage(1)">1</button>`;
            if (startPage > 2) buttonsHtml += `<span class="pagination-ellipsis">...</span>`;
        }

        for (let i = startPage; i <= endPage; i++) {
            buttonsHtml += `<button type="button" class="pagination-btn${i === cur ? ' active' : ''}"
                                onclick="productIdManager.goToPage(${i})"
                                ${i === cur ? 'aria-current="page"' : ''}>${i}</button>`;
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) buttonsHtml += `<span class="pagination-ellipsis">...</span>`;
            buttonsHtml += `<button type="button" class="pagination-btn" onclick="productIdManager.goToPage(${totalPages})">${totalPages}</button>`;
        }

        // Next chevron
        buttonsHtml += `<button type="button" class="pagination-btn${cur === totalPages ? ' disabled' : ''}"
                            onclick="productIdManager.goToPage(${cur + 1})" ${cur === totalPages ? 'disabled' : ''}
                            aria-label="Next page">
                            <i class="fas fa-chevron-right"></i>
                        </button>`;

        const paginationHtml = `
            <div id="pid-pagination" class="pid-pagination-wrapper">
                <span class="pid-pagination-info">Showing ${start}-${end} of ${totalItems}</span>
                <div class="pagination-controls">${buttonsHtml}</div>
            </div>`;

        // Insert or replace pagination after the table container
        const container = document.querySelector('.product-ids-table-container');
        if (existing) existing.remove();
        if (container) {
            container.insertAdjacentHTML('afterend', paginationHtml);
        }
    }

    goToPage(page) {
        this.currentPage = page;
        // CR-018: server-paged mode round-trips for the new page; in-memory
        // mode just re-windows the existing array.
        if (this.serverPaged) {
            this.loadProductIds();
        } else {
            this.renderTable();
        }
        const container = document.querySelector('.product-ids-table-container');
        if (container) container.scrollTop = 0;
    }

    getProductIdRowHtml(productId) {
        const actionsHtml = this.isReadOnly ? '' : `
            <td>
                <div class="admin-inline-actions">
                    <button type="button" class="admin-inline-action-btn admin-inline-action-btn--edit btn-edit-product-id"
                            data-product-id="${productId.id}" title="Edit"
                            aria-label="Edit product ID ${productId.productIdValue || ''}">
                        <i class="fas fa-edit" aria-hidden="true"></i>
                    </button>
                    <button type="button" class="admin-inline-action-btn admin-inline-action-btn--delete btn-delete-product-id"
                            data-product-id="${productId.id}" title="Delete"
                            aria-label="Delete product ID ${productId.productIdValue || ''}">
                        <i class="fas fa-trash" aria-hidden="true"></i>
                    </button>
                </div>
            </td>`;

        // DEF-194: Per-row bulk-select checkbox (non-readonly only). Checked
        // state is restored after render from this.selectedIds.
        const isChecked = this.selectedIds.has(productId.id) ? 'checked' : '';
        const selectHtml = this.isReadOnly ? '' : `
            <td style="text-align: center;">
                <input type="checkbox" class="bulk-select-checkbox"
                       data-product-id="${productId.id}" ${isChecked}
                       aria-label="Select product ID ${productId.productIdValue || ''}">
            </td>`;

        return `
            <tr data-product-id="${productId.id}">
                ${selectHtml}
                <td>
                    <span class="product-type-badge product-type-${productId.productIdType.toLowerCase()}">
                        ${productId.productIdType}
                    </span>
                </td>
                <td class="product-id-value" title="${productId.productIdValue}">
                    ${productId.productIdValue}
                </td>
                <td class="product-name" title="${productId.productName || ''}">
                    ${productId.productName || '-'}
                </td>
                <td class="product-sku" title="${productId.productSku || ''}">
                    ${productId.productSku || '-'}
                </td>
                <!-- CR-015: Status cell removed — derived from parent cert. -->
                <td>
                    ${(() => { const ps = this.getPartnerList(productId); return ps.length
                        ? ps.map(n => `<span class="partner-badge partner-${n.toLowerCase()}">${n}</span>`).join(' ')
                        : '<span class="text-muted">-</span>'; })()}
                </td>
                <td class="created-date" title="${this.formatDateTime(productId.createdAt)}">
                    ${this.formatDate(productId.createdAt)}
                </td>
                <td class="created-by" title="${(productId.createdByUser && productId.createdByUser.username) || (productId.createdAt ? 'System' : '—')}">
                    ${/* QC #200 (DEF-282): unsaved in-memory rows (no createdAt) show '—', not the
                          misleading 'System'; saved rows keep username, or 'System' only when CreatedBy is genuinely null. */ ''}
                    ${(productId.createdByUser && productId.createdByUser.username) || (productId.createdAt ? 'System' : '—')}
                </td>
                ${actionsHtml}
            </tr>`;
    }

    getEmptyStateHtml() {
        // DEF-194: colspan grew by 1 when bulk-select column added for non-readonly.
        const colspan = this.isReadOnly ? 7 : 9;
        const message = this.isReadOnly ?
            'No product IDs added yet.' :
            'No product IDs added yet.<br><small>Click "Add Product ID" to get started.</small>';

        return `
            <tr id="noProductIdsRow">
                <td colspan="${colspan}" class="text-center text-muted py-4">
                    <i class="fas fa-barcode fa-2x mb-2 d-block text-muted"></i>
                    ${message}
                </td>
            </tr>`;
    }

    applyFilters() {
        // DEF-194 cross-page: any filter change invalidates "select all N"
        // because N itself changes. Drop the selection state so the next
        // table render starts with a clean slate.
        if (this.selectAllAcrossPages || this.selectedIds.size > 0) {
            this.selectAllAcrossPages = false;
            this.selectedIds.clear();
        }

        // CR-018: server-paged mode round-trips for filter/search changes.
        // Search input fires per-keystroke, so debounce to 250 ms; the type
        // and partner dropdowns fire on change and don't need debouncing,
        // but routing them through the same debounced path keeps the logic
        // single-track without flicker.
        if (this.serverPaged) {
            if (this.searchDebounceTimer) clearTimeout(this.searchDebounceTimer);
            this.searchDebounceTimer = setTimeout(() => {
                this.currentPage = 1;
                this.loadProductIds();
            }, 250);
            return;
        }

        const typeFilter = document.getElementById('filterByType')?.value || '';
        // CR-015: Status filter removed — product ID status follows parent cert.
        const partnerFilter = document.getElementById('filterByPartner')?.value || '';
        const searchFilter = document.getElementById('searchProductIds')?.value.toLowerCase() || '';

        this.filteredProductIds = this.productIds.filter(productId => {
            const typeMatch = !typeFilter || productId.productIdType === typeFilter;
            const partnerMatch = !partnerFilter || this.getPartnerList(productId).includes(partnerFilter);
            const searchMatch = !searchFilter ||
                productId.productIdValue.toLowerCase().includes(searchFilter) ||
                (productId.productName && productId.productName.toLowerCase().includes(searchFilter)) ||
                (productId.productSku && productId.productSku.toLowerCase().includes(searchFilter));

            return typeMatch && partnerMatch && searchMatch;
        });

        this.applySort();
        this.currentPage = 1;
        this.renderTable();
        this.updatePaginationInfo();
    }

    updateCount() {
        const countElement = document.getElementById('productIdCount');
        if (countElement) {
            // CR-018: server-paged mode reports the full DB total, not just
            // the visible page count.
            countElement.textContent = this.serverPaged ? this.totalCount : this.productIds.length;
        }
    }

    updatePaginationInfo() {
        const paginationInfo = document.getElementById('paginationInfo');
        if (paginationInfo) {
            const total = this.productIds.length;
            const filtered = this.filteredProductIds.length;

            if (total === 0) {
                paginationInfo.innerHTML = '';
            } else if (filtered === total) {
                paginationInfo.innerHTML = `<small>Showing ${total} product ID(s)</small>`;
            } else {
                paginationInfo.innerHTML = `<small>Showing ${filtered} of ${total} product ID(s)</small>`;
            }
        }
    }

    showAddModal() {
        this.resetForm();
        document.getElementById('productIdModalLabel').textContent = 'Add Product ID';
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('productIdModal'));
        modal.show();
    }

    async showEditModal(productIdId) {
        // For temp items (negative IDs on Create page), load from memory
        if (productIdId < 0) {
            const productId = this.productIds.find(p => p.id === productIdId);
            if (productId) {
                this.populateForm(productId);
                document.getElementById('productIdModalLabel').textContent = 'Edit Product ID';
                const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('productIdModal'));
                modal.show();
            } else {
                this.showError('Product ID not found');
            }
            return;
        }

        try {
            const response = await fetch(`/api/productids/${productIdId}`);
            if (response.ok) {
                const result = await response.json();
                const productId = result.data || result;
                this.populateForm(productId);
                document.getElementById('productIdModalLabel').textContent = 'Edit Product ID';
                const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('productIdModal'));
                modal.show();
            } else {
                this.showError('Failed to load product ID details');
            }
        } catch (error) {
            console.error('Error loading product ID:', error);
            this.showError('Error loading product ID details');
        }
    }

    resetForm() {
        const form = document.getElementById('productIdForm');
        if (form) {
            // productIdForm is a <div>, not a <form>, so reset fields manually
            const inputs = form.querySelectorAll('input[type="text"], textarea');
            inputs.forEach(input => input.value = '');
            const selects = form.querySelectorAll('select');
            selects.forEach(select => {
                // CR-171: multi-selects must clear ALL options, not select the first one.
                if (select.multiple) {
                    Array.from(select.options).forEach(o => o.selected = false);
                } else {
                    select.selectedIndex = 0;
                }
            });

            document.getElementById('productIdId').value = '0';
            document.getElementById('certificateId').value = this.certificateId;
            // CR-015: Status field removed from modal — auto-derived from parent cert on save.
            this.clearValidation();
        }
    }

    // CR-171: normalize a product ID's partner association(s) to an array of names. Prefers the
    // partners[] array (from the API DTO); falls back to splitting the delimited partner string.
    getPartnerList(productId) {
        if (Array.isArray(productId.partners) && productId.partners.length) {
            return productId.partners.filter(Boolean);
        }
        return (productId.partner || '')
            .split(/[|,]/)
            .map(s => s.trim())
            .filter(s => s.length && s.toLowerCase() !== 'none');
    }

    // CR-171: select the matching options in the #productIdPartner multi-select.
    setSelectedPartners(names) {
        const sel = document.getElementById('productIdPartner');
        if (!sel) return;
        const wanted = new Set((names || []).map(n => n.toLowerCase()));
        Array.from(sel.options).forEach(o => { o.selected = wanted.has(o.value.toLowerCase()); });
    }

    // CR-171: read the selected partner names from the multi-select.
    getSelectedPartners() {
        const sel = document.getElementById('productIdPartner');
        if (!sel) return [];
        return Array.from(sel.selectedOptions).map(o => o.value).filter(Boolean);
    }

    populateForm(productId) {
        document.getElementById('productIdId').value = productId.id;
        document.getElementById('certificateId').value = productId.certificateId;
        document.getElementById('productIdType').value = productId.productIdType;
        document.getElementById('productIdValue').value = productId.productIdValue;
        // CR-015: Status field removed from modal — derived from parent cert.
        // CR-171: partner is now a multi-select. Prefer partners[] (from the API); fall back to
        // splitting the (possibly delimited) partner string for in-memory items.
        this.setSelectedPartners(this.getPartnerList(productId));
        document.getElementById('productName').value = productId.productName || '';
        document.getElementById('productSku').value = productId.productSku || '';
        document.getElementById('productDescription').value = productId.description || '';

        this.updateTypeHelp(productId.productIdType);
        this.clearValidation();
    }

    async handleFormSubmit(e) {
        e.preventDefault();

        if (!this.validateForm()) {
            return;
        }

        // Collect form data from individual fields (form is a div, not a <form>)
        // CR-015: Status omitted — backend auto-derives from parent cert.
        // DEF-161: normalise the value to canonical case before save so a
        // lowercase ASIN entered as `b08n5wrwnw` saves as `B08N5WRWNW`.
        const _type = document.getElementById('productIdType').value;
        const _rawValue = document.getElementById('productIdValue').value.trim();
        // CR-171: collect the multi-select partners; persist as a pipe-delimited string (the backend
        // SyncPartners splits on '|'/',' to populate the M2M). Empty = shared with all cert partners.
        const _partners = this.getSelectedPartners();
        const productId = {
            Id: parseInt(document.getElementById('productIdId').value) || 0,
            CertificateId: parseInt(document.getElementById('certificateId').value) || this.certificateId,
            ProductIdType: _type,
            ProductIdValue: this.normalizeProductIdValue(_type, _rawValue),
            Partner: _partners.join('|'),
            ProductName: document.getElementById('productName').value.trim(),
            ProductSku: document.getElementById('productSku').value.trim(),
            Description: document.getElementById('productDescription').value.trim()
        };

        // For new certificates (Create page), store in memory instead of API
        if (!this.certificateId || this.certificateId === 0) {
            const isEdit = productId.Id < 0; // Temp IDs are negative

            // CR-015: temp items have no status; backend derives from cert on save.
            if (isEdit) {
                // Update existing temp item
                const index = this.productIds.findIndex(p => p.id === productId.Id);
                if (index !== -1) {
                    this.productIds[index] = {
                        ...this.productIds[index],
                        productIdType: productId.ProductIdType,
                        productIdValue: productId.ProductIdValue,
                        partner: productId.Partner || '',
                        partners: _partners,
                        productName: productId.ProductName || '',
                        productSku: productId.ProductSku || '',
                        description: productId.Description || ''
                    };
                }
            } else {
                // Add new temp item
                const tempId = -(Date.now());
                this.productIds.push({
                    id: tempId,
                    certificateId: 0,
                    productIdType: productId.ProductIdType,
                    productIdValue: productId.ProductIdValue,
                    partner: productId.Partner || '',
                    partners: _partners,
                    productName: productId.ProductName || '',
                    productSku: productId.ProductSku || '',
                    description: productId.Description || '',
                    isNew: true
                });
            }

            this.filteredProductIds = [...this.productIds];
            this.renderTable();
            this.updateCount();

            const modal = bootstrap.Modal.getInstance(document.getElementById('productIdModal'));
            if (modal) modal.hide();
            // QC #80: Clean up any stuck modal backdrops to prevent gray overlay buildup
            document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
            document.body.classList.remove('modal-open');
            document.body.style.removeProperty('overflow');
            document.body.style.removeProperty('padding-right');

            this.showSuccess(isEdit ? 'Product ID updated' : 'Product ID added (will be saved with certificate)');
            return;
        }

        try {
            this.showLoading(true);
            const isEdit = productId.Id > 0;
            const url = isEdit ? `/api/productids/${productId.Id}` : '/api/productids';
            const method = isEdit ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(productId)
            });

            if (response.ok) {
                const modal = bootstrap.Modal.getInstance(document.getElementById('productIdModal'));
                modal.hide();
                // QC #80: Clean up any stuck modal backdrops
                document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
                document.body.classList.remove('modal-open');
                document.body.style.removeProperty('overflow');
                document.body.style.removeProperty('padding-right');
                this.cache.clear(); // CR-018: invalidate page cache after write
                await this.loadProductIds();
                this.showSuccess(isEdit ? 'Product ID updated successfully' : 'Product ID added successfully');
            } else {
                const error = await response.text();
                this.showError(error || 'Failed to save product ID');
            }
        } catch (error) {
            console.error('Error saving product ID:', error);
            this.showError('Error saving product ID');
        } finally {
            this.showLoading(false);
        }
    }

    async handleDelete(productIdId) {
        const confirmed = await AdminCore.showConfirmation({
            title: 'Delete Confirmation',
            message: 'Are you sure you want to delete this product ID? This action cannot be undone.',
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger'
        });

        if (!confirmed) {
            return;
        }

        // For temp items (negative IDs) or new certificates, remove from memory
        if (productIdId < 0 || !this.certificateId || this.certificateId === 0) {
            this.productIds = this.productIds.filter(p => p.id !== productIdId);
            this.filteredProductIds = [...this.productIds];
            this.selectedIds.delete(productIdId); // DEF-194: keep bulk-selection in sync
            this.renderTable();
            this.updateCount();
            this.showSuccess('Product ID removed');
            return;
        }

        try {
            this.showLoading(true);
            const response = await fetch(`/api/productids/${productIdId}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                this.cache.clear(); // CR-018: invalidate page cache after delete
                this.selectedIds.delete(productIdId); // DEF-194: keep bulk-selection in sync
                await this.loadProductIds();
                this.showSuccess('Product ID deleted successfully');
            } else {
                this.showError('Failed to delete product ID');
            }
        } catch (error) {
            console.error('Error deleting product ID:', error);
            this.showError('Error deleting product ID');
        } finally {
            this.showLoading(false);
        }
    }

    // DEF-194: Bulk-delete helpers.
    // selectedIds is a Set<number> that persists across renders, filters, and
    // (in server-paged mode) page changes. The header select-all only acts on
    // currently visible rows; the count badge and confirm dialog reflect the
    // global selection.
    handleRowCheckboxToggle(productIdId, checked) {
        // Unchecking any row exits cross-page mode — you can't claim
        // "all N selected" once you've deselected one of them.
        if (!checked && this.selectAllAcrossPages) {
            this.selectAllAcrossPages = false;
            // Seed selectedIds with the *other* currently visible rows so
            // the badge count drops by one (not to zero) — matches user
            // intent of "I had everything, minus this one."
            const tableBody = document.getElementById('productIdsTableBody');
            this.selectedIds.clear();
            if (tableBody) {
                tableBody.querySelectorAll('.bulk-select-checkbox').forEach(box => {
                    const id = parseInt(box.getAttribute('data-product-id'));
                    if (!isNaN(id) && box.checked) this.selectedIds.add(id);
                });
            }
        } else if (checked) {
            this.selectedIds.add(productIdId);
        } else {
            this.selectedIds.delete(productIdId);
        }
        this.updateBulkActionsUI();
    }

    handleSelectAllToggle(checked) {
        const tableBody = document.getElementById('productIdsTableBody');
        if (!tableBody) return;

        // Unticking the header select-all also exits cross-page mode and
        // clears everything (Gmail-equivalent behavior).
        if (!checked && this.selectAllAcrossPages) {
            this.selectAllAcrossPages = false;
            this.selectedIds.clear();
            tableBody.querySelectorAll('.bulk-select-checkbox').forEach(box => { box.checked = false; });
            this.updateBulkActionsUI();
            return;
        }

        const rowBoxes = tableBody.querySelectorAll('.bulk-select-checkbox');
        rowBoxes.forEach(box => {
            const id = parseInt(box.getAttribute('data-product-id'));
            if (isNaN(id)) return;
            box.checked = checked;
            if (checked) {
                this.selectedIds.add(id);
            } else {
                this.selectedIds.delete(id);
            }
        });
        this.updateBulkActionsUI();
    }

    // DEF-194 cross-page: user clicked the "Select all N" link in the banner.
    // Flip into cross-page mode. The selectedIds set is intentionally NOT
    // populated — bulk delete will use the deleteAll path with current filters.
    enableSelectAllAcrossPages() {
        this.selectAllAcrossPages = true;
        // Pre-check every visible row for visual consistency.
        const tableBody = document.getElementById('productIdsTableBody');
        if (tableBody) {
            tableBody.querySelectorAll('.bulk-select-checkbox').forEach(box => { box.checked = true; });
        }
        this.updateBulkActionsUI();
    }

    // Clear all selection state (Clear-selection link in banner, or post-delete).
    clearAllSelection() {
        this.selectAllAcrossPages = false;
        this.selectedIds.clear();
        const tableBody = document.getElementById('productIdsTableBody');
        if (tableBody) {
            tableBody.querySelectorAll('.bulk-select-checkbox').forEach(box => { box.checked = false; });
        }
        const selectAll = document.getElementById('selectAllProductIds');
        if (selectAll) {
            selectAll.checked = false;
            selectAll.indeterminate = false;
        }
        this.updateBulkActionsUI();
    }

    updateBulkActionsUI() {
        if (this.isReadOnly) return;

        // DEF-194 cross-page: when "select all N" is active, every newly
        // rendered row (e.g. after a page change) should also appear ticked.
        if (this.selectAllAcrossPages) {
            const tbody = document.getElementById('productIdsTableBody');
            if (tbody) {
                tbody.querySelectorAll('.bulk-select-checkbox').forEach(b => { b.checked = true; });
            }
        }

        // Count: total when in cross-page mode, else size of accumulated set.
        const count = this.selectAllAcrossPages ? this.totalCount : this.selectedIds.size;
        const btn = document.getElementById('bulkDeleteProductIds');
        const countSpan = document.getElementById('bulkDeleteCount');
        if (btn) btn.style.display = count > 0 ? '' : 'none';
        if (countSpan) countSpan.textContent = String(count);

        // Header select-all: checked when every visible row is selected,
        // indeterminate when partial, unchecked when none.
        const selectAll = document.getElementById('selectAllProductIds');
        const tableBody = document.getElementById('productIdsTableBody');
        const rowBoxes = tableBody ? tableBody.querySelectorAll('.bulk-select-checkbox') : [];
        if (selectAll && tableBody) {
            if (rowBoxes.length === 0) {
                selectAll.checked = false;
                selectAll.indeterminate = false;
            } else {
                let checkedVisible = 0;
                rowBoxes.forEach(box => { if (box.checked) checkedVisible++; });
                if (checkedVisible === 0) {
                    selectAll.checked = false;
                    selectAll.indeterminate = false;
                } else if (checkedVisible === rowBoxes.length) {
                    selectAll.checked = true;
                    selectAll.indeterminate = false;
                } else {
                    selectAll.checked = false;
                    selectAll.indeterminate = true;
                }
            }
        }

        // DEF-194 cross-page: banner visibility / state.
        //   - hidden when nothing's selected or only a partial page is selected
        //   - "Select all N" prompt when every visible row is selected AND
        //     totalCount > visible rows AND not already in cross-page mode
        //   - "All N selected" + Clear when in cross-page mode
        const banner = document.getElementById('bulkSelectAllBanner');
        const link = document.getElementById('bulkSelectAllLink');
        const clearBtn = document.getElementById('bulkSelectAllClear');
        const visibleCountSpan = document.getElementById('bulkBannerVisibleCount');
        const totalCountSpan = document.getElementById('bulkBannerTotalCount');
        const bannerText = document.getElementById('bulkSelectAllBannerText');
        if (banner) {
            const visible = rowBoxes.length;
            const total = this.totalCount || visible;
            const wholePageSelected = visible > 0 && rowBoxes.length > 0 &&
                Array.from(rowBoxes).every(b => b.checked);
            const moreOffPage = total > visible;

            if (this.selectAllAcrossPages) {
                banner.style.display = '';
                banner.classList.add('bulk-select-all-active');
                if (bannerText) bannerText.textContent = `All ${total} product IDs are selected.`;
                if (link) link.style.display = 'none';
                if (clearBtn) clearBtn.style.display = '';
            } else if (wholePageSelected && moreOffPage) {
                banner.style.display = '';
                banner.classList.remove('bulk-select-all-active');
                if (visibleCountSpan) visibleCountSpan.textContent = String(visible);
                if (totalCountSpan) totalCountSpan.textContent = String(total);
                if (bannerText) {
                    bannerText.innerHTML =
                        `All <span id="bulkBannerVisibleCount">${visible}</span> on this page are selected.`;
                }
                if (link) {
                    link.style.display = '';
                    link.innerHTML = `Select all <span id="bulkBannerTotalCount">${total}</span> product IDs`;
                }
                if (clearBtn) clearBtn.style.display = 'none';
            } else if (wholePageSelected) {
                // DEF-236: Single page, every row selected (no off-page rows).
                // The cross-page "Select all N" link is meaningless here, but the
                // user still expects a visible "all selected / delete-all" bar
                // (the red "Delete Selected (N)" button does the delete). Show a
                // plain confirmation banner with a Clear-selection affordance.
                banner.style.display = '';
                banner.classList.remove('bulk-select-all-active');
                if (bannerText) {
                    bannerText.textContent =
                        `All ${total} product ID${total === 1 ? '' : 's'} selected.`;
                }
                if (link) link.style.display = 'none';
                if (clearBtn) clearBtn.style.display = '';
            } else {
                banner.style.display = 'none';
                banner.classList.remove('bulk-select-all-active');
            }
        }
    }

    async handleBulkDelete() {
        // Cross-page mode: count reflects totalCount, ids set is empty.
        const crossPage = this.selectAllAcrossPages;
        const count = crossPage ? this.totalCount : this.selectedIds.size;
        if (count === 0) return;

        const confirmed = await AdminCore.showConfirmation({
            title: 'Delete Confirmation',
            message: `Are you sure you want to delete ${count} product ID(s)? This action cannot be undone.`,
            confirmText: `Delete ${count}`,
            cancelText: 'Cancel',
            type: 'danger'
        });
        if (!confirmed) return;

        // In-memory mode (Create page or temp negative IDs): drop from local state only.
        const isInMemory = !this.certificateId || this.certificateId === 0 || this.certificateId === '0';
        if (isInMemory) {
            // DEF-229: cross-page mode keeps selectedIds empty by design (the
            // "delete all N" path drives off totalCount + current filters). For
            // in-memory, that means dropping every row that's in the current
            // filtered view from productIds — items outside the filter stay.
            let removed;
            if (crossPage) {
                const toRemove = new Set(this.filteredProductIds.map(p => p.id));
                const before = this.productIds.length;
                this.productIds = this.productIds.filter(p => !toRemove.has(p.id));
                removed = before - this.productIds.length;
            } else {
                const set = new Set(this.selectedIds);
                const before = this.productIds.length;
                this.productIds = this.productIds.filter(p => !set.has(p.id));
                removed = before - this.productIds.length;
            }
            this.filteredProductIds = [...this.productIds];
            this.clearAllSelection();
            this.renderTable();
            this.updateCount();
            this.showSuccess(`${removed} product ID(s) removed`);
            return;
        }

        // DEF-194 cross-page: one server call for the whole job. Mirrors the
        // current filter chips so what the user saw on the table is what we
        // delete on the server. Negative IDs (in-memory new rows from import
        // preview) are stripped from any explicit-ids payload — they never
        // hit the DB in the first place.
        this.showLoading(true);
        try {
            const search = (document.getElementById('searchProductIds')?.value || '').trim();
            const typeFilter = document.getElementById('filterByType')?.value || '';
            const partnerFilter = document.getElementById('filterByPartner')?.value || '';

            const body = crossPage
                ? { deleteAll: true, search, type: typeFilter, partner: partnerFilter }
                : { ids: Array.from(this.selectedIds).filter(id => id > 0) };

            const url = `/api/productids/certificate/${this.certificateId}/bulk-delete`;
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });

            this.cache.clear();
            this.clearAllSelection();

            if (response.ok) {
                const json = await response.json().catch(() => ({}));
                const deleted = json?.data?.deleted ?? count;
                await this.loadProductIds();
                this.showSuccess(`${deleted} product ID(s) deleted successfully`);
            } else {
                await this.loadProductIds();
                this.showError('Failed to delete product IDs');
            }
        } catch (error) {
            console.error('Error during bulk delete:', error);
            this.showError('Error during bulk delete');
        } finally {
            this.showLoading(false);
        }
    }

    /**
     * Get all product IDs for form submission (used on Create page)
     * Returns array suitable for posting with the certificate form
     */
    getProductIdsForSubmission() {
        // CR-015: Status omitted — backend auto-derives from parent cert.
        return this.productIds.map(p => ({
            ProductIdType: p.productIdType,
            ProductIdValue: p.productIdValue,
            Partner: p.partner || '',
            ProductName: p.productName || '',
            ProductSku: p.productSku || '',
            Description: p.description || ''
        }));
    }

    handleTableAction(e) {
        const target = e.target.closest('button');
        if (!target) return;

        const productIdId = target.getAttribute('data-product-id');

        if (target.classList.contains('btn-edit-product-id')) {
            this.showEditModal(parseInt(productIdId));
        } else if (target.classList.contains('btn-delete-product-id')) {
            this.handleDelete(parseInt(productIdId));
        }
    }

    validateForm() {
        const type = document.getElementById('productIdType').value;
        const value = document.getElementById('productIdValue').value;

        if (!type || !value) {
            this.showError('Type and Product ID are required');
            return false;
        }

        return this.validateProductIdValue(document.getElementById('productIdValue'));
    }

    validateProductIdValue(input) {
        const type = document.getElementById('productIdType').value;
        const value = input.value.trim();

        if (!type || !value) {
            this.setValidationState(input, null);
            return true;
        }

        const isValid = this.isValidProductId(type, value);
        this.setValidationState(input, isValid);
        return isValid;
    }

    isValidProductId(type, value) {
        // DEF-161: ASIN regex is case-insensitive (`/i`) so users can type
        // lowercase Amazon ASINs like `b08n5wrwnw`. Save path uppercases.
        const patterns = {
            'ASIN': /^[A-Z0-9]{10}$/i,
            'UPC': /^[0-9]{12}$/,
            'GTIN': /^[0-9]{8}$|^[0-9]{12}$|^[0-9]{13}$|^[0-9]{14}$/,
            'EAN': /^[0-9]{13}$/,
            'SKU': /^[A-Z0-9\-_]{3,50}$/i,
            'MPN': /^[A-Z0-9\-_.]{1,50}$/i,
            'OTHER': /.{1,50}/
        };

        // Special handling for ISBN with checksum validation
        if (type === 'ISBN') {
            return this.validateIsbn(value);
        }

        const pattern = patterns[type];
        return pattern ? pattern.test(value) : value.length <= 50;
    }

    // DEF-161: Normalise to the format the standard mandates before save / dup check.
    // ASIN/UPC/GTIN/EAN/ISBN/SKU/MPN are uppercase by spec; OTHER is freeform.
    normalizeProductIdValue(type, value) {
        if (!value) return value;
        const upperTypes = ['ASIN', 'UPC', 'GTIN', 'EAN', 'ISBN', 'SKU', 'MPN'];
        return upperTypes.includes(type) ? value.toUpperCase() : value;
    }

    /**
     * Validates ISBN with proper checksum verification
     * Supports ISBN-10 and ISBN-13 formats
     */
    validateIsbn(isbn) {
        if (!isbn) return false;

        // Clean ISBN - remove non-digits and non-X characters
        const cleanIsbn = isbn.toUpperCase().replace(/[^0-9X]/g, '');

        if (cleanIsbn.length === 10) {
            return this.validateIsbn10Checksum(cleanIsbn);
        } else if (cleanIsbn.length === 13) {
            return this.validateIsbn13Checksum(cleanIsbn);
        }

        return false;
    }

    /**
     * Validates ISBN-10 checksum using modulo 11 algorithm
     */
    validateIsbn10Checksum(isbn10) {
        if (isbn10.length !== 10) return false;

        let sum = 0;
        for (let i = 0; i < 10; i++) {
            let digit;
            if (isbn10[i] === 'X') {
                // X is only valid in the last position and represents 10
                if (i !== 9) return false;
                digit = 10;
            } else if (/[0-9]/.test(isbn10[i])) {
                digit = parseInt(isbn10[i], 10);
            } else {
                return false;
            }

            // Multiply by weight (10 - position)
            sum += digit * (10 - i);
        }

        return sum % 11 === 0;
    }

    /**
     * Validates ISBN-13 checksum using EAN-13 algorithm
     */
    validateIsbn13Checksum(isbn13) {
        if (isbn13.length !== 13) return false;

        // ISBN-13 must start with 978 or 979
        if (!isbn13.startsWith('978') && !isbn13.startsWith('979')) return false;

        // X is not valid in ISBN-13
        if (isbn13.includes('X')) return false;

        let sum = 0;
        for (let i = 0; i < 13; i++) {
            if (!/[0-9]/.test(isbn13[i])) return false;

            const digit = parseInt(isbn13[i], 10);
            // Alternating weights: 1 for even positions, 3 for odd positions
            sum += (i % 2 === 0) ? digit : digit * 3;
        }

        return sum % 10 === 0;
    }

    setValidationState(input, isValid) {
        const feedback = document.getElementById('valueValidation');

        if (isValid === null) {
            input.classList.remove('is-valid', 'is-invalid');
            if (feedback) feedback.textContent = '';
            return;
        }

        if (isValid) {
            input.classList.remove('is-invalid');
            input.classList.add('is-valid');
            if (feedback) feedback.textContent = '';
        } else {
            input.classList.remove('is-valid');
            input.classList.add('is-invalid');
            if (feedback) {
                const type = document.getElementById('productIdType').value;
                feedback.textContent = this.getValidationMessage(type);
            }
        }
    }

    getValidationMessage(type) {
        const messages = {
            'ASIN': 'ASIN must be exactly 10 alphanumeric characters',
            'UPC': 'UPC must be exactly 12 digits',
            'GTIN': 'GTIN must be 8, 12, 13, or 14 digits',
            'EAN': 'EAN must be exactly 13 digits',
            'ISBN': 'Invalid ISBN format or checksum. Use ISBN-10 (10 digits) or ISBN-13 (13 digits starting with 978/979)',
            'SKU': 'SKU must be 3-50 alphanumeric characters',
            'MPN': 'MPN must be 1-50 alphanumeric characters',
            'OTHER': 'Must be 1-50 characters'
        };

        return messages[type] || 'Invalid format';
    }

    initializeTypeHelp() {
        this.updateTypeHelp('');
    }

    updateTypeHelp(type) {
        const helpElement = document.getElementById('typeHelp');
        if (!helpElement) return;

        const helpTexts = {
            'ASIN': 'Amazon Standard ID - 10 alphanumeric characters (e.g., B08N5WRWNW)',
            'UPC': 'Universal Product Code - 12 digits (e.g., 123456789012)',
            'GTIN': 'Global Trade Item Number - 8, 12, 13, or 14 digits',
            'EAN': 'European Article Number - 13 digits (e.g., 1234567890123)',
            'ISBN': 'ISBN-10 (10 digits, e.g., 0-306-40615-2) or ISBN-13 (13 digits starting with 978/979, e.g., 978-0-306-40615-7)',
            'SKU': 'Stock Keeping Unit - 3-50 alphanumeric characters',
            'MPN': 'Manufacturer Part Number - 1-50 alphanumeric characters',
            'OTHER': 'Any identifier up to 50 characters'
        };

        const text = helpTexts[type] || 'Select a type to see format requirements';
        const className = type ? `type-help-${type.toLowerCase()}` : '';

        helpElement.textContent = text;
        helpElement.className = `form-text ${className}`;
    }

    clearValidation() {
        const inputs = document.querySelectorAll('#productIdForm .form-control, #productIdForm .form-select');
        inputs.forEach(input => {
            input.classList.remove('is-valid', 'is-invalid');
        });

        const feedback = document.getElementById('valueValidation');
        if (feedback) feedback.textContent = '';
    }

    // Import preview state
    importPreviewData = [];
    importFileName = '';
    // Client request (ASIN upload): preview-only filters so a user can isolate
    // (e.g.) the 2 error rows out of a 1,000-row import. Purely visual — they
    // hide/show rendered rows and never touch selection or what gets applied.
    importStatusFilter = null; // 'new' | 'modified' | 'unchanged' | 'error' | null
    importSearchTerm = '';

    showImportModal() {
        // Open file picker directly
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.csv';
        input.onchange = async (e) => {
            const file = e.target.files[0];
            if (file) {
                this.importFileName = file.name;
                const reader = new FileReader();
                reader.onload = async (event) => {
                    const contents = event.target.result;
                    await this.parseAndPreviewImport(contents);
                };
                reader.readAsText(file);
            }
        };
        input.click();
    }

    async parseAndPreviewImport(contents) {
        const lines = contents.split('\n').filter(line => line.trim());
        if (lines.length < 2) {
            this.showError('CSV file must have a header row and at least one data row');
            return;
        }

        // Parse headers
        const rawHeaders = this.parseCSVLine(lines[0]).map(h => h.trim().toLowerCase());
        const parsedProductIds = [];

        // DEF-342 (QC #171): the template's Partner heading now carries the client's parenthetical hint
        // ("Partner (separate by comma)"), so an exact `productId.partner` lookup would miss it
        // and silently import every row with no partner. Resolve the column by prefix — the same
        // tolerance the certificate importer has server-side (DEF-202/DEF-316). Older templates
        // and exported files that say plain "Partner" keep matching on the exact key.
        const partnerHeader = rawHeaders.find(h => h === 'partner' || h.startsWith('partner ')) || 'partner';

        // Parse data rows
        for (let i = 1; i < lines.length; i++) {
            const line = lines[i].trim();
            if (!line) continue;

            const fields = this.parseCSVLine(line);
            if (fields.length < 2) continue;

            // Build product ID object from headers
            const productId = {};
            rawHeaders.forEach((header, index) => {
                productId[header] = fields[index]?.trim() || '';
            });

            const type = productId.type || '';
            let value = productId.value || productId.productid || productId['product id'] || '';

            // Strip Excel text format (="12345") the exporter adds to preserve leading zeros
            // (DEF-306), THEN fix scientific notation from Excel (e.g., 1.23E+12 → 123000000000).
            value = this.fixScientificNotation(this.stripExcelTextFormat(value));

            if (!type && !value) continue;

            // Same strip + scientific-notation fix on SKU.
            const sku = this.fixScientificNotation(this.stripExcelTextFormat(productId.productsku || productId.sku || ''));

            // CR-015: Status from CSV is ignored — derived from parent cert.
            // Header lookup is tolerant of both "ProductName"/"productname" and
            // "Product Name" (with space) so the new template order and any older
            // downloaded templates both parse correctly.
            parsedProductIds.push({
                rowNumber: i,
                productIdType: type.toUpperCase(),
                productIdValue: value,
                partner: productId[partnerHeader] || '',
                productName: productId.productname || productId['product name'] || '',
                productSku: sku,
                description: productId.description || ''
            });
        }

        if (parsedProductIds.length === 0) {
            this.showError('No valid product IDs found in CSV file');
            return;
        }

        // Compare with existing and build preview
        await this.buildImportPreview(parsedProductIds);
    }

    /**
     * Converts scientific notation strings back to full integers.
     * Excel often converts long numeric IDs (UPCs, GTINs, SKUs) to scientific notation
     * like "1.23456E+11" when saving to CSV. This restores the original number.
     */
    /**
     * Strips the Excel text format (="value") that the CSV exporter wraps identifier codes in
     * to preserve leading zeros (DEF-306). Handles both the intact form ="12345" and the residue
     * left by parseCSVLine, which strips the surrounding quotes and leaves a stray leading "="
     * (e.g. ="012345" -> =012345). A product ID value / SKU never legitimately begins with "=",
     * so removing it is safe and makes a raw re-import of an exported file lossless.
     */
    stripExcelTextFormat(value) {
        if (!value || typeof value !== 'string') return value;
        let v = value.trim();
        if (v.startsWith('="') && v.endsWith('"')) {
            return v.slice(2, -1).replace(/""/g, '"');   // intact form ="..."
        }
        if (v.startsWith('=')) {
            return v.slice(1);                            // parseCSVLine residue =...
        }
        return v;
    }

    fixScientificNotation(value) {
        if (!value || typeof value !== 'string') return value;
        const trimmed = value.trim();
        // Match patterns like 1.23E+12, 1.23456E+11, 1E+10, etc.
        if (/^[\d.]+[eE][+\-]?\d+$/.test(trimmed)) {
            try {
                const num = Number(trimmed);
                if (!isNaN(num) && isFinite(num)) {
                    // Convert to fixed-point string (no decimals for product IDs)
                    return num.toFixed(0);
                }
            } catch {
                // Fall through to return original value
            }
        }
        return value;
    }

    parseCSVLine(line) {
        const result = [];
        let current = '';
        let inQuotes = false;

        for (let i = 0; i < line.length; i++) {
            const char = line[i];
            if (char === '"') {
                inQuotes = !inQuotes;
            } else if (char === ',' && !inQuotes) {
                result.push(current.trim());
                current = '';
            } else {
                current += char;
            }
        }
        result.push(current.trim());
        return result;
    }

    async buildImportPreview(parsedProductIds) {
        this.importPreviewData = [];

        // Track values seen in this import for duplicate detection
        const valuesInImport = new Map(); // "type:value" -> rowNumber

        // DEF-153: With CR-018 server-paging, this.productIds only holds the
        // currently visible page. Pre-fetch the FULL list once so the preview
        // can correctly classify rows as new / modified / unchanged against
        // every existing row on the cert, not just the visible page. The
        // un-paged endpoint already exists for the export path and returns
        // the full list as DTOs in the standard ApiResponse wrapper.
        let existingByKey;
        if (this.serverPaged && this.certificateId) {
            existingByKey = new Map();
            try {
                const resp = await fetch(`/api/productids/certificate/${this.certificateId}`);
                if (resp.ok) {
                    const json = await resp.json();
                    const all = Array.isArray(json.data) ? json.data : (Array.isArray(json) ? json : []);
                    for (const p of all) {
                        const key = `${(p.productIdType || '').toUpperCase()}:${(p.productIdValue || '').toUpperCase()}`;
                        existingByKey.set(key, p);
                    }
                }
            } catch (e) {
                console.warn('[buildImportPreview] full-list fetch failed, falling back to current page only:', e);
            }
        }

        for (const importItem of parsedProductIds) {
            // Match by type + value (unique identifier for product IDs).
            // DEF-153: prefer the pre-fetched full-list map (server-paged
            // mode); fall back to scanning this.productIds when no map was
            // built (Create page in-memory mode, or fetch failed above).
            let existingItem = null;
            if (existingByKey) {
                const key = `${(importItem.productIdType || '').toUpperCase()}:${(importItem.productIdValue || '').toUpperCase()}`;
                existingItem = existingByKey.get(key) || null;
            } else {
                existingItem = this.productIds.find(p =>
                    p.productIdType?.toUpperCase() === importItem.productIdType?.toUpperCase() &&
                    p.productIdValue?.toUpperCase() === importItem.productIdValue?.toUpperCase()
                ) || null;
            }

            const previewRow = {
                rowNumber: importItem.rowNumber,
                data: importItem,
                existingData: existingItem || null,
                status: 'new',
                changes: [],
                errors: [],
                selected: true,
                action: 'create'
            };

            if (existingItem) {
                // Compare fields and determine status
                // CR-015: Status comparison removed — Status is no longer in the import payload.
                const changes = [];

                if ((existingItem.partner || '') !== (importItem.partner || '')) {
                    changes.push({ field: 'Partner', oldValue: existingItem.partner || '', newValue: importItem.partner || '' });
                }
                if ((existingItem.productName || '') !== (importItem.productName || '')) {
                    changes.push({ field: 'Product Name', oldValue: existingItem.productName || '', newValue: importItem.productName || '' });
                }
                if ((existingItem.productSku || '') !== (importItem.productSku || '')) {
                    changes.push({ field: 'SKU', oldValue: existingItem.productSku || '', newValue: importItem.productSku || '' });
                }

                if (changes.length > 0) {
                    previewRow.status = 'modified';
                    previewRow.changes = changes;
                    previewRow.action = 'update';
                } else {
                    previewRow.status = 'unchanged';
                    previewRow.selected = false;
                    previewRow.action = 'skip';
                }
            }

            // Validate
            if (!importItem.productIdType || !importItem.productIdType.trim()) {
                previewRow.errors.push('Type is required');
            } else {
                const validTypes = ['ASIN', 'UPC', 'GTIN', 'EAN', 'ISBN', 'SKU', 'MPN', 'OTHER'];
                if (!validTypes.includes(importItem.productIdType.toUpperCase())) {
                    previewRow.errors.push(`Invalid type: "${importItem.productIdType}"`);
                }
            }

            if (!importItem.productIdValue || !importItem.productIdValue.trim()) {
                previewRow.errors.push('Product ID value is required');
            } else {
                // Check for duplicate in this import file
                const key = `${importItem.productIdType}:${importItem.productIdValue}`.toUpperCase();
                if (valuesInImport.has(key)) {
                    const firstRow = valuesInImport.get(key);
                    previewRow.errors.push(`Duplicate in import file (first seen on row ${firstRow})`);
                } else {
                    valuesInImport.set(key, importItem.rowNumber);
                }

                // Validate format based on type
                if (importItem.productIdType && !this.isValidProductId(importItem.productIdType, importItem.productIdValue)) {
                    previewRow.errors.push(this.getValidationMessage(importItem.productIdType));
                }
            }

            if (previewRow.errors.length > 0) {
                previewRow.status = 'error';
                previewRow.selected = false;
            }

            this.importPreviewData.push(previewRow);
        }

        // Show preview modal
        this.showImportPreviewModal();
    }

    showImportPreviewModal() {
        const modal = document.getElementById('import-preview-modal');
        if (!modal) return;

        // Client request (ASIN upload): every fresh import starts unfiltered.
        this.importStatusFilter = null;
        this.importSearchTerm = '';
        const searchInput = document.getElementById('import-preview-search');
        if (searchInput) searchInput.value = '';

        // Calculate statistics
        const stats = {
            new: this.importPreviewData.filter(r => r.status === 'new').length,
            modified: this.importPreviewData.filter(r => r.status === 'modified').length,
            unchanged: this.importPreviewData.filter(r => r.status === 'unchanged').length,
            errors: this.importPreviewData.filter(r => r.status === 'error').length
        };

        // Update stats display
        document.getElementById('stat-new').textContent = stats.new;
        document.getElementById('stat-modified').textContent = stats.modified;
        document.getElementById('stat-unchanged').textContent = stats.unchanged;
        document.getElementById('stat-errors').textContent = stats.errors;

        // Build table rows
        const tbody = document.getElementById('import-preview-body');
        tbody.innerHTML = '';

        this.importPreviewData.forEach((row, index) => {
            const tr = document.createElement('tr');
            tr.dataset.index = index;

            const statusColors = {
                'new': { bg: '#e8f5e1', color: '#447638', label: 'New' },
                'modified': { bg: '#e6f7ff', color: '#002B76', label: 'Modified' },
                'unchanged': { bg: '#f5f5f5', color: '#999', label: 'Unchanged' },
                'error': { bg: '#ffebee', color: '#ff5252', label: 'Error' }
            };
            const statusStyle = statusColors[row.status];

            // Build changes/errors display
            let changesHtml = '';
            if (row.errors.length > 0) {
                changesHtml = `<span style="color: #ff5252; font-size: 12px;">${row.errors.join('; ')}</span>`;
            } else if (row.changes.length > 0) {
                changesHtml = row.changes.map(c =>
                    `<span style="font-size: 12px; color: #666;">${c.field}: <del style="color: #999;">${c.oldValue || '(empty)'}</del> → <strong style="color: #002B76;">${c.newValue || '(empty)'}</strong></span>`
                ).join('<br>');
            } else if (row.status === 'unchanged') {
                changesHtml = '<span style="font-size: 12px; color: #999;">No changes</span>';
            } else {
                changesHtml = '<span style="font-size: 12px; color: #447638;">Will be created</span>';
            }

            tr.innerHTML = `
                <td style="padding: 10px;">
                    <input type="checkbox" class="import-row-checkbox" data-index="${index}"
                        ${row.selected ? 'checked' : ''} ${row.status === 'error' ? 'disabled' : ''}
                        onchange="productIdManager.toggleImportRow(${index}, this.checked)">
                </td>
                <td style="padding: 10px;">
                    <span style="display: inline-block; padding: 4px 10px; border-radius: 12px; font-size: 12px; font-weight: 500;
                        background: ${statusStyle.bg}; color: ${statusStyle.color};">${statusStyle.label}</span>
                </td>
                <td style="padding: 10px; font-weight: 500;">${row.data.productIdType || ''}</td>
                <td style="padding: 10px; font-family: monospace;">${row.data.productIdValue || ''}</td>
                <td style="padding: 10px;">${row.data.productName || '-'}</td>
                <!-- DEF-202 (QC #179): show SKU column so client can verify import data before confirming -->
                <td style="padding: 10px; font-family: monospace;">${row.data.productSku || '-'}</td>
                <td style="padding: 10px;">${row.data.partner || '-'}</td>
                <td style="padding: 10px;">${changesHtml}</td>
            `;

            tbody.appendChild(tr);
        });

        // Client request (ASIN upload): apply any active status/search filter to
        // the freshly-built rows and refresh the filter UI (highlight + count).
        this.applyImportRowVisibility();

        // Show modal
        modal.style.display = 'block';
        this.updateApplyButtonState();
    }

    // Client request (ASIN upload): toggle a status filter from the summary boxes.
    // Clicking the active box again clears it.
    toggleImportStatusFilter(status) {
        this.importStatusFilter = (this.importStatusFilter === status) ? null : status;
        this.applyImportRowVisibility();
    }

    // Client request (ASIN upload): free-text search across the preview rows.
    searchImportPreview(term) {
        this.importSearchTerm = (term || '').trim().toLowerCase();
        this.applyImportRowVisibility();
    }

    clearImportPreviewFilters() {
        this.importStatusFilter = null;
        this.importSearchTerm = '';
        const searchInput = document.getElementById('import-preview-search');
        if (searchInput) searchInput.value = '';
        this.applyImportRowVisibility();
    }

    // Client request (ASIN upload): show/hide already-rendered rows based on the
    // active status box + search term, then update the box highlight, the
    // "showing X of Y" count, and the Clear-filter button. Never rebuilds rows or
    // changes selection — apply/select-all still operate on the full data set.
    /**
     * DEF-312 — single source of truth for "is this row currently shown?".
     *
     * Extracted from applyImportRowVisibility so the row rendering and the header
     * select-all can never disagree about what the active filter covers. Any future
     * filter dimension goes in HERE and both callers inherit it.
     */
    importRowMatchesFilter(row) {
        if (!row) return false;

        const status = this.importStatusFilter;
        if (status && row.status !== status) return false;

        const term = this.importSearchTerm;
        if (term) {
            const haystack = [
                row.data.productIdType,
                row.data.productIdValue,
                row.data.productName,
                row.data.productSku,
                row.data.partner,
                (row.errors || []).join(' ')
            ].filter(Boolean).join(' ').toLowerCase();
            if (!haystack.includes(term)) return false;
        }

        return true;
    }

    /** Is a status or search filter currently narrowing the preview? */
    isImportFiltered() {
        return !!this.importStatusFilter || !!this.importSearchTerm;
    }

    applyImportRowVisibility() {
        const tbody = document.getElementById('import-preview-body');
        if (!tbody) return;

        const status = this.importStatusFilter;
        const total = this.importPreviewData.length;
        let visible = 0;

        Array.from(tbody.children).forEach(tr => {
            const idx = parseInt(tr.dataset.index);
            const row = this.importPreviewData[idx];
            if (!row) return;

            const show = this.importRowMatchesFilter(row);
            tr.style.display = show ? '' : 'none';
            if (show) visible++;
        });

        // Highlight the active status box (2px colored border) and reset the rest.
        const boxStatuses = ['new', 'modified', 'unchanged', 'error'];
        const borderColors = { new: '#447638', modified: '#002B76', unchanged: '#999', error: '#ff5252' };
        boxStatuses.forEach(s => {
            const box = document.getElementById(`stat-box-${s === 'error' ? 'errors' : s}`);
            if (box) {
                box.style.borderColor = (status === s) ? borderColors[s] : 'transparent';
                box.setAttribute('aria-pressed', String(status === s));
            }
        });

        // "Showing X of Y" count + Clear-filter button visibility.
        const filtering = this.isImportFiltered();
        const countEl = document.getElementById('import-preview-count');
        if (countEl) countEl.textContent = filtering ? `Showing ${visible} of ${total}` : '';
        const clearBtn = document.getElementById('import-preview-clear-filter');
        if (clearBtn) clearBtn.style.display = filtering ? '' : 'none';
    }

    toggleImportRow(index, checked) {
        if (this.importPreviewData[index]) {
            this.importPreviewData[index].selected = checked;
            this.updateApplyButtonState();
        }
    }

    /**
     * DEF-312 — "select all" acts on the rows the user can SEE, not the whole batch.
     *
     * The CR-025 status/search filter made the old blanket behaviour a real hazard:
     * filter a 1,000-row import down to the 3 error rows, click the header checkbox,
     * and it flipped `selected` on all 997 hidden rows too — the only cue being the
     * Apply button's count. On a large import that is a wrong-import risk, so when a
     * filter is active the toggle is scoped to the filtered set. With no filter on,
     * behaviour is unchanged (every non-error row).
     *
     * Rows hidden by the filter deliberately KEEP their existing selection — the user
     * never saw them, so we neither select nor deselect on their behalf.
     */
    toggleAllImportRows(checked) {
        this.importPreviewData.forEach((row, index) => {
            if (row.status === 'error') return;
            if (!this.importRowMatchesFilter(row)) return;

            row.selected = checked;
            const checkbox = document.querySelector(`.import-row-checkbox[data-index="${index}"]`);
            if (checkbox) checkbox.checked = checked;
        });
        this.updateApplyButtonState();
    }

    updateApplyButtonState() {
        const selectedCount = this.importPreviewData.filter(r => r.selected && r.status !== 'error').length;
        const applyBtn = document.getElementById('apply-import-btn');
        if (applyBtn) {
            applyBtn.disabled = selectedCount === 0;
            applyBtn.innerHTML = selectedCount === 0
                ? '<i class="fas fa-check me-1"></i> Apply Import'
                : `<i class="fas fa-check me-1"></i> Apply Import (${selectedCount})`;
        }
    }

    closeImportPreviewModal() {
        const modal = document.getElementById('import-preview-modal');
        if (modal) {
            modal.style.display = 'none';
        }
        this.importPreviewData = [];
        this.importFileName = '';
    }

    async applyImport() {
        // DEF-154: When "Replace all existing" is checked, the import file is
        // the new source of truth — every non-error row in the file is part
        // of the import set, regardless of per-row selection. The per-row
        // checkboxes only have meaning in normal merge mode (toggle off).
        // This is critical for the server-side deactivation logic: the bulk
        // endpoint builds importKeys from the items it receives, and any
        // existing row whose key isn't in importKeys gets deactivated. If we
        // sent only "selectedRows" or only "creates" (the prior bug), keys for
        // unchecked / modified rows would be missing → the server would
        // wrongly deactivate them.
        const overwriteToggle = document.getElementById('overwrite-existing-toggle');
        const overwriteExisting = !!(overwriteToggle && overwriteToggle.checked);

        const selectedRows = overwriteExisting
            ? this.importPreviewData.filter(r => r.status !== 'error')
            : this.importPreviewData.filter(r => r.selected && r.status !== 'error');

        if (selectedRows.length === 0) {
            this.showError(overwriteExisting
                ? 'Import file has no valid rows. Replace-all needs at least one valid row to define the new state.'
                : 'No rows selected for import');
            return;
        }

        const applyBtn = document.getElementById('apply-import-btn');
        if (applyBtn) {
            applyBtn.disabled = true;
            applyBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> Importing...';
        }

        // For new certificates (Create page), store in memory instead of API
        if (!this.certificateId || this.certificateId === 0) {
            let successCount = 0;
            for (const row of selectedRows) {
                // Generate a temporary negative ID for new items
                const tempId = -(Date.now() + successCount);
                // CR-015: temp items have no status; backend derives from cert on save.
                const newItem = {
                    id: tempId,
                    certificateId: 0,
                    productIdType: row.data.productIdType,
                    productIdValue: row.data.productIdValue,
                    partner: row.data.partner || '',
                    productName: row.data.productName || '',
                    productSku: row.data.productSku || '',
                    description: row.data.description || '',
                    isNew: true // Flag for items to be saved with certificate
                };
                this.productIds.push(newItem);
                successCount++;
            }

            this.filteredProductIds = [...this.productIds];
            this.renderTable();
            this.updateCount();
            this.closeImportPreviewModal();
            this.showSuccess(`Added ${successCount} product ID(s) to be saved with certificate`);
            return;
        }

        // DEF-112: previously this loop fired one fetch() per row. With
        // AspNetCoreRateLimit set to 100/min for */api/*, any import larger than
        // 100 rows blew the budget and the rest returned 429 / "Unknown error".
        // Now: creates go to the bulk endpoint as a single request; updates
        // (typically a small count from preview "Modified" rows) still PUT
        // individually. Replace-all deactivation is handled server-side via the
        // overwriteExisting flag — no more 1000 individual DELETEs from the JS.
        //
        // DEF-154: When overwriteExisting is true, we MUST send every row in
        // the import set to the bulk endpoint, not just the "creates". The
        // server's overwrite logic deactivates any existing row whose key is
        // not in the request's items array. If we sent only "creates", the
        // keys for "modified" rows would be missing and those existing rows
        // would be wrongly deactivated. The server already handles dups
        // (silent success in overwrite mode), so sending matching rows in
        // the bulk request is harmless on the insert path. Updates still go
        // through PUT individually after, to apply field changes.
        const updates = selectedRows.filter(r => r.action === 'update' && r.existingData);
        // DEF-191: filter must be `action === 'create'`, not `!== 'update'`.
        // The looser `!== 'update'` form let `action === 'skip'` rows (CSV rows
        // whose type+value already match an existing PID with identical fields)
        // through into the bulk POST. The server then rejects them as
        // "already exists" and the user sees "Import failed" even though they
        // didn't actually request any change. Now skip rows are treated as
        // no-ops in non-overwrite mode and surfaced in the success toast.
        const bulkRows = overwriteExisting
            ? selectedRows
            : selectedRows.filter(r => r.action === 'create');
        const skippedExisting = overwriteExisting
            ? []
            : selectedRows.filter(r => r.action === 'skip');

        let createSuccess = 0;
        let updateSuccess = 0;
        let errorCount = 0;
        const errorMessages = [];

        // DEF-154: also fire the bulk endpoint when overwriteExisting is true
        // even if bulkRows is empty — never happens in practice because the
        // overwrite branch above keeps every non-error row, but kept here so
        // the deactivation logic can never silently drop.
        if (bulkRows.length > 0 || overwriteExisting) {
            try {
                // CR-015: Status omitted — backend auto-derives from parent cert.
                const items = bulkRows.map(r => ({
                    Type: r.data.productIdType,
                    Value: r.data.productIdValue,
                    Partner: r.data.partner || null,
                    ProductName: r.data.productName || null,
                    ProductSku: r.data.productSku || null,
                    Description: r.data.description || null
                }));

                const bulkResp = await fetch(`/api/productids/certificate/${this.certificateId}/bulk`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ items, overwriteExisting })
                });

                if (bulkResp.ok) {
                    const json = await bulkResp.json();
                    const result = json.data || json.Data || json;
                    createSuccess = result.successCount ?? result.SuccessCount ?? 0;
                    const reportedErrors = result.errorCount ?? result.ErrorCount ?? 0;
                    errorCount += reportedErrors;
                    const serverErrors = result.errors ?? result.Errors ?? [];
                    if (Array.isArray(serverErrors) && serverErrors.length > 0) {
                        errorMessages.push(...serverErrors.slice(0, 5));
                        if (serverErrors.length > 5) {
                            errorMessages.push(`...and ${serverErrors.length - 5} more`);
                        }
                    }
                } else {
                    errorCount += bulkRows.length;
                    let bodyText = '';
                    try { bodyText = await bulkResp.text(); } catch { /* ignore */ }
                    errorMessages.push(`Bulk import HTTP ${bulkResp.status}: ${bodyText.slice(0, 200) || bulkResp.statusText}`);
                    console.error('Bulk import failed:', bulkResp.status, bodyText);
                }
            } catch (err) {
                errorCount += bulkRows.length;
                errorMessages.push(`Bulk import network error: ${err.message || err}`);
                console.error('Bulk import network error:', err);
            }
        }

        for (const row of updates) {
            try {
                const productIdData = {
                    Id: row.existingData.id,
                    CertificateId: this.certificateId,
                    ProductIdType: row.data.productIdType,
                    ProductIdValue: row.data.productIdValue,
                    Partner: row.data.partner || '',
                    ProductName: row.data.productName || '',
                    ProductSku: row.data.productSku || '',
                    Description: row.data.description || ''
                };
                const response = await fetch(`/api/productids/${row.existingData.id}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(productIdData)
                });
                if (response.ok) {
                    updateSuccess++;
                } else {
                    errorCount++;
                    let errorMsg = '';
                    try {
                        const errJson = await response.json();
                        errorMsg = errJson.message || errJson.Message || '';
                    } catch {
                        errorMsg = response.statusText;
                    }
                    errorMessages.push(`Row ${row.rowNumber} update: ${errorMsg || `HTTP ${response.status}`}`);
                }
            } catch (err) {
                errorCount++;
                errorMessages.push(`Row ${row.rowNumber} update: ${err.message || 'Network error'}`);
            }
        }

        this.closeImportPreviewModal();

        const totalSuccess = createSuccess + updateSuccess;
        // DEF-191: skippedExisting (rows the user checked that already exist
        // with identical fields) counts as a successful no-op. Include it in
        // the "we did something useful" condition so the result toast is
        // accurate when the only selected rows were skips.
        if (totalSuccess > 0 || (errorCount === 0 && (bulkRows.length + updates.length + skippedExisting.length) > 0)) {
            this.cache.clear(); // CR-018: invalidate page cache after bulk import
            await this.loadProductIds();
            let msg;
            if (createSuccess > 0 && updateSuccess > 0) {
                msg = `Imported ${createSuccess} new, updated ${updateSuccess}`;
            } else if (updateSuccess > 0) {
                msg = `Updated ${updateSuccess} product ID(s)`;
            } else if (createSuccess > 0) {
                msg = `Imported ${createSuccess} product ID(s)`;
            } else if (skippedExisting.length > 0) {
                msg = `${skippedExisting.length} row(s) already on this certificate — nothing to change`;
            } else {
                msg = `Imported ${createSuccess} product ID(s)`;
            }
            if (overwriteExisting) {
                msg += ' (replace-all mode applied)';
            } else if (skippedExisting.length > 0 && (createSuccess > 0 || updateSuccess > 0)) {
                msg += `; ${skippedExisting.length} already on file`;
            }
            if (errorCount > 0) {
                msg += `. ${errorCount} failed: ${errorMessages.slice(0, 3).join('; ')}`;
            }
            this.showSuccess(msg);
        } else {
            this.showError(`Import failed: ${errorMessages.slice(0, 3).join('; ') || 'Unknown error'}`);
        }
    }

    async exportProductIds() {
        // For new certificates (no ID yet), export from in-memory data
        if (!this.certificateId || this.certificateId === 0) {
            this.exportInMemoryProductIds();
            return;
        }

        try {
            this.showLoading(true);
            const response = await fetch(`/api/productids/certificate/${this.certificateId}/export`);

            if (response.ok) {
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `certificate-${this.certificateId}-product-ids.csv`;
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                document.body.removeChild(a);

                this.showSuccess('Product IDs exported successfully');
            } else {
                const errorData = await response.json().catch(() => null);
                this.showError(errorData?.message || 'Failed to export product IDs');
            }
        } catch (error) {
            console.error('Export error:', error);
            this.showError('Export failed');
        } finally {
            this.showLoading(false);
        }
    }

    exportInMemoryProductIds() {
        // CR-015: Status column dropped from in-memory export — derived from parent cert.
        // DEF-110 alignment: header order + labels match the on-screen table
        // (Type, Product ID, Product Name, SKU, Partner, Created, Created By).
        // Description trails — it's saved data but not a table column.
        // For new (unsaved) certificates the in-memory items have no createdAt/createdBy yet,
        // so Created/Created By cells will be blank in the export — accurate for that state.
        const header = 'Type,Product ID,Product Name,SKU,Partner,Created,Created By,Description';
        const formatCreated = (iso) => {
            if (!iso) return '';
            const d = new Date(iso);
            return isNaN(d.getTime()) ? '' : d.toISOString().slice(0, 10);
        };
        // CR-171: Partner column carries ALL associated partners, pipe-delimited (e.g. "Amazon|Novi");
        // empty = shared with all the certificate's partners. Re-import understands the same format.
        const rows = this.productIds.map(pid =>
            `"${pid.productIdType || ''}","${pid.productIdValue || ''}","${pid.productName || ''}","${pid.productSku || ''}","${this.getPartnerList(pid).join('|')}","${formatCreated(pid.createdAt)}","${(pid.createdByUser && pid.createdByUser.username) || pid.createdBy || ''}","${pid.description || ''}"`
        );
        const csvContent = [header, ...rows].join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'product-ids-export.csv';
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        if (this.productIds.length === 0) {
            this.showSuccess('Exported empty template (no product IDs added yet)');
        } else {
            this.showSuccess(`Exported ${this.productIds.length} product ID(s)`);
        }
    }

    downloadSample() {
        // CR-015: Status header removed from import template — auto-derived from parent cert.
        // Order matches the on-screen Product ID Manager table and the in-memory export
        // (Type, Product ID, Product Name, SKU, Partner, Description) so users see one
        // consistent column order across UI, export, and import template.
        // DEF-342 / QC #171 (re-flagged 2026-08-07): the client asked for the separator hint in the
        // Partner column heading of the template behind the Product Identifiers "Template"
        // button. DEF-316 added it to the *certificate import* template — a different file,
        // reached from a different button — which is why this one still read plain "Partner".
        // Wording is client-specified; do not reword ([[feedback_client_requested_labels_immutable]]).
        //
        // Headers are quoted because "Partner (separate by comma)" contains a comma: unquoted,
        // every CSV reader (Excel included, per RFC 4180) would split it into two columns.
        // Comma is genuinely accepted on the way back in — getPartnerList() splits on /[|,]/ and
        // the backend SyncPartners splits on '|'/',' — so the hint is truthful, not aspirational.
        const csvContent = '"Type","Product ID","Product Name","SKU","Partner (separate by comma)","Description"';

        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'product-ids-sample.csv';
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
    }

    showLoading(show) {
        // Implement loading indicator
        const container = this.container;
        if (show) {
            if (!container.querySelector('.loading-overlay')) {
                const overlay = document.createElement('div');
                overlay.className = 'loading-overlay position-relative';
                overlay.innerHTML = '<div class="loading-spinner"></div>';
                container.style.position = 'relative';
                container.appendChild(overlay);
            }
        } else {
            const overlay = container.querySelector('.loading-overlay');
            if (overlay) {
                overlay.remove();
            }
        }
    }

    showError(message) {
        // Use AdminCore for consistent modal-style notifications across the app
        if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
            AdminCore.showNotification(message, 'error');
        } else {
            console.error(message);
        }
    }

    showSuccess(message) {
        // Use AdminCore for consistent modal-style notifications across the app
        if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
            AdminCore.showNotification(message, 'success');
        } else {
            console.log(message);
        }
    }

    // QC #200 (DEF-282): guard against missing/invalid dates so unsaved (in-memory,
    // Create-page) product IDs — which have no createdAt until the cert is saved —
    // render "—" instead of the alarming "Invalid Date".
    formatDate(dateString) {
        if (!dateString) return '—';
        const d = new Date(dateString);
        return isNaN(d.getTime()) ? '—' : d.toLocaleDateString();
    }

    formatDateTime(dateString) {
        if (!dateString) return '';
        const d = new Date(dateString);
        return isNaN(d.getTime()) ? '' : d.toLocaleString();
    }
}

// Initialize the Product ID Manager
function initProductIdManager() {
    // Guard against double initialization
    if (window.productIdManager) {
        return;
    }

    const productIdManagerEl = document.querySelector('.product-id-manager');
    if (productIdManagerEl) {
        const certificateId = productIdManagerEl.getAttribute('data-certificate-id');
        const isReadOnly = productIdManagerEl.hasAttribute('data-readonly');

        // Initialize even if certificateId is 0 (new certificate on Create page)
        if (certificateId !== null && certificateId !== undefined) {
            window.productIdManager = new ProductIdManager(null, parseInt(certificateId), isReadOnly);
        }
    }
}

// Initialize when DOM is loaded, or immediately if already loaded
// (scripts in @section Scripts load after DOMContentLoaded has fired)
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initProductIdManager);
} else {
    initProductIdManager();
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ProductIdManager;
}