/**
 * Certificate Types Management JavaScript
 * Handles CRUD operations for certificate types in the admin panel
 */

// Certificate types data will be loaded from the server
let certTypes = [];

// DOM Elements
let certTypesTableBody;
let certTypesCountElement;
let searchInput;
let addCertTypeBtn;

// Track editing states
let editingId = null;
let addingNew = false;

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', async function() {
    console.log('Certificate Types page - DOM loaded');
    
    // Initialize DOM elements
    certTypesTableBody = document.querySelector('#cert-types-table tbody');
    certTypesCountElement = document.getElementById('certtypes-count');
    searchInput = document.getElementById('search-input');
    addCertTypeBtn = document.getElementById('add-certtype-btn');
    
    if (!certTypesTableBody || !certTypesCountElement || !searchInput || !addCertTypeBtn) {
        console.error('Required DOM elements not found');
        return;
    }
    
    // Set up event listeners
    setupEventListeners();
    
    console.log('About to call loadCertificateTypes');
    try {
        await loadCertificateTypes();
        console.log('loadCertificateTypes completed');
    } catch (error) {
        console.error('Error in loadCertificateTypes:', error);
    }
});

// API Functions
async function loadCertificateTypes() {
    console.log('loadCertificateTypes function called');
    try {
        console.log('Making fetch request to /Admin/GetCertificateTypes');
        const response = await fetch('/Admin/GetCertificateTypes');
        console.log('Response received:', response.status, response.statusText);
        
        if (response.status === 401) {
            console.log('Authentication required - redirecting to login may occur');
            showError('Please login to access this page');
            return;
        }
        
        const result = await response.json();
        console.log('Response parsed as JSON:', result);
        
        if (result.success) {
            certTypes = result.data || [];
            console.log('Certificate types loaded:', certTypes.length, 'items');
            populateTable(filterCertTypes());
        } else {
            console.error('Error loading certificate types:', result.message);
            showError('Failed to load certificate types: ' + result.message);
        }
    } catch (error) {
        console.error('Error loading certificate types:', error);
        showError('Failed to load certificate types. Please try again.');
    }
}

async function createCertificateType(name) {
    try {
        const response = await fetch('/Admin/CreateCertificateType', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                name: name,
                isActive: true
            })
        });
        
        const result = await response.json();
        return result.success;
    } catch (error) {
        console.error('Error creating certificate type:', error);
        return false;
    }
}

async function updateCertificateType(id, name) {
    try {
        const response = await fetch(`/Admin/UpdateCertificateType/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name: name,
                isActive: true
            })
        });
        
        const result = await response.json();
        return result.success;
    } catch (error) {
        console.error('Error updating certificate type:', error);
        return false;
    }
}

async function deleteCertificateTypeApi(id) {
    try {
        const response = await fetch(`/Admin/DeleteCertificateType/${id}`, {
            method: 'DELETE'
        });
        
        const result = await response.json();
        return result.success;
    } catch (error) {
        console.error('Error deleting certificate type:', error);
        return false;
    }
}

function showError(message) {
    AdminCore.showNotification(message, 'error');
}

// Function to populate the table
function populateTable(data) {
    certTypesTableBody.innerHTML = '';

    // Add rows for existing certificate types first
    data.forEach(certType => {
        const row = document.createElement('tr');
        row.className = 'admin-table-row';
        row.setAttribute('data-id', certType.id);

        // Certificate Type Name cell
        const nameCell = document.createElement('td');
        nameCell.className = 'admin-table-td';
        nameCell.setAttribute('data-label', 'Certificate Type Name');

        // Check if this certificate type is currently being edited
        if (editingId === certType.id) {
            const input = document.createElement('input');
            input.type = 'text';
            input.className = 'edit-input';
            input.value = certType.name;
            input.id = `edit-certtype-${certType.id}`;
            nameCell.appendChild(input);
        } else {
            nameCell.textContent = certType.name;
        }

        row.appendChild(nameCell);

        // Actions cell
        const actionsCell = document.createElement('td');
        actionsCell.className = 'admin-table-td admin-table-td--actions';
        actionsCell.setAttribute('data-label', 'Actions');

        if (editingId === certType.id) {
            // If editing, show save and cancel buttons
            const actionButtons = document.createElement('div');
            actionButtons.className = 'admin-action-buttons';

            const saveBtn = document.createElement('button');
            saveBtn.className = 'admin-action-btn admin-action-btn--save';
            saveBtn.innerHTML = '<i class="fas fa-check" aria-hidden="true"></i>';
            saveBtn.title = 'Save';
            saveBtn.setAttribute('aria-label', 'Save changes to certificate type');
            saveBtn.addEventListener('click', () => saveCertTypeEdit(certType.id));

            const cancelBtn = document.createElement('button');
            cancelBtn.className = 'admin-action-btn admin-action-btn--cancel';
            cancelBtn.innerHTML = '<i class="fas fa-times" aria-hidden="true"></i>';
            cancelBtn.title = 'Cancel';
            cancelBtn.setAttribute('aria-label', 'Cancel editing');
            cancelBtn.addEventListener('click', cancelCertTypeEdit);

            actionButtons.appendChild(saveBtn);
            actionButtons.appendChild(cancelBtn);
            actionsCell.appendChild(actionButtons);
        } else {
            // If not editing, show edit and delete buttons
            const actionButtons = document.createElement('div');
            actionButtons.className = 'admin-action-buttons';

            const editBtn = document.createElement('button');
            editBtn.className = 'admin-action-btn admin-action-btn--edit';
            editBtn.innerHTML = '<i class="fas fa-edit" aria-hidden="true"></i>';
            editBtn.title = 'Edit';
            editBtn.setAttribute('aria-label', `Edit ${certType.name}`);
            editBtn.addEventListener('click', () => editCertType(certType.id));

            const deleteBtn = document.createElement('button');
            deleteBtn.className = 'admin-action-btn admin-action-btn--delete';
            deleteBtn.innerHTML = '<i class="fas fa-trash-alt" aria-hidden="true"></i>';
            deleteBtn.title = 'Delete';
            deleteBtn.setAttribute('aria-label', `Delete ${certType.name}`);
            deleteBtn.addEventListener('click', () => deleteCertType(certType.id));

            actionButtons.appendChild(editBtn);
            actionButtons.appendChild(deleteBtn);
            actionsCell.appendChild(actionButtons);
        }

        row.appendChild(actionsCell);
        certTypesTableBody.appendChild(row);
    });

    // Check if we need to add a new row for adding a certificate type (at bottom)
    if (addingNew) {
        const addRow = document.createElement('tr');
        addRow.className = 'admin-table-row add-row';

        const inputCell = document.createElement('td');
        inputCell.className = 'admin-table-td';
        inputCell.setAttribute('data-label', 'Certificate Type Name');
        const input = document.createElement('input');
        input.type = 'text';
        input.placeholder = 'Enter certificate type name';
        input.id = 'new-certtype-input';
        inputCell.appendChild(input);

        const actionsCell = document.createElement('td');
        actionsCell.className = 'admin-table-td admin-table-td--actions';
        actionsCell.setAttribute('data-label', 'Actions');

        const actionButtons = document.createElement('div');
        actionButtons.className = 'admin-action-buttons';

        const saveBtn = document.createElement('button');
        saveBtn.className = 'admin-action-btn admin-action-btn--save';
        saveBtn.innerHTML = '<i class="fas fa-check" aria-hidden="true"></i>';
        saveBtn.title = 'Save';
        saveBtn.setAttribute('aria-label', 'Save new certificate type');
        saveBtn.addEventListener('click', saveNewCertType);

        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'admin-action-btn admin-action-btn--cancel';
        cancelBtn.innerHTML = '<i class="fas fa-times" aria-hidden="true"></i>';
        cancelBtn.title = 'Cancel';
        cancelBtn.setAttribute('aria-label', 'Cancel adding certificate type');
        cancelBtn.addEventListener('click', cancelAddCertType);

        actionButtons.appendChild(saveBtn);
        actionButtons.appendChild(cancelBtn);
        actionsCell.appendChild(actionButtons);

        addRow.appendChild(inputCell);
        addRow.appendChild(actionsCell);

        certTypesTableBody.appendChild(addRow);
    }

    // Update the certificate types count
    updateCertTypesCount();
}

// Function to filter certificate types based on search input
function filterCertTypes() {
    const searchTerm = searchInput.value.toLowerCase();

    if (!searchTerm) {
        return [...certTypes];
    }

    return certTypes.filter(certType =>
        certType.name.toLowerCase().includes(searchTerm)
    );
}

// Function to update the certificate types count display
function updateCertTypesCount() {
    const filteredCount = filterCertTypes().length;
    const totalCount = certTypes.length;
    certTypesCountElement.textContent = `Showing ${filteredCount} of ${totalCount} certificate types`;
}

// Function to add a new certificate type
function addCertType() {
    // Prevent multiple add rows
    if (addingNew || editingId !== null) return;

    addingNew = true;
    populateTable(filterCertTypes());

    // Focus on the new input
    setTimeout(() => {
        const newInput = document.getElementById('new-certtype-input');
        if (newInput) newInput.focus();
    }, 0);
}

// Function to save a new certificate type
async function saveNewCertType() {
    const newInput = document.getElementById('new-certtype-input');
    const certTypeName = newInput.value.trim();

    if (certTypeName) {
        const success = await createCertificateType(certTypeName);
        if (success) {
            addingNew = false;
            await loadCertificateTypes(); // Reload data from server
            AdminCore.showNotification('Certificate type created successfully', 'success');
        } else {
            AdminCore.showNotification('Failed to create certificate type. Please try again.', 'error');
        }
    } else {
        AdminCore.showNotification('Please enter a certificate type name.', 'warning');
    }
}

// Function to cancel adding a new certificate type
function cancelAddCertType() {
    addingNew = false;
    populateTable(filterCertTypes());
}

// Function to edit a certificate type
function editCertType(id) {
    // Prevent multiple edits
    if (editingId !== null || addingNew) return;

    editingId = id;
    populateTable(filterCertTypes());

    // Focus on the edit input
    setTimeout(() => {
        const editInput = document.getElementById(`edit-certtype-${id}`);
        if (editInput) {
            editInput.focus();
            editInput.select();
        }
    }, 0);
}

// Function to save certificate type edit
async function saveCertTypeEdit(id) {
    const editInput = document.getElementById(`edit-certtype-${id}`);
    const newName = editInput.value.trim();

    if (newName) {
        const success = await updateCertificateType(id, newName);
        if (success) {
            editingId = null;
            await loadCertificateTypes(); // Reload data from server
            AdminCore.showNotification('Certificate type updated successfully', 'success');
        } else {
            AdminCore.showNotification('Failed to update certificate type. Please try again.', 'error');
        }
    } else {
        AdminCore.showNotification('Please enter a certificate type name.', 'warning');
    }
}

// Function to cancel certificate type edit
function cancelCertTypeEdit() {
    editingId = null;
    populateTable(filterCertTypes());
}

// Function to delete a certificate type
async function deleteCertType(id) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Delete Confirmation',
        message: 'Are you sure you want to delete this certificate type? This action cannot be undone.',
        confirmText: 'Delete',
        cancelText: 'Cancel',
        type: 'danger'
    });

    if (confirmed) {
        const success = await deleteCertificateTypeApi(id);
        if (success) {
            await loadCertificateTypes(); // Reload data from server
            AdminCore.showNotification('Certificate type deleted successfully', 'success');
        } else {
            AdminCore.showNotification('Failed to delete certificate type. Please try again.', 'error');
        }
    }
}

// Export functionality - CSV only
function exportCertificateTypes() {
    const columns = [
        { key: 'id', header: 'ID' },
        { key: 'name', header: 'Certificate Type Name' }
    ];

    // Direct CSV export only
    ExportUtils.exportToCSV(
        filterCertTypes(),
        columns,
        'certificate_types',
        (message, type) => AdminCore.showNotification(message, type)
    );
}

// Set up all event listeners
function setupEventListeners() {
    // Add certificate type button
    addCertTypeBtn.addEventListener('click', addCertType);
    
    // Export button
    const exportBtn = document.getElementById('export-btn');
    if (exportBtn) {
        exportBtn.addEventListener('click', exportCertificateTypes);
    }

    // Search input
    searchInput.addEventListener('input', () => {
        populateTable(filterCertTypes());
    });

    // Add event listener for "enter" key on search input
    searchInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            populateTable(filterCertTypes());
        }
    });

    // Initialize sortable header
    const nameHeader = document.querySelector('th[data-sort="name"]');
    let sortDirection = 'asc';

    if (nameHeader) {
        nameHeader.addEventListener('click', () => {
            sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';

            certTypes.sort((a, b) => {
                const nameA = a.name.toLowerCase();
                const nameB = b.name.toLowerCase();

                if (sortDirection === 'asc') {
                    return nameA.localeCompare(nameB);
                } else {
                    return nameB.localeCompare(nameA);
                }
            });

            // Update sort icon
            const sortIcon = sortDirection === 'asc' ? '▼' : '▲';
            nameHeader.setAttribute('data-sort-direction', sortDirection);

            populateTable(filterCertTypes());
        });
    }
}