// User Management JavaScript - Full CRUD functionality with backend API integration
class UserManagement {
    constructor() {
        this.users = [];
        this.filteredUsers = [];
        this.companies = [];
        this.currentSort = { field: 'name', direction: 'asc' };
        this.currentEditingId = null;
        
        this.init();
    }
    
    async init() {
        console.log('Initializing User Management...');
        this.bindEvents();
        await this.loadCompanies();
        await this.loadUsers();
        this.renderUsers();
        this.updateUserCount();
    }
    
    bindEvents() {
        // Button events
        document.getElementById('add-user-btn')?.addEventListener('click', () => this.showAddUserModal());
        document.getElementById('template-btn')?.addEventListener('click', () => this.downloadImportTemplate());
        document.getElementById('export-btn')?.addEventListener('click', () => this.exportUsers());
        document.getElementById('import-btn')?.addEventListener('click', () => this.showImportModal());
        
        // Search and filter events
        document.getElementById('search-input')?.addEventListener('input', (e) => this.handleSearch(e.target.value));
        document.getElementById('status-filter')?.addEventListener('change', () => this.applyFilters());
        document.getElementById('role-filter')?.addEventListener('change', () => this.applyFilters());
        document.getElementById('company-filter')?.addEventListener('change', () => this.applyFilters());
        
        // Modal events
        document.querySelector('.admin-modal-close')?.addEventListener('click', () => this.hideModal());
        document.querySelector('.admin-modal-overlay')?.addEventListener('click', () => this.hideModal());
        document.getElementById('user-form')?.addEventListener('submit', (e) => this.handleFormSubmit(e));
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
            const response = await fetch('/Admin/GetCompanies', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            const result = await response.json();
            
            if (result.success && result.data) {
                this.companies = result.data;
                console.log(`Loaded ${this.companies.length} companies from backend`);
                this.populateCompanyDropdowns();
            } else {
                console.error('Failed to load companies:', result.message);
                this.companies = [];
            }
        } catch (error) {
            console.error('Error loading companies:', error);
            this.companies = [];
        }
    }

    async loadUsers() {
        try {
            this.showLoading(true);
            const response = await fetch('/Admin/GetUsers', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            const result = await response.json();
            
            if (result.success && result.data) {
                this.users = result.data;
                this.filteredUsers = [...this.users];
                console.log(`Loaded ${this.users.length} users from backend`);
            } else {
                console.error('Failed to load users:', result.message);
                this.showNotification('Failed to load users: ' + (result.message || 'Unknown error'), 'error');
                // Fallback to empty array
                this.users = [];
                this.filteredUsers = [];
            }
        } catch (error) {
            console.error('Error loading users:', error);
            this.showNotification('Error connecting to server. Please check your connection.', 'error');
            // Fallback to empty array  
            this.users = [];
            this.filteredUsers = [];
        } finally {
            this.showLoading(false);
        }
    }
    
    renderUsers() {
        const tbody = document.querySelector('#user-table tbody');
        if (!tbody) return;
        
        if (this.filteredUsers.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="7" class="no-data">
                        <div class="no-data-message">
                            <i class="fas fa-users"></i>
                            <p>No users found</p>
                            <small>Try adjusting your search or filters</small>
                        </div>
                    </td>
                </tr>
            `;
            return;
        }
        
        tbody.innerHTML = this.filteredUsers.map(user => this.createUserRow(user)).join('');
        
        // Bind action button events
        this.bindActionEvents();
    }
    
    createUserRow(user) {
        const statusClass = user.status?.toLowerCase() === 'active' ? 'admin-badge--success' : 'admin-badge--inactive';
        const lastLogin = user.lastLogin ? new Date(user.lastLogin).toLocaleDateString() : 'Never';
        const companyDisplay = user.companyNames ? user.companyNames.join(', ') : (user.company || '');

        return `
            <tr class="admin-table-row" data-id="${user.id}">
                <td class="admin-table-td user-name">${this.escapeHtml(user.name || '')}</td>
                <td class="admin-table-td user-company">${this.escapeHtml(companyDisplay)}</td>
                <td class="admin-table-td user-email">${this.escapeHtml(user.email || '')}</td>
                <td class="admin-table-td user-role">
                    <span class="admin-badge admin-badge--info">${this.escapeHtml(user.role || '')}</span>
                </td>
                <td class="admin-table-td user-status">
                    <span class="admin-badge ${statusClass}">${this.escapeHtml(user.status || '')}</span>
                </td>
                <td class="admin-table-td admin-table-td--hidden-mobile user-last-login">${lastLogin}</td>
                <td class="admin-table-td admin-table-td--actions">
                    <div class="admin-action-buttons">
                        <button class="admin-action-btn admin-action-btn--edit" onclick="userManagement.editUser(${user.id})" title="Edit User" aria-label="Edit ${this.escapeHtml(user.name || 'user')}">
                            <i class="fas fa-edit"></i>
                        </button>
                        <div class="admin-dropdown" style="display: inline-block;">
                            <button class="admin-action-btn admin-action-btn--2fa ${user.twoFactorEnabled ? '' : 'disabled'}" onclick="userManagement.toggle2FADropdown(${user.id}); event.stopPropagation();" title="2FA Management - ${user.twoFactorEnabled ? 'Enabled' : 'Disabled'}" aria-label="2FA Management for ${this.escapeHtml(user.name || 'user')}" style="opacity: ${user.twoFactorEnabled ? '1' : '0.5'}">
                                <i class="fas fa-shield-alt"></i>
                            </button>
                            <div id="dropdown-${user.id}" class="admin-dropdown-menu admin-dropdown-menu--right">
                                <button class="admin-dropdown-item" onclick="userManagement.view2FAStatus(${user.id}); event.stopPropagation();">
                                    <i class="fas fa-eye"></i>
                                    <span>View 2FA Status</span>
                                </button>
                                ${user.twoFactorEnabled ? `
                                    <button class="admin-dropdown-item" onclick="userManagement.reset2FA(${user.id}); event.stopPropagation();">
                                        <i class="fas fa-redo"></i>
                                        <span>Reset 2FA</span>
                                    </button>
                                    <div class="admin-dropdown-divider"></div>
                                    <button class="admin-dropdown-item" onclick="userManagement.disable2FA(${user.id}); event.stopPropagation();" style="color: var(--admin-danger);">
                                        <i class="fas fa-ban"></i>
                                        <span>Disable 2FA</span>
                                    </button>
                                ` : `
                                    <div class="admin-dropdown-divider"></div>
                                    <button class="admin-dropdown-item" onclick="userManagement.enable2FA(${user.id}); event.stopPropagation();" style="color: var(--admin-success);">
                                        <i class="fas fa-shield-alt"></i>
                                        <span>Enable 2FA</span>
                                    </button>
                                `}
                            </div>
                        </div>
                        ${user.role !== 'Admin' ? `
                            <button class="admin-action-btn admin-action-btn--delete" onclick="userManagement.deleteUser(${user.id})" title="Delete User" aria-label="Delete ${this.escapeHtml(user.name || 'user')}">
                                <i class="fas fa-trash-alt"></i>
                            </button>
                        ` : `
                            <span class="admin-note" title="Admin users cannot be deleted">Protected</span>
                        `}
                    </div>
                </td>
            </tr>
        `;
    }
    
    bindActionEvents() {
        // Event delegation is handled by onclick attributes in createUserRow
        // This method can be used for additional event binding if needed
    }
    
    handleSearch(searchTerm) {
        const term = searchTerm.toLowerCase().trim();
        this.applyFilters(term);
    }
    
    applyFilters(searchTerm = null) {
        const statusFilter = document.getElementById('status-filter')?.value || 'all';
        const roleFilter = document.getElementById('role-filter')?.value || 'all';
        const companyFilter = document.getElementById('company-filter')?.value || 'all';
        const search = searchTerm !== null ? searchTerm : (document.getElementById('search-input')?.value || '').toLowerCase().trim();
        
        this.filteredUsers = this.users.filter(user => {
            // Search filter
            if (search) {
                const companyText = user.companyNames ? user.companyNames.join(' ') : (user.company || '');
                const searchableText = [user.name, user.email, companyText, user.role].join(' ').toLowerCase();
                if (!searchableText.includes(search)) return false;
            }
            
            // Status filter
            if (statusFilter !== 'all' && user.status?.toLowerCase() !== statusFilter) return false;
            
            // Role filter  
            if (roleFilter !== 'all' && user.role !== roleFilter) return false;
            
            // Company filter
            if (companyFilter !== 'all') {
                const companyId = parseInt(companyFilter);
                if (!user.companyIds || !user.companyIds.includes(companyId)) return false;
            }
            
            return true;
        });
        
        this.renderUsers();
        this.updateUserCount();
    }
    
    handleSort(field) {
        if (this.currentSort.field === field) {
            this.currentSort.direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
        } else {
            this.currentSort.field = field;
            this.currentSort.direction = 'asc';
        }
        
        this.filteredUsers.sort((a, b) => {
            let aVal = a[field] || '';
            let bVal = b[field] || '';
            
            // Handle date sorting for lastLogin
            if (field === 'lastLogin') {
                aVal = a[field] ? new Date(a[field]) : new Date(0);
                bVal = b[field] ? new Date(b[field]) : new Date(0);
            }
            
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
        this.renderUsers();
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
    
    updateUserCount() {
        const countElement = document.getElementById('user-count');
        if (countElement) {
            countElement.textContent = `Showing ${this.filteredUsers.length} of ${this.users.length} users`;
        }
    }
    
    showAddUserModal() {
        document.getElementById('modal-title').textContent = 'Add New User';
        document.getElementById('user-form').reset();
        document.getElementById('user-id').value = '';
        this.currentEditingId = null;
        
        // Set password as required for new users
        const passwordField = document.getElementById('user-password');
        const passwordHelp = document.getElementById('password-help');
        if (passwordField && passwordHelp) {
            passwordField.required = true;
            passwordHelp.textContent = 'Password is required for new users';
        }
        
        this.showModal();
    }
    
    async editUser(userId) {
        const user = this.users.find(u => u.id === userId);
        if (!user) {
            this.showNotification('User not found', 'error');
            return;
        }
        
        document.getElementById('modal-title').textContent = 'Edit User';
        document.getElementById('user-id').value = user.id;
        document.getElementById('user-name').value = user.name || '';
        document.getElementById('user-email').value = user.email || '';
        document.getElementById('user-role').value = user.role || '';
        document.getElementById('user-status').value = user.status || '';
        
        // Set selected companies
        const companySelect = document.getElementById('user-company');
        if (companySelect && user.companyIds) {
            Array.from(companySelect.options).forEach(option => {
                option.selected = user.companyIds.includes(parseInt(option.value));
            });
        }

        // Set password as optional for existing users
        const passwordField = document.getElementById('user-password');
        const passwordHelp = document.getElementById('password-help');
        if (passwordField && passwordHelp) {
            passwordField.required = false;
            passwordField.value = '';
            passwordHelp.textContent = 'Leave blank to keep current password';
        }
        
        this.currentEditingId = userId;
        this.showModal();
    }
    
    async deleteUser(userId) {
        const user = this.users.find(u => u.id === userId);
        if (!user) {
            this.showNotification('User not found', 'error');
            return;
        }

        // Prevent deletion of Admin users
        if (user.role === 'Admin') {
            this.showNotification('Cannot delete Admin users. Admin users are protected from deletion.', 'error');
            return;
        }

        const confirmed = await AdminCore.showConfirmation({
            title: 'Delete Confirmation',
            message: `Are you sure you want to delete user "${user.name}"? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger'
        });

        if (!confirmed) {
            return;
        }
        
        try {
            this.showLoading(true, 'Deleting user...');
            
            const response = await fetch(`/Admin/DeleteUser/${userId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showNotification(result.message || 'User deleted successfully', 'success');
                await this.loadUsers(); // Reload from server
                this.renderUsers();
                this.updateUserCount();
            } else {
                this.showNotification('Failed to delete user: ' + (result.message || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error deleting user:', error);
            this.showNotification('Error deleting user. Please try again.', 'error');
        } finally {
            this.showLoading(false);
        }
    }
    
    async handleFormSubmit(e) {
        e.preventDefault();
        
        const companySelect = document.getElementById('user-company');
        const selectedCompanies = Array.from(companySelect.selectedOptions).map(option => parseInt(option.value));
        const passwordField = document.getElementById('user-password');
        const passwordValue = passwordField.value.trim();
        
        const formData = {
            name: document.getElementById('user-name').value.trim(),
            email: document.getElementById('user-email').value.trim(),
            companyIds: selectedCompanies,
            role: document.getElementById('user-role').value,
            status: document.getElementById('user-status').value,
            requirePasswordReset: document.getElementById('require-password-reset').checked
        };

        // Add password only if provided (for edits) or required (for new users)
        if (passwordValue) {
            formData.password = passwordValue;
        } else if (!this.currentEditingId) {
            // Generate a temporary password for new users if none provided
            formData.password = 'TempPassword123!';
            formData.requirePasswordReset = true; // Force password reset
        }
        
        // Basic validation
        if (!formData.name || !formData.email) {
            this.showNotification('Name and email are required', 'error');
            return;
        }

        // Password validation for new users
        if (!this.currentEditingId && !passwordValue) {
            this.showNotification('Password is required for new users', 'error');
            return;
        }
        
        // Email validation
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(formData.email)) {
            this.showNotification('Please enter a valid email address', 'error');
            return;
        }
        
        try {
            this.showLoading(true, this.currentEditingId ? 'Updating user...' : 'Creating user...');
            
            let response;
            if (this.currentEditingId) {
                // Update existing user
                response = await fetch(`/Admin/UpdateUser/${this.currentEditingId}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': this.getAntiForgeryToken()
                    },
                    body: JSON.stringify(formData)
                });
            } else {
                // Create new user
                response = await fetch('/Admin/CreateUser', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': this.getAntiForgeryToken()
                    },
                    body: JSON.stringify(formData)
                });
            }
            
            const result = await response.json();
            
            if (result.success) {
                this.showNotification(result.message || (this.currentEditingId ? 'User updated successfully' : 'User created successfully'), 'success');
                this.hideModal();
                await this.loadUsers(); // Reload from server
                this.renderUsers();
                this.updateUserCount();
            } else {
                this.showNotification('Failed to save user: ' + (result.message || 'Unknown error'), 'error');
                if (result.errors && result.errors.length > 0) {
                    console.error('Validation errors:', result.errors);
                }
            }
        } catch (error) {
            console.error('Error saving user:', error);
            this.showNotification('Error saving user. Please try again.', 'error');
        } finally {
            this.showLoading(false);
        }
    }
    
    showModal() {
        if (typeof AdminCore !== 'undefined' && AdminCore.openModal) {
            AdminCore.openModal('user-modal');

            // Focus first input
            setTimeout(() => {
                const modal = document.getElementById('user-modal');
                const firstInput = modal?.querySelector('input[type="text"], input[type="email"]');
                if (firstInput) firstInput.focus();
            }, 100);
        } else {
            // Fallback if AdminCore not available
            const modal = document.getElementById('user-modal');
            if (modal) {
                modal.style.display = 'block';
                setTimeout(() => modal.classList.add('show'), 10);

                const firstInput = modal.querySelector('input[type="text"], input[type="email"]');
                if (firstInput) firstInput.focus();
            }
        }
    }

    hideModal() {
        if (typeof AdminCore !== 'undefined' && AdminCore.closeModal) {
            AdminCore.closeModal('user-modal');
        } else {
            // Fallback if AdminCore not available
            const modal = document.getElementById('user-modal');
            if (modal) {
                modal.classList.remove('show');
                setTimeout(() => modal.style.display = 'none', 300);
            }
        }
        this.currentEditingId = null;
    }
    
    async exportUsers() {
        if (this.filteredUsers.length === 0) {
            this.showNotification('No users to export', 'warning');
            return;
        }
        
        try {
            // Prepare export data
            const exportData = this.filteredUsers.map(user => ({
                ID: user.id,
                Name: user.name || '',
                Email: user.email || '',
                Company: user.company || '',
                Role: user.role || '',
                Status: user.status || '',
                'Last Login': user.lastLogin ? new Date(user.lastLogin).toLocaleDateString() : 'Never'
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
            link.setAttribute('download', `users_export_${new Date().toISOString().split('T')[0]}.csv`);
            link.style.visibility = 'hidden';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            
            this.showNotification(`Exported ${exportData.length} users successfully`, 'success');
        } catch (error) {
            console.error('Error exporting users:', error);
            this.showNotification('Error exporting users. Please try again.', 'error');
        }
    }
    
    downloadImportTemplate() {
        try {
            // Create template data with example row
            const templateData = [
                {
                    Name: 'John Doe',
                    Email: 'john.doe@example.com',
                    Company: 'Company ID or Name',
                    Role: 'Admin',
                    Status: 'active',
                    Password: 'TempPassword123!',
                    RequirePasswordReset: 'true'
                }
            ];

            // Create CSV content
            const headers = ['Name', 'Email', 'Company', 'Role', 'Status', 'Password', 'RequirePasswordReset'];
            const csvContent = [
                headers.join(','),
                ...templateData.map(row =>
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
            link.setAttribute('download', `user_import_template_${new Date().toISOString().split('T')[0]}.csv`);
            link.style.visibility = 'hidden';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            this.showNotification('Import template downloaded successfully', 'success');
        } catch (error) {
            console.error('Error downloading template:', error);
            this.showNotification('Error downloading template. Please try again.', 'error');
        }
    }

    showImportModal() {
        // TODO: Implement import functionality
        this.showNotification('Import functionality coming soon', 'info');
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
        // Use AdminCore if available
        if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
            AdminCore.showNotification(message, type);
            return;
        }

        // Fallback: Create notification element
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
    
    populateCompanyDropdowns() {
        // Update filter dropdown
        const companyFilter = document.getElementById('company-filter');
        if (companyFilter) {
            // Clear existing options except "All Companies"
            const firstOption = companyFilter.children[0];
            companyFilter.innerHTML = '';
            companyFilter.appendChild(firstOption);
            
            // Add company options
            this.companies.forEach(company => {
                const option = document.createElement('option');
                option.value = company.id;
                option.textContent = company.name;
                companyFilter.appendChild(option);
            });
        }

        // Update modal company dropdown
        const userCompanySelect = document.getElementById('user-company');
        if (userCompanySelect) {
            userCompanySelect.innerHTML = '';
            this.companies.forEach(company => {
                const option = document.createElement('option');
                option.value = company.id;
                option.textContent = company.name;
                userCompanySelect.appendChild(option);
            });
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    // 2FA Management Methods
    toggle2FADropdown(userId) {
        console.log('Toggle 2FA dropdown for user:', userId);
        const dropdown = document.getElementById(`dropdown-${userId}`);

        if (!dropdown) {
            console.error('Dropdown not found for user:', userId);
            return;
        }

        const allDropdowns = document.querySelectorAll('.admin-dropdown-menu');
        const allRows = document.querySelectorAll('.admin-table-row');

        // Close all other dropdowns and remove elevated class from all rows
        allDropdowns.forEach(menu => {
            if (menu !== dropdown) {
                menu.classList.remove('admin-dropdown-menu--show');
            }
        });
        allRows.forEach(row => {
            row.classList.remove('dropdown-open');
        });

        // Toggle current dropdown
        dropdown.classList.toggle('admin-dropdown-menu--show');
        console.log('Dropdown show class:', dropdown.classList.contains('admin-dropdown-menu--show'));

        // Add elevated class to parent row for proper z-index stacking
        const parentRow = dropdown.closest('.admin-table-row');
        if (parentRow && dropdown.classList.contains('admin-dropdown-menu--show')) {
            parentRow.classList.add('dropdown-open');
        }

        // Close dropdown when clicking outside
        if (dropdown.classList.contains('admin-dropdown-menu--show')) {
            document.addEventListener('click', this.closeDropdownOnOutsideClick, { once: true });
        }
    }

    closeDropdownOnOutsideClick = (event) => {
        if (!event.target.closest('.admin-dropdown')) {
            document.querySelectorAll('.admin-dropdown-menu').forEach(menu => {
                menu.classList.remove('admin-dropdown-menu--show');
            });
            // Remove elevated class from all rows
            document.querySelectorAll('.admin-table-row').forEach(row => {
                row.classList.remove('dropdown-open');
            });
        }
    }
    
    async view2FAStatus(userId) {
        try {
            const user = this.users.find(u => u.id === userId);
            if (!user) {
                this.showNotification('User not found', 'error');
                return;
            }
            
            // Show 2FA status modal or navigate to status page
            const statusInfo = `
                <strong>2FA Status for ${user.name}</strong><br>
                Email: ${user.email}<br>
                Current Status: ${user.twoFactorEnabled ? 'Enabled' : 'Disabled'}<br>
                Method: ${user.twoFactorMethod || 'None'}<br>
                Last Updated: ${user.twoFactorLastUpdated || 'Never'}
            `;
            
            this.showNotification(statusInfo, 'info');
            
        } catch (error) {
            console.error('Error viewing 2FA status:', error);
            this.showNotification('Failed to load 2FA status', 'error');
        }
    }
    
    async reset2FA(userId) {
        const user = this.users.find(u => u.id === userId);
        if (!user) {
            this.showNotification('User not found', 'error');
            return;
        }

        const confirmed = await AdminCore.showConfirmation({
            title: 'Reset Two-Factor Authentication',
            message: `Are you sure you want to reset 2FA for ${user.name}? This will remove their current 2FA setup and require them to set it up again.`,
            confirmText: 'Reset 2FA',
            cancelText: 'Cancel',
            type: 'warning'
        });

        if (!confirmed) {
            return;
        }
        
        try {
            const response = await fetch('/Admin/Reset2FA', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({ userId: userId })
            });
            
            if (response.ok) {
                this.showNotification(`2FA reset successfully for ${user.name}`, 'success');
            } else {
                this.showNotification('Failed to reset 2FA', 'error');
            }
            
        } catch (error) {
            console.error('Error resetting 2FA:', error);
            this.showNotification('Failed to reset 2FA', 'error');
        }
    }
    
    async disable2FA(userId) {
        const user = this.users.find(u => u.id === userId);
        if (!user) {
            this.showNotification('User not found', 'error');
            return;
        }

        const confirmed = await AdminCore.showConfirmation({
            title: 'Disable Two-Factor Authentication',
            message: `Are you sure you want to disable 2FA for ${user.name}? This will make their account less secure.`,
            confirmText: 'Disable 2FA',
            cancelText: 'Cancel',
            type: 'warning'
        });

        if (!confirmed) {
            return;
        }

        try {
            const response = await fetch('/Admin/Disable2FA', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({ userId: userId })
            });

            if (response.ok) {
                this.showNotification(`2FA disabled for ${user.name}`, 'success');
                await this.loadUsers(); // Reload to update UI
                this.renderUsers();
            } else {
                this.showNotification('Failed to disable 2FA', 'error');
            }

        } catch (error) {
            console.error('Error disabling 2FA:', error);
            this.showNotification('Failed to disable 2FA', 'error');
        }
    }

    async enable2FA(userId) {
        const user = this.users.find(u => u.id === userId);
        if (!user) {
            this.showNotification('User not found', 'error');
            return;
        }

        const confirmed = await AdminCore.showConfirmation({
            title: 'Enable Two-Factor Authentication',
            message: `Enable 2FA for ${user.name}? They will be required to set up 2FA on their next login.`,
            confirmText: 'Enable 2FA',
            cancelText: 'Cancel',
            type: 'info'
        });

        if (!confirmed) {
            return;
        }

        try {
            const response = await fetch('/Admin/Enable2FA', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({
                    userId: userId,
                    twoFactorMethod: 'TOTP'  // Default to TOTP (authenticator app)
                })
            });

            if (response.ok) {
                this.showNotification(`2FA enabled for ${user.name}. They will be prompted to set it up on next login.`, 'success');
                await this.loadUsers(); // Reload to update UI
                this.renderUsers();
            } else {
                this.showNotification('Failed to enable 2FA', 'error');
            }

        } catch (error) {
            console.error('Error enabling 2FA:', error);
            this.showNotification('Failed to enable 2FA', 'error');
        }
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing UserManagement...');
    window.userManagement = new UserManagement();
});