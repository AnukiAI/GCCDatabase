// CSF Attributes Management JavaScript

// CSF Attributes data loaded from database
let csfAttributes = [];

// DOM Elements
let csfTable = null;
let csfTableBody = null;
let csfCountElement = null;
let searchInput = null;
let certificationFilter = null;
let addCsfBtn = null;
let exportBtn = null;
let modal = null;
let modalTitle = null;
let modalClose = null;
let csfForm = null;
let cancelBtn = null;

// Sorting variables
let sortColumn = 'name';
let sortDirection = 'asc';
let currentEditingId = null;

// Function to load CSF attributes from database
async function loadCSFAttributes() {
    try {
        const response = await fetch('/Admin/GetCSFAttributes');
        const result = await response.json();

        if (result.success && result.data) {
            // Map database model to our UI model
            csfAttributes = result.data.map(attr => ({
                id: attr.id.toString(),
                name: attr.name,
                description: attr.description || '',
                isActive: attr.isActive
            }));
            populateTable(csfAttributes);
        } else {
            console.error('Failed to load CSF attributes:', result.message);
            AdminCore.showNotification('Failed to load CSF attributes', 'error');
        }
    } catch (error) {
        console.error('Error loading CSF attributes:', error);
        AdminCore.showNotification('Error loading CSF attributes: ' + error.message, 'error');
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeElements();
    initializeEventListeners();
    initializeSortableHeaders();
    loadCSFAttributes(); // Load from database instead of using hardcoded data
});

// Initialize DOM elements
function initializeElements() {
    csfTable = document.getElementById('csf-table');
    csfTableBody = csfTable.querySelector('tbody');
    csfCountElement = document.getElementById('csf-count');
    searchInput = document.getElementById('search-input');
    addCsfBtn = document.getElementById('add-csf-btn');
    exportBtn = document.getElementById('export-btn');
    modal = document.getElementById('csf-modal');
    modalTitle = document.getElementById('modal-title');
    modalClose = document.querySelector('.modal-close');
    csfForm = document.getElementById('csf-form');
    cancelBtn = document.getElementById('cancel-btn');
}

// Initialize event listeners
function initializeEventListeners() {
    addCsfBtn.addEventListener('click', showAddCsfRow);
    exportBtn.addEventListener('click', exportCSV);
    searchInput.addEventListener('input', () => filterAndSort());
}

// Function to populate the table
function populateTable(data) {
    // Clear existing rows except the add row
    const addRow = document.getElementById('csf-add-row');
    csfTableBody.innerHTML = '';
    if (addRow) {
        csfTableBody.appendChild(addRow);
    }

    data.forEach(attribute => {
        const row = document.createElement('tr');
        row.setAttribute('data-id', attribute.id);

        // Attribute Name
        const nameCell = document.createElement('td');
        nameCell.textContent = attribute.name;
        row.appendChild(nameCell);

        // Description
        const descCell = document.createElement('td');
        descCell.textContent = attribute.description || '-';
        row.appendChild(descCell);

        // Active Status
        const activeCell = document.createElement('td');
        const activeIcon = attribute.isActive ?
            '<i class="fas fa-check-circle" style="color: #92C83E;"></i>' :
            '<i class="fas fa-times-circle" style="color: #FF5252;"></i>';
        activeCell.innerHTML = activeIcon;
        row.appendChild(activeCell);

        // Actions
        const actionsCell = document.createElement('td');

        // Edit button
        const editButton = document.createElement('button');
        editButton.className = 'btn-edit';
        editButton.innerHTML = '<i class="fas fa-edit"></i>';
        editButton.title = 'Edit';
        editButton.addEventListener('click', () => editCSF(attribute.id));
        actionsCell.appendChild(editButton);

        // Delete button
        const deleteButton = document.createElement('button');
        deleteButton.className = 'btn-delete';
        deleteButton.innerHTML = '<i class="fas fa-trash-alt"></i>';
        deleteButton.title = 'Delete';
        deleteButton.addEventListener('click', () => deleteCSF(attribute.id));
        actionsCell.appendChild(deleteButton);

        row.appendChild(actionsCell);
        csfTableBody.appendChild(row);
    });

    // Update count
    const totalCount = csfAttributes.length;
    const visibleCount = data.length;
    csfCountElement.textContent = `Showing ${visibleCount} of ${totalCount} CSF attributes`;
}

// Format certification name
function formatCertification(cert) {
    const certMap = {
        'greenguard': 'GREENGUARD',
        'greenguard-gold': 'GREENGUARD Gold',
        'cradle-to-cradle': 'Cradle to Cradle',
        'energy-star': 'ENERGY STAR'
    };
    return certMap[cert] || cert;
}

// Capitalize first letter
function capitalizeFirst(str) {
    return str.charAt(0).toUpperCase() + str.slice(1);
}

// Initialize sortable headers
function initializeSortableHeaders() {
    const headers = csfTable.querySelectorAll('th[data-sort]');
    
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
            
            filterAndSort();
        });
    });
}

// Filter and sort data
function filterAndSort() {
    let filteredData = [...csfAttributes];

    // Apply search filter
    const searchTerm = searchInput.value.toLowerCase();
    if (searchTerm) {
        filteredData = filteredData.filter(attr =>
            attr.name.toLowerCase().includes(searchTerm) ||
            (attr.description && attr.description.toLowerCase().includes(searchTerm))
        );
    }

    // Sort data
    filteredData.sort((a, b) => {
        let valueA = a[sortColumn] !== undefined && a[sortColumn] !== null ? a[sortColumn].toString().toLowerCase() : '';
        let valueB = b[sortColumn] !== undefined && b[sortColumn] !== null ? b[sortColumn].toString().toLowerCase() : '';

        if (sortDirection === 'asc') {
            return valueA.localeCompare(valueB);
        } else {
            return valueB.localeCompare(valueA);
        }
    });

    populateTable(filteredData);
}

// Open modal
function openModal(editId = null) {
    currentEditingId = editId;
    
    if (editId) {
        modalTitle.textContent = 'Edit CSF Attribute';
        const attribute = csfAttributes.find(a => a.id === editId);
        if (attribute) {
            document.getElementById('csf-id').value = attribute.id;
            document.getElementById('csf-name').value = attribute.name;
            document.getElementById('csf-certification').value = attribute.certification;
            document.getElementById('csf-type').value = attribute.type;
            document.getElementById('csf-description').value = attribute.description || '';
            document.getElementById('csf-required').checked = attribute.required;
        }
    } else {
        modalTitle.textContent = 'Add New CSF Attribute';
        csfForm.reset();
        document.getElementById('csf-id').value = '';
    }
    
    modal.classList.add('active');
}

// Close modal
function closeModal() {
    modal.classList.remove('active');
    csfForm.reset();
    currentEditingId = null;
}

// Save CSF attribute
function saveCSF(e) {
    e.preventDefault();
    
    const id = document.getElementById('csf-id').value;
    const name = document.getElementById('csf-name').value.trim();
    const certification = document.getElementById('csf-certification').value;
    const type = document.getElementById('csf-type').value;
    const description = document.getElementById('csf-description').value.trim();
    const required = document.getElementById('csf-required').checked;
    
    if (id) {
        // Update existing
        const index = csfAttributes.findIndex(a => a.id === id);
        if (index !== -1) {
            csfAttributes[index] = { id, name, certification, type, description, required };
            showToast('CSF attribute updated successfully');
        }
    } else {
        // Add new
        const newId = 'CSF' + (csfAttributes.length + 1).toString().padStart(3, '0');
        csfAttributes.push({ id: newId, name, certification, type, description, required });
        showToast('CSF attribute added successfully');
    }
    
    closeModal();
    filterAndSort();
}

// Edit CSF attribute
function editCSF(id) {
    openModal(id);
}

// Delete CSF attribute
async function deleteCSF(id) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Delete Confirmation',
        message: 'Are you sure you want to delete this CSF attribute? This action cannot be undone.',
        confirmText: 'Delete',
        cancelText: 'Cancel',
        type: 'danger'
    });

    if (confirmed) {
        csfAttributes = csfAttributes.filter(attr => attr.id !== id);
        filterAndSort();
        AdminCore.showNotification('CSF attribute deleted successfully', 'success');
    }
}

// Export to CSV
function exportCSV() {
    const headers = ['Attribute Name', 'Description', 'Active'];
    const rows = csfAttributes.map(attr => [
        attr.name,
        attr.description || '',
        attr.isActive ? 'Yes' : 'No'
    ]);

    const csvContent = [
        headers.join(','),
        ...rows.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.setAttribute('href', url);
    link.setAttribute('download', `csf_attributes_${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    AdminCore.showNotification('CSF attributes exported successfully', 'success');
}

// Show toast notification
function showToast(message, type = 'success') {
    // Remove existing toasts
    const existingToasts = document.querySelectorAll('.toast-notification');
    existingToasts.forEach(toast => toast.remove());
    
    // Create toast
    const toast = document.createElement('div');
    toast.className = 'toast-notification';
    if (type === 'error') {
        toast.classList.add('error');
    }
    toast.textContent = message;
    
    document.body.appendChild(toast);
    
    // Auto hide after 3 seconds
    setTimeout(() => {
        toast.classList.add('hide');
        setTimeout(() => {
            toast.remove();
        }, 500);
    }, 3000);
}

// Inline editing functions for CSF Attributes
function showAddCsfRow() {
    const addRow = document.getElementById('csf-add-row');
    if (addRow) {
        addRow.classList.add('active');
        const nameInput = document.getElementById('new-csf-name');
        if (nameInput) {
            nameInput.focus();
        }
    }
}

function saveNewCsfAttribute() {
    const nameInput = document.getElementById('new-csf-name');
    const descriptionInput = document.getElementById('new-csf-description');
    const activeCheckbox = document.getElementById('new-csf-active');

    const formData = {
        'new-csf-name': nameInput ? nameInput.value.trim() : '',
        'new-csf-description': descriptionInput ? descriptionInput.value.trim() : ''
    };

    // Define validation rules
    const validationRules = {
        'new-csf-name': [
            'required',
            { type: 'length', params: FieldLimits.attributeName },
            { type: 'unique', params: csfAttributes.map(attr => attr.name) }
        ],
        'new-csf-description': [
            { type: 'length', params: FieldLimits.description }
        ]
    };

    // Clear previous validation errors
    if (typeof clearValidationErrors === 'function') {
        clearValidationErrors();
    }

    // Validate the form
    if (typeof validateForm === 'function') {
        const validation = validateForm(formData, validationRules);

        if (!validation.valid) {
            if (typeof showValidationErrors === 'function') {
                showValidationErrors(validation.errors);
            }
            // Focus on first error field
            const firstErrorField = document.getElementById(validation.errors[0].field);
            if (firstErrorField) firstErrorField.focus();
            return;
        }
    }

    // Generate new ID
    const newId = generateNewCsfId();
    const newAttribute = {
        id: newId,
        name: formData['new-csf-name'],
        description: formData['new-csf-description'],
        isActive: activeCheckbox ? activeCheckbox.checked : true
    };

    // Add to data
    csfAttributes.push(newAttribute);

    // Clear form and hide row
    clearAddCsfForm();
    const addRow = document.getElementById('csf-add-row');
    if (addRow) addRow.classList.remove('active');

    // Refresh table
    filterAndSort();
    AdminCore.showNotification('CSF attribute added successfully', 'success');
}

function cancelAddCsfAttribute() {
    clearAddCsfForm();
    const addRow = document.getElementById('csf-add-row');
    if (addRow) addRow.classList.remove('active');
}

function clearAddCsfForm() {
    const nameInput = document.getElementById('new-csf-name');
    const descriptionInput = document.getElementById('new-csf-description');
    const activeCheckbox = document.getElementById('new-csf-active');

    if (nameInput) nameInput.value = '';
    if (descriptionInput) descriptionInput.value = '';
    if (activeCheckbox) activeCheckbox.checked = true;
}

function generateNewCsfId() {
    // Find the highest numeric ID and increment
    const numericIds = csfAttributes.map(attr => parseInt(attr.id.replace('CSF', '')));
    const maxId = Math.max(...numericIds);
    
    // Format with leading zeros
    const newId = (maxId + 1).toString().padStart(3, '0');
    return 'CSF' + newId;
}