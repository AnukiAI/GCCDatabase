// Product ID Manager JavaScript
// Handles all interactions for the Product ID Manager component

class ProductIdManager {
    constructor(containerId, certificateId, isReadOnly = false) {
        this.container = document.getElementById(containerId) || document.querySelector('.product-id-manager');
        this.certificateId = certificateId;
        this.isReadOnly = isReadOnly;
        this.productIds = [];
        this.filteredProductIds = [];

        this.init();
    }

    init() {
        if (!this.container) {
            console.error('Product ID Manager container not found');
            return;
        }

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
        document.getElementById('filterByStatus')?.addEventListener('change', () => this.applyFilters());
        document.getElementById('filterByPartner')?.addEventListener('change', () => this.applyFilters());
        document.getElementById('searchProductIds')?.addEventListener('input', () => this.applyFilters());

        // Modal form submission
        const form = document.getElementById('productIdForm');
        if (form) {
            form.addEventListener('submit', (e) => this.handleFormSubmit(e));
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
        try {
            this.showLoading(true);
            const response = await fetch(`/api/productids/certificate/${this.certificateId}`);

            if (response.ok) {
                this.productIds = await response.json();
                this.filteredProductIds = [...this.productIds];
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
            return;
        }

        tbody.innerHTML = this.filteredProductIds.map(productId => this.getProductIdRowHtml(productId)).join('');
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
                <td>
                    <span class="status-badge status-${productId.status.toLowerCase()}">
                        ${productId.status}
                    </span>
                </td>
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
        const colspan = this.isReadOnly ? 8 : 9;
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
        const statusFilter = document.getElementById('filterByStatus')?.value || '';
        const partnerFilter = document.getElementById('filterByPartner')?.value || '';
        const searchFilter = document.getElementById('searchProductIds')?.value.toLowerCase() || '';

        this.filteredProductIds = this.productIds.filter(productId => {
            const typeMatch = !typeFilter || productId.productIdType === typeFilter;
            const statusMatch = !statusFilter || productId.status === statusFilter;
            const partnerMatch = !partnerFilter || productId.partner === partnerFilter;
            const searchMatch = !searchFilter ||
                productId.productIdValue.toLowerCase().includes(searchFilter) ||
                (productId.productName && productId.productName.toLowerCase().includes(searchFilter)) ||
                (productId.productSku && productId.productSku.toLowerCase().includes(searchFilter));

            return typeMatch && statusMatch && partnerMatch && searchMatch;
        });

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
        const modal = new bootstrap.Modal(document.getElementById('productIdModal'));
        modal.show();
    }

    async showEditModal(productIdId) {
        try {
            const response = await fetch(`/api/productids/${productIdId}`);
            if (response.ok) {
                const productId = await response.json();
                this.populateForm(productId);
                document.getElementById('productIdModalLabel').textContent = 'Edit Product ID';
                const modal = new bootstrap.Modal(document.getElementById('productIdModal'));
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
            form.reset();
            document.getElementById('productIdId').value = '0';
            document.getElementById('certificateId').value = this.certificateId;
            document.getElementById('productIdStatus').value = 'Active';
            this.clearValidation();
        }
    }

    populateForm(productId) {
        document.getElementById('productIdId').value = productId.id;
        document.getElementById('certificateId').value = productId.certificateId;
        document.getElementById('productIdType').value = productId.productIdType;
        document.getElementById('productIdValue').value = productId.productIdValue;
        document.getElementById('productIdStatus').value = productId.status;
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

        const formData = new FormData(e.target);
        const productId = Object.fromEntries(formData.entries());

        // Convert string IDs to numbers
        productId.Id = parseInt(productId.Id);
        productId.CertificateId = parseInt(productId.CertificateId);

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

    showImportModal() {
        const modal = new bootstrap.Modal(document.getElementById('importModal'));
        modal.show();
    }

    async handleImport() {
        const fileInput = document.getElementById('importFile');
        const file = fileInput.files[0];

        if (!file) {
            this.showError('Please select a CSV file to import');
            return;
        }

        const formData = new FormData();
        formData.append('file', file);
        formData.append('certificateId', this.certificateId);
        formData.append('overwriteExisting', document.getElementById('overwriteExisting').checked);

        try {
            this.showImportProgress(true);
            const response = await fetch('/api/productids/import', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();
            this.showImportResults(result);

            if (result.successCount > 0) {
                await this.loadProductIds();
            }
        } catch (error) {
            console.error('Import error:', error);
            this.showError('Import failed: ' + error.message);
        } finally {
            this.showImportProgress(false);
        }
    }

    showImportProgress(show) {
        const resultsDiv = document.getElementById('importResults');
        const progressBar = document.getElementById('importProgress');

        if (show) {
            resultsDiv.style.display = 'block';
            progressBar.style.width = '0%';
            progressBar.setAttribute('aria-valuenow', '0');

            // Animate progress bar
            let progress = 0;
            const interval = setInterval(() => {
                progress += 10;
                progressBar.style.width = progress + '%';
                progressBar.setAttribute('aria-valuenow', progress);

                if (progress >= 90) {
                    clearInterval(interval);
                }
            }, 100);
        } else {
            progressBar.style.width = '100%';
            progressBar.setAttribute('aria-valuenow', '100');
        }
    }

    showImportResults(result) {
        const summaryDiv = document.getElementById('importSummary');
        const errorsDiv = document.getElementById('importErrors');

        summaryDiv.innerHTML = `
            <div class="row">
                <div class="col-4">
                    <strong>Processed:</strong> ${result.totalProcessed}
                </div>
                <div class="col-4">
                    <strong class="text-success">Success:</strong> ${result.successCount}
                </div>
                <div class="col-4">
                    <strong class="text-danger">Errors:</strong> ${result.errorCount}
                </div>
            </div>
        `;

        if (result.errors && result.errors.length > 0) {
            errorsDiv.innerHTML = '<h6>Errors:</h6>' +
                result.errors.map(error => `<div class="import-error">${error}</div>`).join('');
        } else {
            errorsDiv.innerHTML = '';
        }

        if (result.successCount > 0) {
            setTimeout(() => {
                const modal = bootstrap.Modal.getInstance(document.getElementById('importModal'));
                modal.hide();
                this.showSuccess(`Successfully imported ${result.successCount} product ID(s)`);
            }, 2000);
        }
    }

    async exportProductIds() {
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
                this.showError('Failed to export product IDs');
            }
        } catch (error) {
            console.error('Export error:', error);
            this.showError('Export failed');
        } finally {
            this.showLoading(false);
        }
    }

    downloadSample() {
        const csvContent = "Type,Value,Status,Partner,ProductName,ProductSku,Description\n" +
                          "ASIN,B08N5WRWNW,Active,Amazon,Sample Product,SKU123,Sample description\n" +
                          "UPC,123456789012,Active,None,Another Product,SKU456,Another description";

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
        this.showToast(message, 'error');
    }

    showSuccess(message) {
        this.showToast(message, 'success');
    }

    showToast(message, type = 'info') {
        // Create toast element
        const toastContainer = document.getElementById('toastContainer') || this.createToastContainer();

        const toastElement = document.createElement('div');
        toastElement.className = `toast align-items-center text-bg-${type === 'error' ? 'danger' : 'success'} border-0`;
        toastElement.setAttribute('role', 'alert');
        toastElement.setAttribute('aria-live', 'assertive');
        toastElement.setAttribute('aria-atomic', 'true');

        toastElement.innerHTML = `
            <div class="d-flex">
                <div class="toast-body" style="font-size: 16px;">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;

        toastContainer.appendChild(toastElement);

        const toast = new bootstrap.Toast(toastElement, { delay: 5000 });
        toast.show();

        // Remove element after toast is hidden
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }

    createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1055';
        document.body.appendChild(container);
        return container;
    }

    formatDate(dateString) {
        return new Date(dateString).toLocaleDateString();
    }

    formatDateTime(dateString) {
        return new Date(dateString).toLocaleString();
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    const productIdManager = document.querySelector('.product-id-manager');
    if (productIdManager) {
        const certificateId = productIdManager.getAttribute('data-certificate-id');
        const isReadOnly = productIdManager.hasAttribute('data-readonly');

        // Initialize even if certificateId is 0 (new certificate on Create page)
        if (certificateId !== null && certificateId !== undefined) {
            window.productIdManager = new ProductIdManager(null, parseInt(certificateId), isReadOnly);
        }
    }
});

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ProductIdManager;
}