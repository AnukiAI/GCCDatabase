// Sample data based on real GCC project data
let users = [
    {
        id: 1,
        name: 'Sarah Johnson',
        company: ['assa-abloy'],
        partners: ['amazon'],
        email: 'sarah.johnson@assaabloy.com',
        role: 'admin',
        status: 'active',
        lastLogin: '2025-07-15 09:15',
        password: 'admin2025',
        requirePasswordReset: false
    },
    {
        id: 2,
        name: 'Michael Chen',
        company: ['assa-abloy-corbin', 'assa-abloy-sargent'],
        partners: ['amazon', 'wayfair'],
        email: 'michael.chen@assaabloy.com',
        role: 'external-user',
        status: 'active',
        lastLogin: '2025-07-14 14:22',
        password: 'external2025',
        requirePasswordReset: false
    },
    {
        id: 3,
        name: 'Lisa Anderson',
        company: ['greencircle-certified'],
        partners: [],
        email: 'lisa.anderson@greencircle.org',
        role: 'gcc-user',
        status: 'active',
        lastLogin: '2025-07-15 11:05',
        password: 'gcc2025',
        requirePasswordReset: false
    },
    {
        id: 4,
        name: 'David Kim',
        company: ['alcamare-intl'],
        partners: ['amazon', 'target'],
        email: 'david.kim@alcamare.com',
        role: 'external-user',
        status: 'inactive',
        lastLogin: '2025-07-10 16:40',
        password: 'alcamare2025',
        requirePasswordReset: true
    },
    {
        id: 5,
        name: 'Emily Rodriguez',
        company: ['greencircle-certified'],
        partners: [],
        email: 'emily.rodriguez@greencircle.org',
        role: 'admin',
        status: 'active',
        lastLogin: '2025-07-15 08:50',
        password: 'admin2025',
        requirePasswordReset: false
    },
    {
        id: 6,
        name: 'James Wilson',
        company: ['assa-abloy'],
        partners: ['amazon', 'wayfair', 'walmart'],
        email: 'james.wilson@assaabloy.com',
        role: 'external-user',
        status: 'active',
        lastLogin: '2025-07-13 13:25',
        password: 'external2025',
        requirePasswordReset: false
    },
    {
        id: 7,
        name: 'Maria Garcia',
        company: ['greencircle-certified'],
        partners: [],
        email: 'maria.garcia@greencircle.org',
        role: 'gcc-user',
        status: 'active',
        lastLogin: '2025-07-15 10:30',
        password: 'gcc2025',
        requirePasswordReset: false
    },
    {
        id: 8,
        name: 'Robert Taylor',
        company: ['assa-abloy-corbin'],
        partners: ['amazon'],
        email: 'robert.taylor@corbinrusswin.com',
        role: 'external-user',
        status: 'active',
        lastLogin: '2025-07-12 15:45',
        password: 'corbin2025',
        requirePasswordReset: false
    }
];

// Company and Partner mappings based on real data
const companyNames = {
    'assa-abloy': 'ASSA ABLOY',
    'assa-abloy-corbin': 'ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division',
    'assa-abloy-sargent': 'ASSA ABLOY Access & Egress Hardware Group, Inc. - SARGENT Manufacturing Co.',
    'greencircle-certified': 'GreenCircle Certified',
    'alcamare-intl': 'Alcamare International Recycling Group',
    'acme-corp': 'ACME Corp',
    'tech-solutions': 'Tech Solutions',
    'global-industries': 'Global Industries'
};

const partnerNames = {
    'amazon': 'Amazon',
    'wayfair': 'Wayfair',
    'walmart': 'Walmart',
    'target': 'Target',
    'home-depot': 'Home Depot',
    'lowes': 'Lowe\'s'
};

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
    return array.map(item => mapping[item] || item).join(', ');
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
    userCountElement.textContent = `Showing ${visibleCount} of ${users.length} users`;
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
function saveUser(event) {
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

    if (userId) {
        // Update existing user
        const index = users.findIndex(u => u.id.toString() === userId);
        if (index !== -1) {
            // Keep the password if not changed
            if (!userData.password) {
                userData.password = users[index].password;
            }

            // Keep the lastLogin
            userData.lastLogin = users[index].lastLogin;

            users[index] = {
                ...users[index],
                ...userData
            };
        }
    } else {
        // Add new user
        const newId = users.length > 0 ? Math.max(...users.map(u => u.id)) + 1 : 1;

        // Set current date as lastLogin
        const now = new Date();
        const lastLogin = now.toISOString().slice(0, 16).replace('T', ' ');

        users.push({
            id: newId,
            lastLogin: lastLogin,
            ...userData
        });
    }

    closeModal();
    applyFiltersAndSort();
}

// Function to delete a user
function deleteUser(id) {
    if (confirm('Are you sure you want to delete this user?')) {
        users = users.filter(user => user.id !== id);
        applyFiltersAndSort();
    }
}

// Function to handle CSV import
function importCSV() {
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.accept = '.csv';

    fileInput.addEventListener('change', (event) => {
        const file = event.target.files[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = (e) => {
            const contents = e.target.result;
            const lines = contents.split('\n');

            // Skip header row and process data
            const newUsers = [];
            let maxId = users.length > 0 ? Math.max(...users.map(u => u.id)) : 0;

            for (let i = 1; i < lines.length; i++) {
                const line = lines[i].trim();
                if (!line) continue;

                const fields = line.split(',');
                if (fields.length < 5) continue;  // Must have at least name, company, email, role, status

                maxId++;
                newUsers.push({
                    id: maxId,
                    name: fields[0].trim(),
                    company: fields[1] ? fields[1].trim().split(';') : [],
                    partners: fields[2] ? fields[2].trim().split(';') : [],
                    email: fields[3].trim(),
                    role: fields[4].trim().toLowerCase(),
                    status: fields[5] ? fields[5].trim().toLowerCase() : 'active',
                    lastLogin: fields[6] ? fields[6].trim() : new Date().toISOString().slice(0, 16).replace('T', ' '),
                    password: fields[7] ? fields[7].trim() : 'defaultpassword',
                    requirePasswordReset: fields[8] ? fields[8].trim().toLowerCase() === 'true' : true
                });
            }

            users = [...users, ...newUsers];
            applyFiltersAndSort();
        };

        reader.readAsText(file);
    });

    fileInput.click();
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

// Initialize
initializeMultiSelect();
initializeSortableHeaders();
applyFiltersAndSort();

// Make remove functions globally accessible
window.removeCompany = removeCompany;
window.removePartner = removePartner;