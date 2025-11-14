/**
 * Certificate Fields Management System
 * Handles CRUD operations and UI interactions for certificate fields
 */

class CertificateFieldsManager {
    constructor() {
        this.fields = [];
        this.filteredFields = [];
        this.displayedFields = [];
        this.currentEditingId = null;
        this.itemsPerLoad = 15;
        this.currentDisplayCount = 15;
        this.isLoading = false;

        this.init();
    }

    async init() {
        this.bindEvents();
        await this.loadFields();
        this.renderFields();
        this.initInfiniteScroll();
    }

    bindEvents() {
        // DOM elements
        this.fieldsTableBody = document.getElementById('fields-table-body');
        this.fieldCountElement = document.getElementById('field-count');
        this.searchInput = document.getElementById('search-input');
        this.addFieldBtn = document.getElementById('add-field-btn');
        this.exportBtn = document.getElementById('export-btn');
        this.tableContainer = document.querySelector('.table-container');

        // Event listeners
        this.addFieldBtn?.addEventListener('click', () => this.startAddNew());
        this.searchInput?.addEventListener('input', this.debounce((e) => this.handleSearch(e), 300));
        this.exportBtn?.addEventListener('click', () => this.showExportModal());

        // Table event delegation
        this.fieldsTableBody?.addEventListener('click', (e) => this.handleTableClick(e));
        
        // Double-click to edit
        this.fieldsTableBody?.addEventListener('dblclick', (e) => {
            const row = e.target.closest('tr');
            if (row && row.dataset.id && !row.classList.contains('edit-row')) {
                const fieldId = parseInt(row.dataset.id);
                this.editField(fieldId);
            }
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.currentEditingId) {
                this.cancelInlineEdit(this.currentEditingId);
            }
            if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
                e.preventDefault();
                this.startAddNew();
            }
        });
    }

    async loadFields() {
        try {
            const response = await fetch('/Admin/GetCertificateFields');
            const result = await response.json();
            
            if (result.success) {
                this.fields = result.data || [];
                this.filteredFields = [...this.fields];
                console.log('Loaded fields:', this.fields);
            } else {
                console.error('Failed to load fields:', result.message);
                this.showToast('Failed to load certificate fields', 'error');
                this.fields = this.getSampleFields(); // Fallback to sample data
                this.filteredFields = [...this.fields];
            }
        } catch (error) {
            console.error('Error loading fields:', error);
            this.showToast('Error loading certificate fields', 'error');
            this.fields = this.getSampleFields(); // Fallback to sample data
            this.filteredFields = [...this.fields];
        }
    }

    getSampleFields() {
        return [
            // Core Certificate Information
            { id: 1, section: "Core Certificate Information", name: "Certificate #", fieldType: "text", isRequired: false, isVisible: true, notes: "Auto-generated if left blank", sortOrder: 1 },
            { id: 2, section: "Core Certificate Information", name: "Certification Type", fieldType: "dropdown", isRequired: true, isVisible: true, notes: "Type of certification issued", sortOrder: 2 },
            { id: 3, section: "Core Certificate Information", name: "CSF Attributes", fieldType: "multi-select", isRequired: false, isVisible: true, notes: "Certification specific framework attributes", sortOrder: 3 },
            { id: 4, section: "Core Certificate Information", name: "Status", fieldType: "dropdown", isRequired: true, isVisible: true, notes: "Required for public visibility", sortOrder: 4 },
            { id: 5, section: "Core Certificate Information", name: "Effective Date", fieldType: "date", isRequired: true, isVisible: true, notes: "When the certification becomes effective", sortOrder: 5 },
            { id: 6, section: "Core Certificate Information", name: "Expiration Date", fieldType: "date", isRequired: true, isVisible: true, notes: "When the certification expires", sortOrder: 6 },
            
            // Company Information
            { id: 7, section: "Company Information", name: "Company", fieldType: "text", isRequired: true, isVisible: true, notes: "Company receiving certification", sortOrder: 7 },
            { id: 8, section: "Company Information", name: "Company Contact Email", fieldType: "text", isRequired: false, isVisible: true, notes: "Company email contact", sortOrder: 8 },
            { id: 9, section: "Company Information", name: "Company Contact URL", fieldType: "text", isRequired: false, isVisible: true, notes: "Company website URL", sortOrder: 9 },
            
            // Product Information
            { id: 10, section: "Product Information", name: "Product Name", fieldType: "text", isRequired: true, isVisible: true, notes: "Name of the certified product", sortOrder: 10 },
            { id: 11, section: "Product Information", name: "Product Category", fieldType: "dropdown", isRequired: true, isVisible: true, notes: "Main category of the product", sortOrder: 11 },
            { id: 12, section: "Product Information", name: "Product Type", fieldType: "dropdown", isRequired: true, isVisible: true, notes: "Specific type within category", sortOrder: 12 },
            { id: 13, section: "Product Information", name: "Product Type Subcategory", fieldType: "dropdown", isRequired: true, isVisible: true, notes: "Subcategory within product type", sortOrder: 13 },
            { id: 14, section: "Product Information", name: "Location", fieldType: "text", isRequired: false, isVisible: true, notes: "Manufacturing or certification location", sortOrder: 14 },
            { id: 15, section: "Product Information", name: "Country", fieldType: "dropdown", isRequired: true, isVisible: true, notes: "Country of origin", sortOrder: 15 },
            
            // Product Identifiers
            { id: 16, section: "Product Identifiers", name: "ASIN", fieldType: "text", isRequired: false, isVisible: true, notes: "Amazon Standard Identification Number", sortOrder: 16 },
            { id: 17, section: "Product Identifiers", name: "UPC", fieldType: "text", isRequired: false, isVisible: true, notes: "Universal Product Code", sortOrder: 17 },
            { id: 18, section: "Product Identifiers", name: "GTIN", fieldType: "text", isRequired: false, isVisible: true, notes: "Global Trade Item Number", sortOrder: 18 },
            { id: 19, section: "Product Identifiers", name: "EAN", fieldType: "text", isRequired: false, isVisible: true, notes: "European Article Number", sortOrder: 19 },
            
            // Tagging & Classification
            { id: 20, section: "Tagging & Classification", name: "External Tag", fieldType: "multi-select", isRequired: false, isVisible: true, notes: "External classification tags", sortOrder: 20 },
            { id: 21, section: "Tagging & Classification", name: "Internal Tag", fieldType: "multi-select", isRequired: false, isVisible: true, notes: "Internal classification tags", sortOrder: 21 },
            
            // New Required Fields
            { id: 22, section: "New Required Fields", name: "Partner", fieldType: "multi-select", isRequired: true, isVisible: true, notes: "Amazon/Wayfair/None/Other partners", sortOrder: 22 },
            { id: 23, section: "New Required Fields", name: "Status Type", fieldType: "dropdown", isRequired: false, isVisible: true, notes: "To signify if client doesn't want to move forward", sortOrder: 23 },
            
            // File Management
            { id: 24, section: "File Management", name: "Certificate Upload", fieldType: "file", isRequired: false, isVisible: true, notes: "Upload certificate document", sortOrder: 24 },
            { id: 25, section: "File Management", name: "Preview", fieldType: "system-generated", isRequired: false, isVisible: true, notes: "Certificate preview/thumbnail", sortOrder: 25 },
            
            // Record Management
            { id: 26, section: "Record Management", name: "Record Created Date", fieldType: "date", isRequired: false, isVisible: true, notes: "When record was created", sortOrder: 26 },
            { id: 27, section: "Record Management", name: "Record Last Updated Date", fieldType: "date", isRequired: false, isVisible: true, notes: "When record was last modified", sortOrder: 27 },
            { id: 28, section: "Record Management", name: "Last Edited By", fieldType: "system-generated", isRequired: false, isVisible: true, notes: "User who last edited the record", sortOrder: 28 }
        ];
    }

    async createField(fieldData) {
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const response = await fetch('/Admin/CreateCertificateField', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(fieldData)
            });

            const result = await response.json();
            
            if (result.success) {
                this.showToast('Certificate field created successfully', 'success');
                await this.loadFields();
                this.renderFields();
                return true;
            } else {
                this.showToast(result.message || 'Failed to create certificate field', 'error');
                return false;
            }
        } catch (error) {
            console.error('Error creating field:', error);
            this.showToast('Error creating certificate field', 'error');
            return false;
        }
    }

    async updateField(id, fieldData) {
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const response = await fetch(`/Admin/UpdateCertificateField/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(fieldData)
            });

            const result = await response.json();
            
            if (result.success) {
                this.showToast('Certificate field updated successfully', 'success');
                await this.loadFields();
                this.renderFields();
                return true;
            } else {
                this.showToast(result.message || 'Failed to update certificate field', 'error');
                return false;
            }
        } catch (error) {
            console.error('Error updating field:', error);
            this.showToast('Error updating certificate field', 'error');
            return false;
        }
    }

    async deleteField(id) {
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const response = await fetch(`/Admin/DeleteCertificateField/${id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                }
            });

            const result = await response.json();
            
            if (result.success) {
                this.showToast('Certificate field deleted successfully', 'success');
                await this.loadFields();
                this.renderFields();
                return true;
            } else {
                this.showToast(result.message || 'Failed to delete certificate field', 'error');
                return false;
            }
        } catch (error) {
            console.error('Error deleting field:', error);
            this.showToast('Error deleting certificate field', 'error');
            return false;
        }
    }

    startAddNew() {
        if (this.currentEditingId !== null) {
            this.cancelInlineEdit(this.currentEditingId);
        }

        const newId = this.fields.length > 0 ? Math.max(...this.fields.map(f => f.id)) + 1 : 1;
        const newField = {
            id: newId,
            section: 'Core Certificate Information',
            name: '',
            fieldType: 'text',
            isRequired: false,
            isVisible: true,
            notes: '',
            sortOrder: this.getNextOrder()
        };

        const newRow = document.createElement('tr');
        newRow.setAttribute('data-id', newId);

        this.renderEditRow(newRow, newField);

        if (this.fieldsTableBody.firstChild) {
            this.fieldsTableBody.insertBefore(newRow, this.fieldsTableBody.firstChild);
        } else {
            this.fieldsTableBody.appendChild(newRow);
        }

        this.currentEditingId = newId;
        
        setTimeout(() => {
            const nameInput = newRow.querySelector('.edit-input');
            if (nameInput) nameInput.focus();
        }, 0);
    }

    getNextOrder() {
        return Math.max(...this.fields.map(f => f.sortOrder || 0), 0) + 1;
    }

    handleTableClick(e) {
        const target = e.target.closest('button');
        if (!target) return;

        const row = target.closest('tr');
        const fieldId = parseInt(row.dataset.id);

        if (target.classList.contains('edit-btn')) {
            this.editField(fieldId);
        } else if (target.classList.contains('delete-btn')) {
            this.confirmDeleteField(fieldId);
        } else if (target.classList.contains('save-btn')) {
            this.saveInlineEdit(fieldId);
        } else if (target.classList.contains('cancel-btn')) {
            this.cancelInlineEdit(fieldId);
        }
    }

    editField(id) {
        if (this.currentEditingId && this.currentEditingId !== id) {
            this.cancelInlineEdit(this.currentEditingId);
        }

        const field = this.fields.find(f => f.id === id);
        if (!field) return;

        this.currentEditingId = id;
        const row = document.querySelector(`tr[data-id="${id}"]`);
        this.renderEditRow(row, field);
    }

    async confirmDeleteField(id) {
        const field = this.fields.find(f => f.id === id);
        if (!field) return;

        const confirmed = await AdminCore.showConfirmation({
            title: 'Delete Confirmation',
            message: `Are you sure you want to delete the field "${field.name}"? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger'
        });

        if (confirmed) {
            await this.deleteField(id);
        }
    }

    async saveInlineEdit(id) {
        const row = document.querySelector(`tr[data-id="${id}"]`);
        const inputs = row.querySelectorAll('.edit-input, .edit-select');

        const updatedData = {
            name: inputs[1].value.trim(),
            fieldType: inputs[2].value,
            isRequired: inputs[3].value === 'true',
            isVisible: true,
            notes: inputs[4].value.trim(),
            sortOrder: this.getNextOrder()
        };

        if (!updatedData.name || !updatedData.fieldType) {
            this.showToast('Please fill in all required fields', 'error');
            return;
        }

        // Check for duplicate names (excluding current field)
        if (this.fields.some(field => field.name.toLowerCase() === updatedData.name.toLowerCase() && field.id !== id)) {
            this.showToast('A field with this name already exists', 'error');
            return;
        }

        const existingField = this.fields.find(f => f.id === id);
        let success = false;

        if (existingField) {
            // Update existing field
            success = await this.updateField(id, updatedData);
        } else {
            // Create new field
            success = await this.createField(updatedData);
        }

        if (success) {
            this.currentEditingId = null;
        }
    }

    cancelInlineEdit(id) {
        const existingField = this.fields.find(f => f.id === id);
        
        if (!existingField) {
            // This is a new field that was being added - just remove the row
            const row = document.querySelector(`tr[data-id="${id}"]`);
            if (row) {
                row.remove();
            }
        } else {
            // This is an existing field being edited - reload the table
            this.renderFields();
        }
        
        this.currentEditingId = null;
    }

    renderFields() {
        if (!this.fieldsTableBody) return;

        this.fieldsTableBody.innerHTML = '';

        // Show all fields at once (no lazy loading for admin pages with small datasets)
        this.displayedFields = this.filteredFields;

        this.displayedFields.forEach(field => {
            const row = document.createElement('tr');
            row.setAttribute('data-id', field.id);

            if (this.currentEditingId === field.id) {
                this.renderEditRow(row, field);
            } else {
                this.renderNormalRow(row, field);
            }

            this.fieldsTableBody.appendChild(row);
        });

        this.updateFieldCount();
    }

    renderNormalRow(row, field) {
        const requiredLabel = field.isRequired ? 'Required' : 'Optional';
        
        row.innerHTML = `
            <td style="font-size: 16px; color: #666;">${field.section || 'Core Certificate Information'}</td>
            <td>${field.name}</td>
            <td><span class="field-type type-${field.fieldType.toLowerCase().replace(/[-\s]/g, '')}">${this.formatFieldType(field.fieldType)}</span></td>
            <td><span class="required-label required-${requiredLabel.toLowerCase().replace('-', '')}">${requiredLabel}</span></td>
            <td>${field.notes || ''}</td>
            <td>
                <div class="action-buttons">
                    <button class="action-btn edit-btn" title="Edit Field" aria-label="Edit field">
                        <i class="fas fa-edit" aria-hidden="true"></i>
                    </button>
                    <button class="action-btn delete-btn" title="Delete Field" aria-label="Delete field">
                        <i class="fas fa-trash-alt" aria-hidden="true"></i>
                    </button>
                </div>
            </td>
        `;
    }

    renderEditRow(row, field) {
        row.classList.add('edit-row');

        const sections = [
            'Core Certificate Information',
            'Company Information',
            'Product Information',
            'Product Identifiers',
            'Tagging & Classification',
            'New Required Fields',
            'File Management',
            'Record Management'
        ];
        const controlTypes = ['text', 'dropdown', 'multi-select', 'date', 'file', 'number', 'checkbox', 'system-generated'];

        row.innerHTML = `
            <td>
                <select class="edit-select" required>
                    ${sections.map(section =>
                        `<option value="${section}" ${section === (field.section || 'Core Certificate Information') ? 'selected' : ''}>${section}</option>`
                    ).join('')}
                </select>
            </td>
            <td>
                <input type="text" class="edit-input" value="${field.name || ''}" required>
            </td>
            <td>
                <select class="edit-select" required>
                    ${controlTypes.map(type =>
                        `<option value="${type}" ${type === field.fieldType ? 'selected' : ''}>${this.formatFieldType(type)}</option>`
                    ).join('')}
                </select>
            </td>
            <td>
                <select class="edit-select" required>
                    <option value="true" ${field.isRequired ? 'selected' : ''}>Required</option>
                    <option value="false" ${!field.isRequired ? 'selected' : ''}>Optional</option>
                </select>
            </td>
            <td>
                <input type="text" class="edit-input" value="${field.notes || ''}" placeholder="Optional notes">
            </td>
            <td>
                <div class="action-buttons edit-actions">
                    <button class="action-btn save-btn" title="Save Changes" aria-label="Save changes to custom field">
                        <i class="fas fa-check" aria-hidden="true"></i>
                    </button>
                    <button class="action-btn cancel-btn" title="Cancel Edit" aria-label="Cancel editing custom field">
                        <i class="fas fa-times" aria-hidden="true"></i>
                    </button>
                </div>
            </td>
        `;
    }

    formatFieldType(type) {
        const typeMap = {
            'text': 'Text',
            'dropdown': 'Dropdown',
            'multi-select': 'Multi-Select',
            'date': 'Date',
            'file': 'File',
            'number': 'Number',
            'checkbox': 'Checkbox',
            'system-generated': 'System Generated'
        };
        return typeMap[type] || type;
    }

    handleSearch(e) {
        const searchTerm = e.target.value.toLowerCase().trim();

        if (searchTerm === '') {
            this.filteredFields = [...this.fields];
        } else {
            this.filteredFields = this.fields.filter(field =>
                (field.section && field.section.toLowerCase().includes(searchTerm)) ||
                field.name.toLowerCase().includes(searchTerm) ||
                field.fieldType.toLowerCase().includes(searchTerm) ||
                (field.isRequired ? 'required' : 'optional').includes(searchTerm) ||
                (field.notes && field.notes.toLowerCase().includes(searchTerm))
            );
        }

        // Show all filtered results (no lazy loading)
        this.renderFields();
    }

    initInfiniteScroll() {
        if (!this.tableContainer) return;

        this.tableContainer.addEventListener('scroll', () => {
            if (this.isLoading) return;

            const scrollTop = this.tableContainer.scrollTop;
            const scrollHeight = this.tableContainer.scrollHeight;
            const clientHeight = this.tableContainer.clientHeight;

            // Load more when 200px from bottom
            if (scrollTop + clientHeight >= scrollHeight - 200) {
                this.loadMoreFields();
            }
        });
    }

    loadMoreFields() {
        if (this.displayedFields.length >= this.filteredFields.length) {
            return; // All fields already displayed
        }

        this.isLoading = true;
        this.currentDisplayCount += this.itemsPerLoad;
        this.renderFields();
        this.isLoading = false;
    }

    updateFieldCount() {
        if (!this.fieldCountElement) return;

        const totalFields = this.filteredFields.length;
        const displayedCount = this.displayedFields.length;

        if (this.filteredFields.length !== this.fields.length) {
            this.fieldCountElement.textContent = `Showing ${displayedCount} of ${totalFields} filtered fields (${this.fields.length} total)`;
        } else {
            this.fieldCountElement.textContent = `Showing ${displayedCount} of ${totalFields} fields`;
        }
    }

    // Removed updateShowMoreButton - using infinite scroll instead

    // Export to CSV only
    showExportModal() {
        if (window.ExportUtils) {
            const columns = [
                { key: 'id', header: 'ID' },
                { key: 'name', header: 'Name' },
                { key: 'fieldType', header: 'Field Type' },
                { key: 'isRequired', header: 'Required' },
                { key: 'isVisible', header: 'Visible' },
                { key: 'sortOrder', header: 'Sort Order' },
                { key: 'notes', header: 'Notes' }
            ];

            // Direct CSV export only
            window.ExportUtils.exportToCSV(this.filteredFields, columns, 'certificate-fields', (message, type) => this.showToast(message, type));
        } else {
            this.showToast('Export functionality not available', 'error');
        }
    }

    showToast(message, type = 'success') {
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.innerHTML = `
            <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
            <span>${message}</span>
        `;

        const container = document.getElementById('toast-container');
        if (container) {
            container.appendChild(toast);

            setTimeout(() => toast.classList.add('show'), 100);

            setTimeout(() => {
                toast.classList.remove('show');
                setTimeout(() => toast.remove(), 300);
            }, 3000);
        }
    }

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.certificateFieldsManager = new CertificateFieldsManager();
});