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

        // Actions cell
        const actionsCell = document.createElement('td');
        actionsCell.style.whiteSpace = 'nowrap';
        actionsCell.style.textAlign = 'center';

        // Edit button
        const editButton = document.createElement('button');
        editButton.className = 'btn-edit';
        editButton.innerHTML = '<i class="fas fa-edit"></i>';
        editButton.title = 'Edit';
        editButton.addEventListener('click', () => openEditModal(user));
        actionsCell.appendChild(editButton);

        // Delete button
        const deleteButton = document.createElement('button');
        deleteButton.className = 'btn-delete';
        deleteButton.innerHTML = '<i class="fas fa-trash-alt"></i>';
        deleteButton.title = 'Delete';
        deleteButton.addEventListener('click', () => deleteUser(user.id));
        actionsCell.appendChild(deleteButton);

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

            // Update header styling
            headers.forEach(h => {
                h.classList.remove('active-sort');
                const icon = h.querySelector('.sort-icon');
                if (icon) {
                    icon.textContent = '▼';
                    icon.style.opacity = '0.6';
                }
            });

            header.classList.add('active-sort');
            const sortIcon = header.querySelector('.sort-icon');
            if (sortIcon) {
                sortIcon.textContent = sortDirection === 'asc' ? '▼' : '▲';
                sortIcon.style.opacity = '1';
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

    // Set partner checkboxes
    partnersDropdown.querySelectorAll('input[type="checkbox"]').forEach(cb => {
        cb.checked = user.partners && user.partners.includes(cb.value);
    });
    updatePartnersDisplay();

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

    // Get selected companies and partners
    const selectedCompanies = Array.from(companyDropdown.querySelectorAll('input[type="checkbox"]:checked'))
        .map(cb => cb.value);
    const selectedPartners = Array.from(partnersDropdown.querySelectorAll('input[type="checkbox"]:checked'))
        .map(cb => cb.value);

    const userData = {
        name: userNameInput.value,
        company: selectedCompanies,
        partners: selectedPartners,
        email: userEmailInput.value,
        role: userRoleInput.value,
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
    loadUsers(); // Load users from database instead of using hardcoded data
});

// Make remove functions globally accessible
window.removeCompany = removeCompany;
window.removePartner = removePartner;