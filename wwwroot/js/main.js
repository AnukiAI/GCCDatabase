/**
 * main.js - Unified Master JavaScript for GreenCircle MVC Application
 * Contains all master layout functionality and role management
 */

document.addEventListener('DOMContentLoaded', function () {
    // Initialize all master functionality
    initializeUserDropdown();
    initializeMobileMenu();
    initializeRoleSystem();
    initializeSearchFunctionality();
    initializeNavigationHighlighting();
});

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
 * Mobile Menu Management
 */
function initializeMobileMenu() {
    const mobileToggle = document.querySelector('.mobile-toggle');
    const navLinks = document.querySelector('.nav-links');

    if (mobileToggle && navLinks) {
        mobileToggle.addEventListener('click', function () {
            navLinks.classList.toggle('mobile-open');
        });

        // Handle mobile submenu toggles
        const navItems = document.querySelectorAll('.nav-link');
        navItems.forEach(item => {
            item.addEventListener('click', function (e) {
                if (window.innerWidth <= 768 && this.nextElementSibling && this.nextElementSibling.classList.contains('submenu')) {
                    e.preventDefault();
                    this.nextElementSibling.classList.toggle('mobile-open');
                }
            });
        });

        // Handle nested submenu toggles
        const submenuToggles = document.querySelectorAll('.submenu-toggle');
        submenuToggles.forEach(toggle => {
            toggle.addEventListener('click', function (e) {
                if (window.innerWidth <= 768) {
                    e.preventDefault();
                    this.nextElementSibling.classList.toggle('mobile-open');
                }
            });
        });
    }
}

/**
 * Role-Based Visibility System
 */
function initializeRoleSystem() {
    const roleToggle = document.getElementById('role-toggle');
    const body = document.body;

    if (roleToggle) {
        roleToggle.addEventListener('change', function () {
            changeUserRole(this.value);
        });

        // Initialize with current role
        const currentRole = getCurrentRole();
        if (currentRole) {
            roleToggle.value = currentRole;
            changeUserRole(currentRole);
        }
    }
}

/**
 * Change user role and update UI visibility
 */
function changeUserRole(role) {
    const body = document.body;
    const validRoles = ['public', 'external', 'analyst', 'admin'];

    if (!validRoles.includes(role)) {
        console.error('Invalid role:', role);
        return;
    }

    // Remove all role classes
    validRoles.forEach(r => {
        body.classList.remove('role-' + r);
    });

    // Add the selected role class
    body.classList.add('role-' + role);

    // Update role badge if it exists
    const roleBadge = document.querySelector('.role-badge');
    if (roleBadge) {
        roleBadge.textContent = role.charAt(0).toUpperCase() + role.slice(1);
    }

    // Update visibility for all role elements
    updateRoleElementsVisibility();

    // Store role preference
    localStorage.setItem('userRole', role);

    console.log('Role changed to:', role);
}

/**
 * Get current user role
 */
function getCurrentRole() {
    // Check body class first
    const body = document.body;
    const classes = body.className.split(' ');

    for (let className of classes) {
        if (className.startsWith('role-')) {
            return className.replace('role-', '');
        }
    }

    // Fallback to localStorage
    return localStorage.getItem('userRole') || 'admin';
}

/**
 * Update visibility of all role-based elements
 */
function updateRoleElementsVisibility() {
    const currentRole = getCurrentRole();

    // Hide all role elements first
    document.querySelectorAll('.role-element').forEach(element => {
        element.style.display = 'none';
    });

    // Show elements for current role
    const roleSelector = '.role-' + currentRole + '-show';
    document.querySelectorAll(roleSelector).forEach(element => {
        // Default display
        element.style.display = '';

        // Special handling for different element types
        if (element.classList.contains('btn')) {
            element.style.display = 'flex';
        } else if (element.classList.contains('nav-item')) {
            element.style.display = 'list-item';
        } else if (element.tagName.toLowerCase() === 'span' || element.tagName.toLowerCase() === 'div') {
            element.style.display = 'block';
        }
    });

    // Hide elements that should be hidden for current role
    const hideSelector = '.role-' + currentRole + '-hide';
    document.querySelectorAll(hideSelector).forEach(element => {
        element.style.display = 'none';
    });
}

/**
 * Search Functionality
 */
function initializeSearchFunctionality() {
    const searchInput = document.querySelector('.search-bar input');

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
        // In MVC, redirect to search results
        window.location.href = '/Certificate/Search?q=' + encodeURIComponent(query);
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
    // Remove existing toasts
    document.querySelectorAll('.toast-notification').forEach(toast => {
        toast.remove();
    });

    // Create new toast
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        background-color: ${type === 'success' ? 'var(--success)' : type === 'error' ? 'var(--warning)' : 'var(--blue-primary)'};
        color: white;
        padding: 12px 20px;
        border-radius: 6px;
        box-shadow: var(--shadow);
        z-index: 10000;
        opacity: 0;
        transition: opacity 0.3s ease;
        max-width: 300px;
        word-wrap: break-word;
    `;
    toast.textContent = message;

    document.body.appendChild(toast);

    // Fade in
    setTimeout(() => {
        toast.style.opacity = '1';
    }, 100);

    // Fade out and remove
    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300);
    }, duration);
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

// Global exports for use in individual pages
window.GreenCircle = {
    showToast,
    debounce,
    formatDate,
    makeRequest,
    changeUserRole,
    getCurrentRole
};