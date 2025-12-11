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
        nameEditDiv.className = 'tag-name-edit admin-inline-edit';
        nameEditDiv.innerHTML = `<input type="text" class="admin-inline-input" value="${escapeHtml(tag.name)}">`;
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
        typeEditDiv.className = 'tag-type-edit admin-inline-edit';
        typeEditDiv.innerHTML = `
            <select class="admin-inline-select">
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
        descEditDiv.className = 'tag-description-edit admin-inline-edit';
        descEditDiv.innerHTML = `<textarea class="admin-inline-textarea" rows="2">${escapeHtml(tag.description || '')}</textarea>`;
        descCell.appendChild(descEditDiv);

        row.appendChild(descCell);

        // Actions cell with Edit/Delete and Save/Cancel buttons
        const actionsCell = document.createElement('td');
        actionsCell.className = 'admin-table-td admin-table-td--actions';

        // Regular action buttons (Edit/Delete)
        const actionButtons = document.createElement('div');
        actionButtons.className = 'admin-inline-actions';
        actionButtons.innerHTML = `
            <button class="admin-inline-action-btn admin-inline-action-btn--edit" onclick="editTag(${tag.id})" title="Edit Tag" aria-label="Edit tag ${escapeHtml(tag.name)}">
                <i class="fas fa-edit" aria-hidden="true"></i>
            </button>
            <button class="admin-inline-action-btn admin-inline-action-btn--delete" onclick="deleteTag(${tag.id}, '${escapeHtml(tag.name)}')" title="Delete Tag" aria-label="Delete tag ${escapeHtml(tag.name)}">
                <i class="fas fa-trash-alt" aria-hidden="true"></i>
            </button>
        `;
        actionsCell.appendChild(actionButtons);

        // Edit action buttons (Save/Cancel)
        const editActions = document.createElement('div');
        editActions.className = 'admin-inline-edit-actions';
        editActions.innerHTML = `
            <button type="button" class="admin-inline-btn-cancel" onclick="cancelEdit(${tag.id})" aria-label="Cancel editing">
                <i class="fas fa-times" aria-hidden="true"></i> Cancel
            </button>
            <button type="button" class="admin-inline-btn-save" onclick="saveTag(${tag.id})" aria-label="Save tag">
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
        const nameInput = addRow.querySelector('.admin-inline-input');
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

    row.classList.add('admin-inline-edit-row');
    const nameInput = row.querySelector('.admin-inline-input');
    if (nameInput) {
        nameInput.focus();
        nameInput.select();
    }
}

// Function to save a tag (inline editing)
async function saveTag(id) {
    const row = document.querySelector(`tr[data-id="${id}"]`);
    if (!row) return;

    const nameInput = row.querySelector('.tag-name-edit .admin-inline-input');
    const typeSelect = row.querySelector('.tag-type-edit .admin-inline-select');
    const descTextarea = row.querySelector('.tag-description-edit .admin-inline-textarea');

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
            row.classList.remove('admin-inline-edit-row');
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
    const nameInput = row.querySelector('.tag-name-edit .admin-inline-input');
    const typeSelect = row.querySelector('.tag-type-edit .admin-inline-select');
    const descTextarea = row.querySelector('.tag-description-edit .admin-inline-textarea');

    if (nameInput) nameInput.value = tag.name;
    if (typeSelect) typeSelect.value = tag.tagType;
    if (descTextarea) descTextarea.value = tag.description || '';

    // Remove edit class
    row.classList.remove('admin-inline-edit-row');
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

// Import preview state
let importPreviewData = [];
let importFileName = '';

// Function to handle CSV import with preview
async function showImportModal() {
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.accept = '.csv';

    fileInput.addEventListener('change', async (event) => {
        const file = event.target.files[0];
        if (!file) return;

        importFileName = file.name;

        const reader = new FileReader();
        reader.onload = async (e) => {
            const contents = e.target.result;
            const lines = contents.split('\n').filter(line => line.trim());

            if (lines.length < 2) {
                AdminCore.showNotification('CSV file must have a header row and at least one data row', 'warning');
                return;
            }

            // Parse and normalize headers
            const rawHeaders = parseCSVLine(lines[0]).map(h => h.trim());
            const headers = rawHeaders.map(h => normalizeTagHeaderName(h));

            const parsedTags = [];

            for (let i = 1; i < lines.length; i++) {
                const line = lines[i].trim();
                if (!line) continue;

                const fields = parseCSVLine(line);
                if (fields.length < 1) continue;

                // Build tag object from headers
                const tag = {};
                headers.forEach((header, index) => {
                    tag[header] = fields[index]?.trim() || '';
                });

                const name = tag.name || '';
                if (!name) continue;

                let tagType = (tag.tagtype || 'external').toLowerCase();
                if (tagType !== 'external' && tagType !== 'internal') {
                    tagType = 'external';
                }

                parsedTags.push({
                    rowNumber: i,
                    name: name,
                    tagType: tagType,
                    description: tag.description || ''
                });
            }

            if (parsedTags.length === 0) {
                AdminCore.showNotification('No valid tags found in CSV file', 'warning');
                return;
            }

            // Compare with existing tags and build preview
            await buildImportPreview(parsedTags);
        };

        reader.readAsText(file);
    });

    fileInput.click();
}

// Build import preview by comparing with existing tags
async function buildImportPreview(parsedTags) {
    importPreviewData = [];

    for (const importTag of parsedTags) {
        // Match by name (case-insensitive)
        const existingTag = tags.find(t =>
            t.name && importTag.name &&
            t.name.trim().toLowerCase() === importTag.name.trim().toLowerCase()
        );

        const previewRow = {
            rowNumber: importTag.rowNumber,
            data: importTag,
            existingData: existingTag || null,
            status: 'new',
            changes: [],
            errors: [],
            selected: true,
            action: 'create'
        };

        if (existingTag) {
            // Compare fields and determine status - normalize for comparison
            const changes = [];

            const existingType = (existingTag.tagType || '').trim().toLowerCase();
            const importType = (importTag.tagType || '').trim().toLowerCase();
            if (existingType !== importType) {
                changes.push({
                    field: 'tagType',
                    oldValue: existingTag.tagType || '',
                    newValue: importTag.tagType || ''
                });
            }

            const existingDesc = (existingTag.description || '').trim();
            const importDesc = (importTag.description || '').trim();
            if (existingDesc !== importDesc) {
                changes.push({
                    field: 'description',
                    oldValue: existingTag.description || '',
                    newValue: importTag.description || ''
                });
            }

            if (changes.length > 0) {
                previewRow.status = 'modified';
                previewRow.changes = changes;
                previewRow.action = 'update';
            } else {
                previewRow.status = 'unchanged';
                previewRow.selected = false;
                previewRow.action = 'skip';
            }
        }

        // Validate
        if (!importTag.name || !importTag.name.trim()) {
            previewRow.status = 'error';
            previewRow.errors.push('Tag name is required');
            previewRow.selected = false;
        }
        // Validate tag type
        const normalizedType = (importTag.tagType || '').trim().toLowerCase();
        if (normalizedType && normalizedType !== 'external' && normalizedType !== 'internal') {
            previewRow.errors.push(`Invalid tag type: "${importTag.tagType}" (must be external or internal)`);
            if (previewRow.errors.length > 0) {
                previewRow.status = 'error';
                previewRow.selected = false;
            }
        }

        importPreviewData.push(previewRow);
    }

    // Show preview modal
    showImportPreviewModal();
}

// Display the import preview modal
function showImportPreviewModal() {
    console.log('showImportPreviewModal called, data:', importPreviewData);
    const modal = document.getElementById('import-preview-modal');
    console.log('Modal element:', modal);
    if (!modal) {
        console.error('Modal element not found!');
        return;
    }

    // Calculate statistics
    const stats = {
        new: importPreviewData.filter(r => r.status === 'new').length,
        modified: importPreviewData.filter(r => r.status === 'modified').length,
        unchanged: importPreviewData.filter(r => r.status === 'unchanged').length,
        errors: importPreviewData.filter(r => r.status === 'error').length
    };

    // Update stats display
    document.getElementById('stat-new').textContent = stats.new;
    document.getElementById('stat-modified').textContent = stats.modified;
    document.getElementById('stat-unchanged').textContent = stats.unchanged;
    document.getElementById('stat-errors').textContent = stats.errors;

    // Build table rows
    const tbody = document.getElementById('import-preview-body');
    tbody.innerHTML = '';

    importPreviewData.forEach((row, index) => {
        const tr = document.createElement('tr');
        tr.dataset.index = index;

        // Status colors and badges
        const statusColors = {
            'new': { bg: '#e8f5e1', color: '#447638', label: 'New' },
            'modified': { bg: '#e6f7ff', color: '#002B76', label: 'Modified' },
            'unchanged': { bg: '#f5f5f5', color: '#999', label: 'Unchanged' },
            'error': { bg: '#ffebee', color: '#ff5252', label: 'Error' }
        };
        const statusStyle = statusColors[row.status];

        // Row background based on status
        tr.style.backgroundColor = row.status === 'error' ? '#fff5f5' : (row.status === 'unchanged' ? '#fafafa' : '');

        // Build changes summary for modified rows
        let changesSummary = '';
        if (row.status === 'modified' && row.changes.length > 0) {
            const fieldNames = row.changes.map(c => c.field === 'tagType' ? 'Type' : (c.field === 'description' ? 'Description' : c.field));
            changesSummary = `<div style="font-size: 10px; color: #666; margin-top: 4px;">${fieldNames.join(', ')} changed</div>`;
        }

        tr.innerHTML = `
            <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                <input type="checkbox" class="import-row-checkbox"
                       data-index="${index}"
                       ${row.selected ? 'checked' : ''}
                       ${row.status === 'error' || row.status === 'unchanged' ? 'disabled' : ''}
                       onchange="updateImportRowSelection(${index}, this.checked)">
            </td>
            <td style="padding: 10px; border-bottom: 1px solid #e9ecef; font-family: monospace; vertical-align: top;">${row.rowNumber}</td>
            <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                <span style="display: inline-block; padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: 500; background: ${statusStyle.bg}; color: ${statusStyle.color};">
                    ${statusStyle.label}
                </span>
                ${changesSummary}
            </td>
            <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                ${renderFieldWithDiff(row, 'name')}
            </td>
            <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                ${renderFieldWithDiff(row, 'tagType')}
            </td>
            <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                ${renderFieldWithDiff(row, 'description')}
                ${row.errors.length > 0 ? `<div style="color: #ff5252; font-size: 11px; margin-top: 4px; font-weight: 500;">${row.errors.join('<br>')}</div>` : ''}
            </td>
            <td style="padding: 10px; border-bottom: 1px solid #e9ecef; vertical-align: top;">
                ${renderActionDropdown(row, index)}
            </td>
        `;

        tbody.appendChild(tr);
    });

    // Update counts
    updateSelectedCount();

    // Show modal - use setProperty with important to override any CSS
    console.log('Setting modal display to block');
    modal.style.setProperty('display', 'block', 'important');
    console.log('Modal display after setting:', modal.style.display);
}

// Render field value with diff highlighting for modified rows
function renderFieldWithDiff(row, field) {
    const newValue = row.data[field] || '';

    if (row.status === 'modified' && row.existingData) {
        const oldValue = row.existingData[field] || '';
        // Check if this specific field changed (case-insensitive comparison for field name)
        const fieldChanged = row.changes.some(c => c.field.toLowerCase() === field.toLowerCase());

        if (fieldChanged) {
            // Show stacked comparison for conflicts (more readable)
            return `
                <div class="field-conflict" style="min-width: 120px;">
                    <div style="display: flex; flex-direction: column; gap: 4px;">
                        <div>
                            <div style="font-size: 10px; color: #856404; font-weight: 600; text-transform: uppercase;">Current:</div>
                            <div style="padding: 4px 8px; background: #fff3cd; border: 1px solid #ffc107; border-radius: 4px; font-size: 12px; word-break: break-word;">${escapeHtml(oldValue || '(empty)')}</div>
                        </div>
                        <div>
                            <div style="font-size: 10px; color: #155724; font-weight: 600; text-transform: uppercase;">Import:</div>
                            <div style="padding: 4px 8px; background: #d4edda; border: 1px solid #28a745; border-radius: 4px; font-size: 12px; word-break: break-word;">${escapeHtml(newValue || '(empty)')}</div>
                        </div>
                    </div>
                </div>
            `;
        }
    }

    // For unchanged fields or new rows, just show the value
    return `<span style="word-break: break-word;">${escapeHtml(newValue)}</span>`;
}

// Render action dropdown for each row
function renderActionDropdown(row, index) {
    if (row.status === 'error') {
        return '<span style="color: #999; font-size: 12px;">Cannot import</span>';
    }

    if (row.status === 'new') {
        return `
            <select class="import-action-select" data-index="${index}" onchange="updateImportRowAction(${index}, this.value)" style="padding: 4px 8px; border: 1px solid #ddd; border-radius: 4px; font-size: 12px;">
                <option value="create" ${row.action === 'create' ? 'selected' : ''}>Create</option>
                <option value="skip" ${row.action === 'skip' ? 'selected' : ''}>Skip</option>
            </select>
        `;
    }

    if (row.status === 'modified') {
        return `
            <div style="display: flex; flex-direction: column; gap: 4px;">
                <select class="import-action-select" data-index="${index}" onchange="updateImportRowAction(${index}, this.value)" style="padding: 6px 10px; border: 2px solid #002B76; border-radius: 4px; font-size: 12px; font-weight: 500; background: white;">
                    <option value="update" ${row.action === 'update' ? 'selected' : ''}>Use Import Data</option>
                    <option value="keep" ${row.action === 'keep' ? 'selected' : ''}>Keep Current</option>
                    <option value="skip" ${row.action === 'skip' ? 'selected' : ''}>Skip Row</option>
                </select>
                <span style="font-size: 10px; color: #666; text-align: center;">
                    ${row.action === 'update' ? 'Will overwrite' : row.action === 'keep' ? 'No changes' : 'Ignored'}
                </span>
            </div>
        `;
    }

    if (row.status === 'unchanged') {
        return '<span style="color: #999; font-size: 12px;">No changes</span>';
    }

    return '';
}

// Update row selection
function updateImportRowSelection(index, selected) {
    importPreviewData[index].selected = selected;
    if (!selected) {
        importPreviewData[index].action = 'skip';
    } else {
        // Restore default action based on status
        if (importPreviewData[index].status === 'new') {
            importPreviewData[index].action = 'create';
        } else if (importPreviewData[index].status === 'modified') {
            importPreviewData[index].action = 'update';
        }
    }
    updateSelectedCount();
}

// Update row action
function updateImportRowAction(index, action) {
    importPreviewData[index].action = action;
    // "keep" means we're acknowledging the row but not changing anything
    importPreviewData[index].selected = (action !== 'skip' && action !== 'keep');

    // Update checkbox to match
    const checkbox = document.querySelector(`input.import-row-checkbox[data-index="${index}"]`);
    if (checkbox) {
        checkbox.checked = (action !== 'skip' && action !== 'keep');
    }

    // Update the row's visual state based on action
    const tr = document.querySelector(`tr[data-index="${index}"]`);
    if (tr) {
        // Update row background color and opacity
        if (action === 'skip') {
            tr.style.backgroundColor = '#f5f5f5';
            tr.style.opacity = '0.7';
        } else if (action === 'keep') {
            tr.style.backgroundColor = '#fafafa';
            tr.style.opacity = '0.9';
        } else {
            tr.style.backgroundColor = '';
            tr.style.opacity = '1';
        }

        // Re-render the action dropdown to update the status text
        const actionCell = tr.querySelector('td:last-child');
        if (actionCell) {
            actionCell.innerHTML = renderActionDropdown(importPreviewData[index], index);
        }
    }

    updateSelectedCount();
}

// Toggle all import rows
function toggleAllImportRows(checkbox) {
    const checked = checkbox.checked;
    importPreviewData.forEach((row, index) => {
        if (row.status !== 'error' && row.status !== 'unchanged') {
            row.selected = checked;
            row.action = checked ? (row.status === 'new' ? 'create' : 'update') : 'skip';

            const rowCheckbox = document.querySelector(`input.import-row-checkbox[data-index="${index}"]`);
            if (rowCheckbox) {
                rowCheckbox.checked = checked;
            }
        }
    });
    updateSelectedCount();
}

// Update selected count display
function updateSelectedCount() {
    // Count rows that will actually be processed (create or update actions)
    const toProcessCount = importPreviewData.filter(r =>
        r.action === 'create' || r.action === 'update'
    ).length;
    const keptCount = importPreviewData.filter(r => r.action === 'keep').length;
    const totalActionable = importPreviewData.filter(r =>
        r.status !== 'error' && r.status !== 'unchanged'
    ).length;

    const selectedCountEl = document.getElementById('selected-count');
    const totalCountEl = document.getElementById('total-count');

    if (selectedCountEl) selectedCountEl.textContent = toProcessCount;
    if (totalCountEl) totalCountEl.textContent = totalActionable;

    // Update the label to be more descriptive
    const countContainer = document.querySelector('.import-selected-count');
    if (countContainer) {
        let label = `${toProcessCount} of ${totalActionable} rows will be imported`;
        if (keptCount > 0) {
            label += ` (${keptCount} keeping current)`;
        }
        countContainer.innerHTML = label;
    }

    // Disable apply button if nothing to process
    const applyBtn = document.getElementById('apply-import-btn');
    if (applyBtn) {
        const hasChanges = toProcessCount > 0;
        applyBtn.disabled = !hasChanges;
        applyBtn.style.opacity = hasChanges ? '1' : '0.5';
        applyBtn.textContent = hasChanges
            ? `Apply Import (${toProcessCount})`
            : 'Apply Import';
    }
}

// Close the import preview modal
function closeImportPreviewModal() {
    const modal = document.getElementById('import-preview-modal');
    if (modal) {
        modal.style.setProperty('display', 'none', 'important');
    }
    importPreviewData = [];
    importFileName = '';
}

// Apply selected imports
async function applySelectedImport() {
    // Only process rows that are creating or updating (not skip, keep, or error)
    const rowsToProcess = importPreviewData.filter(r =>
        r.action === 'create' || r.action === 'update'
    );

    const keptCount = importPreviewData.filter(r => r.action === 'keep').length;

    if (rowsToProcess.length === 0) {
        if (keptCount > 0) {
            AdminCore.showNotification(`No changes to apply. ${keptCount} row(s) will keep current values.`, 'info');
        } else {
            AdminCore.showNotification('No rows selected for import', 'warning');
        }
        return;
    }

    const token = getAntiForgeryToken();
    let successCount = 0;
    let errorCount = 0;
    const errors = [];

    // Process each selected row
    for (const row of rowsToProcess) {
        try {
            let response;

            if (row.action === 'create') {
                response = await fetch('/Admin/CreateTag', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({
                        name: row.data.name,
                        tagType: row.data.tagType,
                        description: row.data.description,
                        importBatchId: null // Will be set by server
                    })
                });
            } else if (row.action === 'update' && row.existingData) {
                response = await fetch(`/Admin/UpdateTag/${row.existingData.id}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({
                        id: row.existingData.id,
                        name: row.data.name,
                        tagType: row.data.tagType,
                        description: row.data.description
                    })
                });
            }

            if (response) {
                const result = await response.json();
                if (result.success) {
                    successCount++;
                } else {
                    errorCount++;
                    errors.push(`Row ${row.rowNumber} (${row.data.name}): ${result.message || 'Unknown error'}`);
                }
            }
        } catch (error) {
            errorCount++;
            errors.push(`Row ${row.rowNumber} (${row.data.name}): ${error.message}`);
        }
    }

    // Close modal
    closeImportPreviewModal();

    // Reload tags
    await loadTags();

    // Show results
    const keptMessage = keptCount > 0 ? `, ${keptCount} kept unchanged` : '';
    if (errorCount === 0) {
        AdminCore.showNotification(`Import complete: ${successCount} tags processed successfully${keptMessage}`, 'success');
    } else if (successCount > 0) {
        AdminCore.showNotification(`Import partial: ${successCount} succeeded, ${errorCount} failed${keptMessage}`, 'warning');
        console.error('Import errors:', errors);
    } else {
        AdminCore.showNotification(`Import failed: ${errorCount} errors occurred`, 'error');
        console.error('Import errors:', errors);
    }
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

/**
 * Normalize CSV header names for tag imports.
 * Handles common variations case-insensitively.
 */
function normalizeTagHeaderName(header) {
    const normalized = header.toLowerCase().replace(/[\s_-]/g, '');

    const headerMappings = {
        // Name variations
        'name': 'name',
        'tagname': 'name',
        'tag': 'name',

        // Type variations
        'tagtype': 'tagtype',
        'type': 'tagtype',
        'visibility': 'tagtype',
        'scope': 'tagtype',

        // Description variations
        'description': 'description',
        'desc': 'description',
        'notes': 'description'
    };

    return headerMappings[normalized] || normalized;
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
// Import preview functions
window.closeImportPreviewModal = closeImportPreviewModal;
window.applySelectedImport = applySelectedImport;
window.toggleAllImportRows = toggleAllImportRows;
window.updateImportRowSelection = updateImportRowSelection;
window.updateImportRowAction = updateImportRowAction;
