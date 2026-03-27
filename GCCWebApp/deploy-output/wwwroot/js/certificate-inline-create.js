/**
 * Certificate Inline Entity Creation
 * Allows creating companies and products directly from the Certificate Create/Edit pages
 * Uses dropdown-integrated "Add New..." option pattern
 */

window.CertificateInlineCreate = window.CertificateInlineCreate || {};

(function() {
    'use strict';

    const ADD_NEW_COMPANY_VALUE = '__ADD_NEW_COMPANY__';
    const ADD_NEW_PRODUCT_VALUE = '__ADD_NEW_PRODUCT__';

    // Cache for default category ID
    let defaultCategoryId = null;

    /**
     * Get default category ID for product creation
     */
    async function getDefaultCategoryId() {
        if (defaultCategoryId !== null) return defaultCategoryId;

        try {
            const response = await fetch('/Product/GetCategories');
            const result = await response.json();
            const success = result.success ?? result.Success;
            const data = result.data ?? result.Data;

            if (success && data && data.length > 0) {
                defaultCategoryId = data[0].id ?? data[0].Id;
                return defaultCategoryId;
            }
        } catch (error) {
            console.error('Error fetching categories:', error);
        }
        return null;
    }

    // Get CSRF token for AJAX requests
    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    /**
     * Show toast notification (with AdminCore fallback)
     */
    function showNotification(message, type = 'info') {
        // Try AdminCore first
        if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
            AdminCore.showNotification(message, type);
            return;
        }

        // Fallback: Center-screen modal notification (matches AdminCore modal style)
        const colors = { success: '#92C83E', error: '#FF5252', warning: '#FFC107', info: '#002B76' };
        const titles = { success: 'Success', error: 'Error', warning: 'Warning', info: 'Information' };
        const bgColor = colors[type] || colors.info;
        const btnTextColor = type === 'warning' ? '#333333' : '#ffffff';

        const overlay = document.createElement('div');
        overlay.style.cssText = `
            position: fixed; top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0,0,0,0.85); backdrop-filter: blur(3px);
            z-index: 10001; display: flex; align-items: center;
            justify-content: center;
        `;
        overlay.innerHTML = `
            <div style="background: #fff; border-radius: 8px; padding: 30px; max-width: 450px; width: 90%; text-align: center; box-shadow: 0 10px 40px rgba(0,0,0,0.5);">
                <h2 style="color: ${type === 'error' ? '#FF5252' : '#002B76'}; font-size: 22px; font-weight: 600; margin: 0 0 15px 0;">${titles[type] || 'Information'}</h2>
                <p style="color: #333; font-size: 16px; line-height: 1.5; margin: 0 0 25px 0;">${message}</p>
                <button style="width: 100%; padding: 12px 30px; border: none; border-radius: 4px; font-size: 16px; font-weight: 500; cursor: pointer; background: ${bgColor}; color: ${btnTextColor};">OK</button>
            </div>
        `;

        overlay.querySelector('button').addEventListener('click', () => overlay.remove());
        overlay.addEventListener('click', (e) => { if (e.target === overlay) overlay.remove(); });
        document.body.appendChild(overlay);
    }

    /**
     * Show Quick Add Company Modal
     */
    CertificateInlineCreate.showQuickAddCompany = async function() {
        const modalHtml = `
            <div id="quickAddCompanyModal" class="admin-modal" style="display: flex; position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 10000; align-items: center; justify-content: center;">
                <div style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0, 0, 0, 0.85); z-index: 9998; backdrop-filter: blur(3px);"></div>
                <div class="admin-modal-container" style="position: relative; z-index: 9999; max-width: 500px; width: 90%; background-color: white; border-radius: 8px; box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);">
                    <div class="admin-modal-header" style="background-color: #f8f9fa; border-bottom: 2px solid #e9ecef; padding: 16px 20px; display: flex; justify-content: space-between; align-items: center;">
                        <h3 class="admin-modal-title" style="color: #002B76; font-weight: 600; margin: 0; font-size: 18px;">Quick Add Company</h3>
                        <button type="button" class="admin-modal-close" onclick="CertificateInlineCreate.closeModal('quickAddCompanyModal')" style="background: none; border: none; font-size: 24px; cursor: pointer; color: #6c757d; line-height: 1;">&times;</button>
                    </div>
                    <div class="admin-modal-body" style="background-color: white; padding: 24px;">
                        <div class="form-group" style="margin-bottom: 16px;">
                            <label for="quickCompanyName" style="font-weight: 600; margin-bottom: 8px; display: block; color: #333;">Company Name <span class="text-danger">*</span></label>
                            <input type="text" id="quickCompanyName" class="form-control" style="width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px;" placeholder="Enter company name" required>
                        </div>
                        <div class="form-group" style="margin-bottom: 16px;">
                            <label for="quickCompanyEmail" style="font-weight: 600; margin-bottom: 8px; display: block; color: #333;">Email</label>
                            <input type="email" id="quickCompanyEmail" class="form-control" style="width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px;" placeholder="company@example.com">
                        </div>
                        <div class="form-group" style="margin-bottom: 0;">
                            <label for="quickCompanyUrl" style="font-weight: 600; margin-bottom: 8px; display: block; color: #333;">Website URL</label>
                            <input type="url" id="quickCompanyUrl" class="form-control" style="width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px;" placeholder="https://www.example.com">
                        </div>
                        <div id="quickCompanyError" style="color: #dc3545; margin-top: 12px; display: none;"></div>
                    </div>
                    <div class="admin-modal-footer" style="background-color: #f8f9fa; border-top: 2px solid #e9ecef; padding: 16px 20px; display: flex; justify-content: flex-end; gap: 12px;">
                        <button type="button" class="btn btn-outline" onclick="CertificateInlineCreate.closeModal('quickAddCompanyModal')" style="padding: 10px 20px; border: 1px solid #92C83E; color: #92C83E; background: white; border-radius: 4px; cursor: pointer; font-weight: 500;">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="CertificateInlineCreate.saveQuickCompany()" style="padding: 10px 20px; background-color: #92C83E; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500;">Add Company</button>
                    </div>
                </div>
            </div>
        `;

        // Remove any existing modal
        const existingModal = document.getElementById('quickAddCompanyModal');
        if (existingModal) existingModal.remove();

        // Add modal to page
        document.body.insertAdjacentHTML('beforeend', modalHtml);

        // Focus the name input
        setTimeout(() => {
            document.getElementById('quickCompanyName').focus();
        }, 100);

        // Add enter key handler
        document.getElementById('quickCompanyName').addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                CertificateInlineCreate.saveQuickCompany();
            }
        });
    };

    /**
     * Save Quick Add Company
     */
    CertificateInlineCreate.saveQuickCompany = async function() {
        const nameInput = document.getElementById('quickCompanyName');
        const emailInput = document.getElementById('quickCompanyEmail');
        const urlInput = document.getElementById('quickCompanyUrl');
        const errorDiv = document.getElementById('quickCompanyError');

        const name = nameInput.value.trim();
        const email = emailInput.value.trim();
        const url = urlInput.value.trim();

        // Validate
        if (!name) {
            errorDiv.textContent = 'Company name is required';
            errorDiv.style.display = 'block';
            nameInput.focus();
            return;
        }

        errorDiv.style.display = 'none';

        try {
            const response = await fetch('/Company/SaveCompany', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({
                    Id: 0,
                    Name: name,
                    Email: email || null,
                    Url: url || null,
                    IsActive: true
                })
            });

            const result = await response.json();

            if (result.success && result.data) {
                // Add new company to dropdown at the end and select it
                const companySelect = document.getElementById('CompanyId');
                if (companySelect) {
                    const option = document.createElement('option');
                    option.value = result.data.id;
                    option.textContent = result.data.name;

                    // Append to end of dropdown
                    companySelect.appendChild(option);

                    // Select the new option and update stored previous value
                    companySelect.value = result.data.id;
                    companySelect.dataset.previousValue = result.data.id;

                    // Trigger change event
                    companySelect.dispatchEvent(new Event('change'));
                }

                // Close modal
                CertificateInlineCreate.closeModal('quickAddCompanyModal');

                // Show success notification
                showNotification('Company "' + name + '" created successfully', 'success');
            } else {
                errorDiv.textContent = result.message || 'Failed to create company';
                errorDiv.style.display = 'block';
            }
        } catch (error) {
            console.error('Error creating company:', error);
            errorDiv.textContent = 'Error creating company: ' + error.message;
            errorDiv.style.display = 'block';
        }
    };

    /**
     * Show Quick Add Product Modal
     */
    CertificateInlineCreate.showQuickAddProduct = async function() {
        // Get current company selection
        const companySelect = document.getElementById('CompanyId');
        const selectedCompanyId = companySelect ? companySelect.value : '';
        const selectedCompanyName = companySelect && companySelect.selectedIndex >= 0
            ? companySelect.options[companySelect.selectedIndex].text
            : '';

        // Build company options from the existing dropdown (excluding separator and "Add New..." option)
        let companyOptions = '<option value="">Select a company...</option>';
        if (companySelect) {
            for (let i = 0; i < companySelect.options.length; i++) {
                const opt = companySelect.options[i];
                // Skip empty values, disabled options (separator), and the "Add New" option
                if (opt.value && !opt.disabled && opt.value !== ADD_NEW_COMPANY_VALUE) {
                    const selected = opt.value === selectedCompanyId ? 'selected' : '';
                    companyOptions += `<option value="${opt.value}" ${selected}>${opt.textContent}</option>`;
                }
            }
        }

        // Build category options from the existing page dropdown if available
        let categoryOptions = '<option value="">Select category...</option>';
        const pageCategorySelect = document.getElementById('ProductCategoryId');
        if (pageCategorySelect) {
            for (let i = 0; i < pageCategorySelect.options.length; i++) {
                const opt = pageCategorySelect.options[i];
                if (opt.value) {
                    categoryOptions += `<option value="${opt.value}">${opt.textContent}</option>`;
                }
            }
        }

        const modalHtml = `
            <div id="quickAddProductModal" class="admin-modal" style="display: flex; position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 10000; align-items: center; justify-content: center;">
                <div style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0, 0, 0, 0.85); z-index: 9998; backdrop-filter: blur(3px);"></div>
                <div class="admin-modal-container" style="position: relative; z-index: 9999; max-width: 550px; width: 90%; background-color: white; border-radius: 8px; box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5); max-height: 90vh; overflow-y: auto;">
                    <div class="admin-modal-header" style="background-color: #f8f9fa; border-bottom: 2px solid #e9ecef; padding: 16px 20px; display: flex; justify-content: space-between; align-items: center;">
                        <h3 class="admin-modal-title" style="color: #002B76; font-weight: 600; margin: 0; font-size: 18px;">Add New Product</h3>
                        <button type="button" class="admin-modal-close" onclick="CertificateInlineCreate.closeModal('quickAddProductModal')" style="background: none; border: none; font-size: 24px; cursor: pointer; color: #6c757d; line-height: 1;">&times;</button>
                    </div>
                    <div class="admin-modal-body" style="background-color: white; padding: 24px;">
                        <div class="form-group" style="margin-bottom: 16px;">
                            <label for="quickProductName" style="font-weight: 600; margin-bottom: 8px; display: block; color: #333;">Product Name <span class="text-danger">*</span></label>
                            <input type="text" id="quickProductName" class="form-control" style="width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px;" placeholder="Enter product name" required>
                        </div>
                        <div class="form-group" style="margin-bottom: 16px;">
                            <label for="quickProductCompany" style="font-weight: 600; margin-bottom: 8px; display: block; color: #333;">Company <span class="text-danger">*</span></label>
                            <select id="quickProductCompany" class="form-control" style="width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px;" required>
                                ${companyOptions}
                            </select>
                            <small style="color: #6c757d; font-size: 12px; margin-top: 4px; display: block;">Product will be associated with this company</small>
                        </div>
                        <div class="form-group" style="margin-bottom: 16px;">
                            <label for="quickProductCategory" style="font-weight: 600; margin-bottom: 8px; display: block; color: #333;">Product Category <span class="text-danger">*</span></label>
                            <select id="quickProductCategory" class="form-control" style="width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px;" required>
                                ${categoryOptions}
                            </select>
                        </div>
                        <div class="form-group" style="margin-bottom: 16px;">
                            <label for="quickProductType" style="font-weight: 600; margin-bottom: 8px; display: block; color: #333;">Product Type</label>
                            <select id="quickProductType" class="form-control" style="width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px;">
                                <option value="">Select category first...</option>
                            </select>
                        </div>
                        <div class="form-group" style="margin-bottom: 16px;">
                            <label for="quickProductSubcategory" style="font-weight: 600; margin-bottom: 8px; display: block; color: #333;">Product Subcategory</label>
                            <select id="quickProductSubcategory" class="form-control" style="width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px;">
                                <option value="">Select type first...</option>
                            </select>
                        </div>
                        <div class="form-group" style="margin-bottom: 0;">
                            <label for="quickProductDescription" style="font-weight: 600; margin-bottom: 8px; display: block; color: #333;">Description</label>
                            <textarea id="quickProductDescription" class="form-control" style="width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px; resize: vertical; min-height: 80px;" placeholder="Optional product description"></textarea>
                        </div>
                        <div id="quickProductError" style="color: #dc3545; margin-top: 12px; display: none;"></div>
                    </div>
                    <div class="admin-modal-footer" style="background-color: #f8f9fa; border-top: 2px solid #e9ecef; padding: 16px 20px; display: flex; justify-content: flex-end; gap: 12px;">
                        <button type="button" class="btn btn-outline" onclick="CertificateInlineCreate.closeModal('quickAddProductModal')" style="padding: 10px 20px; border: 1px solid #92C83E; color: #92C83E; background: white; border-radius: 4px; cursor: pointer; font-weight: 500;">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="CertificateInlineCreate.saveQuickProduct()" style="padding: 10px 20px; background-color: #92C83E; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500;">Add Product</button>
                    </div>
                </div>
            </div>
        `;

        // Remove any existing modal
        const existingModal = document.getElementById('quickAddProductModal');
        if (existingModal) existingModal.remove();

        // Add modal to page
        document.body.insertAdjacentHTML('beforeend', modalHtml);

        // Set up cascading dropdowns within the modal
        CertificateInlineCreate.setupModalCascade();

        // Focus the name input
        setTimeout(() => {
            document.getElementById('quickProductName').focus();
        }, 100);

        // Add enter key handler
        document.getElementById('quickProductName').addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                CertificateInlineCreate.saveQuickProduct();
            }
        });
    };

    /**
     * Set up cascading category → type → subcategory dropdowns within the Add Product modal
     */
    CertificateInlineCreate.setupModalCascade = function() {
        const categorySelect = document.getElementById('quickProductCategory');
        const typeSelect = document.getElementById('quickProductType');
        const subcategorySelect = document.getElementById('quickProductSubcategory');

        if (!categorySelect || !typeSelect || !subcategorySelect) return;

        // Category → Type cascade
        categorySelect.addEventListener('change', async function() {
            const categoryId = this.value;
            typeSelect.innerHTML = '<option value="">Select type...</option>';
            subcategorySelect.innerHTML = '<option value="">Select type first...</option>';

            if (categoryId) {
                try {
                    const response = await fetch(`/Certificate/GetProductTypes?categoryId=${categoryId}`);
                    const result = await response.json();
                    if (result.success && result.data) {
                        result.data.forEach(pt => {
                            typeSelect.innerHTML += `<option value="${pt.id}">${pt.name}</option>`;
                        });
                    }
                } catch (error) {
                    console.error('Error loading product types in modal:', error);
                }
            }
        });

        // Type → Subcategory cascade
        typeSelect.addEventListener('change', async function() {
            const typeId = this.value;
            subcategorySelect.innerHTML = '<option value="">Select subcategory...</option>';

            if (typeId) {
                try {
                    const response = await fetch(`/Certificate/GetProductSubcategories?productTypeId=${typeId}`);
                    const result = await response.json();
                    if (result.success && result.data && result.data.length > 0) {
                        result.data.forEach(sub => {
                            subcategorySelect.innerHTML += `<option value="${sub.id}">${sub.name}</option>`;
                        });
                    }
                } catch (error) {
                    console.error('Error loading product subcategories in modal:', error);
                }
            }
        });
    };

    /**
     * Save Quick Add Product
     */
    CertificateInlineCreate.saveQuickProduct = async function() {
        const nameInput = document.getElementById('quickProductName');
        const companyInput = document.getElementById('quickProductCompany');
        const categoryInput = document.getElementById('quickProductCategory');
        const typeInput = document.getElementById('quickProductType');
        const subcategoryInput = document.getElementById('quickProductSubcategory');
        const descriptionInput = document.getElementById('quickProductDescription');
        const errorDiv = document.getElementById('quickProductError');

        const name = nameInput.value.trim();
        const companyId = companyInput.value;
        const categoryId = categoryInput ? categoryInput.value : '';
        const typeId = typeInput ? typeInput.value : '';
        const subcategoryId = subcategoryInput ? subcategoryInput.value : '';
        const description = descriptionInput.value.trim();

        // Validate
        if (!name) {
            errorDiv.textContent = 'Product name is required';
            errorDiv.style.display = 'block';
            nameInput.focus();
            return;
        }

        if (!companyId) {
            errorDiv.textContent = 'Please select a company';
            errorDiv.style.display = 'block';
            companyInput.focus();
            return;
        }

        if (!categoryId) {
            errorDiv.textContent = 'Please select a product category';
            errorDiv.style.display = 'block';
            categoryInput.focus();
            return;
        }

        errorDiv.style.display = 'none';

        try {
            const response = await fetch('/Product/SaveProduct', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({
                    Id: 0,
                    Name: name,
                    CompanyId: parseInt(companyId),
                    CategoryId: parseInt(categoryId),
                    ProductTypeId: typeId ? parseInt(typeId) : null,
                    ProductSubcategoryId: subcategoryId ? parseInt(subcategoryId) : null,
                    Description: description || null,
                    IsActive: true
                })
            });

            const result = await response.json();

            if (result.success && result.data) {
                // Add new product to the product dropdown and select it
                const productSelect = document.getElementById('ProductId');
                if (productSelect) {
                    const option = document.createElement('option');
                    option.value = result.data.id;
                    option.textContent = result.data.name;
                    productSelect.appendChild(option);
                    productSelect.value = result.data.id;
                    productSelect.dataset.previousValue = result.data.id;

                    if (productSelect.autocompleteInput) {
                        productSelect.autocompleteInput.value = result.data.name;
                    }
                }

                // Set company on the main form if different (without triggering change to avoid reloading products)
                const mainCompanySelect = document.getElementById('CompanyId');
                if (mainCompanySelect && mainCompanySelect.value !== companyId) {
                    mainCompanySelect.value = companyId;
                    mainCompanySelect.dataset.previousValue = companyId;
                }

                // Auto-fill category/type/subcategory on the main form from the modal selections
                const pageCategorySelect = document.getElementById('ProductCategoryId');
                const pageTypeSelect = document.getElementById('ProductTypeId');
                const pageSubcategorySelect = document.getElementById('ProductSubcategoryId');

                if (pageCategorySelect && categoryId) {
                    pageCategorySelect.value = categoryId;
                    // Load types for this category, then set type and subcategory
                    try {
                        const typeResponse = await fetch(`/Certificate/GetProductTypes?categoryId=${categoryId}`);
                        const typeResult = await typeResponse.json();
                        if (typeResult.success && typeResult.data && pageTypeSelect) {
                            pageTypeSelect.innerHTML = '<option value="">Select a type...</option>';
                            typeResult.data.forEach(pt => {
                                pageTypeSelect.innerHTML += `<option value="${pt.id}">${pt.name}</option>`;
                            });
                            if (typeId) {
                                pageTypeSelect.value = typeId;
                                // Load subcategories
                                const subResponse = await fetch(`/Certificate/GetProductSubcategories?productTypeId=${typeId}`);
                                const subResult = await subResponse.json();
                                if (subResult.success && subResult.data && subResult.data.length > 0 && pageSubcategorySelect) {
                                    pageSubcategorySelect.innerHTML = '<option value="">Select subcategory...</option>';
                                    subResult.data.forEach(sub => {
                                        pageSubcategorySelect.innerHTML += `<option value="${sub.id}">${sub.name}</option>`;
                                    });
                                    if (subcategoryId) {
                                        pageSubcategorySelect.value = subcategoryId;
                                    }
                                }
                            }
                        }
                    } catch (err) {
                        console.warn('Error auto-filling page dropdowns after product creation:', err);
                    }
                }

                // Also add product to companyProducts cache (if available on page)
                if (typeof companyProducts !== 'undefined') {
                    companyProducts.push({
                        id: result.data.id,
                        name: result.data.name,
                        categoryId: categoryId ? parseInt(categoryId) : null,
                        productTypeId: typeId ? parseInt(typeId) : null,
                        productSubcategoryId: subcategoryId ? parseInt(subcategoryId) : null
                    });
                }

                // Close modal
                CertificateInlineCreate.closeModal('quickAddProductModal');

                // Show success notification
                showNotification('Product "' + name + '" created successfully', 'success');
            } else {
                errorDiv.textContent = result.message || 'Failed to create product';
                errorDiv.style.display = 'block';
            }
        } catch (error) {
            console.error('Error creating product:', error);
            errorDiv.textContent = 'Error creating product: ' + error.message;
            errorDiv.style.display = 'block';
        }
    };

    /**
     * Close modal by ID
     */
    CertificateInlineCreate.closeModal = function(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.remove();
        }
    };

    /**
     * Track the previous value to restore if user cancels modal
     */
    let previousCompanyValue = '';
    let previousProductValue = '';

    /**
     * Create a custom dropdown with fixed "Add New" footer
     * @param {HTMLSelectElement} selectElement - The original select element to enhance
     * @param {string} type - 'company' or 'product'
     * @param {string} placeholder - Placeholder text for the add input
     */
    function createCustomDropdown(selectElement, type, placeholder) {
        if (!selectElement || selectElement.dataset.customDropdownInit) return;
        selectElement.dataset.customDropdownInit = 'true';

        const formGroup = selectElement.closest('.form-group');
        if (!formGroup) return;

        // Create wrapper
        const wrapper = document.createElement('div');
        wrapper.className = 'custom-dropdown-wrapper';
        wrapper.style.cssText = 'position: relative; width: 100%;';

        // Create trigger button (shows selected value)
        const trigger = document.createElement('div');
        trigger.className = 'custom-dropdown-trigger';
        trigger.style.cssText = `
            display: flex; justify-content: space-between; align-items: center;
            padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px;
            background: white; cursor: pointer; font-size: 14px; min-height: 40px;
            transition: border-color 0.2s;
        `;
        trigger.innerHTML = `
            <span class="custom-dropdown-selected" style="color: #6c757d;">Select ${type}...</span>
            <i class="fas fa-chevron-down" style="color: #6c757d; font-size: 12px; transition: transform 0.2s;"></i>
        `;

        // Create dropdown panel
        const panel = document.createElement('div');
        panel.className = 'custom-dropdown-panel';
        panel.style.cssText = `
            position: absolute; top: 100%; left: 0; right: 0; z-index: 1000;
            background: white; border: 1px solid #ced4da; border-top: none;
            border-radius: 0 0 4px 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            display: none; max-height: 350px; display: none;
        `;

        // Options container (scrollable)
        const optionsContainer = document.createElement('div');
        optionsContainer.className = 'custom-dropdown-options';
        optionsContainer.style.cssText = `
            max-height: 250px; overflow-y: auto; padding: 8px 0;
        `;

        // Build options from select
        function buildOptions() {
            optionsContainer.innerHTML = '';
            Array.from(selectElement.options).forEach((opt, index) => {
                if (index === 0 && !opt.value) return; // Skip placeholder

                const optionDiv = document.createElement('div');
                optionDiv.className = 'custom-dropdown-option';
                optionDiv.dataset.value = opt.value;
                optionDiv.style.cssText = `
                    display: flex; align-items: center; gap: 10px;
                    padding: 10px 16px; cursor: pointer; transition: background 0.15s;
                `;

                const isSelected = opt.value === selectElement.value;
                optionDiv.innerHTML = `
                    <span style="width: 18px; height: 18px; border: 2px solid ${isSelected ? '#92C83E' : '#ced4da'};
                        border-radius: 50%; display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
                        ${isSelected ? '<span style="width: 10px; height: 10px; background: #92C83E; border-radius: 50%;"></span>' : ''}
                    </span>
                    <span style="color: #333;">${opt.textContent}</span>
                `;

                optionDiv.onmouseenter = () => optionDiv.style.backgroundColor = '#f8f9fa';
                optionDiv.onmouseleave = () => optionDiv.style.backgroundColor = 'transparent';

                optionDiv.onclick = (e) => {
                    e.stopPropagation();
                    selectElement.value = opt.value;
                    selectElement.dispatchEvent(new Event('change', { bubbles: true }));
                    updateTrigger();
                    buildOptions(); // Rebuild to update selection indicator
                    closeDropdown();
                };

                optionsContainer.appendChild(optionDiv);
            });
        }

        // Fixed footer with add input
        const footer = document.createElement('div');
        footer.className = 'custom-dropdown-footer';
        footer.style.cssText = `
            padding: 12px 16px; border-top: 1px solid #e9ecef; background: #fafafa;
        `;
        footer.innerHTML = `
            <div style="display: flex; align-items: center; gap: 8px;">
                <span style="color: #92C83E; font-size: 18px; font-weight: bold;">+</span>
                <input type="text" class="custom-dropdown-add-input" placeholder="${placeholder}"
                    style="flex: 1; padding: 8px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px; outline: none;">
                <button type="button" class="custom-dropdown-add-btn"
                    style="padding: 8px 16px; background: #92C83E; color: white; border: none; border-radius: 4px; font-size: 14px; font-weight: 500; cursor: pointer;">
                    Add
                </button>
            </div>
            <div class="custom-dropdown-error" style="display: none; color: #dc3545; font-size: 12px; margin-top: 6px; padding-left: 26px;"></div>
        `;

        // Assemble panel
        panel.appendChild(optionsContainer);
        panel.appendChild(footer);

        // Insert into DOM
        // Use visibility hidden instead of display none to preserve validation
        selectElement.style.position = 'absolute';
        selectElement.style.opacity = '0';
        selectElement.style.height = '0';
        selectElement.style.width = '0';
        selectElement.style.overflow = 'hidden';
        selectElement.style.pointerEvents = 'none';
        selectElement.parentNode.insertBefore(wrapper, selectElement);
        wrapper.appendChild(trigger);
        wrapper.appendChild(panel);
        wrapper.appendChild(selectElement);

        // Auto-refresh when options are dynamically added/removed (e.g., company→product cascade)
        const observer = new MutationObserver(() => {
            buildOptions();
            updateTrigger();
        });
        observer.observe(selectElement, { childList: true });

        // Also refresh on programmatic value changes via 'change' event from jQuery .val().trigger('change')
        selectElement.addEventListener('change', () => {
            updateTrigger();
        });

        // Handle validation - show inline error on trigger if select is invalid
        selectElement.addEventListener('invalid', (e) => {
            e.preventDefault(); // Prevent default browser tooltip
            const formGroup = wrapper.closest('.form-group');
            if (formGroup) {
                formGroup.classList.add('has-error');
                trigger.style.borderColor = '#dc3545';
                // Add inline error if not exists
                if (!formGroup.querySelector('.inline-validation-error')) {
                    const label = formGroup.querySelector('label');
                    const labelText = label ? label.textContent.replace('*', '').replace('(Optional)', '').trim() : type;
                    const errorDiv = document.createElement('div');
                    errorDiv.className = 'inline-validation-error';
                    errorDiv.textContent = `${labelText} is required`;
                    wrapper.insertAdjacentElement('afterend', errorDiv);
                }
            }
        });

        // Clear error when value changes
        selectElement.addEventListener('change', () => {
            const formGroup = wrapper.closest('.form-group');
            if (formGroup && formGroup.classList.contains('has-error')) {
                formGroup.classList.remove('has-error');
                trigger.style.borderColor = '#ced4da';
                const errorDiv = formGroup.querySelector('.inline-validation-error');
                if (errorDiv) errorDiv.remove();
            }
        });

        // Update trigger text
        function updateTrigger() {
            const selectedOpt = selectElement.options[selectElement.selectedIndex];
            const selectedSpan = trigger.querySelector('.custom-dropdown-selected');
            if (selectedOpt && selectedOpt.value) {
                selectedSpan.textContent = selectedOpt.textContent;
                selectedSpan.style.color = '#333';
            } else {
                selectedSpan.textContent = `Select ${type}...`;
                selectedSpan.style.color = '#6c757d';
            }
        }

        // Toggle dropdown
        let isOpen = false;
        function openDropdown() {
            panel.style.display = 'block';
            trigger.style.borderColor = '#92C83E';
            trigger.style.borderRadius = '4px 4px 0 0';
            trigger.querySelector('i').style.transform = 'rotate(180deg)';
            isOpen = true;
            buildOptions();
        }

        function closeDropdown() {
            panel.style.display = 'none';
            trigger.style.borderColor = '#ced4da';
            trigger.style.borderRadius = '4px';
            trigger.querySelector('i').style.transform = 'rotate(0deg)';
            isOpen = false;
        }

        trigger.onclick = (e) => {
            e.stopPropagation();
            if (isOpen) closeDropdown();
            else openDropdown();
        };

        // Close on outside click
        document.addEventListener('click', (e) => {
            if (!wrapper.contains(e.target)) closeDropdown();
        });

        // Handle add button
        const addInput = footer.querySelector('.custom-dropdown-add-input');
        const addBtn = footer.querySelector('.custom-dropdown-add-btn');
        const errorDiv = footer.querySelector('.custom-dropdown-error');

        // Helper to show/hide inline error
        function showInlineError(message) {
            errorDiv.textContent = message;
            errorDiv.style.display = 'block';
            addInput.style.borderColor = '#dc3545';
        }

        function hideInlineError() {
            errorDiv.style.display = 'none';
            addInput.style.borderColor = '#ced4da';
        }

        addInput.onclick = (e) => e.stopPropagation();
        addInput.oninput = () => hideInlineError(); // Clear error on typing
        addInput.onkeypress = (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                addBtn.click();
            }
        };

        addBtn.onclick = async (e) => {
            e.stopPropagation();
            hideInlineError();

            const name = addInput.value.trim();
            if (!name) {
                showInlineError('Please enter a name');
                addInput.focus();
                return;
            }

            // Call appropriate save function
            if (type === 'company') {
                await saveInlineCompany(name, selectElement, addInput, showInlineError, hideInlineError);
            } else {
                await saveInlineProduct(name, selectElement, addInput, showInlineError, hideInlineError);
            }

            updateTrigger();
            buildOptions();
        };

        // Initial state
        updateTrigger();

        // Sync when select changes externally
        selectElement.addEventListener('change', () => {
            updateTrigger();
            if (isOpen) buildOptions();
        });

        return wrapper;
    }

    /**
     * Save company inline (from dropdown footer)
     */
    async function saveInlineCompany(name, selectElement, inputElement, showError, hideError) {
        try {
            const response = await fetch('/Company/SaveCompany', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({
                    Id: 0,
                    Name: name,
                    IsActive: true
                })
            });

            const result = await response.json();

            // Handle both camelCase and PascalCase response properties
            const success = result.success ?? result.Success;
            const data = result.data ?? result.Data;
            const message = result.message ?? result.Message;

            if (success && data) {
                // Add to original select
                const option = document.createElement('option');
                option.value = data.id ?? data.Id;
                option.textContent = data.name ?? data.Name;
                selectElement.appendChild(option);
                selectElement.value = data.id ?? data.Id;
                selectElement.dispatchEvent(new Event('change', { bubbles: true }));

                inputElement.value = '';
                hideError();
                showNotification('Company "' + name + '" created successfully', 'success');
            } else {
                showError(message || 'Failed to create company');
            }
        } catch (error) {
            console.error('Error creating company:', error);
            showError('Error creating company');
        }
    }

    /**
     * Save product inline (from dropdown footer)
     */
    async function saveInlineProduct(name, selectElement, inputElement, showError, hideError) {
        // Get selected company from the hidden original select (inside custom dropdown wrapper)
        // Company is optional - product can be created without a company association
        const companyWrapper = document.querySelector('#CompanyId')?.closest('.custom-dropdown-wrapper');
        const companySelect = companyWrapper ? companyWrapper.querySelector('select') : document.getElementById('CompanyId');
        const companyId = companySelect ? companySelect.value : '';

        // Get a valid category ID (required field)
        const categoryId = await getDefaultCategoryId();
        if (!categoryId) {
            showError('Unable to create product - no categories available');
            return;
        }

        try {
            const response = await fetch('/Product/SaveProduct', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({
                    Id: 0,
                    Name: name,
                    CompanyId: companyId ? parseInt(companyId) : null,
                    CategoryId: categoryId,
                    IsActive: true
                })
            });

            const result = await response.json();

            // Handle both camelCase and PascalCase response properties
            const success = result.success ?? result.Success;
            const data = result.data ?? result.Data;
            const message = result.message ?? result.Message;

            if (success && data) {
                // Add to original select
                const option = document.createElement('option');
                option.value = data.id ?? data.Id;
                option.textContent = data.name ?? data.Name;
                selectElement.appendChild(option);
                selectElement.value = data.id ?? data.Id;
                selectElement.dispatchEvent(new Event('change', { bubbles: true }));

                inputElement.value = '';
                hideError();
                showNotification('Product "' + name + '" created successfully', 'success');
            } else {
                showError(message || 'Failed to create product');
            }
        } catch (error) {
            console.error('Error creating product:', error);
            showError('Error creating product');
        }
    }

    /**
     * Create Product Autocomplete with duplicate detection
     * Replaces the standard dropdown with a searchable autocomplete input
     */
    function createProductAutocomplete(selectElement) {
        if (!selectElement || selectElement.dataset.autocompleteInit) return;
        selectElement.dataset.autocompleteInit = 'true';

        const formGroup = selectElement.closest('.form-group');
        if (!formGroup) return;

        // Create wrapper
        const wrapper = document.createElement('div');
        wrapper.className = 'product-autocomplete-wrapper';
        wrapper.style.cssText = 'position: relative; width: 100%;';

        // Create text input for autocomplete
        const input = document.createElement('input');
        input.type = 'text';
        input.className = 'form-control product-autocomplete-input';
        input.placeholder = 'Type to search products...';
        input.autocomplete = 'off';
        input.style.cssText = 'width: 100%; padding: 10px 12px; border: 1px solid #ced4da; border-radius: 4px; font-size: 14px;';

        // Create dropdown for suggestions
        const dropdown = document.createElement('div');
        dropdown.className = 'product-autocomplete-dropdown';
        dropdown.style.cssText = `
            position: absolute; top: 100%; left: 0; right: 0; z-index: 1000;
            background: white; border: 1px solid #ced4da; border-top: none;
            border-radius: 0 0 4px 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            display: none; max-height: 300px; overflow-y: auto;
        `;

        // Hide original select but keep it for form submission
        selectElement.style.position = 'absolute';
        selectElement.style.opacity = '0';
        selectElement.style.height = '0';
        selectElement.style.width = '0';
        selectElement.style.overflow = 'hidden';
        selectElement.style.pointerEvents = 'none';

        // Insert wrapper
        selectElement.parentNode.insertBefore(wrapper, selectElement);
        wrapper.appendChild(input);
        wrapper.appendChild(dropdown);
        wrapper.appendChild(selectElement);

        // State
        let debounceTimer = null;
        let isOpen = false;
        let selectedProductId = selectElement.value || '';
        let selectedProductName = '';

        // If select already has a value (edit mode), populate input
        if (selectElement.value) {
            const selectedOption = selectElement.options[selectElement.selectedIndex];
            if (selectedOption && selectedOption.value) {
                selectedProductName = selectedOption.textContent;
                input.value = selectedProductName;
            }
        }

        // Search products API
        async function searchProducts(query) {
            try {
                const response = await fetch(`/Certificate/SearchProducts?query=${encodeURIComponent(query)}`);
                const result = await response.json();
                if (result.success) {
                    return result.data || [];
                }
            } catch (error) {
                console.error('Error searching products:', error);
            }
            return [];
        }

        // Find similar products for duplicate checking
        async function findSimilarProducts(name) {
            try {
                const response = await fetch(`/Certificate/FindSimilarProducts?name=${encodeURIComponent(name)}`);
                const result = await response.json();
                if (result.success) {
                    return {
                        exactMatch: result.exactMatch,
                        products: result.data || []
                    };
                }
            } catch (error) {
                console.error('Error finding similar products:', error);
            }
            return { exactMatch: false, products: [] };
        }

        // Render dropdown options
        function renderDropdown(products, query) {
            dropdown.innerHTML = '';

            if (products.length === 0 && query) {
                // No results - show "Add new" option
                const addNewDiv = document.createElement('div');
                addNewDiv.className = 'product-autocomplete-add-new';
                addNewDiv.style.cssText = `
                    padding: 12px 16px; cursor: pointer; display: flex; align-items: center; gap: 8px;
                    background: #f8f9fa; border-top: 1px solid #e9ecef;
                `;
                addNewDiv.innerHTML = `
                    <span style="color: #92C83E; font-size: 18px; font-weight: bold;">+</span>
                    <span style="color: #333;">Add "<strong>${escapeHtml(query)}</strong>" as new product</span>
                `;
                addNewDiv.onclick = () => handleAddNew(query);
                dropdown.appendChild(addNewDiv);
            } else {
                // Show matching products
                products.forEach(product => {
                    const optionDiv = document.createElement('div');
                    optionDiv.className = 'product-autocomplete-option';
                    optionDiv.dataset.id = product.id;
                    optionDiv.dataset.name = product.name;
                    optionDiv.style.cssText = `
                        padding: 10px 16px; cursor: pointer; transition: background 0.15s;
                    `;
                    optionDiv.innerHTML = highlightMatch(product.name, query);

                    optionDiv.onmouseenter = () => optionDiv.style.backgroundColor = '#f8f9fa';
                    optionDiv.onmouseleave = () => optionDiv.style.backgroundColor = 'transparent';

                    optionDiv.onclick = () => selectProduct(product.id, product.name);
                    dropdown.appendChild(optionDiv);
                });

                // Always show "Add new" at bottom if query doesn't exactly match any result
                if (query && !products.some(p => p.name.toLowerCase() === query.toLowerCase())) {
                    const addNewDiv = document.createElement('div');
                    addNewDiv.className = 'product-autocomplete-add-new';
                    addNewDiv.style.cssText = `
                        padding: 12px 16px; cursor: pointer; display: flex; align-items: center; gap: 8px;
                        background: #f8f9fa; border-top: 1px solid #e9ecef;
                    `;
                    addNewDiv.innerHTML = `
                        <span style="color: #92C83E; font-size: 18px; font-weight: bold;">+</span>
                        <span style="color: #333;">Add "<strong>${escapeHtml(query)}</strong>" as new product</span>
                    `;
                    addNewDiv.onclick = () => handleAddNew(query);
                    dropdown.appendChild(addNewDiv);
                }
            }

            openDropdown();
        }

        // Escape HTML to prevent XSS
        function escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }

        // Highlight matching text
        function highlightMatch(text, query) {
            if (!query) return escapeHtml(text);
            const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
            return escapeHtml(text).replace(regex, '<strong style="color: #92C83E;">$1</strong>');
        }

        // Select a product
        function selectProduct(id, name) {
            selectedProductId = id;
            selectedProductName = name;
            input.value = name;
            selectElement.value = id;
            selectElement.dispatchEvent(new Event('change', { bubbles: true }));
            closeDropdown();
            clearValidationError();
        }

        // Handle adding new product with duplicate check
        async function handleAddNew(name) {
            closeDropdown();

            // Check for similar products first
            const similar = await findSimilarProducts(name);

            if (similar.exactMatch && similar.products.length > 0) {
                // Exact match found - just select it
                const product = similar.products[0];
                selectProduct(product.id, product.name);
                showNotification(`Selected existing product "${product.name}"`, 'info');
                return;
            }

            if (similar.products.length > 0) {
                // Similar products found - show "Did you mean?" modal
                showDidYouMeanModal(name, similar.products);
            } else {
                // No similar products - proceed to create
                showQuickAddProductModal(name);
            }
        }

        // Show "Did you mean?" modal for similar products
        function showDidYouMeanModal(newName, similarProducts) {
            const optionsHtml = similarProducts.map(p => `
                <div class="similar-product-option" data-id="${p.id}" data-name="${escapeHtml(p.name)}"
                    style="padding: 12px 16px; border: 1px solid #e9ecef; border-radius: 4px; margin-bottom: 8px; cursor: pointer; transition: all 0.2s;">
                    <div style="font-weight: 500; color: #333;">${escapeHtml(p.name)}</div>
                </div>
            `).join('');

            const modalHtml = `
                <div id="didYouMeanModal" class="admin-modal" style="display: flex; position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 10000; align-items: center; justify-content: center;">
                    <div class="admin-modal-backdrop" style="position: absolute; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5);" onclick="CertificateInlineCreate.closeModal('didYouMeanModal')"></div>
                    <div class="admin-modal-content" style="position: relative; background: white; border-radius: 8px; box-shadow: 0 20px 60px rgba(0,0,0,0.3); width: 100%; max-width: 480px; margin: 20px;">
                        <div class="admin-modal-header" style="padding: 20px; border-bottom: 2px solid #92C83E;">
                            <h4 style="margin: 0; font-size: 18px; color: #333;">
                                <i class="fas fa-question-circle" style="color: #FFC107; margin-right: 8px;"></i>
                                Did you mean...?
                            </h4>
                        </div>
                        <div class="admin-modal-body" style="padding: 20px;">
                            <p style="color: #666; margin-bottom: 16px;">
                                We found similar products. Did you mean one of these?
                            </p>
                            <div id="similarProductOptions" style="margin-bottom: 16px;">
                                ${optionsHtml}
                            </div>
                            <div style="padding: 12px 16px; background: #f8f9fa; border-radius: 4px; border: 2px dashed #92C83E;">
                                <div style="font-weight: 500; color: #333; margin-bottom: 4px;">
                                    <i class="fas fa-plus" style="color: #92C83E; margin-right: 8px;"></i>
                                    Create new: "${escapeHtml(newName)}"
                                </div>
                                <small style="color: #666;">This will add a new product to the database</small>
                            </div>
                        </div>
                        <div class="admin-modal-footer" style="background-color: #f8f9fa; border-top: 2px solid #e9ecef; padding: 16px 20px; display: flex; justify-content: flex-end; gap: 12px;">
                            <button type="button" class="btn btn-outline" onclick="CertificateInlineCreate.closeModal('didYouMeanModal')"
                                style="padding: 10px 20px; border: 1px solid #6c757d; color: #6c757d; background: white; border-radius: 4px; cursor: pointer; font-weight: 500;">
                                Cancel
                            </button>
                            <button type="button" class="btn btn-primary" id="createNewProductBtn"
                                style="padding: 10px 20px; background-color: #92C83E; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500;">
                                Create New Product
                            </button>
                        </div>
                    </div>
                </div>
            `;

            // Remove existing modal if any
            const existingModal = document.getElementById('didYouMeanModal');
            if (existingModal) existingModal.remove();

            document.body.insertAdjacentHTML('beforeend', modalHtml);

            // Add click handlers for similar product options
            document.querySelectorAll('#similarProductOptions .similar-product-option').forEach(opt => {
                opt.onmouseenter = () => {
                    opt.style.borderColor = '#92C83E';
                    opt.style.backgroundColor = '#f8fff0';
                };
                opt.onmouseleave = () => {
                    opt.style.borderColor = '#e9ecef';
                    opt.style.backgroundColor = 'transparent';
                };
                opt.onclick = () => {
                    const id = parseInt(opt.dataset.id);
                    const name = opt.dataset.name;
                    selectProduct(id, name);
                    CertificateInlineCreate.closeModal('didYouMeanModal');
                    showNotification(`Selected existing product "${name}"`, 'success');
                };
            });

            // Create new product button
            document.getElementById('createNewProductBtn').onclick = () => {
                CertificateInlineCreate.closeModal('didYouMeanModal');
                showQuickAddProductModal(newName);
            };
        }

        // Show quick add product modal (reuse existing with prefilled name)
        function showQuickAddProductModal(prefillName) {
            CertificateInlineCreate.showQuickAddProduct();
            // Wait for modal to render, then prefill the name
            setTimeout(() => {
                const nameInput = document.getElementById('quickProductName');
                if (nameInput) {
                    nameInput.value = prefillName;
                }
            }, 100);
        }

        // Open/close dropdown
        function openDropdown() {
            dropdown.style.display = 'block';
            input.style.borderRadius = '4px 4px 0 0';
            input.style.borderColor = '#92C83E';
            isOpen = true;
        }

        function closeDropdown() {
            dropdown.style.display = 'none';
            input.style.borderRadius = '4px';
            input.style.borderColor = '#ced4da';
            isOpen = false;
        }

        // Clear validation error
        function clearValidationError() {
            const errorDiv = formGroup.querySelector('.inline-validation-error');
            if (errorDiv) errorDiv.remove();
            formGroup.classList.remove('has-error');
            input.style.borderColor = '#ced4da';
        }

        // Input event handlers
        input.addEventListener('input', () => {
            clearValidationError();
            const query = input.value.trim();

            // Clear selection if user is typing something different
            if (selectedProductName && query !== selectedProductName) {
                selectedProductId = '';
                selectElement.value = '';
            }

            // Debounce search
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(async () => {
                if (query.length >= 1) {
                    const products = await searchProducts(query);
                    renderDropdown(products, query);
                } else {
                    closeDropdown();
                }
            }, 250);
        });

        input.addEventListener('focus', async () => {
            const query = input.value.trim();
            if (query.length >= 1) {
                const products = await searchProducts(query);
                renderDropdown(products, query);
            } else {
                // Show default products on focus
                const products = await searchProducts('');
                if (products.length > 0) {
                    renderDropdown(products, '');
                }
            }
        });

        // Close on outside click
        document.addEventListener('click', (e) => {
            if (!wrapper.contains(e.target)) {
                closeDropdown();
            }
        });

        // Handle validation
        selectElement.addEventListener('invalid', (e) => {
            e.preventDefault();
            formGroup.classList.add('has-error');
            input.style.borderColor = '#dc3545';
            if (!formGroup.querySelector('.inline-validation-error')) {
                const errorDiv = document.createElement('div');
                errorDiv.className = 'inline-validation-error';
                errorDiv.textContent = 'Product Name is required';
                wrapper.insertAdjacentElement('afterend', errorDiv);
            }
        });

        // Store reference for external access
        selectElement.autocompleteWrapper = wrapper;
        selectElement.autocompleteInput = input;

        return wrapper;
    }

    /**
     * Create a multi-select dropdown with checkboxes
     * @param {HTMLSelectElement} selectElement - The original select element
     * @param {HTMLElement} hiddenInput - The hidden input to store selected IDs as JSON
     * @param {HTMLElement} pillsContainer - Container to display selected items as pills
     * @param {Function} onChangeCallback - Optional callback when selection changes
     */
    function createMultiSelectDropdown(selectElement, hiddenInput, pillsContainer, onChangeCallback) {
        if (!selectElement || selectElement.dataset.multiSelectInit) return;
        selectElement.dataset.multiSelectInit = 'true';

        const formGroup = selectElement.closest('.form-group');
        if (!formGroup) return;

        // Track selected items
        let selectedItems = [];

        // Create wrapper
        const wrapper = document.createElement('div');
        wrapper.className = 'custom-dropdown-wrapper multi-select-dropdown';
        wrapper.style.cssText = 'position: relative; width: 100%;';

        // Create trigger button
        const trigger = document.createElement('div');
        trigger.className = 'custom-dropdown-trigger';
        trigger.style.cssText = `
            display: flex; justify-content: space-between; align-items: center;
            padding: 8px 12px; border: 1px solid #ced4da; border-radius: 4px;
            background: white; cursor: pointer; font-size: 14px; min-height: 38px;
            transition: border-color 0.2s;
        `;
        trigger.innerHTML = `
            <span class="custom-dropdown-selected" style="color: #6c757d;">Select subcategories...</span>
            <i class="fas fa-chevron-down" style="color: #6c757d; font-size: 12px; transition: transform 0.2s;"></i>
        `;

        // Create dropdown panel
        const panel = document.createElement('div');
        panel.className = 'custom-dropdown-panel';
        panel.style.cssText = `
            position: absolute; top: 100%; left: 0; right: 0; z-index: 1000;
            background: white; border: 1px solid #ced4da; border-top: none;
            border-radius: 0 0 4px 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            display: none; max-height: 300px; overflow-y: auto;
        `;

        // Options container
        const optionsContainer = document.createElement('div');
        optionsContainer.className = 'custom-dropdown-options';
        optionsContainer.style.cssText = 'padding: 8px 0;';

        panel.appendChild(optionsContainer);

        // Insert into DOM - keep original select for options source
        selectElement.style.display = 'none';
        selectElement.parentNode.insertBefore(wrapper, selectElement);
        wrapper.appendChild(trigger);
        wrapper.appendChild(panel);
        wrapper.appendChild(selectElement);

        // Build options from select element
        function buildOptions() {
            optionsContainer.innerHTML = '';
            const options = Array.from(selectElement.options);

            if (options.length <= 1) {
                // Only placeholder or empty
                optionsContainer.innerHTML = '<div style="padding: 12px 16px; color: #6c757d; font-style: italic;">No subcategories available</div>';
                return;
            }

            options.forEach((opt, index) => {
                if (index === 0 && !opt.value) return; // Skip placeholder

                const optionDiv = document.createElement('div');
                optionDiv.className = 'custom-dropdown-option';
                optionDiv.dataset.value = opt.value;
                optionDiv.style.cssText = `
                    display: flex; align-items: center; gap: 10px;
                    padding: 10px 16px; cursor: pointer; transition: background 0.15s;
                `;

                const isSelected = selectedItems.some(item => item.id === parseInt(opt.value));
                optionDiv.innerHTML = `
                    <span style="width: 18px; height: 18px; border: 2px solid ${isSelected ? '#92C83E' : '#ced4da'};
                        border-radius: 3px; display: flex; align-items: center; justify-content: center; flex-shrink: 0;
                        background: ${isSelected ? '#92C83E' : 'white'};">
                        ${isSelected ? '<i class="fas fa-check" style="color: white; font-size: 10px;"></i>' : ''}
                    </span>
                    <span style="color: #333;">${opt.textContent}</span>
                `;

                optionDiv.onmouseenter = () => optionDiv.style.backgroundColor = '#f8f9fa';
                optionDiv.onmouseleave = () => optionDiv.style.backgroundColor = 'transparent';

                optionDiv.onclick = (e) => {
                    e.stopPropagation();
                    toggleSelection(parseInt(opt.value), opt.textContent);
                };

                optionsContainer.appendChild(optionDiv);
            });
        }

        // Toggle selection
        function toggleSelection(id, name) {
            const existingIndex = selectedItems.findIndex(item => item.id === id);

            if (existingIndex > -1) {
                // Remove
                selectedItems.splice(existingIndex, 1);
            } else {
                // Add
                selectedItems.push({ id, name });
            }

            updateDisplay();
            updateHiddenInput();
            buildOptions(); // Rebuild to update checkboxes

            if (onChangeCallback) onChangeCallback(selectedItems);
        }

        // Update trigger text
        function updateTrigger() {
            const selectedSpan = trigger.querySelector('.custom-dropdown-selected');
            if (selectedItems.length > 0) {
                selectedSpan.textContent = `${selectedItems.length} selected`;
                selectedSpan.style.color = '#333';
            } else {
                selectedSpan.textContent = 'Select subcategories...';
                selectedSpan.style.color = '#6c757d';
            }
        }

        // Update pills display
        function updateDisplay() {
            updateTrigger();

            if (!pillsContainer) return;

            pillsContainer.innerHTML = selectedItems.map(item => `
                <span class="tag subcategory" data-subcategory-id="${item.id}" style="
                    display: inline-flex; align-items: center; gap: 4px;
                    background: linear-gradient(135deg, #f0f7e6 0%, #e8f5d9 100%);
                    border: 1px solid #92C83E; border-radius: 12px;
                    padding: 2px 8px; font-size: 12px; color: #447638; margin: 3px 3px 3px 0;
                ">
                    ${item.name}
                    <button type="button" class="tag-remove" onclick="window.CertificateInlineCreate.removeSubcategory(${item.id})" style="
                        background: none; border: none; color: #92C83E; cursor: pointer;
                        font-size: 13px; line-height: 1; padding: 0 2px; font-weight: bold;
                    ">&times;</button>
                </span>
            `).join('');
        }

        // Update hidden input
        function updateHiddenInput() {
            if (hiddenInput) {
                hiddenInput.value = JSON.stringify(selectedItems.map(item => item.id));
            }
        }

        // Toggle dropdown
        let isOpen = false;
        function openDropdown() {
            panel.style.display = 'block';
            trigger.style.borderColor = '#92C83E';
            trigger.style.borderRadius = '4px 4px 0 0';
            trigger.querySelector('i').style.transform = 'rotate(180deg)';
            isOpen = true;
            buildOptions();
        }

        function closeDropdown() {
            panel.style.display = 'none';
            trigger.style.borderColor = '#ced4da';
            trigger.style.borderRadius = '4px';
            trigger.querySelector('i').style.transform = 'rotate(0deg)';
            isOpen = false;
        }

        trigger.onclick = (e) => {
            e.stopPropagation();
            if (isOpen) closeDropdown();
            else openDropdown();
        };

        // Close on outside click
        document.addEventListener('click', (e) => {
            if (!wrapper.contains(e.target)) closeDropdown();
        });

        // Public methods for external control
        wrapper.multiSelect = {
            clear: function() {
                selectedItems = [];
                updateDisplay();
                updateHiddenInput();
                if (isOpen) buildOptions();
            },
            loadExisting: function(items) {
                if (!items || !Array.isArray(items)) return;
                selectedItems = items.map(item => ({
                    id: item.Id || item.id,
                    name: item.Name || item.name
                }));
                updateDisplay();
                updateHiddenInput();
            },
            refresh: function() {
                buildOptions();
            },
            getSelected: function() {
                return selectedItems;
            }
        };

        // Store reference for external access
        selectElement.multiSelectWrapper = wrapper;

        // Initial state
        updateDisplay();

        return wrapper;
    }

    // Remove subcategory (called from pill X button)
    CertificateInlineCreate.removeSubcategory = function(id) {
        const wrapper = document.getElementById('subcategorySelect')?.multiSelectWrapper;
        if (wrapper && wrapper.multiSelect) {
            const selected = wrapper.multiSelect.getSelected();
            const newSelected = selected.filter(item => item.id !== parseInt(id));
            wrapper.multiSelect.clear();
            newSelected.forEach(item => {
                // Re-add each item
                const select = document.getElementById('subcategorySelect');
                const opt = Array.from(select.options).find(o => parseInt(o.value) === item.id);
                if (opt) {
                    // Trigger click on the option
                    const optionDiv = wrapper.querySelector(`[data-value="${item.id}"]`);
                    if (optionDiv) optionDiv.click();
                }
            });
            // Simpler approach: just reload
            wrapper.multiSelect.loadExisting(newSelected);
        }
    };

    /**
     * Initialize inline creation - converts dropdowns to custom dropdowns with fixed "Add" footer
     * Called on document ready
     */
    CertificateInlineCreate.init = function() {
        const companySelect = document.getElementById('CompanyId');
        const productSelect = document.getElementById('ProductId');

        // Convert Company dropdown to custom dropdown with Add footer
        if (companySelect) {
            createCustomDropdown(companySelect, 'company', 'Enter company name');
        }

        // Convert Product dropdown to custom dropdown (same pattern as Company)
        if (productSelect) {
            createCustomDropdown(productSelect, 'product', 'Enter product name');
        }

        // Convert Subcategory to multi-select dropdown with checkboxes
        const subcategorySelect = document.getElementById('subcategorySelect');
        const subcategoriesHidden = document.getElementById('subcategoriesHidden');
        const subcategoriesContainer = document.getElementById('subcategoriesContainer');

        if (subcategorySelect && subcategoriesHidden && subcategoriesContainer) {
            createMultiSelectDropdown(subcategorySelect, subcategoriesHidden, subcategoriesContainer);

            // Store reference for SubcategoryManager compatibility
            window.CertificateInlineCreate.subcategoryMultiSelect = subcategorySelect.multiSelectWrapper?.multiSelect;
        }
    };

    // Expose method to clear subcategories (for parent type change)
    CertificateInlineCreate.clearSubcategories = function() {
        const wrapper = document.getElementById('subcategorySelect')?.multiSelectWrapper;
        if (wrapper && wrapper.multiSelect) {
            wrapper.multiSelect.clear();
        }
    };

    // Expose method to refresh subcategory options
    CertificateInlineCreate.refreshSubcategories = function() {
        const wrapper = document.getElementById('subcategorySelect')?.multiSelectWrapper;
        if (wrapper && wrapper.multiSelect) {
            wrapper.multiSelect.refresh();
        }
    };

    // Expose method to load existing subcategories (for edit page)
    CertificateInlineCreate.loadExistingSubcategories = function(items) {
        const wrapper = document.getElementById('subcategorySelect')?.multiSelectWrapper;
        if (wrapper && wrapper.multiSelect) {
            wrapper.multiSelect.loadExisting(items);
        }
    };

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', CertificateInlineCreate.init);
    } else {
        // DOM already loaded, run immediately
        setTimeout(CertificateInlineCreate.init, 0);
    }

})();
