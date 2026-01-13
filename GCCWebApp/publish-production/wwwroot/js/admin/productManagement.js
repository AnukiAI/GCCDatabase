// Product Management JavaScript - Full CRUD functionality with backend API integration
class ProductManagement {
    constructor() {
        this.products = [];
        this.filteredProducts = [];
        this.currentSort = { field: 'name', direction: 'asc' };
        this.currentEditingId = null;
        this.addingNew = false;

        // Import preview state
        this.importPreviewData = [];
        this.importFileName = '';

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
        document.getElementById('template-btn')?.addEventListener('click', () => this.downloadImportTemplate());
        document.getElementById('import-btn')?.addEventListener('click', () => this.showImportModal());
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

            // Parent Identifiers cell
            const identifiersCell = document.createElement('td');
            identifiersCell.setAttribute('data-label', 'Parent Identifiers');
            identifiersCell.className = 'product-identifiers-cell';

            if (product.productIdentifiers && product.productIdentifiers.length > 0) {
                // Create identifiers summary and copy button
                const identifiersContainer = document.createElement('div');
                identifiersContainer.className = 'identifiers-container';
                identifiersContainer.style.display = 'flex';
                identifiersContainer.style.alignItems = 'center';
                identifiersContainer.style.gap = '8px';

                // Group identifiers by type and create summary
                const grouped = {};
                product.productIdentifiers.forEach(pi => {
                    if (!grouped[pi.identifierType]) {
                        grouped[pi.identifierType] = [];
                    }
                    grouped[pi.identifierType].push(pi.identifierValue);
                });

                // Create summary text (e.g., "ASIN: 3, UPC: 2")
                const summaryText = document.createElement('span');
                summaryText.className = 'identifiers-summary';
                summaryText.textContent = Object.keys(grouped)
                    .map(type => `${type}: ${grouped[type].length}`)
                    .join(', ');

                // Create copy button
                const copyBtn = document.createElement('button');
                copyBtn.className = 'copy-identifiers-btn';
                copyBtn.innerHTML = '<i class="fas fa-copy"></i>';
                copyBtn.title = 'Copy all identifiers';
                copyBtn.addEventListener('click', (e) => {
                    e.stopPropagation();
                    this.copyAllIdentifiers(product.productIdentifiers, product.name);
                });

                identifiersContainer.appendChild(summaryText);
                identifiersContainer.appendChild(copyBtn);
                identifiersCell.appendChild(identifiersContainer);
            } else {
                identifiersCell.innerHTML = '<span class="text-muted">None</span>';
            }

            row.appendChild(identifiersCell);

            // Actions cell
            const actionsCell = document.createElement('td');
            actionsCell.className = 'actions-cell role-element role-analyst-show role-admin-show';
            actionsCell.setAttribute('data-label', 'Actions');
            
            if (this.currentEditingId === product.id) {
                // Show cancel and save buttons (Cancel first, then Save for consistency)
                const editActions = document.createElement('div');
                editActions.className = 'edit-actions';

                const cancelBtn = document.createElement('button');
                cancelBtn.className = 'cancel-btn';
                cancelBtn.innerHTML = '<i class="fas fa-times"></i> Cancel';
                cancelBtn.addEventListener('click', () => this.cancelProductEdit());

                const saveBtn = document.createElement('button');
                saveBtn.className = 'save-btn';
                saveBtn.innerHTML = '<i class="fas fa-check"></i> Save';
                saveBtn.addEventListener('click', () => this.saveProductEdit(product.id));

                editActions.appendChild(cancelBtn);
                editActions.appendChild(saveBtn);
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

        const confirmed = await AdminCore.showConfirmation({
            title: 'Delete Confirmation',
            message: `Are you sure you want to delete <strong>${product.name}</strong>? This action cannot be undone.`,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            type: 'danger'
        });

        if (!confirmed) {
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
    
    downloadImportTemplate() {
        try {
            // Create template data with example row
            // Template matches ProductViewModel structure used by the backend API
            const templateData = [
                {
                    Name: 'Sample Product',
                    Description: 'Optional product description',
                    ProductType: 'Building Materials',
                    Company: 'Acme Corporation',
                    Status: 'Active'
                }
            ];

            // Create CSV content with headers and example row
            const headers = ['Name', 'Description', 'ProductType', 'Company', 'Status'];
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
            link.setAttribute('download', `products_import_template_${new Date().toISOString().split('T')[0]}.csv`);
            link.style.visibility = 'hidden';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            this.showToast('Import template downloaded successfully', 'success');
        } catch (error) {
            console.error('Error downloading template:', error);
            this.showToast('Error downloading template. Please try again.', 'error');
        }
    }

    showImportModal() {
        // Create file input and trigger file selection
        const fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.accept = '.csv';
        fileInput.style.display = 'none';
        document.body.appendChild(fileInput);

        fileInput.addEventListener('change', async (e) => {
            const file = e.target.files[0];
            if (!file) {
                document.body.removeChild(fileInput);
                return;
            }

            this.importFileName = file.name;
            const reader = new FileReader();
            reader.onload = async (event) => {
                const contents = event.target.result;
                await this.parseAndPreviewImport(contents);
                document.body.removeChild(fileInput);
            };
            reader.onerror = () => {
                this.showToast('Error reading file', 'error');
                document.body.removeChild(fileInput);
            };
            reader.readAsText(file);
        });

        fileInput.click();
    }

    async parseAndPreviewImport(contents) {
        try {
            const lines = contents.split(/\r?\n/).filter(line => line.trim());
            if (lines.length < 2) {
                this.showToast('CSV file must have a header row and at least one data row', 'error');
                return;
            }

            // Parse header - normalize to standard field names
            const rawHeaders = this.parseCSVLine(lines[0]).map(h => h.trim());
            const headers = rawHeaders.map(h => this.normalizeHeaderName(h));

            const requiredFields = ['name'];
            const missingFields = requiredFields.filter(f => !headers.includes(f));

            if (missingFields.length > 0) {
                this.showToast(`Missing required fields: ${missingFields.join(', ')}`, 'error');
                return;
            }

            // Parse data rows
            const parsedProducts = [];
            for (let i = 1; i < lines.length; i++) {
                const values = this.parseCSVLine(lines[i]);
                if (values.length === 0 || values.every(v => !v.trim())) continue;

                const product = {};
                headers.forEach((header, index) => {
                    product[header] = values[index]?.trim() || '';
                });

                parsedProducts.push({
                    rowNumber: i,
                    data: {
                        name: product.name || '',
                        description: product.description || product.desc || '',
                        productType: product.producttype || '',
                        company: product.company || '',
                        status: product.status || product.isactive || 'Active'
                    }
                });
            }

            if (parsedProducts.length === 0) {
                this.showToast('No valid data rows found in CSV', 'error');
                return;
            }

            // Build preview with comparison to existing data
            await this.buildImportPreview(parsedProducts);

        } catch (error) {
            console.error('Error parsing CSV:', error);
            this.showToast('Error parsing CSV file', 'error');
        }
    }

    async buildImportPreview(parsedProducts) {
        this.importPreviewData = [];

        // Compare with existing products
        parsedProducts.forEach(row => {
            // Match by name (case-insensitive)
            const existingProduct = this.products.find(p =>
                p.name && row.data.name &&
                p.name.trim().toLowerCase() === row.data.name.trim().toLowerCase()
            );

            const previewRow = {
                rowNumber: row.rowNumber,
                data: row.data,
                existingData: null,
                status: 'new',
                changes: [],
                errors: [],
                action: 'create',
                selected: true
            };

            if (existingProduct) {
                previewRow.existingData = {
                    id: existingProduct.id,
                    name: existingProduct.name,
                    description: existingProduct.description || '',
                    productType: existingProduct.productTypeName || '',
                    company: existingProduct.companyName || '',
                    status: existingProduct.isActive ? 'Active' : 'Inactive'
                };

                // Track which fields changed
                const changes = [];

                const existingDesc = (existingProduct.description || '').trim();
                const importDesc = (row.data.description || '').trim();
                if (importDesc && existingDesc !== importDesc) {
                    changes.push({ field: 'description', oldValue: existingDesc, newValue: importDesc });
                }

                const existingType = (existingProduct.productTypeName || '').trim().toLowerCase();
                const importType = (row.data.productType || '').trim().toLowerCase();
                if (importType && existingType !== importType) {
                    changes.push({ field: 'productType', oldValue: existingProduct.productTypeName || '', newValue: row.data.productType });
                }

                const existingCompany = (existingProduct.companyName || '').trim().toLowerCase();
                const importCompany = (row.data.company || '').trim().toLowerCase();
                if (importCompany && existingCompany !== importCompany) {
                    changes.push({ field: 'company', oldValue: existingProduct.companyName || '', newValue: row.data.company });
                }

                const existingStatus = existingProduct.isActive ? 'active' : 'inactive';
                const importStatus = (row.data.status || 'active').trim().toLowerCase();
                if (importStatus && existingStatus !== importStatus) {
                    changes.push({ field: 'status', oldValue: existingProduct.isActive ? 'Active' : 'Inactive', newValue: row.data.status });
                }

                previewRow.changes = changes;

                if (changes.length > 0) {
                    previewRow.status = 'modified';
                    previewRow.action = 'update';
                    previewRow.selected = true;
                } else {
                    previewRow.status = 'unchanged';
                    previewRow.action = 'skip';
                    previewRow.selected = false;
                }
            }

            // Validate required fields
            if (!row.data.name || !row.data.name.trim()) {
                previewRow.errors.push('Product name is required');
                previewRow.status = 'error';
                previewRow.selected = false;
            }

            // Validate company reference with fuzzy matching
            if (row.data.company && row.data.company.trim()) {
                const companyValidation = this.validateCompanyReference(row.data.company);

                if (!companyValidation.valid) {
                    // Company not found
                    let errorMsg = companyValidation.message;
                    if (companyValidation.suggestions && companyValidation.suggestions.length > 0) {
                        errorMsg += `. Did you mean: ${companyValidation.suggestions.join(', ')}?`;
                    }
                    previewRow.errors.push(errorMsg);
                    previewRow.status = 'error';
                    previewRow.selected = false;
                } else if (companyValidation.warning) {
                    // Fuzzy match found - show warning but allow import
                    previewRow.warnings = previewRow.warnings || [];
                    previewRow.warnings.push(companyValidation.message);
                    // Update the company name to the matched value for import
                    previewRow.data.resolvedCompany = companyValidation.company.name;
                }
            }

            // Validate ProductType reference with fuzzy matching
            if (row.data.productType && row.data.productType.trim()) {
                const typeValidation = this.validateProductTypeReference(row.data.productType);

                if (!typeValidation.valid) {
                    // ProductType not found
                    let errorMsg = typeValidation.message;
                    if (typeValidation.suggestions && typeValidation.suggestions.length > 0) {
                        errorMsg += `. Did you mean: ${typeValidation.suggestions.join(', ')}?`;
                    }
                    previewRow.errors.push(errorMsg);
                    previewRow.status = 'error';
                    previewRow.selected = false;
                } else if (typeValidation.warning) {
                    // Fuzzy match found - show warning but allow import
                    previewRow.warnings = previewRow.warnings || [];
                    previewRow.warnings.push(typeValidation.message);
                    // Update to the matched value for import
                    previewRow.data.resolvedProductType = typeValidation.productType.name;
                    previewRow.data.resolvedProductTypeId = typeValidation.productType.id;
                }
            }

            // Validate Category reference with fuzzy matching
            if (row.data.category && row.data.category.trim()) {
                const categoryValidation = this.validateCategoryReference(row.data.category);

                if (!categoryValidation.valid) {
                    // Category not found
                    let errorMsg = categoryValidation.message;
                    if (categoryValidation.suggestions && categoryValidation.suggestions.length > 0) {
                        errorMsg += `. Did you mean: ${categoryValidation.suggestions.join(', ')}?`;
                    }
                    previewRow.errors.push(errorMsg);
                    previewRow.status = 'error';
                    previewRow.selected = false;
                } else if (categoryValidation.warning) {
                    // Fuzzy match found - show warning but allow import
                    previewRow.warnings = previewRow.warnings || [];
                    previewRow.warnings.push(categoryValidation.message);
                    // Update to the matched value for import
                    previewRow.data.resolvedCategory = categoryValidation.category.name;
                    previewRow.data.resolvedCategoryId = categoryValidation.category.id;
                }
            }

            this.importPreviewData.push(previewRow);
        });

        // Show preview modal
        this.showImportPreviewModal();
    }

    showImportPreviewModal() {
        const modal = document.getElementById('import-preview-modal');
        if (!modal) {
            this.showToast('Import preview modal not found', 'error');
            return;
        }

        // Calculate stats
        const stats = {
            total: this.importPreviewData.length,
            new: this.importPreviewData.filter(r => r.status === 'new').length,
            modified: this.importPreviewData.filter(r => r.status === 'modified').length,
            unchanged: this.importPreviewData.filter(r => r.status === 'unchanged').length,
            errors: this.importPreviewData.filter(r => r.status === 'error').length
        };

        // Update stats display
        document.getElementById('import-stat-new').textContent = stats.new;
        document.getElementById('import-stat-modified').textContent = stats.modified;
        document.getElementById('import-stat-unchanged').textContent = stats.unchanged;
        document.getElementById('import-stat-errors').textContent = stats.errors;

        // Update filename display
        const filenameEl = document.getElementById('import-filename');
        if (filenameEl) {
            filenameEl.textContent = this.importFileName;
        }

        // Check if there are company-not-found errors and show import order guidance
        const companyErrors = this.importPreviewData.filter(r =>
            r.errors && r.errors.some(e => e.includes('not found in system'))
        );

        // Show/hide import order guidance banner
        let guidanceBanner = document.getElementById('import-order-guidance');
        if (companyErrors.length > 0) {
            if (!guidanceBanner) {
                guidanceBanner = document.createElement('div');
                guidanceBanner.id = 'import-order-guidance';
                guidanceBanner.className = 'import-guidance-banner';
                guidanceBanner.innerHTML = `
                    <div style="background: #fff3cd; border: 1px solid #ffc107; border-radius: 6px; padding: 12px 16px; margin-bottom: 16px; display: flex; align-items: flex-start; gap: 12px;">
                        <i class="fas fa-info-circle" style="color: #856404; font-size: 18px; margin-top: 2px;"></i>
                        <div>
                            <strong style="color: #856404;">Import Order Matters</strong>
                            <p style="margin: 4px 0 0 0; font-size: 13px; color: #856404;">
                                Products reference Companies and Product Types. Please import in this order:<br>
                                <strong>1. Companies</strong> (/Company) → <strong>2. Product Types</strong> (/Admin/ProductTypes) → <strong>3. Products</strong> (this page)
                            </p>
                        </div>
                        <button onclick="this.parentElement.remove()" style="background: none; border: none; color: #856404; cursor: pointer; font-size: 16px; padding: 0; margin-left: auto;">&times;</button>
                    </div>
                `;
                const modalBody = document.querySelector('#import-preview-modal .admin-modal-body');
                if (modalBody) {
                    modalBody.insertBefore(guidanceBanner, modalBody.firstChild);
                }
            }
        } else if (guidanceBanner) {
            guidanceBanner.remove();
        }

        // Build preview table
        const tbody = document.getElementById('import-preview-tbody');
        tbody.innerHTML = '';

        this.importPreviewData.forEach((row, index) => {
            const tr = document.createElement('tr');
            tr.className = `import-row import-row-${row.status}`;
            tr.dataset.index = index;

            // Row styling based on status
            tr.style.backgroundColor = row.status === 'error' ? '#fff5f5' : (row.status === 'unchanged' ? '#fafafa' : '');

            // Status badge with changes summary
            const statusLabels = {
                'new': '<span class="badge badge-success">New</span>',
                'modified': '<span class="badge badge-warning">Modified</span>',
                'unchanged': '<span class="badge badge-secondary">Unchanged</span>',
                'error': '<span class="badge badge-danger">Error</span>'
            };

            // Build changes summary for modified rows
            let changesSummary = '';
            if (row.status === 'modified' && row.changes && row.changes.length > 0) {
                const fieldNames = row.changes.map(c => {
                    if (c.field === 'productType') return 'Type';
                    if (c.field === 'description') return 'Description';
                    if (c.field === 'company') return 'Company';
                    if (c.field === 'status') return 'Status';
                    return c.field;
                });
                changesSummary = `<div style="font-size: 10px; color: #666; margin-top: 4px;">${fieldNames.join(', ')} changed</div>`;
            }

            // Error display with clickable suggestions
            let errorDisplay = '';
            if (row.errors && row.errors.length > 0) {
                errorDisplay = '<div style="margin-top: 4px;">';
                row.errors.forEach(error => {
                    // Check if error has suggestions
                    const suggestionMatch = error.match(/\. Did you mean: (.+)\?$/);
                    if (suggestionMatch) {
                        const baseError = error.replace(/\. Did you mean:.+$/, '');
                        const suggestions = suggestionMatch[1].split(', ').map(s => s.trim());
                        const field = this.detectFieldFromError(baseError);

                        errorDisplay += `
                            <div style="color: #ff5252; font-size: 11px; font-weight: 500; margin-bottom: 4px;">
                                ${baseError}
                            </div>
                            <div style="display: flex; flex-wrap: wrap; gap: 4px; align-items: center;">
                                <span style="font-size: 10px; color: #666;">Click to apply:</span>
                                ${suggestions.map(s => `
                                    <button type="button" class="suggestion-btn"
                                            onclick="productManagement.applySuggestion(${index}, '${field}', '${s.replace(/'/g, "\\'")}')"
                                            style="padding: 2px 8px; font-size: 11px; background: #e3f2fd; border: 1px solid #2196f3; border-radius: 12px; cursor: pointer; color: #1976d2; font-weight: 500;">
                                        ${s}
                                    </button>
                                `).join('')}
                            </div>
                        `;
                    } else {
                        errorDisplay += `<div style="color: #ff5252; font-size: 11px; font-weight: 500;">${error}</div>`;
                    }
                });
                errorDisplay += '</div>';
            }

            // Warning display (for fuzzy matches)
            let warningDisplay = '';
            if (row.warnings && row.warnings.length > 0) {
                warningDisplay = `<div style="color: #ff9800; font-size: 11px; margin-top: 4px; font-weight: 500;"><i class="fas fa-exclamation-triangle"></i> ${row.warnings.join('<br>')}</div>`;
            }

            tr.innerHTML = `
                <td style="text-align: center; vertical-align: top;">
                    <input type="checkbox" class="import-row-checkbox" data-index="${index}" ${row.selected ? 'checked' : ''} ${row.status === 'error' || row.status === 'unchanged' ? 'disabled' : ''}>
                </td>
                <td style="text-align: center; vertical-align: top;">${row.rowNumber}</td>
                <td style="vertical-align: top;">${statusLabels[row.status]}${changesSummary}</td>
                <td style="vertical-align: top;">${this.renderFieldWithDiff(row, 'name')}</td>
                <td style="vertical-align: top;">${this.renderFieldWithDiff(row, 'description')}${errorDisplay}${warningDisplay}</td>
                <td style="vertical-align: top;">${this.renderActionDropdown(row, index)}</td>
            `;

            tbody.appendChild(tr);
        });

        // Bind checkbox events
        tbody.querySelectorAll('.import-row-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                this.updateImportRowSelection(parseInt(e.target.dataset.index), e.target.checked);
            });
        });

        // Bind action dropdown events
        tbody.querySelectorAll('.import-action-select').forEach(select => {
            select.addEventListener('change', (e) => {
                this.updateImportRowAction(parseInt(e.target.dataset.index), e.target.value);
            });
        });

        // Bind select all checkbox
        const selectAllCheckbox = document.getElementById('import-select-all');
        if (selectAllCheckbox) {
            selectAllCheckbox.checked = this.importPreviewData.filter(r => r.status !== 'error' && r.status !== 'unchanged').every(r => r.selected);
            selectAllCheckbox.addEventListener('change', (e) => {
                this.toggleAllImportRows(e.target.checked);
            });
        }

        // Update selected count
        this.updateSelectedCount();

        // Show modal with important flag to ensure visibility
        modal.style.setProperty('display', 'block', 'important');
    }

    renderFieldWithDiff(row, field) {
        const escapeHtml = (str) => {
            if (str === null || str === undefined) return '';
            return String(str)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#039;');
        };

        const newValue = row.data[field] || '';

        if (row.status === 'modified' && row.existingData && row.changes) {
            const oldValue = row.existingData[field] || '';
            // Check if this specific field changed
            const fieldChanged = row.changes.some(c => c.field.toLowerCase() === field.toLowerCase());

            if (fieldChanged) {
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

        return `<span style="word-break: break-word;">${escapeHtml(newValue)}</span>`;
    }

    renderActionDropdown(row, index) {
        if (row.status === 'error') {
            return '<span class="text-muted">N/A</span>';
        }

        if (row.status === 'new') {
            return `
                <select class="import-action-select form-control form-control-sm" data-index="${index}">
                    <option value="create" ${row.action === 'create' ? 'selected' : ''}>Create New</option>
                    <option value="skip" ${row.action === 'skip' ? 'selected' : ''}>Skip Row</option>
                </select>
            `;
        }

        if (row.status === 'modified') {
            return `
                <select class="import-action-select form-control form-control-sm" data-index="${index}">
                    <option value="update" ${row.action === 'update' ? 'selected' : ''}>Use Import Data</option>
                    <option value="keep" ${row.action === 'keep' ? 'selected' : ''}>Keep Current</option>
                    <option value="skip" ${row.action === 'skip' ? 'selected' : ''}>Skip Row</option>
                </select>
            `;
        }

        if (row.status === 'unchanged') {
            return `
                <select class="import-action-select form-control form-control-sm" data-index="${index}">
                    <option value="skip" ${row.action === 'skip' ? 'selected' : ''}>Skip Row</option>
                    <option value="update" ${row.action === 'update' ? 'selected' : ''}>Update Anyway</option>
                </select>
            `;
        }

        return '';
    }

    updateImportRowSelection(index, checked) {
        if (index >= 0 && index < this.importPreviewData.length) {
            this.importPreviewData[index].selected = checked;
            this.updateSelectedCount();
        }
    }

    updateImportRowAction(index, action) {
        if (index >= 0 && index < this.importPreviewData.length) {
            this.importPreviewData[index].action = action;
            // Auto-select if choosing an action other than skip/keep
            this.importPreviewData[index].selected = (action !== 'skip' && action !== 'keep');

            const checkbox = document.querySelector(`.import-row-checkbox[data-index="${index}"]`);
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
            }

            this.updateSelectedCount();
        }
    }

    toggleAllImportRows(checked) {
        this.importPreviewData.forEach((row, index) => {
            if (row.status !== 'error') {
                row.selected = checked;
                const checkbox = document.querySelector(`.import-row-checkbox[data-index="${index}"]`);
                if (checkbox) checkbox.checked = checked;
            }
        });
        this.updateSelectedCount();
    }

    updateSelectedCount() {
        const selectedForImport = this.importPreviewData.filter(r =>
            r.selected && r.action !== 'skip' && r.action !== 'keep' && r.status !== 'error'
        ).length;
        const keepingCurrent = this.importPreviewData.filter(r =>
            r.selected && r.action === 'keep'
        ).length;
        const total = this.importPreviewData.filter(r => r.status !== 'error').length;

        const countEl = document.getElementById('import-selected-count');
        if (countEl) {
            let text = `${selectedForImport} of ${total} rows will be imported`;
            if (keepingCurrent > 0) {
                text += ` (${keepingCurrent} keeping current)`;
            }
            countEl.textContent = text;
        }

        // Update apply button state
        const applyBtn = document.getElementById('import-apply-btn');
        if (applyBtn) {
            applyBtn.disabled = selectedForImport === 0 && keepingCurrent === 0;
        }
    }

    closeImportPreviewModal() {
        const modal = document.getElementById('import-preview-modal');
        if (modal) {
            modal.style.setProperty('display', 'none', 'important');
        }
        this.importPreviewData = [];
        this.importFileName = '';
    }

    // ============================================
    // Auto-Suggest Click-to-Apply Functions
    // ============================================

    /**
     * Detect which field an error message is about
     */
    detectFieldFromError(errorMessage) {
        const msg = errorMessage.toLowerCase();
        if (msg.includes('company')) return 'company';
        if (msg.includes('producttype') || msg.includes('product type')) return 'productType';
        if (msg.includes('category')) return 'category';
        return 'unknown';
    }

    /**
     * Apply a suggested value to a specific field in a preview row
     */
    applySuggestion(rowIndex, field, suggestedValue) {
        if (rowIndex < 0 || rowIndex >= this.importPreviewData.length) {
            console.error('Invalid row index for suggestion:', rowIndex);
            return;
        }

        const row = this.importPreviewData[rowIndex];

        // Update the field value
        row.data[field] = suggestedValue;

        // Clear errors related to this field
        row.errors = row.errors.filter(e => {
            const errorField = this.detectFieldFromError(e);
            return errorField !== field && errorField !== 'unknown';
        });

        // Re-validate the specific field
        if (field === 'company') {
            const validation = this.validateCompanyReference(suggestedValue);
            if (validation.valid) {
                row.data.resolvedCompany = validation.company.name;
                if (validation.warning) {
                    row.warnings = row.warnings || [];
                    if (!row.warnings.some(w => w.includes(suggestedValue))) {
                        row.warnings.push(validation.message);
                    }
                }
            }
        } else if (field === 'productType') {
            const validation = this.validateProductTypeReference(suggestedValue);
            if (validation.valid) {
                row.data.resolvedProductType = validation.productType.name;
                row.data.resolvedProductTypeId = validation.productType.id;
                if (validation.warning) {
                    row.warnings = row.warnings || [];
                    if (!row.warnings.some(w => w.includes(suggestedValue))) {
                        row.warnings.push(validation.message);
                    }
                }
            }
        } else if (field === 'category') {
            const validation = this.validateCategoryReference(suggestedValue);
            if (validation.valid) {
                row.data.resolvedCategory = validation.category.name;
                row.data.resolvedCategoryId = validation.category.id;
                if (validation.warning) {
                    row.warnings = row.warnings || [];
                    if (!row.warnings.some(w => w.includes(suggestedValue))) {
                        row.warnings.push(validation.message);
                    }
                }
            }
        }

        // Update row status if no more errors
        if (row.errors.length === 0) {
            row.status = row.existingData ? 'modified' : 'new';
            row.action = row.existingData ? 'update' : 'create';
            row.selected = true;
        }

        // Re-render the preview table
        this.refreshPreviewTable();

        // Show notification
        if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
            AdminCore.showNotification(`Applied "${suggestedValue}" for ${field}`, 'success');
        } else {
            this.showToast(`Applied "${suggestedValue}" for ${field}`, 'success');
        }
    }

    /**
     * Apply all available suggestions at once
     */
    applyAllSuggestions() {
        let appliedCount = 0;

        this.importPreviewData.forEach((row, index) => {
            if (row.errors && row.errors.length > 0) {
                row.errors.forEach(error => {
                    const suggestionMatch = error.match(/\. Did you mean: (.+)\?$/);
                    if (suggestionMatch) {
                        const suggestions = suggestionMatch[1].split(', ').map(s => s.trim());
                        if (suggestions.length > 0) {
                            const baseError = error.replace(/\. Did you mean:.+$/, '');
                            const field = this.detectFieldFromError(baseError);
                            // Apply the first (best) suggestion
                            this.applySuggestionSilent(index, field, suggestions[0]);
                            appliedCount++;
                        }
                    }
                });
            }
        });

        // Re-render after all suggestions applied
        this.refreshPreviewTable();

        // Show notification
        if (appliedCount > 0) {
            if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
                AdminCore.showNotification(`Applied ${appliedCount} suggestion(s)`, 'success');
            } else {
                this.showToast(`Applied ${appliedCount} suggestion(s)`, 'success');
            }
        } else {
            if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
                AdminCore.showNotification('No suggestions to apply', 'info');
            } else {
                this.showToast('No suggestions to apply', 'info');
            }
        }
    }

    /**
     * Apply suggestion without showing notification (for bulk apply)
     */
    applySuggestionSilent(rowIndex, field, suggestedValue) {
        if (rowIndex < 0 || rowIndex >= this.importPreviewData.length) return;

        const row = this.importPreviewData[rowIndex];
        row.data[field] = suggestedValue;

        // Clear errors related to this field
        row.errors = row.errors.filter(e => {
            const errorField = this.detectFieldFromError(e);
            return errorField !== field && errorField !== 'unknown';
        });

        // Re-validate and update resolved values
        if (field === 'company') {
            const validation = this.validateCompanyReference(suggestedValue);
            if (validation.valid) {
                row.data.resolvedCompany = validation.company.name;
            }
        } else if (field === 'productType') {
            const validation = this.validateProductTypeReference(suggestedValue);
            if (validation.valid) {
                row.data.resolvedProductType = validation.productType.name;
                row.data.resolvedProductTypeId = validation.productType.id;
            }
        } else if (field === 'category') {
            const validation = this.validateCategoryReference(suggestedValue);
            if (validation.valid) {
                row.data.resolvedCategory = validation.category.name;
                row.data.resolvedCategoryId = validation.category.id;
            }
        }

        // Update row status if no more errors
        if (row.errors.length === 0) {
            row.status = row.existingData ? 'modified' : 'new';
            row.action = row.existingData ? 'update' : 'create';
            row.selected = true;
        }
    }

    /**
     * Re-render the preview table without closing the modal
     */
    refreshPreviewTable() {
        const tbody = document.getElementById('import-preview-tbody');
        if (!tbody) return;

        // Re-render all rows by calling showImportPreviewModal logic
        // but just for the table body
        const statusLabels = {
            'new': '<span style="background: #92C83E; color: white; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">New</span>',
            'modified': '<span style="background: #002B76; color: white; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">Modified</span>',
            'unchanged': '<span style="background: #9e9e9e; color: white; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">Unchanged</span>',
            'error': '<span style="background: #ff5252; color: white; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">Error</span>'
        };

        tbody.innerHTML = '';

        this.importPreviewData.forEach((row, index) => {
            const tr = document.createElement('tr');
            tr.className = `import-row import-row-${row.status}`;
            tr.dataset.index = index;

            if (row.status === 'error') {
                tr.style.backgroundColor = '#ffebee';
            } else if (row.status === 'modified') {
                tr.style.backgroundColor = '#e8f5e9';
            }

            let changesSummary = '';
            if (row.status === 'modified' && row.changes && row.changes.length > 0) {
                const fieldNames = row.changes.map(c => {
                    if (c.field === 'productType') return 'Type';
                    if (c.field === 'description') return 'Description';
                    if (c.field === 'company') return 'Company';
                    if (c.field === 'status') return 'Status';
                    return c.field;
                });
                changesSummary = `<div style="font-size: 10px; color: #666; margin-top: 4px;">${fieldNames.join(', ')} changed</div>`;
            }

            // Error display with clickable suggestions
            let errorDisplay = '';
            if (row.errors && row.errors.length > 0) {
                errorDisplay = '<div style="margin-top: 4px;">';
                row.errors.forEach(error => {
                    const suggestionMatch = error.match(/\. Did you mean: (.+)\?$/);
                    if (suggestionMatch) {
                        const baseError = error.replace(/\. Did you mean:.+$/, '');
                        const suggestions = suggestionMatch[1].split(', ').map(s => s.trim());
                        const field = this.detectFieldFromError(baseError);

                        errorDisplay += `
                            <div style="color: #ff5252; font-size: 11px; font-weight: 500; margin-bottom: 4px;">
                                ${baseError}
                            </div>
                            <div style="display: flex; flex-wrap: wrap; gap: 4px; align-items: center;">
                                <span style="font-size: 10px; color: #666;">Click to apply:</span>
                                ${suggestions.map(s => `
                                    <button type="button" class="suggestion-btn"
                                            onclick="productManagement.applySuggestion(${index}, '${field}', '${s.replace(/'/g, "\\'")}')"
                                            style="padding: 2px 8px; font-size: 11px; background: #e3f2fd; border: 1px solid #2196f3; border-radius: 12px; cursor: pointer; color: #1976d2; font-weight: 500;">
                                        ${s}
                                    </button>
                                `).join('')}
                            </div>
                        `;
                    } else {
                        errorDisplay += `<div style="color: #ff5252; font-size: 11px; font-weight: 500;">${error}</div>`;
                    }
                });
                errorDisplay += '</div>';
            }

            let warningDisplay = '';
            if (row.warnings && row.warnings.length > 0) {
                warningDisplay = `<div style="color: #ff9800; font-size: 11px; margin-top: 4px; font-weight: 500;"><i class="fas fa-exclamation-triangle"></i> ${row.warnings.join('<br>')}</div>`;
            }

            tr.innerHTML = `
                <td style="text-align: center; vertical-align: top;">
                    <input type="checkbox" class="import-row-checkbox" data-index="${index}" ${row.selected ? 'checked' : ''} ${row.status === 'error' || row.status === 'unchanged' ? 'disabled' : ''}>
                </td>
                <td style="text-align: center; vertical-align: top;">${row.rowNumber}</td>
                <td style="vertical-align: top;">${statusLabels[row.status]}${changesSummary}</td>
                <td style="vertical-align: top;">${this.renderFieldWithDiff(row, 'name')}</td>
                <td style="vertical-align: top;">${this.renderFieldWithDiff(row, 'description')}${errorDisplay}${warningDisplay}</td>
                <td style="vertical-align: top;">${this.renderActionDropdown(row, index)}</td>
            `;

            tbody.appendChild(tr);
        });

        // Re-bind checkbox events
        tbody.querySelectorAll('.import-row-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                this.updateImportRowSelection(parseInt(e.target.dataset.index), e.target.checked);
            });
        });

        // Re-bind action dropdown events
        tbody.querySelectorAll('.import-action-select').forEach(select => {
            select.addEventListener('change', (e) => {
                this.updateImportRowAction(parseInt(e.target.dataset.index), e.target.value);
            });
        });

        // Update stats
        const stats = {
            total: this.importPreviewData.length,
            new: this.importPreviewData.filter(r => r.status === 'new').length,
            modified: this.importPreviewData.filter(r => r.status === 'modified').length,
            unchanged: this.importPreviewData.filter(r => r.status === 'unchanged').length,
            errors: this.importPreviewData.filter(r => r.status === 'error').length
        };

        document.getElementById('import-stat-new').textContent = stats.new;
        document.getElementById('import-stat-modified').textContent = stats.modified;
        document.getElementById('import-stat-unchanged').textContent = stats.unchanged;
        document.getElementById('import-stat-errors').textContent = stats.errors;

        this.updateSelectedCount();

        // Update Apply All Suggestions button visibility
        this.updateApplyAllSuggestionsButton();
    }

    /**
     * Update the Apply All Suggestions button based on available suggestions
     */
    updateApplyAllSuggestionsButton() {
        const rowsWithSuggestions = this.importPreviewData.filter(r =>
            r.errors && r.errors.some(e => e.includes('Did you mean'))
        );

        let btn = document.getElementById('apply-all-suggestions-btn');
        const summaryDiv = document.getElementById('import-summary');

        if (rowsWithSuggestions.length > 0) {
            if (!btn && summaryDiv) {
                btn = document.createElement('button');
                btn.id = 'apply-all-suggestions-btn';
                btn.type = 'button';
                btn.innerHTML = `<i class="fas fa-magic"></i> Apply All Suggestions (${rowsWithSuggestions.length})`;
                btn.style.cssText = 'margin-left: 12px; padding: 6px 12px; font-size: 12px; background: #4caf50; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500;';
                btn.onclick = () => this.applyAllSuggestions();
                summaryDiv.appendChild(btn);
            } else if (btn) {
                btn.innerHTML = `<i class="fas fa-magic"></i> Apply All Suggestions (${rowsWithSuggestions.length})`;
                btn.style.display = 'inline-block';
            }
        } else if (btn) {
            btn.style.display = 'none';
        }
    }

    async applySelectedImport() {
        const rowsToProcess = this.importPreviewData.filter(r =>
            r.selected && r.action !== 'skip' && r.status !== 'error'
        );

        if (rowsToProcess.length === 0) {
            this.showToast('No rows selected for import', 'warning');
            return;
        }

        // Separate by action type
        const toCreate = rowsToProcess.filter(r => r.action === 'create');
        const toUpdate = rowsToProcess.filter(r => r.action === 'update');
        const toKeep = rowsToProcess.filter(r => r.action === 'keep');

        let successCount = 0;
        let errorCount = 0;
        const errors = [];

        this.showLoading(true, 'Importing products...');

        try {
            // Process creates
            for (const row of toCreate) {
                try {
                    const productData = {
                        name: row.data.name,
                        description: row.data.description || '',
                        isActive: row.data.status?.toLowerCase() !== 'inactive'
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
                        successCount++;
                    } else {
                        errorCount++;
                        errors.push(`Row ${row.rowNumber} (${row.data.name}): ${result.message}`);
                    }
                } catch (error) {
                    errorCount++;
                    errors.push(`Row ${row.rowNumber} (${row.data.name}): ${error.message}`);
                }
            }

            // Process updates
            for (const row of toUpdate) {
                try {
                    const productData = {
                        name: row.data.name,
                        description: row.data.description || row.existingData?.description || '',
                        isActive: row.data.status?.toLowerCase() !== 'inactive'
                    };

                    const response = await fetch(`/Admin/UpdateProduct/${row.existingData.id}`, {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': this.getAntiForgeryToken()
                        },
                        body: JSON.stringify(productData)
                    });

                    const result = await response.json();
                    if (result.success) {
                        successCount++;
                    } else {
                        errorCount++;
                        errors.push(`Row ${row.rowNumber} (${row.data.name}): ${result.message}`);
                    }
                } catch (error) {
                    errorCount++;
                    errors.push(`Row ${row.rowNumber} (${row.data.name}): ${error.message}`);
                }
            }

            // Count keeps as success (they chose to keep current)
            successCount += toKeep.length;

            this.closeImportPreviewModal();
            await this.loadProducts();
            this.renderProducts();

            if (errorCount === 0) {
                this.showToast(`Successfully imported ${successCount} products`, 'success');
            } else {
                this.showToast(`Imported ${successCount} products with ${errorCount} errors`, 'warning');
                console.error('Import errors:', errors);
            }

        } catch (error) {
            console.error('Import error:', error);
            this.showToast('Error during import. Please try again.', 'error');
        } finally {
            this.showLoading(false);
        }
    }

    parseCSVLine(line) {
        const result = [];
        let current = '';
        let inQuotes = false;

        for (let i = 0; i < line.length; i++) {
            const char = line[i];
            const nextChar = line[i + 1];

            if (inQuotes) {
                if (char === '"' && nextChar === '"') {
                    current += '"';
                    i++;
                } else if (char === '"') {
                    inQuotes = false;
                } else {
                    current += char;
                }
            } else {
                if (char === '"') {
                    inQuotes = true;
                } else if (char === ',') {
                    result.push(current);
                    current = '';
                } else {
                    current += char;
                }
            }
        }
        result.push(current);
        return result;
    }

    /**
     * Normalize CSV header names to standard field names.
     * This makes imports case-insensitive and handles common variations.
     *
     * Supported variations:
     * - name, Name, NAME, ProductName, Product Name, product_name
     * - description, Description, Desc, desc
     * - company, Company, CompanyName, Company Name, company_name
     * - producttype, ProductType, Product Type, Type, product_type, ProductTypeName
     * - status, Status, IsActive, Active, is_active
     */
    normalizeHeaderName(header) {
        // Remove spaces, underscores, dashes and convert to lowercase for comparison
        const normalized = header.toLowerCase().replace(/[\s_-]/g, '');

        // Map common variations to standard field names
        const headerMappings = {
            // Name variations
            'name': 'name',
            'productname': 'name',

            // Description variations
            'description': 'description',
            'desc': 'description',

            // Company variations
            'company': 'company',
            'companyname': 'company',

            // Product Type variations
            'producttype': 'producttype',
            'producttypename': 'producttype',
            'type': 'producttype',
            'category': 'producttype',

            // Status variations
            'status': 'status',
            'isactive': 'status',
            'active': 'status'
        };

        return headerMappings[normalized] || normalized;
    }

    /**
     * Fuzzy match a company name against available companies
     * Returns { match: Company|null, confidence: 'exact'|'normalized'|'fuzzy'|'none', originalValue: string }
     */
    findMatchingCompany(inputCompanyName) {
        if (!inputCompanyName || !inputCompanyName.trim()) {
            return { match: null, confidence: 'none', originalValue: inputCompanyName };
        }

        // Get companies from global scope (loaded by Management.cshtml inline script)
        const availableCompanies = window.companies || [];
        if (availableCompanies.length === 0) {
            console.warn('No companies available for matching. Companies may not be loaded yet.');
            return { match: null, confidence: 'none', originalValue: inputCompanyName };
        }

        const input = inputCompanyName.trim();
        const inputLower = input.toLowerCase();
        const inputNormalized = this.normalizeCompanyName(input);

        // 1. Exact match (case-insensitive)
        let exactMatch = availableCompanies.find(c =>
            c.name && c.name.toLowerCase() === inputLower
        );
        if (exactMatch) {
            return { match: exactMatch, confidence: 'exact', originalValue: input };
        }

        // 2. Normalized match (removes common suffixes, punctuation)
        let normalizedMatch = availableCompanies.find(c =>
            c.name && this.normalizeCompanyName(c.name) === inputNormalized
        );
        if (normalizedMatch) {
            return { match: normalizedMatch, confidence: 'normalized', originalValue: input };
        }

        // 3. Fuzzy match (Levenshtein distance or substring matching)
        let bestFuzzyMatch = null;
        let bestFuzzyScore = 0;

        for (const company of availableCompanies) {
            if (!company.name) continue;

            const companyNormalized = this.normalizeCompanyName(company.name);

            // Check if one contains the other
            if (inputNormalized.includes(companyNormalized) || companyNormalized.includes(inputNormalized)) {
                const score = Math.min(inputNormalized.length, companyNormalized.length) /
                             Math.max(inputNormalized.length, companyNormalized.length);
                if (score > bestFuzzyScore && score >= 0.6) {
                    bestFuzzyScore = score;
                    bestFuzzyMatch = company;
                }
            }

            // Check similarity score
            const similarity = this.calculateSimilarity(inputNormalized, companyNormalized);
            if (similarity > bestFuzzyScore && similarity >= 0.8) {
                bestFuzzyScore = similarity;
                bestFuzzyMatch = company;
            }
        }

        if (bestFuzzyMatch) {
            return { match: bestFuzzyMatch, confidence: 'fuzzy', originalValue: input };
        }

        return { match: null, confidence: 'none', originalValue: input };
    }

    /**
     * Normalize company name for matching
     * Removes common suffixes, punctuation, extra whitespace
     */
    normalizeCompanyName(name) {
        if (!name) return '';

        return name
            .toLowerCase()
            .replace(/[.,\-_'"]/g, ' ')  // Replace punctuation with spaces
            .replace(/\b(inc|incorporated|corp|corporation|llc|ltd|limited|co|company|enterprises|group|holdings)\b/gi, '')
            .replace(/\s+/g, ' ')  // Normalize whitespace
            .trim();
    }

    /**
     * Calculate string similarity (simple approach)
     */
    calculateSimilarity(str1, str2) {
        if (str1 === str2) return 1;
        if (!str1 || !str2) return 0;

        const longer = str1.length > str2.length ? str1 : str2;
        const shorter = str1.length > str2.length ? str2 : str1;

        if (longer.length === 0) return 1;

        // Simple character overlap score
        let matches = 0;
        for (let i = 0; i < shorter.length; i++) {
            if (longer.includes(shorter[i])) matches++;
        }

        return matches / longer.length;
    }

    /**
     * Validate company reference and return error/warning if not found
     */
    validateCompanyReference(companyName) {
        const result = this.findMatchingCompany(companyName);

        if (result.confidence === 'exact') {
            return { valid: true, company: result.match, message: null };
        }

        if (result.confidence === 'normalized' || result.confidence === 'fuzzy') {
            return {
                valid: true,
                company: result.match,
                message: `Company "${result.originalValue}" matched to "${result.match.name}" (${result.confidence} match)`,
                warning: true
            };
        }

        // Not found
        return {
            valid: false,
            company: null,
            message: `Company "${companyName}" not found in system`,
            suggestions: this.getSimilarCompanies(companyName)
        };
    }

    /**
     * Get similar company names for suggestions
     */
    getSimilarCompanies(inputName) {
        if (!inputName) return [];

        const availableCompanies = window.companies || [];
        const inputNormalized = this.normalizeCompanyName(inputName);

        return availableCompanies
            .filter(c => c.name)
            .map(c => ({
                name: c.name,
                similarity: this.calculateSimilarity(inputNormalized, this.normalizeCompanyName(c.name))
            }))
            .filter(c => c.similarity > 0.3)
            .sort((a, b) => b.similarity - a.similarity)
            .slice(0, 3)
            .map(c => c.name);
    }

    // ============================================
    // ProductType Fuzzy Matching Functions
    // ============================================

    /**
     * Normalize ProductType name for comparison
     * Removes common suffixes like "type", "category", "products", etc.
     */
    normalizeProductTypeName(name) {
        if (!name) return '';
        return name
            .toLowerCase()
            .replace(/[.,\-_'"]/g, ' ')
            .replace(/\b(type|types|category|categories|products?|materials?|items?|goods?)\b/gi, '')
            .replace(/\s+/g, ' ')
            .trim();
    }

    /**
     * Find matching ProductType with 3-tier matching strategy
     * Tier 1: Exact match (case-insensitive)
     * Tier 2: Normalized match (without suffixes)
     * Tier 3: Fuzzy match (similarity >= 0.8)
     */
    findMatchingProductType(inputName) {
        if (!inputName || !inputName.trim()) {
            return { match: null, confidence: null, originalValue: inputName };
        }

        const availableTypes = window.productTypes || this.productTypes || [];
        const inputLower = inputName.trim().toLowerCase();
        const inputNormalized = this.normalizeProductTypeName(inputName);

        // Tier 1: Exact match (case-insensitive)
        let match = availableTypes.find(t => t.name && t.name.toLowerCase() === inputLower);
        if (match) {
            return { match, confidence: 'exact', originalValue: inputName };
        }

        // Tier 2: Normalized match
        match = availableTypes.find(t => t.name && this.normalizeProductTypeName(t.name) === inputNormalized);
        if (match) {
            return { match, confidence: 'normalized', originalValue: inputName };
        }

        // Tier 3: Fuzzy match (similarity >= 0.8)
        const fuzzyMatches = availableTypes
            .filter(t => t.name)
            .map(t => ({
                type: t,
                similarity: this.calculateSimilarity(inputNormalized, this.normalizeProductTypeName(t.name))
            }))
            .filter(t => t.similarity >= 0.8)
            .sort((a, b) => b.similarity - a.similarity);

        if (fuzzyMatches.length > 0) {
            return { match: fuzzyMatches[0].type, confidence: 'fuzzy', originalValue: inputName };
        }

        return { match: null, confidence: null, originalValue: inputName };
    }

    /**
     * Get similar ProductType names for suggestions
     * Returns top 3 matches with similarity > 0.3
     */
    getSimilarProductTypes(inputName) {
        if (!inputName) return [];

        const availableTypes = window.productTypes || this.productTypes || [];
        const inputNormalized = this.normalizeProductTypeName(inputName);

        return availableTypes
            .filter(t => t.name)
            .map(t => ({
                id: t.id,
                name: t.name,
                similarity: this.calculateSimilarity(inputNormalized, this.normalizeProductTypeName(t.name))
            }))
            .filter(t => t.similarity > 0.3)
            .sort((a, b) => b.similarity - a.similarity)
            .slice(0, 3);
    }

    /**
     * Validate ProductType reference with fuzzy matching
     * Returns validation result with suggestions if not found
     */
    validateProductTypeReference(productTypeName) {
        const result = this.findMatchingProductType(productTypeName);

        if (result.match) {
            if (result.confidence === 'exact') {
                return { valid: true, productType: result.match, message: null };
            }
            // Normalized or fuzzy match - valid but show warning
            return {
                valid: true,
                productType: result.match,
                message: `ProductType "${result.originalValue}" matched to "${result.match.name}" (${result.confidence} match)`,
                warning: true
            };
        }

        // Not found - return suggestions
        const suggestions = this.getSimilarProductTypes(productTypeName);
        return {
            valid: false,
            productType: null,
            message: `ProductType "${productTypeName}" not found in system`,
            suggestions: suggestions.map(s => s.name)
        };
    }

    // ============================================
    // Category Fuzzy Matching Functions
    // ============================================

    /**
     * Normalize Category name for comparison
     */
    normalizeCategoryName(name) {
        if (!name) return '';
        return name
            .toLowerCase()
            .replace(/[.,\-_'"]/g, ' ')
            .replace(/\b(category|categories|type|types|group|groups)\b/gi, '')
            .replace(/\s+/g, ' ')
            .trim();
    }

    /**
     * Find matching Category with 3-tier matching strategy
     */
    findMatchingCategory(inputName) {
        if (!inputName || !inputName.trim()) {
            return { match: null, confidence: null, originalValue: inputName };
        }

        const availableCategories = window.categories || this.categories || [];
        const inputLower = inputName.trim().toLowerCase();
        const inputNormalized = this.normalizeCategoryName(inputName);

        // Tier 1: Exact match
        let match = availableCategories.find(c => c.name && c.name.toLowerCase() === inputLower);
        if (match) {
            return { match, confidence: 'exact', originalValue: inputName };
        }

        // Tier 2: Normalized match
        match = availableCategories.find(c => c.name && this.normalizeCategoryName(c.name) === inputNormalized);
        if (match) {
            return { match, confidence: 'normalized', originalValue: inputName };
        }

        // Tier 3: Fuzzy match (similarity >= 0.8)
        const fuzzyMatches = availableCategories
            .filter(c => c.name)
            .map(c => ({
                category: c,
                similarity: this.calculateSimilarity(inputNormalized, this.normalizeCategoryName(c.name))
            }))
            .filter(c => c.similarity >= 0.8)
            .sort((a, b) => b.similarity - a.similarity);

        if (fuzzyMatches.length > 0) {
            return { match: fuzzyMatches[0].category, confidence: 'fuzzy', originalValue: inputName };
        }

        return { match: null, confidence: null, originalValue: inputName };
    }

    /**
     * Get similar Category names for suggestions
     */
    getSimilarCategories(inputName) {
        if (!inputName) return [];

        const availableCategories = window.categories || this.categories || [];
        const inputNormalized = this.normalizeCategoryName(inputName);

        return availableCategories
            .filter(c => c.name)
            .map(c => ({
                id: c.id,
                name: c.name,
                similarity: this.calculateSimilarity(inputNormalized, this.normalizeCategoryName(c.name))
            }))
            .filter(c => c.similarity > 0.3)
            .sort((a, b) => b.similarity - a.similarity)
            .slice(0, 3);
    }

    /**
     * Validate Category reference with fuzzy matching
     */
    validateCategoryReference(categoryName) {
        const result = this.findMatchingCategory(categoryName);

        if (result.match) {
            if (result.confidence === 'exact') {
                return { valid: true, category: result.match, message: null };
            }
            return {
                valid: true,
                category: result.match,
                message: `Category "${result.originalValue}" matched to "${result.match.name}" (${result.confidence} match)`,
                warning: true
            };
        }

        const suggestions = this.getSimilarCategories(categoryName);
        return {
            valid: false,
            category: null,
            message: `Category "${categoryName}" not found in system`,
            suggestions: suggestions.map(s => s.name)
        };
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

    copyAllIdentifiers(identifiers, productName) {
        try {
            // Create formatted text with all identifiers
            let copyText = `Product: ${productName}\n\n`;

            // Group by type
            const grouped = {};
            identifiers.forEach(pi => {
                if (!grouped[pi.identifierType]) {
                    grouped[pi.identifierType] = [];
                }
                grouped[pi.identifierType].push(pi.identifierValue);
            });

            // Format: Type: value1, value2, value3
            Object.keys(grouped).forEach(type => {
                copyText += `${type}: ${grouped[type].join(', ')}\n`;
            });

            // Copy to clipboard
            navigator.clipboard.writeText(copyText).then(() => {
                this.showToast(`Copied ${identifiers.length} identifiers for ${productName}`, 'success');
            }).catch(err => {
                console.error('Failed to copy:', err);
                this.showToast('Failed to copy identifiers. Please try again.', 'error');
            });
        } catch (error) {
            console.error('Error copying identifiers:', error);
            this.showToast('Error copying identifiers. Please try again.', 'error');
        }
    }

    // ============================================
    // Import Order Enforcement Functions
    // ============================================

    /**
     * Check if prerequisite data exists before allowing import
     * Returns array of issues if prerequisites are missing
     */
    checkImportPrerequisites() {
        const issues = [];

        // Check if companies exist
        const companies = window.companies || this.companies || [];
        if (companies.length === 0) {
            issues.push({
                entity: 'Companies',
                message: 'No companies exist in the system. Products must be associated with a company.',
                importUrl: '/Company',
                importLabel: 'Go to Company Management'
            });
        }

        // Check if product types exist
        const productTypes = window.productTypes || this.productTypes || [];
        if (productTypes.length === 0) {
            issues.push({
                entity: 'Product Types',
                message: 'No product types exist in the system. Products should have a type classification.',
                importUrl: '/Admin/ProductTypes',
                importLabel: 'Configure Product Types'
            });
        }

        return issues;
    }

    /**
     * Handle import button click - check prerequisites before showing file picker
     */
    handleImportClick() {
        // Check prerequisites BEFORE showing file picker
        const issues = this.checkImportPrerequisites();

        if (issues.length > 0) {
            this.showPrerequisiteModal(issues);
            return; // Don't show file picker
        }

        // Proceed with normal file picker
        const fileInput = document.getElementById('import-file-input');
        if (fileInput) {
            fileInput.click();
        } else {
            this.showToast('Import file input not found', 'error');
        }
    }

    /**
     * Show modal explaining missing prerequisites and how to fix them
     */
    showPrerequisiteModal(issues) {
        // Remove any existing modal
        const existingModal = document.getElementById('prerequisite-modal');
        if (existingModal) {
            existingModal.remove();
        }

        const modalHtml = `
            <div id="prerequisite-modal" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 9997;">
                <!-- Backdrop -->
                <div style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0, 0, 0, 0.85); z-index: 9998; backdrop-filter: blur(3px);"></div>

                <!-- Modal Container -->
                <div style="position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); z-index: 9999; background: white; border-radius: 8px; padding: 0; max-width: 550px; width: 90%; box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);">

                    <!-- Header -->
                    <div style="padding: 16px 20px; background: #f8f9fa; border-bottom: 2px solid #e9ecef; border-radius: 8px 8px 0 0;">
                        <h3 style="margin: 0; color: #d32f2f; font-size: 18px; display: flex; align-items: center; gap: 10px;">
                            <i class="fas fa-exclamation-triangle"></i>
                            Cannot Import Products
                        </h3>
                    </div>

                    <!-- Body -->
                    <div style="padding: 20px;">
                        <p style="margin: 0 0 16px 0; color: #666; font-size: 14px;">
                            Products reference other entities that must exist first. Please set up the following before importing products:
                        </p>

                        <div style="background: #fff3cd; border: 1px solid #ffc107; border-radius: 6px; overflow: hidden;">
                            ${issues.map((issue, idx) => `
                                <div style="display: flex; justify-content: space-between; align-items: center; padding: 12px 16px; ${idx > 0 ? 'border-top: 1px solid #ffeeba;' : ''}">
                                    <div style="flex: 1;">
                                        <div style="font-weight: 600; color: #856404; font-size: 14px;">
                                            <i class="fas fa-exclamation-circle" style="margin-right: 6px;"></i>
                                            ${issue.entity}
                                        </div>
                                        <div style="font-size: 12px; color: #856404; margin-top: 4px;">${issue.message}</div>
                                    </div>
                                    <a href="${issue.importUrl}"
                                       style="margin-left: 12px; padding: 6px 12px; font-size: 12px; background: #ffc107; color: #856404; border: none; border-radius: 4px; text-decoration: none; font-weight: 600; white-space: nowrap;">
                                        ${issue.importLabel}
                                    </a>
                                </div>
                            `).join('')}
                        </div>

                        <div style="margin-top: 16px; padding: 12px; background: #e3f2fd; border-radius: 6px; border: 1px solid #2196f3;">
                            <div style="font-size: 13px; color: #1565c0;">
                                <i class="fas fa-info-circle" style="margin-right: 6px;"></i>
                                <strong>Recommended Import Order:</strong>
                            </div>
                            <div style="font-size: 12px; color: #1976d2; margin-top: 8px; padding-left: 22px;">
                                1. <strong>Companies</strong> (required) → 2. <strong>Product Types</strong> (recommended) → 3. <strong>Products</strong>
                            </div>
                        </div>
                    </div>

                    <!-- Footer -->
                    <div style="padding: 12px 20px; background: #f8f9fa; border-top: 2px solid #e9ecef; border-radius: 0 0 8px 8px; display: flex; justify-content: flex-end; gap: 12px;">
                        <button type="button"
                                onclick="document.getElementById('prerequisite-modal').remove()"
                                style="padding: 8px 20px; font-size: 14px; background: #6c757d; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500;">
                            Cancel
                        </button>
                        <button type="button"
                                onclick="document.getElementById('prerequisite-modal').remove(); document.getElementById('import-file-input').click();"
                                style="padding: 8px 20px; font-size: 14px; background: #ff9800; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500;">
                            Import Anyway
                        </button>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHtml);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing Product Management...');
    window.productManagement = new ProductManagement();
});