// User Management JavaScript - Full CRUD functionality with backend API integration
class UserManagement {
    constructor() {
        this.users = [];
        this.filteredUsers = [];
        this.companies = [];
        this.roles = [];
        this.partners = [];
        this.currentSort = { field: 'name', direction: 'asc' };
        this.currentEditingId = null;
        this.pagination = null;

        this.init();
    }

    async init() {
        console.log('Initializing User Management...');
        this.bindEvents();
        await this.loadCompanies();
        await this.loadRoles();
        await this.loadPartners();
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

        // Role change handler - show/hide Company/Partner fields based on role
        document.getElementById('user-role')?.addEventListener('change', (e) => this.handleRoleChange(e.target.value));

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

    async loadRoles() {
        try {
            const response = await fetch('/Admin/GetRolesList', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            const result = await response.json();

            if (result.success && result.data) {
                this.roles = result.data;
                console.log(`Loaded ${this.roles.length} roles from backend`);
                this.populateRoleDropdowns();
            } else {
                console.error('Failed to load roles:', result.message);
                // Fallback to default roles if API fails
                this.roles = [
                    { id: 1, name: 'Admin' },
                    { id: 2, name: 'Analyst' },
                    { id: 3, name: 'Partner' },
                    { id: 4, name: 'External' },
                    { id: 5, name: 'API Integration' },
                    { id: 6, name: 'Public' }
                ];
                this.populateRoleDropdowns();
            }
        } catch (error) {
            console.error('Error loading roles:', error);
            // Fallback to default roles if API fails
            this.roles = [
                { id: 1, name: 'Admin' },
                { id: 2, name: 'Analyst' },
                { id: 3, name: 'Partner' },
                { id: 4, name: 'External' },
                { id: 5, name: 'API Integration' },
                { id: 6, name: 'Public' }
            ];
            this.populateRoleDropdowns();
        }
    }

    async loadPartners() {
        try {
            const response = await fetch('/Admin/GetPartners', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            const result = await response.json();

            if (result.success && result.data) {
                this.partners = result.data;
                console.log(`Loaded ${this.partners.length} partners from backend`);
                this.populatePartnerDropdown();
            } else {
                console.error('Failed to load partners:', result.message);
                this.partners = [];
            }
        } catch (error) {
            console.error('Error loading partners:', error);
            this.partners = [];
        }
    }

    populatePartnerDropdown() {
        const partnerSelect = document.getElementById('user-partner');
        if (partnerSelect) {
            // Clear existing options except "Select a partner"
            const firstOption = partnerSelect.children[0];
            partnerSelect.innerHTML = '';
            partnerSelect.appendChild(firstOption);

            // Add partner options
            this.partners.forEach(partner => {
                const option = document.createElement('option');
                option.value = partner.id;
                option.textContent = partner.name;
                partnerSelect.appendChild(option);
            });
        }
    }

    handleRoleChange(role) {
        const companyGroup = document.getElementById('company-group');
        const partnerGroup = document.getElementById('partner-group');
        const partnerSelect = document.getElementById('user-partner');

        // Reset required attribute
        if (partnerSelect) {
            partnerSelect.required = false;
        }

        // Role-based visibility:
        // - Partner role: Show Partner dropdown, hide Companies
        // - External role: Show Companies, hide Partner
        // - Admin/Analyst: Hide both (full access, no filtering)
        // - API Integration/Public: Hide both

        if (role === 'Partner') {
            // Partner users see all certificates tagged with their partner
            if (companyGroup) companyGroup.style.display = 'none';
            if (partnerGroup) partnerGroup.style.display = 'block';
            if (partnerSelect) partnerSelect.required = true;
        } else {
            // All non-Partner roles: show Company, hide Partner
            if (companyGroup) companyGroup.style.display = 'block';
            if (partnerGroup) partnerGroup.style.display = 'none';
        }
    }

    populateRoleDropdowns() {
        // Update filter dropdown
        const roleFilter = document.getElementById('role-filter');
        if (roleFilter) {
            // Clear existing options except "All Roles"
            const firstOption = roleFilter.children[0];
            roleFilter.innerHTML = '';
            roleFilter.appendChild(firstOption);

            // Add role options
            this.roles.forEach(role => {
                const option = document.createElement('option');
                option.value = role.name;
                option.textContent = role.name;
                roleFilter.appendChild(option);
            });
        }

        // Update modal role dropdown
        const roleSelect = document.getElementById('user-role');
        if (roleSelect) {
            // Clear existing options except "Select a role"
            const firstOption = roleSelect.children[0];
            roleSelect.innerHTML = '';
            roleSelect.appendChild(firstOption);

            // Add role options
            this.roles.forEach(role => {
                const option = document.createElement('option');
                option.value = role.name;
                option.textContent = role.name;
                roleSelect.appendChild(option);
            });
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
                    <td colspan="8" class="no-data">
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

        // Use pagination if available
        if (typeof AdminPagination !== 'undefined') {
            if (!this.pagination) {
                this.pagination = new AdminPagination({
                    containerId: 'user-pagination',
                    pageSize: 25,
                    onPageChange: (pageData) => {
                        this.renderPage(pageData);
                    }
                });
            }
            this.pagination.setData(this.filteredUsers);
        } else {
            tbody.innerHTML = this.filteredUsers.map(user => this.createUserRow(user)).join('');
            this.bindActionEvents();
        }
    }

    renderPage(pageData) {
        const tbody = document.querySelector('#user-table tbody');
        if (!tbody) return;
        tbody.innerHTML = pageData.map(user => this.createUserRow(user)).join('');
        this.bindActionEvents();
    }
    
    getCompanyAccessDisplay(user) {
        const ids = user.companyIds || [];
        if (ids.length === 0) {
            return user.company || '';
        }
        if (this.companies && this.companies.length > 0 &&
            this.companies.every(c => ids.includes(c.id))) {
            return 'All';
        }
        return user.companyNames ? user.companyNames.join(', ') : (user.company || '');
    }

    createUserRow(user) {
        const statusClass = user.status?.toLowerCase() === 'active' ? 'admin-badge--success' : 'admin-badge--inactive';
        const lastLogin = user.lastLogin ? new Date(user.lastLogin).toLocaleDateString() : 'Never';
        const companyDisplay = this.getCompanyAccessDisplay(user);

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
                <td class="admin-table-td user-2fa-status">
                    ${user.twoFactorEnabled ?
                        `<span class="admin-badge admin-badge--success" title="2FA Method: ${this.escapeHtml(user.twoFactorMethod || 'TOTP')}"><i class="fas fa-shield-alt"></i> Enabled</span>` :
                        `<span class="admin-badge admin-badge--inactive" title="Two-factor authentication is not enabled"><i class="fas fa-unlock"></i> Disabled</span>`
                    }
                </td>
                <td class="admin-table-td admin-table-td--actions">
                    <div class="admin-action-buttons">
                        <div class="admin-dropdown" style="display: inline-block;">
                            <button class="admin-action-btn admin-action-btn--menu" onclick="userManagement.toggleActionsDropdown(${user.id}); event.stopPropagation();" title="Actions menu" aria-label="Actions for ${this.escapeHtml(user.name || 'user')}">
                                <i class="fas fa-ellipsis-v"></i>
                            </button>
                            <div id="actions-dropdown-${user.id}" class="admin-dropdown-menu admin-dropdown-menu--right">
                                <button class="admin-dropdown-item" onclick="userManagement.editUser(${user.id}); event.stopPropagation();">
                                    <i class="fas fa-edit"></i>
                                    <span>Edit User</span>
                                </button>
                                <div class="admin-dropdown-divider"></div>
                                <button class="admin-dropdown-item" onclick="userManagement.view2FAStatus(${user.id}); event.stopPropagation();">
                                    <i class="fas fa-eye"></i>
                                    <span>View 2FA Status</span>
                                </button>
                                ${user.twoFactorEnabled ? `
                                    <button class="admin-dropdown-item" onclick="userManagement.reset2FA(${user.id}); event.stopPropagation();">
                                        <i class="fas fa-redo"></i>
                                        <span>Reset 2FA</span>
                                    </button>
                                    <button class="admin-dropdown-item" onclick="userManagement.disable2FA(${user.id}); event.stopPropagation();" style="color: var(--admin-danger);">
                                        <i class="fas fa-ban"></i>
                                        <span>Disable 2FA</span>
                                    </button>
                                ` : `
                                    <button class="admin-dropdown-item" onclick="userManagement.enable2FA(${user.id}); event.stopPropagation();" style="color: var(--admin-success);">
                                        <i class="fas fa-shield-alt"></i>
                                        <span>Enable 2FA</span>
                                    </button>
                                `}
                                ${user.role !== 'Admin' ? `
                                    <div class="admin-dropdown-divider"></div>
                                    <button class="admin-dropdown-item admin-dropdown-item--danger" onclick="userManagement.deleteUser(${user.id}); event.stopPropagation();">
                                        <i class="fas fa-trash"></i>
                                        <span>Delete User</span>
                                    </button>
                                ` : `
                                    <div class="admin-dropdown-divider"></div>
                                    <div class="admin-dropdown-item admin-dropdown-item--disabled" title="Admin users cannot be deleted">
                                        <i class="fas fa-shield-alt"></i>
                                        <span>Protected User</span>
                                    </div>
                                `}
                            </div>
                        </div>
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

        // Reset partner field
        const partnerSelect = document.getElementById('user-partner');
        if (partnerSelect) {
            partnerSelect.value = '';
        }

        // Reset role-based field visibility (show company by default, hide partner)
        const companyGroup = document.getElementById('company-group');
        const partnerGroup = document.getElementById('partner-group');
        if (companyGroup) companyGroup.style.display = 'block';
        if (partnerGroup) partnerGroup.style.display = 'none';

        // Reset Check All checkbox
        const checkAll = document.getElementById('company-check-all');
        if (checkAll) checkAll.checked = false;

        this.showModal();
    }
    
    async editUser(userId) {
        // Close dropdowns first
        this.closeAllDropdowns();

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

        // Trigger role change to show/hide appropriate fields
        this.handleRoleChange(user.role || '');

        // Set selected companies via checkboxes
        const companyCheckboxes = document.querySelectorAll('input[name="user-companies"]');
        if (companyCheckboxes && user.companyIds) {
            companyCheckboxes.forEach(checkbox => {
                checkbox.checked = user.companyIds.includes(parseInt(checkbox.value));
            });
            // Sync Check All state
            const checkAll = document.getElementById('company-check-all');
            if (checkAll) {
                checkAll.checked = companyCheckboxes.length > 0 && Array.from(companyCheckboxes).every(cb => cb.checked);
            }
        }

        // Set selected partner (first partner in the array, per design: single partner per user)
        const partnerSelect = document.getElementById('user-partner');
        if (partnerSelect && user.partnerIds && user.partnerIds.length > 0) {
            partnerSelect.value = user.partnerIds[0];
        } else if (partnerSelect) {
            partnerSelect.value = '';
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
        // Close dropdowns first
        this.closeAllDropdowns();

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
            message: `Are you sure you want to delete user <strong>${user.name}</strong>? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger',
            allowHtml: true
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

        const companyCheckboxes = document.querySelectorAll('input[name="user-companies"]:checked');
        const selectedCompanies = Array.from(companyCheckboxes).map(checkbox => parseInt(checkbox.value));
        const passwordField = document.getElementById('user-password');
        const passwordValue = passwordField.value.trim();
        const role = document.getElementById('user-role').value;

        // Get partner selection (for Partner role)
        const partnerSelect = document.getElementById('user-partner');
        const selectedPartner = partnerSelect ? partnerSelect.value : '';
        const partnerIds = selectedPartner ? [parseInt(selectedPartner)] : [];

        const formData = {
            name: document.getElementById('user-name').value.trim(),
            email: document.getElementById('user-email').value.trim(),
            companyIds: role !== 'Partner' ? selectedCompanies : [],  // All roles except Partner
            partnerIds: role === 'Partner' ? partnerIds : [],  // Only for Partner role
            role: role,
            status: document.getElementById('user-status').value,
            requirePasswordReset: document.getElementById('require-password-reset').checked
        };

        // Add password only if provided (for edits) or required (for new users)
        if (passwordValue) {
            formData.password = passwordValue;
        } else if (!this.currentEditingId) {
            // Generate a temporary password for new users if none provided
            formData.password = 'TestPass123!';
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
        
        // Email validation — QC #181 alignment with UserViewModel/CreateUserViewModel server regex.
        // Requires name@domain.tld with a 2+ char TLD; rejects "a@b" and "a@b.c".
        const emailRegex = /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/;
        if (!emailRegex.test(formData.email)) {
            this.showNotification('Enter a valid email address (e.g., name@example.com).', 'error');
            return;
        }

        // Role-specific validation
        if (role === 'Partner' && formData.partnerIds.length === 0) {
            this.showNotification('Partner role requires a partner selection', 'error');
            return;
        }

        if (role === 'External' && formData.companyIds.length === 0) {
            this.showNotification('External role requires at least one company selection', 'error');
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
                let errorMsg = result.message || 'Unknown error';
                if (result.errors && result.errors.length > 0) {
                    errorMsg += ': ' + result.errors.join(', ');
                }
                this.showNotification('Failed to save user: ' + errorMsg, 'error');
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
            // CSV columns must match UI table headers exactly: Name, User Company, Email, Role, Status, Last Login, 2FA
            const exportData = this.filteredUsers.map(user => {
                const access = this.getCompanyAccessDisplay(user);
                return {
                    Name: user.name || '',
                    'User Company': access === 'All'
                        ? 'All'
                        : (user.companyNames ? user.companyNames.join('; ') : (user.company || '')),
                    Email: user.email || '',
                    Role: user.role || '',
                    Status: user.status || '',
                    'Last Login': user.lastLogin ? new Date(user.lastLogin).toLocaleDateString() : 'Never',
                    '2FA': user.twoFactorEnabled ? 'Enabled' : 'Disabled'
                };
            });

            // Create CSV content
            const headers = Object.keys(exportData[0]);
            const csvContent = [
                headers.join(','),
                ...exportData.map(row =>
                    headers.map(header => {
                        // Convert value to string to handle numbers
                        const value = String(row[header] ?? '');
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
            // DEF-186: Password and RequirePasswordReset columns removed. Server generates
            // a cryptographically random temporary password per imported user and forces
            // password reset on first login. Admins should never type plaintext passwords
            // into a CSV that ends up in Downloads / SharePoint / email.
            // Multiple companies separated by semicolons (company names, not IDs); use literal "All" to grant access to every company.
            const headers = [
                'Name',
                'Email',
                'Role (Admin/Analyst/External)',
                'Status (Active/Inactive)',
                'User Company (semicolon-separated names, or "All")'
            ];
            // Quote any header that contains a comma or quote so the row parses as the
            // correct column count (RFC 4180). Otherwise the User Company header's
            // internal comma yields a phantom extra column.
            const csvContent = headers
                .map(h => /[",]/.test(h) ? `"${h.replace(/"/g, '""')}"` : h)
                .join(',');

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

    // Import preview state
    importPreviewData = [];
    importFileName = '';

    async showImportModal() {
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
            this.showNotification('CSV file must have a header row and at least one data row', 'warning');
            return;
        }

        // Parse and normalize headers
        const rawHeaders = this.parseCSVLine(lines[0]).map(h => h.trim());
        const headers = rawHeaders.map(h => this.normalizeUserHeaderName(h));

        const parsedUsers = [];

        // Parse data rows
        for (let i = 1; i < lines.length; i++) {
            const line = lines[i].trim();
            if (!line) continue;

            const fields = this.parseCSVLine(line);
            if (fields.length < 2) continue;

            // Build user object from headers
            const user = {};
            headers.forEach((header, index) => {
                user[header] = fields[index]?.trim() || '';
            });

            const name = user.name || '';
            const email = user.email || '';
            if (!name && !email) continue;

            parsedUsers.push({
                rowNumber: i,
                name: name,
                email: email,
                company: user.company || '',
                role: user.role || 'External',
                status: (user.status || 'active').toLowerCase(),
                password: user.password || '',
                requirePasswordReset: (user.requirepasswordreset || '').toLowerCase() === 'true'
            });
        }

        if (parsedUsers.length === 0) {
            this.showNotification('No valid users found in CSV file', 'warning');
            return;
        }

        // Compare with existing users and build preview
        await this.buildImportPreview(parsedUsers);
    }

    /**
     * Normalize CSV header names for user imports.
     * Handles common variations case-insensitively.
     */
    normalizeUserHeaderName(header) {
        const normalized = header.toLowerCase().replace(/\s*\(.*?\)\s*/g, '').replace(/[\s_-]/g, '');

        const headerMappings = {
            // Name variations
            'name': 'name',
            'username': 'name',
            'fullname': 'name',
            'displayname': 'name',

            // Email variations
            'email': 'email',
            'emailaddress': 'email',
            'useremail': 'email',

            // Company variations
            'company': 'company',
            'companyname': 'company',
            'companyaccess': 'company',
            'usercompany': 'company',
            'organization': 'company',

            // Role variations
            'role': 'role',
            'userrole': 'role',
            'rolename': 'role',

            // Status variations
            'status': 'status',
            'userstatus': 'status',
            'isactive': 'status',
            'active': 'status',

            // Password variations
            'password': 'password',
            'pass': 'password',
            'pwd': 'password',

            // Password reset variations
            'requirepasswordreset': 'requirepasswordreset',
            'resetpassword': 'requirepasswordreset',
            'forcereset': 'requirepasswordreset'
        };

        return headerMappings[normalized] || normalized;
    }

    async buildImportPreview(parsedUsers) {
        this.importPreviewData = [];

        // Track emails seen in this import for duplicate detection within file
        const emailsInImport = new Map(); // email -> rowNumber

        for (const importUser of parsedUsers) {
            // Match by email (primary identifier for users)
            const existingUser = this.users.find(u =>
                u.email?.toLowerCase() === importUser.email?.toLowerCase()
            );

            const previewRow = {
                rowNumber: importUser.rowNumber,
                data: importUser,
                existingData: existingUser || null,
                status: 'new',
                changes: [],
                errors: [],
                selected: true,
                action: 'create'
            };

            if (existingUser) {
                // Compare fields and determine status - using case-insensitive comparison where appropriate
                const changes = [];

                if ((existingUser.name || '').trim().toLowerCase() !== (importUser.name || '').trim().toLowerCase()) {
                    changes.push({ field: 'Name', oldValue: existingUser.name || '', newValue: importUser.name || '' });
                }
                if ((existingUser.role || '').trim() !== (importUser.role || '').trim()) {
                    changes.push({ field: 'Role', oldValue: existingUser.role || '', newValue: importUser.role || '' });
                }
                if ((existingUser.status || '').trim().toLowerCase() !== (importUser.status || '').trim().toLowerCase()) {
                    changes.push({ field: 'Status', oldValue: existingUser.status || '', newValue: importUser.status || '' });
                }
                // Compare company if provided
                if (importUser.company && importUser.company.trim()) {
                    const existingAccess = this.getCompanyAccessDisplay(existingUser);
                    const existingCompany = existingAccess === 'All'
                        ? 'All'
                        : (existingUser.company || existingUser.companyNames?.join(', ') || '');
                    if (existingCompany.toLowerCase() !== importUser.company.trim().toLowerCase()) {
                        changes.push({ field: 'User Company', oldValue: existingCompany, newValue: importUser.company });
                    }
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
            if (!importUser.name || !importUser.name.trim()) {
                previewRow.errors.push('Name is required');
            }
            if (!importUser.email || !importUser.email.trim()) {
                previewRow.errors.push('Email is required');
            } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(importUser.email.trim())) {
                previewRow.errors.push('Invalid email format');
            } else {
                // Check for duplicate email within this import file
                const emailLower = importUser.email.trim().toLowerCase();
                if (emailsInImport.has(emailLower)) {
                    const firstRow = emailsInImport.get(emailLower);
                    previewRow.errors.push(`Duplicate email in import file (first seen on row ${firstRow})`);
                } else {
                    emailsInImport.set(emailLower, importUser.rowNumber);
                }
            }
            // Validate role against dynamically loaded roles
            const normalizedRole = (importUser.role || '').trim();
            const validRoleNames = this.roles.map(r => r.name);
            if (normalizedRole && !validRoleNames.includes(normalizedRole)) {
                previewRow.errors.push(`Invalid role: "${normalizedRole}" (valid roles: ${validRoleNames.join(', ')})`);
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

            tr.style.backgroundColor = row.status === 'error' ? '#fff5f5' : (row.status === 'unchanged' ? '#fafafa' : '');

            // Build changes summary for modified rows
            let changesSummary = '';
            if (row.status === 'modified' && row.changes.length > 0) {
                changesSummary = `<div style="font-size: 10px; color: #666; margin-top: 4px;">${row.changes.map(c => c.field).join(', ')} changed</div>`;
            }

            tr.innerHTML = `
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    <input type="checkbox" class="import-row-checkbox"
                           data-index="${index}"
                           ${row.selected ? 'checked' : ''}
                           ${row.status === 'error' || row.status === 'unchanged' ? 'disabled' : ''}
                           onchange="userManagement.updateImportRowSelection(${index}, this.checked)">
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
                    ${this.escapeHtml(row.data.email || '')}
                </td>
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    ${this.renderFieldWithDiff(row, 'role')}
                </td>
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    ${this.renderFieldWithDiff(row, 'status')}
                    ${row.errors.length > 0 ? `<div style="color: #ff5252; font-size: 11px; margin-top: 4px; font-weight: 500;">${row.errors.join('<br>')}</div>` : ''}
                </td>
                <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                    ${this.renderActionDropdown(row, index)}
                </td>
            `;

            tbody.appendChild(tr);
        });

        // Update counts
        this.updateSelectedCount();

        // Show modal
        modal.style.setProperty('display', 'block', 'important');
    }

    renderFieldWithDiff(row, field) {
        const newValue = row.data[field] || '';

        if (row.status === 'modified' && row.existingData) {
            const oldValue = row.existingData[field] || '';
            // Check if this specific field changed (case-insensitive comparison)
            const fieldChanged = row.changes.some(c => c.field.toLowerCase() === field.toLowerCase());

            if (fieldChanged) {
                return `
                    <div class="field-conflict" style="min-width: 140px;">
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

        // For unchanged fields or new rows, just show the value
        return `<span style="word-break: break-word;">${this.escapeHtml(newValue)}</span>`;
    }

    renderActionDropdown(row, index) {
        if (row.status === 'error') {
            return '<span style="color: #999; font-size: 12px;">Cannot import</span>';
        }

        if (row.status === 'new') {
            return `
                <select class="import-action-select" data-index="${index}" onchange="userManagement.updateImportRowAction(${index}, this.value)" style="padding: 4px 8px; border: 1px solid #ddd; border-radius: 4px; font-size: 12px;">
                    <option value="create" ${row.action === 'create' ? 'selected' : ''}>Create</option>
                    <option value="skip" ${row.action === 'skip' ? 'selected' : ''}>Skip</option>
                </select>
            `;
        }

        if (row.status === 'modified') {
            return `
                <div style="display: flex; flex-direction: column; gap: 4px;">
                    <select class="import-action-select" data-index="${index}" onchange="userManagement.updateImportRowAction(${index}, this.value)" style="padding: 5px 8px; border: 2px solid #002B76; border-radius: 4px; font-size: 12px; font-weight: 500;">
                        <option value="update" ${row.action === 'update' ? 'selected' : ''}>Use Import</option>
                        <option value="keep" ${row.action === 'keep' ? 'selected' : ''}>Keep Current</option>
                        <option value="skip" ${row.action === 'skip' ? 'selected' : ''}>Skip Row</option>
                    </select>
                </div>
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

        // Update the row's visual state based on action
        const tr = document.querySelector(`tr[data-index="${index}"]`);
        if (tr) {
            // Update row background color
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

    toggleAllImportRows(checkbox) {
        const checked = checkbox.checked;
        this.importPreviewData.forEach((row, index) => {
            if (row.status !== 'error' && row.status !== 'unchanged') {
                row.selected = checked;
                row.action = checked ? (row.status === 'new' ? 'create' : 'update') : 'skip';

                const rowCheckbox = document.querySelector(`input.import-row-checkbox[data-index="${index}"]`);
                if (rowCheckbox) {
                    rowCheckbox.checked = checked;
                }
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

        const countContainer = document.querySelector('.import-selected-count');
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
            applyBtn.innerHTML = hasChanges
                ? `<i class="fas fa-check"></i> Apply Import (${toProcessCount})`
                : '<i class="fas fa-check"></i> Apply Import';
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

        const keptCount = this.importPreviewData.filter(r => r.action === 'keep').length;

        if (rowsToProcess.length === 0) {
            if (keptCount > 0) {
                this.showNotification(`No changes to apply. ${keptCount} row(s) keeping current values.`, 'info');
            } else {
                this.showNotification('No rows selected for import', 'warning');
            }
            return;
        }

        const token = this.getAntiForgeryToken();
        let successCount = 0;
        let errorCount = 0;
        const errors = [];

        this.showLoading(true, 'Importing users...');

        for (const row of rowsToProcess) {
            try {
                let response;
                // Resolve company name/ID from CSV to actual company IDs
                const companyIds = this.resolveCompanyIds(row.data.company);

                // DEF-186: never send a fallback password for bulk-imported users.
                // Server (AdminService.CreateUserAsync) generates a random one and
                // forces RequirePasswordReset=true when password is empty.
                const userData = {
                    name: row.data.name,
                    email: row.data.email,
                    role: row.data.role,
                    status: row.data.status,
                    password: '',
                    requirePasswordReset: true,
                    companyIds: companyIds
                };

                if (row.action === 'create') {
                    response = await fetch('/Admin/CreateUser', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': token
                        },
                        body: JSON.stringify(userData)
                    });
                } else if (row.action === 'update' && row.existingData) {
                    response = await fetch(`/Admin/UpdateUser/${row.existingData.id}`, {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': token
                        },
                        body: JSON.stringify(userData)
                    });
                }

                if (response) {
                    const result = await response.json();
                    if (result.success) {
                        successCount++;
                    } else {
                        errorCount++;
                        errors.push(`Row ${row.rowNumber} (${row.data.email}): ${result.message || 'Unknown error'}`);
                    }
                }
            } catch (error) {
                errorCount++;
                errors.push(`Row ${row.rowNumber} (${row.data.email}): ${error.message}`);
            }
        }

        this.showLoading(false);
        this.closeImportPreviewModal();
        await this.loadUsers();
        this.renderUsers();

        const keptMessage = keptCount > 0 ? `, ${keptCount} kept unchanged` : '';
        if (errorCount === 0) {
            this.showNotification(`Import complete: ${successCount} users processed successfully${keptMessage}`, 'success');
        } else if (successCount > 0) {
            const errorDetail = errors.slice(0, 3).join(' | ');
            this.showNotification(`Import partial: ${successCount} succeeded, ${errorCount} failed${keptMessage}. ${errorDetail}`, 'warning');
        } else {
            const errorDetail = errors.slice(0, 3).join(' | ');
            this.showNotification(`Import failed: ${errorCount} errors. ${errorDetail}`, 'error');
        }
    }

    parseCSVLine(line) {
        const result = [];
        let current = '';
        let inQuotes = false;

        for (let i = 0; i < line.length; i++) {
            const char = line[i];

            if (char === '"') {
                if (inQuotes && line[i + 1] === '"') {
                    current += '"';
                    i++;
                } else {
                    inQuotes = !inQuotes;
                }
            } else if (char === ',' && !inQuotes) {
                result.push(current);
                current = '';
            } else {
                current += char;
            }
        }
        result.push(current);
        return result;
    }
    
    resolveCompanyIds(companyValue) {
        if (!companyValue || !companyValue.trim()) return [];

        const trimmed = companyValue.trim();
        if (trimmed.toLowerCase() === 'all') {
            return this.companies.map(c => c.id);
        }

        // Support semicolon-separated multiple companies
        const parts = trimmed.split(';').map(p => p.trim()).filter(p => p);
        const ids = [];

        for (const part of parts) {
            // Try as numeric ID first
            const asNum = parseInt(part);
            if (!isNaN(asNum) && this.companies.some(c => c.id === asNum)) {
                ids.push(asNum);
                continue;
            }
            // Match by name (case-insensitive)
            const match = this.companies.find(c => c.name?.toLowerCase() === part.toLowerCase());
            if (match) {
                ids.push(match.id);
            }
        }
        return ids;
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
        // Sort companies alphabetically by name
        const sorted = [...this.companies].sort((a, b) =>
            (a.name || '').localeCompare(b.name || '')
        );

        // Update filter dropdown
        const companyFilter = document.getElementById('company-filter');
        if (companyFilter) {
            const firstOption = companyFilter.children[0];
            companyFilter.innerHTML = '';
            companyFilter.appendChild(firstOption);

            sorted.forEach(company => {
                const option = document.createElement('option');
                option.value = company.id;
                option.textContent = company.name;
                companyFilter.appendChild(option);
            });
        }

        // Update modal company checkboxes
        const companiesContainer = document.getElementById('user-companies-container');
        if (companiesContainer) {
            companiesContainer.innerHTML = '';

            // Add "Check All" option at top
            const checkAllLabel = document.createElement('label');
            checkAllLabel.className = 'admin-checkbox-item';
            checkAllLabel.style.cssText = 'width: 100%; border-bottom: 1px solid #e0e0e0; padding-bottom: 6px; margin-bottom: 4px; font-weight: 600;';

            const checkAllBox = document.createElement('input');
            checkAllBox.type = 'checkbox';
            checkAllBox.id = 'company-check-all';

            const checkAllSpan = document.createElement('span');
            checkAllSpan.textContent = 'Check All';
            checkAllSpan.style.fontWeight = '600';

            checkAllLabel.appendChild(checkAllBox);
            checkAllLabel.appendChild(checkAllSpan);
            companiesContainer.appendChild(checkAllLabel);

            // Toggle all company checkboxes when Check All is clicked
            checkAllBox.addEventListener('change', () => {
                const boxes = companiesContainer.querySelectorAll('input[name="user-companies"]');
                boxes.forEach(cb => cb.checked = checkAllBox.checked);
            });

            // Add individual company checkboxes (alphabetical)
            sorted.forEach(company => {
                const label = document.createElement('label');
                label.className = 'admin-checkbox-item';

                const checkbox = document.createElement('input');
                checkbox.type = 'checkbox';
                checkbox.name = 'user-companies';
                checkbox.value = company.id;
                checkbox.id = `company-${company.id}`;

                // Update Check All state when individual boxes change
                checkbox.addEventListener('change', () => {
                    const boxes = companiesContainer.querySelectorAll('input[name="user-companies"]');
                    const allChecked = Array.from(boxes).every(cb => cb.checked);
                    document.getElementById('company-check-all').checked = allChecked;
                });

                const span = document.createElement('span');
                span.textContent = company.name;

                label.appendChild(checkbox);
                label.appendChild(span);
                companiesContainer.appendChild(label);
            });
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    // 2FA Management Methods
    toggleActionsDropdown(userId) {
        console.log('Toggle actions dropdown for user:', userId);
        const dropdown = document.getElementById(`actions-dropdown-${userId}`);

        if (!dropdown) {
            console.error('Actions dropdown not found for user:', userId);
            return;
        }

        const allDropdowns = document.querySelectorAll('.admin-dropdown-menu');
        const allRows = document.querySelectorAll('.admin-table-row');

        // Close all other dropdowns
        allDropdowns.forEach(menu => {
            if (menu !== dropdown) {
                menu.classList.remove('admin-dropdown-menu--show');
            }
        });

        // Remove elevated class from all rows
        allRows.forEach(row => row.classList.remove('dropdown-open'));

        // Toggle current dropdown
        dropdown.classList.toggle('admin-dropdown-menu--show');

        // Add elevated class to current row if dropdown is open
        if (dropdown.classList.contains('admin-dropdown-menu--show')) {
            const currentRow = dropdown.closest('.admin-table-row');
            if (currentRow) {
                currentRow.classList.add('dropdown-open');
            }
        }
    }

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
            this.closeAllDropdowns();
        }
    }

    closeAllDropdowns() {
        document.querySelectorAll('.admin-dropdown-menu').forEach(menu => {
            menu.classList.remove('admin-dropdown-menu--show');
        });
        // Remove elevated class from all rows
        document.querySelectorAll('.admin-table-row').forEach(row => {
            row.classList.remove('dropdown-open');
        });
    }

    async view2FAStatus(userId) {
        // Close dropdowns first
        this.closeAllDropdowns();
        try {
            const user = this.users.find(u => u.id === userId);
            if (!user) {
                this.showNotification('User not found', 'error');
                return;
            }

            // Format 2FA status as plain text
            const statusIcon = user.twoFactorEnabled ? '✓' : '✗';
            const statusText = user.twoFactorEnabled ? 'Enabled' : 'Disabled';
            const statusInfo = `User: ${user.name}
Email: ${user.email}
Current Status: ${statusIcon} ${statusText}
Method: ${user.twoFactorMethod || 'None'}
Last Updated: ${user.twoFactorLastUpdated || 'Never'}`;

            await AdminCore.showConfirmation({
                title: '2FA Status',
                message: statusInfo,
                confirmText: 'OK',
                showCancel: false,
                type: 'info'
            });

        } catch (error) {
            console.error('Error viewing 2FA status:', error);
            this.showNotification('Failed to load 2FA status', 'error');
        }
    }
    
    async reset2FA(userId) {
        // Close dropdowns first
        this.closeAllDropdowns();

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
        // Close dropdowns first
        this.closeAllDropdowns();

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
        // Close dropdowns first
        this.closeAllDropdowns();

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