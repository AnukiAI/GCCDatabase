// Company Management JavaScript - Full CRUD functionality with backend API integration
class CompanyManagement {
    constructor() {
        this.companies = [];
        this.filteredCompanies = [];
        this.currentSort = { field: 'name', direction: 'asc' };
        this.currentEditingId = null;

        // Import preview state
        this.importPreviewData = [];
        this.importFileName = '';

        this.init();
    }
    
    async init() {
        console.log('Initializing Company Management...');
        this.bindEvents();
        await this.loadCompanies();
        this.renderCompanies();
        this.updateCompanyCount();
    }
    
    bindEvents() {
        // Button events
        document.getElementById('add-company-btn')?.addEventListener('click', () => this.showAddCompanyModal());
        document.getElementById('export-btn')?.addEventListener('click', () => this.exportCompanies());
        document.getElementById('import-btn')?.addEventListener('click', () => this.showImportModal());
        
        // Search event
        document.getElementById('search-input')?.addEventListener('input', (e) => this.handleSearch(e.target.value));
        
        // Modal events
        document.querySelector('.modal-close')?.addEventListener('click', () => this.hideModal());
        document.querySelector('.modal')?.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal')) this.hideModal();
        });
        document.getElementById('company-form')?.addEventListener('submit', (e) => this.handleFormSubmit(e));
        document.getElementById('cancel-btn')?.addEventListener('click', () => this.hideModal());
        
        // Table sorting events
        document.querySelectorAll('[data-sort]').forEach(header => {
            header.addEventListener('click', () => this.handleSort(header.dataset.sort));
        });
        
        // Handle escape key to close modal
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') this.hideModal();
        });
    }
    
    async loadCompanies() {
        try {
            this.showLoading(true);
            const response = await fetch('/Company/GetCompanies', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            const result = await response.json();
            
            if (result.success && result.data) {
                this.companies = result.data;
                this.filteredCompanies = [...this.companies];
                console.log(`Loaded ${this.companies.length} companies from backend`);
            } else {
                console.error('Failed to load companies:', result.message);
                this.showNotification('Failed to load companies: ' + (result.message || 'Unknown error'), 'error');
                // Fallback to empty array
                this.companies = [];
                this.filteredCompanies = [];
            }
        } catch (error) {
            console.error('Error loading companies:', error);
            this.showNotification('Error connecting to server. Please check your connection.', 'error');
            // Fallback to empty array  
            this.companies = [];
            this.filteredCompanies = [];
        } finally {
            this.showLoading(false);
        }
    }
    
    renderCompanies() {
        const tbody = document.querySelector('#company-table tbody');
        if (!tbody) return;
        
        if (this.filteredCompanies.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="4" class="no-data">
                        <div class="no-data-message">
                            <i class="fas fa-building"></i>
                            <p>No companies found</p>
                            <small>Try adjusting your search or add a new company</small>
                        </div>
                    </td>
                </tr>
            `;
            return;
        }
        
        tbody.innerHTML = this.filteredCompanies.map(company => this.createCompanyRow(company)).join('');
        
        // Bind action button events
        this.bindActionEvents();
    }
    
    createCompanyRow(company) {
        const websiteDisplay = company.url ? `<a href="${company.url}" target="_blank" title="Visit Website">${company.url}</a>` : '';
        
        return `
            <tr data-company-id="${company.id}">
                <td class="company-name">${this.escapeHtml(company.name || '')}</td>
                <td class="company-email">
                    <a href="mailto:${company.email}" title="Send Email">${this.escapeHtml(company.email || '')}</a>
                </td>
                <td class="company-url">${websiteDisplay}</td>
                <td class="actions-col">
                    <div class="action-buttons">
                        <button class="action-btn edit-btn" onclick="companyManagement.editCompany(${company.id})" title="Edit Company">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="action-btn delete-btn" onclick="companyManagement.deleteCompany(${company.id})" title="Delete Company">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }
    
    bindActionEvents() {
        // Event delegation is handled by onclick attributes in createCompanyRow
        // This method can be used for additional event binding if needed
    }
    
    handleSearch(searchTerm) {
        const term = searchTerm.toLowerCase().trim();
        this.applyFilters(term);
    }
    
    applyFilters(searchTerm = null) {
        const search = searchTerm !== null ? searchTerm : (document.getElementById('search-input')?.value || '').toLowerCase().trim();
        
        this.filteredCompanies = this.companies.filter(company => {
            // Search filter
            if (search) {
                const searchableText = [company.name, company.email, company.url].join(' ').toLowerCase();
                if (!searchableText.includes(search)) return false;
            }
            
            return true;
        });
        
        this.renderCompanies();
        this.updateCompanyCount();
    }
    
    handleSort(field) {
        if (this.currentSort.field === field) {
            this.currentSort.direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
        } else {
            this.currentSort.field = field;
            this.currentSort.direction = 'asc';
        }
        
        this.filteredCompanies.sort((a, b) => {
            let aVal = a[field] || '';
            let bVal = b[field] || '';
            
            // String comparison
            if (typeof aVal === 'string') {
                aVal = aVal.toLowerCase();
                bVal = bVal.toLowerCase();
            }
            
            let comparison = 0;
            if (aVal > bVal) comparison = 1;
            if (aVal < bVal) comparison = -1;
            
            return this.currentSort.direction === 'desc' ? -comparison : comparison;
        });
        
        this.updateSortIcons(field);
        this.renderCompanies();
    }
    
    updateSortIcons(activeField) {
        document.querySelectorAll('[data-sort] .sort-icon').forEach(icon => {
            const field = icon.closest('[data-sort]').dataset.sort;
            if (field === activeField) {
                icon.textContent = this.currentSort.direction === 'asc' ? '▲' : '▼';
                icon.closest('th').classList.add('sorted');
            } else {
                icon.textContent = '▼';
                icon.closest('th').classList.remove('sorted');
            }
        });
    }
    
    updateCompanyCount() {
        const countElement = document.getElementById('company-count');
        if (countElement) {
            countElement.textContent = `Showing ${this.filteredCompanies.length} of ${this.companies.length} companies`;
        }
    }
    
    showAddCompanyModal() {
        document.getElementById('modal-title').textContent = 'Add New Company';
        document.getElementById('company-form').reset();
        document.getElementById('company-id').value = '';
        this.currentEditingId = null;
        this.showModal();
    }
    
    async editCompany(companyId) {
        const company = this.companies.find(c => c.id === companyId);
        if (!company) {
            this.showNotification('Company not found', 'error');
            return;
        }
        
        document.getElementById('modal-title').textContent = 'Edit Company';
        document.getElementById('company-id').value = company.id;
        document.getElementById('company-name').value = company.name || '';
        document.getElementById('company-email').value = company.email || '';
        document.getElementById('company-url').value = company.url || '';
        
        this.currentEditingId = companyId;
        this.showModal();
    }
    
    async deleteCompany(companyId) {
        const company = this.companies.find(c => c.id === companyId);
        if (!company) {
            this.showNotification('Company not found', 'error');
            return;
        }

        const confirmed = await AdminCore.showConfirmation({
            title: 'Delete Confirmation',
            message: `Are you sure you want to delete company <strong>${company.name}</strong>? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger',
            allowHtml: true
        });

        if (!confirmed) {
            return;
        }
        
        try {
            this.showLoading(true, 'Deleting company...');
            
            const response = await fetch(`/Company/DeleteCompany/${companyId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showNotification(result.message || 'Company deleted successfully', 'success');
                await this.loadCompanies(); // Reload from server
                this.renderCompanies();
                this.updateCompanyCount();
            } else {
                this.showNotification('Failed to delete company: ' + (result.message || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error deleting company:', error);
            this.showNotification('Error deleting company. Please try again.', 'error');
        } finally {
            this.showLoading(false);
        }
    }
    
    async handleFormSubmit(e) {
        e.preventDefault();
        
        const formData = {
            id: this.currentEditingId || 0,
            name: document.getElementById('company-name').value.trim(),
            email: document.getElementById('company-email').value.trim(),
            url: document.getElementById('company-url').value.trim()
        };
        
        // Basic validation
        if (!formData.name || !formData.email) {
            this.showNotification('Name and email are required', 'error');
            return;
        }
        
        // Email validation
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(formData.email)) {
            this.showNotification('Please enter a valid email address', 'error');
            return;
        }
        
        // URL validation (if provided)
        if (formData.url && !this.isValidUrl(formData.url)) {
            this.showNotification('Please enter a valid URL', 'error');
            return;
        }
        
        try {
            this.showLoading(true, this.currentEditingId ? 'Updating company...' : 'Creating company...');
            
            const response = await fetch('/Company/SaveCompany', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(formData)
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showNotification(result.message || (this.currentEditingId ? 'Company updated successfully' : 'Company created successfully'), 'success');
                this.hideModal();
                await this.loadCompanies(); // Reload from server
                this.renderCompanies();
                this.updateCompanyCount();
            } else {
                this.showNotification('Failed to save company: ' + (result.message || 'Unknown error'), 'error');
                if (result.errors && result.errors.length > 0) {
                    console.error('Validation errors:', result.errors);
                }
            }
        } catch (error) {
            console.error('Error saving company:', error);
            this.showNotification('Error saving company. Please try again.', 'error');
        } finally {
            this.showLoading(false);
        }
    }
    
    showModal() {
        const modal = document.getElementById('company-modal');
        if (modal) {
            modal.style.display = 'block';
            setTimeout(() => modal.classList.add('show'), 10);
            
            // Focus first input
            const firstInput = modal.querySelector('input[type="text"], input[type="email"]');
            if (firstInput) firstInput.focus();
        }
    }
    
    hideModal() {
        const modal = document.getElementById('company-modal');
        if (modal) {
            modal.classList.remove('show');
            setTimeout(() => modal.style.display = 'none', 300);
        }
        this.currentEditingId = null;
    }
    
    async exportCompanies() {
        if (this.filteredCompanies.length === 0) {
            this.showNotification('No companies to export', 'warning');
            return;
        }
        
        try {
            // Prepare export data
            const exportData = this.filteredCompanies.map(company => ({
                ID: company.id,
                Name: company.name || '',
                Email: company.email || '',
                Website: company.url || ''
            }));
            
            // Create CSV content
            const headers = Object.keys(exportData[0]);
            const csvContent = [
                headers.join(','),
                ...exportData.map(row => 
                    headers.map(header => {
                        const value = row[header] || '';
                        // Escape quotes and wrap in quotes if contains comma
                        return value.includes(',') || value.includes('"') 
                            ? `"${value.replace(/"/g, '""')}"` 
                            : value;
                    }).join(',')
                )
            ].join('\n');
            
            // Download file
            const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
            const link = document.createElement('a');
            const url = URL.createObjectURL(blob);
            link.setAttribute('href', url);
            link.setAttribute('download', `companies_export_${new Date().toISOString().split('T')[0]}.csv`);
            link.style.visibility = 'hidden';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            
            this.showNotification(`Exported ${exportData.length} companies successfully`, 'success');
        } catch (error) {
            console.error('Error exporting companies:', error);
            this.showNotification('Error exporting companies. Please try again.', 'error');
        }
    }
    
    showImportModal() {
        // Create file input and trigger file selection
        const fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.accept = '.csv';
        fileInput.style.display = 'none';
        document.body.appendChild(fileInput);

        fileInput.addEventListener('change', async (e) => {
            const file = e.target.files[0];
            if (!file) {
                document.body.removeChild(fileInput);
                return;
            }

            this.importFileName = file.name;
            const reader = new FileReader();
            reader.onload = async (event) => {
                const contents = event.target.result;
                await this.parseAndPreviewImport(contents);
                document.body.removeChild(fileInput);
            };
            reader.onerror = () => {
                this.showNotification('Error reading file', 'error');
                document.body.removeChild(fileInput);
            };
            reader.readAsText(file);
        });

        fileInput.click();
    }

    async parseAndPreviewImport(contents) {
        try {
            const lines = contents.split(/\r?\n/).filter(line => line.trim());
            if (lines.length < 2) {
                this.showNotification('CSV file must have a header row and at least one data row', 'error');
                return;
            }

            // Parse and normalize headers
            const rawHeaders = this.parseCSVLine(lines[0]).map(h => h.trim());
            const headers = rawHeaders.map(h => this.normalizeCompanyHeaderName(h));

            const requiredFields = ['name'];
            const missingFields = requiredFields.filter(f => !headers.includes(f));

            if (missingFields.length > 0) {
                this.showNotification(`Missing required fields: ${missingFields.join(', ')}`, 'error');
                return;
            }

            // Parse data rows
            const parsedCompanies = [];
            for (let i = 1; i < lines.length; i++) {
                const values = this.parseCSVLine(lines[i]);
                if (values.length === 0 || values.every(v => !v.trim())) continue;

                const company = {};
                headers.forEach((header, index) => {
                    company[header] = values[index]?.trim() || '';
                });

                parsedCompanies.push({
                    rowNumber: i,
                    data: {
                        name: company.name || '',
                        email: company.email || '',
                        url: company.url || ''
                    }
                });
            }

            if (parsedCompanies.length === 0) {
                this.showNotification('No valid data rows found in CSV', 'error');
                return;
            }

            // Build preview with comparison
            await this.buildImportPreview(parsedCompanies);

        } catch (error) {
            console.error('Error parsing CSV:', error);
            this.showNotification('Error parsing CSV file', 'error');
        }
    }

    parseCSVLine(line) {
        const result = [];
        let current = '';
        let inQuotes = false;

        for (let i = 0; i < line.length; i++) {
            const char = line[i];
            const nextChar = line[i + 1];

            if (inQuotes) {
                if (char === '"' && nextChar === '"') {
                    current += '"';
                    i++;
                } else if (char === '"') {
                    inQuotes = false;
                } else {
                    current += char;
                }
            } else {
                if (char === '"') {
                    inQuotes = true;
                } else if (char === ',') {
                    result.push(current);
                    current = '';
                } else {
                    current += char;
                }
            }
        }
        result.push(current);
        return result;
    }

    /**
     * Normalize CSV header names for company imports.
     * Handles common variations case-insensitively.
     */
    normalizeCompanyHeaderName(header) {
        const normalized = header.toLowerCase().replace(/[\s_-]/g, '');

        const headerMappings = {
            // Name variations
            'name': 'name',
            'companyname': 'name',
            'company': 'name',
            'organization': 'name',

            // Email variations
            'email': 'email',
            'emailaddress': 'email',
            'companyemail': 'email',
            'contactemail': 'email',

            // URL variations
            'url': 'url',
            'website': 'url',
            'websiteurl': 'url',
            'companyurl': 'url',
            'web': 'url',
            'homepage': 'url'
        };

        return headerMappings[normalized] || normalized;
    }

    async buildImportPreview(parsedCompanies) {
        this.importPreviewData = [];

        parsedCompanies.forEach(row => {
            // Match by name (case-insensitive)
            const existingCompany = this.companies.find(c =>
                c.name && row.data.name &&
                c.name.trim().toLowerCase() === row.data.name.trim().toLowerCase()
            );

            const previewRow = {
                rowNumber: row.rowNumber,
                data: row.data,
                existingData: null,
                status: 'new',
                changes: [],
                errors: [],
                action: 'create',
                selected: true
            };

            if (existingCompany) {
                previewRow.existingData = {
                    id: existingCompany.id,
                    name: existingCompany.name,
                    email: existingCompany.email || '',
                    url: existingCompany.url || ''
                };

                // Track which fields changed
                const changes = [];

                const existingEmail = (existingCompany.email || '').trim().toLowerCase();
                const importEmail = (row.data.email || '').trim().toLowerCase();
                if (importEmail && existingEmail !== importEmail) {
                    changes.push({ field: 'email', oldValue: existingCompany.email || '', newValue: row.data.email });
                }

                const existingUrl = (existingCompany.url || '').trim().toLowerCase();
                const importUrl = (row.data.url || '').trim().toLowerCase();
                if (importUrl && existingUrl !== importUrl) {
                    changes.push({ field: 'url', oldValue: existingCompany.url || '', newValue: row.data.url });
                }

                previewRow.changes = changes;

                if (changes.length > 0) {
                    previewRow.status = 'modified';
                    previewRow.action = 'update';
                    previewRow.selected = true;
                } else {
                    previewRow.status = 'unchanged';
                    previewRow.action = 'skip';
                    previewRow.selected = false;
                }
            }

            // Validate
            if (!row.data.name || !row.data.name.trim()) {
                previewRow.errors.push('Company name is required');
                previewRow.status = 'error';
                previewRow.selected = false;
            }
            if (row.data.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(row.data.email)) {
                previewRow.errors.push('Invalid email format');
                previewRow.status = 'error';
                previewRow.selected = false;
            }

            this.importPreviewData.push(previewRow);
        });

        this.showImportPreviewModal();
    }

    showImportPreviewModal() {
        const modal = document.getElementById('import-preview-modal');
        if (!modal) return;

        // Calculate stats
        const stats = {
            new: this.importPreviewData.filter(r => r.status === 'new').length,
            modified: this.importPreviewData.filter(r => r.status === 'modified').length,
            unchanged: this.importPreviewData.filter(r => r.status === 'unchanged').length,
            errors: this.importPreviewData.filter(r => r.status === 'error').length
        };

        document.getElementById('stat-new').textContent = stats.new;
        document.getElementById('stat-modified').textContent = stats.modified;
        document.getElementById('stat-unchanged').textContent = stats.unchanged;
        document.getElementById('stat-errors').textContent = stats.errors;

        const filenameEl = document.getElementById('import-filename');
        if (filenameEl) filenameEl.textContent = this.importFileName;

        const tbody = document.getElementById('import-preview-body');
        tbody.innerHTML = '';

        this.importPreviewData.forEach((row, index) => {
            const tr = document.createElement('tr');
            tr.dataset.index = index;
            tr.style.backgroundColor = row.status === 'error' ? '#fff5f5' : (row.status === 'unchanged' ? '#fafafa' : '');

            const statusColors = {
                'new': { bg: '#e8f5e1', color: '#447638', label: 'New' },
                'modified': { bg: '#e6f7ff', color: '#002B76', label: 'Modified' },
                'unchanged': { bg: '#f5f5f5', color: '#999', label: 'Unchanged' },
                'error': { bg: '#ffebee', color: '#ff5252', label: 'Error' }
            };
            const statusStyle = statusColors[row.status];

            let changesSummary = '';
            if (row.status === 'modified' && row.changes.length > 0) {
                const fieldNames = row.changes.map(c => c.field === 'url' ? 'Website' : c.field.charAt(0).toUpperCase() + c.field.slice(1));
                changesSummary = `<div style="font-size: 10px; color: #666; margin-top: 4px;">${fieldNames.join(', ')} changed</div>`;
            }

            let errorDisplay = '';
            if (row.errors.length > 0) {
                errorDisplay = `<div style="color: #ff5252; font-size: 11px; margin-top: 4px; font-weight: 500;">${row.errors.join('<br>')}</div>`;
            }

            tr.innerHTML = `
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    <input type="checkbox" class="import-row-checkbox" data-index="${index}"
                           ${row.selected ? 'checked' : ''} ${row.status === 'error' || row.status === 'unchanged' ? 'disabled' : ''}
                           onchange="companyManagement.updateImportRowSelection(${index}, this.checked)">
                </td>
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; font-family: monospace; vertical-align: top;">${row.rowNumber}</td>
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    <span style="display: inline-block; padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: 500; background: ${statusStyle.bg}; color: ${statusStyle.color};">
                        ${statusStyle.label}
                    </span>
                    ${changesSummary}
                </td>
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    ${this.renderFieldWithDiff(row, 'name')}
                </td>
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    ${this.renderFieldWithDiff(row, 'email')}
                    ${errorDisplay}
                </td>
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    ${this.renderFieldWithDiff(row, 'url')}
                </td>
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    ${this.renderActionDropdown(row, index)}
                </td>
            `;

            tbody.appendChild(tr);
        });

        this.updateSelectedCount();
        modal.style.setProperty('display', 'block', 'important');
    }

    renderFieldWithDiff(row, field) {
        const newValue = row.data[field] || '';

        if (row.status === 'modified' && row.existingData && row.changes) {
            const oldValue = row.existingData[field] || '';
            const fieldChanged = row.changes.some(c => c.field.toLowerCase() === field.toLowerCase());

            if (fieldChanged) {
                return `
                    <div style="min-width: 120px;">
                        <div style="display: flex; flex-direction: column; gap: 4px;">
                            <div>
                                <div style="font-size: 10px; color: #856404; font-weight: 600; text-transform: uppercase;">Current:</div>
                                <div style="padding: 4px 8px; background: #fff3cd; border: 1px solid #ffc107; border-radius: 4px; font-size: 12px; word-break: break-word;">${this.escapeHtml(oldValue || '(empty)')}</div>
                            </div>
                            <div>
                                <div style="font-size: 10px; color: #155724; font-weight: 600; text-transform: uppercase;">Import:</div>
                                <div style="padding: 4px 8px; background: #d4edda; border: 1px solid #28a745; border-radius: 4px; font-size: 12px; word-break: break-word;">${this.escapeHtml(newValue || '(empty)')}</div>
                            </div>
                        </div>
                    </div>
                `;
            }
        }

        return `<span style="word-break: break-word;">${this.escapeHtml(newValue)}</span>`;
    }

    renderActionDropdown(row, index) {
        if (row.status === 'error') {
            return '<span style="color: #999; font-size: 12px;">Cannot import</span>';
        }

        if (row.status === 'new') {
            return `
                <select class="import-action-select" data-index="${index}" onchange="companyManagement.updateImportRowAction(${index}, this.value)" style="padding: 5px 8px; border: 1px solid #ddd; border-radius: 4px; font-size: 12px;">
                    <option value="create" ${row.action === 'create' ? 'selected' : ''}>Create</option>
                    <option value="skip" ${row.action === 'skip' ? 'selected' : ''}>Skip</option>
                </select>
            `;
        }

        if (row.status === 'modified') {
            return `
                <select class="import-action-select" data-index="${index}" onchange="companyManagement.updateImportRowAction(${index}, this.value)" style="padding: 5px 8px; border: 2px solid #002B76; border-radius: 4px; font-size: 12px; font-weight: 500;">
                    <option value="update" ${row.action === 'update' ? 'selected' : ''}>Use Import</option>
                    <option value="keep" ${row.action === 'keep' ? 'selected' : ''}>Keep Current</option>
                    <option value="skip" ${row.action === 'skip' ? 'selected' : ''}>Skip Row</option>
                </select>
            `;
        }

        if (row.status === 'unchanged') {
            return '<span style="color: #999; font-size: 12px;">No changes</span>';
        }

        return '';
    }

    updateImportRowSelection(index, selected) {
        this.importPreviewData[index].selected = selected;
        if (!selected) {
            this.importPreviewData[index].action = 'skip';
        } else {
            if (this.importPreviewData[index].status === 'new') {
                this.importPreviewData[index].action = 'create';
            } else if (this.importPreviewData[index].status === 'modified') {
                this.importPreviewData[index].action = 'update';
            }
        }
        this.updateSelectedCount();
    }

    updateImportRowAction(index, action) {
        this.importPreviewData[index].action = action;
        this.importPreviewData[index].selected = (action !== 'skip' && action !== 'keep');

        const checkbox = document.querySelector(`input.import-row-checkbox[data-index="${index}"]`);
        if (checkbox) {
            checkbox.checked = (action !== 'skip' && action !== 'keep');
        }

        const tr = document.querySelector(`tr[data-index="${index}"]`);
        if (tr) {
            if (action === 'skip') {
                tr.style.backgroundColor = '#f5f5f5';
                tr.style.opacity = '0.7';
            } else if (action === 'keep') {
                tr.style.backgroundColor = '#fafafa';
                tr.style.opacity = '0.9';
            } else {
                tr.style.backgroundColor = '';
                tr.style.opacity = '1';
            }
        }

        this.updateSelectedCount();
    }

    toggleAllImportRows(checked) {
        this.importPreviewData.forEach((row, index) => {
            if (row.status !== 'error' && row.status !== 'unchanged') {
                row.selected = checked;
                row.action = checked ? (row.status === 'new' ? 'create' : 'update') : 'skip';

                const checkbox = document.querySelector(`input.import-row-checkbox[data-index="${index}"]`);
                if (checkbox) checkbox.checked = checked;
            }
        });
        this.updateSelectedCount();
    }

    updateSelectedCount() {
        const toProcessCount = this.importPreviewData.filter(r =>
            r.action === 'create' || r.action === 'update'
        ).length;
        const keptCount = this.importPreviewData.filter(r => r.action === 'keep').length;
        const totalActionable = this.importPreviewData.filter(r =>
            r.status !== 'error' && r.status !== 'unchanged'
        ).length;

        const countContainer = document.getElementById('import-selected-count');
        if (countContainer) {
            let label = `${toProcessCount} of ${totalActionable} rows will be imported`;
            if (keptCount > 0) {
                label += ` (${keptCount} keeping current)`;
            }
            countContainer.innerHTML = label;
        }

        const applyBtn = document.getElementById('apply-import-btn');
        if (applyBtn) {
            const hasChanges = toProcessCount > 0;
            applyBtn.disabled = !hasChanges;
            applyBtn.style.opacity = hasChanges ? '1' : '0.5';
        }
    }

    closeImportPreviewModal() {
        const modal = document.getElementById('import-preview-modal');
        if (modal) {
            modal.style.setProperty('display', 'none', 'important');
        }
        this.importPreviewData = [];
        this.importFileName = '';
    }

    async applySelectedImport() {
        const rowsToProcess = this.importPreviewData.filter(r =>
            r.action === 'create' || r.action === 'update'
        );

        if (rowsToProcess.length === 0) {
            this.showNotification('No rows selected for import', 'warning');
            return;
        }

        const token = this.getAntiForgeryToken();
        let successCount = 0;
        let errorCount = 0;
        const errors = [];

        this.showLoading(true, 'Importing companies...');

        for (const row of rowsToProcess) {
            try {
                const companyData = {
                    id: row.action === 'update' ? row.existingData.id : 0,
                    name: row.data.name,
                    email: row.data.email,
                    url: row.data.url
                };

                const response = await fetch('/Company/SaveCompany', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify(companyData)
                });

                const result = await response.json();
                if (result.success) {
                    successCount++;
                } else {
                    errorCount++;
                    errors.push(`Row ${row.rowNumber} (${row.data.name}): ${result.message || 'Unknown error'}`);
                }
            } catch (error) {
                errorCount++;
                errors.push(`Row ${row.rowNumber} (${row.data.name}): ${error.message}`);
            }
        }

        this.showLoading(false);
        this.closeImportPreviewModal();
        await this.loadCompanies();
        this.renderCompanies();

        if (errorCount === 0) {
            this.showNotification(`Import complete: ${successCount} companies processed successfully`, 'success');
        } else if (successCount > 0) {
            this.showNotification(`Import partial: ${successCount} succeeded, ${errorCount} failed`, 'warning');
            console.error('Import errors:', errors);
        } else {
            this.showNotification(`Import failed: ${errorCount} errors occurred`, 'error');
            console.error('Import errors:', errors);
        }
    }
    
    isValidUrl(url) {
        try {
            new URL(url);
            return true;
        } catch (e) {
            return false;
        }
    }
    
    showLoading(show, message = 'Loading...') {
        let loader = document.getElementById('loading-overlay');
        if (!loader) {
            loader = document.createElement('div');
            loader.id = 'loading-overlay';
            loader.innerHTML = `
                <div class="loading-content">
                    <div class="loading-spinner"></div>
                    <div class="loading-message">${message}</div>
                </div>
            `;
            document.body.appendChild(loader);
        }
        
        loader.querySelector('.loading-message').textContent = message;
        loader.style.display = show ? 'flex' : 'none';
    }
    
    async showNotification(message, type = 'info') {
        // Route all notifications to AdminCore modals for consistency
        const titleMap = {
            'success': 'Success',
            'error': 'Error',
            'warning': 'Warning',
            'info': 'Information'
        };
        const title = titleMap[type] || 'Information';

        switch (type) {
            case 'success':
                await AdminCore.showSuccessModal({ title, message });
                break;
            case 'error':
                await AdminCore.showErrorModal({ title, message });
                break;
            case 'warning':
                await AdminCore.showWarningModal({ title, message });
                break;
            case 'info':
            default:
                await AdminCore.showInfoModal({ title, message });
                break;
        }
    }
    
    getAntiForgeryToken() {
        // Get anti-forgery token from meta tag or hidden input
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
                     document.querySelector('meta[name="__RequestVerificationToken"]')?.content;
        return token || '';
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing Company Management...');
    window.companyManagement = new CompanyManagement();
});