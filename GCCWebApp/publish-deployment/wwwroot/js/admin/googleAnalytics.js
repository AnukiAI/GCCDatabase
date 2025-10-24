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

function saveSettings() {
    const trackingId = document.getElementById('ga-tracking-id').value.trim();
    const enabled = document.getElementById('enable-tracking').checked;

    // Validate tracking ID format
    if (enabled && trackingId && !validateTrackingIdFormat(trackingId)) {
        AdminCore.showNotification('Please enter a valid Google Analytics Tracking ID (e.g., G-XXXXXXXXXX)', 'error');
        return;
    }

    // Prepare settings object
    const settings = {
        trackingId: trackingId,
        enabled: enabled,
        savedAt: new Date().toISOString(),
        savedBy: 'Admin' // In real app, get from current user
    };

    // Save to localStorage (in real app, save to server)
    localStorage.setItem('googleAnalyticsSettings', JSON.stringify(settings));

    // Show success message
    showNotification('Settings saved successfully!', 'success');
    
    // Add to history
    const action = enabled ? 'Enabled' : 'Disabled';
    addToHistory(trackingId || 'No tracking ID', action);

    // Update status display
    updateStatusDisplay();
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

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <i class="fas fa-check-circle"></i>
            <span>${message}</span>
        </div>
    `;
    
    // Add styles if not already present
    if (!document.querySelector('#notification-styles')) {
        const styles = document.createElement('style');
        styles.id = 'notification-styles';
        styles.innerHTML = `
            .notification {
                position: fixed;
                top: 20px;
                right: 20px;
                background: #28a745;
                color: white;
                padding: 15px 20px;
                border-radius: 5px;
                box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                z-index: 1000;
                opacity: 0;
                transform: translateX(300px);
                transition: all 0.3s ease;
            }
            .notification.show {
                opacity: 1;
                transform: translateX(0);
            }
            .notification.notification-error {
                background: #dc3545;
            }
            .notification.notification-warning {
                background: #ffc107;
                color: #212529;
            }
            .notification-content {
                display: flex;
                align-items: center;
                gap: 10px;
            }
        `;
        document.head.appendChild(styles);
    }
    
    // Add to document
    document.body.appendChild(notification);
    
    // Show notification
    setTimeout(() => notification.classList.add('show'), 100);
    
    // Remove after 3 seconds
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => document.body.removeChild(notification), 300);
    }, 3000);
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