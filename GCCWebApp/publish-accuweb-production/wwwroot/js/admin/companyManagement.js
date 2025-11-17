// Company Management JavaScript - Full CRUD functionality with backend API integration
class CompanyManagement {
    constructor() {
        this.companies = [];
        this.filteredCompanies = [];
        this.currentSort = { field: 'name', direction: 'asc' };
        this.currentEditingId = null;
        
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
            message: `Are you sure you want to delete company "${company.name}"? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger'
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
        // TODO: Implement import functionality
        this.showNotification('Import functionality coming soon', 'info');
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
    
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fas fa-${this.getNotificationIcon(type)}"></i>
                <span class="notification-message">${this.escapeHtml(message)}</span>
                <button class="notification-close">&times;</button>
            </div>
        `;
        
        // Add to DOM
        document.body.appendChild(notification);
        
        // Show with animation
        setTimeout(() => notification.classList.add('show'), 10);
        
        // Auto-hide after 5 seconds
        const timeout = setTimeout(() => this.hideNotification(notification), 5000);
        
        // Close button
        notification.querySelector('.notification-close').addEventListener('click', () => {
            clearTimeout(timeout);
            this.hideNotification(notification);
        });
    }
    
    hideNotification(notification) {
        notification.classList.remove('show');
        setTimeout(() => notification.remove(), 300);
    }
    
    getNotificationIcon(type) {
        switch (type) {
            case 'success': return 'check-circle';
            case 'error': return 'exclamation-circle';
            case 'warning': return 'exclamation-triangle';
            case 'info': 
            default: return 'info-circle';
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