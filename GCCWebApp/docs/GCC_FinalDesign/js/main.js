// main.js - Unified Master JavaScript

document.addEventListener('DOMContentLoaded', function () {

    // ===== MASTER LAYOUT FUNCTIONALITY =====

    // Toggle user dropdown
    const userProfile = document.querySelector('.user-profile');
    const userDropdown = document.querySelector('.user-dropdown');

    if (userProfile && userDropdown) {
        userProfile.addEventListener('click', function (e) {
            e.stopPropagation();
            userDropdown.classList.toggle('open');
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', function () {
            userDropdown.classList.remove('open');
        });
    }

    // Mobile menu toggle
    const mobileToggle = document.querySelector('.mobile-toggle');
    const navLinks = document.querySelector('.nav-links');

    if (mobileToggle && navLinks) {
        mobileToggle.addEventListener('click', function () {
            navLinks.classList.toggle('mobile-open');
        });
    }

    // Mobile submenu toggle
    const navItems = document.querySelectorAll('.nav-link');

    navItems.forEach(item => {
        item.addEventListener('click', function (e) {
            if (window.innerWidth <= 768 && this.nextElementSibling && this.nextElementSibling.classList.contains('submenu')) {
                e.preventDefault();
                this.nextElementSibling.classList.toggle('mobile-open');
            }
        });
    });

    // Mobile nested submenu toggle
    const submenuToggles = document.querySelectorAll('.submenu-toggle');

    submenuToggles.forEach(toggle => {
        toggle.addEventListener('click', function (e) {
            if (window.innerWidth <= 768) {
                e.preventDefault();
                this.nextElementSibling.classList.toggle('mobile-open');
            }
        });
    });

    // ===== ROLE MANAGEMENT SYSTEM =====

    // Create role selector for demo purposes
    const roleSelector = document.createElement('div');
    roleSelector.className = 'role-selector';
    roleSelector.innerHTML = `
        <h4>Demo Role Switcher</h4>
        <select id="role-toggle">
            <option value="public">Public</option>
            <option value="external">External</option>
            <option value="analyst">Analyst</option>
            <option value="admin" selected>Admin</option>
        </select>
    `;

    document.body.appendChild(roleSelector);

    const roleToggle = document.getElementById('role-toggle');
    const body = document.body;
    const roleBadge = document.querySelector('.role-badge');

    if (roleToggle) {
        roleToggle.addEventListener('change', function () {
            // Remove all role classes
            body.classList.remove('role-public', 'role-external', 'role-analyst', 'role-admin');

            // Add the selected role class
            body.classList.add('role-' + this.value);

            // Update the role badge if it exists
            if (roleBadge) {
                roleBadge.textContent = this.value.charAt(0).toUpperCase() + this.value.slice(1);
            }

            // Show/hide elements based on role classes
            updateRoleElements();
        });

        // Initialize with default admin role
        body.classList.add('role-admin');
        updateRoleElements();
    }

    // Function to update role-based element visibility
    function updateRoleElements() {
        const currentRole = getCurrentRole();

        // Hide all role elements first
        document.querySelectorAll('.role-element').forEach(element => {
            element.style.display = 'none';
        });

        // Show elements for current role
        const roleShowElements = document.querySelectorAll(`.role-${currentRole}-show`);
        roleShowElements.forEach(element => {
            // Determine appropriate display type
            let displayType = 'block';
            if (element.classList.contains('btn')) {
                displayType = 'flex';
            } else if (element.classList.contains('nav-item')) {
                displayType = 'list-item';
            } else if (element.tagName.toLowerCase() === 'span') {
                displayType = 'inline';
            }

            element.style.display = displayType;
        });

        // Hide elements that should be hidden for current role
        const roleHideElements = document.querySelectorAll(`.role-${currentRole}-hide`);
        roleHideElements.forEach(element => {
            element.style.display = 'none';
        });
    }

    // Get current role from body class
    function getCurrentRole() {
        const classList = body.classList;
        if (classList.contains('role-public')) return 'public';
        if (classList.contains('role-external')) return 'external';
        if (classList.contains('role-analyst')) return 'analyst';
        if (classList.contains('role-admin')) return 'admin';
        return 'admin'; // default
    }

    // ===== UTILITY FUNCTIONS =====

    // Show toast notification
    function showToast(message, type = 'success') {
        let toast = document.getElementById('toast');

        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'toast';
            toast.className = 'toast';
            document.body.appendChild(toast);
        }

        toast.textContent = message;
        toast.className = `toast ${type}`;
        toast.classList.add('show');

        // Hide after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
        }, 3000);
    }

    // Confirm dialog wrapper
    function confirmAction(message, callback) {
        if (confirm(message)) {
            callback();
        }
    }

    // Generic AJAX helper
    function makeRequest(url, method = 'GET', data = null) {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open(method, url);

            if (method === 'POST' && data) {
                xhr.setRequestHeader('Content-Type', 'application/json');
            }

            xhr.onload = function () {
                if (xhr.status >= 200 && xhr.status < 300) {
                    try {
                        const response = JSON.parse(xhr.responseText);
                        resolve(response);
                    } catch (e) {
                        resolve(xhr.responseText);
                    }
                } else {
                    reject(new Error(`Request failed with status ${xhr.status}`));
                }
            };

            xhr.onerror = function () {
                reject(new Error('Network error'));
            };

            if (data) {
                xhr.send(JSON.stringify(data));
            } else {
                xhr.send();
            }
        });
    }

    // CSV export helper
    function exportToCSV(data, filename) {
        if (!data || data.length === 0) {
            showToast('No data to export', 'error');
            return;
        }

        // Get headers from first object
        const headers = Object.keys(data[0]);
        const csvContent = [
            headers.join(','),
            ...data.map(row => headers.map(header => `"${row[header] || ''}"`).join(','))
        ].join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();

        URL.revokeObjectURL(url);
        showToast('Export completed successfully');
    }

    // File import helper
    function importFromCSV(callback) {
        const fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.accept = '.csv';

        fileInput.addEventListener('change', event => {
            const file = event.target.files[0];
            if (!file) return;

            const reader = new FileReader();
            reader.onload = e => {
                const content = e.target.result;
                const lines = content.split('\n');

                if (lines.length < 2) {
                    showToast('Invalid CSV file', 'error');
                    return;
                }

                // Parse CSV data
                const headers = lines[0].split(',').map(h => h.trim().replace(/"/g, ''));
                const data = [];

                for (let i = 1; i < lines.length; i++) {
                    const line = lines[i].trim();
                    if (!line) continue;

                    const values = line.split(',').map(v => v.trim().replace(/"/g, ''));
                    if (values.length === headers.length) {
                        const row = {};
                        headers.forEach((header, index) => {
                            row[header] = values[index];
                        });
                        data.push(row);
                    }
                }

                callback(data);
            };

            reader.readAsText(file);
        });

        fileInput.click();
    }

    // Make utility functions globally available
    window.AppUtils = {
        showToast,
        confirmAction,
        makeRequest,
        exportToCSV,
        importFromCSV,
        updateRoleElements,
        getCurrentRole
    };

    // ===== SEARCH AND FILTER FUNCTIONALITY =====

    // Generic search functionality
    function initializeSearch(inputSelector, dataArray, filterCallback) {
        const searchInput = document.querySelector(inputSelector);
        if (!searchInput) return;

        searchInput.addEventListener('input', function () {
            const searchTerm = this.value.toLowerCase().trim();
            const filteredData = searchTerm
                ? dataArray.filter(item => filterCallback(item, searchTerm))
                : dataArray;

            // Trigger custom event with filtered data
            document.dispatchEvent(new CustomEvent('dataFiltered', {
                detail: { filteredData, searchTerm }
            }));
        });
    }

    // Generic sort functionality
    function initializeSort(tableSelector) {
        const table = document.querySelector(tableSelector);
        if (!table) return;

        const headers = table.querySelectorAll('th[data-sort]');
        let currentSort = { column: null, direction: 'asc' };

        headers.forEach(header => {
            header.addEventListener('click', () => {
                const column = header.getAttribute('data-sort');

                // Toggle direction if same column, otherwise reset to asc
                if (currentSort.column === column) {
                    currentSort.direction = currentSort.direction === 'asc' ? 'desc' : 'asc';
                } else {
                    currentSort.column = column;
                    currentSort.direction = 'asc';
                }

                // Update header styling
                headers.forEach(h => {
                    h.classList.remove('active-sort');
                    const icon = h.querySelector('.sort-icon');
                    if (icon) icon.textContent = '?';
                });

                header.classList.add('active-sort');
                const sortIcon = header.querySelector('.sort-icon');
                if (sortIcon) {
                    sortIcon.textContent = currentSort.direction === 'asc' ? '?' : '?';
                }

                // Trigger custom sort event
                document.dispatchEvent(new CustomEvent('dataSorted', {
                    detail: currentSort
                }));
            });
        });
    }

    // Make search and sort available globally
    window.AppUtils.initializeSearch = initializeSearch;
    window.AppUtils.initializeSort = initializeSort;

    console.log('Main application JavaScript loaded successfully');
});