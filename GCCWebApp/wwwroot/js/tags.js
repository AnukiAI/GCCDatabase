// Tags data loaded from database
let tags = [];

// DOM Elements
const tagTable = document.getElementById('tag-table');
const tagTableBody = tagTable.querySelector('tbody');
const tagCountElement = document.getElementById('tag-count');
const searchInput = document.getElementById('search-input');
const addTagBtn = document.getElementById('add-tag-btn');
const importBtn = document.getElementById('import-btn');
const exportBtn = document.getElementById('export-btn');

const modal = document.getElementById('tag-modal');
const modalTitle = document.getElementById('modal-title');
const modalClose = document.querySelector('.modal-close');
const cancelBtn = document.getElementById('cancel-btn');
const tagForm = document.getElementById('tag-form');
const tagIdInput = document.getElementById('tag-id');
const tagNameInput = document.getElementById('tag-name');
const tagTypeInput = document.getElementById('tag-type');
const tagDescriptionInput = document.getElementById('tag-description');

// Sorting variables
let sortColumn = 'name';
let sortDirection = 'asc';

// Function to populate the table
function populateTable(data) {
    tagTableBody.innerHTML = '';

    data.forEach(tag => {
        const row = document.createElement('tr');
        row.dataset.id = tag.id;

        // Tag name
        const nameCell = document.createElement('td');
        nameCell.textContent = tag.name;
        row.appendChild(nameCell);

        // Tag type
        const typeCell = document.createElement('td');
        const typeSpan = document.createElement('span');
        typeSpan.className = `tag-type-badge ${tag.type}`;
        typeSpan.textContent = tag.type.charAt(0).toUpperCase() + tag.type.slice(1);
        typeCell.appendChild(typeSpan);
        row.appendChild(typeCell);

        // Description
        const descCell = document.createElement('td');
        descCell.textContent = tag.description || '';
        row.appendChild(descCell);

        // Actions
        const actionsCell = document.createElement('td');
        const actionButtons = document.createElement('div');
        actionButtons.className = 'action-buttons';

        const editButton = document.createElement('button');
        editButton.className = 'action-btn';
        editButton.innerHTML = '<i class="fas fa-edit"></i>';
        editButton.title = 'Edit';
        editButton.addEventListener('click', () => openEditModal(tag));
        actionButtons.appendChild(editButton);

        const deleteButton = document.createElement('button');
        deleteButton.className = 'action-btn';
        deleteButton.innerHTML = '<i class="fas fa-trash-alt"></i>';
        deleteButton.title = 'Delete';
        deleteButton.addEventListener('click', () => deleteTag(tag.id));
        actionButtons.appendChild(deleteButton);

        actionsCell.appendChild(actionButtons);
        row.appendChild(actionsCell);

        tagTableBody.appendChild(row);
    });

    tagCountElement.textContent = `Showing ${data.length} of ${tags.length} tags`;
}

// Function to save inline edits
function saveInlineEdit(id, field, value) {
    const tagIndex = tags.findIndex(t => t.id === id);
    if (tagIndex !== -1) {
        tags[tagIndex][field] = value;
        sortData();
    }
}

// Function to initialize the table sortable headers
function initializeSortableHeaders() {
    const headers = tagTable.querySelectorAll('th[data-sort]');

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

            // Sort the data
            sortData();
        });
    });
}

// Function to sort the data
function sortData() {
    const filteredData = getFilteredData();

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

// Function to filter the data based on search input
function getFilteredData() {
    const searchTerm = searchInput.value.toLowerCase();
    const typeFilter = document.getElementById('type-filter').value;

    let filteredData = [...tags];

    // Apply search filter
    if (searchTerm) {
        filteredData = filteredData.filter(tag => {
            return (
                tag.name.toLowerCase().includes(searchTerm) ||
                tag.type.toLowerCase().includes(searchTerm) ||
                (tag.description && tag.description.toLowerCase().includes(searchTerm))
            );
        });
    }

    // Apply type filter
    if (typeFilter && typeFilter !== 'all') {
        filteredData = filteredData.filter(tag => tag.type === typeFilter);
    }

    return filteredData;
}

// Function to open the modal for adding a new tag
function openAddModal() {
    modalTitle.textContent = 'Add New Tag';
    tagForm.reset();
    tagIdInput.value = '';
    modal.style.display = 'block';
}

// Function to open the modal for editing a tag
function openEditModal(tag) {
    modalTitle.textContent = 'Edit Tag';
    tagIdInput.value = tag.id;
    tagNameInput.value = tag.name;
    tagTypeInput.value = tag.type;
    tagDescriptionInput.value = tag.description || '';
    modal.style.display = 'block';
}

// Function to close the modal
function closeModal() {
    modal.style.display = 'none';
}

// Function to save a tag (add or update)
function saveTag(event) {
    event.preventDefault();

    const tagId = tagIdInput.value;
    const tagData = {
        name: tagNameInput.value,
        type: tagTypeInput.value,
        description: tagDescriptionInput.value
    };

    if (tagId) {
        // Update existing tag
        const index = tags.findIndex(t => t.id === tagId);
        if (index !== -1) {
            tags[index] = {
                ...tags[index],
                ...tagData
            };
        }
    } else {
        // Add new tag - Create a new ID
        const newId = generateNewId();
        tags.push({
            id: newId,
            ...tagData
        });
    }

    closeModal();
    sortData();
}

// Function to generate a new Tag ID
function generateNewId() {
    // Find the highest numeric ID and increment
    const numericIds = tags.map(tag => parseInt(tag.id));
    const maxId = Math.max(...numericIds);

    // Format with leading zero if needed
    const newId = (maxId + 1).toString().padStart(2, '0');
    return newId;
}

// Function to delete a tag
function deleteTag(id) {
    if (confirm('Are you sure you want to delete this tag?')) {
        tags = tags.filter(tag => tag.id !== id);
        sortData();
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
            const newTags = [];

            for (let i = 1; i < lines.length; i++) {
                const line = lines[i].trim();
                if (!line) continue;

                const fields = line.split(',');
                if (fields.length < 3) continue;

                newTags.push({
                    id: fields[0].trim(),
                    name: fields[1].trim(),
                    type: fields[2].trim(),
                    products: parseInt(fields[3]) || 0
                });
            }

            // Merge with existing data
            const existingIds = tags.map(t => t.id);
            const filteredNewTags = newTags.filter(t => !existingIds.includes(t.id));

            tags = [...tags, ...filteredNewTags];
            sortData();
        };

        reader.readAsText(file);
    });

    fileInput.click();
}

// Function to export to CSV
function exportCSV() {
    if (tags.length === 0) {
        alert('No tags to export');
        return;
    }

    // Create CSV header
    const header = 'Tag ID,Tag Name,Tag Type,Description\n';
    
    // Create CSV rows - properly escape fields that contain commas or quotes
    const rows = tags.map(tag => {
        const id = tag.id;
        const name = tag.name.includes(',') || tag.name.includes('"') 
            ? `"${tag.name.replace(/"/g, '""')}"` 
            : tag.name;
        const type = tag.type;
        const description = (tag.description || '').includes(',') || (tag.description || '').includes('"')
            ? `"${(tag.description || '').replace(/"/g, '""')}"`
            : (tag.description || '');
        
        return `${id},${name},${type},${description}`;
    }).join('\n');

    const csv = header + rows;

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.setAttribute('href', url);
    link.setAttribute('download', `tags_${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

// Event Listeners
addTagBtn.addEventListener('click', openAddModal);
exportBtn.addEventListener('click', exportCSV);
modalClose.addEventListener('click', closeModal);
cancelBtn.addEventListener('click', closeModal);
tagForm.addEventListener('submit', saveTag);

searchInput.addEventListener('input', () => {
    sortData();
});

// Add type filter listener
const typeFilter = document.getElementById('type-filter');
if (typeFilter) {
    typeFilter.addEventListener('change', () => {
        sortData();
    });
}

// Close modal when clicking outside
window.addEventListener('click', (event) => {
    if (event.target === modal) {
        closeModal();
    }
});

// Function to load tags from database
async function loadTags() {
    try {
        const response = await fetch('/Admin/GetTags');
        const result = await response.json();

        if (result.success && result.data) {
            // Map database model to our UI model
            tags = result.data.map(tag => ({
                id: tag.id.toString(),
                name: tag.name,
                type: tag.tagType,
                description: tag.description || '',
                productCount: tag.productCount || 0
            }));
            sortData();
        } else {
            console.error('Failed to load tags:', result.message);
        }
    } catch (error) {
        console.error('Error loading tags:', error);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeSortableHeaders();
    loadTags(); // Load from database instead of using hardcoded data
});