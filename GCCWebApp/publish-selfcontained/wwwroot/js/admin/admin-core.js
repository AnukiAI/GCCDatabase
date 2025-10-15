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
                config.headers['__RequestVerificationToken'] = token;
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
                    event.detail.headers['__RequestVerificationToken'] = token;
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

    showNotification(message, type = 'info', options = {}) {
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