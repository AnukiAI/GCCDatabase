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

    // Form Validation
    const FormValidation = {
        init: function() {
            const form = document.getElementById('certificateCreateForm');
            if (!form) return;

            form.addEventListener('submit', (event) => {
                if (!form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();
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
        }
    };

    // Initialize all components on page load
    window.addEventListener('load', function() {
        ImagePreview.init();
        PdfUpload.init();
        TagManager.init();
        FormValidation.init();

        // Initialize Product ID Manager (if present)
        const productIdManagerElement = document.querySelector('.product-id-manager');
        if (productIdManagerElement && typeof ProductIdManager !== 'undefined') {
            new ProductIdManager('.product-id-manager', 0, false);
        }

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
        showConfirmation: ModalSystem.showConfirmation
    };

})();
