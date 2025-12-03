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

        // Attribute Name cell
        const nameCell = document.createElement('td');

        // View mode
        const nameView = document.createElement('span');
        nameView.className = 'admin-inline-view';
        nameView.textContent = attribute.name;
        nameCell.appendChild(nameView);

        // Edit mode
        const nameEdit = document.createElement('div');
        nameEdit.className = 'admin-inline-edit';
        nameEdit.innerHTML = `<input type="text" class="admin-inline-input" value="${escapeHtml(attribute.name)}">`;
        nameCell.appendChild(nameEdit);

        row.appendChild(nameCell);

        // Description cell
        const descCell = document.createElement('td');

        // View mode
        const descView = document.createElement('span');
        descView.className = 'admin-inline-view';
        descView.textContent = attribute.description || '-';
        descCell.appendChild(descView);

        // Edit mode
        const descEdit = document.createElement('div');
        descEdit.className = 'admin-inline-edit';
        descEdit.innerHTML = `<input type="text" class="admin-inline-input" value="${escapeHtml(attribute.description || '')}">`;
        descCell.appendChild(descEdit);

        row.appendChild(descCell);

        // Active Status cell
        const activeCell = document.createElement('td');

        // View mode
        const activeView = document.createElement('span');
        activeView.className = 'admin-inline-view';
        const activeIcon = attribute.isActive ?
            '<i class="fas fa-check-circle" style="color: #92C83E;"></i>' :
            '<i class="fas fa-times-circle" style="color: #FF5252;"></i>';
        activeView.innerHTML = activeIcon;
        activeCell.appendChild(activeView);

        // Edit mode
        const activeEdit = document.createElement('div');
        activeEdit.className = 'admin-inline-edit';
        activeEdit.innerHTML = `<input type="checkbox" ${attribute.isActive ? 'checked' : ''}>`;
        activeCell.appendChild(activeEdit);

        row.appendChild(activeCell);

        // Actions cell
        const actionsCell = document.createElement('td');

        // View mode action buttons
        const actionButtons = document.createElement('div');
        actionButtons.className = 'admin-inline-actions';
        actionButtons.innerHTML = `
            <button class="admin-inline-action-btn admin-inline-action-btn--edit" title="Edit Attribute" aria-label="Edit attribute ${escapeHtml(attribute.name)}">
                <i class="fas fa-edit" aria-hidden="true"></i>
            </button>
            <button class="admin-inline-action-btn admin-inline-action-btn--delete" title="Delete Attribute" aria-label="Delete attribute ${escapeHtml(attribute.name)}">
                <i class="fas fa-trash-alt" aria-hidden="true"></i>
            </button>
        `;

        // Add event listeners
        actionButtons.querySelector('.admin-inline-action-btn--edit').addEventListener('click', () => editCSF(attribute.id));
        actionButtons.querySelector('.admin-inline-action-btn--delete').addEventListener('click', () => deleteCSF(attribute.id));

        actionsCell.appendChild(actionButtons);

        // Edit mode action buttons
        const editActions = document.createElement('div');
        editActions.className = 'admin-inline-edit-actions';
        editActions.innerHTML = `
            <button type="button" class="admin-inline-btn-cancel" aria-label="Cancel editing">
                <i class="fas fa-times" aria-hidden="true"></i> Cancel
            </button>
            <button type="button" class="admin-inline-btn-save" aria-label="Save changes">
                <i class="fas fa-check" aria-hidden="true"></i> Save
            </button>
        `;

        // Add event listeners for edit mode buttons
        editActions.querySelector('.admin-inline-btn-cancel').addEventListener('click', () => cancelEditCSF(attribute.id));
        editActions.querySelector('.admin-inline-btn-save').addEventListener('click', () => saveCSFEdit(attribute.id));

        actionsCell.appendChild(editActions);
        row.appendChild(actionsCell);

        csfTableBody.appendChild(row);
    });

    // Update count
    const totalCount = csfAttributes.length;
    const visibleCount = data.length;
    csfCountElement.textContent = `Showing ${visibleCount} of ${totalCount} CSF attributes`;
}

// Helper function to escape HTML
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
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
                const icon = h.querySelector('.admin-sort-icon');
                if (icon) {
                    icon.className = 'admin-sort-icon fas fa-sort';
                }
            });

            header.classList.add('active-sort');
            const sortIcon = header.querySelector('.admin-sort-icon');
            if (sortIcon) {
                sortIcon.className = 'admin-sort-icon fas ' + (sortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
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

// Edit CSF attribute - now uses inline editing instead of modal
function editCSF(id) {
    const row = document.querySelector(`tr[data-id="${id}"]`);
    if (!row) return;

    // Add edit mode class
    row.classList.add('admin-inline-edit-row');

    // Focus on the first input
    const firstInput = row.querySelector('.admin-inline-input');
    if (firstInput) {
        firstInput.focus();
        firstInput.select();
    }
}

// Cancel inline editing
function cancelEditCSF(id) {
    const row = document.querySelector(`tr[data-id="${id}"]`);
    if (!row) return;

    // Get original data
    const attribute = csfAttributes.find(a => a.id === id);
    if (!attribute) return;

    // Reset inputs to original values
    const inputs = row.querySelectorAll('.admin-inline-edit .admin-inline-input');
    if (inputs.length >= 2) {
        inputs[0].value = attribute.name;
        inputs[1].value = attribute.description || '';
    }

    const checkbox = row.querySelector('.admin-inline-edit input[type="checkbox"]');
    if (checkbox) {
        checkbox.checked = attribute.isActive;
    }

    // Remove edit mode class
    row.classList.remove('admin-inline-edit-row');
}

// Save inline edits
async function saveCSFEdit(id) {
    const row = document.querySelector(`tr[data-id="${id}"]`);
    if (!row) return;

    // Get values from inputs
    const inputs = row.querySelectorAll('.admin-inline-edit .admin-inline-input');
    const newName = inputs[0] ? inputs[0].value.trim() : '';
    const newDescription = inputs[1] ? inputs[1].value.trim() : '';
    const checkbox = row.querySelector('.admin-inline-edit input[type="checkbox"]');
    const newIsActive = checkbox ? checkbox.checked : true;

    // Validate name
    if (!newName) {
        AdminCore.showNotification('Attribute name cannot be empty', 'error');
        if (inputs[0]) inputs[0].focus();
        return;
    }

    // Check for duplicate names (excluding current attribute)
    const existingAttribute = csfAttributes.find(a => a.id !== id && a.name.toLowerCase() === newName.toLowerCase());
    if (existingAttribute) {
        AdminCore.showNotification('Attribute name already exists', 'error');
        if (inputs[0]) inputs[0].focus();
        return;
    }

    // Update local data
    const attributeIndex = csfAttributes.findIndex(a => a.id === id);
    if (attributeIndex !== -1) {
        csfAttributes[attributeIndex].name = newName;
        csfAttributes[attributeIndex].description = newDescription;
        csfAttributes[attributeIndex].isActive = newIsActive;
    }

    // Update view mode displays
    const viewElements = row.querySelectorAll('.admin-inline-view');
    if (viewElements.length >= 3) {
        viewElements[0].textContent = newName;
        viewElements[1].textContent = newDescription || '-';
        viewElements[2].innerHTML = newIsActive ?
            '<i class="fas fa-check-circle" style="color: #92C83E;"></i>' :
            '<i class="fas fa-times-circle" style="color: #FF5252;"></i>';
    }

    // Remove edit mode class
    row.classList.remove('admin-inline-edit-row');
    AdminCore.showNotification('CSF attribute updated successfully', 'success');
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