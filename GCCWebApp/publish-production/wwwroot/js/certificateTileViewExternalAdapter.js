/**
 * Certificate Tile View External Adapter
 * Extends the certificateslist.js functionality for External users
 * Provides the same functionality as Internal view but restricts editing
 */

// Wait for certificateslist.js to load
document.addEventListener('DOMContentLoaded', () => {
    // Check if CertificateList class is available
    if (typeof CertificateList !== 'undefined') {
        // Extend the CertificateList class for external users
        class CertificateListExternal extends CertificateList {
            constructor() {
                super();
                this.initExternalSpecificFeatures();
            }

            initExternalSpecificFeatures() {
                console.log('Initializing external-specific features...');
                
                // Hide any edit buttons that might have been rendered
                this.hideEditFunctionality();
                
                // Load certificates from external API if needed
                this.loadExternalCertificates();
            }

            hideEditFunctionality() {
                // Hide add certificate buttons
                const addButtons = document.querySelectorAll('.btn.btn-add');
                addButtons.forEach(btn => btn.style.display = 'none');
                
                // Hide edit action icons
                const editIcons = document.querySelectorAll('.action-icon.edit, .action-btn.edit');
                editIcons.forEach(icon => icon.style.display = 'none');
            }

            // Override the edit certificate method to prevent editing
            editCertificate(certificateNumber) {
                console.log('Edit functionality disabled for external users');
                this.showToast('Edit functionality is not available for external users', 'warning');
            }

            // Load certificates if the grid is empty (for dynamic loading)
            async loadExternalCertificates() {
                const grid = document.getElementById('certificatesGrid');
                if (grid && grid.children.length === 0) {
                    try {
                        // Call the API to load certificates dynamically
                        const response = await fetch('/api/CertificateTileExternal');
                        if (response.ok) {
                            const result = await response.json();
                            if (result.certificates && result.certificates.length > 0) {
                                this.renderCertificatesFromApi(result.certificates);
                                this.updateResultsCount(result.totalCount || result.certificates.length);
                            }
                        }
                    } catch (error) {
                        console.error('Error loading external certificates:', error);
                        // Show error state - no fallback to mock data
                        this.showErrorState('Unable to load certificates. Please try again later.');
                    }
                }
            }

            renderCertificatesFromApi(certificates) {
                const grid = document.getElementById('certificatesGrid');
                if (!grid) return;

                certificates.forEach(cert => {
                    const cardElement = this.createExternalCertificateCard(cert);
                    grid.appendChild(cardElement);
                });

                // Update results count
                this.updateResultsCount(certificates.length);
            }

            updateResultsCount(count) {
                const resultsCountElement = document.getElementById('resultsCount');
                if (resultsCountElement) {
                    resultsCountElement.textContent = `${count} Results`;
                }
            }

            createExternalCertificateCard(cert) {
                const card = document.createElement('div');
                card.className = 'certificate-card';
                card.setAttribute('data-type', cert.certificationType?.toLowerCase().replace(/\s+/g, '-') || 'certificate');
                card.setAttribute('data-company', cert.companyName?.toLowerCase().replace(/\s+/g, '-') || 'unknown');
                card.setAttribute('data-country', cert.country?.toLowerCase().replace(/\s+/g, '-') || 'unknown');
                card.setAttribute('data-category', cert.productCategory?.toLowerCase().replace(/\s+/g, '-') || 'general');
                card.setAttribute('data-date', cert.effectiveDate || new Date().toISOString());

                const effectiveDate = cert.effectiveDate ? new Date(cert.effectiveDate).toLocaleDateString() : 'N/A';
                const expirationDate = cert.expirationDate ? new Date(cert.expirationDate).toLocaleDateString() : 'N/A';

                card.innerHTML = `
                    <div class="card-header">
                        <div class="header-row">
                            <div class="certificate-category">${cert.certificationType || 'CERTIFICATE'}</div>
                            <div class="certificate-id">${cert.certificateNumber || 'N/A'}</div>
                        </div>
                        <div class="company-name">${cert.companyName || 'Unknown Company'}</div>
                    </div>
                    <div class="card-body">
                        <div class="product-info">
                            <div class="product-name">${cert.productName || 'Product Name'}</div>
                            <div class="product-type">${cert.productType || cert.productCategory || 'General'}</div>
                        </div>
                        <div class="certificate-details">
                            <div class="certificate-period">
                                <strong>Certification Period:</strong>
                                ${effectiveDate} - ${expirationDate}
                            </div>
                            <div class="certificate-location">
                                <i class="fas fa-map-marker-alt"></i>
                                ${cert.location || ''} ${cert.country || ''}
                            </div>
                        </div>
                    </div>
                    <div class="card-actions">
                        <div class="action-icon download" title="Download PDF" data-action="download" data-cert="${cert.certificateNumber}">
                            <i class="fas fa-download"></i>
                        </div>
                        <div class="action-icon external" title="View Details" data-action="view" data-cert="${cert.certificateNumber}">
                            <i class="fas fa-external-link-alt"></i>
                        </div>
                    </div>
                `;

                return card;
            }

            showErrorState(message) {
                // Show error message instead of fallback data
                const grid = document.getElementById('certificatesGrid');
                if (grid) {
                    grid.innerHTML = `
                        <div class="error-state" style="text-align: center; padding: 40px; color: #666;">
                            <i class="fas fa-exclamation-triangle" style="font-size: 48px; color: #ff6b6b; margin-bottom: 16px;"></i>
                            <h3 style="margin-bottom: 8px;">Unable to Load Certificates</h3>
                            <p>${message}</p>
                            <button onclick="location.reload()" class="btn btn-primary" style="margin-top: 16px;">
                                <i class="fas fa-sync-alt"></i> Try Again
                            </button>
                        </div>
                    `;
                }
                this.updateResultsCount(0);
            }

            // Override the table view functionality to hide edit columns
            renderTableView() {
                // Call the parent method first
                if (super.renderTableView) {
                    super.renderTableView();
                }
                
                // Hide edit-related columns in table view
                const editHeaders = document.querySelectorAll('[data-sort-column="edit"], .edit-column');
                editHeaders.forEach(header => header.style.display = 'none');
            }
        }

        // Only initialize the modern CertificateTileViewExternal class
        if (typeof CertificateTileViewExternal !== 'undefined') {
            window.certificateTileViewExternal = new CertificateTileViewExternal();
            console.log('CertificateTileViewExternal initialized successfully');
        } else {
            // Fallback: Initialize the external certificate list only if modern class not available
            window.certificateListExternal = new CertificateListExternal();
            console.log('External Certificate List initialized successfully (fallback mode)');
        }
    } else {
        console.error('CertificateList class not found. Make sure certificateslist.js is loaded first.');
    }
});