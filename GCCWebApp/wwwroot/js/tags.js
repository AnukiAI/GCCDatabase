// Tags data loaded from database
let tags = [];
let currentFilter = 'all';
let currentSearch = '';

// DOM Elements
const tagTable = document.getElementById('tag-table');
const tagTableBody = tagTable?.querySelector('tbody');
const tagCountElement = document.getElementById('tag-count');
const searchInput = document.getElementById('search-input');
const typeFilter = document.getElementById('type-filter');

// Sorting variables
let sortColumn = 'name';
let sortDirection = 'asc';

// Get CSRF token for POST/PUT/DELETE requests
function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

// Function to populate the table
function populateTable(data) {
    if (!tagTableBody) return;

    tagTableBody.innerHTML = '';

    if (data.length === 0) {
        const row = document.createElement('tr');
        row.className = 'admin-table-row';
        const cell = document.createElement('td');
        cell.className = 'admin-table-td admin-table-td--empty';
        cell.colSpan = 4;
        cell.textContent = 'No tags found';
        cell.style.textAlign = 'center';
        cell.style.padding = '2rem';
        row.appendChild(cell);
        tagTableBody.appendChild(row);

        if (tagCountElement) {
            tagCountElement.textContent = 'No tags found';
        }
        return;
    }

    data.forEach(tag => {
        const row = document.createElement('tr');
        row.className = 'admin-table-row';
        row.dataset.id = tag.id;

        // Tag name
        const nameCell = document.createElement('td');
        nameCell.className = 'admin-table-td tag-name';
        nameCell.textContent = tag.name;
        row.appendChild(nameCell);

        // Tag type
        const typeCell = document.createElement('td');
        typeCell.className = 'admin-table-td admin-table-td--hidden-mobile';
        const typeSpan = document.createElement('span');
        typeSpan.className = `admin-badge admin-badge--${tag.tagType === 'external' ? 'success' : 'info'}`;
        typeSpan.textContent = tag.tagType.charAt(0).toUpperCase() + tag.tagType.slice(1);
        typeCell.appendChild(typeSpan);
        row.appendChild(typeCell);

        // Description
        const descCell = document.createElement('td');
        descCell.className = 'admin-table-td admin-table-td--hidden-mobile';
        descCell.textContent = tag.description || '';
        row.appendChild(descCell);

        // Actions
        const actionsCell = document.createElement('td');
        actionsCell.className = 'admin-table-td admin-table-td--actions';
        const actionButtons = document.createElement('div');
        actionButtons.className = 'admin-action-buttons';

        const editButton = document.createElement('button');
        editButton.className = 'admin-action-btn admin-action-btn--edit';
        editButton.innerHTML = '<i class="fas fa-edit"></i>';
        editButton.title = 'Edit tag';
        editButton.setAttribute('aria-label', `Edit ${tag.name}`);
        editButton.addEventListener('click', () => openEditModal(tag));
        actionButtons.appendChild(editButton);

        const deleteButton = document.createElement('button');
        deleteButton.className = 'admin-action-btn admin-action-btn--delete';
        deleteButton.innerHTML = '<i class="fas fa-trash-alt"></i>';
        deleteButton.title = 'Delete tag';
        deleteButton.setAttribute('aria-label', `Delete ${tag.name}`);
        deleteButton.addEventListener('click', () => deleteTag(tag.id, tag.name));
        actionButtons.appendChild(deleteButton);

        actionsCell.appendChild(actionButtons);
        row.appendChild(actionsCell);

        tagTableBody.appendChild(row);
    });

    if (tagCountElement) {
        tagCountElement.textContent = `Showing ${data.length} of ${tags.length} tags`;
    }
}

// Function to initialize the table sortable headers
function initializeSortableHeaders() {
    if (!tagTable) return;

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
                h.classList.remove('admin-table-th--sorted-asc', 'admin-table-th--sorted-desc');
                h.removeAttribute('aria-sort');
            });

            header.classList.add(`admin-table-th--sorted-${sortDirection}`);
            header.setAttribute('aria-sort', sortDirection === 'asc' ? 'ascending' : 'descending');

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
    const searchTerm = currentSearch.toLowerCase();
    let filteredData = [...tags];

    // Apply search filter
    if (searchTerm) {
        filteredData = filteredData.filter(tag => {
            return (
                tag.name.toLowerCase().includes(searchTerm) ||
                tag.tagType.toLowerCase().includes(searchTerm) ||
                (tag.description && tag.description.toLowerCase().includes(searchTerm))
            );
        });
    }

    // Apply type filter
    if (currentFilter && currentFilter !== 'all') {
        filteredData = filteredData.filter(tag => tag.tagType === currentFilter);
    }

    return filteredData;
}

// Function to open the modal for adding a new tag
function openAddModal() {
    const modal = document.getElementById('tag-modal');
    const modalTitle = document.getElementById('modal-title');
    const tagForm = document.getElementById('tag-form');
    const tagIdInput = document.getElementById('tag-id');

    if (modalTitle) modalTitle.textContent = 'Add New Tag';
    if (tagForm) tagForm.reset();
    if (tagIdInput) tagIdInput.value = '';

    AdminCore.openModal('tag-modal');
}

// Function to open the modal for editing a tag
function openEditModal(tag) {
    const modalTitle = document.getElementById('modal-title');
    const tagIdInput = document.getElementById('tag-id');
    const tagNameInput = document.getElementById('tag-name');
    const tagTypeInput = document.getElementById('tag-type');
    const tagDescriptionInput = document.getElementById('tag-description');

    if (modalTitle) modalTitle.textContent = 'Edit Tag';
    if (tagIdInput) tagIdInput.value = tag.id;
    if (tagNameInput) tagNameInput.value = tag.name;
    if (tagTypeInput) tagTypeInput.value = tag.tagType;
    if (tagDescriptionInput) tagDescriptionInput.value = tag.description || '';

    AdminCore.openModal('tag-modal');
}

// Function to save a tag (add or update)
async function saveTag(event) {
    event.preventDefault();

    const tagIdInput = document.getElementById('tag-id');
    const tagNameInput = document.getElementById('tag-name');
    const tagTypeInput = document.getElementById('tag-type');
    const tagDescriptionInput = document.getElementById('tag-description');
    const saveBtn = document.getElementById('save-btn');

    const tagId = tagIdInput?.value;
    const tagData = {
        name: tagNameInput?.value || '',
        tagType: tagTypeInput?.value || '',
        description: tagDescriptionInput?.value || ''
    };

    // Validate required fields
    if (!tagData.name || !tagData.tagType) {
        AdminCore.showNotification('Please fill in all required fields', 'error');
        return;
    }

    // Show loading state
    if (saveBtn) AdminCore.showLoading(saveBtn);

    try {
        const token = getAntiForgeryToken();
        let response;

        if (tagId) {
            // Update existing tag
            response = await fetch(`/Admin/UpdateTag/${tagId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(tagData)
            });
        } else {
            // Create new tag
            response = await fetch('/Admin/CreateTag', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(tagData)
            });
        }

        const result = await response.json();

        if (result.success) {
            AdminCore.showNotification(result.message || 'Tag saved successfully!', 'success');
            AdminCore.closeModal('tag-modal');

            // Reload tags from server
            await loadTags();
        } else {
            AdminCore.showNotification(result.message || 'Failed to save tag', 'error');
        }
    } catch (error) {
        console.error('Error saving tag:', error);
        AdminCore.showNotification('An error occurred while saving the tag', 'error');
    } finally {
        if (saveBtn) AdminCore.hideLoading(saveBtn);
    }
}

// Function to delete a tag
async function deleteTag(id, name) {
    if (!confirm(`Are you sure you want to delete the tag "${name}"?`)) {
        return;
    }

    try {
        const token = getAntiForgeryToken();
        const response = await fetch(`/Admin/DeleteTag/${id}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            }
        });

        const result = await response.json();

        if (result.success) {
            AdminCore.showNotification(result.message || 'Tag deleted successfully!', 'success');

            // Reload tags from server
            await loadTags();
        } else {
            AdminCore.showNotification(result.message || 'Failed to delete tag', 'error');
        }
    } catch (error) {
        console.error('Error deleting tag:', error);
        AdminCore.showNotification('An error occurred while deleting the tag', 'error');
    }
}

// Function to load tags from database
async function loadTags() {
    try {
        const response = await fetch('/Admin/GetTags');
        const result = await response.json();

        if (result.success && result.data) {
            tags = result.data;
            sortData();
        } else {
            console.error('Failed to load tags:', result.message);
            AdminCore.showNotification('Failed to load tags', 'error');
        }
    } catch (error) {
        console.error('Error loading tags:', error);
        AdminCore.showNotification('An error occurred while loading tags', 'error');
    }
}

// Function to download import template
function downloadImportTemplate() {
    try {
        // Create template data with example row
        const templateData = [
            {
                Name: 'Sustainable',
                TagType: 'external',
                Description: 'Products with sustainability features'
            }
        ];

        // Create CSV content
        const headers = ['Name', 'TagType', 'Description'];
        const csvContent = [
            headers.join(','),
            ...templateData.map(row =>
                headers.map(header => {
                    const value = row[header] || '';
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
        link.setAttribute('download', `tags_import_template_${new Date().toISOString().split('T')[0]}.csv`);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        AdminCore.showNotification('Import template downloaded successfully', 'success');
    } catch (error) {
        console.error('Error downloading template:', error);
        AdminCore.showNotification('Error downloading template. Please try again.', 'error');
    }
}

// Function to show import modal (placeholder)
function showImportModal() {
    AdminCore.showNotification('Import functionality coming soon', 'info');
}

// Initialize tag management
function initializeTagManagement() {
    initializeSortableHeaders();
    loadTags();
}

// Event Listeners
document.addEventListener('DOMContentLoaded', function() {
    // Initialize tag management
    if (typeof initializeTagManagement === 'function') {
        initializeTagManagement();
    }

    // Add Tag button
    const addTagBtn = document.getElementById('add-tag-btn');
    if (addTagBtn) {
        addTagBtn.addEventListener('click', openAddModal);
    }

    // Cancel button
    const cancelBtn = document.getElementById('cancel-btn');
    if (cancelBtn) {
        cancelBtn.addEventListener('click', () => {
            AdminCore.closeModal('tag-modal');
        });
    }

    // Form submission
    const tagForm = document.getElementById('tag-form');
    if (tagForm) {
        tagForm.addEventListener('submit', saveTag);
    }

    // Search input
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                currentSearch = this.value;
                sortData();
                AdminCore.announceToScreenReader(`Searching for: ${this.value || 'all tags'}`);
            }, 300);
        });
    }

    // Type filter
    if (typeFilter) {
        typeFilter.addEventListener('change', function() {
            currentFilter = this.value;
            sortData();
            AdminCore.announceToScreenReader(`Filter changed to: ${this.value === 'all' ? 'All Types' : this.value}`);
        });
    }

    // Template button
    const templateBtn = document.getElementById('template-btn');
    if (templateBtn) {
        templateBtn.addEventListener('click', downloadImportTemplate);
    }

    // Import button
    const importBtn = document.getElementById('import-btn');
    if (importBtn) {
        importBtn.addEventListener('click', showImportModal);
    }

    // Export button
    const exportBtn = document.getElementById('export-btn');
    if (exportBtn) {
        exportBtn.addEventListener('click', function() {
            AdminCore.showLoading(exportBtn);

            // Prepare export data
            const exportData = tags.map(tag => ({
                id: tag.id,
                name: tag.name,
                type: tag.tagType,
                description: tag.description || ''
            }));

            const columns = [
                { key: 'id', header: 'ID' },
                { key: 'name', header: 'Tag Name' },
                { key: 'type', header: 'Tag Type' },
                { key: 'description', header: 'Description' }
            ];

            setTimeout(() => {
                AdminCore.hideLoading(exportBtn);
                if (typeof ExportUtils !== 'undefined' && ExportUtils.showExportModal) {
                    ExportUtils.showExportModal(
                        exportData,
                        columns,
                        'tags',
                        (message, type) => AdminCore.showNotification(message, type)
                    );
                }
            }, 500);
        });
    }
});

// Export functions for use in inline scripts
window.initializeTagManagement = initializeTagManagement;
window.refreshTagList = loadTags;
window.handleTagSearch = function(value) {
    currentSearch = value;
    sortData();
};
window.handleTagFilter = function(value) {
    currentFilter = value;
    sortData();
};
