// Certificate Types Management JavaScript
// Based on ProductTypeManager pattern with 2-level hierarchy (Type -> Subcategory)

class CertificateTypeManager {
    constructor() {
        this.currentView = 'tree';
        this.hierarchyData = [];
        this.flatData = [];
        this.init();
    }

    init() {
        this.loadCertificateTypes();
        this.setupEventListeners();
    }

    async loadCertificateTypes() {
        try {
            const response = await fetch('/Admin/GetCertificateTypes');
            const result = await response.json();

            if (result.success) {
                this.flatData = result.data || [];
                this.hierarchyData = this.buildHierarchy(this.flatData);
                this.renderTreeView();
                this.updateStats();
            } else {
                console.error('Failed to load certificate types:', result.message);
                this.showNotification('Failed to load certificate types: ' + (result.message || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error loading certificate types:', error);
            this.showNotification('Error loading certificate types. Please try again.', 'error');
        }
    }

    buildHierarchy(flatData) {
        const map = {};
        const roots = [];

        // First pass: create all nodes
        flatData.forEach(item => {
            map[item.id] = { ...item, children: [] };
        });

        // Second pass: build hierarchy
        flatData.forEach(item => {
            if (item.parentId) {
                if (map[item.parentId]) {
                    map[item.parentId].children.push(map[item.id]);
                }
            } else {
                roots.push(map[item.id]);
            }
        });

        return roots;
    }

    renderTreeView() {
        const treeContent = document.querySelector('.tree-content');
        if (!treeContent) return;

        // Hide loading state
        const loadingState = document.getElementById('tree-loading');
        const emptyState = document.getElementById('tree-empty');

        if (loadingState) loadingState.style.display = 'none';

        // Check if we have data
        if (!this.hierarchyData || this.hierarchyData.length === 0) {
            if (emptyState) emptyState.style.display = 'flex';
            return;
        }

        if (emptyState) emptyState.style.display = 'none';

        // Clear existing content (except loading/empty states) and render dynamic data
        const existingNodes = treeContent.querySelectorAll('.tree-node');
        existingNodes.forEach(node => node.remove());

        // Render each top-level type
        this.hierarchyData.forEach(type => {
            treeContent.appendChild(this.createTreeNode(type, 1));
        });
    }

    createTreeNode(node, level) {
        const treeNode = document.createElement('div');
        treeNode.className = 'tree-node';
        treeNode.dataset.id = node.id;

        // Level 1 = type, Level 2 = subcategory
        const nodeType = level === 1 ? 'type' : 'subcategory';
        const hasChildren = node.children && node.children.length > 0;

        const nodeContent = document.createElement('div');
        nodeContent.className = `node-content ${nodeType}-node ${hasChildren ? 'expandable' : ''}`;

        // Calculate certificate count (sum of own certs + children certs)
        const totalCerts = this.calculateCertificateCount(node);

        nodeContent.innerHTML = `
            <div class="node-indent level-${level}"></div>
            ${hasChildren ? '<div class="node-icon"><i class="fas fa-chevron-right"></i></div>' : '<div class="node-icon"></div>'}
            <div class="node-label">${node.name}</div>
            <div class="node-meta">
                <span class="node-count">${totalCerts}</span>
                <div class="node-actions">
                    <button class="action-btn edit" title="Edit ${nodeType}" data-id="${node.id}" data-type="${nodeType}">
                        <i class="fas fa-edit"></i>
                    </button>
                    ${level === 1 ? `<button class="action-btn add" title="Add Subcategory" data-parent-id="${node.id}" data-type="subcategory"><i class="fas fa-plus"></i></button>` : ''}
                    <button class="action-btn delete" title="Delete ${nodeType}" data-id="${node.id}" data-type="${nodeType}">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `;

        if (hasChildren) {
            nodeContent.addEventListener('click', (e) => {
                if (!e.target.closest('.node-actions')) {
                    this.toggleNode(nodeContent);
                }
            });
        }

        treeNode.appendChild(nodeContent);

        // Add children container if has children
        if (hasChildren) {
            const childrenContainer = document.createElement('div');
            childrenContainer.className = 'children';
            childrenContainer.style.display = 'none';

            node.children.forEach(child => {
                childrenContainer.appendChild(this.createTreeNode(child, level + 1));
            });

            treeNode.appendChild(childrenContainer);
        }

        return treeNode;
    }

    calculateCertificateCount(node) {
        let count = node.certificateCount || 0;
        if (node.children && node.children.length > 0) {
            node.children.forEach(child => {
                count += this.calculateCertificateCount(child);
            });
        }
        return count;
    }

    toggleNode(element) {
        const children = element.parentElement.querySelector('.children');
        const icon = element.querySelector('.node-icon i');

        if (children) {
            const computedStyle = window.getComputedStyle(children);
            const isExpanded = computedStyle.display !== 'none';
            children.style.display = isExpanded ? 'none' : 'block';

            if (icon) {
                if (isExpanded) {
                    icon.classList.remove('fa-chevron-down');
                    icon.classList.add('fa-chevron-right');
                } else {
                    icon.classList.remove('fa-chevron-right');
                    icon.classList.add('fa-chevron-down');
                }
            }
        }
    }

    updateStats() {
        // Update the stats bar with real data
        let types = 0;
        let subcategories = 0;
        let totalCerts = 0;

        this.flatData.forEach(item => {
            if (item.parentId === null) {
                types++;
            } else {
                subcategories++;
            }
            totalCerts += item.certificateCount || 0;
        });

        // Update stat displays if elements exist
        const statNumbers = document.querySelectorAll('.stat-number');
        if (statNumbers.length >= 3) {
            statNumbers[0].textContent = types;
            statNumbers[1].textContent = subcategories;
            statNumbers[2].textContent = totalCerts;
        }

        // Update footer item count
        const totalItems = types + subcategories;
        const itemCount = document.getElementById('item-count');
        if (itemCount) {
            itemCount.textContent = `Showing ${totalItems} items`;
        }
    }

    setupEventListeners() {
        // Search functionality with autocomplete
        const searchInput = document.getElementById('search-input');
        if (searchInput) {
            this.initializeAutocomplete(searchInput);
        }
    }

    initializeAutocomplete(searchInput) {
        // Create autocomplete dropdown container
        const autocompleteContainer = document.createElement('div');
        autocompleteContainer.className = 'autocomplete-dropdown';
        autocompleteContainer.style.cssText = `
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            background: white;
            border: 1px solid #e9ecef;
            border-top: none;
            border-radius: 0 0 4px 4px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            max-height: 300px;
            overflow-y: auto;
            z-index: 1000;
            display: none;
        `;

        // Make search input container relative for positioning
        const searchContainer = searchInput.parentElement;
        searchContainer.style.position = 'relative';
        searchContainer.appendChild(autocompleteContainer);

        // Store reference
        this.autocompleteContainer = autocompleteContainer;

        // Debounce timer
        let debounceTimer = null;

        // Input event handler
        searchInput.addEventListener('input', (e) => {
            const term = e.target.value.trim();

            // Clear existing debounce timer
            clearTimeout(debounceTimer);

            // Handle autocomplete suggestions
            if (term.length >= 1) {
                const suggestions = this.getAutocompleteSuggestions(term);
                this.renderAutocomplete(suggestions, autocompleteContainer, searchInput);
            } else {
                autocompleteContainer.style.display = 'none';
            }

            // Debounced tree filter
            debounceTimer = setTimeout(() => {
                this.filterTree(term);
            }, 150);
        });

        // Close autocomplete on outside click
        document.addEventListener('click', (e) => {
            if (!searchInput.contains(e.target) && !autocompleteContainer.contains(e.target)) {
                autocompleteContainer.style.display = 'none';
            }
        });

        // Keyboard navigation
        searchInput.addEventListener('keydown', (e) => {
            const items = autocompleteContainer.querySelectorAll('.autocomplete-item');
            let selectedIndex = Array.from(items).findIndex(item => item.classList.contains('selected'));

            switch (e.key) {
                case 'ArrowDown':
                    e.preventDefault();
                    if (items.length > 0) {
                        if (selectedIndex >= 0) items[selectedIndex].classList.remove('selected');
                        const nextIndex = selectedIndex < items.length - 1 ? selectedIndex + 1 : 0;
                        items[nextIndex].classList.add('selected');
                        items[nextIndex].scrollIntoView({ block: 'nearest' });
                        this.updateAutocompleteSelectedStyles(autocompleteContainer);
                    }
                    break;
                case 'ArrowUp':
                    e.preventDefault();
                    if (items.length > 0) {
                        if (selectedIndex >= 0) items[selectedIndex].classList.remove('selected');
                        const prevIndex = selectedIndex > 0 ? selectedIndex - 1 : items.length - 1;
                        items[prevIndex].classList.add('selected');
                        items[prevIndex].scrollIntoView({ block: 'nearest' });
                        this.updateAutocompleteSelectedStyles(autocompleteContainer);
                    }
                    break;
                case 'Enter':
                    if (selectedIndex >= 0) {
                        e.preventDefault();
                        const selectedItem = items[selectedIndex];
                        searchInput.value = selectedItem.dataset.value;
                        autocompleteContainer.style.display = 'none';
                        this.filterTree(selectedItem.dataset.value);
                    }
                    break;
                case 'Escape':
                    autocompleteContainer.style.display = 'none';
                    break;
            }
        });
    }

    getAutocompleteSuggestions(term) {
        if (!this.flatData || this.flatData.length === 0) return [];

        const termLower = term.toLowerCase();
        const suggestions = [];

        this.flatData.forEach(item => {
            if (item.name.toLowerCase().includes(termLower)) {
                const isType = item.parentId === null;
                suggestions.push({
                    value: item.name,
                    label: item.name,
                    type: isType ? 'Type' : 'Subcategory'
                });
            }
        });

        // Sort: exact matches first, then alphabetically
        suggestions.sort((a, b) => {
            const aExact = a.value.toLowerCase() === termLower;
            const bExact = b.value.toLowerCase() === termLower;
            if (aExact && !bExact) return -1;
            if (!aExact && bExact) return 1;
            return a.value.localeCompare(b.value);
        });

        // Limit to 10 suggestions
        return suggestions.slice(0, 10);
    }

    renderAutocomplete(suggestions, container, input) {
        if (!suggestions || suggestions.length === 0) {
            container.style.display = 'none';
            return;
        }

        container.innerHTML = suggestions.map((s, index) => `
            <div class="autocomplete-item ${index === 0 ? 'selected' : ''}"
                 data-value="${this.escapeHtml(s.value)}"
                 style="padding: 10px 12px; cursor: pointer; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #f0f0f0; transition: background-color 0.15s ease;">
                <span class="autocomplete-label" style="font-size: 14px; color: #333;">${this.escapeHtml(s.label)}</span>
                <span class="autocomplete-type" style="font-size: 12px; background: ${s.type === 'Type' ? '#002B76' : '#92C83E'}; color: white; padding: 2px 8px; border-radius: 3px;">${this.escapeHtml(s.type)}</span>
            </div>
        `).join('');

        // Apply selected styling
        this.updateAutocompleteSelectedStyles(container);

        container.style.display = 'block';

        // Add click handlers
        container.querySelectorAll('.autocomplete-item').forEach(item => {
            item.addEventListener('click', () => {
                input.value = item.dataset.value;
                container.style.display = 'none';
                this.filterTree(item.dataset.value);
            });

            // Hover handlers
            item.addEventListener('mouseenter', () => {
                container.querySelectorAll('.autocomplete-item').forEach(i => i.classList.remove('selected'));
                item.classList.add('selected');
                this.updateAutocompleteSelectedStyles(container);
            });
        });
    }

    updateAutocompleteSelectedStyles(container) {
        container.querySelectorAll('.autocomplete-item').forEach(item => {
            if (item.classList.contains('selected')) {
                item.style.backgroundColor = '#e8f4fd';
            } else {
                item.style.backgroundColor = 'white';
            }
        });
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    switchView(viewType) {
        this.currentView = viewType;

        // Remove active class from all view buttons
        const viewButtons = document.querySelectorAll('.view-btn');
        viewButtons.forEach(btn => btn.classList.remove('active'));

        // Find and activate the correct button using data-view attribute
        const activeButton = document.querySelector(`.view-btn[data-view="${viewType}"]`);
        if (activeButton) activeButton.classList.add('active');

        if (viewType === 'tree') {
            this.showTreeView();
        } else if (viewType === 'table') {
            this.showTableView();
        }
    }

    showTreeView() {
        const treeView = document.getElementById('treeView');
        if (treeView) {
            treeView.style.display = 'block';
        }

        // Hide table view if it exists
        const tableView = document.getElementById('tableView');
        if (tableView) {
            tableView.style.display = 'none';
        }
    }

    showTableView() {
        // Create table view if it doesn't exist
        let tableView = document.getElementById('tableView');
        if (!tableView) {
            tableView = this.createTableView();
            const container = document.querySelector('.admin-container');
            // Insert after toolbar
            const toolbar = container.querySelector('.toolbar');
            if (toolbar && toolbar.nextSibling) {
                container.insertBefore(tableView, toolbar.nextSibling);
            } else {
                container.appendChild(tableView);
            }
        } else {
            // Refresh table data
            this.populateTableView(tableView);
        }

        tableView.style.display = 'block';

        // Hide tree view
        const treeView = document.getElementById('treeView');
        if (treeView) {
            treeView.style.display = 'none';
        }
    }

    createTableView() {
        const tableView = document.createElement('div');
        tableView.id = 'tableView';
        tableView.className = 'table-view';
        tableView.style.display = 'none';

        tableView.innerHTML = `
            <div class="table-container">
                <table class="admin-table">
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Type</th>
                            <th>Parent</th>
                            <th>Certificate Count</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody id="tableBody">
                        <!-- Dynamic content will be inserted here -->
                    </tbody>
                </table>
            </div>
        `;

        // Populate table with data
        this.populateTableView(tableView);

        return tableView;
    }

    populateTableView(tableView) {
        const tbody = tableView.querySelector('#tableBody');
        if (!tbody || !this.flatData) return;

        tbody.innerHTML = '';

        // Sort data: parent types first, then children grouped under their parent
        const sortedData = [];
        const parentTypes = this.flatData.filter(ct => ct.parentId === null);
        const childTypes = this.flatData.filter(ct => ct.parentId !== null);

        parentTypes.sort((a, b) => a.name.localeCompare(b.name));

        parentTypes.forEach(parent => {
            sortedData.push({ ...parent, level: 0, parentName: '-' });
            const children = childTypes.filter(ct => ct.parentId === parent.id);
            children.sort((a, b) => a.name.localeCompare(b.name));
            children.forEach(child => {
                sortedData.push({ ...child, level: 1, parentName: parent.name });
            });
        });

        sortedData.forEach(item => {
            const row = document.createElement('tr');
            const typeLabel = item.level === 0 ? 'Type' : 'Subcategory';

            row.innerHTML = `
                <td>${'&nbsp;'.repeat(item.level * 4)}${item.name}</td>
                <td><span class="badge badge-${typeLabel.toLowerCase()}">${typeLabel}</span></td>
                <td>${item.parentName}</td>
                <td>${item.certificateCount || 0}</td>
                <td>
                    <div class="action-buttons">
                        <button class="action-btn edit edit-btn" data-id="${item.id}" data-type="${typeLabel.toLowerCase()}" title="Edit">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="action-btn delete delete-btn" data-id="${item.id}" data-type="${typeLabel.toLowerCase()}" title="Delete">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            `;

            tbody.appendChild(row);
        });

        // Attach event handlers to new buttons
        this.attachTableEventHandlers(tbody);
    }

    attachTableEventHandlers(tbody) {
        // Edit buttons
        tbody.querySelectorAll('.edit-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const id = e.currentTarget.dataset.id;
                const type = e.currentTarget.dataset.type;
                this.showEditModal(id, type);
            });
        });

        // Delete buttons
        tbody.querySelectorAll('.delete-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const id = e.currentTarget.dataset.id;
                const type = e.currentTarget.dataset.type;
                this.showDeleteConfirm(id, type);
            });
        });
    }

    filterTree(searchTerm) {
        const searchLower = searchTerm.toLowerCase();
        const treeNodes = document.querySelectorAll('.tree-node');

        treeNodes.forEach(node => {
            const label = node.querySelector('.node-label');
            if (label) {
                const text = label.textContent.toLowerCase();
                const match = text.includes(searchLower);
                node.style.display = match || !searchTerm ? 'block' : 'none';
            }
        });
    }

    expandAll() {
        const allTreeNodes = document.querySelectorAll('.tree-node');
        allTreeNodes.forEach(treeNode => {
            const nodeContent = treeNode.querySelector('.node-content.expandable');
            if (nodeContent) {
                const children = treeNode.querySelector('.children');
                if (children) {
                    const computedStyle = window.getComputedStyle(children);
                    if (computedStyle.display === 'none') {
                        children.style.display = 'block';
                        const icon = nodeContent.querySelector('.node-icon i');
                        if (icon) {
                            icon.classList.remove('fa-chevron-right');
                            icon.classList.add('fa-chevron-down');
                        }
                    }
                }
            }
        });
    }

    collapseAll() {
        const allTreeNodes = document.querySelectorAll('.tree-node');
        allTreeNodes.forEach(treeNode => {
            const nodeContent = treeNode.querySelector('.node-content.expandable');
            if (nodeContent) {
                const children = treeNode.querySelector('.children');
                if (children) {
                    const computedStyle = window.getComputedStyle(children);
                    if (computedStyle.display !== 'none') {
                        children.style.display = 'none';
                        const icon = nodeContent.querySelector('.node-icon i');
                        if (icon) {
                            icon.classList.remove('fa-chevron-down');
                            icon.classList.add('fa-chevron-right');
                        }
                    }
                }
            }
        });
    }

    showNotification(message, type = 'info') {
        AdminCore.showNotification(message, type);
    }

    showAddModal(entityType, parentId = null) {
        const isSubcategory = entityType === 'subcategory';
        const modal = this.createModal(
            isSubcategory ? 'Add Subcategory' : 'Add Certificate Type',
            entityType,
            null,
            parentId
        );
        document.body.appendChild(modal);
        this.setupModalEvents(modal);
    }

    showEditModal(id, entityType) {
        const item = this.flatData.find(d => d.id == id);
        if (!item) return;

        const modal = this.createModal('Edit ' + this.capitalize(entityType), entityType, item);
        document.body.appendChild(modal);
        this.setupModalEvents(modal);
    }

    async showDeleteConfirm(id, entityType) {
        console.log('showDeleteConfirm called:', { id, entityType });
        const item = this.flatData.find(d => d.id == id);
        console.log('Item found:', item);
        if (!item) return;

        // Check if item has children
        const hasChildren = this.flatData.some(d => d.parentId == id);
        if (hasChildren) {
            this.showNotification('Cannot delete a type that has subcategories. Please delete the subcategories first.', 'error');
            return;
        }

        const confirmed = await AdminCore.showConfirmation({
            title: 'Delete Confirmation',
            message: `Are you sure you want to delete <strong>${item.name}</strong>? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger',
            allowHtml: true
        });

        if (confirmed) {
            this.deleteCertificateType(id);
        }
    }

    createModal(title, entityType, item = null, parentId = null) {
        const isEdit = item !== null;
        const isSubcategory = entityType === 'subcategory';
        const modal = document.createElement('div');
        modal.className = 'modal fade show';
        modal.style.cssText = `
            display: block;
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.85);
            backdrop-filter: blur(3px);
            z-index: 1050;
            overflow: auto;
        `;

        const parentOptions = isSubcategory ? this.getParentOptions(item?.id) : '';

        // Get parent name for context display
        let parentName = '';
        if (parentId) {
            const parent = this.flatData.find(d => d.id == parentId);
            parentName = parent ? parent.name : '';
        } else if (isEdit && item?.parentId) {
            const parent = this.flatData.find(d => d.id == item.parentId);
            parentName = parent ? parent.name : '';
        }

        // Get existing items count for context
        const existingTypes = this.flatData.filter(d => d.parentId === null);
        const existingSubcategories = parentId
            ? this.flatData.filter(d => d.parentId == parentId)
            : [];

        // Build compact info hint
        const infoHintContent = isSubcategory
            ? `Subcategories organize products within a certification type (e.g., "Baby Products" under "Made Safe").`
            : `Certificate Types are certification programs (e.g., "Made Safe", "EWG Verified").`;

        // Build collapsible examples
        const examplesContent = isSubcategory
            ? `Baby Products, Household Cleaners, Personal Care, Pet Products`
            : `Made Safe, EWG Verified, USDA Organic, Cradle to Cradle`;

        // Build context bar for subcategories (compact)
        const contextBar = isSubcategory && parentName ? `
            <div style="background: #e8f4fd; padding: 8px 16px; border-bottom: 1px solid #bee5eb; font-size: 14px;">
                <i class="fas fa-sitemap" style="color: #002B76; margin-right: 8px;"></i>
                Adding subcategory to: <strong style="color: #002B76;">${parentName}</strong>
            </div>
        ` : '';

        // No hierarchy diagram - keep it simple
        const hierarchyDiagram = '';

        // Existing subcategories inline
        const existingSubcategoriesList = isSubcategory && existingSubcategories.length > 0
            ? `<div style="font-size: 12px; color: #856404; margin-top: 4px;">Existing: ${existingSubcategories.map(s => s.name).join(', ')}</div>`
            : '';

        modal.innerHTML = `
            <div class="modal-dialog" style="margin: 50px auto; max-width: 800px; width: 95%;">
                <div class="modal-content" style="background: white; border-radius: 8px; box-shadow: 0 10px 40px rgba(0,0,0,0.5); max-width: 800px !important; width: 100%;">
                    <div class="modal-header" style="padding: 1rem; border-bottom: 2px solid #e9ecef; background-color: #f8f9fa;">
                        <h5 class="modal-title" style="margin: 0; font-size: 1.25rem; color: #002B76; font-weight: 600;">${title}</h5>
                        <button type="button" class="close" data-dismiss="modal" style="background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #666;">
                            <span>&times;</span>
                        </button>
                    </div>
                    ${contextBar}
                    <div class="modal-body" style="padding: 1.25rem; max-height: 60vh; overflow-y: auto;">
                        <form id="certTypeForm">
                            <input type="hidden" id="certTypeId" value="${item?.id || ''}">
                            <input type="hidden" id="entityType" value="${entityType}">

                            ${isSubcategory && !parentId ? `
                            <div class="form-group" style="margin-bottom: 1rem;">
                                <label for="parentId" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">
                                    Parent Certificate Type <span style="color: #dc3545;">*</span>
                                </label>
                                <select class="form-control" id="parentId" name="parentId" required
                                        style="width: 100%; padding: 0.5rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">
                                    ${parentOptions}
                                </select>
                            </div>
                            ` : isSubcategory && parentId ? `
                            <input type="hidden" id="parentId" name="parentId" value="${parentId}">
                            ` : ''}

                            <div class="form-group" style="margin-bottom: 1rem;">
                                <label for="name" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">
                                    ${isSubcategory ? 'Subcategory' : 'Certificate Type'} Name <span style="color: #dc3545;">*</span>
                                </label>
                                <input type="text" class="form-control" id="name" name="name"
                                       value="${item?.name || ''}" required
                                       placeholder="${isSubcategory ? 'e.g., Baby Products, Household Cleaners' : 'e.g., Made Safe, EWG Verified'}"
                                       style="width: 100%; padding: 0.5rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">
                                <div style="font-size: 12px; color: #6c757d; margin-top: 4px;">
                                    Examples: ${examplesContent}
                                </div>
                                ${existingSubcategoriesList}
                            </div>

                            <!-- Info Hint - compact -->
                            <div style="display: flex; align-items: center; gap: 8px; padding: 8px 12px; background: #e8f4fd; border-radius: 4px; font-size: 12px; color: #0c5460;">
                                <i class="fas fa-info-circle" style="color: #17a2b8;"></i>
                                <span>${infoHintContent}</span>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer" style="padding: 1rem; border-top: 2px solid #e9ecef; background-color: #f8f9fa; display: flex; justify-content: flex-end; gap: 0.75rem;">
                        <button type="button" class="btn btn-danger" data-dismiss="modal"
                                style="padding: 10px 20px; border: 1px solid #FF5252; background: #FF5252; color: white; border-radius: 4px; cursor: pointer; font-size: 16px; font-weight: 500;">Cancel</button>
                        <button type="button" class="btn btn-primary" id="saveBtn"
                                style="padding: 10px 20px; border: 1px solid #92C83E; background: #92C83E; color: white; border-radius: 4px; cursor: pointer; font-size: 16px; font-weight: 500;">
                            ${isEdit ? 'Update' : `Create ${isSubcategory ? 'Subcategory' : 'Type'}`}
                        </button>
                    </div>
                </div>
            </div>
        `;

        // Set parent if provided (for edit mode)
        if (isEdit && item.parentId && !parentId) {
            const parentSelect = modal.querySelector('#parentId');
            if (parentSelect) parentSelect.value = item.parentId;
        }

        return modal;
    }

    getParentOptions(excludeId = null) {
        // Get all parent types (items with no parent)
        let parents = this.flatData.filter(d => d.parentId === null);

        // Exclude current item if editing
        if (excludeId) {
            parents = parents.filter(p => p.id != excludeId);
        }

        return '<option value="">Select Parent Type...</option>' +
               parents.map(p => `<option value="${p.id}">${p.name}</option>`).join('');
    }

    setupModalEvents(modal) {
        // Close button (X button)
        const closeBtn = modal.querySelector('.close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => modal.remove());
        }

        // Cancel button
        const cancelBtnByAttr = modal.querySelector('[data-dismiss="modal"]');
        if (cancelBtnByAttr) {
            cancelBtnByAttr.addEventListener('click', () => modal.remove());
        }

        // Also handle by class name as backup
        const cancelBtnByClass = modal.querySelector('.btn-danger');
        if (cancelBtnByClass) {
            cancelBtnByClass.addEventListener('click', () => modal.remove());
        }

        // Save button
        const saveBtn = modal.querySelector('#saveBtn');
        console.log('Save button found:', saveBtn ? 'Yes' : 'No');
        if (saveBtn) {
            saveBtn.addEventListener('click', () => {
                console.log('Save button clicked');
                const form = modal.querySelector('#certTypeForm');
                console.log('Form found:', form ? 'Yes' : 'No');
                console.log('Form valid:', form ? form.checkValidity() : 'N/A');
                if (form && form.checkValidity()) {
                    const id = modal.querySelector('#certTypeId').value;
                    const name = modal.querySelector('#name').value.trim();
                    const parentSelect = modal.querySelector('#parentId');
                    const parentId = parentSelect ? parentSelect.value : null;

                    if (id) {
                        this.updateCertificateType(id, name, parentId || null);
                    } else {
                        this.createCertificateType(name, parentId || null);
                    }

                    modal.remove();
                } else if (form) {
                    form.reportValidity();
                }
            });
        }

        // Click outside to close
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.remove();
            }
        });

        // ESC key to close
        const handleEscape = (e) => {
            if (e.key === 'Escape') {
                modal.remove();
                document.removeEventListener('keydown', handleEscape);
            }
        };
        document.addEventListener('keydown', handleEscape);
    }

    async createCertificateType(name, parentId) {
        console.log('createCertificateType called:', { name, parentId });
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            console.log('Token found:', token ? 'Yes' : 'No');
            const response = await fetch('/Admin/CreateCertificateType', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({
                    name: name,
                    isActive: true,
                    parentId: parentId || null
                })
            });

            const result = await response.json();
            if (result.success) {
                const typeLabel = parentId ? 'Subcategory' : 'Certificate type';
                this.showNotification(`${typeLabel} created successfully`, 'success');
                this.loadCertificateTypes();
            } else {
                this.showNotification(result.message || 'Failed to create certificate type', 'error');
            }
        } catch (error) {
            console.error('Error creating certificate type:', error);
            this.showNotification('Error creating certificate type', 'error');
        }
    }

    async updateCertificateType(id, name, parentId) {
        try {
            const response = await fetch(`/Admin/UpdateCertificateType/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({
                    name: name,
                    isActive: true,
                    parentId: parentId || null
                })
            });

            const result = await response.json();
            if (result.success) {
                this.showNotification('Certificate type updated successfully', 'success');
                this.loadCertificateTypes();
            } else {
                this.showNotification(result.message || 'Failed to update certificate type', 'error');
            }
        } catch (error) {
            console.error('Error updating certificate type:', error);
            this.showNotification('Error updating certificate type', 'error');
        }
    }

    async deleteCertificateType(id) {
        console.log('deleteCertificateType called:', id);
        try {
            const response = await fetch(`/Admin/DeleteCertificateType/${id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });

            const result = await response.json();
            if (result.success) {
                this.showNotification('Certificate type deleted successfully', 'success');
                this.loadCertificateTypes();
            } else {
                this.showNotification(result.message || 'Failed to delete certificate type. It may be in use by certificates.', 'error');
            }
        } catch (error) {
            console.error('Error deleting certificate type:', error);
            this.showNotification('Error deleting certificate type', 'error');
        }
    }

    capitalize(str) {
        return str.charAt(0).toUpperCase() + str.slice(1);
    }
}

// Global functions called from template
function showAddForm(entityType, parentElement = null) {
    if (window.certTypeManager) {
        const parentId = parentElement ? parentElement.dataset.parentId : null;
        window.certTypeManager.showAddModal(entityType, parentId);
    }
}

function switchView(viewType) {
    if (window.certTypeManager) {
        window.certTypeManager.switchView(viewType);
    }
}

function filterTree(searchTerm) {
    if (window.certTypeManager) {
        window.certTypeManager.filterTree(searchTerm);
    }
}

function expandAll() {
    if (window.certTypeManager) {
        window.certTypeManager.expandAll();
    }
}

function collapseAll() {
    if (window.certTypeManager) {
        window.certTypeManager.collapseAll();
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOMContentLoaded - Initializing CertificateTypeManager');
    console.log('AdminCore available:', typeof AdminCore !== 'undefined');
    window.certTypeManager = new CertificateTypeManager();

    // Initialize export functionality
    initializeExportFunctionality();

    // Initialize all event handlers
    initializeEventHandlers();
});

// Export functionality - CSV only
function initializeExportFunctionality() {
    const exportBtn = document.getElementById('export-btn');
    if (exportBtn) {
        exportBtn.addEventListener('click', function() {
            if (!window.certTypeManager || !window.certTypeManager.flatData || window.certTypeManager.flatData.length === 0) {
                AdminCore.showNotification('No data available to export. Please wait for data to load.', 'warning');
                return;
            }

            // Prepare data for export
            const exportData = window.certTypeManager.flatData.map(item => {
                const parent = window.certTypeManager.flatData.find(p => p.id === item.parentId);
                return {
                    id: item.id,
                    name: item.name,
                    type: item.parentId === null ? 'Type' : 'Subcategory',
                    parent: parent ? parent.name : '(Top Level)',
                    certificateCount: item.certificateCount || 0
                };
            });

            const columns = [
                { key: 'id', header: 'ID' },
                { key: 'name', header: 'Name' },
                { key: 'type', header: 'Type' },
                { key: 'parent', header: 'Parent' },
                { key: 'certificateCount', header: 'Certificate Count' }
            ];

            if (typeof ExportUtils !== 'undefined') {
                ExportUtils.exportToCSV(
                    exportData,
                    columns,
                    'certificate_types',
                    (message, type) => AdminCore.showNotification(message, type)
                );
            } else {
                AdminCore.showNotification('Export functionality not available', 'error');
            }
        });
    }
}

// Initialize all event handlers to replace inline onclick handlers
function initializeEventHandlers() {
    // Delegate events for dynamically created elements
    document.addEventListener('click', (e) => {
        // Handle edit buttons
        if (e.target.closest('.action-btn.edit')) {
            const btn = e.target.closest('.action-btn.edit');
            const id = btn.dataset.id;
            const type = btn.dataset.type;
            if (window.certTypeManager) {
                window.certTypeManager.showEditModal(id, type);
            }
        }

        // Handle add buttons
        if (e.target.closest('.action-btn.add')) {
            const btn = e.target.closest('.action-btn.add');
            const parentId = btn.dataset.parentId;
            const type = btn.dataset.type;
            if (window.certTypeManager) {
                window.certTypeManager.showAddModal(type, parentId);
            }
        }

        // Handle delete buttons
        if (e.target.closest('.action-btn.delete')) {
            const btn = e.target.closest('.action-btn.delete');
            const id = btn.dataset.id;
            const type = btn.dataset.type;
            if (window.certTypeManager) {
                window.certTypeManager.showDeleteConfirm(id, type);
            }
        }
    });

    // Add new type button
    const addTypeBtn = document.querySelector('button[onclick*="showAddForm(\'type\')"]');
    if (addTypeBtn) {
        addTypeBtn.removeAttribute('onclick');
        addTypeBtn.addEventListener('click', () => {
            if (window.certTypeManager) {
                window.certTypeManager.showAddModal('type');
            }
        });
    }

    // View buttons
    const viewButtons = document.querySelectorAll('.view-btn[data-view]');
    viewButtons.forEach(button => {
        const viewType = button.getAttribute('data-view');
        button.removeAttribute('onclick');
        button.addEventListener('click', () => switchView(viewType));
    });

    // Expand/Collapse all buttons
    const expandAllBtn = document.querySelector('button[onclick*="expandAll"]');
    if (expandAllBtn) {
        expandAllBtn.removeAttribute('onclick');
        expandAllBtn.addEventListener('click', expandAll);
    }

    const collapseAllBtn = document.querySelector('button[onclick*="collapseAll"]');
    if (collapseAllBtn) {
        collapseAllBtn.removeAttribute('onclick');
        collapseAllBtn.addEventListener('click', collapseAll);
    }

    // Search input
    const searchInput = document.querySelector('input[onkeyup*="filterTree"]');
    if (searchInput) {
        searchInput.removeAttribute('onkeyup');
        searchInput.addEventListener('input', (e) => filterTree(e.target.value));
    }
}
