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

    // Clear existing data rows but keep the add row if it exists
    const addRow = document.getElementById('tags-add-row');
    tagTableBody.innerHTML = '';
    if (addRow) {
        tagTableBody.appendChild(addRow);
    }

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

        // Tag name cell with view and edit modes
        const nameCell = document.createElement('td');
        nameCell.className = 'admin-table-td';

        // View mode
        const nameSpan = document.createElement('span');
        nameSpan.className = 'tag-name';
        nameSpan.textContent = tag.name;
        nameCell.appendChild(nameSpan);

        // Edit mode
        const nameEditDiv = document.createElement('div');
        nameEditDiv.className = 'tag-name-edit';
        nameEditDiv.innerHTML = `<input type="text" class="tags-edit-input" value="${escapeHtml(tag.name)}">`;
        nameCell.appendChild(nameEditDiv);

        row.appendChild(nameCell);

        // Tag type cell with view and edit modes
        const typeCell = document.createElement('td');
        typeCell.className = 'admin-table-td admin-table-td--hidden-mobile';

        // View mode
        const typeSpan = document.createElement('span');
        typeSpan.className = `tag-type admin-badge admin-badge--${tag.tagType === 'external' ? 'success' : 'info'}`;
        typeSpan.textContent = tag.tagType.charAt(0).toUpperCase() + tag.tagType.slice(1);
        typeCell.appendChild(typeSpan);

        // Edit mode
        const typeEditDiv = document.createElement('div');
        typeEditDiv.className = 'tag-type-edit';
        typeEditDiv.innerHTML = `
            <select class="tags-edit-select">
                <option value="external" ${tag.tagType === 'external' ? 'selected' : ''}>External</option>
                <option value="internal" ${tag.tagType === 'internal' ? 'selected' : ''}>Internal</option>
            </select>
        `;
        typeCell.appendChild(typeEditDiv);

        row.appendChild(typeCell);

        // Description cell with view and edit modes
        const descCell = document.createElement('td');
        descCell.className = 'admin-table-td admin-table-td--hidden-mobile';

        // View mode
        const descSpan = document.createElement('span');
        descSpan.className = 'tag-description';
        descSpan.textContent = tag.description || '';
        descCell.appendChild(descSpan);

        // Edit mode
        const descEditDiv = document.createElement('div');
        descEditDiv.className = 'tag-description-edit';
        descEditDiv.innerHTML = `<textarea class="tags-edit-textarea" rows="2">${escapeHtml(tag.description || '')}</textarea>`;
        descCell.appendChild(descEditDiv);

        row.appendChild(descCell);

        // Actions cell with Edit/Delete and Save/Cancel buttons
        const actionsCell = document.createElement('td');
        actionsCell.className = 'admin-table-td admin-table-td--actions';

        // Regular action buttons (Edit/Delete)
        const actionButtons = document.createElement('div');
        actionButtons.className = 'action-buttons';
        actionButtons.innerHTML = `
            <button class="admin-action-btn admin-action-btn--edit" onclick="editTag(${tag.id})" title="Edit Tag" aria-label="Edit tag ${escapeHtml(tag.name)}">
                <i class="fas fa-edit" aria-hidden="true"></i>
            </button>
            <button class="admin-action-btn admin-action-btn--delete" onclick="deleteTag(${tag.id}, '${escapeHtml(tag.name)}')" title="Delete Tag" aria-label="Delete tag ${escapeHtml(tag.name)}">
                <i class="fas fa-trash-alt" aria-hidden="true"></i>
            </button>
        `;
        actionsCell.appendChild(actionButtons);

        // Edit action buttons (Save/Cancel)
        const editActions = document.createElement('div');
        editActions.className = 'tags-edit-actions';
        editActions.innerHTML = `
            <button class="admin-btn admin-btn--sm admin-btn--secondary" onclick="cancelEdit(${tag.id})" aria-label="Cancel editing">
                <i class="fas fa-times" aria-hidden="true"></i> Cancel
            </button>
            <button class="admin-btn admin-btn--sm admin-btn--success" onclick="saveTag(${tag.id})" aria-label="Save tag">
                <i class="fas fa-check" aria-hidden="true"></i> Save
            </button>
        `;
        actionsCell.appendChild(editActions);

        row.appendChild(actionsCell);

        tagTableBody.appendChild(row);
    });

    if (tagCountElement) {
        tagCountElement.textContent = `Showing ${data.length} of ${tags.length} tags`;
    }
}

// Helper function to escape HTML
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
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

// Function to show the add tag row (inline)
function showAddTagRow() {
    const addRow = document.getElementById('tags-add-row');
    if (addRow) {
        addRow.classList.add('active');
        const nameInput = addRow.querySelector('.tags-edit-input');
        if (nameInput) {
            nameInput.focus();
        }
    }
}

// Function to save new tag from add row
async function saveNewTag() {
    const addRow = document.getElementById('tags-add-row');
    if (!addRow) return;

    const nameInput = addRow.querySelector('input[name="new-tag-name"]');
    const typeSelect = addRow.querySelector('select[name="new-tag-type"]');
    const descTextarea = addRow.querySelector('textarea[name="new-tag-description"]');

    const name = nameInput?.value.trim() || '';
    const tagType = typeSelect?.value || '';
    const description = descTextarea?.value.trim() || '';

    if (!name) {
        AdminCore.showNotification('Tag name cannot be empty', 'error');
        if (nameInput) nameInput.focus();
        return;
    }

    if (!tagType) {
        AdminCore.showNotification('Please select a tag type', 'error');
        if (typeSelect) typeSelect.focus();
        return;
    }

    try {
        const token = getAntiForgeryToken();
        const response = await fetch('/Admin/CreateTag', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ name, tagType, description })
        });

        const result = await response.json();

        if (result.success) {
            // Reset add row
            if (nameInput) nameInput.value = '';
            if (typeSelect) typeSelect.value = '';
            if (descTextarea) descTextarea.value = '';
            addRow.classList.remove('active');

            // Reload tags from server
            await loadTags();
            AdminCore.showNotification('Tag added successfully', 'success');
        } else {
            AdminCore.showNotification(result.message || 'Failed to create tag', 'error');
        }
    } catch (error) {
        console.error('Error creating tag:', error);
        AdminCore.showNotification('An error occurred while creating the tag', 'error');
    }
}

// Function to cancel adding new tag
function cancelAddTag() {
    const addRow = document.getElementById('tags-add-row');
    if (!addRow) return;

    const nameInput = addRow.querySelector('input[name="new-tag-name"]');
    const typeSelect = addRow.querySelector('select[name="new-tag-type"]');
    const descTextarea = addRow.querySelector('textarea[name="new-tag-description"]');

    if (nameInput) nameInput.value = '';
    if (typeSelect) typeSelect.value = '';
    if (descTextarea) descTextarea.value = '';

    addRow.classList.remove('active');
}

// Function to edit a tag (inline)
function editTag(id) {
    const row = document.querySelector(`tr[data-id="${id}"]`);
    if (!row) return;

    row.classList.add('tags-edit-row');
    const nameInput = row.querySelector('.tags-edit-input');
    if (nameInput) {
        nameInput.focus();
        nameInput.select();
    }
}

// Function to save a tag (inline editing)
async function saveTag(id) {
    const row = document.querySelector(`tr[data-id="${id}"]`);
    if (!row) return;

    const nameInput = row.querySelector('.tag-name-edit input');
    const typeSelect = row.querySelector('.tag-type-edit select');
    const descTextarea = row.querySelector('.tag-description-edit textarea');

    const newName = nameInput?.value.trim() || '';
    const newType = typeSelect?.value || '';
    const newDescription = descTextarea?.value.trim() || '';

    if (!newName) {
        AdminCore.showNotification('Tag name cannot be empty', 'error');
        if (nameInput) nameInput.focus();
        return;
    }

    if (!newType) {
        AdminCore.showNotification('Please select a tag type', 'error');
        if (typeSelect) typeSelect.focus();
        return;
    }

    // Get current tag data
    const currentTag = tags.find(t => t.id === id);
    if (!currentTag) {
        AdminCore.showNotification('Tag not found', 'error');
        return;
    }

    try {
        const token = getAntiForgeryToken();
        const response = await fetch(`/Admin/UpdateTag/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                id: id,
                name: newName,
                tagType: newType,
                description: newDescription
            })
        });

        const result = await response.json();

        if (result.success) {
            // Update local data
            const tagIndex = tags.findIndex(t => t.id === id);
            if (tagIndex !== -1) {
                tags[tagIndex].name = newName;
                tags[tagIndex].tagType = newType;
                tags[tagIndex].description = newDescription;
            }

            // Update display
            const nameSpan = row.querySelector('.tag-name');
            const typeSpan = row.querySelector('.tag-type');
            const descSpan = row.querySelector('.tag-description');

            if (nameSpan) nameSpan.textContent = newName;
            if (typeSpan) {
                typeSpan.textContent = newType.charAt(0).toUpperCase() + newType.slice(1);
                typeSpan.className = `tag-type admin-badge admin-badge--${newType === 'external' ? 'success' : 'info'}`;
            }
            if (descSpan) descSpan.textContent = newDescription;

            // Remove edit class
            row.classList.remove('tags-edit-row');
            AdminCore.showNotification('Tag updated successfully', 'success');
        } else {
            AdminCore.showNotification(result.message || 'Failed to update tag', 'error');
        }
    } catch (error) {
        console.error('Error updating tag:', error);
        AdminCore.showNotification('An error occurred while updating the tag', 'error');
    }
}

// Function to cancel editing (inline)
function cancelEdit(id) {
    const row = document.querySelector(`tr[data-id="${id}"]`);
    if (!row) return;

    // Get original tag data
    const tag = tags.find(t => t.id === id);
    if (!tag) return;

    // Reset input values to original
    const nameInput = row.querySelector('.tag-name-edit input');
    const typeSelect = row.querySelector('.tag-type-edit select');
    const descTextarea = row.querySelector('.tag-description-edit textarea');

    if (nameInput) nameInput.value = tag.name;
    if (typeSelect) typeSelect.value = tag.tagType;
    if (descTextarea) descTextarea.value = tag.description || '';

    // Remove edit class
    row.classList.remove('tags-edit-row');
}

// Function to delete a tag
async function deleteTag(id, name) {
    // Use custom confirmation modal instead of native confirm()
    const confirmed = await AdminCore.showConfirmation({
        title: 'Delete Confirmation',
        message: `Are you sure you want to delete the tag "${name}"? This action cannot be undone.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        type: 'danger'
    });

    if (!confirmed) {
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

    // Add Tag button - show inline add row instead of modal
    const addTagBtn = document.getElementById('add-tag-btn');
    if (addTagBtn) {
        addTagBtn.addEventListener('click', showAddTagRow);
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

    // Export button - CSV only
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
                // Direct CSV export only
                if (typeof ExportUtils !== 'undefined' && ExportUtils.exportToCSV) {
                    ExportUtils.exportToCSV(
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

// Export functions for use in inline scripts and onclick handlers
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
window.showAddTagRow = showAddTagRow;
window.saveNewTag = saveNewTag;
window.cancelAddTag = cancelAddTag;
window.editTag = editTag;
window.saveTag = saveTag;
window.cancelEdit = cancelEdit;
window.deleteTag = deleteTag;
