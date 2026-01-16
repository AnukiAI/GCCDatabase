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
            this.updateRoleCount();
            return;
        }

        tbody.innerHTML = this.filteredRoles.map(role => `
            <tr data-role-id="${role.id}" role="row" tabindex="0" aria-label="Role: ${this.escapeHtml(role.name)}">
                <td role="cell">
                    <span class="role-name">${this.escapeHtml(role.name)}</span>
                </td>
                <td role="cell">${this.escapeHtml(role.description || '')}</td>
                <td role="cell">
                    <div class="permissions-list" aria-label="${role.permissions?.length || 0} permissions assigned">
                        ${this.renderPermissions(role.permissions)}
                    </div>
                </td>
                <td class="text-center" role="cell" aria-label="${role.userCount || 0} users">${role.userCount || 0}</td>
                <td class="actions-col" role="cell">
                    <div class="action-buttons">
                        <button class="admin-action-btn admin-action-btn--edit"
                                onclick="roleManager.editRole(${role.id})"
                                title="Edit ${this.escapeHtml(role.name)} role"
                                aria-label="Edit ${this.escapeHtml(role.name)} role">
                            <i class="fas fa-edit" aria-hidden="true"></i>
                        </button>
                        <button class="admin-action-btn admin-action-btn--delete"
                                onclick="roleManager.deleteRole(${role.id})"
                                title="${role.name === 'Admin' ? 'Cannot delete Admin role - system protected' : 'Delete ' + this.escapeHtml(role.name) + ' role'}"
                                aria-label="${role.name === 'Admin' ? 'Cannot delete Admin role - system protected' : 'Delete ' + this.escapeHtml(role.name) + ' role'}"
                                ${role.name === 'Admin' ? 'disabled aria-disabled="true" style="opacity: 0.5; cursor: not-allowed;"' : ''}>
                            <i class="fas fa-trash" aria-hidden="true"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');

        this.updateRoleCount();
    }

    updateRoleCount() {
        const countDisplay = document.getElementById('role-count');
        if (countDisplay) {
            const count = this.filteredRoles.length;
            countDisplay.textContent = `Showing ${count} role${count !== 1 ? 's' : ''}`;
        }
    }
    
    renderPermissions(permissions) {
        if (!permissions || permissions.length === 0) {
            return '<span class="no-permissions">No permissions assigned</span>';
        }

        // Show first 3 permissions and count of remaining
        const displayPerms = permissions.slice(0, 3);
        const remaining = permissions.length - 3;

        // Create tooltip with all permissions
        const allPermsTitle = permissions.map(p => this.escapeHtml(p)).join(', ');

        let html = displayPerms.map(perm =>
            `<span class="permission-badge" title="${allPermsTitle}">${this.escapeHtml(perm)}</span>`
        ).join('');

        if (remaining > 0) {
            html += `<span class="permission-more" title="${allPermsTitle}">+${remaining} more</span>`;
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

        // Populate the existing modal
        document.getElementById('modal-title').textContent = 'Add New Role';
        document.getElementById('role-name').value = '';
        document.getElementById('role-description').value = '';
        document.getElementById('system-role-warning').style.display = 'none';

        // Populate permissions
        this.populatePermissions([]);

        // Show modal using AdminCore if available
        if (typeof AdminCore !== 'undefined' && AdminCore.openModal) {
            AdminCore.openModal('role-modal');
        } else {
            document.getElementById('role-modal').style.display = 'block';
        }
    }

    async editRole(id) {
        const role = this.roles.find(r => r.id === id);
        if (!role) return;

        this.currentEditingId = id;

        // Populate the existing modal
        document.getElementById('modal-title').textContent = 'Edit Role';
        document.getElementById('role-name').value = role.name || '';
        document.getElementById('role-description').value = role.description || '';

        // Show system role warning if applicable
        const isSystemRole = role.isSystem || role.name === 'Admin';
        document.getElementById('system-role-warning').style.display = isSystemRole ? 'block' : 'none';

        // Populate permissions
        this.populatePermissions(role.permissions || []);

        // Show modal using AdminCore if available
        if (typeof AdminCore !== 'undefined' && AdminCore.openModal) {
            AdminCore.openModal('role-modal');
        } else {
            document.getElementById('role-modal').style.display = 'block';
        }
    }

    populatePermissions(selectedPermissions) {
        const container = document.getElementById('permissions-container');
        if (!container) return;

        if (this.permissions.length === 0) {
            container.innerHTML = '<p class="admin-text-muted">No permissions available</p>';
            return;
        }

        // Normalize selectedPermissions to handle various formats
        const normalizedSelected = selectedPermissions || [];
        console.log('Selected permissions:', normalizedSelected);
        console.log('Available permissions:', this.permissions);

        // Group permissions by category for better display
        const permissionsByCategory = {};

        this.permissions.forEach(perm => {
            const category = perm.category || 'Other';
            if (!permissionsByCategory[category]) {
                permissionsByCategory[category] = [];
            }
            permissionsByCategory[category].push(perm);
        });

        // Generate HTML for permissions
        let html = '';
        Object.keys(permissionsByCategory).sort().forEach(category => {
            html += `<div class="admin-permission-group">
                        <h4 class="admin-permission-category">${this.escapeHtml(category)}</h4>
                        <div class="admin-checkbox-grid">`;

            permissionsByCategory[category].forEach(perm => {
                // Check multiple formats: permission name, display name, id as number, id as string
                const isChecked = normalizedSelected.includes(perm.name) ||
                                normalizedSelected.includes(perm.displayName) ||
                                normalizedSelected.includes(perm.id) ||
                                normalizedSelected.includes(perm.id.toString()) ||
                                normalizedSelected.includes(String(perm.id));

                if (isChecked) {
                    console.log(`Checking permission: ${perm.name} (${perm.displayName})`);
                }

                html += `
                    <label class="admin-checkbox-item">
                        <input type="checkbox"
                               name="permissions"
                               value="${perm.id}"
                               ${isChecked ? 'checked' : ''}
                               aria-label="${this.escapeHtml(perm.displayName || perm.name)}">
                        <span>${this.escapeHtml(perm.displayName || perm.name)}</span>
                        ${perm.description ? `<small>${this.escapeHtml(perm.description)}</small>` : ''}
                    </label>
                `;
            });

            html += '</div></div>';
        });

        container.innerHTML = html;
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

                // Close modal using AdminCore if available
                if (typeof AdminCore !== 'undefined' && AdminCore.closeModal) {
                    AdminCore.closeModal('role-modal');
                } else {
                    document.getElementById('role-modal').style.display = 'none';
                }

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

        // Prevent deletion of Admin role
        if (role.name === 'Admin') {
            this.showNotification('The Admin role cannot be deleted as it is required by the system.', 'error');
            return;
        }

        const confirmed = await AdminCore.showConfirmation({
            title: 'Delete Confirmation',
            message: `Are you sure you want to delete the role <strong>${role.name}</strong>? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger',
            allowHtml: true
        });

        if (!confirmed) {
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