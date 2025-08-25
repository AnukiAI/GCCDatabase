// User Management JavaScript - Full CRUD functionality with backend API integration
class UserManagement {
    constructor() {
        this.users = [];
        this.filteredUsers = [];
        this.currentSort = { field: 'name', direction: 'asc' };
        this.currentEditingId = null;
        
        this.init();
    }
    
    async init() {
        console.log('Initializing User Management...');
        this.bindEvents();
        await this.loadUsers();
        this.renderUsers();
        this.updateUserCount();
    }
    
    bindEvents() {
        // Button events
        document.getElementById('add-user-btn')?.addEventListener('click', () => this.showAddUserModal());
        document.getElementById('export-btn')?.addEventListener('click', () => this.exportUsers());
        document.getElementById('import-btn')?.addEventListener('click', () => this.showImportModal());
        
        // Search and filter events
        document.getElementById('search-input')?.addEventListener('input', (e) => this.handleSearch(e.target.value));
        document.getElementById('status-filter')?.addEventListener('change', () => this.applyFilters());
        document.getElementById('role-filter')?.addEventListener('change', () => this.applyFilters());
        document.getElementById('company-filter')?.addEventListener('change', () => this.applyFilters());
        
        // Modal events
        document.querySelector('.modal-close')?.addEventListener('click', () => this.hideModal());
        document.querySelector('.modal-overlay')?.addEventListener('click', () => this.hideModal());
        document.getElementById('user-form')?.addEventListener('submit', (e) => this.handleFormSubmit(e));
        
        // Table sorting events
        document.querySelectorAll('[data-sort]').forEach(header => {
            header.addEventListener('click', () => this.handleSort(header.dataset.sort));
        });
        
        // Handle escape key to close modal
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') this.hideModal();
        });
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
        const statusClass = user.status?.toLowerCase() === 'active' ? 'status-active' : 'status-inactive';
        const lastLogin = user.lastLogin ? new Date(user.lastLogin).toLocaleDateString() : 'Never';
        const companyDisplay = user.companyNames ? user.companyNames.join(', ') : (user.company || '');
        
        return `
            <tr data-user-id="${user.id}">
                <td class="user-name">${this.escapeHtml(user.name || '')}</td>
                <td class="user-company">${this.escapeHtml(companyDisplay)}</td>
                <td class="user-email">${this.escapeHtml(user.email || '')}</td>
                <td class="user-role">
                    <span class="role-badge role-${(user.role || '').toLowerCase().replace(' ', '-')}">${this.escapeHtml(user.role || '')}</span>
                </td>
                <td class="user-status">
                    <span class="status-badge ${statusClass}">${this.escapeHtml(user.status || '')}</span>
                </td>
                <td class="user-last-login">${lastLogin}</td>
                <td class="actions-col">
                    <div class="action-buttons">
                        <button class="action-btn edit-btn" onclick="userManagement.editUser(${user.id})" title="Edit User">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="action-btn delete-btn" onclick="userManagement.deleteUser(${user.id})" title="Delete User">
                            <i class="fas fa-trash"></i>
                        </button>
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
            if (roleFilter !== 'all' && user.role?.toLowerCase().replace(' ', '-') !== roleFilter) return false;
            
            // Company filter (simplified - in real app would match company IDs)
            if (companyFilter !== 'all') {
                const companyText = user.companyNames ? user.companyNames.join(' ') : (user.company || '');
                const companyMatch = companyText?.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '');
                if (companyMatch !== companyFilter) return false;
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
        document.getElementById('user-company').value = user.companyNames ? user.companyNames.join(', ') : (user.company || '');
        document.getElementById('user-role').value = user.role || '';
        document.getElementById('user-status').value = user.status || '';
        
        this.currentEditingId = userId;
        this.showModal();
    }
    
    async deleteUser(userId) {
        const user = this.users.find(u => u.id === userId);
        if (!user) {
            this.showNotification('User not found', 'error');
            return;
        }
        
        if (!confirm(`Are you sure you want to delete user "${user.name}"? This action cannot be undone.`)) {
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
        
        const formData = {
            name: document.getElementById('user-name').value.trim(),
            email: document.getElementById('user-email').value.trim(),
            company: document.getElementById('user-company').value.trim(),
            role: document.getElementById('user-role').value,
            status: document.getElementById('user-status').value
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
        const modal = document.getElementById('user-modal');
        if (modal) {
            modal.style.display = 'block';
            setTimeout(() => modal.classList.add('show'), 10);
            
            // Focus first input
            const firstInput = modal.querySelector('input[type="text"], input[type="email"]');
            if (firstInput) firstInput.focus();
        }
    }
    
    hideModal() {
        const modal = document.getElementById('user-modal');
        if (modal) {
            modal.classList.remove('show');
            setTimeout(() => modal.style.display = 'none', 300);
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
    console.log('DOM loaded, initializing UserManagement...');
    window.userManagement = new UserManagement();
});