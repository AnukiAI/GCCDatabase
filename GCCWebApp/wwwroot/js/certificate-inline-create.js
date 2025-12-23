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

        // Fallback: Create inline toast
        const colors = {
            success: '#92C83E',
            error: '#FF5252',
            warning: '#FFC107',
            info: '#002B76'
        };

        const toast = document.createElement('div');
        toast.style.cssText = `
            position: fixed; top: 20px; right: 20px; z-index: 10001;
            padding: 14px 20px; border-radius: 6px; color: white;
            background: ${colors[type] || colors.info};
            box-shadow: 0 4px 12px rgba(0,0,0,0.2);
            font-size: 14px; font-weight: 500; max-width: 350px;
            animation: slideIn 0.3s ease;
        `;
        toast.textContent = message;

        // Add animation keyframes if not exists
        if (!document.getElementById('toast-animation-styles')) {
            const style = document.createElement('style');
            style.id = 'toast-animation-styles';
            style.textContent = `
                @keyframes slideIn { from { transform: translateX(100%); opacity: 0; } to { transform: translateX(0); opacity: 1; } }
                @keyframes slideOut { from { transform: translateX(0); opacity: 1; } to { transform: translateX(100%); opacity: 0; } }
            `;
            document.head.appendChild(style);
        }

        document.body.appendChild(toast);

        // Auto-remove after 4 seconds
        setTimeout(() => {
            toast.style.animation = 'slideOut 0.3s ease';
            setTimeout(() => toast.remove(), 300);
        }, 4000);
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
                // Add new product to dropdown at the end and select it
                const productSelect = document.getElementById('ProductId');
                if (productSelect) {
                    const option = document.createElement('option');
                    option.value = result.data.id;
                    option.textContent = result.data.name;

                    // Append to end of dropdown
                    productSelect.appendChild(option);

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

        // Convert Product dropdown to custom dropdown with Add footer
        if (productSelect) {
            createCustomDropdown(productSelect, 'product', 'Enter product name');
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
