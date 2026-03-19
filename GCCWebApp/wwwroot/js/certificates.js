// certificates.js - Certificate Component JavaScript

document.addEventListener('DOMContentLoaded', function () {

    // ===== CERTIFICATE FUNCTIONALITY =====

    // Certificate download functionality
    function downloadCertificate(certificateId) {
        // Download the certificate PDF
        console.log(`Downloading certificate: ${certificateId}`);

        try {
            window.open(`/Certificate/Download/${certificateId}`, '_blank');
            if (window.AppUtils && window.AppUtils.showToast) {
                window.AppUtils.showToast('Certificate download started');
            } else {
                console.log('Certificate download started');
            }
        } catch (error) {
            console.error('Error downloading certificate:', error);
            if (window.AppUtils && window.AppUtils.showToast) {
                window.AppUtils.showToast('Download failed', 'error');
            }
        }
    }

    // Certificate expand/view functionality
    function expandCertificate(certificateId) {
        console.log(`Expanding certificate: ${certificateId}`);
        // Navigate to detailed view
        window.location.href = `/Certificate/CertificateSingleView/${certificateId}`;
    }

    // Initialize certificate action buttons
    function initializeCertificateActions() {
        // Download buttons
        document.querySelectorAll('.action-button[title="Download Certificate"]').forEach(button => {
            button.addEventListener('click', function () {
                const certificateCard = this.closest('.certificate-card');
                const certificateId = certificateCard ? certificateCard.getAttribute('data-id') : 'unknown';
                downloadCertificate(certificateId);
            });
        });

        // Expand buttons
        document.querySelectorAll('.action-button[title="Expand Certificate"]').forEach(button => {
            button.addEventListener('click', function () {
                const certificateCard = this.closest('.certificate-card');
                const certificateId = certificateCard ? certificateCard.getAttribute('data-id') : 'unknown';
                expandCertificate(certificateId);
            });
        });

        // Download button in detail view
        const downloadButton = document.querySelector('.download-button');
        if (downloadButton) {
            downloadButton.addEventListener('click', function () {
                const certificateContainer = document.querySelector('.certificate-container');
                const certificateId = certificateContainer ? certificateContainer.getAttribute('data-id') : 'current';
                downloadCertificate(certificateId);
            });
        }
    }

    // ===== CERTIFICATE SEARCH AND FILTER =====

    // Certificate data storage
    let certificatesData = [];
    let allCertificatesData = [];

    // Load certificates from API
    async function loadCertificatesFromAPI() {
        try {
            console.log('Loading certificates from API...');
            const response = await fetch('/api/certificates?pageSize=100&page=1');
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const result = await response.json();
            
            if (result.success && result.data) {
                // Transform API data to match expected format
                certificatesData = result.data.map(cert => ({
                    id: cert.number || cert.id,
                    companyName: cert.companyName || 'Unknown Company',
                    productName: cert.productName || 'Unknown Product',
                    category: cert.categoryName || 'Uncategorized',
                    certType: cert.certificationTypeName || 'Unknown Type',
                    effectiveDate: cert.effectiveDate ? new Date(cert.effectiveDate).toLocaleDateString() : 'N/A',
                    expirationDate: cert.expirationDate ? new Date(cert.expirationDate).toLocaleDateString() : 'N/A',
                    location: cert.location || 'Unknown Location',
                    country: cert.country || 'Unknown Country',
                    status: cert.status || 'unknown',
                    asin: cert.asin || '',
                    upc: cert.upc || ''
                }));
                
                allCertificatesData = [...certificatesData];
                console.log(`Loaded ${certificatesData.length} certificates from API`);
                
                // Update the certificate grid with loaded data
                updateCertificateGrid(certificatesData);
                
                return certificatesData;
            } else {
                console.error('API response indicates failure:', result);
                return [];
            }
        } catch (error) {
            console.error('Error loading certificates from API:', error);
            // Fallback to empty array
            certificatesData = [];
            allCertificatesData = [];
            return [];
        }
    }

    // Initialize certificate search
    function initializeCertificateSearch() {
        window.AppUtils.initializeSearch('#search-input, .search-input', certificatesData, (certificate, searchTerm) => {
            return certificate.companyName.toLowerCase().includes(searchTerm) ||
                certificate.productName.toLowerCase().includes(searchTerm) ||
                certificate.category.toLowerCase().includes(searchTerm) ||
                certificate.id.toLowerCase().includes(searchTerm);
        });

        // Listen for search results
        document.addEventListener('dataFiltered', function (event) {
            updateCertificateGrid(event.detail.filteredData);
        });
    }

    // Initialize certificate sorting
    function initializeCertificateSort() {
        // Sort dropdown functionality
        const sortDropdown = document.querySelector('.sort-dropdown');
        if (sortDropdown) {
            const sortOptions = sortDropdown.querySelectorAll('.sort-option');
            const sortButton = sortDropdown.querySelector('.sort-dropdown-button span');

            sortOptions.forEach(option => {
                option.addEventListener('click', function (e) {
                    e.preventDefault();
                    const sortType = this.textContent.trim();
                    if (sortButton) {
                        sortButton.textContent = sortType;
                    }

                    // Sort the certificates
                    sortCertificates(sortType);
                });
            });
        }
    }

    // Sort certificates based on selected criteria
    function sortCertificates(sortType) {
        let sortedData = [...certificatesData];

        switch (sortType) {
            case 'Newest Effective Date':
                sortedData.sort((a, b) => new Date(b.effectiveDate) - new Date(a.effectiveDate));
                break;
            case 'Product Name A-Z':
                sortedData.sort((a, b) => a.productName.localeCompare(b.productName));
                break;
            case 'Company A-Z':
                sortedData.sort((a, b) => a.companyName.localeCompare(b.companyName));
                break;
            case 'Certification Type':
                sortedData.sort((a, b) => a.certType.localeCompare(b.certType));
                break;
            case 'Country':
                sortedData.sort((a, b) => a.country.localeCompare(b.country));
                break;
            default:
                // Default sort by ID
                break;
        }

        updateCertificateGrid(sortedData);
    }

    // Update certificate grid with new data
    function updateCertificateGrid(certificates) {
        const grid = document.querySelector('.certificates-grid');
        const resultsCount = document.querySelector('.results-count');

        if (!grid) return;

        // Update results count
        if (resultsCount) {
            resultsCount.textContent = `${certificates.length} Results`;
        }

        // Clear existing cards (except template cards)
        const existingCards = grid.querySelectorAll('.certificate-card:not([data-template])');
        existingCards.forEach(card => card.remove());

        // Add new cards
        certificates.forEach(cert => {
            const card = createCertificateCard(cert);
            grid.appendChild(card);
        });

        // Reinitialize action buttons for new cards
        initializeCertificateActions();
    }

    // Create certificate card HTML
    function createCertificateCard(certificate) {
        const card = document.createElement('div');
        card.className = 'certificate-card';
        card.setAttribute('data-id', certificate.id);

        card.innerHTML = `
            <div class="card-header">
                <h3>${certificate.certType}</h3>
            </div>
            <div class="certificate-info">
                <div class="certificate-number">Certificate #: ${certificate.id}</div>
                <div class="company-name">${certificate.companyName}</div>
                <div class="product-name">${certificate.productName}</div>
                <div class="product-category">${certificate.category}</div>
                <div class="certification-period">
                    <strong>Certification Period:</strong><br>
                    ${certificate.effectiveDate} - ${certificate.expirationDate}
                </div>
                <div class="certificate-location">
                    ${certificate.location}<br>
                    ${certificate.country}
                </div>
            </div>
            <div class="certificate-actions">
                <button class="action-button" title="Download Certificate">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                        <path d="M.5 9.9a.5.5 0 0 1 .5.5v2.5a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-2.5a.5.5 0 0 1 1 0v2.5a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2v-2.5a.5.5 0 0 1 .5-.5z"/>
                        <path d="M7.646 11.854a.5.5 0 0 0 .708 0l3-3a.5.5 0 0 0-.708-.708L8.5 10.293V1.5a.5.5 0 0 0-1 0v8.793L5.354 8.146a.5.5 0 1 0-.708.708l3 3z"/>
                    </svg>
                </button>
                <button class="action-button" title="Expand Certificate">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                        <path d="M5.828 10.172a.5.5 0 0 0-.707 0l-4.096 4.096V11.5a.5.5 0 0 0-1 0v3.975a.5.5 0 0 0 .5.5H4.5a.5.5 0 0 0 0-1H1.732l4.096-4.096a.5.5 0 0 0 0-.707zm4.344 0a.5.5 0 0 1 .707 0l4.096 4.096V11.5a.5.5 0 1 1 1 0v3.975a.5.5 0 0 1-.5.5H11.5a.5.5 0 0 1 0-1h2.768l-4.096-4.096a.5.5 0 0 1 0-.707zm0-4.344a.5.5 0 0 0 .707 0l4.096-4.096V4.5a.5.5 0 1 0 1 0V.525a.5.5 0 0 0-.5-.5H11.5a.5.5 0 0 0 0 1h2.768l-4.096 4.096a.5.5 0 0 0 0 .707zm-4.344 0a.5.5 0 0 1-.707 0L1.025 1.732V4.5a.5.5 0 0 1-1 0V.525a.5.5 0 0 1 .5-.5H4.5a.5.5 0 0 1 0 1H1.732l4.096 4.096a.5.5 0 0 1 0 .707z"/>
                    </svg>
                </button>
            </div>
        `;

        return card;
    }

    // ===== FILTER FUNCTIONALITY =====

    // Initialize filters
    function initializeFilters() {
        // Filter panel toggle
        const filterHeaders = document.querySelectorAll('.filter-header');
        filterHeaders.forEach(header => {
            header.addEventListener('click', function () {
                const content = this.nextElementSibling;
                if (content && content.classList.contains('filter-content')) {
                    const isVisible = content.style.display !== 'none';
                    content.style.display = isVisible ? 'none' : 'block';

                    // Update arrow direction
                    const icon = this.querySelector('.filter-icon');
                    if (icon) {
                        icon.textContent = isVisible ? '?' : '?';
                    }
                }
            });
        });

        // Clear filters
        const clearFilters = document.querySelector('.clear-filters');
        if (clearFilters) {
            clearFilters.addEventListener('click', function (e) {
                e.preventDefault();
                // Uncheck all filter checkboxes
                document.querySelectorAll('.filter-item input[type="checkbox"]').forEach(checkbox => {
                    checkbox.checked = false;
                });

                // Reset certificate display
                updateCertificateGrid(certificatesData);
                window.AppUtils.showToast('Filters cleared');
            });
        }

        // Filter checkboxes
        const filterCheckboxes = document.querySelectorAll('.filter-item input[type="checkbox"]');
        filterCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function () {
                applyFilters();
            });
        });
    }

    // Apply active filters
    function applyFilters() {
        const activeFilters = {
            categories: [],
            companies: [],
            countries: [],
            certTypes: []
        };

        // Collect active filters
        document.querySelectorAll('.filter-item input[type="checkbox"]:checked').forEach(checkbox => {
            const filterType = checkbox.closest('.filter-panel').querySelector('.filter-header h3').textContent.toLowerCase();
            const value = checkbox.parentElement.textContent.trim();

            switch (filterType) {
                case 'product categories':
                    activeFilters.categories.push(value);
                    break;
                case 'company':
                    activeFilters.companies.push(value);
                    break;
                case 'countries':
                    activeFilters.countries.push(value);
                    break;
                case 'certification type':
                    activeFilters.certTypes.push(value);
                    break;
            }
        });

        // Apply filters to certificate data
        let filteredData = certificatesData.filter(cert => {
            // If no filters active, show all
            const hasActiveFilters = Object.values(activeFilters).some(arr => arr.length > 0);
            if (!hasActiveFilters) return true;

            // Check each filter type
            let matches = true;

            if (activeFilters.categories.length > 0) {
                matches = matches && activeFilters.categories.some(cat =>
                    cert.category.toLowerCase().includes(cat.toLowerCase())
                );
            }

            if (activeFilters.companies.length > 0) {
                matches = matches && activeFilters.companies.some(comp =>
                    cert.companyName.toLowerCase().includes(comp.toLowerCase())
                );
            }

            if (activeFilters.countries.length > 0) {
                matches = matches && activeFilters.countries.includes(cert.country);
            }

            if (activeFilters.certTypes.length > 0) {
                matches = matches && activeFilters.certTypes.includes(cert.certType);
            }

            return matches;
        });

        updateCertificateGrid(filteredData);
    }

    // ===== INITIALIZATION =====

    // Initialize all certificate functionality
    async function initializeCertificates() {
        console.log('Initializing certificate functionality...');

        // Initialize core functionality
        initializeCertificateActions();
        initializeCertificateSort();
        initializeFilters();

        // Load data from API
        await loadCertificatesFromAPI();
        
        // Initialize search after data is loaded
        initializeCertificateSearch();

        console.log('Certificate functionality initialized');
    }

    // ===== API SIMULATION FUNCTIONS =====

    // Simulate loading certificates from API
    function loadCertificates() {
        return new Promise((resolve) => {
            // Simulate API delay
            setTimeout(() => {
                resolve(certificatesData);
            }, 500);
        });
    }

    // Simulate certificate export
    function exportCertificates() {
        const exportData = certificatesData.map(cert => ({
            'Certificate ID': cert.id,
            'Company Name': cert.companyName,
            'Product Name': cert.productName,
            'Category': cert.category,
            'Certification Type': cert.certType,
            'Effective Date': cert.effectiveDate,
            'Expiration Date': cert.expirationDate,
            'Location': cert.location,
            'Country': cert.country
        }));

        window.AppUtils.exportToCSV(exportData, 'certificates.csv');
    }

    // Import certificates from CSV and save to database
    async function importCertificates() {
        window.AppUtils.importFromCSV(async (data) => {
            console.log('Imported certificate data:', data);

            if (!data || data.length === 0) {
                window.AppUtils.showToast('No data to import', 'error');
                return;
            }

            if (data.length > 100) {
                window.AppUtils.showToast('Maximum 100 certificates can be imported at once', 'error');
                return;
            }

            // Transform CSV data to match backend DTO format
            const importDto = data.map((item) => ({
                Number: item['Certificate Number'] || item['Number'] || '',
                CompanyName: item['Company Name'] || '',
                ProductName: item['Product Name'] || '',
                Category: item['Category'] || '',
                CertificationType: item['Certification Type'] || 'CERTIFIED SUSTAINABILITY FACTS',
                EffectiveDate: item['Effective Date'] || '',
                ExpirationDate: item['Expiration Date'] || '',
                Location: item['Location'] || '',
                Country: item['Country'] || '',
                Status: item['Status'] || 'Active'
            }));

            try {
                // Send to backend API
                const response = await fetch('/Certificate/Import', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(importDto)
                });

                const result = await response.json();

                if (result.success) {
                    const { successCount, failureCount, failed } = result.data;

                    // Show success message
                    if (failureCount === 0) {
                        window.AppUtils.showToast(`Successfully imported ${successCount} certificates`);
                    } else {
                        window.AppUtils.showToast(
                            `Imported ${successCount} of ${successCount + failureCount} certificates. ${failureCount} failed.`,
                            'warning'
                        );

                        // Log failed imports to console
                        if (failed && failed.length > 0) {
                            console.error('Failed imports:', failed);
                        }
                    }

                    // Reload certificates from API to show newly imported data
                    await loadCertificatesFromAPI();
                } else {
                    window.AppUtils.showToast(result.message || 'Import failed', 'error');
                }
            } catch (error) {
                console.error('Error importing certificates:', error);
                window.AppUtils.showToast('Failed to import certificates. Please try again.', 'error');
            }
        });
    }

    // ===== EVENT LISTENERS FOR PAGE ACTIONS =====

    // Export button
    const exportBtn = document.querySelector('#export-btn, .btn[title*="Export"]');
    if (exportBtn) {
        exportBtn.addEventListener('click', exportCertificates);
    }

    // Import button
    const importBtn = document.querySelector('#import-btn, .btn[title*="Import"]');
    if (importBtn) {
        importBtn.addEventListener('click', importCertificates);
    }

    // Add new certificate button
    const addCertBtn = document.querySelector('#add-certificate-btn, .btn[title*="Add"]');
    if (addCertBtn) {
        addCertBtn.addEventListener('click', function () {
            // Navigate to add certificate page
            window.location.href = 'add-certificate.html';
        });
    }

    // ===== MAKE FUNCTIONS GLOBALLY AVAILABLE =====

    window.CertificateUtils = {
        downloadCertificate,
        expandCertificate,
        loadCertificates,
        loadCertificatesFromAPI,
        exportCertificates,
        importCertificates,
        updateCertificateGrid,
        applyFilters
    };

    // Initialize everything
    initializeCertificates().catch(console.error);
});