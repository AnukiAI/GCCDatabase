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

    // Get CSRF token for AJAX requests
    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
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
                // Add new company to dropdown before the separator and select it
                const companySelect = document.getElementById('CompanyId');
                if (companySelect) {
                    const option = document.createElement('option');
                    option.value = result.data.id;
                    option.textContent = result.data.name;

                    // Find the separator (disabled option with dashes) and insert before it
                    const separatorOption = Array.from(companySelect.options).find(opt => opt.disabled && opt.textContent.includes('───'));
                    if (separatorOption) {
                        companySelect.insertBefore(option, separatorOption);
                    } else {
                        companySelect.appendChild(option);
                    }

                    // Select the new option and update stored previous value
                    companySelect.value = result.data.id;
                    companySelect.dataset.previousValue = result.data.id;

                    // Trigger change event
                    companySelect.dispatchEvent(new Event('change'));
                }

                // Close modal
                CertificateInlineCreate.closeModal('quickAddCompanyModal');

                // Show success notification
                if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
                    AdminCore.showNotification('Company "' + name + '" created successfully', 'success');
                }
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

        const modalHtml = `
            <div id="quickAddProductModal" class="admin-modal" style="display: flex; position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 10000; align-items: center; justify-content: center;">
                <div style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0, 0, 0, 0.85); z-index: 9998; backdrop-filter: blur(3px);"></div>
                <div class="admin-modal-container" style="position: relative; z-index: 9999; max-width: 500px; width: 90%; background-color: white; border-radius: 8px; box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);">
                    <div class="admin-modal-header" style="background-color: #f8f9fa; border-bottom: 2px solid #e9ecef; padding: 16px 20px; display: flex; justify-content: space-between; align-items: center;">
                        <h3 class="admin-modal-title" style="color: #002B76; font-weight: 600; margin: 0; font-size: 18px;">Quick Add Product</h3>
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
     * Save Quick Add Product
     */
    CertificateInlineCreate.saveQuickProduct = async function() {
        const nameInput = document.getElementById('quickProductName');
        const companyInput = document.getElementById('quickProductCompany');
        const descriptionInput = document.getElementById('quickProductDescription');
        const errorDiv = document.getElementById('quickProductError');

        const name = nameInput.value.trim();
        const companyId = companyInput.value;
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
                    Description: description || null,
                    IsActive: true
                })
            });

            const result = await response.json();

            if (result.success && result.data) {
                // Add new product to dropdown before the separator and select it
                const productSelect = document.getElementById('ProductId');
                if (productSelect) {
                    const option = document.createElement('option');
                    option.value = result.data.id;
                    option.textContent = result.data.name;

                    // Find the separator (disabled option with dashes) and insert before it
                    const separatorOption = Array.from(productSelect.options).find(opt => opt.disabled && opt.textContent.includes('───'));
                    if (separatorOption) {
                        productSelect.insertBefore(option, separatorOption);
                    } else {
                        productSelect.appendChild(option);
                    }

                    // Select the new option and update stored previous value
                    productSelect.value = result.data.id;
                    productSelect.dataset.previousValue = result.data.id;

                    // Trigger change event
                    productSelect.dispatchEvent(new Event('change'));
                }

                // Also update the company dropdown if different
                const companySelect = document.getElementById('CompanyId');
                if (companySelect && companySelect.value !== companyId) {
                    companySelect.value = companyId;
                    companySelect.dataset.previousValue = companyId;
                    companySelect.dispatchEvent(new Event('change'));
                }

                // Close modal
                CertificateInlineCreate.closeModal('quickAddProductModal');

                // Show success notification
                if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
                    AdminCore.showNotification('Product "' + name + '" created successfully', 'success');
                }
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
     * Initialize inline creation - adds "Add New..." option at bottom of dropdowns
     * Called on document ready
     */
    CertificateInlineCreate.init = function() {
        const companySelect = document.getElementById('CompanyId');
        const productSelect = document.getElementById('ProductId');

        // Add "Add New Company..." option at the bottom of Company dropdown
        if (companySelect && !companySelect.querySelector(`option[value="${ADD_NEW_COMPANY_VALUE}"]`)) {
            const separator = document.createElement('option');
            separator.disabled = true;
            separator.textContent = '──────────────────';
            companySelect.appendChild(separator);

            const addNewOption = document.createElement('option');
            addNewOption.value = ADD_NEW_COMPANY_VALUE;
            addNewOption.textContent = '+ Add New Company...';
            addNewOption.style.cssText = 'color: #92C83E; font-weight: 600;';
            companySelect.appendChild(addNewOption);

            // Handle selection of "Add New Company..."
            companySelect.addEventListener('change', function(e) {
                if (this.value === ADD_NEW_COMPANY_VALUE) {
                    // Store previous value to restore if cancelled
                    previousCompanyValue = this.dataset.previousValue || '';
                    // Reset to previous value immediately (before modal)
                    this.value = previousCompanyValue;
                    // Show the modal
                    CertificateInlineCreate.showQuickAddCompany();
                } else {
                    // Store current value as previous
                    this.dataset.previousValue = this.value;
                }
            });

            // Initialize previous value
            companySelect.dataset.previousValue = companySelect.value;
        }

        // Add "Add New Product..." option at the bottom of Product dropdown
        if (productSelect && !productSelect.querySelector(`option[value="${ADD_NEW_PRODUCT_VALUE}"]`)) {
            const separator = document.createElement('option');
            separator.disabled = true;
            separator.textContent = '──────────────────';
            productSelect.appendChild(separator);

            const addNewOption = document.createElement('option');
            addNewOption.value = ADD_NEW_PRODUCT_VALUE;
            addNewOption.textContent = '+ Add New Product...';
            addNewOption.style.cssText = 'color: #92C83E; font-weight: 600;';
            productSelect.appendChild(addNewOption);

            // Handle selection of "Add New Product..."
            productSelect.addEventListener('change', function(e) {
                if (this.value === ADD_NEW_PRODUCT_VALUE) {
                    // Store previous value to restore if cancelled
                    previousProductValue = this.dataset.previousValue || '';
                    // Reset to previous value immediately (before modal)
                    this.value = previousProductValue;
                    // Show the modal
                    CertificateInlineCreate.showQuickAddProduct();
                } else {
                    // Store current value as previous
                    this.dataset.previousValue = this.value;
                }
            });

            // Initialize previous value
            productSelect.dataset.previousValue = productSelect.value;
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
