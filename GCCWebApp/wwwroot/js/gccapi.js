// GCC API Management - API Key CRUD Operations
// Manages API keys for accessing GCC REST API endpoints

let allApiKeys = [];
let filteredApiKeys = [];
let sortColumn = 'name';
let sortDirection = 'asc';

// Initialize API Management
function initializeAPIManagement() {
    console.log('Initializing API Management...');

    // Setup event listeners first
    setupEventListeners();

    // Load API keys with a small delay to ensure page is fully loaded
    setTimeout(() => {
        console.log('Loading API keys...');
        loadApiKeys();
    }, 100);
}

// Setup Event Listeners
function setupEventListeners() {
    // Generate Key button
    const generateBtn = document.getElementById('generate-key-btn');
    if (generateBtn) {
        generateBtn.addEventListener('click', showGenerateKeyModal);
    }

    // Swagger button
    const swaggerBtn = document.getElementById('swagger-btn');
    if (swaggerBtn) {
        swaggerBtn.addEventListener('click', () => {
            window.open('/swagger', '_blank');
        });
    }

    // Modal controls
    const modal = document.getElementById('apikey-modal');
    const cancelBtn = document.getElementById('cancel-btn');
    const closeBtn = modal?.querySelector('.admin-modal-close');

    if (cancelBtn) {
        cancelBtn.addEventListener('click', closeGenerateKeyModal);
    }
    if (closeBtn) {
        closeBtn.addEventListener('click', closeGenerateKeyModal);
    }

    // Form submission
    const form = document.getElementById('apikey-form');
    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            await generateApiKey();
        });
    }

    // Success modal close
    const closeSuccessBtn = document.getElementById('close-success-btn');
    if (closeSuccessBtn) {
        closeSuccessBtn.addEventListener('click', closeSuccessModal);
    }

    // Copy key button
    const copyKeyBtn = document.getElementById('copy-key-btn');
    if (copyKeyBtn) {
        copyKeyBtn.addEventListener('click', copyApiKeyToClipboard);
    }

    // Search and filter
    const searchInput = document.getElementById('search-input');
    if (searchInput) {
        searchInput.addEventListener('input', handleSearchAndFilter);
    }

    const statusFilter = document.getElementById('status-filter');
    if (statusFilter) {
        statusFilter.addEventListener('change', handleSearchAndFilter);
    }

    // Initialize sorting
    initializeSorting();

    // Escape key closes modals
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            // Check for any open modal
            const generateModal = document.getElementById('apikey-modal');
            const successModal = document.getElementById('apikey-success-modal');

            if (generateModal && generateModal.classList.contains('admin-modal--active')) {
                e.preventDefault();
                closeGenerateKeyModal();
            } else if (successModal && successModal.classList.contains('admin-modal--active')) {
                e.preventDefault();
                closeSuccessModal();
            }
        }
    });

    // Backdrop click closes Generate modal only (not Success modal for security)
    const generateModal = document.getElementById('apikey-modal');
    if (generateModal) {
        generateModal.addEventListener('click', (e) => {
            // Only close if clicking the modal itself (backdrop), not its children
            if (e.target === generateModal) {
                closeGenerateKeyModal();
            }
        });
    }

    // Event delegation for retry button in error state
    const apiKeyTable = document.getElementById('apikey-table');
    if (apiKeyTable) {
        apiKeyTable.addEventListener('click', (e) => {
            const retryBtn = e.target.closest('[data-action="retry"]');
            if (retryBtn) {
                loadApiKeys();
            }

            // Event delegation for revoke buttons
            const revokeBtn = e.target.closest('[data-action="revoke"]');
            if (revokeBtn && !revokeBtn.disabled) {
                const keyId = revokeBtn.dataset.keyId;
                const keyName = revokeBtn.dataset.keyName;
                revokeApiKey(keyId, keyName);
            }
        });
    }
}

// Load API Keys
async function loadApiKeys() {
    try {
        const response = await fetch('/api/v1/apikeys', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'same-origin'
        });

        if (!response.ok) {
            // Try to get error details from response
            let errorMsg = `HTTP ${response.status}: ${response.statusText}`;
            try {
                const errorData = await response.text();
                if (errorData) {
                    errorMsg += ` - ${errorData}`;
                }
            } catch (e) {
                // Ignore parse errors
            }
            throw new Error(errorMsg);
        }

        const result = await response.json();

        if (result.success && result.data) {
            allApiKeys = result.data;
            filteredApiKeys = [...allApiKeys];
            renderApiKeys();
        } else {
            throw new Error(result.message || 'Failed to load API keys');
        }
    } catch (error) {
        console.error('Error loading API keys:', error);

        // Only show notification if AdminCore is available
        if (window.AdminCore && typeof AdminCore.showNotification === 'function') {
            AdminCore.showNotification('Failed to load API keys: ' + error.message, 'error');
        } else {
            console.warn('AdminCore not available for notification');
        }

        // Show empty state with error details
        const tbody = document.querySelector('#apikey-table tbody');
        if (tbody) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="7" style="text-align: center; padding: 40px; color: #666;">
                        <i class="fas fa-exclamation-triangle" style="font-size: 48px; color: #ccc; margin-bottom: 12px;"></i>
                        <p style="margin: 0 0 8px 0; font-weight: 600;">Failed to load API keys</p>
                        <p style="margin: 0; font-size: 12px; color: #999;">${escapeHtml(error.message)}</p>
                        <button data-action="retry" style="margin-top: 16px; padding: 8px 16px; background: #00AEEF; color: white; border: none; border-radius: 4px; cursor: pointer;">
                            <i class="fas fa-sync"></i> Retry
                        </button>
                    </td>
                </tr>
            `;
        }
    }
}

// Render API Keys Table
function renderApiKeys() {
    const tbody = document.querySelector('#apikey-table tbody');
    if (!tbody) return;

    if (filteredApiKeys.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" style="text-align: center; padding: 40px; color: #666;">
                    <i class="fas fa-key" style="font-size: 48px; color: #ccc; margin-bottom: 12px;"></i>
                    <p style="margin: 0;">No API keys found. Click "Generate API Key" to create your first key.</p>
                </td>
            </tr>
        `;
        updateCount(0, 0);
        return;
    }

    tbody.innerHTML = filteredApiKeys.map(key => {
        const createdDate = key.createdAt ? new Date(key.createdAt).toLocaleDateString() : 'N/A';
        const lastUsed = key.lastUsedAt ? new Date(key.lastUsedAt).toLocaleDateString() : 'Never';
        const expires = key.expiresAt ? new Date(key.expiresAt).toLocaleDateString() : 'Never';

        let statusBadge;
        let statusClass;

        if (key.isExpired) {
            statusBadge = '<span class="admin-badge admin-badge--danger">Expired</span>';
            statusClass = 'expired';
        } else if (!key.isActive) {
            statusBadge = '<span class="admin-badge admin-badge--secondary">Revoked</span>';
            statusClass = 'revoked';
        } else {
            statusBadge = '<span class="admin-badge admin-badge--success">Active</span>';
            statusClass = 'active';
        }

        return `
            <tr data-id="${key.id}" data-status="${statusClass}">
                <td class="admin-table-td">
                    <div style="font-weight: 500;">${escapeHtml(key.name)}</div>
                    ${key.description ? `<div style="font-size: 12px; color: #666; margin-top: 4px;">${escapeHtml(key.description)}</div>` : ''}
                </td>
                <td class="admin-table-td admin-table-td--hidden-mobile">
                    <code style="font-size: 11px; background: #f5f5f5; padding: 4px 8px; border-radius: 3px;">${escapeHtml(key.keyPreview || '***')}</code>
                </td>
                <td class="admin-table-td admin-table-td--hidden-mobile">${createdDate}</td>
                <td class="admin-table-td admin-table-td--hidden-mobile">${lastUsed}</td>
                <td class="admin-table-td admin-table-td--hidden-mobile">${expires}</td>
                <td class="admin-table-td">${statusBadge}</td>
                <td class="admin-table-td admin-table-td--actions">
                    <div class="admin-table-actions">
                        <button class="admin-table-action-btn admin-table-action-btn--delete"
                                data-action="revoke"
                                data-key-id="${key.id}"
                                data-key-name="${escapeHtml(key.name)}"
                                title="Revoke API key"
                                ${!key.isActive ? 'disabled style="opacity: 0.5; cursor: not-allowed;"' : ''}>
                            <i class="fas fa-ban"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }).join('');

    updateCount(filteredApiKeys.length, allApiKeys.length);
}

/**
 * Focus trap handler for modal accessibility
 */
let focusTrapHandler = null;

function setupFocusTrap(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;

    const focusableElements = modal.querySelectorAll(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
    );

    if (focusableElements.length === 0) return;

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    // Trap focus within modal
    focusTrapHandler = (e) => {
        if (e.key !== 'Tab') return;

        if (e.shiftKey) {
            // Shift + Tab
            if (document.activeElement === firstElement) {
                lastElement.focus();
                e.preventDefault();
            }
        } else {
            // Tab
            if (document.activeElement === lastElement) {
                firstElement.focus();
                e.preventDefault();
            }
        }
    };

    modal.addEventListener('keydown', focusTrapHandler);
}

function removeFocusTrap(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal || !focusTrapHandler) return;
    modal.removeEventListener('keydown', focusTrapHandler);
    focusTrapHandler = null;
}

// Show Generate Key Modal
function showGenerateKeyModal() {
    const modal = document.getElementById('apikey-modal');
    if (modal) {
        // Clear form
        document.getElementById('key-name').value = '';
        document.getElementById('key-description').value = '';
        document.getElementById('key-expiry').value = '';

        // Show modal
        modal.classList.add('admin-modal--active');
        modal.setAttribute('aria-hidden', 'false');

        // Focus first input and setup focus trap
        setTimeout(() => {
            document.getElementById('key-name')?.focus();
            setupFocusTrap('apikey-modal');
        }, 100);
    }
}

// Close Generate Key Modal
function closeGenerateKeyModal() {
    const modal = document.getElementById('apikey-modal');
    if (modal) {
        removeFocusTrap('apikey-modal');
        modal.classList.remove('admin-modal--active');
        modal.setAttribute('aria-hidden', 'true');
    }
}

// Generate API Key
async function generateApiKey() {
    const name = document.getElementById('key-name').value.trim();
    const description = document.getElementById('key-description').value.trim();
    const expiryDate = document.getElementById('key-expiry').value;

    if (!name || !description) {
        AdminCore.showNotification('Please fill in all required fields', 'error');
        return;
    }

    try {
        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const requestData = {
            name: name,
            description: description,
            expiresAt: expiryDate || null
        };

        const response = await fetch('/api/v1/apikeys', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            credentials: 'same-origin',
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        const result = await response.json();

        if (result.success && result.data) {
            closeGenerateKeyModal();
            showSuccessModal(result.data);

            // Reload keys
            await loadApiKeys();
        } else {
            throw new Error(result.message || 'Failed to generate API key');
        }
    } catch (error) {
        console.error('Error generating API key:', error);
        AdminCore.showNotification('Failed to generate API key: ' + error.message, 'error');
    }
}

// Show Success Modal with Generated Key
function showSuccessModal(keyData) {
    const modal = document.getElementById('apikey-success-modal');
    if (!modal) return;

    // Set key information
    document.getElementById('generated-key').value = keyData.key;
    document.getElementById('generated-key-name').value = keyData.name;

    // Update usage example
    const usageExample = document.getElementById('usage-example');
    if (usageExample) {
        usageExample.textContent = `curl -H "X-API-Key: ${keyData.key}" \\
  ${window.location.origin}/api/v1/certificates`;
    }

    // Show modal
    modal.classList.add('admin-modal--active');
    modal.setAttribute('aria-hidden', 'false');

    // Select the key for easy copying and setup focus trap
    setTimeout(() => {
        const keyInput = document.getElementById('generated-key');
        keyInput?.select();
        setupFocusTrap('apikey-success-modal');
    }, 100);
}

// Close Success Modal
function closeSuccessModal() {
    const modal = document.getElementById('apikey-success-modal');
    if (modal) {
        removeFocusTrap('apikey-success-modal');
        modal.classList.remove('admin-modal--active');
        modal.setAttribute('aria-hidden', 'true');
    }
}

// Copy API Key to Clipboard
async function copyApiKeyToClipboard() {
    const keyInput = document.getElementById('generated-key');
    if (!keyInput) return;

    try {
        await navigator.clipboard.writeText(keyInput.value);

        const copyBtn = document.getElementById('copy-key-btn');
        if (copyBtn) {
            const originalHTML = copyBtn.innerHTML;
            copyBtn.innerHTML = '<i class="fas fa-check"></i> Copied!';
            copyBtn.style.background = '#92C83E';
            copyBtn.style.color = 'white';

            setTimeout(() => {
                copyBtn.innerHTML = originalHTML;
                copyBtn.style.background = '';
                copyBtn.style.color = '';
            }, 2000);
        }

        AdminCore.showNotification('API key copied to clipboard', 'success');
    } catch (error) {
        console.error('Failed to copy:', error);

        // Fallback: select text
        keyInput.select();
        AdminCore.showNotification('Please manually copy the API key', 'warning');
    }
}

// Revoke API Key
async function revokeApiKey(id, name) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Revoke API Key',
        message: `Are you sure you want to revoke the API key "${name}"? This action cannot be undone and any applications using this key will lose access immediately.`,
        confirmText: 'Revoke',
        type: 'danger'
    });

    if (!confirmed) return;

    try {
        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const response = await fetch(`/api/v1/apikeys/${id}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': token
            },
            credentials: 'same-origin'
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        AdminCore.showNotification('API key revoked successfully', 'success');

        // Reload keys
        await loadApiKeys();
    } catch (error) {
        console.error('Error revoking API key:', error);
        AdminCore.showNotification('Failed to revoke API key: ' + error.message, 'error');
    }
}

// Initialize Sorting
function initializeSorting() {
    const table = document.getElementById('apikey-table');
    if (!table) return;

    const headers = table.querySelectorAll('th[data-sort]');
    headers.forEach(header => {
        header.style.cursor = 'pointer';
        header.addEventListener('click', () => {
            const column = header.getAttribute('data-sort');

            // Toggle sort direction if clicking the same column
            if (sortColumn === column) {
                sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
            } else {
                sortColumn = column;
                sortDirection = 'asc';
            }

            // Update header styling and icons
            headers.forEach(h => {
                h.classList.remove('active-sort');
                const icon = h.querySelector('.sort-icon');
                if (icon) {
                    icon.textContent = '▼';
                }
            });

            header.classList.add('active-sort');
            const sortIcon = header.querySelector('.sort-icon');
            if (sortIcon) {
                sortIcon.textContent = sortDirection === 'asc' ? '▲' : '▼';
            }

            // Re-filter and render
            handleSearchAndFilter();
        });
    });
}

// Handle Search and Filter
function handleSearchAndFilter() {
    const searchTerm = document.getElementById('search-input')?.value.toLowerCase() || '';
    const statusFilter = document.getElementById('status-filter')?.value || 'all';

    filteredApiKeys = allApiKeys.filter(key => {
        // Search filter
        const matchesSearch = !searchTerm ||
            key.name.toLowerCase().includes(searchTerm) ||
            (key.description && key.description.toLowerCase().includes(searchTerm)) ||
            (key.keyPreview && key.keyPreview.toLowerCase().includes(searchTerm));

        // Status filter
        let matchesStatus = true;
        if (statusFilter !== 'all') {
            if (statusFilter === 'active') {
                matchesStatus = key.isActive && !key.isExpired;
            } else if (statusFilter === 'expired') {
                matchesStatus = key.isExpired;
            } else if (statusFilter === 'revoked') {
                matchesStatus = !key.isActive;
            }
        }

        return matchesSearch && matchesStatus;
    });

    // Sort filtered results
    filteredApiKeys.sort((a, b) => {
        let valueA, valueB;

        if (sortColumn === 'createdAt') {
            valueA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
            valueB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
        } else {
            valueA = (a[sortColumn] || '').toString().toLowerCase();
            valueB = (b[sortColumn] || '').toString().toLowerCase();
        }

        if (sortDirection === 'asc') {
            return valueA > valueB ? 1 : valueA < valueB ? -1 : 0;
        } else {
            return valueA < valueB ? 1 : valueA > valueB ? -1 : 0;
        }
    });

    renderApiKeys();
}

// Update Count Display
function updateCount(showing, total) {
    const countDisplay = document.getElementById('apikey-count');
    if (countDisplay) {
        countDisplay.textContent = `Showing ${showing} of ${total} API keys`;
    }
}

// HTML Escape Utility
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Export function for initialization
window.initializeAPIManagement = initializeAPIManagement;
