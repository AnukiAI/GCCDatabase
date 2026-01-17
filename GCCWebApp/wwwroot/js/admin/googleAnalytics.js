// Google Analytics Management JavaScript

document.addEventListener('DOMContentLoaded', function() {
    attachEventListeners();
    // Update script preview with initial values from server-rendered page
    updateScriptPreview();
});

function attachEventListeners() {
    const saveBtn = document.getElementById('save-settings-btn');
    const trackingInput = document.getElementById('ga-tracking-id');
    const enableTracking = document.getElementById('enable-tracking');

    if (saveBtn) {
        saveBtn.addEventListener('click', saveSettings);
    }

    if (trackingInput) {
        trackingInput.addEventListener('input', function() {
            validateTrackingId();
            // Update status display when tracking ID changes
            const enabled = document.getElementById('enable-tracking').checked;
            if (enabled) {
                toggleTracking();
            }
        });
    }

    if (enableTracking) {
        enableTracking.addEventListener('change', toggleTracking);
    }
}

async function saveSettings() {
    const trackingId = document.getElementById('ga-tracking-id').value.trim();
    const enabled = document.getElementById('enable-tracking').checked;
    const customScript = document.getElementById('custom-script').value.trim();
    const anonymizeIp = document.getElementById('anonymize-ip').checked;
    const enhancedLink = document.getElementById('enhanced-link').checked;
    const debugMode = document.getElementById('debug-mode').checked;

    // Validate tracking ID format if enabled
    if (enabled && trackingId && !validateTrackingIdFormat(trackingId)) {
        AdminCore.showNotification('Please enter a valid Google Analytics Tracking ID (e.g., G-XXXXXXXXXX)', 'error');
        return;
    }

    // Prepare settings object
    const settings = {
        trackingId: trackingId,
        isEnabled: enabled,
        customScript: customScript,
        anonymizeIp: anonymizeIp,
        enhancedLinkAttribution: enhancedLink,
        debugMode: debugMode
    };

    try {
        const response = await fetch('/Admin/SaveGoogleAnalytics', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify(settings)
        });

        const result = await response.json();

        if (result.success) {
            AdminCore.showNotification('Settings saved successfully!', 'success');

            // Add to history
            const action = enabled ? 'Enabled' : 'Disabled';
            addToHistory(trackingId || 'No tracking ID', action);

            // Update status display
            updateStatusDisplay();
        } else {
            AdminCore.showNotification(result.message || 'Error saving settings', 'error');
        }
    } catch (error) {
        AdminCore.showNotification('Error saving settings: ' + error.message, 'error');
    }
}

function validateTrackingId() {
    const trackingId = document.getElementById('ga-tracking-id').value.trim();
    const isValid = validateTrackingIdFormat(trackingId);
    
    // Update script preview
    updateScriptPreview();
    
    return isValid;
}

function validateTrackingIdFormat(trackingId) {
    // Google Analytics 4 format: G-XXXXXXX (G- followed by 7-12 alphanumeric characters)
    const ga4Pattern = /^G-[A-Z0-9]{7,12}$/i;
    // Universal Analytics format: UA-XXXXXXXX-X (for backward compatibility)
    const uaPattern = /^UA-\d{6,9}-\d{1,2}$/;

    return ga4Pattern.test(trackingId) || uaPattern.test(trackingId);
}

function toggleTracking() {
    const checkbox = document.getElementById('enable-tracking');
    const trackingInput = document.getElementById('ga-tracking-id');
    const toggleContainer = document.getElementById('toggle-container');
    const statusDisplay = document.getElementById('status-display');
    const statusBadge = document.getElementById('status-badge');
    const trackingId = trackingInput.value.trim();

    if (checkbox.checked) {
        toggleContainer.classList.add('enabled');
        if (trackingId) {
            statusDisplay.textContent = `Enabled - Tracking ${trackingId}`;
        } else {
            statusDisplay.textContent = 'Enabled - No tracking ID configured';
        }
        if (statusBadge) {
            statusBadge.className = 'ga-status-badge enabled';
            statusBadge.innerHTML = 'Status: <strong>Enabled</strong>';
        }
    } else {
        toggleContainer.classList.remove('enabled');
        statusDisplay.textContent = 'Disabled - Analytics not being collected';
        if (statusBadge) {
            statusBadge.className = 'ga-status-badge disabled';
            statusBadge.innerHTML = 'Status: <strong>Disabled</strong>';
        }
    }

    updateScriptPreview();
}

function updateScriptPreview() {
    const trackingInput = document.getElementById('ga-tracking-id');
    const scriptPreview = document.getElementById('script-preview');
    const trackingId = trackingInput.value.trim() || 'G-XXXXXXXXXX';
    
    scriptPreview.innerHTML = `&lt;script async src="https://www.googletagmanager.com/gtag/js?id=${trackingId}"&gt;&lt;/script&gt;
&lt;script&gt;
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', '${trackingId}');
&lt;/script&gt;`;
}

function updateStatusDisplay() {
    const enabled = document.getElementById('enable-tracking').checked;
    const trackingId = document.getElementById('ga-tracking-id').value.trim();
    const statusDisplay = document.getElementById('status-display');
    const toggleContainer = document.getElementById('toggle-container');
    const statusBadge = document.getElementById('status-badge');

    if (enabled && trackingId) {
        statusDisplay.textContent = `Enabled - Tracking ${trackingId}`;
        toggleContainer.classList.add('enabled');
    } else if (enabled && !trackingId) {
        statusDisplay.textContent = 'Enabled - No tracking ID configured';
        toggleContainer.classList.add('enabled');
    } else {
        statusDisplay.textContent = 'Disabled - Analytics not being collected';
        toggleContainer.classList.remove('enabled');
    }

    // Update status badge
    if (statusBadge) {
        if (enabled) {
            statusBadge.className = 'ga-status-badge enabled';
            statusBadge.innerHTML = 'Status: <strong>Enabled</strong>';
        } else {
            statusBadge.className = 'ga-status-badge disabled';
            statusBadge.innerHTML = 'Status: <strong>Disabled</strong>';
        }
    }
}

function addToHistory(trackingId, action) {
    const historyLog = document.getElementById('history-log');
    const currentDate = new Date().toLocaleString();
    
    // Remove sample data row if it exists
    const sampleRow = historyLog.querySelector('tr');
    if (sampleRow && sampleRow.querySelector('td').textContent.includes('2025-04-29')) {
        sampleRow.remove();
    }
    
    // Create new history entry
    const newRow = document.createElement('tr');
    newRow.innerHTML = `
        <td>${currentDate}</td>
        <td>Admin</td>
        <td>${trackingId}</td>
        <td>${action}</td>
    `;
    
    // Insert at the beginning of the table
    historyLog.insertBefore(newRow, historyLog.firstChild);
    
    // Keep only last 10 entries
    while (historyLog.children.length > 10) {
        historyLog.removeChild(historyLog.lastChild);
    }
}

async function showNotification(message, type = 'info') {
    // Route all notifications to AdminCore modals for consistency
    const titleMap = {
        'success': 'Success',
        'error': 'Error',
        'warning': 'Warning',
        'info': 'Information'
    };
    const title = titleMap[type] || 'Information';

    switch (type) {
        case 'success':
            await AdminCore.showSuccessModal({ title, message });
            break;
        case 'error':
            await AdminCore.showErrorModal({ title, message });
            break;
        case 'warning':
            await AdminCore.showWarningModal({ title, message });
            break;
        case 'info':
        default:
            await AdminCore.showInfoModal({ title, message });
            break;
    }
}

function exportSettings() {
    // Export current form values as JSON
    const settings = {
        trackingId: document.getElementById('ga-tracking-id').value.trim(),
        isEnabled: document.getElementById('enable-tracking').checked,
        customScript: document.getElementById('custom-script').value.trim(),
        anonymizeIp: document.getElementById('anonymize-ip').checked,
        enhancedLinkAttribution: document.getElementById('enhanced-link').checked,
        debugMode: document.getElementById('debug-mode').checked
    };

    if (!settings.trackingId) {
        AdminCore.showNotification('No tracking ID configured to export.', 'warning');
        return;
    }

    const blob = new Blob([JSON.stringify(settings, null, 2)], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `google-analytics-settings-${new Date().toISOString().slice(0, 10)}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
}

function importSettings() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = function(e) {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function(e) {
                try {
                    const settings = JSON.parse(e.target.result);

                    // Populate form with imported settings
                    if (settings.trackingId) {
                        document.getElementById('ga-tracking-id').value = settings.trackingId;
                    }
                    document.getElementById('enable-tracking').checked = settings.isEnabled || false;
                    if (settings.customScript) {
                        document.getElementById('custom-script').value = settings.customScript;
                    }
                    document.getElementById('anonymize-ip').checked = settings.anonymizeIp || false;
                    document.getElementById('enhanced-link').checked = settings.enhancedLinkAttribution || false;
                    document.getElementById('debug-mode').checked = settings.debugMode || false;

                    // Update UI
                    toggleTracking();
                    updateScriptPreview();

                    AdminCore.showNotification('Settings imported. Click Save to apply changes.', 'success');
                } catch (error) {
                    AdminCore.showNotification('Invalid settings file.', 'error');
                }
            };
            reader.readAsText(file);
        }
    };
    input.click();
}