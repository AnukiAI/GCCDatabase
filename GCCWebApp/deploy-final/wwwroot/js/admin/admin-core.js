/* =============================================================================
   GCC Admin Core JavaScript
   Base utilities, AJAX helpers, notifications, and common functionality
   ============================================================================= */

// Admin Core namespace
const AdminCore = {
    // Configuration
    config: {
        baseUrl: window.location.origin,
        apiVersion: 'v1',
        timeout: 30000,
        retries: 3
    },

    // Initialize admin framework
    init() {
        this.setupAjaxDefaults();
        this.initializeNotifications();
        this.bindGlobalEvents();
        this.setupAccessibility();
        console.log('AdminCore initialized');
    },

    // AJAX wrapper with standardized error handling
    ajax(options) {
        const defaults = {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            timeout: this.config.timeout,
            onStart: null,
            onSuccess: null,
            onError: null,
            onComplete: null
        };

        const config = { ...defaults, ...options };

        // Add anti-forgery token for POST/PUT/DELETE requests
        if (['POST', 'PUT', 'DELETE'].includes(config.method.toUpperCase())) {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (token) {
                config.headers['RequestVerificationToken'] = token;
            }
        }

        // Start callback
        if (config.onStart) config.onStart();

        // Create and configure fetch request
        const fetchOptions = {
            method: config.method,
            headers: config.headers,
            signal: AbortSignal.timeout(config.timeout)
        };

        if (config.data && ['POST', 'PUT', 'PATCH'].includes(config.method.toUpperCase())) {
            fetchOptions.body = typeof config.data === 'string' ? config.data : JSON.stringify(config.data);
        }

        return fetch(config.url, fetchOptions)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }
                return response.json();
            })
            .then(data => {
                if (config.onSuccess) config.onSuccess(data);
                return data;
            })
            .catch(error => {
                console.error('AJAX Error:', error);
                const errorMessage = error.name === 'AbortError' ? 'Request timed out' : error.message;
                if (config.onError) {
                    config.onError({ message: errorMessage, error });
                } else {
                    this.showNotification(errorMessage, 'error');
                }
                throw error;
            })
            .finally(() => {
                if (config.onComplete) config.onComplete();
            });
    },

    // Setup default AJAX configurations
    setupAjaxDefaults() {
        // Add CSRF token to all AJAX requests
        const token = document.querySelector('meta[name="__RequestVerificationToken"]')?.content;
        if (token) {
            document.addEventListener('ajaxSend', (event) => {
                if (event.detail?.headers) {
                    event.detail.headers['RequestVerificationToken'] = token;
                }
            });
        }
    },

    // Notification system
    notifications: {
        container: null,
        queue: [],
        maxNotifications: 5
    },

    initializeNotifications() {
        // Create notifications container
        if (!this.notifications.container) {
            this.notifications.container = document.createElement('div');
            this.notifications.container.className = 'admin-notification-stack';
            this.notifications.container.id = 'admin-notifications';
            document.body.appendChild(this.notifications.container);
        }
    },

    async showNotification(message, type = 'info', options = {}) {
        // Route ALL notifications to centered modals for consistency
        const titleMap = {
            'success': 'Success',
            'error': 'Error',
            'warning': 'Warning',
            'info': 'Information'
        };
        const title = options.title || titleMap[type] || 'Information';

        switch (type) {
            case 'success':
                return await this.showSuccessModal({ title, message });
            case 'error':
                return await this.showErrorModal({ title, message });
            case 'warning':
                return await this.showWarningModal({ title, message });
            case 'info':
            default:
                return await this.showInfoModal({ title, message });
        }
    },

    // Legacy toast notification method - kept for backward compatibility if needed
    showToastNotification(message, type = 'info', options = {}) {
        const defaults = {
            title: this.getNotificationTitle(type),
            duration: type === 'error' ? 8000 : 5000,
            closable: true,
            icon: this.getNotificationIcon(type)
        };

        const config = { ...defaults, ...options };

        const notification = this.createNotificationElement(message, type, config);

        // Add to container
        this.notifications.container.appendChild(notification);

        // Trigger show animation
        requestAnimationFrame(() => {
            notification.classList.add('admin-notification--show');
        });

        // Auto-remove after duration
        if (config.duration > 0) {
            setTimeout(() => {
                this.removeNotification(notification);
            }, config.duration);
        }

        // Limit number of notifications
        this.enforceNotificationLimit();

        // Announce to screen readers
        this.announceToScreenReader(`${config.title}: ${message}`);

        return notification;
    },

    createNotificationElement(message, type, config) {
        const notification = document.createElement('div');
        notification.className = `admin-notification admin-notification--${type}`;

        notification.innerHTML = `
            <div class="admin-notification-header">
                <div class="admin-notification-title">
                    <i class="fas ${config.icon}"></i>
                    ${config.title}
                </div>
                ${config.closable ? '<button class="admin-notification-close" aria-label="Close notification">&times;</button>' : ''}
            </div>
            <div class="admin-notification-body">${message}</div>
            ${config.duration > 0 ? '<div class="admin-notification-progress"><div class="admin-notification-progress-bar"></div></div>' : ''}
        `;

        // Add close handler
        if (config.closable) {
            const closeBtn = notification.querySelector('.admin-notification-close');
            closeBtn.addEventListener('click', () => {
                this.removeNotification(notification);
            });
        }

        return notification;
    },

    removeNotification(notification) {
        notification.classList.remove('admin-notification--show');
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    },

    enforceNotificationLimit() {
        const notifications = this.notifications.container.querySelectorAll('.admin-notification');
        if (notifications.length > this.notifications.maxNotifications) {
            const oldest = notifications[0];
            this.removeNotification(oldest);
        }
    },

    getNotificationTitle(type) {
        const titles = {
            success: 'Success',
            error: 'Error',
            warning: 'Warning',
            info: 'Information'
        };
        return titles[type] || 'Notification';
    },

    getNotificationIcon(type) {
        const icons = {
            success: 'fa-check-circle',
            error: 'fa-exclamation-circle',
            warning: 'fa-exclamation-triangle',
            info: 'fa-info-circle'
        };
        return icons[type] || 'fa-bell';
    },

    // Form helpers
    serializeForm(form) {
        const formData = new FormData(form);
        const data = {};

        for (let [key, value] of formData.entries()) {
            if (data[key]) {
                // Handle multiple values (checkboxes, multi-select)
                if (Array.isArray(data[key])) {
                    data[key].push(value);
                } else {
                    data[key] = [data[key], value];
                }
            } else {
                data[key] = value;
            }
        }

        return data;
    },

    validateForm(form) {
        const errors = [];
        const requiredFields = form.querySelectorAll('[required]');

        requiredFields.forEach(field => {
            if (!field.value.trim()) {
                errors.push(`${field.name || field.id || 'Field'} is required`);
                field.classList.add('admin-input--invalid');
            } else {
                field.classList.remove('admin-input--invalid');
            }
        });

        // Email validation
        const emailFields = form.querySelectorAll('input[type="email"]');
        emailFields.forEach(field => {
            if (field.value && !this.isValidEmail(field.value)) {
                errors.push(`${field.name || field.id || 'Email field'} must be a valid email`);
                field.classList.add('admin-input--invalid');
            }
        });

        return errors;
    },

    isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    },

    // Loading states
    showLoading(element, message = 'Loading...') {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }

        if (!element) return;

        element.classList.add('admin-loading');
        element.setAttribute('aria-busy', 'true');

        if (message && !element.querySelector('.admin-loading-text')) {
            const loadingText = document.createElement('span');
            loadingText.className = 'admin-loading-text admin-sr-only';
            loadingText.textContent = message;
            element.appendChild(loadingText);
        }
    },

    hideLoading(element) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }

        if (!element) return;

        element.classList.remove('admin-loading');
        element.setAttribute('aria-busy', 'false');

        const loadingText = element.querySelector('.admin-loading-text');
        if (loadingText) {
            loadingText.remove();
        }
    },

    // Modal helpers
    openModal(modalId, options = {}) {
        const modal = document.getElementById(modalId);
        if (!modal) return;

        modal.classList.add('admin-modal--show');
        modal.setAttribute('aria-hidden', 'false');

        // Focus management
        const focusableElements = modal.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        if (focusableElements.length > 0) {
            focusableElements[0].focus();
        }

        // Close on overlay click
        const overlay = modal.querySelector('.admin-modal-overlay');
        if (overlay) {
            overlay.addEventListener('click', () => this.closeModal(modalId));
        }

        // Close on escape key
        const escapeHandler = (e) => {
            if (e.key === 'Escape') {
                this.closeModal(modalId);
                document.removeEventListener('keydown', escapeHandler);
            }
        };
        document.addEventListener('keydown', escapeHandler);
    },

    closeModal(modalId) {
        const modal = document.getElementById(modalId);
        if (!modal) return;

        modal.classList.remove('admin-modal--show');
        modal.setAttribute('aria-hidden', 'true');

        // Return focus to trigger element if available
        const trigger = document.querySelector(`[data-modal="${modalId}"]`);
        if (trigger) {
            trigger.focus();
        }
    },

    /**
     * Show a confirmation modal dialog
     * @param {Object} options - Modal configuration options
     * @param {string} options.title - Modal title
     * @param {string} options.message - Modal message/description
     * @param {string} options.confirmText - Confirm button text (default: "Confirm")
     * @param {string} options.cancelText - Cancel button text (default: "Cancel")
     * @param {string} options.type - Modal type: 'danger', 'warning', 'info' (default: 'danger')
     * @param {Function} options.onConfirm - Callback when confirmed
     * @param {Function} options.onCancel - Callback when cancelled
     * @returns {Promise} Resolves with true if confirmed, false if cancelled
     */
    showConfirmation(options = {}) {
        const defaults = {
            title: 'Confirm Action',
            message: 'Are you sure you want to proceed?',
            confirmText: 'Confirm',
            cancelText: 'Cancel',
            type: 'danger', // 'danger', 'warning', 'info'
            allowHtml: false, // Allow HTML in message (use with caution - only for trusted content)
            onConfirm: null,
            onCancel: null
        };

        const config = { ...defaults, ...options };

        // Process message - allow HTML if explicitly enabled, otherwise escape
        const messageHtml = config.allowHtml ? config.message : this.escapeHtml(config.message);

        return new Promise((resolve) => {
            // Create modal overlay
            const overlay = document.createElement('div');
            overlay.className = 'admin-modal-overlay gcc-modal-overlay';
            overlay.setAttribute('role', 'dialog');
            overlay.setAttribute('aria-modal', 'true');
            overlay.setAttribute('aria-labelledby', 'modal-title');
            overlay.setAttribute('aria-describedby', 'modal-message');

            // Create modal HTML - Delete button on LEFT (red), Cancel button on RIGHT (green)
            overlay.innerHTML = `
                <div class="admin-modal-dialog gcc-modal-dialog">
                    <div class="gcc-modal-content">
                        <h2 id="modal-title" class="gcc-modal-title">${this.escapeHtml(config.title)}</h2>
                        <p id="modal-message" class="gcc-modal-message">${messageHtml}</p>
                        <div class="gcc-modal-buttons">
                            <button class="gcc-modal-btn gcc-modal-btn--danger" data-action="confirm">
                                ${this.escapeHtml(config.confirmText)}
                            </button>
                            <button class="gcc-modal-btn gcc-modal-btn--success" data-action="cancel">
                                ${this.escapeHtml(config.cancelText)}
                            </button>
                        </div>
                    </div>
                </div>
            `;

            // Add to document
            document.body.appendChild(overlay);

            // Handle confirmation
            const handleConfirm = () => {
                cleanup();
                if (config.onConfirm) config.onConfirm();
                resolve(true);
            };

            // Handle cancellation
            const handleCancel = () => {
                cleanup();
                if (config.onCancel) config.onCancel();
                resolve(false);
            };

            // Cleanup function
            const cleanup = () => {
                overlay.classList.remove('show');
                setTimeout(() => {
                    if (overlay.parentNode) {
                        overlay.parentNode.removeChild(overlay);
                    }
                }, 300);
            };

            // Bind events
            const confirmBtn = overlay.querySelector('[data-action="confirm"]');
            const cancelBtn = overlay.querySelector('[data-action="cancel"]');

            confirmBtn.addEventListener('click', handleConfirm);
            cancelBtn.addEventListener('click', handleCancel);

            // Close on overlay click
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) {
                    handleCancel();
                }
            });

            // Handle keyboard events
            const keyHandler = (e) => {
                if (e.key === 'Escape') {
                    handleCancel();
                    document.removeEventListener('keydown', keyHandler);
                } else if (e.key === 'Enter' && e.target.tagName !== 'BUTTON') {
                    handleConfirm();
                    document.removeEventListener('keydown', keyHandler);
                }
            };
            document.addEventListener('keydown', keyHandler);

            // Show modal with animation
            requestAnimationFrame(() => {
                overlay.classList.add('show');
                cancelBtn.focus(); // Focus on Cancel (safe option)
            });

            // Announce to screen readers
            this.announceToScreenReader(`${config.title}. ${config.message}`);
        });
    },

    /**
     * Show a success modal (for delete/update confirmations)
     * @param {Object} options - Modal configuration options
     * @param {string} options.title - Modal title (e.g., "Delete Successful", "Update Successful")
     * @param {string} options.message - Modal message
     * @param {string} options.buttonText - Button text (default: "OK")
     * @returns {Promise} Resolves when user clicks OK
     */
    showSuccessModal(options = {}) {
        const defaults = {
            title: 'Success',
            message: 'Operation completed successfully.',
            buttonText: 'OK'
        };

        const config = { ...defaults, ...options };

        return this._showAlertModal(config, 'success');
    },

    /**
     * Show an error modal (for operation failures)
     * @param {Object} options - Modal configuration options
     * @param {string} options.title - Modal title (default: "Error")
     * @param {string} options.message - Modal message
     * @param {string} options.buttonText - Button text (default: "OK")
     * @returns {Promise} Resolves when user clicks OK
     */
    showErrorModal(options = {}) {
        const defaults = {
            title: 'Error',
            message: 'An error occurred.',
            buttonText: 'OK'
        };

        const config = { ...defaults, ...options };

        return this._showAlertModal(config, 'error');
    },

    /**
     * Show a warning modal (for caution messages)
     * @param {Object} options - Modal configuration options
     * @param {string} options.title - Modal title (default: "Warning")
     * @param {string} options.message - Modal message
     * @param {string} options.buttonText - Button text (default: "OK")
     * @returns {Promise} Resolves when user clicks OK
     */
    showWarningModal(options = {}) {
        const defaults = {
            title: 'Warning',
            message: 'Please review this information.',
            buttonText: 'OK'
        };

        const config = { ...defaults, ...options };

        return this._showAlertModal(config, 'warning');
    },

    /**
     * Show an info modal (for informational messages)
     * @param {Object} options - Modal configuration options
     * @param {string} options.title - Modal title (default: "Information")
     * @param {string} options.message - Modal message
     * @param {string} options.buttonText - Button text (default: "OK")
     * @returns {Promise} Resolves when user clicks OK
     */
    showInfoModal(options = {}) {
        const defaults = {
            title: 'Information',
            message: '',
            buttonText: 'OK'
        };

        const config = { ...defaults, ...options };

        return this._showAlertModal(config, 'info');
    },

    /**
     * Internal method to show alert modals with different types
     * @private
     */
    _showAlertModal(config, type) {
        return new Promise((resolve) => {
            // Create modal overlay with inline styles as failsafe
            const overlay = document.createElement('div');
            overlay.className = 'admin-modal-overlay gcc-modal-overlay';
            overlay.setAttribute('role', 'alertdialog');
            overlay.setAttribute('aria-modal', 'true');
            overlay.setAttribute('aria-labelledby', `${type}-modal-title`);
            overlay.setAttribute('aria-describedby', `${type}-modal-message`);

            // Inline styles ensure modal works even if CSS fails to load
            overlay.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background-color: rgba(0, 0, 0, 0.85);
                backdrop-filter: blur(3px);
                z-index: 9999;
                display: flex;
                align-items: center;
                justify-content: center;
                opacity: 0;
                transition: opacity 0.3s ease;
            `;

            // Determine button colors based on type
            const buttonColors = {
                success: '#92C83E',
                error: '#FF5252',
                warning: '#FFC107',
                info: '#002B76'
            };
            const titleColors = {
                success: '#002B76',
                error: '#FF5252',
                warning: '#FFA000',
                info: '#002B76'
            };

            const buttonColor = buttonColors[type] || '#92C83E';
            const titleColor = titleColors[type] || '#002B76';
            const buttonTextColor = type === 'warning' ? '#333333' : '#ffffff';

            // Create modal HTML with inline styles
            overlay.innerHTML = `
                <div class="admin-modal-dialog gcc-modal-dialog" style="position: relative; z-index: 10000; max-width: 450px; width: 90%; margin: auto;">
                    <div class="gcc-modal-content gcc-modal-content--${type}" style="background-color: #ffffff; border-radius: 8px; box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5); padding: 30px; text-align: center;">
                        <h2 id="${type}-modal-title" class="gcc-modal-title gcc-modal-title--${type}" style="color: ${titleColor}; font-size: 22px; font-weight: 600; margin: 0 0 15px 0;">${this.escapeHtml(config.title)}</h2>
                        <p id="${type}-modal-message" class="gcc-modal-message" style="color: #333333; font-size: 16px; line-height: 1.5; margin: 0 0 25px 0;">${this.escapeHtml(config.message)}</p>
                        <div class="gcc-modal-buttons gcc-modal-buttons--centered" style="display: flex; gap: 15px; justify-content: center;">
                            <button class="gcc-modal-btn gcc-modal-btn--full" data-action="ok" style="width: 100%; padding: 12px 30px; border: none; border-radius: 4px; font-size: 16px; font-weight: 500; cursor: pointer; background-color: ${buttonColor}; color: ${buttonTextColor};">
                                ${this.escapeHtml(config.buttonText)}
                            </button>
                        </div>
                    </div>
                </div>
            `;

            // Add to document
            document.body.appendChild(overlay);

            // Handle OK click
            const handleOk = () => {
                overlay.classList.remove('show');
                setTimeout(() => {
                    if (overlay.parentNode) {
                        overlay.parentNode.removeChild(overlay);
                    }
                }, 300);
                document.removeEventListener('keydown', keyHandler);
                resolve(true);
            };

            // Bind events
            const okBtn = overlay.querySelector('[data-action="ok"]');
            okBtn.addEventListener('click', handleOk);

            // Handle keyboard events
            const keyHandler = (e) => {
                if (e.key === 'Escape' || e.key === 'Enter') {
                    handleOk();
                }
            };
            document.addEventListener('keydown', keyHandler);

            // Show modal with animation
            requestAnimationFrame(() => {
                overlay.classList.add('show');
                overlay.style.opacity = '1'; // Inline style failsafe
                okBtn.focus();
            });

            // Announce to screen readers
            this.announceToScreenReader(`${config.title}. ${config.message}`);
        });
    },

    /**
     * Show a progress modal for long-running operations
     * @param {Object} options - Progress modal configuration
     * @param {string} options.title - Modal title
     * @param {string} options.message - Modal message
     * @param {boolean} options.indeterminate - Show indeterminate progress (default: false)
     * @param {string} options.cancelText - Cancel button text (default: "Cancel")
     * @param {Function} options.onCancel - Callback when cancelled
     * @returns {Object} Progress modal controller with update() and close() methods
     */
    showProgress(options = {}) {
        const defaults = {
            title: 'Processing',
            message: 'Please wait...',
            indeterminate: false,
            cancelText: 'Cancel',
            onCancel: null
        };

        const config = { ...defaults, ...options };

        // Create modal overlay
        const overlay = document.createElement('div');
        overlay.className = 'admin-modal-overlay';
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        overlay.setAttribute('aria-labelledby', 'progress-modal-title');
        overlay.setAttribute('aria-live', 'polite');

        const progressFillClass = config.indeterminate ? 'indeterminate' : '';

        // Create modal HTML
        overlay.innerHTML = `
            <div class="admin-modal-dialog">
                <div class="admin-modal-header">
                    <h2 id="progress-modal-title" class="admin-modal-title">${this.escapeHtml(config.title)}</h2>
                </div>
                <div class="admin-modal-body">
                    <p class="admin-modal-message">${this.escapeHtml(config.message)}</p>
                    <div class="admin-modal-progress">
                        <div class="admin-modal-progress-bar">
                            <div class="admin-modal-progress-fill ${progressFillClass}" style="width: 0%"></div>
                        </div>
                        <p class="admin-modal-progress-text">0%</p>
                    </div>
                </div>
                <div class="admin-modal-footer admin-modal-footer--centered">
                    <button class="admin-modal-btn admin-modal-btn--success admin-modal-btn--full" data-action="cancel">
                        ${this.escapeHtml(config.cancelText)}
                    </button>
                </div>
            </div>
        `;

        // Add to document
        document.body.appendChild(overlay);

        // Handle cancellation
        const handleCancel = () => {
            overlay.classList.remove('show');
            setTimeout(() => {
                if (overlay.parentNode) {
                    overlay.parentNode.removeChild(overlay);
                }
            }, 300);
            if (config.onCancel) config.onCancel();
        };

        // Bind cancel button
        const cancelBtn = overlay.querySelector('[data-action="cancel"]');
        cancelBtn.addEventListener('click', handleCancel);

        // Handle keyboard events
        const keyHandler = (e) => {
            if (e.key === 'Escape') {
                handleCancel();
                document.removeEventListener('keydown', keyHandler);
            }
        };
        document.addEventListener('keydown', keyHandler);

        // Show modal with animation
        requestAnimationFrame(() => {
            overlay.classList.add('show');
        });

        // Return controller object
        return {
            /**
             * Update progress bar
             * @param {number} percent - Progress percentage (0-100)
             * @param {string} message - Optional status message
             */
            update(percent, message) {
                const progressFill = overlay.querySelector('.admin-modal-progress-fill');
                const progressText = overlay.querySelector('.admin-modal-progress-text');
                const messageEl = overlay.querySelector('.admin-modal-message');

                if (progressFill && !config.indeterminate) {
                    progressFill.style.width = `${Math.min(100, Math.max(0, percent))}%`;
                }

                if (progressText && !config.indeterminate) {
                    progressText.textContent = `${Math.round(percent)}%`;
                }

                if (message && messageEl) {
                    messageEl.textContent = message;
                }

                // Announce progress to screen readers
                if (percent % 25 === 0) {
                    this.announceToScreenReader(`Progress: ${percent}%`);
                }
            },

            /**
             * Close the progress modal
             */
            close() {
                handleCancel();
            },

            /**
             * Get the overlay element
             */
            getElement() {
                return overlay;
            }
        };
    },

    /**
     * Escape HTML to prevent XSS
     * @param {string} text - Text to escape
     * @returns {string} Escaped HTML
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    // Accessibility helpers
    setupAccessibility() {
        // Add ARIA labels where missing
        document.querySelectorAll('input[type="search"]').forEach(input => {
            if (!input.getAttribute('aria-label')) {
                input.setAttribute('aria-label', 'Search');
            }
        });

        // Add role attributes to tables
        document.querySelectorAll('.admin-table').forEach(table => {
            if (!table.getAttribute('role')) {
                table.setAttribute('role', 'table');
            }
        });

        // Announce dynamic content changes
        this.createLiveRegion();
    },

    createLiveRegion() {
        if (!document.getElementById('admin-live-region')) {
            const liveRegion = document.createElement('div');
            liveRegion.id = 'admin-live-region';
            liveRegion.className = 'admin-sr-only';
            liveRegion.setAttribute('aria-live', 'polite');
            liveRegion.setAttribute('aria-atomic', 'true');
            document.body.appendChild(liveRegion);
        }
    },

    announceToScreenReader(message) {
        const liveRegion = document.getElementById('admin-live-region');
        if (liveRegion) {
            liveRegion.textContent = message;
            // Clear after announcement
            setTimeout(() => {
                liveRegion.textContent = '';
            }, 1000);
        }
    },

    // Global event handlers
    bindGlobalEvents() {
        // Handle modal close buttons
        document.addEventListener('click', (e) => {
            if (e.target.matches('.admin-modal-close') || e.target.closest('.admin-modal-close')) {
                const modal = e.target.closest('.admin-modal');
                if (modal) {
                    this.closeModal(modal.id);
                }
            }
        });

        // Handle notification close buttons
        document.addEventListener('click', (e) => {
            if (e.target.matches('.admin-notification-close')) {
                const notification = e.target.closest('.admin-notification');
                if (notification) {
                    this.removeNotification(notification);
                }
            }
        });

        // Handle form validation on submit
        document.addEventListener('submit', (e) => {
            const form = e.target;
            if (form.hasAttribute('data-validate')) {
                const errors = this.validateForm(form);
                if (errors.length > 0) {
                    e.preventDefault();
                    this.showNotification(errors.join('<br>'), 'error');
                }
            }
        });
    },

    // Utility functions
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    throttle(func, limit) {
        let inThrottle;
        return function(...args) {
            if (!inThrottle) {
                func.apply(this, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    },

    formatDate(date, options = {}) {
        const defaults = {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        };
        return new Intl.DateTimeFormat('en-US', { ...defaults, ...options }).format(new Date(date));
    },

    formatNumber(number, options = {}) {
        return new Intl.NumberFormat('en-US', options).format(number);
    },

    // Storage helpers
    storage: {
        set(key, value, session = false) {
            const storage = session ? sessionStorage : localStorage;
            try {
                storage.setItem(`gcc_admin_${key}`, JSON.stringify(value));
            } catch (e) {
                console.warn('Storage not available:', e);
            }
        },

        get(key, session = false) {
            const storage = session ? sessionStorage : localStorage;
            try {
                const item = storage.getItem(`gcc_admin_${key}`);
                return item ? JSON.parse(item) : null;
            } catch (e) {
                console.warn('Storage not available:', e);
                return null;
            }
        },

        remove(key, session = false) {
            const storage = session ? sessionStorage : localStorage;
            try {
                storage.removeItem(`gcc_admin_${key}`);
            } catch (e) {
                console.warn('Storage not available:', e);
            }
        }
    }
};

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    AdminCore.init();
});

// Export for ES6 modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdminCore;
}