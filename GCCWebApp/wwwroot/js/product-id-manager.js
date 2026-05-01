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
        this.loadProductIds();
        this.initializeTypeHelp();
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
        }
    }

    async loadProductIds() {
        // Skip loading if certificate ID is 0 (new certificate being created)
        if (!this.certificateId || this.certificateId === 0 || this.certificateId === '0') {
            this.productIds = [];
            this.filteredProductIds = [];
            this.currentPage = 1;
            this.renderTable();
            this.updateCount();
            return;
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

        if (this.filteredProductIds.length === 0) {
            tbody.innerHTML = this.getEmptyStateHtml();
            this.renderPagination();
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
    }

    renderPagination() {
        const PAGE_SIZE = 50;
        const totalItems = this.filteredProductIds.length;
        if (totalItems <= PAGE_SIZE) {
            // Remove pagination if it exists
            const existing = document.getElementById('pid-pagination');
            if (existing) existing.remove();
            return;
        }

        const totalPages = Math.ceil(totalItems / PAGE_SIZE);
        const start = (this.currentPage - 1) * PAGE_SIZE + 1;
        const end = Math.min(this.currentPage * PAGE_SIZE, totalItems);

        let paginationHtml = `
            <div id="pid-pagination" style="display: flex; justify-content: space-between; align-items: center; padding: 8px 12px; border-top: 1px solid #e9ecef; font-size: 14px;">
                <span style="color: #666;">Showing ${start}-${end} of ${totalItems}</span>
                <div style="display: flex; gap: 4px;">
                    <button type="button" onclick="productIdManager.goToPage(1)" ${this.currentPage === 1 ? 'disabled' : ''}
                        style="padding: 4px 8px; border: 1px solid #ced4da; border-radius: 4px; background: white; cursor: pointer; font-size: 13px;">First</button>
                    <button type="button" onclick="productIdManager.goToPage(${this.currentPage - 1})" ${this.currentPage === 1 ? 'disabled' : ''}
                        style="padding: 4px 8px; border: 1px solid #ced4da; border-radius: 4px; background: white; cursor: pointer; font-size: 13px;">Prev</button>
                    <span style="padding: 4px 10px; font-weight: 500;">Page ${this.currentPage} of ${totalPages}</span>
                    <button type="button" onclick="productIdManager.goToPage(${this.currentPage + 1})" ${this.currentPage === totalPages ? 'disabled' : ''}
                        style="padding: 4px 8px; border: 1px solid #ced4da; border-radius: 4px; background: white; cursor: pointer; font-size: 13px;">Next</button>
                    <button type="button" onclick="productIdManager.goToPage(${totalPages})" ${this.currentPage === totalPages ? 'disabled' : ''}
                        style="padding: 4px 8px; border: 1px solid #ced4da; border-radius: 4px; background: white; cursor: pointer; font-size: 13px;">Last</button>
                </div>
            </div>`;

        // Insert or replace pagination after the table container
        const container = document.querySelector('.product-ids-table-container');
        const existing = document.getElementById('pid-pagination');
        if (existing) existing.remove();
        if (container) {
            container.insertAdjacentHTML('afterend', paginationHtml);
        }
    }

    goToPage(page) {
        this.currentPage = page;
        this.renderTable();
        // Scroll table container to top
        const container = document.querySelector('.product-ids-table-container');
        if (container) container.scrollTop = 0;
    }

    getProductIdRowHtml(productId) {
        const actionsHtml = this.isReadOnly ? '' : `
            <td>
                <div class="btn-group btn-group-sm" role="group">
                    <button type="button" class="btn btn-outline-primary btn-edit-product-id"
                            data-product-id="${productId.id}" title="Edit">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button type="button" class="btn btn-outline-danger btn-delete-product-id"
                            data-product-id="${productId.id}" title="Delete">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </td>`;

        return `
            <tr data-product-id="${productId.id}">
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
                    ${productId.partner ?
                        `<span class="partner-badge partner-${productId.partner.toLowerCase()}">${productId.partner}</span>` :
                        '<span class="text-muted">-</span>'}
                </td>
                <td class="created-date" title="${this.formatDateTime(productId.createdAt)}">
                    ${this.formatDate(productId.createdAt)}
                </td>
                <td class="created-by" title="${productId.createdBy || 'System'}">
                    ${productId.createdBy || 'System'}
                </td>
                ${actionsHtml}
            </tr>`;
    }

    getEmptyStateHtml() {
        const colspan = this.isReadOnly ? 7 : 8;
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
        const typeFilter = document.getElementById('filterByType')?.value || '';
        // CR-015: Status filter removed — product ID status follows parent cert.
        const partnerFilter = document.getElementById('filterByPartner')?.value || '';
        const searchFilter = document.getElementById('searchProductIds')?.value.toLowerCase() || '';

        this.filteredProductIds = this.productIds.filter(productId => {
            const typeMatch = !typeFilter || productId.productIdType === typeFilter;
            const partnerMatch = !partnerFilter || productId.partner === partnerFilter;
            const searchMatch = !searchFilter ||
                productId.productIdValue.toLowerCase().includes(searchFilter) ||
                (productId.productName && productId.productName.toLowerCase().includes(searchFilter)) ||
                (productId.productSku && productId.productSku.toLowerCase().includes(searchFilter));

            return typeMatch && partnerMatch && searchMatch;
        });

        this.currentPage = 1;
        this.renderTable();
        this.updatePaginationInfo();
    }

    updateCount() {
        const countElement = document.getElementById('productIdCount');
        if (countElement) {
            countElement.textContent = this.productIds.length;
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
            selects.forEach(select => select.selectedIndex = 0);

            document.getElementById('productIdId').value = '0';
            document.getElementById('certificateId').value = this.certificateId;
            // CR-015: Status field removed from modal — auto-derived from parent cert on save.
            this.clearValidation();
        }
    }

    populateForm(productId) {
        document.getElementById('productIdId').value = productId.id;
        document.getElementById('certificateId').value = productId.certificateId;
        document.getElementById('productIdType').value = productId.productIdType;
        document.getElementById('productIdValue').value = productId.productIdValue;
        // CR-015: Status field removed from modal — derived from parent cert.
        document.getElementById('productIdPartner').value = productId.partner || '';
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
        const productId = {
            Id: parseInt(document.getElementById('productIdId').value) || 0,
            CertificateId: parseInt(document.getElementById('certificateId').value) || this.certificateId,
            ProductIdType: document.getElementById('productIdType').value,
            ProductIdValue: document.getElementById('productIdValue').value.trim(),
            Partner: document.getElementById('productIdPartner').value || '',
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
        const patterns = {
            'ASIN': /^[A-Z0-9]{10}$/,
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

            // Fix scientific notation from Excel (e.g., 1.23E+12 → 123000000000)
            value = this.fixScientificNotation(value);

            if (!type && !value) continue;

            // Also fix scientific notation on SKU field
            const sku = this.fixScientificNotation(productId.productsku || productId.sku || '');

            // CR-015: Status from CSV is ignored — derived from parent cert.
            parsedProductIds.push({
                rowNumber: i,
                productIdType: type.toUpperCase(),
                productIdValue: value,
                partner: productId.partner || '',
                productName: productId.productname || '',
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

        for (const importItem of parsedProductIds) {
            // Match by type + value (unique identifier for product IDs)
            const existingItem = this.productIds.find(p =>
                p.productIdType?.toUpperCase() === importItem.productIdType?.toUpperCase() &&
                p.productIdValue?.toUpperCase() === importItem.productIdValue?.toUpperCase()
            );

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
                <td style="padding: 10px;">${row.data.partner || '-'}</td>
                <td style="padding: 10px;">${changesHtml}</td>
            `;

            tbody.appendChild(tr);
        });

        // Show modal
        modal.style.display = 'block';
        this.updateApplyButtonState();
    }

    toggleImportRow(index, checked) {
        if (this.importPreviewData[index]) {
            this.importPreviewData[index].selected = checked;
            this.updateApplyButtonState();
        }
    }

    toggleAllImportRows(checked) {
        this.importPreviewData.forEach((row, index) => {
            if (row.status !== 'error') {
                row.selected = checked;
                const checkbox = document.querySelector(`.import-row-checkbox[data-index="${index}"]`);
                if (checkbox) checkbox.checked = checked;
            }
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
        const selectedRows = this.importPreviewData.filter(r => r.selected && r.status !== 'error');
        if (selectedRows.length === 0) {
            this.showError('No rows selected for import');
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

        let successCount = 0;
        let errorCount = 0;
        const errorMessages = [];

        for (const row of selectedRows) {
            try {
                // CR-015: Status omitted — backend auto-derives from parent cert.
                const productIdData = {
                    CertificateId: this.certificateId,
                    ProductIdType: row.data.productIdType,
                    ProductIdValue: row.data.productIdValue,
                    Partner: row.data.partner || '',
                    ProductName: row.data.productName || '',
                    ProductSku: row.data.productSku || '',
                    Description: row.data.description || ''
                };

                let response;
                if (row.action === 'update' && row.existingData) {
                    productIdData.Id = row.existingData.id;
                    response = await fetch(`/api/productids/${row.existingData.id}`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(productIdData)
                    });
                } else {
                    response = await fetch('/api/productids', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(productIdData)
                    });
                }

                if (response.ok) {
                    successCount++;
                } else {
                    errorCount++;
                    let errorMsg = '';
                    try {
                        const errorData = await response.json();
                        errorMsg = errorData.message || errorData.Message || '';
                    } catch {
                        errorMsg = await response.text();
                    }
                    errorMessages.push(`Row ${row.rowNumber} (${row.data.productIdValue}): ${errorMsg || 'Unknown error'}`);
                    console.error(`Failed to import row ${row.rowNumber}:`, errorMsg);
                }
            } catch (error) {
                errorCount++;
                errorMessages.push(`Row ${row.rowNumber} (${row.data.productIdValue}): ${error.message || 'Network error'}`);
                console.error(`Error importing row ${row.rowNumber}:`, error);
            }
        }

        // Handle "Replace all existing" — deactivate IDs not in the import
        const overwriteToggle = document.getElementById('overwrite-existing-toggle');
        let deactivatedCount = 0;
        if (overwriteToggle && overwriteToggle.checked && this.certificateId && this.certificateId > 0) {
            const importKeys = new Set(
                this.importPreviewData
                    .filter(r => r.status !== 'error')
                    .map(r => `${(r.data.productIdType || '').toUpperCase()}:${(r.data.productIdValue || '').toUpperCase()}`)
            );

            for (const existing of this.productIds) {
                const key = `${(existing.productIdType || '').toUpperCase()}:${(existing.productIdValue || '').toUpperCase()}`;
                if (!importKeys.has(key)) {
                    try {
                        const resp = await fetch(`/api/productids/${existing.id}`, { method: 'DELETE' });
                        if (resp.ok) deactivatedCount++;
                    } catch (err) {
                        console.error(`Error deactivating product ID ${existing.id}:`, err);
                    }
                }
            }
        }

        // Close modal and refresh
        this.closeImportPreviewModal();

        if (successCount > 0 || deactivatedCount > 0) {
            await this.loadProductIds();
            let msg = `Imported ${successCount} product ID(s)`;
            if (deactivatedCount > 0) msg += `, removed ${deactivatedCount} existing`;
            if (errorCount > 0) msg += `. ${errorCount} failed: ${errorMessages.join('; ')}`;
            this.showSuccess(msg);
        } else {
            this.showError(`Import failed: ${errorMessages.join('; ')}`);
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
        const header = 'Type,Product ID,Partner,ProductName,ProductSku,Description';
        const rows = this.productIds.map(pid =>
            `"${pid.productIdType || ''}","${pid.productIdValue || ''}","${pid.partner || ''}","${pid.productName || ''}","${pid.productSku || ''}","${pid.description || ''}"`
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
        const csvContent = "Type,Product ID,Partner,ProductName,ProductSku,Description";

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

    formatDate(dateString) {
        return new Date(dateString).toLocaleDateString();
    }

    formatDateTime(dateString) {
        return new Date(dateString).toLocaleString();
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