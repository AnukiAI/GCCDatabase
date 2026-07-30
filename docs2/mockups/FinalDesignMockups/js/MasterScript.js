document.addEventListener('DOMContentLoaded', function () {
    // Toggle user dropdown
    const userProfile = document.querySelector('.user-profile');
    const userDropdown = document.querySelector('.user-dropdown');

    userProfile.addEventListener('click', function (e) {
        e.stopPropagation();
        userDropdown.classList.toggle('open');
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', function () {
        userDropdown.classList.remove('open');
    });

    // Mobile menu toggle
    const mobileToggle = document.querySelector('.mobile-toggle');
    const navLinks = document.querySelector('.nav-links');

    mobileToggle.addEventListener('click', function () {
        navLinks.classList.toggle('mobile-open');
    });

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

    // Role selector for demo purposes
    const roleSelector = document.createElement('div');
    roleSelector.style.position = 'fixed';
    roleSelector.style.bottom = '10px';
    roleSelector.style.right = '10px';
    roleSelector.style.background = 'white';
    roleSelector.style.padding = '10px';
    roleSelector.style.borderRadius = '4px';
    roleSelector.style.boxShadow = '0 2px 10px rgba(0,0,0,0.1)';
    roleSelector.style.zIndex = '9999';

    roleSelector.innerHTML = `
                                            <div style="margin-bottom: 5px; font-weight: bold;">Change Role:</div>
                                            <select id="role-toggle" style="padding: 5px; width: 100%;">
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

    roleToggle.addEventListener('change', function () {
        // Remove all role classes
        body.classList.remove('role-public', 'role-external', 'role-analyst', 'role-admin');

        // Add the selected role class
        body.classList.add('role-' + this.value);

        // Update the role badge
        roleBadge.textContent = this.value.charAt(0).toUpperCase() + this.value.slice(1);

        // Show/hide elements based on role classes
        updatePublicElements(this.value === 'public');
        updateExternalElements(this.value === 'external');
        updateAnalystElements(this.value === 'analyst');
        updateAdminElements(this.value === 'admin');
    });

    // Function to update public-only elements
    function updatePublicElements(isPublic) {
        const publicElements = document.querySelectorAll('.public-only');
        const notPublicElements = document.querySelectorAll('.not-public');

        publicElements.forEach(element => {
            element.style.display = isPublic ? 'block' : 'none';
        });

        notPublicElements.forEach(element => {
            element.style.display = isPublic ? 'none' : '';
        });

        // Special handling for flex elements
        document.querySelectorAll('.public-only.btn').forEach(btn => {
            btn.style.display = isPublic ? 'flex' : 'none';
        });

        // Special handling for list items
        document.querySelectorAll('.nav-item.public-only').forEach(item => {
            item.style.display = isPublic ? 'list-item' : 'none';
        });
    }

    // Function to update external-only elements
    function updateExternalElements(isExternal) {
        const externalElements = document.querySelectorAll('.external-only');
        const notExternalElements = document.querySelectorAll('.not-external');

        externalElements.forEach(element => {
            element.style.display = isExternal ? 'block' : 'none';
        });

        notExternalElements.forEach(element => {
            element.style.display = isExternal ? 'none' : '';
        });

        // Special handling for flex elements
        document.querySelectorAll('.external-only.btn').forEach(btn => {
            btn.style.display = isExternal ? 'flex' : 'none';
        });

        // Special handling for list items
        document.querySelectorAll('.nav-item.external-only').forEach(item => {
            item.style.display = isExternal ? 'list-item' : 'none';
        });
    }

    // Function to update analyst-only elements
    function updateAnalystElements(isAnalyst) {
        const analystElements = document.querySelectorAll('.analyst-only');

        analystElements.forEach(element => {
            element.style.display = isAnalyst ? 'block' : 'none';
        });

        // Special handling for flex elements
        document.querySelectorAll('.analyst-only.btn').forEach(btn => {
            btn.style.display = isAnalyst ? 'flex' : 'none';
        });

        // Special handling for list items
        document.querySelectorAll('.nav-item.analyst-only').forEach(item => {
            item.style.display = isAnalyst ? 'list-item' : 'none';
        });
    }

    // Function to update admin-only elements
    function updateAdminElements(isAdmin) {
        const adminElements = document.querySelectorAll('.admin-only');

        adminElements.forEach(element => {
            element.style.display = isAdmin ? 'block' : 'none';
        });

        // Special handling for flex elements
        document.querySelectorAll('.admin-only.btn').forEach(btn => {
            btn.style.display = isAdmin ? 'flex' : 'none';
        });

        // Special handling for list items
        document.querySelectorAll('.nav-item.admin-only').forEach(item => {
            item.style.display = isAdmin ? 'list-item' : 'none';
        });
    }
});