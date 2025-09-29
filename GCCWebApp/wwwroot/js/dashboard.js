// Dashboard-specific JavaScript functionality

// Improved stat download functionality with better error handling
async function downloadStat(statType) {
    console.log('Downloading stat:', statType);

    try {
        // Use fetch to handle authentication properly - request Excel format
        const response = await fetch(`/Export/DownloadStat?type=${statType}&format=excel`, {
            method: 'GET',
            credentials: 'same-origin', // Include cookies for authentication
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            // Get the Excel/CSV content
            const blob = await response.blob();
            
            // Create download link
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `${statType}-${new Date().toISOString().split('T')[0]}.xlsx`;
            link.style.display = 'none';
            
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            
            // Clean up the blob URL
            window.URL.revokeObjectURL(url);

            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast(`Downloaded ${statType.replace(/-/g, ' ')} data`, 'success');
            }
        } else if (response.status === 302 || response.status === 401) {
            console.error('Authentication required');
            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast('Please login to download data', 'error');
            }
        } else {
            console.error('Download failed:', response.status, response.statusText);
            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast('Download failed. Please try again.', 'error');
            }
        }
    } catch (error) {
        console.error('Download error:', error);
        if (window.GreenCircle?.showToast) {
            window.GreenCircle.showToast('Download failed. Please try again.', 'error');
        }
    }
}

// Table download functionality (matching async pattern)
async function downloadTable(tableType) {
    console.log('Downloading table:', tableType);

    try {
        const response = await fetch(`/Export/DownloadTable?type=${tableType}&format=excel`, {
            method: 'GET',
            credentials: 'same-origin',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const blob = await response.blob();
            
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `${tableType}-table-${new Date().toISOString().split('T')[0]}.xlsx`;
            link.style.display = 'none';
            
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            
            window.URL.revokeObjectURL(url);

            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast(`Downloaded ${tableType.replace(/-/g, ' ')} table`, 'success');
            }
        } else if (response.status === 302 || response.status === 401) {
            console.error('Authentication required');
            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast('Please login to download data', 'error');
            }
        } else {
            console.error('Download failed:', response.status, response.statusText);
            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast('Download failed. Please try again.', 'error');
            }
        }
    } catch (error) {
        console.error('Download error:', error);
        if (window.GreenCircle?.showToast) {
            window.GreenCircle.showToast('Download failed. Please try again.', 'error');
        }
    }
}

// Dashboard functionality
function exportMetric() {
    console.log('Exporting selected metric...');
    window.GreenCircle?.showToast('Export started. You will be notified when complete.', 'info');
}

async function showExpiringCerts(months) {
    const period = months === 1 ? '30 days' : `${months} months`;
    console.log(`Generating report for certificates expiring in next ${period}...`);

    try {
        const reportType = months === 1 ? 'expiring-30-days' : `expiring-${months}-months`;
        const response = await fetch(`/Export/DownloadReport?type=${reportType}&format=excel`, {
            method: 'GET',
            credentials: 'same-origin',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const blob = await response.blob();

            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `certificates-expiring-next-${period.replace(' ', '-')}-${new Date().toISOString().split('T')[0]}.xlsx`;
            link.style.display = 'none';

            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            window.URL.revokeObjectURL(url);

            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast(`Downloaded report: Certificates expiring in next ${period}`, 'success');
            }
        } else if (response.status === 302 || response.status === 401) {
            console.error('Authentication required');
            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast('Please login to download report', 'error');
            }
        } else {
            console.error('Download failed:', response.status, response.statusText);
            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast('Failed to generate report. Please try again.', 'error');
            }
        }
    } catch (error) {
        console.error('Download error:', error);
        if (window.GreenCircle?.showToast) {
            window.GreenCircle.showToast('Failed to generate report. Please try again.', 'error');
        }
    }
}

async function showActiveCerts() {
    console.log('Generating report for current active certificates...');

    try {
        const response = await fetch(`/Export/DownloadReport?type=active-certificates&format=excel`, {
            method: 'GET',
            credentials: 'same-origin',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const blob = await response.blob();

            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `active-certificates-${new Date().toISOString().split('T')[0]}.xlsx`;
            link.style.display = 'none';

            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            window.URL.revokeObjectURL(url);

            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast('Downloaded report: Current active certificates', 'success');
            }
        } else if (response.status === 302 || response.status === 401) {
            console.error('Authentication required');
            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast('Please login to download report', 'error');
            }
        } else {
            console.error('Download failed:', response.status, response.statusText);
            if (window.GreenCircle?.showToast) {
                window.GreenCircle.showToast('Failed to generate report. Please try again.', 'error');
            }
        }
    } catch (error) {
        console.error('Download error:', error);
        if (window.GreenCircle?.showToast) {
            window.GreenCircle.showToast('Failed to generate report. Please try again.', 'error');
        }
    }
}

// Setup event delegation for all download buttons
document.addEventListener('DOMContentLoaded', function() {
    console.log('Setting up download button handlers');
    
    // Single click handler for all download buttons
    document.addEventListener('click', function(event) {
        // Check if clicked element or its parent is a stat download button
        const statButton = event.target.closest('.stat-download-icon');
        if (statButton) {
            event.preventDefault();
            event.stopPropagation();
            const statType = statButton.dataset.statType;
            console.log('Stat download clicked:', statType);
            if (statType) {
                downloadStat(statType);
            }
            return;
        }
        
        // Check if clicked element or its parent is a table download button
        const tableButton = event.target.closest('.table-download-icon');
        if (tableButton) {
            event.preventDefault();
            event.stopPropagation();
            const tableType = tableButton.dataset.tableType;
            console.log('Table download clicked:', tableType);
            if (tableType) {
                downloadTable(tableType);
            }
            return;
        }
    });

    // Animate stat cards on scroll
    const statCards = document.querySelectorAll('.stat-card');

    // Add stagger animation
    statCards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.1}s`;
        card.style.animation = 'slideUp 0.6s ease-out forwards';
    });

    // Add number counting animation
    const statValues = document.querySelectorAll('.stat-value');
    statValues.forEach(valueElement => {
        const finalValue = parseInt(valueElement.textContent.replace(/,/g, ''));
        let currentValue = 0;
        const increment = finalValue / 50;
        const timer = setInterval(() => {
            currentValue += increment;
            if (currentValue >= finalValue) {
                valueElement.textContent = finalValue.toLocaleString();
                clearInterval(timer);
            } else {
                valueElement.textContent = Math.floor(currentValue).toLocaleString();
            }
        }, 30);
    });
});

// Export functions for global use
window.DashboardFunctions = {
    downloadStat,
    downloadTable,
    exportMetric,
    showExpiringCerts,
    showActiveCerts
};