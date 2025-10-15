// Product Management JavaScript - Full CRUD functionality with backend API integration
class ProductManagement {
    constructor() {
        this.products = [];
        this.filteredProducts = [];
        this.currentSort = { field: 'name', direction: 'asc' };
        this.currentEditingId = null;
        this.addingNew = false;
        
        this.init();
    }
    
    async init() {
        console.log('Initializing Product Management...');
        this.bindEvents();
        await this.loadProducts();
        this.renderProducts();
        this.updateProductCount();
    }
    
    bindEvents() {
        // Button events
        document.getElementById('add-product-btn')?.addEventListener('click', () => this.showAddProduct());
        document.getElementById('export-btn')?.addEventListener('click', () => this.exportProducts());
        
        // Search event
        document.getElementById('search-input')?.addEventListener('input', (e) => this.handleSearch(e.target.value));
        
        // Add row events
        document.getElementById('save-new-btn')?.addEventListener('click', () => this.saveNewProduct());
        document.getElementById('cancel-new-btn')?.addEventListener('click', () => this.cancelAddProduct());
        document.getElementById('new-product-input')?.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                this.saveNewProduct();
            }
        });
        
        // Table sorting events
        document.querySelectorAll('[data-sort]').forEach(header => {
            header.addEventListener('click', () => this.handleSort(header.dataset.sort));
        });
        
        // Handle escape key to cancel editing
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.cancelAddProduct();
                this.cancelProductEdit();
            }
        });
    }
    
    async loadProducts() {
        try {
            this.showLoading(true);
            const response = await fetch('/Admin/GetProducts', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            const result = await response.json();
            
            if (result.success && result.data) {
                this.products = result.data;
                this.filteredProducts = [...this.products];
                console.log(`Loaded ${this.products.length} products from backend`);
            } else {
                console.error('Failed to load products:', result.message);
                this.showToast('Failed to load products: ' + (result.message || 'Unknown error'), 'error');
                // Fallback to empty array
                this.products = [];
                this.filteredProducts = [];
            }
        } catch (error) {
            console.error('Error loading products:', error);
            this.showToast('Error connecting to server. Please check your connection.', 'error');
            // Fallback to empty array  
            this.products = [];
            this.filteredProducts = [];
        } finally {
            this.showLoading(false);
        }
    }
    
    renderProducts() {
        const tbody = document.getElementById('products-table-body');
        if (!tbody) return;
        
        // Clear existing rows except add-row
        const existingRows = tbody.querySelectorAll('tr:not(#add-row)');
        existingRows.forEach(row => row.remove());
        
        if (this.filteredProducts.length === 0) {
            // Show no data message
            const noDataRow = document.createElement('tr');
            noDataRow.innerHTML = `
                <td colspan="2" class="no-data">
                    <div class="no-data-message">
                        <i class="fas fa-box-open"></i>
                        <p>No products found</p>
                        <small>Try adjusting your search or add a new product</small>
                    </div>
                </td>
            `;
            tbody.appendChild(noDataRow);
            return;
        }
        
        this.filteredProducts.forEach(product => {
            const row = document.createElement('tr');
            row.setAttribute('data-id', product.id);
            
            // Product Name cell
            const nameCell = document.createElement('td');
            nameCell.setAttribute('data-label', 'Product Name');
            
            if (this.currentEditingId === product.id) {
                const input = document.createElement('input');
                input.type = 'text';
                input.className = 'edit-input';
                input.value = product.name;
                input.id = `edit-product-${product.id}`;
                nameCell.appendChild(input);
                row.classList.add('edit-row');
            } else {
                nameCell.textContent = product.name;
            }
            
            row.appendChild(nameCell);
            
            // Actions cell
            const actionsCell = document.createElement('td');
            actionsCell.className = 'actions-cell';
            actionsCell.setAttribute('data-label', 'Actions');
            
            if (this.currentEditingId === product.id) {
                // Show save and cancel buttons
                const editActions = document.createElement('div');
                editActions.className = 'edit-actions';
                
                const saveBtn = document.createElement('button');
                saveBtn.className = 'save-btn';
                saveBtn.textContent = 'Save';
                saveBtn.addEventListener('click', () => this.saveProductEdit(product.id));
                
                const cancelBtn = document.createElement('button');
                cancelBtn.className = 'cancel-btn';
                cancelBtn.textContent = 'Cancel';
                cancelBtn.addEventListener('click', () => this.cancelProductEdit());
                
                editActions.appendChild(saveBtn);
                editActions.appendChild(cancelBtn);
                actionsCell.appendChild(editActions);
            } else {
                // Show edit and delete buttons
                const editBtn = document.createElement('button');
                editBtn.className = 'action-btn edit-btn';
                editBtn.innerHTML = '<i class="fas fa-edit"></i>';
                editBtn.title = 'Edit';
                editBtn.addEventListener('click', () => this.editProduct(product.id));
                
                const deleteBtn = document.createElement('button');
                deleteBtn.className = 'action-btn delete-btn';
                deleteBtn.innerHTML = '<i class="fas fa-trash"></i>';
                deleteBtn.title = 'Delete';
                deleteBtn.addEventListener('click', () => this.deleteProduct(product.id));
                
                actionsCell.appendChild(editBtn);
                actionsCell.appendChild(deleteBtn);
            }
            
            row.appendChild(actionsCell);
            tbody.appendChild(row);
        });
        
        this.updateProductCount();
    }
    
    handleSearch(searchTerm) {
        const term = searchTerm.toLowerCase().trim();
        this.applyFilters(term);
    }
    
    applyFilters(searchTerm = null) {
        const search = searchTerm !== null ? searchTerm : (document.getElementById('search-input')?.value || '').toLowerCase().trim();
        
        this.filteredProducts = this.products.filter(product => {
            if (search) {
                const searchableText = [product.name, product.description, product.productTypeName, product.companyName].join(' ').toLowerCase();
                if (!searchableText.includes(search)) return false;
            }
            return true;
        });
        
        this.renderProducts();
        this.updateProductCount();
    }
    
    handleSort(field) {
        if (this.currentSort.field === field) {
            this.currentSort.direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
        } else {
            this.currentSort.field = field;
            this.currentSort.direction = 'asc';
        }
        
        this.filteredProducts.sort((a, b) => {
            let aVal = a[field] || '';
            let bVal = b[field] || '';
            
            if (typeof aVal === 'string') {
                aVal = aVal.toLowerCase();
                bVal = bVal.toLowerCase();
            }
            
            let comparison = 0;
            if (aVal > bVal) comparison = 1;
            if (aVal < bVal) comparison = -1;
            
            return this.currentSort.direction === 'desc' ? -comparison : comparison;
        });
        
        this.updateSortIcons(field);
        this.renderProducts();
    }
    
    updateSortIcons(activeField) {
        document.querySelectorAll('[data-sort] .sort-icon').forEach(icon => {
            const field = icon.closest('[data-sort]').dataset.sort;
            if (field === activeField) {
                icon.textContent = this.currentSort.direction === 'asc' ? '▲' : '▼';
                icon.closest('th').classList.add('sorted');
            } else {
                icon.textContent = '▼';
                icon.closest('th').classList.remove('sorted');
            }
        });
    }
    
    updateProductCount() {
        const countElement = document.getElementById('products-count');
        if (countElement) {
            countElement.textContent = `Showing ${this.filteredProducts.length} of ${this.products.length} products`;
        }
    }
    
    showAddProduct() {
        if (this.addingNew || this.currentEditingId !== null) return;
        
        this.addingNew = true;
        const addRow = document.getElementById('add-row');
        if (addRow) {
            addRow.classList.add('active');
            const input = document.getElementById('new-product-input');
            if (input) input.focus();
        }
    }
    
    async saveNewProduct() {
        const input = document.getElementById('new-product-input');
        const productName = input?.value.trim();
        
        if (!productName) {
            this.showToast('Product name cannot be empty', 'error');
            input?.focus();
            return;
        }
        
        // Check for duplicate names
        const existingProduct = this.products.find(p => p.name.toLowerCase() === productName.toLowerCase());
        if (existingProduct) {
            this.showToast('Product name already exists', 'error');
            input?.focus();
            return;
        }
        
        try {
            this.showLoading(true, 'Creating product...');
            
            const productData = {
                name: productName,
                description: '',
                isActive: true
            };
            
            const response = await fetch('/Admin/CreateProduct', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(productData)
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showToast(result.message || 'Product created successfully', 'success');
                this.cancelAddProduct();
                await this.loadProducts();
                this.renderProducts();
            } else {
                this.showToast('Failed to create product: ' + (result.message || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error creating product:', error);
            this.showToast('Error creating product. Please try again.', 'error');
        } finally {
            this.showLoading(false);
        }
    }
    
    cancelAddProduct() {
        this.addingNew = false;
        const addRow = document.getElementById('add-row');
        if (addRow) {
            addRow.classList.remove('active');
        }
        const input = document.getElementById('new-product-input');
        if (input) input.value = '';
    }
    
    editProduct(id) {
        if (this.currentEditingId !== null || this.addingNew) return;
        
        this.currentEditingId = id;
        this.renderProducts();
        
        // Focus on the edit input
        setTimeout(() => {
            const editInput = document.getElementById(`edit-product-${id}`);
            if (editInput) {
                editInput.focus();
                editInput.select();
            }
        }, 0);
    }
    
    async saveProductEdit(id) {
        const editInput = document.getElementById(`edit-product-${id}`);
        const newName = editInput?.value.trim();
        
        if (!newName) {
            this.showToast('Product name cannot be empty', 'error');
            editInput?.focus();
            return;
        }
        
        // Check for duplicate names (excluding current product)
        const existingProduct = this.products.find(p => p.id !== id && p.name.toLowerCase() === newName.toLowerCase());
        if (existingProduct) {
            this.showToast('Product name already exists', 'error');
            editInput?.focus();
            return;
        }
        
        try {
            this.showLoading(true, 'Updating product...');
            
            const product = this.products.find(p => p.id === id);
            const productData = {
                name: newName,
                description: product?.description || '',
                isActive: product?.isActive || true
            };
            
            const response = await fetch(`/Admin/UpdateProduct/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(productData)
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showToast(result.message || 'Product updated successfully', 'success');
                this.currentEditingId = null;
                await this.loadProducts();
                this.renderProducts();
            } else {
                this.showToast('Failed to update product: ' + (result.message || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error updating product:', error);
            this.showToast('Error updating product. Please try again.', 'error');
        } finally {
            this.showLoading(false);
        }
    }
    
    cancelProductEdit() {
        this.currentEditingId = null;
        this.renderProducts();
    }
    
    async deleteProduct(id) {
        const product = this.products.find(p => p.id === id);
        if (!product) return;
        
        if (!confirm(`Are you sure you want to delete "${product.name}"? This action cannot be undone.`)) {
            return;
        }
        
        try {
            this.showLoading(true, 'Deleting product...');
            
            const response = await fetch(`/Admin/DeleteProduct/${id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showToast(result.message || 'Product deleted successfully', 'success');
                await this.loadProducts();
                this.renderProducts();
            } else {
                this.showToast('Failed to delete product: ' + (result.message || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error deleting product:', error);
            this.showToast('Error deleting product. Please try again.', 'error');
        } finally {
            this.showLoading(false);
        }
    }
    
    async exportProducts() {
        if (this.filteredProducts.length === 0) {
            this.showToast('No products to export', 'warning');
            return;
        }
        
        try {
            const exportData = this.filteredProducts.map(product => ({
                ID: product.id,
                Name: product.name || '',
                Description: product.description || '',
                'Product Type': product.productTypeName || '',
                Company: product.companyName || '',
                Status: product.isActive ? 'Active' : 'Inactive',
                'Certificate Count': product.certificateCount || 0,
                'Created Date': product.createdDate ? new Date(product.createdDate).toLocaleDateString() : ''
            }));
            
            const headers = Object.keys(exportData[0]);
            const csvContent = [
                headers.join(','),
                ...exportData.map(row => 
                    headers.map(header => {
                        const value = row[header] || '';
                        return value.includes(',') || value.includes('"') 
                            ? `"${value.replace(/"/g, '""')}"` 
                            : value;
                    }).join(',')
                )
            ].join('\n');
            
            const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
            const link = document.createElement('a');
            const url = URL.createObjectURL(blob);
            link.setAttribute('href', url);
            link.setAttribute('download', `products_export_${new Date().toISOString().split('T')[0]}.csv`);
            link.style.visibility = 'hidden';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            
            this.showToast(`Exported ${exportData.length} products successfully`, 'success');
        } catch (error) {
            console.error('Error exporting products:', error);
            this.showToast('Error exporting products. Please try again.', 'error');
        }
    }
    
    showLoading(show, message = 'Loading...') {
        let loader = document.getElementById('loading-overlay');
        if (!loader) {
            loader = document.createElement('div');
            loader.id = 'loading-overlay';
            loader.innerHTML = `
                <div class="loading-content">
                    <div class="loading-spinner"></div>
                    <div class="loading-message">${message}</div>
                </div>
            `;
            document.body.appendChild(loader);
        }
        
        loader.querySelector('.loading-message').textContent = message;
        loader.style.display = show ? 'flex' : 'none';
    }
    
    showToast(message, type = 'success') {
        const toast = document.getElementById('toast');
        if (!toast) return;
        
        toast.textContent = message;
        toast.className = `toast ${type} show`;
        
        setTimeout(() => {
            toast.classList.remove('show');
        }, 3000);
    }
    
    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
                     document.querySelector('meta[name="__RequestVerificationToken"]')?.content;
        return token || '';
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing Product Management...');
    window.productManagement = new ProductManagement();
});