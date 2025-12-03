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

        // Build hierarchy from flat data
        const hierarchy = this.buildHierarchy(this.hierarchyData);

        // Render each top-level category
        hierarchy.forEach(category => {
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
        // Update the stats bar with real data
        const totalProducts = this.hierarchyData.reduce((sum, item) => sum + (item.productCount || 0), 0);
        const categories = this.hierarchyData.filter(item => item.level === 0 || item.parentId === null).length;
        const types = this.hierarchyData.filter(item => item.level === 1).length;
        const subcategories = this.hierarchyData.filter(item => item.level === 2).length;

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
        
        const viewButtons = document.querySelectorAll('.view-btn');
        viewButtons.forEach(btn => btn.classList.remove('active'));
        
        const activeButton = document.querySelector(`[onclick*="switchView('${viewType}')"]`);
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
        
        // Sort data by level and sort order
        const sortedData = [...this.hierarchyData].sort((a, b) => {
            if (a.level !== b.level) return a.level - b.level;
            return (a.sortOrder || 0) - (b.sortOrder || 0);
        });
        
        sortedData.forEach(item => {
            const row = document.createElement('tr');
            const typeLabel = item.level === 0 ? 'Category' : item.level === 1 ? 'Type' : 'Subcategory';
            const parentName = item.parentId ? this.hierarchyData.find(p => p.id === item.parentId)?.name || '-' : '-';
            
            row.innerHTML = `
                <td>${'&nbsp;'.repeat(item.level * 4)}${item.name}</td>
                <td><span class="badge badge-${typeLabel.toLowerCase()}">${typeLabel}</span></td>
                <td>${parentName}</td>
                <td>${item.productCount || 0}</td>
                <td>${item.sortOrder || 0}</td>
                <td>
                    <div class="action-buttons">
                        <button class="btn btn-sm btn-outline-primary edit-btn" data-id="${item.id}" data-type="${typeLabel.toLowerCase()}">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger delete-btn" data-id="${item.id}" data-type="${typeLabel.toLowerCase()}">
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
        // Create a simple notification system
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 12px 24px;
            background: ${type === 'error' ? '#dc3545' : type === 'success' ? '#28a745' : '#007bff'};
            color: white;
            border-radius: 4px;
            z-index: 1000;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        `;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.remove();
        }, 5000);
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
            message: `Are you sure you want to delete "${item.name}"? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger'
        });

        if (confirmed) {
            this.deleteProductType(id);
        }
    }

    createModal(title, entityType, item = null, parentId = null) {
        const isEdit = item !== null;
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
        
        const parentOptions = entityType !== 'category' ? this.getParentOptions(entityType, item?.id) : '';
        
        modal.innerHTML = `
            <div class="modal-dialog" style="margin: 50px auto; max-width: 500px;">
                <div class="modal-content" style="background: white; border-radius: 8px; box-shadow: 0 5px 15px rgba(0,0,0,0.3);">
                    <div class="modal-header" style="padding: 1rem; border-bottom: 1px solid #dee2e6;">
                        <h5 class="modal-title" style="margin: 0; font-size: 1.25rem;">${title}</h5>
                        <button type="button" class="close" data-dismiss="modal" style="background: none; border: none; font-size: 1.5rem; cursor: pointer;">
                            <span>&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 1rem;">
                        <form id="productTypeForm">
                            <div class="form-group" style="margin-bottom: 1rem;">
                                <label for="name" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">Name</label>
                                <input type="text" class="form-control" id="name" name="name" 
                                       value="${item?.name || ''}" required
                                       style="width: 100%; padding: 0.375rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">
                            </div>
                            ${entityType !== 'category' ? `
                            <div class="form-group" style="margin-bottom: 1rem;">
                                <label for="parentId" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">Parent</label>
                                <select class="form-control" id="parentId" name="parentId" required
                                        style="width: 100%; padding: 0.375rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">
                                    ${parentOptions}
                                </select>
                            </div>
                            ` : ''}
                            <div class="form-group" style="margin-bottom: 1rem;">
                                <label for="sortOrder" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">Sort Order</label>
                                <input type="number" class="form-control" id="sortOrder" name="sortOrder" 
                                       value="${item?.sortOrder || 0}"
                                       style="width: 100%; padding: 0.375rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">
                            </div>
                            <div class="form-group" style="margin-bottom: 1rem;">
                                <label for="description" style="display: block; margin-bottom: 0.5rem; font-weight: 500;">Description</label>
                                <textarea class="form-control" id="description" name="description" rows="3"
                                          style="width: 100%; padding: 0.375rem 0.75rem; border: 1px solid #ced4da; border-radius: 0.25rem; font-size: 16px;">${item?.description || ''}</textarea>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer" style="padding: 1rem; border-top: 1px solid #dee2e6; display: flex; justify-content: flex-end; gap: 0.5rem;">
                        <button type="button" class="btn btn-secondary" data-dismiss="modal" 
                                style="padding: 0.375rem 0.75rem; border: 1px solid #6c757d; background: #6c757d; color: white; border-radius: 0.25rem; cursor: pointer;">Cancel</button>
                        <button type="button" class="btn btn-primary" id="saveBtn"
                                style="padding: 0.375rem 0.75rem; border: 1px solid #007bff; background: #007bff; color: white; border-radius: 0.25rem; cursor: pointer;">
                            ${isEdit ? 'Update' : 'Create'}
                        </button>
                    </div>
                </div>
            </div>
        `;
        
        // Set parent if provided
        if (parentId && !isEdit) {
            const parentSelect = modal.querySelector('#parentId');
            if (parentSelect) parentSelect.value = parentId;
        } else if (isEdit && item.parentId) {
            const parentSelect = modal.querySelector('#parentId');
            if (parentSelect) parentSelect.value = item.parentId;
        }
        
        return modal;
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
            
            // Flatten hierarchical data for export
            const flattenedData = window.productTypeManager.hierarchyData.map(item => {
                const typeLabel = item.level === 0 ? 'Category' : item.level === 1 ? 'Type' : 'Subcategory';
                const parentName = item.parentId ? 
                    window.productTypeManager.hierarchyData.find(p => p.id === item.parentId)?.name || '' : '';
                
                return {
                    id: item.id,
                    name: item.name,
                    type: typeLabel,
                    parent: parentName,
                    level: item.level + 1,
                    sortOrder: item.sortOrder || 0,
                    productCount: item.productCount || 0
                };
            });

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

    // View buttons
    const viewButtons = document.querySelectorAll('button[onclick*="switchView"]');
    viewButtons.forEach(button => {
        const onclickAttr = button.getAttribute('onclick');
        button.removeAttribute('onclick');
        
        if (onclickAttr.includes("switchView('tree')")) {
            button.addEventListener('click', () => switchView('tree'));
        } else if (onclickAttr.includes("switchView('table')")) {
            button.addEventListener('click', () => switchView('table'));
        }
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