// Product Types Management JavaScript

class ProductTypeManager {
    constructor() {
        this.currentView = 'tree';
        this.hierarchyData = [];
        this.init();
    }

    init() {
        this.loadProductTypes();
        this.setupEventListeners();
    }

    async loadProductTypes() {
        try {
            const response = await fetch('/Admin/GetProductTypes');
            const result = await response.json();
            
            if (result.success) {
                this.hierarchyData = result.data;
                this.renderTreeView();
                this.updateStats();
            } else {
                console.error('Failed to load product types:', result.message);
                this.showNotification('Failed to load product types: ' + (result.message || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error loading product types:', error);
            this.showNotification('Error loading product types. Please try again.', 'error');
        }
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

        // API returns already-nested hierarchical data with 'children' property
        // No need to call buildHierarchy - data is already in the correct format
        // Render each top-level category directly
        this.hierarchyData.forEach(category => {
            treeContent.appendChild(this.createTreeNode(category, 1));
        });
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

    createTreeNode(node, level) {
        const treeNode = document.createElement('div');
        treeNode.className = 'tree-node';
        treeNode.dataset.id = node.id;
        
        const nodeType = level === 1 ? 'category' : level === 2 ? 'type' : 'subcategory';
        const hasChildren = node.children && node.children.length > 0;
        
        const nodeContent = document.createElement('div');
        nodeContent.className = `node-content ${nodeType}-node ${hasChildren ? 'expandable' : ''}`;
        
        nodeContent.innerHTML = `
            <div class="node-indent level-${level}"></div>
            ${hasChildren ? '<div class="node-icon"><i class="fas fa-chevron-right"></i></div>' : '<div class="node-icon"></div>'}
            <div class="node-label">${node.name}</div>
            <div class="node-meta">
                <span class="node-count">${node.productCount || 0}</span>
                <div class="node-actions">
                    <button class="action-btn edit" title="Edit ${nodeType}" data-id="${node.id}" data-type="${nodeType}">
                        <i class="fas fa-edit"></i>
                    </button>
                    ${level < 3 ? `<button class="action-btn add" title="Add ${level === 1 ? 'Type' : 'Subcategory'}" data-parent-id="${node.id}" data-type="${level === 1 ? 'type' : 'subcategory'}"><i class="fas fa-plus"></i></button>` : ''}
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

    toggleNode(element) {
        const children = element.parentElement.querySelector('.children');
        const icon = element.querySelector('.node-icon i');

        if (children) {
            // Check computed style to handle both CSS class and inline style display
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
        // Update the stats bar with real data from nested hierarchical structure
        let categories = 0;
        let types = 0;
        let subcategories = 0;
        let totalProducts = 0;

        // Traverse nested hierarchy to count items
        const countItems = (items, level) => {
            items.forEach(item => {
                totalProducts += item.productCount || 0;
                if (level === 0) {
                    categories++;
                } else if (level === 1) {
                    types++;
                } else if (level === 2) {
                    subcategories++;
                }
                if (item.children && item.children.length > 0) {
                    countItems(item.children, level + 1);
                }
            });
        };

        countItems(this.hierarchyData, 0);

        // Update stat displays if elements exist
        const statNumbers = document.querySelectorAll('.stat-number');
        if (statNumbers.length >= 4) {
            statNumbers[0].textContent = categories;
            statNumbers[1].textContent = types;
            statNumbers[2].textContent = subcategories;
            statNumbers[3].textContent = totalProducts;
        }
    }

    setupEventListeners() {
        // View switching
        const viewButtons = document.querySelectorAll('.view-btn');
        viewButtons.forEach(btn => {
            btn.addEventListener('click', (e) => {
                const view = e.target.closest('[onclick*="switchView"]')?.getAttribute('onclick')?.match(/switchView\('(\w+)'\)/)?.[1];
                if (view) this.switchView(view);
            });
        });

        // Search functionality
        const searchInput = document.querySelector('.search-box input');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                this.filterTree(e.target.value);
            });
        }
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
                            <th>Product Count</th>
                            <th>Sort Order</th>
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
        if (!tbody || !this.hierarchyData) return;

        tbody.innerHTML = '';

        // Flatten nested hierarchy for table display
        const flattenedData = [];
        const flatten = (items, level, parentName = '-') => {
            items.forEach(item => {
                flattenedData.push({
                    ...item,
                    level: level,
                    parentName: parentName
                });
                if (item.children && item.children.length > 0) {
                    flatten(item.children, level + 1, item.name);
                }
            });
        };
        flatten(this.hierarchyData, 0);

        flattenedData.forEach(item => {
            const row = document.createElement('tr');
            const typeLabel = item.level === 0 ? 'Category' : item.level === 1 ? 'Type' : 'Subcategory';

            row.innerHTML = `
                <td>${'&nbsp;'.repeat(item.level * 4)}${item.name}</td>
                <td><span class="badge badge-${typeLabel.toLowerCase()}">${typeLabel}</span></td>
                <td>${item.parentName}</td>
                <td>${item.productCount || 0}</td>
                <td>${item.sortOrder || 0}</td>
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

    async showNotification(message, type = 'info') {
        // Route all notifications to AdminCore modals for consistency
        const titleMap = {
            'success': 'Success',
            'error': 'Error',
            'warning': 'Warning',
            'info': 'Information'
        };
        const title = titleMap[type] || 'Information';

        switch (type) {
            case 'success':
                await AdminCore.showSuccessModal({ title, message });
                break;
            case 'error':
                await AdminCore.showErrorModal({ title, message });
                break;
            case 'warning':
                await AdminCore.showWarningModal({ title, message });
                break;
            case 'info':
            default:
                await AdminCore.showInfoModal({ title, message });
                break;
        }
    }

    showAddModal(entityType, parentId = null) {
        const modal = this.createModal('Add ' + this.capitalize(entityType), entityType, null, parentId);
        document.body.appendChild(modal);
        this.setupModalEvents(modal);
    }

    showEditModal(id, entityType) {
        const item = this.hierarchyData.find(d => d.id == id);
        if (!item) return;
        
        const modal = this.createModal('Edit ' + this.capitalize(entityType), entityType, item);
        document.body.appendChild(modal);
        this.setupModalEvents(modal);
    }

    async showDeleteConfirm(id, entityType) {
        const item = this.hierarchyData.find(d => d.id == id);
        if (!item) return;

        const confirmed = await AdminCore.showConfirmation({
            title: 'Delete Confirmation',
            message: `Are you sure you want to delete <strong>${item.name}</strong>? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger',
            allowHtml: true
        });

        if (confirmed) {
            this.deleteProductType(id);
        }
    }

    createModal(title, entityType, item = null, parentId = null) {
        const isEdit = item !== null;
        const modal = document.createElement('div');
        modal.className = 'modal show';
        modal.style.cssText = `
            display: block !important;
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.85);
            backdrop-filter: blur(3px);
            z-index: 1050;
            overflow: auto;
            opacity: 1 !important;
            visibility: visible !important;
        `;

        const parentOptions = entityType !== 'category' ? this.getParentOptions(entityType, item?.id) : '';

        // Find parent info for context display
        let parentName = '';
        let grandparentName = '';
        let parentItem = null;

        if (parentId) {
            parentItem = this.findItemById(parentId);
            if (parentItem) {
                parentName = parentItem.name;
                // For subcategory, also find grandparent (category)
                if (entityType === 'subcategory') {
                    const grandparent = this.findParentOf(parentItem);
                    grandparentName = grandparent ? grandparent.name : '';
                }
            }
        } else if (isEdit && item) {
            parentItem = this.findParentOf(item);
            if (parentItem) {
                parentName = parentItem.name;
                if (entityType === 'subcategory') {
                    const grandparent = this.findParentOf(parentItem);
                    grandparentName = grandparent ? grandparent.name : '';
                }
            }
        }

        // Get existing siblings for context
        const existingSiblings = parentId ? this.getChildrenOf(parentId) : [];

        // Build compact info hint content based on entity type
        const infoHintContent = {
            category: `Categories are the top level of product organization (e.g., "Personal Care", "Household").`,
            type: `Types are product groups within a category (e.g., "Skincare" under "Personal Care").`,
            subcategory: `Subcategories are the most specific level where products are assigned (e.g., "Moisturizers" under "Skincare").`
        }[entityType];

        // Build inline examples based on entity type
        const examplesContent = {
            category: `Personal Care, Household, Baby Products, Food & Beverage, Pet Products`,
            type: `Skincare, Hair Care, Body Care, Oral Care, Cosmetics`,
            subcategory: `Moisturizers, Cleansers, Serums, Sunscreen, Face Masks`
        }[entityType];

        // Build placeholder text based on entity type
        const placeholderText = {
            category: 'e.g., Personal Care, Household, Baby Products',
            type: 'e.g., Skincare, Hair Care, Cleaning Products',
            subcategory: 'e.g., Moisturizers, Shampoos, All-Purpose Cleaners'
        }[entityType];

        // Build field description based on entity type
        const fieldDescription = {
            category: 'Enter a broad product grouping name. This will be the top level in the product hierarchy.',
            type: 'Enter the product type name. This groups related subcategories together.',
            subcategory: 'Enter the specific product subcategory. Products will be assigned to this level.'
        }[entityType];

        // Build compact context bar for types and subcategories (one line)
        const contextBar = (entityType !== 'category' && parentName) ? `
            <div style="background: #e8f4fd; padding: 8px 16px; border-bottom: 1px solid #bee5eb; font-size: 14px;">
                <i class="fas fa-sitemap" style="color: #002B76; margin-right: 8px;"></i>
                Adding ${entityType} to: <strong style="color: #002B76;">${parentName}</strong>${grandparentName ? ` (in ${grandparentName})` : ''}
            </div>
        ` : '';

        // Simplified - no breadcrumb needed, context bar shows parent info
        const breadcrumb = '';

        // No hierarchy diagram - keep it simple
        const hierarchyDiagram = '';

        // Simplified existing siblings list (inline under name field)
        const existingSiblingsList = (entityType !== 'category' && existingSiblings.length > 0) ? `
            <span style="font-size: 13px; color: #856404;">
                Existing: ${existingSiblings.map(s => s.name).join(', ')}
            </span>
        ` : '';

        // Build parent selector for types/subcategories when no parent is pre-selected
        const parentSelectorHtml = (entityType !== 'category' && !parentId) ? `
            <div class="form-group" style="margin-bottom: 1.25rem;">
                <label for="parentId" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">
                    Parent ${entityType === 'type' ? 'Category' : 'Type'} <span style="color: #dc3545;">*</span>
                </label>
                <select class="form-control" id="parentId" name="parentId" required
                        style="width: 100%; padding: 0.5rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">
                    ${parentOptions}
                </select>
                <div style="font-size: 13px; color: #6c757d; margin-top: 6px;">
                    Choose which ${entityType === 'type' ? 'category' : 'type'} this ${entityType} belongs to
                </div>
            </div>
        ` : (entityType !== 'category' && parentId) ? `
            <input type="hidden" id="parentId" name="parentId" value="${parentId}">
        ` : '';

        // Build locked parent display when parent is pre-selected
        const parentDisplayHtml = (entityType !== 'category' && parentId && parentName) ? `
            <div class="form-group" style="margin-bottom: 1.25rem;">
                <label style="display: block; margin-bottom: 0.5rem; font-weight: 500;">
                    Parent ${entityType === 'type' ? 'Category' : 'Type'}
                </label>
                <div style="padding: 10px 12px; background: #e9ecef; border-radius: 4px; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                    <i class="fas fa-${entityType === 'type' ? 'folder' : 'tags'}" style="color: #002B76;"></i>
                    ${parentName}${grandparentName ? ` <span style="color: #6c757d; font-weight: normal;">(under ${grandparentName})</span>` : ''}
                </div>
                <div style="font-size: 13px; color: #6c757d; margin-top: 6px; font-style: italic;">
                    This ${entityType} will be created under "${parentName}"
                </div>
            </div>
        ` : '';

        modal.innerHTML = `
            <div class="modal-dialog" style="margin: 50px auto; max-width: 800px; width: 90%;">
                <div class="modal-content" style="background: white; border-radius: 8px; box-shadow: 0 10px 40px rgba(0,0,0,0.5); max-width: 800px !important; width: 100%;">
                    <div class="modal-header" style="padding: 1rem; border-bottom: 2px solid #e9ecef; background-color: #f8f9fa;">
                        <h5 class="modal-title" style="margin: 0; font-size: 1.25rem; color: #002B76; font-weight: 600;">${title}</h5>
                        <button type="button" class="close" data-dismiss="modal" style="background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #666;">
                            <span>&times;</span>
                        </button>
                    </div>
                    ${contextBar}
                    <div class="modal-body" style="padding: 1.25rem; max-height: 60vh; overflow-y: auto;">
                        <!-- Compact Info Hint -->
                        <div style="display: flex; align-items: center; gap: 8px; padding: 10px 12px; background: #e8f4fd; border-radius: 4px; margin-bottom: 16px; font-size: 14px; color: #0c5460;">
                            <i class="fas fa-info-circle" style="color: #17a2b8;"></i>
                            <span>${infoHintContent}</span>
                        </div>

                        <form id="productTypeForm">
                            ${parentSelectorHtml}
                            ${parentDisplayHtml}

                            <div class="form-group" style="margin-bottom: 1.25rem;">
                                <label for="name" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">
                                    ${this.capitalize(entityType)} Name <span style="color: #dc3545;">*</span>
                                </label>
                                <input type="text" class="form-control" id="name" name="name"
                                       value="${item?.name || ''}" required
                                       placeholder="${placeholderText}"
                                       style="width: 100%; padding: 0.5rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">
                                <div style="font-size: 13px; color: #6c757d; margin-top: 6px;">
                                    ${fieldDescription}
                                    ${existingSiblingsList ? ' | ' + existingSiblingsList : ''}
                                </div>
                                <!-- Inline Examples -->
                                <div style="font-size: 13px; color: #92C83E; margin-top: 4px;">
                                    <strong>Examples:</strong> ${examplesContent}
                                </div>
                            </div>

                            <div class="form-group" style="margin-bottom: 1.25rem;">
                                <label for="sortOrder" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">Sort Order</label>
                                <input type="number" class="form-control" id="sortOrder" name="sortOrder"
                                       value="${item?.sortOrder || 0}"
                                       style="width: 120px; padding: 0.5rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">
                                <div style="font-size: 13px; color: #6c757d; margin-top: 6px;">
                                    Controls display order. Lower numbers appear first.
                                </div>
                            </div>

                            <div class="form-group" style="margin-bottom: 0;">
                                <label for="description" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">Description (Optional)</label>
                                <textarea class="form-control" id="description" name="description" rows="2"
                                          placeholder="Brief description of this ${entityType}..."
                                          style="width: 100%; padding: 0.5rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">${item?.description || ''}</textarea>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer" style="padding: 1rem; border-top: 2px solid #e9ecef; background-color: #f8f9fa; display: flex; justify-content: flex-end; gap: 0.75rem;">
                        <button type="button" class="btn btn-danger" data-dismiss="modal"
                                style="padding: 10px 20px; border: 1px solid #FF5252; background: #FF5252; color: white; border-radius: 4px; cursor: pointer; font-size: 16px; font-weight: 500;">Cancel</button>
                        <button type="button" class="btn btn-primary" id="saveBtn"
                                style="padding: 10px 20px; border: 1px solid #92C83E; background: #92C83E; color: white; border-radius: 4px; cursor: pointer; font-size: 16px; font-weight: 500;">
                            ${isEdit ? 'Update' : `Create ${this.capitalize(entityType)}`}
                        </button>
                    </div>
                </div>
            </div>
        `;

        // Set parent if provided (for edit mode without pre-selected parent)
        if (isEdit && item && !parentId) {
            const parentSelect = modal.querySelector('#parentId');
            if (parentSelect && parentItem) {
                parentSelect.value = parentItem.id;
            }
        }

        return modal;
    }

    // Helper method to find an item by ID in the nested hierarchy
    findItemById(id) {
        const search = (items) => {
            for (const item of items) {
                if (item.id == id) return item;
                if (item.children && item.children.length > 0) {
                    const found = search(item.children);
                    if (found) return found;
                }
            }
            return null;
        };
        return search(this.hierarchyData);
    }

    // Helper method to find the parent of an item
    findParentOf(targetItem) {
        const search = (items, parent = null) => {
            for (const item of items) {
                if (item.id == targetItem.id) return parent;
                if (item.children && item.children.length > 0) {
                    const found = search(item.children, item);
                    if (found) return found;
                }
            }
            return null;
        };
        return search(this.hierarchyData);
    }

    // Helper method to get children of a parent by ID
    getChildrenOf(parentId) {
        const parent = this.findItemById(parentId);
        return parent && parent.children ? parent.children : [];
    }

    getParentOptions(entityType, excludeId = null) {
        let parents = [];
        
        if (entityType === 'type') {
            // For types, parents are categories
            parents = this.hierarchyData.filter(d => d.level === 0);
        } else if (entityType === 'subcategory') {
            // For subcategories, parents are types
            parents = this.hierarchyData.filter(d => d.level === 1);
        }
        
        // Exclude current item and its children if editing
        if (excludeId) {
            parents = parents.filter(p => p.id != excludeId);
        }
        
        return '<option value="">Select Parent...</option>' + 
               parents.map(p => `<option value="${p.id}">${p.name}</option>`).join('');
    }

    setupModalEvents(modal) {
        // Close button (X button)
        const closeBtn = modal.querySelector('.close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => modal.remove());
        }
        
        // Cancel button
        const cancelBtn = modal.querySelector('[data-dismiss="modal"]');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', () => modal.remove());
        }
        
        // Also handle by class name as backup
        const cancelBtnByClass = modal.querySelector('.btn-secondary');
        if (cancelBtnByClass) {
            cancelBtnByClass.addEventListener('click', () => modal.remove());
        }
        
        // Save button
        const saveBtn = modal.querySelector('#saveBtn');
        if (saveBtn) {
            saveBtn.addEventListener('click', () => {
                const form = modal.querySelector('#productTypeForm');
                if (form && form.checkValidity()) {
                    const formData = new FormData(form);
                    const data = Object.fromEntries(formData.entries());
                    
                    // Determine if it's edit or create based on presence of item
                    const title = modal.querySelector('.modal-title').textContent;
                    const isEdit = title.includes('Edit');
                    
                    if (isEdit) {
                        // Get ID from the item being edited
                        const itemName = form.querySelector('#name').defaultValue;
                        const item = this.hierarchyData.find(d => d.name === itemName);
                        if (item) {
                            this.updateProductType(item.id, data);
                        }
                    } else {
                        this.createProductType(data);
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

    async createProductType(data) {
        try {
            const response = await fetch('/Admin/CreateProductType', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify(data)
            });
            
            const result = await response.json();
            if (result.success) {
                this.showNotification('Product type created successfully', 'success');
                this.loadProductTypes();
            } else {
                this.showNotification(result.message || 'Failed to create product type', 'error');
            }
        } catch (error) {
            console.error('Error creating product type:', error);
            this.showNotification('Error creating product type', 'error');
        }
    }

    async updateProductType(id, data) {
        try {
            const response = await fetch(`/Admin/UpdateProductType/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({ ...data, id })
            });
            
            const result = await response.json();
            if (result.success) {
                this.showNotification('Product type updated successfully', 'success');
                this.loadProductTypes();
            } else {
                this.showNotification(result.message || 'Failed to update product type', 'error');
            }
        } catch (error) {
            console.error('Error updating product type:', error);
            this.showNotification('Error updating product type', 'error');
        }
    }

    async deleteProductType(id) {
        try {
            const response = await fetch(`/Admin/DeleteProductType/${id}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });
            
            const result = await response.json();
            if (result.success) {
                this.showNotification('Product type deleted successfully', 'success');
                this.loadProductTypes();
            } else {
                this.showNotification(result.message || 'Failed to delete product type', 'error');
            }
        } catch (error) {
            console.error('Error deleting product type:', error);
            this.showNotification('Error deleting product type', 'error');
        }
    }

    capitalize(str) {
        return str.charAt(0).toUpperCase() + str.slice(1);
    }
}

// Tree node functionality
function toggleNode(element) {
    const children = element.parentElement.querySelector('.children');
    const icon = element.querySelector('.node-icon i');

    if (children) {
        // Check computed style to handle both CSS class and inline style display
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

// Form functions
function showAddForm(entityType, parentElement = null) {
    if (window.productTypeManager) {
        const parentId = parentElement ? parentElement.dataset.parentId : null;
        window.productTypeManager.showAddModal(entityType, parentId);
    }
}

function showEditForm(element, entityType) {
    if (window.productTypeManager) {
        const id = element.dataset.id;
        window.productTypeManager.showEditModal(id, entityType);
    }
}

// View switching functions (called from template)
function switchView(viewType) {
    if (window.productTypeManager) {
        window.productTypeManager.switchView(viewType);
    }
}

function filterTree(searchTerm) {
    if (window.productTypeManager) {
        window.productTypeManager.filterTree(searchTerm);
    }
}

function expandAll() {
    if (window.productTypeManager) {
        window.productTypeManager.expandAll();
    }
}

function collapseAll() {
    if (window.productTypeManager) {
        window.productTypeManager.collapseAll();
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    window.productTypeManager = new ProductTypeManager();
    
    // Initialize export functionality
    initializeExportFunctionality();
    
    // Initialize all event handlers to replace inline onclick handlers
    initializeEventHandlers();
});

// Export functionality
function initializeExportFunctionality() {
    const exportBtn = document.getElementById('export-btn');
    if (exportBtn) {
        exportBtn.addEventListener('click', function() {
            // Use actual loaded data from productTypeManager
            if (!window.productTypeManager || !window.productTypeManager.hierarchyData || window.productTypeManager.hierarchyData.length === 0) {
                AdminCore.showNotification('No data available to export. Please wait for data to load.', 'warning');
                return;
            }

            // Flatten nested hierarchical data for export
            const flattenedData = [];
            const flatten = (items, level, parentName = '') => {
                items.forEach(item => {
                    const typeLabel = level === 0 ? 'Category' : level === 1 ? 'Type' : 'Subcategory';
                    flattenedData.push({
                        id: item.id,
                        name: item.name,
                        type: typeLabel,
                        parent: parentName,
                        level: level + 1,
                        sortOrder: item.sortOrder || 0,
                        productCount: item.productCount || 0
                    });
                    if (item.children && item.children.length > 0) {
                        flatten(item.children, level + 1, item.name);
                    }
                });
            };
            flatten(window.productTypeManager.hierarchyData, 0);

            const columns = [
                { key: 'id', header: 'ID' },
                { key: 'name', header: 'Name' },
                { key: 'type', header: 'Type' },
                { key: 'parent', header: 'Parent' },
                { key: 'level', header: 'Hierarchy Level' },
                { key: 'sortOrder', header: 'Sort Order' },
                { key: 'productCount', header: 'Product Count' }
            ];
            
            if (typeof ExportUtils !== 'undefined') {
                ExportUtils.showExportModal(
                    flattenedData, 
                    columns, 
                    'product_types_hierarchy',
                    (message, type) => {
                        if (window.productTypeManager) {
                            window.productTypeManager.showNotification(message, type);
                        } else {
                            AdminCore.showNotification(message, type);
                        }
                    }
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
            if (window.productTypeManager) {
                window.productTypeManager.showEditModal(id, type);
            }
        }
        
        // Handle add buttons
        if (e.target.closest('.action-btn.add')) {
            const btn = e.target.closest('.action-btn.add');
            const parentId = btn.dataset.parentId;
            const type = btn.dataset.type;
            if (window.productTypeManager) {
                window.productTypeManager.showAddModal(type, parentId);
            }
        }
        
        // Handle delete buttons
        if (e.target.closest('.action-btn.delete')) {
            const btn = e.target.closest('.action-btn.delete');
            const id = btn.dataset.id;
            const type = btn.dataset.type;
            if (window.productTypeManager) {
                window.productTypeManager.showDeleteConfirm(id, type);
            }
        }
    });
    
    // Add new category button
    const addCategoryBtn = document.querySelector('button[onclick*="showAddForm(\'category\')"]');
    if (addCategoryBtn) {
        addCategoryBtn.removeAttribute('onclick');
        addCategoryBtn.addEventListener('click', () => {
            if (window.productTypeManager) {
                window.productTypeManager.showAddModal('category');
            }
        });
    }

    // View buttons - use data-view attribute for reliable identification
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