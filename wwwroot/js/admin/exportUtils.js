/**
 * Export Utilities for Admin Pages
 * Provides common export functionality for CSV, JSON, and Excel formats
 */

class ExportUtils {
    /**
     * Export data to CSV format
     * @param {Array} data - Array of objects to export
     * @param {Array} columns - Array of column definitions {key: string, header: string}
     * @param {string} filename - Base filename without extension
     * @param {Function} toastCallback - Optional callback for showing notifications
     */
    static exportToCSV(data, columns, filename, toastCallback = null) {
        console.log('exportToCSV called with:', { data, columns, filename });
        
        if (!data || data.length === 0) {
            console.log('No data to export');
            if (toastCallback) toastCallback('No data to export', 'warning');
            return;
        }

        try {
            // Create CSV header
            const headers = columns.map(col => col.header).join(',');
            console.log('CSV headers:', headers);
            
            // Create CSV rows
            const rows = data.map(item => {
                return columns.map(col => {
                    let value = item[col.key];
                    
                    // Handle arrays (like permissions, companies)
                    if (Array.isArray(value)) {
                        value = value.join('; ');
                    }
                    
                    // Handle null/undefined
                    if (value === null || value === undefined) {
                        value = '';
                    }
                    
                    // Escape quotes and wrap in quotes if contains comma or quote
                    value = String(value);
                    if (value.includes('"')) {
                        value = value.replace(/"/g, '""');
                    }
                    if (value.includes(',') || value.includes('"') || value.includes('\n')) {
                        value = `"${value}"`;
                    }
                    
                    return value;
                }).join(',');
            });

            const csv = [headers, ...rows].join('\n');
            console.log('Generated CSV content:', csv);
            
            // Create and download file
            const fullFilename = `${filename}_${this._getDateString()}.csv`;
            console.log('Downloading file:', fullFilename);
            this._downloadFile(csv, fullFilename, 'text/csv;charset=utf-8;');
            
            if (toastCallback) toastCallback('Data exported to CSV successfully', 'success');
        } catch (error) {
            console.error('CSV export error:', error);
            if (toastCallback) toastCallback('Export failed', 'error');
        }
    }

    /**
     * Export data to JSON format
     * @param {Array} data - Array of objects to export
     * @param {string} filename - Base filename without extension
     * @param {Function} toastCallback - Optional callback for showing notifications
     */
    static exportToJSON(data, filename, toastCallback = null) {
        if (!data || data.length === 0) {
            if (toastCallback) toastCallback('No data to export', 'warning');
            return;
        }

        try {
            const json = JSON.stringify(data, null, 2);
            this._downloadFile(json, `${filename}_${this._getDateString()}.json`, 'application/json;charset=utf-8;');
            
            if (toastCallback) toastCallback('Data exported to JSON successfully', 'success');
        } catch (error) {
            console.error('JSON export error:', error);
            if (toastCallback) toastCallback('Export failed', 'error');
        }
    }

    /**
     * Export data to Excel format (using CSV with .xlsx extension for simplicity)
     * @param {Array} data - Array of objects to export
     * @param {Array} columns - Array of column definitions
     * @param {string} filename - Base filename without extension
     * @param {Function} toastCallback - Optional callback for showing notifications
     */
    static exportToExcel(data, columns, filename, toastCallback = null) {
        // For now, use CSV format but with .xlsx extension
        // In a real implementation, you'd use a library like SheetJS
        if (!data || data.length === 0) {
            if (toastCallback) toastCallback('No data to export', 'warning');
            return;
        }

        try {
            // Create tab-separated values for better Excel compatibility
            const headers = columns.map(col => col.header).join('\t');
            const rows = data.map(item => {
                return columns.map(col => {
                    let value = item[col.key];
                    
                    if (Array.isArray(value)) {
                        value = value.join('; ');
                    }
                    
                    if (value === null || value === undefined) {
                        value = '';
                    }
                    
                    return String(value);
                }).join('\t');
            });

            const tsv = [headers, ...rows].join('\n');
            this._downloadFile(tsv, `${filename}_${this._getDateString()}.xlsx`, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet');
            
            if (toastCallback) toastCallback('Data exported to Excel successfully', 'success');
        } catch (error) {
            console.error('Excel export error:', error);
            if (toastCallback) toastCallback('Export failed', 'error');
        }
    }

    /**
     * Show export format selection modal
     * @param {Array} data - Data to export
     * @param {Array} columns - Column definitions
     * @param {string} filename - Base filename
     * @param {Function} toastCallback - Toast notification callback
     */
    static showExportModal(data, columns, filename, toastCallback = null) {
        const modal = this._createExportModal();
        document.body.appendChild(modal);
        
        // Handle format selection
        modal.querySelectorAll('.export-format-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                // Find the button element (in case clicked on child element)
                const button = e.target.closest('.export-format-btn');
                const format = button.dataset.format;
                
                console.log('Export format selected:', format);
                console.log('Data to export:', data);
                
                switch (format) {
                    case 'csv':
                        this.exportToCSV(data, columns, filename, toastCallback);
                        break;
                    case 'json':
                        this.exportToJSON(data, filename, toastCallback);
                        break;
                    case 'excel':
                        this.exportToExcel(data, columns, filename, toastCallback);
                        break;
                    default:
                        console.error('Unknown export format:', format);
                        if (toastCallback) toastCallback('Unknown export format', 'error');
                        return;
                }
                
                this._closeExportModal(modal);
            });
        });
        
        // Handle modal close
        modal.querySelector('.export-modal-close').addEventListener('click', () => {
            this._closeExportModal(modal);
        });
        
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                this._closeExportModal(modal);
            }
        });
    }

    /**
     * Quick CSV export with default settings
     * @param {Array} data - Data to export
     * @param {string} filename - Base filename
     * @param {Function} toastCallback - Toast callback
     */
    static quickCSVExport(data, filename, toastCallback = null) {
        if (!data || data.length === 0) {
            if (toastCallback) toastCallback('No data to export', 'warning');
            return;
        }

        // Auto-generate columns from first object
        const firstItem = data[0];
        const columns = Object.keys(firstItem).map(key => ({
            key: key,
            header: key.charAt(0).toUpperCase() + key.slice(1).replace(/([A-Z])/g, ' $1')
        }));

        this.exportToCSV(data, columns, filename, toastCallback);
    }

    // Private helper methods
    static _downloadFile(content, filename, mimeType) {
        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        
        URL.revokeObjectURL(url);
    }

    static _getDateString() {
        return new Date().toISOString().split('T')[0];
    }

    static _createExportModal() {
        const modal = document.createElement('div');
        modal.className = 'export-modal';
        modal.innerHTML = `
            <div class="export-modal-overlay"></div>
            <div class="export-modal-content">
                <div class="export-modal-header">
                    <h3>Export Data</h3>
                    <span class="export-modal-close">&times;</span>
                </div>
                <div class="export-modal-body">
                    <p>Choose export format:</p>
                    <div class="export-format-buttons">
                        <button class="export-format-btn" data-format="csv">
                            <i class="fas fa-file-csv"></i>
                            <span>CSV</span>
                            <small>Comma-separated values</small>
                        </button>
                        <button class="export-format-btn" data-format="json">
                            <i class="fas fa-file-code"></i>
                            <span>JSON</span>
                            <small>JavaScript Object Notation</small>
                        </button>
                        <button class="export-format-btn" data-format="excel">
                            <i class="fas fa-file-excel"></i>
                            <span>Excel</span>
                            <small>Microsoft Excel format</small>
                        </button>
                    </div>
                </div>
            </div>
        `;

        // Add basic styles
        const style = document.createElement('style');
        style.textContent = `
            .export-modal {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.5);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 10000;
            }
            .export-modal-content {
                background: white;
                border-radius: 8px;
                padding: 20px;
                max-width: 500px;
                width: 90%;
                box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
            }
            .export-modal-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 20px;
                border-bottom: 1px solid #eee;
                padding-bottom: 10px;
            }
            .export-modal-close {
                font-size: 24px;
                cursor: pointer;
                color: #999;
            }
            .export-modal-close:hover {
                color: #333;
            }
            .export-format-buttons {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
                gap: 15px;
            }
            .export-format-btn {
                display: flex;
                flex-direction: column;
                align-items: center;
                padding: 20px;
                border: 2px solid #e0e0e0;
                border-radius: 8px;
                background: white;
                cursor: pointer;
                transition: all 0.3s ease;
            }
            .export-format-btn:hover {
                border-color: #007bff;
                background: #f8f9fa;
            }
            .export-format-btn i {
                font-size: 24px;
                margin-bottom: 8px;
                color: #007bff;
            }
            .export-format-btn span {
                font-weight: 600;
                margin-bottom: 4px;
            }
            .export-format-btn small {
                color: #666;
                text-align: center;
            }
        `;
        modal.appendChild(style);

        return modal;
    }

    static _closeExportModal(modal) {
        if (modal && modal.parentNode) {
            modal.parentNode.removeChild(modal);
        }
    }
}

// Export for use in other scripts
window.ExportUtils = ExportUtils;