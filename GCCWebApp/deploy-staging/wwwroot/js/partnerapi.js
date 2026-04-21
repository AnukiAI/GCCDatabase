/**
 * Partner API Access Management
 * Manages API keys for external partners (Amazon, Novi, etc.)
 */

let selectedPartnerId = null;
let partnersData = [];

/**
 * Initialize the Partner API page
 */
function initializePartnerAPI() {
    console.log('Initializing Partner API management...');

    // Set up event listeners
    setupEventListeners();

    // Load initial data
    loadPartners();
    loadAllApiKeys();
}

/**
 * Set up all event listeners
 */
function setupEventListeners() {
    // Refresh button
    document.getElementById('refresh-btn')?.addEventListener('click', () => {
        loadPartners();
        loadAllApiKeys();
        if (selectedPartnerId) {
            loadPartnerDetails(selectedPartnerId);
        }
    });

    // Generate API Key button
    document.getElementById('generate-key-btn')?.addEventListener('click', () => {
        openGenerateKeyModal();
    });

    // Generate Key Modal - Cancel
    document.getElementById('cancel-generate-btn')?.addEventListener('click', () => {
        closeGenerateKeyModal();
    });

    // Generate Key Modal - Confirm
    document.getElementById('confirm-generate-btn')?.addEventListener('click', () => {
        generateApiKey();
    });

    // Generate Key Modal - Close button
    document.querySelector('#generate-key-modal .admin-modal-close')?.addEventListener('click', () => {
        closeGenerateKeyModal();
    });

    // Key Display Modal - Done button
    document.getElementById('close-key-display-btn')?.addEventListener('click', () => {
        closeKeyDisplayModal();
    });

    // Key Display Modal - Close button
    document.querySelector('#key-display-modal .admin-modal-close')?.addEventListener('click', () => {
        closeKeyDisplayModal();
    });

    // Copy API Key button
    document.getElementById('copy-key-btn')?.addEventListener('click', () => {
        copyApiKey();
    });

    // Escape key closes modals
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            // Check for any open modal
            const generateModal = document.getElementById('generate-key-modal');
            const displayModal = document.getElementById('key-display-modal');

            if (generateModal && generateModal.classList.contains('admin-modal--show')) {
                e.preventDefault();
                closeGenerateKeyModal();
            } else if (displayModal && displayModal.classList.contains('admin-modal--show')) {
                e.preventDefault();
                closeKeyDisplayModal();
            }
        }
    });

    // Backdrop click closes Generate modal only (not Display modal for security)
    const generateModal = document.getElementById('generate-key-modal');
    if (generateModal) {
        generateModal.addEventListener('click', (e) => {
            // Only close if clicking the modal itself (backdrop), not its children
            if (e.target === generateModal) {
                closeGenerateKeyModal();
            }
        });
    }

    // Event delegation for revoke/delete buttons in API keys table
    document.getElementById('api-keys-tbody')?.addEventListener('click', (e) => {
        const revokeBtn = e.target.closest('[data-action="revoke"]');
        if (revokeBtn) {
            revokeApiKey(revokeBtn.dataset.keyId, revokeBtn.dataset.keyValue);
            return;
        }
        const deleteBtn = e.target.closest('[data-action="delete"]');
        if (deleteBtn) {
            deleteApiKey(deleteBtn.dataset.keyId);
        }
    });

    // Event delegation for revoke/delete buttons in all keys table
    document.getElementById('all-keys-tbody')?.addEventListener('click', (e) => {
        const revokeBtn = e.target.closest('[data-action="revoke"]');
        if (revokeBtn) {
            revokeApiKey(revokeBtn.dataset.keyId, revokeBtn.dataset.keyValue);
            return;
        }
        const deleteBtn = e.target.closest('[data-action="delete"]');
        if (deleteBtn) {
            deleteApiKey(deleteBtn.dataset.keyId);
        }
    });
}

/**
 * Load all partners
 */
async function loadPartners() {
    const loadingEl = document.getElementById('partners-loading');
    const containerEl = document.getElementById('partners-container');
    const errorEl = document.getElementById('partners-error');
    const gridEl = document.getElementById('partners-grid');

    // Show loading
    loadingEl.style.display = 'block';
    containerEl.style.display = 'none';
    errorEl.style.display = 'none';

    try {
        const response = await fetch('/api/admin/partner/list');
        const result = await response.json();

        if (!response.ok || !result.success) {
            throw new Error(result.message || 'Failed to load partners');
        }

        partnersData = result.data || [];

        if (partnersData.length === 0) {
            gridEl.innerHTML = `
                <div style="grid-column: 1 / -1; text-align: center; padding: 40px; color: #666;">
                    <i class="fas fa-handshake" style="font-size: 48px; color: #ccc; margin-bottom: 12px;"></i>
                    <p style="margin: 0;">No partners found</p>
                </div>
            `;
        } else {
            gridEl.innerHTML = partnersData.map(partner => createPartnerCard(partner)).join('');

            // Add click listeners to partner cards
            partnersData.forEach(partner => {
                document.getElementById(`partner-card-${partner.id}`)?.addEventListener('click', () => {
                    selectPartner(partner.id);
                });
            });
        }

        // Populate partner dropdown in generate modal
        populatePartnerDropdown();

        // Update stats bar - total partners count
        document.getElementById('total-partners').textContent = partnersData.length;

        loadingEl.style.display = 'none';
        containerEl.style.display = 'block';

    } catch (error) {
        console.error('Error loading partners:', error);
        loadingEl.style.display = 'none';
        errorEl.style.display = 'block';
        AdminCore.showNotification('Failed to load partners: ' + error.message, 'error');
    }
}

/**
 * Create HTML for a partner card
 */
function createPartnerCard(partner) {
    const isSelected = selectedPartnerId === partner.id;
    return `
        <div id="partner-card-${partner.id}"
             class="partner-card ${isSelected ? 'selected' : ''}">
            <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px;">
                <h4 style="margin: 0; color: #002B76; font-size: 18px; font-weight: 600;">${escapeHtml(partner.name)}</h4>
                ${isSelected ? '<i class="fas fa-check-circle" style="color: #92C83E; font-size: 20px;"></i>' : ''}
            </div>
            <p style="margin: 0; color: #666; font-size: 14px; line-height: 1.4;">
                ${partner.description || 'No description'}
            </p>
        </div>
    `;
}

/**
 * Select a partner and load their details
 */
async function selectPartner(partnerId) {
    selectedPartnerId = partnerId;

    // Update partner cards using CSS classes
    partnersData.forEach(partner => {
        const cardEl = document.getElementById(`partner-card-${partner.id}`);
        if (cardEl) {
            const isSelected = partner.id === partnerId;
            if (isSelected) {
                cardEl.classList.add('selected');
            } else {
                cardEl.classList.remove('selected');
            }
        }
    });

    // Load partner details
    await loadPartnerDetails(partnerId);
}

/**
 * Load details for a specific partner
 */
async function loadPartnerDetails(partnerId) {
    const detailsSection = document.getElementById('partner-details-section');
    const partnerNameEl = document.getElementById('partner-name');

    // Show details section
    detailsSection.style.display = 'block';

    const partner = partnersData.find(p => p.id === partnerId);
    if (partner) {
        partnerNameEl.textContent = partner.name;
    }

    try {
        // Load usage stats
        const statsResponse = await fetch(`/api/admin/partner/${partnerId}/usage`);
        const statsResult = await statsResponse.json();

        if (statsResponse.ok && statsResult.success) {
            displayUsageStats(statsResult.data);
        }

        // Load API keys
        const keysResponse = await fetch(`/api/admin/partner/${partnerId}/apikeys`);
        const keysResult = await keysResponse.json();

        if (keysResponse.ok && keysResult.success) {
            displayPartnerApiKeys(keysResult.data || []);
        }

    } catch (error) {
        console.error('Error loading partner details:', error);
        AdminCore.showNotification('Failed to load partner details', 'error');
    }
}

/**
 * Display usage statistics
 */
function displayUsageStats(stats) {
    document.getElementById('stat-total-keys').textContent = stats.TotalApiKeys || 0;
    document.getElementById('stat-active-keys').textContent = stats.ActiveApiKeys || 0;
    document.getElementById('stat-revoked-keys').textContent = stats.RevokedApiKeys || 0;
    document.getElementById('stat-total-requests').textContent = (stats.TotalRequests || 0).toLocaleString();

    const lastUsed = stats.LastUsed ? formatDateTime(stats.LastUsed) : 'Never';
    document.getElementById('stat-last-used').textContent = lastUsed;
}

/**
 * Display API keys for a partner
 */
function displayPartnerApiKeys(keys) {
    const tbody = document.getElementById('api-keys-tbody');

    if (!keys || keys.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8" style="text-align: center; padding: 40px; color: #666;">
                    <i class="fas fa-key" style="font-size: 48px; color: #ccc; margin-bottom: 12px;"></i>
                    <p style="margin: 0;">No API keys found for this partner</p>
                </td>
            </tr>
        `;
        return;
    }

    tbody.innerHTML = keys.map(key => `
        <tr>
            <td>
                <code style="font-size: 12px; color: #002B76; background: #f5f5f5; padding: 4px 8px; border-radius: 4px;">
                    ${maskApiKey(key.apiKey)}
                </code>
            </td>
            <td>${escapeHtml(key.description || '-')}</td>
            <td>${formatDate(key.createdAt)}</td>
            <td>${key.expiresAt ? formatDate(key.expiresAt) : 'Never'}</td>
            <td>${(key.requestCount || 0).toLocaleString()}</td>
            <td>${key.lastUsedAt ? formatDateTime(key.lastUsedAt) : 'Never'}</td>
            <td>
                ${key.isActive
                    ? '<span class="admin-badge admin-badge--success">Active</span>'
                    : '<span class="admin-badge admin-badge--danger">Revoked</span>'}
            </td>
            <td class="admin-table-actions">
                ${key.isActive
                    ? `<button class="admin-btn-icon admin-btn-icon--danger"
                              data-action="revoke"
                              data-key-id="${key.id}"
                              data-key-value="${escapeHtml(key.apiKey)}"
                              title="Revoke this API key">
                           <i class="fas fa-ban"></i>
                       </button>`
                    : `<button class="admin-btn-icon admin-btn-icon--danger"
                              data-action="delete"
                              data-key-id="${key.id}"
                              title="Delete this revoked API key">
                           <i class="fas fa-trash"></i>
                       </button>`}
            </td>
        </tr>
    `).join('');
}

/**
 * Load all API keys across all partners
 */
async function loadAllApiKeys() {
    const loadingEl = document.getElementById('all-keys-loading');
    const containerEl = document.getElementById('all-keys-container');
    const errorEl = document.getElementById('all-keys-error');
    const tbody = document.getElementById('all-keys-tbody');

    // Show loading
    loadingEl.style.display = 'block';
    containerEl.style.display = 'none';
    errorEl.style.display = 'none';

    try {
        const response = await fetch('/api/admin/partner/apikeys/all');
        const result = await response.json();

        if (!response.ok || !result.success) {
            throw new Error(result.message || 'Failed to load API keys');
        }

        const keys = result.data || [];

        if (keys.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="9" style="text-align: center; padding: 40px; color: #666;">
                        <i class="fas fa-key" style="font-size: 48px; color: #ccc; margin-bottom: 12px;"></i>
                        <p style="margin: 0;">No API keys found</p>
                    </td>
                </tr>
            `;
            // Update stats bar - API keys counts (0)
            document.getElementById('total-api-keys').textContent = '0';
            document.getElementById('active-api-keys').textContent = '0';
            document.getElementById('partner-count').textContent = `Showing ${partnersData.length} partners and 0 API keys`;
        } else {
            tbody.innerHTML = keys.map(key => `
                <tr>
                    <td><strong>${escapeHtml(key.partner?.name || 'Unknown')}</strong></td>
                    <td>
                        <code style="font-size: 12px; color: #002B76; background: #f5f5f5; padding: 4px 8px; border-radius: 4px;">
                            ${maskApiKey(key.apiKey)}
                        </code>
                    </td>
                    <td>${escapeHtml(key.description || '-')}</td>
                    <td>${formatDate(key.createdAt)}</td>
                    <td>${key.expiresAt ? formatDate(key.expiresAt) : 'Never'}</td>
                    <td>${(key.requestCount || 0).toLocaleString()}</td>
                    <td>${key.lastUsedAt ? formatDateTime(key.lastUsedAt) : 'Never'}</td>
                    <td>
                        ${key.isActive
                            ? '<span class="admin-badge admin-badge--success">Active</span>'
                            : '<span class="admin-badge admin-badge--danger">Revoked</span>'}
                    </td>
                    <td class="admin-table-actions">
                        ${key.isActive
                            ? `<button class="admin-btn-icon admin-btn-icon--danger"
                                      data-action="revoke"
                                      data-key-id="${key.id}"
                                      data-key-value="${escapeHtml(key.apiKey)}"
                                      title="Revoke this API key">
                                   <i class="fas fa-ban"></i>
                               </button>`
                            : `<button class="admin-btn-icon admin-btn-icon--danger"
                                      data-action="delete"
                                      data-key-id="${key.id}"
                                      title="Delete this revoked API key">
                                   <i class="fas fa-trash"></i>
                               </button>`}
                    </td>
                </tr>
            `).join('');
        }

        // Update stats bar - API keys counts
        document.getElementById('total-api-keys').textContent = keys.length;
        const activeCount = keys.filter(k => k.isActive).length;
        document.getElementById('active-api-keys').textContent = activeCount;

        // Update footer count
        const partnerCount = partnersData.length;
        document.getElementById('partner-count').textContent = `Showing ${partnerCount} partners and ${keys.length} API keys`;

        loadingEl.style.display = 'none';
        containerEl.style.display = 'block';

    } catch (error) {
        console.error('Error loading all API keys:', error);
        loadingEl.style.display = 'none';
        errorEl.style.display = 'block';
        AdminCore.showNotification('Failed to load API keys: ' + error.message, 'error');
    }
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

    // Focus first element
    firstElement.focus();

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

/**
 * Open the Generate API Key modal
 */
function openGenerateKeyModal() {
    const modal = document.getElementById('generate-key-modal');
    // Use setTimeout to allow display change to apply before adding show class
    setTimeout(() => {
        modal.classList.add('admin-modal--show');
        setupFocusTrap('generate-key-modal');
    }, 10);

    // Reset form
    document.getElementById('generate-key-form').reset();

    // Populate partners dropdown
    populatePartnerDropdown();
}

/**
 * Close the Generate API Key modal
 */
function closeGenerateKeyModal() {
    const modal = document.getElementById('generate-key-modal');
    removeFocusTrap('generate-key-modal');
    modal.classList.remove('admin-modal--show');
}

/**
 * Populate the partner dropdown in the generate modal
 */
function populatePartnerDropdown() {
    const select = document.getElementById('key-partner-id');
    if (!select) return;

    // Keep the default option
    select.innerHTML = '<option value="">Select Partner</option>';

    // Add partners
    partnersData.forEach(partner => {
        const option = document.createElement('option');
        option.value = partner.id;
        option.textContent = partner.name;
        if (selectedPartnerId && partner.id === selectedPartnerId) {
            option.selected = true;
        }
        select.appendChild(option);
    });
}

/**
 * Generate a new API key
 */
async function generateApiKey() {
    const partnerId = document.getElementById('key-partner-id').value;
    const description = document.getElementById('key-description').value;
    const expiresAt = document.getElementById('key-expires-at').value;

    if (!partnerId) {
        AdminCore.showNotification('Please select a partner', 'warning');
        return;
    }

    const data = {
        description: description || null,
        expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null
    };

    try {
        const response = await fetch(`/api/admin/partner/${partnerId}/apikey`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (!response.ok || !result.success) {
            throw new Error(result.message || 'Failed to generate API key');
        }

        // Close generate modal
        closeGenerateKeyModal();

        // Show the new API key
        showGeneratedKey(result.data);

        // Reload data
        loadAllApiKeys();
        if (selectedPartnerId) {
            loadPartnerDetails(selectedPartnerId);
        }

    } catch (error) {
        console.error('Error generating API key:', error);
        AdminCore.showNotification('Failed to generate API key: ' + error.message, 'error');
    }
}

/**
 * Show the generated API key in a modal
 */
function showGeneratedKey(keyData) {
    document.getElementById('generated-key-value').value = keyData.apiKey;
    document.getElementById('generated-key-partner').textContent = keyData.partner?.name || 'Unknown';
    document.getElementById('generated-key-created').textContent = formatDateTime(keyData.createdAt);
    document.getElementById('generated-key-description').textContent = keyData.description || 'None';

    const modal = document.getElementById('key-display-modal');
    // Use setTimeout to allow display change to apply before adding show class
    setTimeout(() => {
        modal.classList.add('admin-modal--show');
        setupFocusTrap('key-display-modal');
    }, 10);
}

/**
 * Close the key display modal
 */
function closeKeyDisplayModal() {
    const modal = document.getElementById('key-display-modal');
    removeFocusTrap('key-display-modal');
    modal.classList.remove('admin-modal--show');
}

/**
 * Copy API key to clipboard
 */
async function copyApiKey() {
    const input = document.getElementById('generated-key-value');
    const apiKey = input.value;

    try {
        await navigator.clipboard.writeText(apiKey);
        AdminCore.showNotification('API key copied to clipboard', 'success');

        // Change button text temporarily
        const btn = document.getElementById('copy-key-btn');
        const originalHtml = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-check"></i> Copied!';
        btn.style.background = '#92C83E';
        btn.style.color = 'white';

        setTimeout(() => {
            btn.innerHTML = originalHtml;
            btn.style.background = '';
            btn.style.color = '';
        }, 2000);

    } catch (error) {
        console.error('Error copying to clipboard:', error);

        // Fallback: select the text
        input.select();
        document.execCommand('copy');
        AdminCore.showNotification('API key copied to clipboard', 'success');
    }
}

/**
 * Revoke an API key
 */
async function revokeApiKey(keyId, apiKey) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Revoke API Key',
        message: `Are you sure you want to revoke this API key? The partner will immediately lose access.\n\nKey: ${maskApiKey(apiKey)}`,
        confirmText: 'Revoke',
        type: 'danger'
    });

    if (!confirmed) return;

    try {
        const response = await fetch(`/api/admin/partner/apikey/${keyId}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });

        const result = await response.json();

        if (!response.ok || !result.success) {
            throw new Error(result.message || 'Failed to revoke API key');
        }

        AdminCore.showNotification('API key revoked successfully', 'success');

        // Reload data
        loadAllApiKeys();
        if (selectedPartnerId) {
            loadPartnerDetails(selectedPartnerId);
        }

    } catch (error) {
        console.error('Error revoking API key:', error);
        AdminCore.showNotification('Failed to revoke API key: ' + error.message, 'error');
    }
}

/**
 * Mask API key for display (show only first 16 and last 8 characters)
 */
function maskApiKey(apiKey) {
    if (!apiKey || apiKey.length < 24) return apiKey;
    const prefix = apiKey.substring(0, 16);
    const suffix = apiKey.substring(apiKey.length - 8);
    return `${prefix}...${suffix}`;
}

/**
 * Format date (YYYY-MM-DD)
 */
function formatDate(dateString) {
    if (!dateString) return '-';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return '-';
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
}

/**
 * Format date and time
 */
function formatDateTime(dateString) {
    if (!dateString) return '-';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return '-';
    return date.toLocaleString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Delete a revoked API key permanently
 */
async function deleteApiKey(keyId) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Delete API Key',
        message: 'Are you sure you want to permanently delete this revoked API key? This action cannot be undone.',
        confirmText: 'Delete',
        type: 'danger'
    });

    if (!confirmed) return;

    try {
        const response = await fetch(`/api/admin/partner/apikey/${keyId}/delete`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });

        const result = await response.json();

        if (!response.ok || !result.success) {
            throw new Error(result.message || 'Failed to delete API key');
        }

        AdminCore.showNotification('API key deleted successfully', 'success');

        // Reload data
        loadAllApiKeys();
        if (selectedPartnerId) {
            loadPartnerDetails(selectedPartnerId);
        }

    } catch (error) {
        console.error('Error deleting API key:', error);
        AdminCore.showNotification('Failed to delete API key: ' + error.message, 'error');
    }
}

// Make functions available globally for inline onclick handlers
window.revokeApiKey = revokeApiKey;
window.deleteApiKey = deleteApiKey;
window.initializePartnerAPI = initializePartnerAPI;
