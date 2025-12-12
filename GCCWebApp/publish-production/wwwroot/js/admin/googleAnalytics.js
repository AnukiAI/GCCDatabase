// Google Analytics Management JavaScript

document.addEventListener('DOMContentLoaded', function() {
    loadSettings();
    attachEventListeners();
});

function attachEventListeners() {
    const saveBtn = document.getElementById('save-settings-btn');
    const trackingInput = document.getElementById('ga-tracking-id');
    const enableTracking = document.getElementById('enable-tracking');

    if (saveBtn) {
        saveBtn.addEventListener('click', saveSettings);
    }

    if (trackingInput) {
        trackingInput.addEventListener('input', validateTrackingId);
    }

    if (enableTracking) {
        enableTracking.addEventListener('change', toggleTracking);
    }
}

function loadSettings() {
    // Load settings from localStorage or server
    const savedSettings = localStorage.getItem('googleAnalyticsSettings');
    
    if (savedSettings) {
        const settings = JSON.parse(savedSettings);
        
        document.getElementById('ga-tracking-id').value = settings.trackingId || '';
        document.getElementById('enable-tracking').checked = settings.enabled || false;
        
        toggleTracking();
        updateScriptPreview();
        addToHistory('Settings loaded from storage', 'Loaded');
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
    // Google Analytics 4 format: G-XXXXXXXXXX (G- followed by 10 alphanumeric characters)
    const ga4Pattern = /^G-[A-Z0-9]{10}$/;
    // Universal Analytics format: UA-XXXXXXXX-X (for backward compatibility)
    const uaPattern = /^UA-\d{6,9}-\d{1,2}$/;
    
    return ga4Pattern.test(trackingId) || uaPattern.test(trackingId);
}

function toggleTracking() {
    const checkbox = document.getElementById('enable-tracking');
    const trackingInput = document.getElementById('ga-tracking-id');
    const statusDisplay = document.getElementById('status-display');
    
    if (checkbox.checked) {
        statusDisplay.className = 'status-display enabled';
        statusDisplay.innerHTML = 'Tracking is currently <strong>enabled</strong>.';
        trackingInput.disabled = false;
    } else {
        statusDisplay.className = 'status-display disabled';
        statusDisplay.innerHTML = 'Tracking is currently <strong>disabled</strong>.';
        trackingInput.disabled = false; // Keep enabled so users can enter ID before enabling
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
    
    if (enabled && trackingId) {
        statusDisplay.className = 'status-display enabled';
        statusDisplay.innerHTML = `Tracking is currently <strong>enabled</strong> for ${trackingId}.`;
    } else if (enabled && !trackingId) {
        statusDisplay.className = 'status-display warning';
        statusDisplay.innerHTML = 'Tracking is <strong>enabled</strong> but no Tracking ID is set.';
    } else {
        statusDisplay.className = 'status-display disabled';
        statusDisplay.innerHTML = 'Tracking is currently <strong>disabled</strong>.';
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
    const settings = localStorage.getItem('googleAnalyticsSettings');

    if (!settings) {
        AdminCore.showNotification('No settings to export.', 'warning');
        return;
    }
    
    const blob = new Blob([settings], { type: 'application/json' });
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
                    localStorage.setItem('googleAnalyticsSettings', JSON.stringify(settings));
                    loadSettings();
                    showNotification('Settings imported successfully!', 'success');
                } catch (error) {
                    showNotification('Invalid settings file.', 'error');
                }
            };
            reader.readAsText(file);
        }
    };
    input.click();
}

// Auto-save functionality
let autoSaveTimeout;
function autoSave() {
    clearTimeout(autoSaveTimeout);
    autoSaveTimeout = setTimeout(() => {
        const trackingId = document.getElementById('ga-tracking-id').value.trim();
        if (trackingId) {
            saveSettings();
        }
    }, 2000); // Auto-save 2 seconds after user stops typing
}

// Attach auto-save to tracking ID input
document.addEventListener('DOMContentLoaded', function() {
    const trackingInput = document.getElementById('ga-tracking-id');
    if (trackingInput) {
        trackingInput.addEventListener('input', autoSave);
    }
});