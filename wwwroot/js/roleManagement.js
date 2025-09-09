// Role Management JavaScript - Full CRUD functionality with backend API integration
class RoleManagement {
    constructor() {
        this.roles = [];
        this.filteredRoles = [];
        this.permissions = [];
        this.currentSort = { field: 'name', direction: 'asc' };
        this.currentEditingId = null;
        
        this.init();
    }
    
    async init() {
        console.log('Initializing Role Management...');
        this.bindEvents();
        await this.loadPermissions();
        await this.loadRoles();
        this.renderRoles();
    }
    
    bindEvents() {
        // Button events
        document.getElementById('add-role-btn')?.addEventListener('click', () => this.showAddRoleModal());
        document.getElementById('export-btn')?.addEventListener('click', () => this.exportRoles());
        
        // Search event
        document.getElementById('search-input')?.addEventListener('input', (e) => this.handleSearch(e.target.value));
        
        // Table sorting events
        document.querySelectorAll('[data-sort]').forEach(header => {
            header.addEventListener('click', () => this.handleSort(header.dataset.sort));
        });
    }
    
    async loadPermissions() {
        try {
            const response = await fetch('/Admin/GetPermissions', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            const result = await response.json();
            
            if (result.success && result.data) {
                this.permissions = result.data;
                console.log(`Loaded ${this.permissions.length} permissions`);
            } else {
                console.error('Failed to load permissions:', result.message);
                this.permissions = [];
            }
        } catch (error) {
            console.error('Error loading permissions:', error);
            this.permissions = [];
        }
    }
    
    async loadRoles() {
        try {
            this.showLoading(true);
            const response = await fetch('/Admin/GetRoles', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            const result = await response.json();
            
            if (result.success && result.data) {
                this.roles = result.data;
                this.filteredRoles = [...this.roles];
                console.log(`Loaded ${this.roles.length} roles from backend`);
            } else {
                console.error('Failed to load roles:', result.message);
                this.showNotification('Failed to load roles: ' + (result.message || 'Unknown error'), 'error');
                this.roles = [];
                this.filteredRoles = [];
            }
        } catch (error) {
            console.error('Error loading roles:', error);
            this.showNotification('Error connecting to server. Please check your connection.', 'error');
            this.roles = [];
            this.filteredRoles = [];
        } finally {
            this.showLoading(false);
        }
    }
    
    renderRoles() {
        const tbody = document.querySelector('#role-table tbody');
        if (!tbody) return;
        
        if (this.filteredRoles.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="no-data">
                        <div class="no-data-message">
                            <i class="fas fa-user-shield"></i>
                            <p>No roles found</p>
                            <small>Try adjusting your search</small>
                        </div>
                    </td>
                </tr>
            `;
            return;
        }
        
        tbody.innerHTML = this.filteredRoles.map(role => `
            <tr data-role-id="${role.id}">
                <td>
                    <strong>${this.escapeHtml(role.name)}</strong>
                    ${role.isSystemRole ? '<span class="badge badge-system">System</span>' : ''}
                </td>
                <td>${this.escapeHtml(role.description || '')}</td>
                <td>
                    <div class="permissions-list">
                        ${this.renderPermissions(role.permissions)}
                    </div>
                </td>
                <td class="text-center">${role.userCount || 0}</td>
                <td class="actions-col">
                    <div class="action-buttons">
                        ${!role.isSystem ? `
                            <button class="btn-icon" onclick="roleManager.editRole(${role.id})" title="Edit">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn-icon btn-delete" onclick="roleManager.deleteRole(${role.id})" title="Delete">
                                <i class="fas fa-trash"></i>
                            </button>
                        ` : `
                            <button class="btn-icon" onclick="roleManager.viewRole(${role.id})" title="View System Role">
                                <i class="fas fa-eye"></i>
                            </button>
                            <span class="system-role-note" title="System roles cannot be deleted">System</span>
                        `}
                    </div>
                </td>
            </tr>
        `).join('');
    }
    
    renderPermissions(permissions) {
        if (!permissions || permissions.length === 0) {
            return '<span class="no-permissions">No permissions assigned</span>';
        }
        
        // Show first 3 permissions and count of remaining
        const displayPerms = permissions.slice(0, 3);
        const remaining = permissions.length - 3;
        
        let html = displayPerms.map(perm => 
            `<span class="permission-badge">${this.escapeHtml(perm)}</span>`
        ).join('');
        
        if (remaining > 0) {
            html += `<span class="permission-more">+${remaining} more</span>`;
        }
        
        return html;
    }
    
    handleSearch(searchTerm) {
        searchTerm = searchTerm.toLowerCase().trim();
        
        if (!searchTerm) {
            this.filteredRoles = [...this.roles];
        } else {
            this.filteredRoles = this.roles.filter(role => {
                return role.name.toLowerCase().includes(searchTerm) ||
                       (role.description && role.description.toLowerCase().includes(searchTerm)) ||
                       (role.permissions && role.permissions.some(p => p.toLowerCase().includes(searchTerm)));
            });
        }
        
        this.renderRoles();
    }
    
    handleSort(field) {
        // Toggle direction if same field
        if (this.currentSort.field === field) {
            this.currentSort.direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
        } else {
            this.currentSort.field = field;
            this.currentSort.direction = 'asc';
        }
        
        // Update sort icons
        document.querySelectorAll('.sort-icon').forEach(icon => {
            icon.textContent = '▼';
        });
        
        const currentIcon = document.querySelector(`[data-sort="${field}"] .sort-icon`);
        if (currentIcon) {
            currentIcon.textContent = this.currentSort.direction === 'asc' ? '▲' : '▼';
        }
        
        // Sort the roles
        this.filteredRoles.sort((a, b) => {
            let aVal = a[field];
            let bVal = b[field];
            
            // Handle special cases
            if (field === 'users') {
                aVal = a.userCount || 0;
                bVal = b.userCount || 0;
            }
            
            // Handle null/undefined
            if (aVal == null) aVal = '';
            if (bVal == null) bVal = '';
            
            // Compare
            if (typeof aVal === 'string') {
                aVal = aVal.toLowerCase();
                bVal = bVal.toLowerCase();
            }
            
            if (aVal < bVal) return this.currentSort.direction === 'asc' ? -1 : 1;
            if (aVal > bVal) return this.currentSort.direction === 'asc' ? 1 : -1;
            return 0;
        });
        
        this.renderRoles();
    }
    
    showAddRoleModal() {
        this.currentEditingId = null;
        this.showModal('Add New Role', {
            name: '',
            description: '',
            permissions: []
        });
    }
    
    async editRole(id) {
        const role = this.roles.find(r => r.id === id);
        if (!role) return;
        
        this.currentEditingId = id;
        this.showModal('Edit Role', role);
    }
    
    viewRole(id) {
        const role = this.roles.find(r => r.id === id);
        if (!role) return;
        
        this.showModal('View System Role', role, true);
    }
    
    showModal(title, role, readOnly = false) {
        // Create modal HTML
        const modalHtml = `
            <div id="role-modal" class="modal">
                <div class="modal-overlay"></div>
                <div class="modal-content">
                    <div class="modal-header">
                        <h3 class="modal-title">${title}</h3>
                        <span class="modal-close">&times;</span>
                    </div>
                    <div class="modal-body">
                        <form id="role-form">
                            <div class="form-group">
                                <label for="role-name">Role Name</label>
                                <input type="text" id="role-name" value="${this.escapeHtml(role.name || '')}" 
                                       ${readOnly ? 'readonly' : 'required'}>
                            </div>
                            
                            <div class="form-group">
                                <label for="role-description">Description</label>
                                <textarea id="role-description" rows="3" 
                                          ${readOnly ? 'readonly' : ''}>${this.escapeHtml(role.description || '')}</textarea>
                            </div>
                            
                            <div class="form-group">
                                <label>Permissions</label>
                                <div class="permissions-container">
                                    ${this.renderPermissionCheckboxes(role.permissions || [], readOnly)}
                                </div>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer">
                        ${!readOnly ? `
                            <button type="button" class="btn btn-cancel" onclick="roleManager.hideModal()">Cancel</button>
                            <button type="submit" class="btn btn-primary" onclick="roleManager.saveRole(event)">Save</button>
                        ` : `
                            <button type="button" class="btn btn-primary" onclick="roleManager.hideModal()">Close</button>
                        `}
                    </div>
                </div>
            </div>
        `;
        
        // Add modal to page
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        
        // Show modal
        setTimeout(() => {
            document.getElementById('role-modal').style.display = 'block';
        }, 10);
        
        // Bind close events
        document.querySelector('.modal-close').addEventListener('click', () => this.hideModal());
        document.querySelector('.modal-overlay').addEventListener('click', () => this.hideModal());
    }
    
    renderPermissionCheckboxes(selectedPermissions, readOnly) {
        if (this.permissions.length === 0) {
            return '<p>No permissions available</p>';
        }
        
        return this.permissions.map(perm => `
            <label class="permission-checkbox">
                <input type="checkbox" 
                       name="permissions" 
                       value="${perm.id}"
                       ${selectedPermissions.includes(perm.name) ? 'checked' : ''}
                       ${readOnly ? 'disabled' : ''}>
                <span>${this.escapeHtml(perm.name)}</span>
                ${perm.description ? `<small>${this.escapeHtml(perm.description)}</small>` : ''}
            </label>
        `).join('');
    }
    
    hideModal() {
        const modal = document.getElementById('role-modal');
        if (modal) {
            modal.remove();
        }
    }
    
    async saveRole(event) {
        event.preventDefault();
        
        const name = document.getElementById('role-name').value.trim();
        const description = document.getElementById('role-description').value.trim();
        const permissions = Array.from(document.querySelectorAll('input[name="permissions"]:checked'))
            .map(cb => cb.value);
        
        if (!name) {
            this.showNotification('Role name is required', 'error');
            return;
        }
        
        const roleData = {
            name: name,
            description: description,
            permissions: permissions
        };
        
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const url = this.currentEditingId 
                ? `/Admin/UpdateRole/${this.currentEditingId}`
                : '/Admin/CreateRole';
            const method = this.currentEditingId ? 'PUT' : 'POST';
            
            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                },
                body: JSON.stringify(roleData)
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showNotification(result.message || 'Role saved successfully', 'success');
                this.hideModal();
                await this.loadRoles();
                this.renderRoles();
            } else {
                this.showNotification(result.message || 'Failed to save role', 'error');
            }
        } catch (error) {
            console.error('Error saving role:', error);
            this.showNotification('Error saving role. Please try again.', 'error');
        }
    }
    
    async deleteRole(id) {
        const role = this.roles.find(r => r.id === id);
        if (!role) return;

        // Prevent deletion of system roles
        if (role.isSystem) {
            this.showNotification('Cannot delete system roles. System roles are protected from deletion.', 'error');
            return;
        }
        
        if (!confirm(`Are you sure you want to delete the role "${role.name}"?`)) {
            return;
        }
        
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const response = await fetch(`/Admin/DeleteRole/${id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                }
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showNotification(result.message || 'Role deleted successfully', 'success');
                await this.loadRoles();
                this.renderRoles();
            } else {
                this.showNotification(result.message || 'Failed to delete role', 'error');
            }
        } catch (error) {
            console.error('Error deleting role:', error);
            this.showNotification('Error deleting role. Please try again.', 'error');
        }
    }
    
    async exportRoles() {
        try {
            window.location.href = '/Admin/ExportData?entityType=roles&format=csv';
        } catch (error) {
            console.error('Error exporting roles:', error);
            this.showNotification('Error exporting roles', 'error');
        }
    }
    
    showLoading(show) {
        const tbody = document.querySelector('#role-table tbody');
        if (!tbody) return;
        
        if (show) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="loading">
                        <div class="spinner"></div>
                        <p>Loading roles...</p>
                    </td>
                </tr>
            `;
        }
    }
    
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <i class="fas fa-${type === 'success' ? 'check' : type === 'error' ? 'exclamation' : 'info'}-circle"></i>
            <span>${message}</span>
        `;
        
        // Add to page
        document.body.appendChild(notification);
        
        // Show notification
        setTimeout(() => notification.classList.add('show'), 10);
        
        // Hide and remove after 3 seconds
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.roleManager = new RoleManagement();
});