/**
 * main.js - Unified Master JavaScript for GreenCircle MVC Application
 * Contains all master layout functionality and role management
 * Version: Complete with Mobile Submenu Support - FIXED for <480px
 */

document.addEventListener('DOMContentLoaded', function () {
    console.log('DOM loaded, initializing...');

    // Initialize theme
    initializeTheme();

    // Initialize CSRF protection
    initializeCSRFProtection();

    // Initialize all master functionality
    initializeUserDropdown();
    initializeMobileMenu();
    initializeMobileSubmenus();
    initializeSearchFunctionality();
    initializeNavigationHighlighting();

    console.log('All initialization complete');
});

/**
 * Theme Initialization
 */
function initializeTheme() {
    // Initialize with Option 4 theme - ensure it's always applied
    document.body.classList.add('theme-option4');
}

/**
 * CSRF Protection for AJAX Requests
 */
function initializeCSRFProtection() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (tokenInput) {
        const tokenValue = tokenInput.value;
        
        // Set default headers for fetch requests
        const originalFetch = window.fetch;
        window.fetch = function(url, options = {}) {
            if (!options.headers) {
                options.headers = {};
            }
            
            // Add CSRF token for state-changing requests
            const method = (options.method || 'GET').toUpperCase();
            if (['POST', 'PUT', 'DELETE', 'PATCH'].includes(method)) {
                options.headers['X-CSRF-TOKEN'] = tokenValue;
                options.headers['RequestVerificationToken'] = tokenValue;
            }
            
            return originalFetch.call(this, url, options);
        };

        // Set up for jQuery if available
        if (window.$ && window.$.ajaxSetup) {
            $.ajaxSetup({
                beforeSend: function(xhr, settings) {
                    if (settings.type && ['POST', 'PUT', 'DELETE', 'PATCH'].includes(settings.type.toUpperCase())) {
                        xhr.setRequestHeader('X-CSRF-TOKEN', tokenValue);
                        xhr.setRequestHeader('RequestVerificationToken', tokenValue);
                    }
                }
            });
        }
        
        console.log('CSRF protection initialized');
    }
}

/**
 * User Dropdown Management
 */
function initializeUserDropdown() {
    const userProfile = document.querySelector('.user-profile');
    const userDropdown = document.querySelector('.user-dropdown');

    if (userProfile && userDropdown) {
        userProfile.addEventListener('click', function (e) {
            e.stopPropagation();
            userDropdown.classList.toggle('open');
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', function (e) {
            if (!userProfile.contains(e.target)) {
                userDropdown.classList.remove('open');
            }
        });
    }
}

/**
 * Mobile Menu Management - Enhanced with Submenu Support
 */
function initializeMobileMenu() {
    const mobileToggle = document.querySelector('.mobile-toggle');
    const navLinks = document.querySelector('.nav-links');
    const mobileClose = document.querySelector('.mobile-nav-close');

    if (mobileToggle && navLinks) {
        console.log('Mobile menu elements found');

        mobileToggle.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            console.log('Mobile toggle clicked');

            const isOpen = navLinks.classList.contains('mobile-open');

            if (isOpen) {
                navLinks.classList.remove('mobile-open');
                mobileToggle.classList.remove('active');
                document.body.style.overflow = '';
            } else {
                navLinks.classList.add('mobile-open');
                mobileToggle.classList.add('active');
                document.body.style.overflow = 'hidden';
            }

            console.log('Nav links classes:', navLinks.className);
        });

        // Close button functionality
        if (mobileClose) {
            mobileClose.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                navLinks.classList.remove('mobile-open');
                mobileToggle.classList.remove('active');
                document.body.style.overflow = '';
            });
        }

        // Close menu when clicking outside
        document.addEventListener('click', function (e) {
            if (!mobileToggle.contains(e.target) && !navLinks.contains(e.target)) {
                navLinks.classList.remove('mobile-open');
                mobileToggle.classList.remove('active');
                document.body.style.overflow = '';
            }
        });

        // Close menu when window is resized to desktop - FIXED BREAKPOINT
        window.addEventListener('resize', function () {
            if (window.innerWidth > 1024) {
                navLinks.classList.remove('mobile-open');
                mobileToggle.classList.remove('active');
                document.body.style.overflow = '';

                // Reset all mobile states
                document.querySelectorAll('.mobile-open').forEach(element => {
                    element.classList.remove('mobile-open');
                });
            } else {
                // Reinitialize mobile submenus when resizing
                setTimeout(initializeMobileSubmenus, 100);
            }
        });

        // Close menu on Escape key
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && navLinks.classList.contains('mobile-open')) {
                navLinks.classList.remove('mobile-open');
                mobileToggle.classList.remove('active');
                document.body.style.overflow = '';
            }
        });
    } else {
        console.log('Mobile menu elements not found');
    }
}

/**
 * Mobile Submenu Management - COMPLETELY REWRITTEN for proper <480px support
 */
function initializeMobileSubmenus() {
    console.log('Initializing mobile submenus...');

    // Only run on mobile/tablet devices
    if (window.innerWidth <= 1024) {
        console.log('Mobile/tablet detected, setting up submenus');

        // Remove any existing event listeners by cloning nodes
        const navItems = document.querySelectorAll('.nav-item');

        navItems.forEach(navItem => {
            const submenu = navItem.querySelector('.submenu');
            const navLink = navItem.querySelector('.nav-link');
            const dropdownIcon = navItem.querySelector('.dropdown-icon');

            if (submenu && navLink && dropdownIcon) {
                console.log('Setting up submenu for:', navLink.textContent.trim());

                // Clone to remove existing listeners
                const newNavLink = navLink.cloneNode(true);
                navLink.parentNode.replaceChild(newNavLink, navLink);

                // Add new click handler for main nav items
                newNavLink.addEventListener('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    console.log('Main nav item clicked:', this.textContent.trim());

                    // Close other nav items
                    navItems.forEach(otherItem => {
                        if (otherItem !== navItem) {
                            otherItem.classList.remove('mobile-open');
                            const otherIcon = otherItem.querySelector('.dropdown-icon');
                            if (otherIcon) {
                                otherIcon.style.transform = 'rotate(0deg)';
                            }
                        }
                    });

                    // Toggle current nav item
                    const isOpen = navItem.classList.contains('mobile-open');
                    if (isOpen) {
                        navItem.classList.remove('mobile-open');
                        dropdownIcon.style.transform = 'rotate(0deg)';
                        console.log('Closed submenu');
                    } else {
                        navItem.classList.add('mobile-open');
                        dropdownIcon.style.transform = 'rotate(180deg)';
                        console.log('Opened submenu');
                    }
                });
            }

            // Handle nested submenu toggles
            const submenuToggles = navItem.querySelectorAll('.submenu-toggle');
            submenuToggles.forEach(toggle => {
                // Clone to remove existing listeners
                const newToggle = toggle.cloneNode(true);
                toggle.parentNode.replaceChild(newToggle, toggle);

                newToggle.addEventListener('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    console.log('Submenu toggle clicked:', this.textContent.trim());

                    const nestedSubmenu = this.nextElementSibling;
                    const toggleIcon = this.querySelector('.toggle-icon');

                    if (nestedSubmenu && nestedSubmenu.classList.contains('nested-submenu')) {
                        // Close other nested submenus in same parent
                        const parentSubmenu = this.closest('.submenu');
                        parentSubmenu.querySelectorAll('.submenu-toggle').forEach(otherToggle => {
                            if (otherToggle !== this) {
                                otherToggle.classList.remove('mobile-open');
                                const otherNested = otherToggle.nextElementSibling;
                                if (otherNested) {
                                    otherNested.classList.remove('mobile-open');
                                }
                                const otherIcon = otherToggle.querySelector('.toggle-icon');
                                if (otherIcon) {
                                    otherIcon.style.transform = 'rotate(0deg)';
                                }
                            }
                        });

                        // Toggle current nested submenu
                        const isNestedOpen = this.classList.contains('mobile-open');
                        if (isNestedOpen) {
                            this.classList.remove('mobile-open');
                            nestedSubmenu.classList.remove('mobile-open');
                            if (toggleIcon) {
                                toggleIcon.style.transform = 'rotate(0deg)';
                            }
                            console.log('Closed nested submenu');
                        } else {
                            this.classList.add('mobile-open');
                            nestedSubmenu.classList.add('mobile-open');
                            if (toggleIcon) {
                                toggleIcon.style.transform = 'rotate(90deg)';
                            }
                            console.log('Opened nested submenu');
                        }
                    }
                });
            });
        });

        console.log('Mobile submenus initialized');
    } else {
        console.log('Desktop mode - cleaning up mobile states');
        // Clean up mobile classes when on desktop
        document.querySelectorAll('.mobile-open').forEach(element => {
            element.classList.remove('mobile-open');
        });

        // Reset all transforms
        document.querySelectorAll('.dropdown-icon, .toggle-icon').forEach(icon => {
            icon.style.transform = '';
        });
    }
}


/**
 * Search Functionality
 */
function initializeSearchFunctionality() {
    const searchInput = document.querySelector('.search-bar input');

    // Skip on Certificate tile view pages - they have their own search handling
    const isTileViewPage = window.location.pathname.toLowerCase().includes('/certificate/certificatetileview');
    if (isTileViewPage) {
        return;
    }

    if (searchInput) {
        searchInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                performSearch(this.value);
            }
        });

        // Optional: Add search button functionality
        const searchIcon = document.querySelector('.search-bar i');
        if (searchIcon) {
            searchIcon.addEventListener('click', function () {
                performSearch(searchInput.value);
            });
        }
    }
}

/**
 * Perform search action
 */
function performSearch(query) {
    if (query.trim()) {
        // Redirect to public certificate tile view with search term
        window.location.href = '/Certificate/CertificateTileView?search=' + encodeURIComponent(query);
    }
}

/**
 * Navigation Highlighting
 */
function initializeNavigationHighlighting() {
    // Get current controller and action from URL or data attributes
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.nav-link[href], .nav-link[data-controller]');

    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        const controller = link.getAttribute('data-controller');

        if (href && currentPath.includes(href.toLowerCase())) {
            link.classList.add('active');
        } else if (controller && currentPath.includes('/' + controller.toLowerCase())) {
            link.classList.add('active');
        } else {
            link.classList.remove('active');
        }
    });
}

/**
 * Toast Notification System
 */
function showToast(message, type = 'info', duration = 3000) {
    // Route through AdminCore for consistent centered modals
    if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
        AdminCore.showNotification(message, type);
        return;
    }
    // Minimal fallback if AdminCore not available
    console.warn(`[showToast] ${type}: ${message}`);
}

/**
 * Utility Functions
 */

// Debounce function for search and other inputs
function debounce(func, wait, immediate) {
    let timeout;
    return function executedFunction() {
        const context = this;
        const args = arguments;

        const later = function () {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };

        const callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);

        if (callNow) func.apply(context, args);
    };
}

// Format date utility
function formatDate(date, format = 'MM/DD/YYYY') {
    const d = new Date(date);
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    const year = d.getFullYear();

    switch (format) {
        case 'MM/DD/YYYY':
            return `${month}/${day}/${year}`;
        case 'DD/MM/YYYY':
            return `${day}/${month}/${year}`;
        case 'YYYY-MM-DD':
            return `${year}-${month}-${day}`;
        default:
            return d.toLocaleDateString();
    }
}

// AJAX helper for MVC actions
function makeRequest(url, method = 'GET', data = null) {
    return fetch(url, {
        method: method,
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]')?.value || ''
        },
        body: data ? JSON.stringify(data) : null
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .catch(error => {
            console.error('Request failed:', error);
            showToast('An error occurred. Please try again.', 'error');
            throw error;
        });
}

/**
 * Event Listeners for Dynamic Content
 */


// Handle window resize events - IMPROVED
window.addEventListener('resize', debounce(function () {
    console.log('Window resized to:', window.innerWidth);

    // Close mobile menu if resized to desktop
    if (window.innerWidth > 1024) {
        const navLinks = document.querySelector('.nav-links');
        const mobileToggle = document.querySelector('.mobile-toggle');

        if (navLinks && navLinks.classList.contains('mobile-open')) {
            navLinks.classList.remove('mobile-open');
            document.body.style.overflow = '';
        }

        if (mobileToggle && mobileToggle.classList.contains('active')) {
            mobileToggle.classList.remove('active');
        }

        // Reset all mobile submenu states
        document.querySelectorAll('.mobile-open').forEach(el => {
            el.classList.remove('mobile-open');
        });

        // Reset all icon transforms
        document.querySelectorAll('.dropdown-icon, .toggle-icon').forEach(icon => {
            icon.style.transform = '';
        });
    } else {
        // Reinitialize mobile submenus when in mobile/tablet mode
        setTimeout(initializeMobileSubmenus, 100);
    }
}, 250));

// Handle orientation change specifically for mobile devices
window.addEventListener('orientationchange', function () {
    setTimeout(() => {
        console.log('Orientation changed, reinitializing mobile submenus');
        if (window.innerWidth <= 1024) {
            initializeMobileSubmenus();
        }
    }, 300);
});

// User role management functions
function changeUserRole(role) {
    localStorage.setItem('userRole', role);
    console.log('User role changed to:', role);
    // Optionally reload the page or update UI based on new role
    if (window.location.pathname.includes('/Admin/') || window.location.pathname.includes('/Dashboard/')) {
        window.location.reload();
    }
}

function getCurrentRole() {
    return localStorage.getItem('userRole') || 'guest';
}

// Global exports for use in individual pages
window.GreenCircle = {
    showToast,
    debounce,
    formatDate,
    makeRequest,
    changeUserRole,
    getCurrentRole,
    initializeMobileSubmenus
};

//login

// Add this function before the window.GreenCircle export
async function logout() {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Sign Out',
        message: 'Are you sure you want to sign out?',
        confirmText: 'Sign Out',
        cancelText: 'Stay Signed In',
        type: 'warning'
    });

    if (confirmed) {
        // Clear any stored auth data
        localStorage.removeItem('userRole');
        localStorage.removeItem('authToken');

        // Show loading toast
        AdminCore.showNotification('Signing out...', 'info');

        // Redirect to login after brief delay
        setTimeout(() => {
            window.location.href = '/Account/Login';
        }, 1000);
    }
}

// Update the global exports section
window.GreenCircle = {
    showToast,
    debounce,
    formatDate,
    makeRequest,
    initializeMobileSubmenus,
    logout
};

/**
* Toggle Review Instructions Panel
*/
function toggleInstructions() {
    const content = document.getElementById('instructionsContent');
    const button = document.querySelector('.toggle-instructions i');

    if (content && button) {
        content.classList.toggle('collapsed');

        if (content.classList.contains('collapsed')) {
            button.classList.remove('fa-chevron-down');
            button.classList.add('fa-chevron-up');
        } else {
            button.classList.remove('fa-chevron-up');
            button.classList.add('fa-chevron-down');
        }
    }
}

/**
 * Initialize Review Instructions (call this on page load)
 */
function initializeReviewInstructions() {
    // Auto-collapse on mobile
    if (window.innerWidth <= 768) {
        const content = document.getElementById('instructionsContent');
        if (content) {
            content.classList.add('collapsed');
            const button = document.querySelector('.toggle-instructions i');
            if (button) {
                button.classList.remove('fa-chevron-down');
                button.classList.add('fa-chevron-up');
            }
        }
    }
}