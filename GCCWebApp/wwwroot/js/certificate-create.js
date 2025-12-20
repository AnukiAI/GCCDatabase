/**
 * Certificate Create/Edit Form JavaScript
 * Handles image preview, PDF upload, tags, and form validation
 * Uses custom modals instead of native alerts per system requirements
 */

(function() {
    'use strict';

    // Modal Notification System
    const ModalSystem = {
        /**
         * Show custom modal confirmation dialog
         * @param {Object} options - Modal configuration
         * @returns {Promise<boolean>} - User's choice
         */
        showConfirmation: function(options) {
            return new Promise((resolve) => {
                const modalHtml = `
                    <div class="modal-overlay" id="customConfirmModal">
                        <div class="modal-dialog">
                            <h2 class="modal-title">${options.title || 'Confirm'}</h2>
                            <p class="modal-message">${options.message}</p>
                            <div class="modal-actions">
                                <button class="modal-btn modal-btn--danger" id="modalConfirmBtn">${options.confirmText || 'Confirm'}</button>
                                <button class="modal-btn modal-btn--success" id="modalCancelBtn">Cancel</button>
                            </div>
                        </div>
                    </div>
                `;

                document.body.insertAdjacentHTML('beforeend', modalHtml);

                const modal = document.getElementById('customConfirmModal');
                const confirmBtn = document.getElementById('modalConfirmBtn');
                const cancelBtn = document.getElementById('modalCancelBtn');

                confirmBtn.addEventListener('click', () => {
                    modal.remove();
                    document.removeEventListener('keydown', keyHandler);
                    resolve(true);
                });

                cancelBtn.addEventListener('click', () => {
                    modal.remove();
                    document.removeEventListener('keydown', keyHandler);
                    resolve(false);
                });

                // Close on overlay click
                modal.addEventListener('click', (e) => {
                    if (e.target === modal) {
                        modal.remove();
                        document.removeEventListener('keydown', keyHandler);
                        resolve(false);
                    }
                });

                // Handle keyboard events (Escape key to close)
                const keyHandler = (e) => {
                    if (e.key === 'Escape') {
                        modal.remove();
                        document.removeEventListener('keydown', keyHandler);
                        resolve(false);
                    }
                };
                document.addEventListener('keydown', keyHandler);
            });
        },

        /**
         * Show toast notification
         * @param {string} message - Notification message
         * @param {string} type - Notification type (success, error, warning, info)
         */
        showNotification: function(message, type = 'info') {
            // Use AdminCore.showNotification if available
            if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
                AdminCore.showNotification(message, type);
                return;
            }

            // Fallback: Simple toast notification
            const toast = document.createElement('div');
            toast.className = `toast toast--${type}`;
            toast.textContent = message;
            toast.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                padding: 16px 24px;
                background: ${type === 'success' ? '#92C83E' : type === 'error' ? '#FF5252' : type === 'warning' ? '#FFC107' : '#002B76'};
                color: white;
                border-radius: 4px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                z-index: 10000;
                animation: slideInRight 0.3s ease;
            `;

            document.body.appendChild(toast);

            setTimeout(() => {
                toast.style.animation = 'slideOutRight 0.3s ease';
                setTimeout(() => toast.remove(), 300);
            }, 5000);
        }
    };

    // Certificate Image Preview
    const ImagePreview = {
        init: function() {
            const input = document.getElementById('certificateImage');
            const preview = document.getElementById('certificateImagePreview');
            const previewImg = document.getElementById('certificateImagePreviewImg');
            const placeholder = document.getElementById('certificateImagePlaceholder');

            if (!input) return;

            input.addEventListener('change', async (e) => {
                const file = e.target.files[0];
                if (!file) return;

                // Validate file type
                const validTypes = ['image/jpeg', 'image/jpg', 'image/png'];
                if (!validTypes.includes(file.type)) {
                    ModalSystem.showNotification('Please select a JPG or PNG image file.', 'error');
                    input.value = '';
                    return;
                }

                // Validate file size (5MB max)
                const maxSize = 5 * 1024 * 1024;
                if (file.size > maxSize) {
                    ModalSystem.showNotification('File size must be less than 5MB.', 'error');
                    input.value = '';
                    return;
                }

                // Show preview
                const reader = new FileReader();
                reader.onload = (e) => {
                    previewImg.src = e.target.result;
                    preview.style.display = 'block';
                    placeholder.style.display = 'none';
                };
                reader.readAsDataURL(file);
            });
        }
    };

    // PDF Upload Handler
    const PdfUpload = {
        init: function() {
            const uploadZone = document.getElementById('pdfUploadZone');
            const pdfInput = document.getElementById('certificatePdf');
            const previewContainer = document.getElementById('pdfPreviewContainer');

            if (!uploadZone || !pdfInput) return;

            // Click to upload
            uploadZone.addEventListener('click', () => pdfInput.click());

            // Drag and drop
            uploadZone.addEventListener('dragover', (e) => {
                e.preventDefault();
                uploadZone.style.borderColor = '#447638';
                uploadZone.style.background = 'rgba(146, 200, 62, 0.05)';
            });

            uploadZone.addEventListener('dragleave', () => {
                uploadZone.style.borderColor = '#00AEEF';
                uploadZone.style.background = 'var(--white)';
            });

            uploadZone.addEventListener('drop', (e) => {
                e.preventDefault();
                uploadZone.style.borderColor = '#00AEEF';
                uploadZone.style.background = 'var(--white)';

                const file = e.dataTransfer.files[0];
                if (file) {
                    // Assign the dropped file to the input element using DataTransfer
                    // This is required for the form submission to include the file
                    try {
                        const dataTransfer = new DataTransfer();
                        dataTransfer.items.add(file);
                        pdfInput.files = dataTransfer.files;
                    } catch (err) {
                        console.warn('DataTransfer not supported, drag-drop may not work:', err);
                    }
                    this.handleFile(file, pdfInput, previewContainer);
                }
            });

            // File input change
            pdfInput.addEventListener('change', (e) => {
                const file = e.target.files[0];
                if (file) {
                    this.handleFile(file, pdfInput, previewContainer);
                }
            });
        },

        handleFile: function(file, input, previewContainer) {
            // Validate file type
            if (file.type !== 'application/pdf') {
                ModalSystem.showNotification('Please select a valid PDF file.', 'error');
                input.value = '';
                return;
            }

            // Validate file size (10MB max)
            const maxSize = 10 * 1024 * 1024;
            if (file.size > maxSize) {
                ModalSystem.showNotification('PDF file size must be less than 10MB.', 'error');
                input.value = '';
                return;
            }

            // Show preview
            const fileSizeMB = (file.size / 1024 / 1024).toFixed(2);
            previewContainer.innerHTML = `
                <div class="preview-item" style="margin-top: 16px; padding: 16px; border: 1px solid var(--border-color); border-radius: 4px; display: flex; align-items: center; gap: 12px; background: var(--light-gray);">
                    <div class="file-icon" style="color: var(--warning); font-size: 24px;">📄</div>
                    <div class="file-info" style="flex-grow: 1;">
                        <div class="file-name" style="font-weight: 500; color: var(--text-color);">${file.name}</div>
                        <div class="file-size" style="font-size: 14px; color: var(--text-secondary);">${fileSizeMB}MB</div>
                    </div>
                    <button type="button" class="remove-btn" onclick="window.CertificateCreate.removePdfPreview()" style="background: none; border: none; color: var(--warning); cursor: pointer; font-size: 20px; padding: 4px 8px; border-radius: 4px; transition: var(--transition);">×</button>
                </div>
            `;

            // Reset validation styling on the upload zone
            const uploadZone = document.getElementById('pdfUploadZone');
            if (uploadZone) {
                uploadZone.style.borderColor = '#00AEEF';
                uploadZone.style.backgroundColor = 'rgba(255, 193, 7, 0.03)';
                uploadZone.style.animation = '';
            }

            ModalSystem.showNotification('PDF uploaded successfully!', 'success');
        },

        removePdfPreview: function() {
            const previewContainer = document.getElementById('pdfPreviewContainer');
            const pdfInput = document.getElementById('certificatePdf');
            if (previewContainer) previewContainer.innerHTML = '';
            if (pdfInput) pdfInput.value = '';
        }
    };

    // Tag Management System
    const TagManager = {
        externalTags: [],
        internalTags: [],

        init: function() {
            // External tags
            const externalSelect = document.getElementById('externalTagSelect');
            if (externalSelect) {
                externalSelect.addEventListener('change', () => this.addTag('external'));
            }

            // Internal tags
            const internalSelect = document.getElementById('internalTagSelect');
            if (internalSelect) {
                internalSelect.addEventListener('change', () => this.addTag('internal'));
            }
        },

        addTag: function(type) {
            const select = document.getElementById(type + 'TagSelect');
            const selectedValue = select.value;
            const selectedText = select.options[select.selectedIndex].text;

            if (!selectedValue || selectedValue === '') {
                return;
            }

            const tagArray = type === 'external' ? this.externalTags : this.internalTags;

            // Check if tag already exists
            if (tagArray.includes(parseInt(selectedValue))) {
                ModalSystem.showNotification('Tag already added.', 'warning');
                select.selectedIndex = 0;
                return;
            }

            // Add tag
            tagArray.push(parseInt(selectedValue));
            this.createTagElement(selectedText, selectedValue, type);
            this.updateHiddenInput(type);

            select.selectedIndex = 0;
        },

        createTagElement: function(tagText, tagValue, type) {
            const container = document.getElementById(type + 'TagsContainer');
            if (!container) return;

            const tagElement = document.createElement('span');
            tagElement.className = `tag ${type}`;
            tagElement.dataset.tagId = tagValue;
            tagElement.innerHTML = `
                ${tagText}
                <button type="button" class="tag-remove" onclick="window.CertificateCreate.removeTag('${tagValue}', '${type}')">
                    ×
                </button>
            `;
            container.appendChild(tagElement);
        },

        removeTag: function(tagValue, type) {
            const tagArray = type === 'external' ? this.externalTags : this.internalTags;
            const index = tagArray.indexOf(parseInt(tagValue));

            if (index > -1) {
                tagArray.splice(index, 1);
                this.updateHiddenInput(type);

                // Remove the visual element
                const container = document.getElementById(type + 'TagsContainer');
                if (container) {
                    const tagElements = container.querySelectorAll('.tag');
                    tagElements.forEach(element => {
                        if (element.dataset.tagId === tagValue) {
                            element.remove();
                        }
                    });
                }
            }
        },

        updateHiddenInput: function(type) {
            const tagArray = type === 'external' ? this.externalTags : this.internalTags;
            const hiddenInput = document.getElementById(type + 'TagsHidden');
            if (hiddenInput) {
                hiddenInput.value = JSON.stringify(tagArray);
            }
        }
    };

    // Unsaved Changes Detection
    const UnsavedChanges = {
        originalFormData: null,
        isSubmitting: false,
        hasChanges: false,

        init: function() {
            const form = document.getElementById('certificateCreateForm') || document.getElementById('certificateEditForm');
            if (!form) return;

            // Store original form values
            this.originalFormData = new FormData(form);
            this.originalValues = this.getFormValues(form);

            // Track changes on all form inputs
            form.addEventListener('input', () => this.markAsChanged());
            form.addEventListener('change', () => this.markAsChanged());

            // Track form submission
            form.addEventListener('submit', () => {
                this.isSubmitting = true;
            });

            // Warn before leaving if there are unsaved changes
            window.addEventListener('beforeunload', (e) => {
                if (this.hasChanges && !this.isSubmitting) {
                    e.preventDefault();
                    e.returnValue = 'You have unsaved changes. Are you sure you want to leave?';
                    return e.returnValue;
                }
            });

            // Also handle clicks on navigation links
            document.querySelectorAll('a[href]:not([target="_blank"])').forEach(link => {
                link.addEventListener('click', (e) => {
                    if (this.hasChanges && !this.isSubmitting) {
                        const href = link.getAttribute('href');
                        // Skip if it's an anchor link or JavaScript
                        if (href.startsWith('#') || href.startsWith('javascript:')) return;

                        e.preventDefault();
                        this.confirmNavigation(href);
                    }
                });
            });
        },

        getFormValues: function(form) {
            const values = {};
            const formData = new FormData(form);
            for (const [key, value] of formData.entries()) {
                values[key] = value;
            }
            return values;
        },

        markAsChanged: function() {
            this.hasChanges = true;
        },

        confirmNavigation: async function(href) {
            const confirmed = await ModalSystem.showConfirmation({
                title: 'Unsaved Changes',
                message: 'You have unsaved changes that will be lost. Are you sure you want to leave this page?',
                confirmText: 'Leave Page',
                cancelText: 'Stay'
            });

            if (confirmed) {
                this.isSubmitting = true; // Prevent double prompt
                window.location.href = href;
            }
        },

        reset: function() {
            this.hasChanges = false;
            this.isSubmitting = false;
        }
    };

    // Loading Spinner System
    const LoadingSpinner = {
        overlay: null,

        show: function(message = 'Saving certificate...') {
            if (this.overlay) return; // Already showing

            this.overlay = document.createElement('div');
            this.overlay.className = 'loading-overlay';
            this.overlay.innerHTML = `
                <div class="loading-content">
                    <div class="loading-spinner"></div>
                    <p class="loading-message">${message}</p>
                </div>
            `;
            this.overlay.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background-color: rgba(0, 0, 0, 0.7);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 10000;
                backdrop-filter: blur(3px);
            `;

            const content = this.overlay.querySelector('.loading-content');
            content.style.cssText = `
                text-align: center;
                color: white;
            `;

            const spinner = this.overlay.querySelector('.loading-spinner');
            spinner.style.cssText = `
                width: 50px;
                height: 50px;
                border: 4px solid rgba(255, 255, 255, 0.3);
                border-top-color: #92C83E;
                border-radius: 50%;
                animation: spin 1s linear infinite;
                margin: 0 auto 16px;
            `;

            const msg = this.overlay.querySelector('.loading-message');
            msg.style.cssText = `
                font-size: 18px;
                font-weight: 500;
                margin: 0;
            `;

            // Add keyframes animation
            if (!document.getElementById('spinner-keyframes')) {
                const style = document.createElement('style');
                style.id = 'spinner-keyframes';
                style.textContent = '@keyframes spin { to { transform: rotate(360deg); } }';
                document.head.appendChild(style);
            }

            document.body.appendChild(this.overlay);
            document.body.style.overflow = 'hidden';
        },

        hide: function() {
            if (this.overlay) {
                this.overlay.remove();
                this.overlay = null;
                document.body.style.overflow = '';
            }
        },

        updateMessage: function(message) {
            if (this.overlay) {
                const msg = this.overlay.querySelector('.loading-message');
                if (msg) msg.textContent = message;
            }
        }
    };

    // Form Validation
    const FormValidation = {
        init: function() {
            const form = document.getElementById('certificateCreateForm') || document.getElementById('certificateEditForm');
            if (!form) return;

            form.addEventListener('submit', (event) => {
                if (!form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();

                    // Show notification about validation errors
                    const invalidFields = form.querySelectorAll(':invalid');
                    const fieldNames = [];

                    invalidFields.forEach(field => {
                        // Get field label or name
                        const label = field.closest('.form-group')?.querySelector('label');
                        if (label) {
                            // Remove the asterisk span from label text
                            const labelText = label.textContent.replace('*', '').trim();
                            if (labelText && !fieldNames.includes(labelText)) {
                                fieldNames.push(labelText);
                            }
                        }
                    });

                    // Check for PDF upload specifically (hidden field)
                    const pdfInput = document.getElementById('certificatePdf');
                    if (pdfInput && !pdfInput.files.length && pdfInput.hasAttribute('required')) {
                        if (!fieldNames.includes('Upload PDF Certificate')) {
                            fieldNames.push('Upload PDF Certificate');
                        }
                        // Highlight the PDF upload zone
                        const pdfZone = document.getElementById('pdfUploadZone');
                        if (pdfZone) {
                            pdfZone.style.borderColor = '#FF5252';
                            pdfZone.style.backgroundColor = 'rgba(255, 82, 82, 0.05)';
                            pdfZone.style.animation = 'shake 0.5s ease-in-out';
                        }
                    }

                    if (fieldNames.length > 0) {
                        const message = fieldNames.length === 1
                            ? `Please fill in the required field: ${fieldNames[0]}`
                            : `Please fill in the required fields: ${fieldNames.slice(0, 3).join(', ')}${fieldNames.length > 3 ? ` and ${fieldNames.length - 3} more` : ''}`;
                        ModalSystem.showNotification(message, 'error');
                    } else {
                        ModalSystem.showNotification('Please fill in all required fields.', 'error');
                    }

                    // Scroll to first invalid field
                    const firstInvalid = form.querySelector(':invalid');
                    if (firstInvalid) {
                        firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        firstInvalid.focus();
                    }
                } else {
                    // Form is valid - show loading spinner
                    LoadingSpinner.show('Saving certificate...');
                }
                form.classList.add('was-validated');
            });

            // Date validation
            this.initDateValidation();
        },

        initDateValidation: function() {
            const effectiveDate = document.querySelector('input[name="EffectiveDate"]');
            const expirationDate = document.querySelector('input[name="ExpirationDate"]');

            if (!effectiveDate || !expirationDate) return;

            const validateDates = () => {
                if (effectiveDate.value && expirationDate.value) {
                    const effective = new Date(effectiveDate.value);
                    const expiration = new Date(expirationDate.value);

                    if (expiration <= effective) {
                        expirationDate.setCustomValidity('Expiration date must be after effective date');
                        ModalSystem.showNotification('Expiration date must be after effective date.', 'warning');
                    } else {
                        expirationDate.setCustomValidity('');
                    }
                }
            };

            effectiveDate.addEventListener('change', validateDates);
            expirationDate.addEventListener('change', validateDates);
        },

        /**
         * Handle server-side validation errors on page load
         * This handles errors returned after POST when ModelState is invalid
         */
        handleServerValidationErrors: function() {
            // Check for validation summary errors (must have actual visible content)
            const validationSummary = document.querySelector('.alert-danger, .validation-summary-errors');
            const hasErrors = validationSummary && validationSummary.textContent.trim().length > 0
                && validationSummary.querySelector('li'); // Must have actual error items

            // Check for field-level validation errors - only count spans with actual visible text
            const allFieldErrors = document.querySelectorAll('.field-validation-error, span.text-danger');
            const fieldErrors = Array.from(allFieldErrors).filter(span => {
                const text = span.textContent.trim();
                return text.length > 0 && text !== '*'; // Ignore asterisks and empty spans
            });
            const hasFieldErrors = fieldErrors.length > 0;

            // Only process if there are ACTUAL server-side errors (not empty validation spans)
            if (!hasErrors && !hasFieldErrors) return;

            // Add was-validated class to show validation styles
            const form = document.getElementById('certificateCreateForm') || document.getElementById('certificateEditForm');
            if (form) {
                form.classList.add('was-validated');
            }

            // Highlight fields with server-side errors
            fieldErrors.forEach(errorSpan => {
                if (errorSpan.textContent.trim()) {
                    const formGroup = errorSpan.closest('.form-group');
                    if (formGroup) {
                        const input = formGroup.querySelector('input, select, textarea');
                        if (input) {
                            input.classList.add('is-invalid');
                            input.style.borderColor = '#FF5252';
                            input.style.backgroundColor = 'rgba(255, 82, 82, 0.05)';
                            input.style.boxShadow = '0 0 0 3px rgba(255, 82, 82, 0.15)';
                        }
                        // Highlight label
                        const label = formGroup.querySelector('label');
                        if (label) {
                            label.style.color = '#FF5252';
                        }
                    }
                }
            });

            // Scroll to validation summary or first error
            if (validationSummary && hasErrors) {
                validationSummary.scrollIntoView({ behavior: 'smooth', block: 'center' });
                // Add attention animation
                validationSummary.style.animation = 'shake 0.5s ease-in-out';

                // Show toast notification
                const errorCount = validationSummary.querySelectorAll('li').length || 1;
                ModalSystem.showNotification(
                    `Please fix ${errorCount} validation error${errorCount > 1 ? 's' : ''} before saving.`,
                    'error'
                );
            } else if (hasFieldErrors) {
                const firstError = document.querySelector('.field-validation-error:not(:empty), .text-danger:not(:empty)');
                if (firstError) {
                    firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
                ModalSystem.showNotification('Please correct the highlighted fields.', 'error');
            }
        }
    };

    // Initialize all components on page load
    window.addEventListener('load', function() {
        ImagePreview.init();
        PdfUpload.init();
        TagManager.init();
        FormValidation.init();
        UnsavedChanges.init();

        // Handle server-side validation errors (displayed after POST failure)
        FormValidation.handleServerValidationErrors();

        // Product ID Manager is auto-initialized by product-id-manager.js
        // No need to initialize here - it handles its own DOMContentLoaded event

        // Show success message if present (for "Save & Add Another")
        const successMessage = document.querySelector('[data-success-message]');
        if (successMessage) {
            ModalSystem.showNotification(successMessage.dataset.successMessage, 'success');
        }
    });

    // Expose public API
    window.CertificateCreate = {
        removePdfPreview: PdfUpload.removePdfPreview,
        removeTag: TagManager.removeTag.bind(TagManager),
        showNotification: ModalSystem.showNotification,
        showConfirmation: ModalSystem.showConfirmation,
        resetUnsavedChanges: UnsavedChanges.reset.bind(UnsavedChanges),
        markAsChanged: UnsavedChanges.markAsChanged.bind(UnsavedChanges),
        showLoading: LoadingSpinner.show.bind(LoadingSpinner),
        hideLoading: LoadingSpinner.hide.bind(LoadingSpinner)
    };

})();
