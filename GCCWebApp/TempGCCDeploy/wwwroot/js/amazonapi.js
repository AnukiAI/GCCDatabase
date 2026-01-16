// Amazon API Integration Management
// Manages Amazon Selling Partner API connection settings and sync operations

let currentSettings = null;
let syncHistoryData = [];

// Initialize Amazon API Management
function initializeAmazonAPI() {
    console.log('Initializing Amazon API Management...');

    // Setup event listeners
    setupEventListeners();

    // Load current settings
    setTimeout(() => {
        console.log('Loading Amazon API settings...');
        loadSettings();
        loadSyncHistory();
    }, 100);
}

// Setup Event Listeners
function setupEventListeners() {
    // Save Settings button
    const saveBtn = document.getElementById('save-settings-btn');
    if (saveBtn) {
        saveBtn.addEventListener('click', saveSettings);
    }

    // Test Connection button
    const testBtn = document.getElementById('test-connection-btn');
    if (testBtn) {
        testBtn.addEventListener('click', testConnection);
    }

    // Sync Now button
    const syncBtn = document.getElementById('sync-now-btn');
    if (syncBtn) {
        syncBtn.addEventListener('click', triggerSync);
    }

    // Toggle secret visibility
    const toggleSecretBtn = document.getElementById('toggle-secret-btn');
    if (toggleSecretBtn) {
        toggleSecretBtn.addEventListener('click', toggleSecretVisibility);
    }

    // Region change updates marketplace options
    const regionSelect = document.getElementById('region');
    if (regionSelect) {
        regionSelect.addEventListener('change', updateMarketplaceOptions);
    }
}

// Load Current Settings
async function loadSettings() {
    try {
        const response = await fetch('/api/admin/amazon/settings', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'same-origin'
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success && result.data) {
            currentSettings = result.data;
            populateForm(currentSettings);
            updateConnectionStatus(currentSettings);
        } else {
            throw new Error(result.message || 'Failed to load settings');
        }
    } catch (error) {
        console.error('Error loading settings:', error);
        if (window.AdminCore && typeof AdminCore.showNotification === 'function') {
            AdminCore.showNotification('Failed to load settings: ' + error.message, 'error');
        }
    }
}

// Populate Form with Settings
function populateForm(settings) {
    document.getElementById('is-enabled').checked = settings.isEnabled || false;
    document.getElementById('sync-interval').value = settings.syncInterval || 24;
    document.getElementById('access-key-id').value = settings.accessKeyId || '';
    document.getElementById('secret-access-key').value = settings.secretAccessKey || '';
    document.getElementById('refresh-token').value = settings.refreshToken || '';
    document.getElementById('region').value = settings.region || '';
    document.getElementById('marketplace-id').value = settings.marketplaceId || '';
    document.getElementById('sync-products').checked = settings.syncProducts !== false;
    document.getElementById('sync-certificates').checked = settings.syncCertificates !== false;
    document.getElementById('sync-compliance').checked = settings.syncCompliance !== false;
    document.getElementById('webhook-url').value = settings.webhookUrl || '';
}

// Update Connection Status Display
function updateConnectionStatus(settings) {
    const statusEl = document.getElementById('connection-status');
    const lastSyncEl = document.getElementById('last-sync');
    const syncStatusEl = document.getElementById('sync-status');
    const recordsEl = document.getElementById('records-synced');

    if (!statusEl) return;

    if (settings.isEnabled && settings.accessKeyId && settings.secretAccessKey) {
        statusEl.innerHTML = '<span style="color: #92C83E;"><i class="fas fa-circle" style="font-size: 8px; margin-right: 6px;"></i>Connected</span>';
    } else if (settings.accessKeyId || settings.secretAccessKey) {
        statusEl.innerHTML = '<span style="color: #FFA500;"><i class="fas fa-circle" style="font-size: 8px; margin-right: 6px;"></i>Incomplete</span>';
    } else {
        statusEl.innerHTML = '<span style="color: #999;"><i class="fas fa-circle" style="font-size: 8px; margin-right: 6px;"></i>Not Configured</span>';
    }

    if (settings.lastSync) {
        const lastSync = new Date(settings.lastSync);
        lastSyncEl.textContent = lastSync.toLocaleString();
    } else {
        lastSyncEl.textContent = 'Never';
    }

    if (settings.lastSyncStatus === 'success') {
        syncStatusEl.innerHTML = '<span class="admin-badge admin-badge--success">Success</span>';
    } else if (settings.lastSyncStatus === 'error') {
        syncStatusEl.innerHTML = '<span class="admin-badge admin-badge--danger">Error</span>';
    } else if (settings.lastSyncStatus === 'running') {
        syncStatusEl.innerHTML = '<span class="admin-badge admin-badge--warning">Running...</span>';
    } else {
        syncStatusEl.innerHTML = '<span class="admin-badge admin-badge--secondary">Idle</span>';
    }

    if (recordsEl) {
        recordsEl.textContent = settings.recordsSynced || 0;
    }
}

// Save Settings
async function saveSettings() {
    const saveBtn = document.getElementById('save-settings-btn');
    const originalText = saveBtn.innerHTML;

    try {
        // Validate form
        const form = document.getElementById('amazon-api-form');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        // Show loading state
        saveBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
        saveBtn.disabled = true;

        // Collect form data
        const settings = {
            isEnabled: document.getElementById('is-enabled').checked,
            syncInterval: parseInt(document.getElementById('sync-interval').value) || 24,
            accessKeyId: document.getElementById('access-key-id').value.trim(),
            secretAccessKey: document.getElementById('secret-access-key').value.trim(),
            refreshToken: document.getElementById('refresh-token').value.trim(),
            region: document.getElementById('region').value,
            marketplaceId: document.getElementById('marketplace-id').value,
            syncProducts: document.getElementById('sync-products').checked,
            syncCertificates: document.getElementById('sync-certificates').checked,
            syncCompliance: document.getElementById('sync-compliance').checked,
            webhookUrl: document.getElementById('webhook-url').value.trim(),
            lastSync: currentSettings?.lastSync || null,
            lastSyncStatus: currentSettings?.lastSyncStatus || 'idle',
            recordsSynced: currentSettings?.recordsSynced || 0
        };

        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        // Save via API
        const response = await fetch('/api/admin/amazon/settings', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            credentials: 'same-origin',
            body: JSON.stringify(settings)
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        const result = await response.json();

        if (result.success && result.data) {
            currentSettings = result.data;

            // Update status display
            updateConnectionStatus(currentSettings);

            // Show success - US-015: Always use AdminCore.showNotification
            AdminCore.showNotification(result.message || 'Amazon API settings saved successfully', 'success');
        } else {
            throw new Error(result.message || 'Failed to save settings');
        }

    } catch (error) {
        console.error('Error saving settings:', error);
        AdminCore.showNotification('Failed to save settings: ' + error.message, 'error');
    } finally {
        saveBtn.innerHTML = originalText;
        saveBtn.disabled = false;
    }
}

// Test Connection
async function testConnection() {
    const testBtn = document.getElementById('test-connection-btn');
    const originalText = testBtn.innerHTML;

    try {
        // Check if credentials are entered
        const accessKeyId = document.getElementById('access-key-id').value.trim();
        const secretAccessKey = document.getElementById('secret-access-key').value.trim();
        const refreshToken = document.getElementById('refresh-token').value.trim();

        if (!accessKeyId || !secretAccessKey || !refreshToken) {
            AdminCore.showNotification('Please enter all required credentials first', 'warning');
            return;
        }

        // Show loading state
        testBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Testing...';
        testBtn.disabled = true;

        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        // Test via API
        const testData = {
            accessKeyId,
            secretAccessKey,
            refreshToken,
            region: document.getElementById('region').value,
            marketplaceId: document.getElementById('marketplace-id').value,
            isEnabled: true,
            syncIntervalHours: 24,
            syncProducts: true,
            syncCertificates: true,
            syncCompliance: true
        };

        const response = await fetch('/api/admin/amazon/test', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            credentials: 'same-origin',
            body: JSON.stringify(testData)
        });

        const result = await response.json();

        if (result.success) {
            AdminCore.showNotification(result.message || 'Connection test successful!', 'success');
        } else {
            throw new Error(result.message || 'Connection test failed');
        }

    } catch (error) {
        console.error('Connection test failed:', error);
        AdminCore.showNotification('Connection test failed: ' + error.message, 'error');
    } finally {
        testBtn.innerHTML = originalText;
        testBtn.disabled = false;
    }
}

// Trigger Immediate Sync
async function triggerSync() {
    const syncBtn = document.getElementById('sync-now-btn');
    const originalText = syncBtn.innerHTML;

    try {
        // Check if enabled and configured
        if (!currentSettings || !currentSettings.isEnabled) {
            AdminCore.showNotification('Please enable and configure the integration first', 'warning');
            return;
        }

        const confirmed = await AdminCore.showConfirmation({
            title: 'Trigger Sync',
            message: 'This will immediately sync all certificates with Amazon. This may take several minutes. Continue?',
            confirmText: 'Start Sync',
            type: 'info'
        });

        if (!confirmed) return;

        // Show loading state
        syncBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Syncing...';
        syncBtn.disabled = true;

        // Update status
        if (currentSettings) {
            currentSettings.lastSyncStatus = 'running';
            updateConnectionStatus(currentSettings);
        }

        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        // Trigger sync via API
        const response = await fetch('/api/admin/amazon/sync', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            credentials: 'same-origin'
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        const result = await response.json();

        if (result.success) {
            // Reload settings to get updated status
            await loadSettings();

            // Reload sync history
            await loadSyncHistory();

            AdminCore.showNotification(result.message || 'Sync completed successfully', 'success');
        } else {
            throw new Error(result.message || 'Sync failed');
        }

    } catch (error) {
        console.error('Sync failed:', error);

        // Update error status
        if (currentSettings) {
            currentSettings.lastSyncStatus = 'error';
            updateConnectionStatus(currentSettings);
        }

        AdminCore.showNotification('Sync failed: ' + error.message, 'error');
    } finally {
        syncBtn.innerHTML = originalText;
        syncBtn.disabled = false;
    }
}

// Toggle Secret Visibility
function toggleSecretVisibility() {
    const secretInput = document.getElementById('secret-access-key');
    const toggleBtn = document.getElementById('toggle-secret-btn');

    if (secretInput.type === 'password') {
        secretInput.type = 'text';
        toggleBtn.innerHTML = '<i class="fas fa-eye-slash"></i>';
    } else {
        secretInput.type = 'password';
        toggleBtn.innerHTML = '<i class="fas fa-eye"></i>';
    }
}

// Update Marketplace Options based on Region
function updateMarketplaceOptions() {
    const region = document.getElementById('region').value;
    const marketplaceSelect = document.getElementById('marketplace-id');

    if (!marketplaceSelect) return;

    // Clear current options except first
    marketplaceSelect.innerHTML = '<option value="">Select Marketplace</option>';

    const marketplaces = {
        'na': [
            { value: 'ATVPDKIKX0DER', label: 'US - ATVPDKIKX0DER' },
            { value: 'A2EUQ1WTGCTBG2', label: 'CA - A2EUQ1WTGCTBG2' },
            { value: 'A1AM78C64UM0Y8', label: 'MX - A1AM78C64UM0Y8' }
        ],
        'eu': [
            { value: 'A1F83G8C2ARO7P', label: 'UK - A1F83G8C2ARO7P' },
            { value: 'A13V1IB3VIYZZH', label: 'FR - A13V1IB3VIYZZH' },
            { value: 'A1PA6795UKMFR9', label: 'DE - A1PA6795UKMFR9' },
            { value: 'APJ6JRA9NG5V4', label: 'IT - APJ6JRA9NG5V4' },
            { value: 'A1RKKUPIHCS9HS', label: 'ES - A1RKKUPIHCS9HS' }
        ],
        'fe': [
            { value: 'A1VC38T7YXB528', label: 'JP - A1VC38T7YXB528' },
            { value: 'A39IBJ37TRP1C6', label: 'AU - A39IBJ37TRP1C6' }
        ]
    };

    if (region && marketplaces[region]) {
        marketplaces[region].forEach(marketplace => {
            const option = document.createElement('option');
            option.value = marketplace.value;
            option.textContent = marketplace.label;
            marketplaceSelect.appendChild(option);
        });
    }
}

// Load Sync History
async function loadSyncHistory() {
    try {
        const response = await fetch('/api/admin/amazon/history?count=10', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'same-origin'
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success && result.data) {
            syncHistoryData = result.data;
            renderSyncHistory();
        }
    } catch (error) {
        console.error('Error loading sync history:', error);
        // Don't show error notification for history load failure
    }
}

// Add Sync History Entry
function addSyncHistoryEntry(entry) {
    syncHistoryData.unshift(entry);

    // Keep only last 10 entries
    if (syncHistoryData.length > 10) {
        syncHistoryData = syncHistoryData.slice(0, 10);
    }

    localStorage.setItem('amazonSyncHistory', JSON.stringify(syncHistoryData));
    renderSyncHistory();
}

// Render Sync History
function renderSyncHistory() {
    const tbody = document.querySelector('#sync-history-table tbody');
    if (!tbody) return;

    if (syncHistoryData.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 40px; color: #666;">
                    <i class="fas fa-clock" style="font-size: 48px; color: #ccc; margin-bottom: 12px;"></i>
                    <p style="margin: 0;">No sync history available</p>
                </td>
            </tr>
        `;
        return;
    }

    tbody.innerHTML = syncHistoryData.map(entry => {
        const timestamp = new Date(entry.timestamp).toLocaleString();
        let statusBadge;

        if (entry.status === 'success') {
            statusBadge = '<span class="admin-badge admin-badge--success">Success</span>';
        } else if (entry.status === 'error') {
            statusBadge = '<span class="admin-badge admin-badge--danger">Error</span>';
        } else if (entry.status === 'partial') {
            statusBadge = '<span class="admin-badge admin-badge--warning">Partial</span>';
        } else {
            statusBadge = '<span class="admin-badge admin-badge--secondary">Running</span>';
        }

        const duration = entry.durationSeconds ? `${entry.durationSeconds.toFixed(1)}s` : 'N/A';
        const records = entry.recordsProcessed || entry.records || 0;

        return `
            <tr>
                <td class="admin-table-td">${timestamp}</td>
                <td class="admin-table-td">${entry.type || 'Auto'}</td>
                <td class="admin-table-td">${records}</td>
                <td class="admin-table-td">${duration}</td>
                <td class="admin-table-td">${statusBadge}</td>
                <td class="admin-table-td">${entry.message || ''}</td>
            </tr>
        `;
    }).join('');
}

// Export function for initialization
window.initializeAmazonAPI = initializeAmazonAPI;
