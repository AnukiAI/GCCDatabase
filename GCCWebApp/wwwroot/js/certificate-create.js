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
                    <div class="custom-modal-overlay" id="customConfirmModal">
                        <div class="custom-modal-dialog">
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
            // Non-blocking corner TOAST for upload feedback — must never overlay the Save button.
            //
            // DEF-261: this previously called AdminCore.showNotification (which routes ALL
            // notifications to a centered, full-screen blocking modal) with a center-screen modal
            // fallback. On the cert Create/Edit pages that modal sits on top of the Save button, so a
            // user who attached a PDF, saw the "Warning: PDF uploaded…" message, and clicked Save had
            // the click swallowed by the overlay — the certificate never updated. A toast can't block Save.
            // Scoped to the certificate pages only; AdminCore's global modal behavior is left untouched.
            const colors = { success: '#92C83E', error: '#FF5252', warning: '#FFC107', info: '#002B76' };
            const titles = { success: 'Success', error: 'Error', warning: 'Warning', info: 'Information' };
            const accent = colors[type] || colors.info;
            const titleColor = type === 'error' ? '#FF5252' : '#002B76';

            // Shared top-right, click-through container (created once).
            let container = document.getElementById('certUploadToastStack');
            if (!container) {
                container = document.createElement('div');
                container.id = 'certUploadToastStack';
                container.style.cssText = 'position:fixed; top:20px; right:20px; z-index:10000;'
                    + 'display:flex; flex-direction:column; gap:10px; pointer-events:none; max-width:90vw;';
                document.body.appendChild(container);
            }

            const toast = document.createElement('div');
            toast.setAttribute('role', type === 'error' ? 'alert' : 'status');
            toast.style.cssText = 'pointer-events:auto; min-width:280px; max-width:380px; background:#ffffff;'
                + 'border-left:5px solid ' + accent + '; border-radius:6px; box-shadow:0 4px 16px rgba(0,0,0,0.2);'
                + 'padding:14px 16px; display:flex; align-items:flex-start; gap:10px;'
                + 'opacity:0; transform:translateX(20px); transition:opacity .25s ease, transform .25s ease;';
            toast.innerHTML = `
                <div style="flex-grow:1;">
                    <div style="color:${titleColor}; font-size:16px; font-weight:600; margin-bottom:2px;">${titles[type] || 'Information'}</div>
                    <div style="color:#333333; font-size:16px; line-height:1.4;">${message}</div>
                </div>
                <button type="button" aria-label="Dismiss notification" style="background:none; border:none; color:#888888; font-size:18px; line-height:1; cursor:pointer; padding:0 2px;">&times;</button>
            `;
            container.appendChild(toast);
            requestAnimationFrame(() => { toast.style.opacity = '1'; toast.style.transform = 'translateX(0)'; });

            const dismiss = () => {
                toast.style.opacity = '0';
                toast.style.transform = 'translateX(20px)';
                setTimeout(() => toast.remove(), 250);
            };
            toast.querySelector('button').addEventListener('click', dismiss);
            // Errors linger a little longer; nothing ever blocks the page.
            setTimeout(dismiss, type === 'error' ? 6000 : 4000);
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

        handleFile: async function(file, input, previewContainer) {
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

            // Show file info preview
            const fileSizeMB = (file.size / 1024 / 1024).toFixed(2);
            previewContainer.innerHTML = `
                <div class="preview-item" style="margin-top: 16px; padding: 16px; border: 1px solid var(--border-color); border-radius: 4px; display: flex; align-items: center; gap: 12px; background: var(--light-gray);">
                    <div class="file-icon" style="color: var(--warning); font-size: 24px;">📄</div>
                    <div class="file-info" style="flex-grow: 1;">
                        <div class="file-name" style="font-weight: 500; color: var(--text-color);">${file.name}</div>
                        <div class="file-size" style="font-size: 14px; color: var(--text-secondary);">${fileSizeMB}MB</div>
                    </div>
                    <button type="button" class="remove-btn" onclick="window.CertificateCreate.removePdfPreview()" style="background: none; border: none; color: #FF5252; cursor: pointer; font-size: 18px; padding: 4px 8px; border-radius: 4px; transition: var(--transition);" title="Remove PDF"><i class="fas fa-trash-alt"></i></button>
                </div>
            `;

            // Reset validation styling on the upload zone
            const uploadZone = document.getElementById('pdfUploadZone');
            if (uploadZone) {
                uploadZone.style.borderColor = '#00AEEF';
                uploadZone.style.backgroundColor = 'rgba(255, 193, 7, 0.03)';
                uploadZone.style.animation = '';
            }

            // Show loading spinner in sidebar preview container
            const sidebarPreview = document.querySelector('.preview-container');
            if (sidebarPreview) {
                sidebarPreview.innerHTML = `
                    <div class="thumbnail-loading" style="text-align: center; padding: 40px 20px;">
                        <div style="
                            width: 40px;
                            height: 40px;
                            border: 3px solid #e0e0e0;
                            border-top-color: #00AEEF;
                            border-radius: 50%;
                            animation: spin 1s linear infinite;
                            margin: 0 auto 16px;
                        "></div>
                        <p style="color: #002B76; margin: 0; font-weight: 500;">Generating preview...</p>
                        <small style="color: #6c757d;">This may take a few seconds</small>
                    </div>
                `;
            }

            // Call API to generate thumbnail
            try {
                const formData = new FormData();
                formData.append('file', file);

                // Get certificate number if available (for naming)
                const certNumberInput = document.getElementById('Number');
                if (certNumberInput && certNumberInput.value) {
                    formData.append('certificateNumber', certNumberInput.value);
                }

                const response = await fetch('/api/CertificatesApi/GenerateThumbnail', {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    }
                });

                const result = await response.json();

                if (response.ok && result.success && result.thumbnailUrl) {
                    // Display thumbnail in sidebar with zoom functionality
                    if (sidebarPreview) {
                        sidebarPreview.innerHTML = `
                            <img id="certificateImagePreviewImg"
                                 src="${result.thumbnailUrl}"
                                 alt="Certificate Preview"
                                 style="max-width: 100%; max-height: 100%; object-fit: contain; cursor: pointer;"
                                 onclick="typeof openImageZoom === 'function' && openImageZoom('${result.thumbnailUrl}')"
                            />
                            <div class="zoom-icon-overlay" onclick="typeof openImageZoom === 'function' && openImageZoom('${result.thumbnailUrl}')" title="Click to zoom"
                                 style="position: absolute; bottom: 8px; right: 8px; background: rgba(0,0,0,0.6); color: white; padding: 6px 10px; border-radius: 4px; font-size: 12px; cursor: pointer;">
                                <i class="fas fa-search-plus"></i>
                            </div>
                        `;
                    }

                    // Store thumbnail path in hidden input
                    const hiddenInput = document.getElementById('certificateImagePathHidden') || document.getElementById('CertificateImagePath');
                    if (hiddenInput) {
                        hiddenInput.value = result.thumbnailPath;
                    }

                    ModalSystem.showNotification('PDF uploaded and preview generated!', 'success');
                } else {
                    throw new Error(result.message || 'Failed to generate preview');
                }
            } catch (error) {
                console.error('Thumbnail generation failed:', error);

                // Show fallback state but keep the file selected
                if (sidebarPreview) {
                    sidebarPreview.innerHTML = `
                        <div class="no-preview" style="text-align: center; color: #6c757d;">
                            <i class="fas fa-exclamation-triangle" style="font-size: 48px; color: #FFC107; margin-bottom: 12px; display: block;"></i>
                            <p style="font-size: 14px; font-weight: 600; color: #6c757d; margin: 0 0 4px 0;">Preview unavailable</p>
                            <small style="color: #adb5bd; font-size: 11px;">Preview will be generated when you save</small>
                        </div>
                    `;
                }

                ModalSystem.showNotification('PDF uploaded. Preview will be generated on save.', 'success');
            }
        },

        removePdfPreview: function() {
            const previewContainer = document.getElementById('pdfPreviewContainer');
            const pdfInput = document.getElementById('certificatePdf');
            const sidebarPreview = document.querySelector('.preview-container');
            const hiddenInput = document.getElementById('certificateImagePathHidden') || document.getElementById('CertificateImagePath');

            // Clear file info preview
            if (previewContainer) previewContainer.innerHTML = '';
            if (pdfInput) pdfInput.value = '';

            // Reset sidebar preview to placeholder state
            if (sidebarPreview) {
                sidebarPreview.innerHTML = `
                    <div class="no-preview" style="text-align: center; color: #6c757d;">
                        <i class="fas fa-file-pdf" style="font-size: 48px; color: #adb5bd; margin-bottom: 12px; display: block;"></i>
                        <p style="font-size: 14px; font-weight: 600; color: #6c757d; margin: 0 0 4px 0;">Certificate Unavailable</p>
                        <small style="color: #adb5bd; font-size: 11px;">Upload a PDF to generate preview</small>
                    </div>
                `;
            }

            // Clear stored thumbnail path
            if (hiddenInput) hiddenInput.value = '';
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

    // Subcategory Management System (Multi-Select)
    const SubcategoryManager = {
        subcategories: [],

        init: function() {
            const subcategorySelect = document.getElementById('subcategorySelect');
            if (subcategorySelect) {
                subcategorySelect.addEventListener('change', () => this.addSubcategory());
            }
        },

        addSubcategory: function() {
            const select = document.getElementById('subcategorySelect');
            const selectedValue = parseInt(select.value);
            const selectedText = select.options[select.selectedIndex].text;

            if (!selectedValue) {
                return;
            }

            // Check if subcategory already exists
            if (this.subcategories.find(s => s.id === selectedValue)) {
                ModalSystem.showNotification('Subcategory already added.', 'warning');
                select.selectedIndex = 0;
                return;
            }

            // Add subcategory
            this.subcategories.push({ id: selectedValue, name: selectedText });
            this.updateDisplay();
            this.updateHiddenInput();

            select.selectedIndex = 0;
        },

        removeSubcategory: function(subcatId) {
            this.subcategories = this.subcategories.filter(s => s.id !== parseInt(subcatId));
            this.updateDisplay();
            this.updateHiddenInput();
        },

        updateDisplay: function() {
            const container = document.getElementById('subcategoriesContainer');
            if (!container) return;

            container.innerHTML = this.subcategories.map(s => `
                <span class="tag subcategory" data-subcategory-id="${s.id}">
                    ${s.name}
                    <button type="button" class="tag-remove" onclick="window.CertificateCreate.removeSubcategory('${s.id}')">
                        ×
                    </button>
                </span>
            `).join('');
        },

        updateHiddenInput: function() {
            const hiddenInput = document.getElementById('subcategoriesHidden');
            if (hiddenInput) {
                hiddenInput.value = JSON.stringify(this.subcategories.map(s => s.id));
            }
        },

        loadExisting: function(existingSubcategories) {
            if (!existingSubcategories || !Array.isArray(existingSubcategories)) return;
            this.subcategories = existingSubcategories.map(s => ({ id: s.Id || s.id, name: s.Name || s.name }));
            this.updateDisplay();
            this.updateHiddenInput();
        },

        clear: function() {
            this.subcategories = [];
            this.updateDisplay();
            this.updateHiddenInput();
        }
    };

    // Unsaved Changes Detection
    const UnsavedChanges = {
        originalFormData: null,
        isSubmitting: false,
        hasChanges: false,
        initialized: false,
        form: null,

        storageKey: null,
        autoSaveInterval: null,

        init: function() {
            const form = document.getElementById('certificateCreateForm') || document.getElementById('certificateEditForm');
            if (!form) return;
            this.form = form;
            this.storageKey = 'gcc_form_backup_' + (form.id || 'cert') + '_' + window.location.pathname;

            // Check for recovered data before initializing
            this.checkForRecovery();

            // Delay initialization to allow other components to finish setup
            // This prevents false positives from initialization-triggered changes
            setTimeout(() => {
                // Store original form values AFTER other components have initialized
                this.originalFormData = new FormData(form);
                this.originalValues = this.getFormValues(form);
                this.initialized = true;
                this.hasChanges = false;
                console.log('[UnsavedChanges] Initialized - now tracking changes');
            }, 500);

            // Track changes on all form inputs (only mark as changed if initialized)
            form.addEventListener('input', () => {
                if (this.initialized) this.markAsChanged();
            });
            form.addEventListener('change', () => {
                if (this.initialized) this.markAsChanged();
            });

            // Track form submission — clear backup on successful submit
            form.addEventListener('submit', () => {
                this.isSubmitting = true;
                this.clearBackup();
            });

            // Auto-save form data to localStorage every 30 seconds
            this.autoSaveInterval = setInterval(() => {
                if (this.initialized && this.hasChanges) {
                    this.saveToLocalStorage();
                }
            }, 30000);

            // Warn before leaving if there are unsaved changes
            window.addEventListener('beforeunload', (e) => {
                if (this.hasActualChanges() && !this.isSubmitting) {
                    e.preventDefault();
                    e.returnValue = 'You have unsaved changes. Are you sure you want to leave?';
                    return e.returnValue;
                }
            });

            // Also handle clicks on navigation links
            document.querySelectorAll('a[href]:not([target="_blank"])').forEach(link => {
                link.addEventListener('click', (e) => {
                    if (this.hasActualChanges() && !this.isSubmitting) {
                        const href = link.getAttribute('href');
                        // Skip if it's an anchor link or JavaScript
                        if (!href || href.startsWith('#') || href.startsWith('javascript:')) return;

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

        // Compare current form values against the baseline to detect real changes
        hasActualChanges: function() {
            if (!this.initialized || !this.form || !this.originalValues) return false;
            if (!this.hasChanges) return false;

            // Do a real comparison of form values vs original baseline
            const currentValues = this.getFormValues(this.form);
            const allKeys = new Set([...Object.keys(currentValues), ...Object.keys(this.originalValues)]);
            for (const key of allKeys) {
                if ((currentValues[key] || '') !== (this.originalValues[key] || '')) {
                    return true;
                }
            }
            // Boolean flag was set but values match baseline - reset the flag
            this.hasChanges = false;
            return false;
        },

        markAsChanged: function() {
            this.hasChanges = true;
        },

        confirmNavigation: async function(href) {
            console.log('[UnsavedChanges] Showing navigation confirmation for:', href);
            try {
                const confirmed = await ModalSystem.showConfirmation({
                    title: 'Unsaved Changes',
                    message: 'You have unsaved changes that will be lost. Are you sure you want to leave this page?',
                    confirmText: 'Leave Page',
                    cancelText: 'Stay'
                });

                console.log('[UnsavedChanges] User confirmed:', confirmed);
                if (confirmed) {
                    // Reset all flags to prevent any interference with navigation
                    this.hasChanges = false;
                    this.isSubmitting = true;
                    console.log('[UnsavedChanges] Navigating to:', href);
                    window.location.href = href;
                }
            } catch (error) {
                console.error('[UnsavedChanges] Error in confirmNavigation:', error);
                // If modal fails, allow navigation rather than trapping the user
                this.hasChanges = false;
                this.isSubmitting = true;
                window.location.href = href;
            }
        },

        reset: function() {
            this.hasChanges = false;
            this.isSubmitting = false;
        },

        // Re-capture the baseline form values (call after async initialization completes)
        rebaseline: function() {
            if (this.form) {
                this.originalValues = this.getFormValues(this.form);
                this.hasChanges = false;
                console.log('[UnsavedChanges] Rebaselined form values');
            }
        },

        // Save current form data to localStorage
        saveToLocalStorage: function() {
            if (!this.form || !this.storageKey) return;
            try {
                const data = {
                    values: this.getFormValues(this.form),
                    timestamp: Date.now(),
                    url: window.location.href
                };
                localStorage.setItem(this.storageKey, JSON.stringify(data));
                console.log('[UnsavedChanges] Auto-saved form data to localStorage');
            } catch (e) {
                console.warn('[UnsavedChanges] Failed to save to localStorage:', e);
            }
        },

        // Clear localStorage backup
        clearBackup: function() {
            if (this.storageKey) {
                localStorage.removeItem(this.storageKey);
                console.log('[UnsavedChanges] Cleared localStorage backup');
            }
            if (this.autoSaveInterval) {
                clearInterval(this.autoSaveInterval);
            }
        },

        // DEF-151 / CR-017: Recovery prompt suppressed pending Phase 2 UX rework.
        // The current banner fires on minor field-level diffs (cascading dropdowns
        // re-firing change events on initial load can trigger it), and the
        // Restore button doesn't replay file uploads or the cert PDF, so a
        // partial restore puts the form into an inconsistent state. Hiding the
        // banner is a one-line gate; auto-save to localStorage continues so the
        // data is still there for Phase 2 to consume cleanly. Stale entries
        // older than 2 hours are GC'd on each page load (existing logic below)
        // so localStorage doesn't accumulate forever while this is hidden.
        SHOW_RECOVERY_PROMPT: false,

        // Check for recovered data on page load
        checkForRecovery: function() {
            if (!this.storageKey || !this.form) return;

            // DEF-151: hide the recovery banner. Continue to GC stale entries
            // so we don't leak localStorage while disabled.
            if (!this.SHOW_RECOVERY_PROMPT) {
                try {
                    const saved = localStorage.getItem(this.storageKey);
                    if (saved) {
                        const data = JSON.parse(saved);
                        const twoHours = 2 * 60 * 60 * 1000;
                        if (Date.now() - data.timestamp > twoHours) {
                            localStorage.removeItem(this.storageKey);
                        }
                    }
                } catch (e) {
                    // Corrupt entry — clear it so it doesn't keep failing parse on every load.
                    localStorage.removeItem(this.storageKey);
                }
                return;
            }

            try {
                const saved = localStorage.getItem(this.storageKey);
                if (!saved) return;

                const data = JSON.parse(saved);
                // Only offer recovery if data is less than 2 hours old
                const twoHours = 2 * 60 * 60 * 1000;
                if (Date.now() - data.timestamp > twoHours) {
                    localStorage.removeItem(this.storageKey);
                    return;
                }

                // Check if there's actually different data to restore
                const currentValues = this.getFormValues(this.form);
                let hasDifferences = false;
                for (const key of Object.keys(data.values)) {
                    // Skip anti-forgery tokens and file inputs
                    if (key === '__RequestVerificationToken' || key === 'CertificateFile' || key === 'ThumbnailFile') continue;
                    if ((data.values[key] || '') !== (currentValues[key] || '')) {
                        hasDifferences = true;
                        break;
                    }
                }

                if (!hasDifferences) {
                    localStorage.removeItem(this.storageKey);
                    return;
                }

                const savedTime = new Date(data.timestamp);
                const timeStr = savedTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

                // Show recovery prompt
                if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
                    const recoveryBanner = document.createElement('div');
                    recoveryBanner.className = 'form-recovery-banner';
                    recoveryBanner.innerHTML = `
                        <div style="background: #fff3cd; border: 1px solid #ffc107; border-radius: 6px; padding: 12px 16px; margin-bottom: 16px; display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 8px;">
                            <span style="font-size: 14px;"><i class="fas fa-exclamation-triangle" style="color: #856404; margin-right: 8px;"></i>Unsaved form data recovered from ${timeStr}. Would you like to restore it?</span>
                            <span>
                                <button type="button" class="btn btn-sm btn-warning" id="restoreFormData" style="margin-right: 8px;">Restore</button>
                                <button type="button" class="btn btn-sm btn-outline-secondary" id="discardFormData">Discard</button>
                            </span>
                        </div>`;
                    this.form.parentNode.insertBefore(recoveryBanner, this.form);

                    document.getElementById('restoreFormData').addEventListener('click', () => {
                        this.restoreFromLocalStorage(data.values);
                        recoveryBanner.remove();
                        AdminCore.showNotification('Form data restored successfully.', 'success');
                    });

                    document.getElementById('discardFormData').addEventListener('click', () => {
                        localStorage.removeItem(this.storageKey);
                        recoveryBanner.remove();
                    });
                }
            } catch (e) {
                console.warn('[UnsavedChanges] Error checking for recovery:', e);
            }
        },

        // Restore form values from saved data
        restoreFromLocalStorage: function(values) {
            if (!this.form || !values) return;
            for (const [key, value] of Object.entries(values)) {
                // Skip tokens and file inputs
                if (key === '__RequestVerificationToken' || key === 'CertificateFile' || key === 'ThumbnailFile') continue;
                const input = this.form.querySelector(`[name="${key}"]`);
                if (input && input.type !== 'file') {
                    input.value = value;
                    // Trigger change event for cascading dropdowns
                    input.dispatchEvent(new Event('change', { bubbles: true }));
                }
            }
            this.markAsChanged();
            console.log('[UnsavedChanges] Restored form data from localStorage');
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

    // Form Validation - Minimal, delegates to jQuery Unobtrusive Validation
    // Custom dropdowns have their own validation in certificate-inline-create.js
    const FormValidation = {
        init: function() {
            const form = document.getElementById('certificateCreateForm') || document.getElementById('certificateEditForm');
            if (!form) return;

            // Add inline validation styles
            this.addInlineValidationStyles();

            // Date validation (additional custom validation)
            this.initDateValidation();

            // Note: Loading spinner is NOT shown here - form submits normally
            // jQuery Unobtrusive Validation handles required field validation
        },

        /**
         * Validate all required fields and return list of errors
         */
        validateRequiredFields: function(form) {
            const errors = [];

            // Check Company (required in UI)
            const companySelect = form.querySelector('#CompanyId');
            if (companySelect && !companySelect.value) {
                errors.push({
                    formGroup: companySelect.closest('.form-group'),
                    field: companySelect,
                    message: 'Company is required'
                });
            }

            // Check Certification Type
            const certTypeSelect = form.querySelector('#CertificationTypeId');
            if (certTypeSelect && !certTypeSelect.value) {
                errors.push({
                    formGroup: certTypeSelect.closest('.form-group'),
                    field: certTypeSelect,
                    message: 'Certification Type is required'
                });
            }

            // Check Product Name
            const productSelect = form.querySelector('#ProductId');
            if (productSelect && !productSelect.value) {
                errors.push({
                    formGroup: productSelect.closest('.form-group'),
                    field: productSelect,
                    message: 'Product Name is required'
                });
            }

            // Check Effective Date
            const effectiveDate = form.querySelector('input[name="EffectiveDate"]');
            if (effectiveDate && !effectiveDate.value) {
                errors.push({
                    formGroup: effectiveDate.closest('.form-group'),
                    field: effectiveDate,
                    message: 'Effective Date is required'
                });
            }

            // Check Expiration Date
            const expirationDate = form.querySelector('input[name="ExpirationDate"]');
            if (expirationDate && !expirationDate.value) {
                errors.push({
                    formGroup: expirationDate.closest('.form-group'),
                    field: expirationDate,
                    message: 'Expiration Date is required'
                });
            }

            // Check Status
            const statusSelect = form.querySelector('#Status, select[name="Status"]');
            if (statusSelect && !statusSelect.value) {
                errors.push({
                    formGroup: statusSelect.closest('.form-group'),
                    field: statusSelect,
                    message: 'Status is required'
                });
            }

            // Check PDF Upload
            const pdfInput = document.getElementById('certificatePdf');
            if (pdfInput && pdfInput.hasAttribute('required') && !pdfInput.files.length) {
                const pdfZone = document.getElementById('pdfUploadZone');
                errors.push({
                    formGroup: pdfZone?.closest('.form-group'),
                    field: pdfInput,
                    message: 'Please upload a PDF certificate'
                });
                // Highlight PDF zone
                if (pdfZone) {
                    pdfZone.style.borderColor = '#FF5252';
                    pdfZone.style.backgroundColor = 'rgba(255, 82, 82, 0.05)';
                }
            }

            return errors;
        },

        /**
         * Show error for a form group - uses existing asp-validation-for spans
         */
        showError: function(formGroup, message) {
            if (!formGroup) return;

            formGroup.classList.add('has-error');

            // Highlight custom dropdown trigger if present
            const trigger = formGroup.querySelector('.custom-dropdown-trigger');
            if (trigger) {
                trigger.style.borderColor = '#FF5252';
                trigger.style.backgroundColor = 'rgba(255, 82, 82, 0.05)';
            }

            // Use existing asp-validation-for span if present
            const existingValidationSpan = formGroup.querySelector('.text-danger[data-valmsg-for], span.text-danger[role="alert"]');
            if (existingValidationSpan) {
                existingValidationSpan.textContent = message;
                existingValidationSpan.classList.remove('field-validation-valid');
                existingValidationSpan.classList.add('field-validation-error');
                return; // Don't create duplicate
            }

            // Fallback: Add error message only if no existing validation span
            if (!formGroup.querySelector('.inline-validation-error')) {
                const errorDiv = document.createElement('div');
                errorDiv.className = 'inline-validation-error';
                errorDiv.textContent = message;

                // Insert after the input/dropdown wrapper
                const insertAfter = formGroup.querySelector('.custom-dropdown-wrapper') ||
                                   formGroup.querySelector('.pdf-upload') ||
                                   formGroup.querySelector('input, select, textarea');
                if (insertAfter) {
                    insertAfter.insertAdjacentElement('afterend', errorDiv);
                } else {
                    formGroup.appendChild(errorDiv);
                }
            }
        },

        /**
         * Add CSS styles for inline validation
         */
        addInlineValidationStyles: function() {
            if (document.getElementById('inline-validation-styles')) return;

            const style = document.createElement('style');
            style.id = 'inline-validation-styles';
            style.textContent = `
                .inline-validation-error {
                    display: block;
                    margin-top: 8px;
                    padding: 0;
                }
                .form-group.has-error label {
                    color: #FF5252;
                }
            `;
            document.head.appendChild(style);
        },

        /**
         * Clear inline error for a specific form group
         */
        clearInlineError: function(formGroup) {
            formGroup.classList.remove('has-error');

            // Clear existing asp-validation-for spans
            const existingValidationSpan = formGroup.querySelector('.text-danger[data-valmsg-for], span.text-danger[role="alert"]');
            if (existingValidationSpan) {
                existingValidationSpan.textContent = '';
                existingValidationSpan.classList.remove('field-validation-error');
                existingValidationSpan.classList.add('field-validation-valid');
            }

            // Remove any inline-validation-error divs
            const errorDiv = formGroup.querySelector('.inline-validation-error');
            if (errorDiv) errorDiv.remove();

            // Reset custom dropdown trigger
            const trigger = formGroup.querySelector('.custom-dropdown-trigger');
            if (trigger) {
                trigger.style.borderColor = '#ced4da';
                trigger.style.backgroundColor = '';
            }

            // Reset PDF zone
            const pdfZone = formGroup.querySelector('.pdf-upload-zone, #pdfUploadZone');
            if (pdfZone) {
                pdfZone.style.borderColor = '';
                pdfZone.style.backgroundColor = '';
            }
        },

        /**
         * Clear all inline errors from the form
         */
        clearAllInlineErrors: function(form) {
            form.querySelectorAll('.form-group.has-error').forEach(fg => {
                this.clearInlineError(fg);
            });
            form.querySelectorAll('.inline-validation-error').forEach(e => e.remove());
            // Also clear any asp-validation-for spans
            form.querySelectorAll('.field-validation-error').forEach(span => {
                span.textContent = '';
                span.classList.remove('field-validation-error');
                span.classList.add('field-validation-valid');
            });
        },

        /**
         * Add input listeners to clear errors when user interacts
         */
        addInputListeners: function(form) {
            const self = this;
            form.querySelectorAll('input, select, textarea').forEach(field => {
                ['input', 'change'].forEach(eventType => {
                    field.addEventListener(eventType, function() {
                        const formGroup = this.closest('.form-group') ||
                                         this.closest('.custom-dropdown-wrapper')?.closest('.form-group');
                        if (formGroup && formGroup.classList.contains('has-error')) {
                            self.clearInlineError(formGroup);
                        }
                    });
                });
            });
        },

        /**
         * Date validation - check expiration is after effective
         */
        initDateValidation: function() {
            const effectiveDate = document.querySelector('input[name="EffectiveDate"]');
            const expirationDate = document.querySelector('input[name="ExpirationDate"]');

            if (!effectiveDate || !expirationDate) return;

            const self = this;

            const validateDates = () => {
                const expirationFormGroup = expirationDate.closest('.form-group');

                if (effectiveDate.value && expirationDate.value) {
                    const effective = new Date(effectiveDate.value);
                    const expiration = new Date(expirationDate.value);

                    if (expiration <= effective) {
                        if (expirationFormGroup) {
                            self.showError(expirationFormGroup, 'Expiration date must be after effective date');
                        }
                    } else {
                        if (expirationFormGroup) {
                            self.clearInlineError(expirationFormGroup);
                        }
                    }
                }
            };

            effectiveDate.addEventListener('change', validateDates);
            expirationDate.addEventListener('change', validateDates);
        },

        /**
         * Handle server-side validation errors on page load
         */
        handleServerValidationErrors: function() {
            const validationSummary = document.querySelector('.alert-danger, .validation-summary-errors');
            if (validationSummary && validationSummary.textContent.trim().length > 0) {
                validationSummary.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
    };

    // Partner Manager - Multi-partner selection for certificates
    const PartnerManager = {
        partners: [],
        selectedPartnerIds: [],

        init: async function() {
            const container = document.getElementById('partner-checkboxes');
            if (!container) return;

            await this.loadPartners();
            this.renderCheckboxes();
            this.loadExistingSelections();
        },

        loadPartners: async function() {
            try {
                const response = await fetch('/Admin/GetPartners', {
                    method: 'GET',
                    headers: { 'Content-Type': 'application/json' }
                });

                const result = await response.json();
                if (result.success && result.data) {
                    // Filter to only active partners
                    this.partners = result.data.filter(p => p.isActive);
                } else {
                    console.error('Failed to load partners:', result.message);
                    this.partners = [];
                }
            } catch (error) {
                console.error('Error loading partners:', error);
                this.partners = [];
            }
        },

        renderCheckboxes: function() {
            const container = document.getElementById('partner-checkboxes');
            if (!container) return;

            // Clear loading message
            container.innerHTML = '';

            if (this.partners.length === 0) {
                container.innerHTML = '<span class="text-muted" style="font-size: 14px;">No partners available</span>';
                return;
            }

            // Create checkbox for each partner
            this.partners.forEach(partner => {
                const checkboxItem = document.createElement('label');
                checkboxItem.className = 'partner-checkbox-item';
                checkboxItem.style.cssText = `
                    display: flex;
                    align-items: center;
                    gap: 6px;
                    background: white;
                    padding: 8px 14px;
                    border-radius: 20px;
                    border: 1px solid #e9ecef;
                    cursor: pointer;
                    transition: all 0.2s;
                    font-size: 14px;
                `;

                const checkbox = document.createElement('input');
                checkbox.type = 'checkbox';
                checkbox.name = 'partner-checkbox';
                checkbox.value = partner.id;
                checkbox.dataset.partnerName = partner.name;
                checkbox.style.accentColor = '#92C83E';
                checkbox.addEventListener('change', () => this.updateSelectedPartners());

                const label = document.createElement('span');
                label.textContent = partner.name;

                checkboxItem.appendChild(checkbox);
                checkboxItem.appendChild(label);

                // Hover effect
                checkboxItem.addEventListener('mouseenter', () => {
                    checkboxItem.style.borderColor = '#92C83E';
                    checkboxItem.style.background = '#f0f7e6';
                });
                checkboxItem.addEventListener('mouseleave', () => {
                    if (!checkbox.checked) {
                        checkboxItem.style.borderColor = '#e9ecef';
                        checkboxItem.style.background = 'white';
                    }
                });

                container.appendChild(checkboxItem);
            });
        },

        loadExistingSelections: function() {
            // For edit mode: check if there are existing partner selections
            // Look for data attribute on the container or hidden field
            const hiddenField = document.getElementById('partner-ids-hidden');
            const existingPartner = document.querySelector('select[name="Partner"]')?.value;

            // Handle legacy single partner field (for backward compatibility)
            if (existingPartner && existingPartner !== '' && existingPartner !== 'None') {
                const checkbox = document.querySelector(`input[data-partner-name="${existingPartner}"]`);
                if (checkbox) {
                    checkbox.checked = true;
                    this.updateSelectedPartners();
                }
            }

            // Handle existing PartnerIds from model (for edit mode)
            if (hiddenField && hiddenField.dataset.existingIds) {
                const ids = hiddenField.dataset.existingIds.split(',').map(id => parseInt(id.trim()));
                ids.forEach(id => {
                    const checkbox = document.querySelector(`input[name="partner-checkbox"][value="${id}"]`);
                    if (checkbox) {
                        checkbox.checked = true;
                    }
                });
                this.updateSelectedPartners();
            }
        },

        updateSelectedPartners: function() {
            const checkboxes = document.querySelectorAll('input[name="partner-checkbox"]:checked');
            this.selectedPartnerIds = Array.from(checkboxes).map(cb => parseInt(cb.value));

            // Update hidden field for form submission
            const hiddenField = document.getElementById('partner-ids-hidden');
            if (hiddenField) {
                hiddenField.value = this.selectedPartnerIds.join(',');
            }

            // Update visual state of checkbox items
            document.querySelectorAll('input[name="partner-checkbox"]').forEach(cb => {
                const item = cb.closest('.partner-checkbox-item');
                if (item) {
                    if (cb.checked) {
                        item.style.borderColor = '#92C83E';
                        item.style.background = '#f0f7e6';
                    } else {
                        item.style.borderColor = '#e9ecef';
                        item.style.background = 'white';
                    }
                }
            });

            // Mark form as changed for unsaved changes tracking
            if (typeof UnsavedChanges !== 'undefined' && UnsavedChanges.markAsChanged) {
                UnsavedChanges.markAsChanged();
            }
        },

        getSelectedPartnerIds: function() {
            return this.selectedPartnerIds;
        }
    };

    // Initialize all components on page load
    window.addEventListener('load', function() {
        ImagePreview.init();
        PdfUpload.init();
        TagManager.init();
        SubcategoryManager.init();
        // PartnerManager.init(); // Commented out - multi-partner UI pending (see todo_pendingChangeRequest.md)
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
        // DEF: Edit.cshtml loads pre-saved tags into window.CertificateCreate.TagManager.
        // Expose the real closure instance so those existing tags land in the SAME arrays
        // removeTag() reads — otherwise the loader built a separate object, removeTag found
        // index -1, and clicking the tag's "×" did nothing (pill stayed, save was a no-op).
        TagManager: TagManager,
        removeTag: TagManager.removeTag.bind(TagManager),
        removeSubcategory: SubcategoryManager.removeSubcategory.bind(SubcategoryManager),
        SubcategoryManager: SubcategoryManager,
        showNotification: ModalSystem.showNotification,
        showConfirmation: ModalSystem.showConfirmation,
        resetUnsavedChanges: UnsavedChanges.reset.bind(UnsavedChanges),
        rebaselineUnsavedChanges: UnsavedChanges.rebaseline.bind(UnsavedChanges),
        markAsChanged: UnsavedChanges.markAsChanged.bind(UnsavedChanges),
        showLoading: LoadingSpinner.show.bind(LoadingSpinner),
        hideLoading: LoadingSpinner.hide.bind(LoadingSpinner),
        getSelectedPartnerIds: PartnerManager.getSelectedPartnerIds.bind(PartnerManager)
    };

})();
