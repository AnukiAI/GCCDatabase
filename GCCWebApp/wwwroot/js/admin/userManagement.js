// Users data loaded from database
let users = [];

// Company and Partner name lookups (will be populated from dropdowns)
const companyNames = {};
const partnerNames = {};

// Function to load users from database
async function loadUsers() {
    try {
        const response = await fetch('/Admin/GetUsers');
        const result = await response.json();

        if (result.success && result.data) {
            // Map database model to UI model
            users = result.data.map(user => ({
                id: user.id,
                name: user.name,
                company: user.companyNames || [],
                partners: user.partnerNames || [],
                email: user.email,
                role: user.role.toLowerCase(),
                status: user.status.toLowerCase(),
                lastLogin: user.lastLogin,
                requirePasswordReset: user.requirePasswordReset || false
            }));
            applyFiltersAndSort();
        } else {
            console.error('Failed to load users:', result.message);
        }
    } catch (error) {
        console.error('Error loading users:', error);
    }
}

// DOM Elements
const userTable = document.getElementById('user-table');
const userTableBody = userTable.querySelector('tbody');
const userCountElement = document.getElementById('user-count');
const searchInput = document.getElementById('search-input');
const statusFilter = document.getElementById('status-filter');
const roleFilter = document.getElementById('role-filter');
const companyFilter = document.getElementById('company-filter');
const addUserBtn = document.getElementById('add-user-btn');
const importBtn = document.getElementById('import-btn');
const exportBtn = document.getElementById('export-btn');

const modal = document.getElementById('user-modal');
const modalOverlay = document.querySelector('.modal-overlay');
const modalTitle = document.getElementById('modal-title');
const modalClose = document.querySelector('.modal-close');
const cancelBtn = document.getElementById('cancel-btn');
const userForm = document.getElementById('user-form');
const userIdInput = document.getElementById('user-id');
const userNameInput = document.getElementById('user-name');
const userEmailInput = document.getElementById('user-email');
const userRoleInput = document.getElementById('user-role');
const userStatusInput = document.getElementById('user-status');
const userPasswordInput = document.getElementById('user-password');
const requirePasswordResetInput = document.getElementById('require-password-reset');

// Multi-select elements
const companyDisplay = document.getElementById('company-display');
const companyDropdown = document.getElementById('company-dropdown');
const partnersDisplay = document.getElementById('partners-display');
const partnersDropdown = document.getElementById('partners-dropdown');

// Role-based field elements
const companyGroup = document.getElementById('company-group');
const partnerGroup = document.getElementById('partner-group');
const userPartnerSelect = document.getElementById('user-partner');

// Sorting variables
let sortColumn = 'name';
let sortDirection = 'asc';

// Multi-select functionality
function initializeMultiSelect() {
    // Company multi-select
    companyDisplay.addEventListener('click', () => {
        const isVisible = companyDropdown.classList.contains('active');
        companyDropdown.classList.toggle('active', !isVisible);
        partnersDropdown.classList.remove('active');
    });

    // Partners multi-select
    partnersDisplay.addEventListener('click', () => {
        const isVisible = partnersDropdown.classList.contains('active');
        partnersDropdown.classList.toggle('active', !isVisible);
        companyDropdown.classList.remove('active');
    });

    // Close dropdowns when clicking outside
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.multi-select-container')) {
            companyDropdown.classList.remove('active');
            partnersDropdown.classList.remove('active');
        }
    });

    // Handle checkbox changes
    const companyCheckboxes = companyDropdown.querySelectorAll('input[type="checkbox"]');
    const partnerCheckboxes = partnersDropdown.querySelectorAll('input[type="checkbox"]');

    companyCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', updateCompanyDisplay);
    });

    partnerCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', updatePartnersDisplay);
    });
}

function updateCompanyDisplay() {
    const selected = Array.from(companyDropdown.querySelectorAll('input[type="checkbox"]:checked'))
        .map(cb => cb.value);

    if (selected.length === 0) {
        companyDisplay.innerHTML = '<span style="color: #999;">Select companies...</span>';
    } else {
        companyDisplay.innerHTML = selected.map(value =>
            `<span class="multi-select-tag">
                    ${companyNames[value]}
                    <span class="remove" onclick="removeCompany('${value}')">&times;</span>
                </span>`
        ).join('');
    }
}

function updatePartnersDisplay() {
    const selected = Array.from(partnersDropdown.querySelectorAll('input[type="checkbox"]:checked'))
        .map(cb => cb.value);

    if (selected.length === 0) {
        partnersDisplay.innerHTML = '<span style="color: #999;">Select partners...</span>';
    } else {
        partnersDisplay.innerHTML = selected.map(value =>
            `<span class="multi-select-tag">
                    ${partnerNames[value]}
                    <span class="remove" onclick="removePartner('${value}')">&times;</span>
                </span>`
        ).join('');
    }
}

function removeCompany(value) {
    const checkbox = companyDropdown.querySelector(`input[value="${value}"]`);
    if (checkbox) {
        checkbox.checked = false;
        updateCompanyDisplay();
    }
}

function removePartner(value) {
    const checkbox = partnersDropdown.querySelector(`input[value="${value}"]`);
    if (checkbox) {
        checkbox.checked = false;
        updatePartnersDisplay();
    }
}

// ============================================================
// Role-Based Field Visibility (per Design Recommendations)
// - Partner role: Show Partner dropdown, hide Companies
// - External role: Show Companies, hide Partner
// - Admin/Analyst/Other: Hide both (full access)
// ============================================================

// Load partners into the single-select Partner dropdown
async function loadPartnersDropdown() {
    if (!userPartnerSelect) return;

    try {
        const response = await fetch('/Admin/GetPartners');
        const result = await response.json();

        if (result.success && result.data) {
            // Clear existing options except the first placeholder
            userPartnerSelect.innerHTML = '<option value="">Select a partner...</option>';

            // Add active partners
            result.data
                .filter(p => p.isActive)
                .forEach(partner => {
                    const option = document.createElement('option');
                    option.value = partner.id;
                    option.textContent = partner.name;
                    userPartnerSelect.appendChild(option);
                });
        }
    } catch (error) {
        console.error('Error loading partners for dropdown:', error);
    }
}

// Handle role change - show/hide appropriate fields
function handleRoleChange(role) {
    const normalizedRole = (role || '').toLowerCase();

    if (!companyGroup || !partnerGroup) {
        console.warn('Role-based field groups not found');
        return;
    }

    // Reset both groups first
    companyGroup.style.display = 'none';
    partnerGroup.style.display = 'none';

    switch (normalizedRole) {
        case 'partner':
            // Partner role: Show Partner dropdown only
            partnerGroup.style.display = 'block';
            // Clear company selections
            if (companyDropdown) {
                companyDropdown.querySelectorAll('input[type="checkbox"]').forEach(cb => cb.checked = false);
                updateCompanyDisplay();
            }
            break;

        case 'external':
            // External role: Show Companies only
            companyGroup.style.display = 'block';
            // Clear partner selection
            if (userPartnerSelect) {
                userPartnerSelect.value = '';
            }
            break;

        case 'admin':
        case 'analyst':
        case 'api integration':
        case 'public':
        default:
            // These roles have full access or no data filtering
            // Hide both association fields, clear selections
            if (companyDropdown) {
                companyDropdown.querySelectorAll('input[type="checkbox"]').forEach(cb => cb.checked = false);
                updateCompanyDisplay();
            }
            if (userPartnerSelect) {
                userPartnerSelect.value = '';
            }
            break;
    }
}

// Validate role-based associations before save
function validateRoleAssociations(role, selectedCompanies, selectedPartnerId) {
    const normalizedRole = (role || '').toLowerCase();

    if (normalizedRole === 'partner') {
        if (!selectedPartnerId) {
            return { valid: false, message: 'Partner role requires a partner selection.' };
        }
    } else if (normalizedRole === 'external') {
        if (!selectedCompanies || selectedCompanies.length === 0) {
            return { valid: false, message: 'External role requires at least one company selection.' };
        }
    }

    return { valid: true };
}

// Function to format date
function formatDate(dateStr) {
    const date = new Date(dateStr);
    return date.toLocaleString('en-US', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hour12: true
    });
}

// Function to format company/partner lists for display
function formatList(array, mapping) {
    if (!array || array.length === 0) return '';
    // If items are already full names (not slugs), just join them
    // Otherwise use the mapping
    return array.map(item => {
        // If mapping exists and has the item, use it; otherwise return item as-is
        return (mapping && mapping[item]) ? mapping[item] : item;
    }).join(', ');
}

// Function to populate the table
function populateTable(data) {
    userTableBody.innerHTML = '';
    let visibleCount = 0;

    data.forEach(user => {
        const row = document.createElement('tr');
        row.dataset.id = user.id;

        // Name cell
        const nameCell = document.createElement('td');
        nameCell.dataset.field = 'name';
        nameCell.textContent = user.name;
        nameCell.addEventListener('dblclick', () => makeEditable(nameCell, 'text'));
        row.appendChild(nameCell);

        // Company cell
        const companyCell = document.createElement('td');
        companyCell.dataset.field = 'company';
        companyCell.textContent = formatList(user.company, companyNames);
        row.appendChild(companyCell);

        // Email cell
        const emailCell = document.createElement('td');
        emailCell.dataset.field = 'email';
        emailCell.textContent = user.email;
        emailCell.addEventListener('dblclick', () => makeEditable(emailCell, 'email'));
        row.appendChild(emailCell);

        // Role cell
        const roleCell = document.createElement('td');
        roleCell.dataset.field = 'role';
        roleCell.textContent = capitalizeFirstLetter(user.role.replace('-', ' '));
        roleCell.addEventListener('dblclick', () => makeEditable(roleCell, 'select-role'));
        row.appendChild(roleCell);

        // Status cell
        const statusCell = document.createElement('td');
        statusCell.dataset.field = 'status';
        const statusIndicator = document.createElement('span');
        statusIndicator.className = `status-indicator status-${user.status}`;
        statusCell.appendChild(statusIndicator);
        const statusText = document.createTextNode(capitalizeFirstLetter(user.status));
        statusCell.appendChild(statusText);
        statusCell.addEventListener('dblclick', () => makeEditable(statusCell, 'select-status'));
        row.appendChild(statusCell);

        // Last Login cell
        const lastLoginCell = document.createElement('td');
        lastLoginCell.dataset.field = 'lastLogin';
        lastLoginCell.textContent = formatDate(user.lastLogin);
        row.appendChild(lastLoginCell);

        // 2FA Status cell
        const twoFACell = document.createElement('td');
        twoFACell.dataset.field = 'twoFactorEnabled';
        twoFACell.style.textAlign = 'center';
        if (user.twoFactorEnabled) {
            const badge = document.createElement('span');
            badge.className = 'badge-2fa badge-2fa-enabled';
            const method = (user.twoFactorMethod || 'Email').toUpperCase();
            badge.innerHTML = '<i class="fas fa-shield-alt"></i> ' + method;
            badge.title = '2FA enabled via ' + method;
            twoFACell.appendChild(badge);
        } else {
            const badge = document.createElement('span');
            badge.className = 'badge-2fa badge-2fa-disabled';
            badge.textContent = 'Off';
            badge.title = '2FA not enabled';
            twoFACell.appendChild(badge);
        }
        row.appendChild(twoFACell);

        // Actions cell
        const actionsCell = document.createElement('td');
        actionsCell.style.whiteSpace = 'nowrap';
        actionsCell.style.textAlign = 'center';

        // Edit button
        const editButton = document.createElement('button');
        editButton.className = 'btn-edit';
        editButton.innerHTML = '<i class="fas fa-edit"></i>';
        editButton.title = 'Edit';
        editButton.setAttribute('aria-label', `Edit user ${user.name}`);
        editButton.addEventListener('click', () => openEditModal(user));
        actionsCell.appendChild(editButton);

        // Delete button
        const deleteButton = document.createElement('button');
        deleteButton.className = 'btn-delete';
        deleteButton.innerHTML = '<i class="fas fa-trash"></i>';
        deleteButton.title = 'Delete';
        deleteButton.setAttribute('aria-label', `Delete user ${user.name}`);
        deleteButton.addEventListener('click', () => deleteUser(user.id));
        actionsCell.appendChild(deleteButton);

        // 2FA action dropdown
        const twoFADropdown = document.createElement('div');
        twoFADropdown.className = 'dropdown-2fa';
        twoFADropdown.style.display = 'inline-block';
        twoFADropdown.style.position = 'relative';

        const twoFABtn = document.createElement('button');
        twoFABtn.className = 'btn-2fa';
        twoFABtn.innerHTML = '<i class="fas fa-shield-alt"></i>';
        twoFABtn.title = '2FA Actions';
        twoFABtn.setAttribute('aria-label', `2FA actions for ${user.name}`);
        twoFABtn.addEventListener('click', (e) => {
            e.stopPropagation();
            show2FAMenu(user, twoFADropdown);
        });
        twoFADropdown.appendChild(twoFABtn);
        actionsCell.appendChild(twoFADropdown);

        row.appendChild(actionsCell);
        userTableBody.appendChild(row);
        visibleCount++;
    });

    // Update the user count
    const countText = `Showing ${visibleCount} of ${users.length} users`;
    userCountElement.textContent = countText;

    // Announce table updates to screen readers
    const announcement = document.getElementById('tableAnnouncement');
    if (announcement) {
        announcement.textContent = `Table updated. ${countText}.`;
    }
}

// Function to make a cell editable
function makeEditable(cell, type) {
    const row = cell.parentElement;
    const userId = parseInt(row.dataset.id);
    const field = cell.dataset.field;
    const user = users.find(u => u.id === userId);

    if (!user) return;

    const currentValue = user[field];
    cell.classList.add('edit-mode');

    // Save the original content to restore if cancelled
    const originalContent = cell.innerHTML;

    // Clear the cell
    cell.innerHTML = '';

    let input;

    if (type === 'text' || type === 'email') {
        // Create input element
        input = document.createElement('input');
        input.type = type;
        input.className = 'text-input';
        input.value = currentValue;
        cell.appendChild(input);
        input.focus();
    } else if (type === 'select-role') {
        // Create select for role
        input = document.createElement('select');
        input.className = 'text-input';

        const options = ['admin', 'gcc-user', 'external-user'];
        options.forEach(option => {
            const optElement = document.createElement('option');
            optElement.value = option;
            optElement.text = capitalizeFirstLetter(option.replace('-', ' '));
            if (option === currentValue) {
                optElement.selected = true;
            }
            input.appendChild(optElement);
        });

        cell.appendChild(input);
        input.focus();
    } else if (type === 'select-status') {
        // Create select for status
        input = document.createElement('select');
        input.className = 'text-input';

        const options = ['active', 'inactive'];
        options.forEach(option => {
            const optElement = document.createElement('option');
            optElement.value = option;
            optElement.text = capitalizeFirstLetter(option);
            if (option === currentValue) {
                optElement.selected = true;
            }
            input.appendChild(optElement);
        });

        cell.appendChild(input);
        input.focus();
    }

    // Function to save the edit
    const saveEdit = () => {
        const newValue = input.value.trim();

        if (newValue !== '' && newValue !== currentValue) {
            // Update the user object
            user[field] = newValue;

            // Update the cell
            cell.classList.remove('edit-mode');

            // Refresh the table
            applyFiltersAndSort();
        } else {
            // No change or empty value, restore original
            cell.innerHTML = originalContent;
            cell.classList.remove('edit-mode');
        }

        // Remove event listeners
        document.removeEventListener('click', handleClickOutside);
    };

    // Function to handle click outside
    const handleClickOutside = (event) => {
        if (event.target !== input) {
            saveEdit();
        }
    };

    // Add event listeners
    input.addEventListener('blur', saveEdit);
    input.addEventListener('keydown', (event) => {
        if (event.key === 'Enter') {
            saveEdit();
        } else if (event.key === 'Escape') {
            cell.innerHTML = originalContent;
            cell.classList.remove('edit-mode');
            document.removeEventListener('click', handleClickOutside);
        }
    });

    // Delay adding click outside listener to prevent immediate triggering
    setTimeout(() => {
        document.addEventListener('click', handleClickOutside);
    }, 100);
}

// Helper function to capitalize first letter
function capitalizeFirstLetter(string) {
    return string.charAt(0).toUpperCase() + string.slice(1);
}

// Function to initialize the table sortable headers
function initializeSortableHeaders() {
    const headers = userTable.querySelectorAll('th[data-sort]');

    headers.forEach(header => {
        header.addEventListener('click', () => {
            const column = header.getAttribute('data-sort');

            // Toggle sort direction if clicking the same column
            if (sortColumn === column) {
                sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
            } else {
                sortColumn = column;
                sortDirection = 'asc';
            }

            // Update header styling and ARIA attributes
            headers.forEach(h => {
                h.classList.remove('active-sort');
                h.setAttribute('aria-sort', 'none');
                const icon = h.querySelector('.sort-icon');
                if (icon) {
                    icon.textContent = '▼';
                    icon.style.opacity = '0.6';
                }
            });

            header.classList.add('active-sort');
            header.setAttribute('aria-sort', sortDirection === 'asc' ? 'ascending' : 'descending');
            const sortIcon = header.querySelector('.sort-icon');
            if (sortIcon) {
                sortIcon.textContent = sortDirection === 'asc' ? '▼' : '▲';
                sortIcon.style.opacity = '1';
            }

            // Announce sort change to screen readers
            if (window.AdminCore && window.AdminCore.announceToScreenReader) {
                AdminCore.announceToScreenReader(`Table sorted by ${header.textContent.trim()}, ${sortDirection === 'asc' ? 'ascending' : 'descending'}`);
            }

            // Apply filters and sort
            applyFiltersAndSort();
        });
    });
}

// Function to apply filters and sort
function applyFiltersAndSort() {
    // Apply filters
    const filteredData = getFilteredData();

    // Sort the data
    filteredData.sort((a, b) => {
        let valueA, valueB;

        if (sortColumn === 'company') {
            valueA = formatList(a.company, companyNames).toLowerCase();
            valueB = formatList(b.company, companyNames).toLowerCase();
        } else if (sortColumn === 'lastLogin') {
            valueA = new Date(a.lastLogin).getTime();
            valueB = new Date(b.lastLogin).getTime();

            if (sortDirection === 'asc') {
                return valueA - valueB;
            } else {
                return valueB - valueA;
            }
        } else {
            valueA = a[sortColumn] ? a[sortColumn].toString().toLowerCase() : '';
            valueB = b[sortColumn] ? b[sortColumn].toString().toLowerCase() : '';
        }

        if (sortDirection === 'asc') {
            return valueA.localeCompare(valueB);
        } else {
            return valueB.localeCompare(valueA);
        }
    });

    populateTable(filteredData);
}

// Function to filter the data based on search input and filters
function getFilteredData() {
    const searchTerm = searchInput.value.toLowerCase();
    const statusValue = statusFilter.value;
    const roleValue = roleFilter.value;
    const companyValue = companyFilter.value;

    return users.filter(user => {
        // Search term filter
        const matchesSearch =
            searchTerm === '' ||
            user.name.toLowerCase().includes(searchTerm) ||
            user.email.toLowerCase().includes(searchTerm) ||
            user.role.toLowerCase().includes(searchTerm) ||
            user.status.toLowerCase().includes(searchTerm) ||
            formatList(user.company, companyNames).toLowerCase().includes(searchTerm) ||
            formatList(user.partners, partnerNames).toLowerCase().includes(searchTerm);

        // Status filter
        const matchesStatus =
            statusValue === 'all' ||
            user.status === statusValue;

        // Role filter
        const matchesRole =
            roleValue === 'all' ||
            user.role === roleValue;

        // Company filter
        const matchesCompany =
            companyValue === 'all' ||
            (user.company && user.company.includes(companyValue));

        return matchesSearch && matchesStatus && matchesRole && matchesCompany;
    });
}

// Function to open the modal for adding a new user
function openAddModal() {
    modalTitle.textContent = 'Add New User';
    userForm.reset();
    userIdInput.value = '';
    userPasswordInput.required = true;
    requirePasswordResetInput.checked = true;

    // Reset multi-selects
    companyDropdown.querySelectorAll('input[type="checkbox"]').forEach(cb => cb.checked = false);
    partnersDropdown.querySelectorAll('input[type="checkbox"]').forEach(cb => cb.checked = false);
    updateCompanyDisplay();
    updatePartnersDisplay();

    // Reset single partner dropdown
    if (userPartnerSelect) {
        userPartnerSelect.value = '';
    }

    // Apply role-based field visibility (default role)
    handleRoleChange(userRoleInput.value);

    modal.style.display = 'flex';
    modal.classList.add('active');
}

// Function to open the modal for editing a user
function openEditModal(user) {
    modalTitle.textContent = 'Edit User';
    userIdInput.value = user.id;
    userNameInput.value = user.name;
    userEmailInput.value = user.email;
    userRoleInput.value = user.role;
    userStatusInput.value = user.status;
    userPasswordInput.value = '';
    userPasswordInput.required = false;
    requirePasswordResetInput.checked = user.requirePasswordReset || false;

    // Set company checkboxes
    companyDropdown.querySelectorAll('input[type="checkbox"]').forEach(cb => {
        cb.checked = user.company && user.company.includes(cb.value);
    });
    updateCompanyDisplay();

    // Set partner checkboxes (multi-select)
    partnersDropdown.querySelectorAll('input[type="checkbox"]').forEach(cb => {
        cb.checked = user.partners && user.partners.includes(cb.value);
    });
    updatePartnersDisplay();

    // Set single partner dropdown for Partner role users
    if (userPartnerSelect && user.partners && user.partners.length > 0) {
        // Try to find matching partner option by name
        const partnerName = user.partners[0];
        const options = userPartnerSelect.querySelectorAll('option');
        let found = false;
        options.forEach(opt => {
            if (opt.textContent === partnerName) {
                userPartnerSelect.value = opt.value;
                found = true;
            }
        });
        if (!found) {
            userPartnerSelect.value = '';
        }
    }

    // Apply role-based field visibility
    handleRoleChange(user.role);

    modal.style.display = 'flex';
    modal.classList.add('active');
}

// Function to close the modal
function closeModal() {
    modal.style.display = 'none';
    modal.classList.remove('active');
    companyDropdown.classList.remove('active');
    partnersDropdown.classList.remove('active');
}

// Function to save a user (add or update)
async function saveUser(event) {
    event.preventDefault();

    const userId = userIdInput.value;
    const role = userRoleInput.value;
    const normalizedRole = (role || '').toLowerCase();

    // Get selected companies (for External role)
    const selectedCompanies = Array.from(companyDropdown.querySelectorAll('input[type="checkbox"]:checked'))
        .map(cb => cb.value);

    // Get selected partner ID based on role
    let selectedPartnerIds = [];
    if (normalizedRole === 'partner' && userPartnerSelect) {
        // For Partner role, use single-select dropdown
        const partnerId = userPartnerSelect.value;
        if (partnerId) {
            selectedPartnerIds = [parseInt(partnerId, 10)];
        }
    } else {
        // For other roles, use multi-select checkboxes (legacy behavior)
        selectedPartnerIds = Array.from(partnersDropdown.querySelectorAll('input[type="checkbox"]:checked'))
            .map(cb => parseInt(cb.value, 10));
    }

    // Validate role-based associations
    const validation = validateRoleAssociations(role, selectedCompanies, selectedPartnerIds[0] || null);
    if (!validation.valid) {
        await AdminCore.showWarningModal({
            title: 'Validation Error',
            message: validation.message
        });
        return;
    }

    const userData = {
        name: userNameInput.value,
        companyIds: selectedCompanies.map(c => parseInt(c, 10)),
        partnerIds: selectedPartnerIds,
        email: userEmailInput.value,
        role: role,
        status: userStatusInput.value,
        requirePasswordReset: requirePasswordResetInput.checked
    };

    if (userPasswordInput.value) {
        userData.password = userPasswordInput.value;
    }

    try {
        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        if (userId) {
            // Update existing user
            const response = await fetch(`/Admin/UpdateUser?id=${userId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                },
                body: JSON.stringify(userData)
            });

            const result = await response.json();

            if (result.success) {
                // Update local array
                const index = users.findIndex(u => u.id.toString() === userId);
                if (index !== -1) {
                    // Keep the lastLogin
                    userData.lastLogin = users[index].lastLogin;
                    users[index] = {
                        ...users[index],
                        ...userData,
                        id: parseInt(userId)
                    };
                }
                closeModal();
                applyFiltersAndSort();
                await AdminCore.showSuccessModal({
                    title: 'Update Successful',
                    message: 'User updated successfully.'
                });
            } else {
                await AdminCore.showErrorModal({
                    title: 'Update Failed',
                    message: result.message || 'Failed to update user'
                });
            }
        } else {
            // Create new user
            const response = await fetch('/Admin/CreateUser', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                },
                body: JSON.stringify(userData)
            });

            const result = await response.json();

            if (result.success) {
                // Reload users from database to get the new user with correct ID
                await loadUsers();
                closeModal();
                applyFiltersAndSort();
                await AdminCore.showSuccessModal({
                    title: 'User Created',
                    message: 'User created successfully.'
                });
            } else {
                await AdminCore.showErrorModal({
                    title: 'Creation Failed',
                    message: result.message || 'Failed to create user'
                });
            }
        }
    } catch (error) {
        console.error('Error saving user:', error);
        await AdminCore.showErrorModal({
            title: 'Error',
            message: 'Error saving user: ' + error.message
        });
    }
}

// Function to delete a user
async function deleteUser(id) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Delete Confirmation',
        message: 'Are you sure you want to delete this user? This action cannot be undone.',
        confirmText: 'Delete',
        cancelText: 'Cancel',
        type: 'danger'
    });

    if (!confirmed) return;

    try {
        // Delete from database
        const response = await fetch(`/Admin/DeleteUser?id=${id}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const result = await response.json();

        if (result.success) {
            // Remove from local array only if database delete succeeded
            users = users.filter(user => user.id !== id);
            applyFiltersAndSort();
            await AdminCore.showSuccessModal({
                title: 'Delete Successful',
                message: 'User deleted successfully.'
            });
        } else {
            await AdminCore.showErrorModal({
                title: 'Delete Failed',
                message: result.message || 'Failed to delete user'
            });
        }
    } catch (error) {
        console.error('Error deleting user:', error);
        await AdminCore.showErrorModal({
            title: 'Error',
            message: 'Error deleting user: ' + error.message
        });
    }
}

// ===== 2FA Management Functions =====

function show2FAMenu(user, container) {
    // Close any existing menus
    document.querySelectorAll('.dropdown-2fa-menu').forEach(m => m.remove());

    const menu = document.createElement('div');
    menu.className = 'dropdown-2fa-menu';

    const currentUserId = parseInt(document.getElementById('currentUserId')?.value || '0');
    const isSelf = user.id === currentUserId;

    if (user.twoFactorEnabled) {
        menu.appendChild(create2FAMenuItem('Reset 2FA', 'fas fa-undo', () => reset2FA(user), isSelf));
        menu.appendChild(create2FAMenuItem('Disable 2FA', 'fas fa-times-circle', () => disable2FA(user), isSelf));
    } else {
        menu.appendChild(create2FAMenuItem('Enable 2FA (Email)', 'fas fa-envelope', () => enable2FA(user, 'Email'), isSelf));
        menu.appendChild(create2FAMenuItem('Enable 2FA (TOTP)', 'fas fa-mobile-alt', () => enable2FA(user, 'TOTP'), isSelf));
    }

    container.appendChild(menu);

    // Close on outside click
    setTimeout(() => {
        document.addEventListener('click', function closeMenu() {
            menu.remove();
            document.removeEventListener('click', closeMenu);
        }, { once: true });
    }, 0);
}

function create2FAMenuItem(text, iconClass, onClick, disabled) {
    const btn = document.createElement('button');
    btn.className = 'dropdown-2fa-item';
    btn.innerHTML = `<i class="${iconClass}"></i> ${text}`;
    if (disabled) {
        btn.disabled = true;
        btn.title = 'Cannot modify your own 2FA from admin panel';
    } else {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            document.querySelectorAll('.dropdown-2fa-menu').forEach(m => m.remove());
            onClick();
        });
    }
    return btn;
}

async function reset2FA(user) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Reset 2FA',
        message: `Are you sure you want to reset 2FA for ${user.name}? This will clear their authenticator setup and recovery codes. They will need to set up 2FA again.`,
        confirmText: 'Reset',
        cancelText: 'Cancel',
        type: 'warning'
    });
    if (!confirmed) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    try {
        const response = await fetch('/Admin/Reset2FA', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' },
            body: JSON.stringify({ userId: user.id })
        });
        const result = await response.json();
        if (result.success) {
            user.twoFactorEnabled = false;
            user.twoFactorMethod = null;
            applyFiltersAndSort();
            AdminCore.showNotification('2FA has been reset successfully.', 'success');
        } else {
            AdminCore.showNotification(result.message || 'Failed to reset 2FA.', 'error');
        }
    } catch (err) {
        AdminCore.showNotification('Error resetting 2FA.', 'error');
    }
}

async function disable2FA(user) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Disable 2FA',
        message: `Are you sure you want to disable 2FA for ${user.name}?`,
        confirmText: 'Disable',
        cancelText: 'Cancel',
        type: 'warning'
    });
    if (!confirmed) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    try {
        const response = await fetch('/Admin/Disable2FA', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' },
            body: JSON.stringify({ userId: user.id })
        });
        const result = await response.json();
        if (result.success) {
            user.twoFactorEnabled = false;
            applyFiltersAndSort();
            AdminCore.showNotification('2FA has been disabled successfully.', 'success');
        } else {
            AdminCore.showNotification(result.message || 'Failed to disable 2FA.', 'error');
        }
    } catch (err) {
        AdminCore.showNotification('Error disabling 2FA.', 'error');
    }
}

async function enable2FA(user, method) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Enable 2FA',
        message: `Enable ${method} two-factor authentication for ${user.name}?`,
        confirmText: 'Enable',
        cancelText: 'Cancel',
        type: 'info'
    });
    if (!confirmed) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    try {
        const response = await fetch('/Admin/Enable2FA', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' },
            body: JSON.stringify({ userId: user.id, twoFactorMethod: method })
        });
        const result = await response.json();
        if (result.success) {
            user.twoFactorEnabled = true;
            user.twoFactorMethod = method;
            applyFiltersAndSort();
            AdminCore.showNotification(`2FA (${method}) has been enabled successfully.`, 'success');
        } else {
            AdminCore.showNotification(result.message || 'Failed to enable 2FA.', 'error');
        }
    } catch (err) {
        AdminCore.showNotification('Error enabling 2FA.', 'error');
    }
}

// Function to handle CSV import
async function importCSV() {
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.accept = '.csv';

    fileInput.addEventListener('change', async (event) => {
        const file = event.target.files[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = async (e) => {
            const contents = e.target.result;
            const lines = contents.split('\n');

            // Skip header row and process data
            const usersToImport = [];

            for (let i = 1; i < lines.length; i++) {
                const line = lines[i].trim();
                if (!line) continue;

                // Parse CSV properly handling quoted fields
                const fields = parseCSVLine(line);
                if (fields.length < 5) continue;  // Must have at least name, company, email, role, status

                const name = fields[0]?.trim();
                if (!name) continue;

                usersToImport.push({
                    name: name,
                    company: fields[1] ? fields[1].trim().split(';').filter(c => c) : [],
                    partners: fields[2] ? fields[2].trim().split(';').filter(p => p) : [],
                    email: fields[3]?.trim() || '',
                    role: fields[4]?.trim().toLowerCase() || 'external',
                    status: fields[5]?.trim().toLowerCase() || 'active',
                    password: fields[7]?.trim() || 'TempPassword123!',
                    requirePasswordReset: fields[8] ? fields[8].trim().toLowerCase() === 'true' : true
                });
            }

            if (usersToImport.length === 0) {
                await AdminCore.showWarningModal({
                    title: 'No Data Found',
                    message: 'No valid users found in CSV file.'
                });
                return;
            }

            // Save each user to the database
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            let successCount = 0;
            let errorCount = 0;
            const errors = [];

            for (const userData of usersToImport) {
                try {
                    const response = await fetch('/Admin/CreateUser', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': token || ''
                        },
                        body: JSON.stringify(userData)
                    });

                    const result = await response.json();

                    if (result.success) {
                        successCount++;
                    } else {
                        errorCount++;
                        errors.push(`${userData.name}: ${result.message || 'Unknown error'}`);
                    }
                } catch (error) {
                    errorCount++;
                    errors.push(`${userData.name}: ${error.message}`);
                }
            }

            // Reload users from database
            await loadUsers();

            // Show result
            if (errorCount === 0) {
                await AdminCore.showSuccessModal({
                    title: 'Import Complete',
                    message: `Imported ${successCount} users successfully.`
                });
            } else if (successCount > 0) {
                console.error('Import errors:', errors);
                await AdminCore.showWarningModal({
                    title: 'Partial Import',
                    message: `Imported ${successCount} users. ${errorCount} failed.`
                });
            } else {
                console.error('Import errors:', errors);
                await AdminCore.showErrorModal({
                    title: 'Import Failed',
                    message: `Import failed. ${errorCount} errors occurred.`
                });
            }
        };

        reader.readAsText(file);
    });

    fileInput.click();
}

// Parse CSV line handling quoted fields
function parseCSVLine(line) {
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

// Function to export to CSV
function exportCSV() {
    const headers = ['Name', 'Company', 'Partners', 'Email', 'Role', 'Status', 'Last Login', 'Password', 'Require Password Reset'];
    const rows = users.map(user => [
        user.name,
        user.company ? user.company.join(';') : '',
        user.partners ? user.partners.join(';') : '',
        user.email,
        user.role,
        user.status,
        user.lastLogin,
        user.password,
        user.requirePasswordReset || false
    ]);

    const csvContent = [
        headers.join(','),
        ...rows.map(row => row.map(field => `"${field}"`).join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.setAttribute('href', url);
    link.setAttribute('download', 'user_data.csv');
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Event Listeners
addUserBtn.addEventListener('click', openAddModal);
importBtn.addEventListener('click', importCSV);
exportBtn.addEventListener('click', exportCSV);
modalClose.addEventListener('click', closeModal);
modalOverlay.addEventListener('click', closeModal);
cancelBtn.addEventListener('click', closeModal);
userForm.addEventListener('submit', saveUser);

// Filter event listeners
searchInput.addEventListener('input', () => {
    applyFiltersAndSort();
});

statusFilter.addEventListener('change', () => {
    applyFiltersAndSort();
});

roleFilter.addEventListener('change', () => {
    applyFiltersAndSort();
});

companyFilter.addEventListener('change', () => {
    applyFiltersAndSort();
});

// Role change event listener in modal - show/hide appropriate fields
if (userRoleInput) {
    userRoleInput.addEventListener('change', () => {
        handleRoleChange(userRoleInput.value);
    });
}

// Close modal when clicking outside
window.addEventListener('click', (event) => {
    if (event.target === modal) {
        closeModal();
    }
});

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeMultiSelect();
    initializeSortableHeaders();
    loadPartnersDropdown(); // Load partners for single-select dropdown (Partner role)
    loadUsers(); // Load users from database instead of using hardcoded data
});

// Make remove functions globally accessible
window.removeCompany = removeCompany;
window.removePartner = removePartner;