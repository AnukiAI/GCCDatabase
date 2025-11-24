// CSF Attributes Management JavaScript

// Sample data for initial display
let csfAttributes = [
    { id: "CSF001", name: "Biobased", certification: "greenguard", type: "boolean", description: "Product contains biobased materials", required: true },
    { id: "CSF002", name: "Biodegradable", certification: "greenguard", type: "boolean", description: "Product is biodegradable", required: false },
    { id: "CSF003", name: "Carbon Footprint Reduction", certification: "greenguard-gold", type: "number", description: "Percentage of carbon footprint reduction", required: true },
    { id: "CSF004", name: "Certified Energy Savings", certification: "energy-star", type: "number", description: "Energy savings percentage", required: true },
    { id: "CSF005", name: "Certified Environmental Facts", certification: "cradle-to-cradle", type: "text", description: "Environmental certification details", required: false },
    { id: "CSF006", name: "Certified Sustainability Facts", certification: "greenguard", type: "text", description: "Sustainability certification details", required: false },
    { id: "CSF007", name: "Certified Water Savings", certification: "energy-star", type: "number", description: "Water savings percentage", required: true },
    { id: "CSF008", name: "Closed Loop Product", certification: "cradle-to-cradle", type: "boolean", description: "Product is part of closed loop system", required: false },
    { id: "CSF009", name: "Dematerialization", certification: "greenguard", type: "text", description: "Material reduction details", required: false },
    { id: "CSF010", name: "Greenhouse Gas (GHG) Verification", certification: "greenguard-gold", type: "boolean", description: "GHG emissions verified", required: true }
];

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

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeElements();
    initializeEventListeners();
    initializeSortableHeaders();
    populateTable(csfAttributes);
});

// Initialize DOM elements
function initializeElements() {
    csfTable = document.getElementById('csf-table');
    csfTableBody = csfTable.querySelector('tbody');
    csfCountElement = document.getElementById('csf-count');
    searchInput = document.getElementById('search-input');
    certificationFilter = document.getElementById('certification-filter');
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
    certificationFilter.addEventListener('change', () => filterAndSort());
}

// Function to populate the table
function populateTable(data) {
    csfTableBody.innerHTML = '';
    
    data.forEach(attribute => {
        const row = document.createElement('tr');
        row.setAttribute('data-id', attribute.id);
        
        // Attribute Name
        const nameCell = document.createElement('td');
        nameCell.textContent = attribute.name;
        if (attribute.required) {
            nameCell.innerHTML += ' <span style="color: red;">*</span>';
        }
        row.appendChild(nameCell);
        
        // Certification Type
        const certCell = document.createElement('td');
        certCell.textContent = formatCertification(attribute.certification);
        row.appendChild(certCell);
        
        // Data Type
        const typeCell = document.createElement('td');
        typeCell.textContent = capitalizeFirst(attribute.type);
        row.appendChild(typeCell);
        
        // Description
        const descCell = document.createElement('td');
        descCell.textContent = attribute.description || '-';
        row.appendChild(descCell);
        
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
            attr.description.toLowerCase().includes(searchTerm)
        );
    }
    
    // Apply certification filter
    const certFilter = certificationFilter.value;
    if (certFilter !== 'all') {
        filteredData = filteredData.filter(attr => attr.certification === certFilter);
    }
    
    // Sort data
    filteredData.sort((a, b) => {
        let valueA = a[sortColumn] ? a[sortColumn].toString().toLowerCase() : '';
        let valueB = b[sortColumn] ? b[sortColumn].toString().toLowerCase() : '';
        
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
function deleteCSF(id) {
    if (confirm('Are you sure you want to delete this CSF attribute?')) {
        csfAttributes = csfAttributes.filter(attr => attr.id !== id);
        filterAndSort();
        showToast('CSF attribute deleted successfully');
    }
}

// Export to CSV
function exportCSV() {
    const headers = ['Attribute Name', 'Certification Type', 'Data Type', 'Description', 'Required'];
    const rows = csfAttributes.map(attr => [
        attr.name,
        formatCertification(attr.certification),
        capitalizeFirst(attr.type),
        attr.description || '',
        attr.required ? 'Yes' : 'No'
    ]);
    
    const csvContent = [
        headers.join(','),
        ...rows.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n');
    
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    
    link.setAttribute('href', url);
    link.setAttribute('download', 'csf_attributes.csv');
    link.style.visibility = 'hidden';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    showToast('CSF attributes exported successfully');
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
    const certificationSelect = document.getElementById('new-csf-certification');
    const typeSelect = document.getElementById('new-csf-type');
    const descriptionInput = document.getElementById('new-csf-description');
    
    const formData = {
        'new-csf-name': nameInput ? nameInput.value.trim() : '',
        'new-csf-certification': certificationSelect ? certificationSelect.value : '',
        'new-csf-type': typeSelect ? typeSelect.value : '',
        'new-csf-description': descriptionInput ? descriptionInput.value.trim() : ''
    };
    
    // Define validation rules
    const validationRules = {
        'new-csf-name': [
            'required',
            { type: 'length', params: FieldLimits.attributeName },
            'alphanumeric',
            { type: 'unique', params: csfAttributes.map(attr => attr.name) }
        ],
        'new-csf-certification': ['required'],
        'new-csf-type': ['required'],
        'new-csf-description': [
            { type: 'length', params: FieldLimits.description }
        ]
    };
    
    // Clear previous validation errors
    clearValidationErrors();
    
    // Validate the form
    const validation = validateForm(formData, validationRules);
    
    if (!validation.valid) {
        showValidationErrors(validation.errors);
        // Focus on first error field
        const firstErrorField = document.getElementById(validation.errors[0].field);
        if (firstErrorField) firstErrorField.focus();
        return;
    }
    
    // Generate new ID
    const newId = generateNewCsfId();
    const newAttribute = {
        id: newId,
        name: formData['new-csf-name'],
        certification: formData['new-csf-certification'],
        type: formData['new-csf-type'],
        description: formData['new-csf-description'],
        required: false // Default to not required
    };
    
    // Add to data
    csfAttributes.push(newAttribute);
    
    // Clear form and hide row
    clearAddCsfForm();
    const addRow = document.getElementById('csf-add-row');
    if (addRow) addRow.classList.remove('active');
    
    // Refresh table
    filterAndSort();
    showToast('CSF attribute added successfully');
}

function cancelAddCsfAttribute() {
    clearAddCsfForm();
    const addRow = document.getElementById('csf-add-row');
    if (addRow) addRow.classList.remove('active');
}

function clearAddCsfForm() {
    const nameInput = document.getElementById('new-csf-name');
    const certificationSelect = document.getElementById('new-csf-certification');
    const typeSelect = document.getElementById('new-csf-type');
    const descriptionInput = document.getElementById('new-csf-description');
    
    if (nameInput) nameInput.value = '';
    if (certificationSelect) certificationSelect.value = '';
    if (typeSelect) typeSelect.value = '';
    if (descriptionInput) descriptionInput.value = '';
}

function generateNewCsfId() {
    // Find the highest numeric ID and increment
    const numericIds = csfAttributes.map(attr => parseInt(attr.id.replace('CSF', '')));
    const maxId = Math.max(...numericIds);
    
    // Format with leading zeros
    const newId = (maxId + 1).toString().padStart(3, '0');
    return 'CSF' + newId;
}