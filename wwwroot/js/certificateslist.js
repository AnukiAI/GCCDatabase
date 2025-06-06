/**
 * Certificate List JavaScript
 * Handles interactive functionality for the certificate list page
 * Following GreenCircle Design Guide patterns
 */

class CertificateList {
    constructor() {
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.setupFilters();
        this.setupSearch();
        this.setupSort();
        this.setupMobileFilters();
        this.setupPagination();
    }

    setupEventListeners() {
        // Sort dropdown toggle
        const sortDropdownBtn = document.getElementById('sortDropdownBtn');
        const sortDropdown = sortDropdownBtn?.parentElement;

        if (sortDropdownBtn && sortDropdown) {
            sortDropdownBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                sortDropdown.classList.toggle('open');
            });

            // Close dropdown when clicking outside
            document.addEventListener('click', (e) => {
                if (!sortDropdown.contains(e.target)) {
                    sortDropdown.classList.remove('open');
                }
            });
        }

        // Sort options
        const sortOptions = document.querySelectorAll('.sort-option');
        sortOptions.forEach(option => {
            option.addEventListener('click', (e) => {
                e.preventDefault();
                this.handleSortChange(option);
            });
        });

        // Clear filters
        const clearFiltersBtn = document.getElementById('clearFiltersBtn');
        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.clearAllFilters();
            });
        }

        // Certificate card actions
        this.setupCertificateActions();
    }

    setupFilters() {
        // Filter panel toggles
        const filterHeaders = document.querySelectorAll('.filter-header');
        filterHeaders.forEach(header => {
            header.addEventListener('click', () => {
                const panel = header.parentElement;
                panel.classList.toggle('open');

                // Store filter state in localStorage for persistence
                const filterId = header.dataset.filter;
                if (filterId) {
                    const isOpen = panel.classList.contains('open');
                    localStorage.setItem(`filter_${filterId}_open`, isOpen);
                }
            });

            // Restore filter panel states from localStorage
            const filterId = header.dataset.filter;
            if (filterId) {
                const isOpen = localStorage.getItem(`filter_${filterId}_open`) === 'true';
                if (isOpen) {
                    header.parentElement.classList.add('open');
                }
            }
        });

        // Filter tree toggles for hierarchical categories
        const treeToggles = document.querySelectorAll('.tree-toggle');
        treeToggles.forEach(toggle => {
            toggle.addEventListener('click', (e) => {
                e.stopPropagation();
                const treeItem = toggle.closest('.filter-tree-item');
                treeItem.classList.toggle('expanded');
            });
        });

        // Filter checkboxes
        const filterCheckboxes = document.querySelectorAll('.filter-option input[type="checkbox"]');
        filterCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                this.handleFilterChange(e.target);
            });
        });

        // Filter search inputs
        const filterSearchInputs = document.querySelectorAll('.filter-search-input');
        filterSearchInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                this.handleFilterSearch(e.target);
            });
        });
    }

    setupSearch() {
        const searchInput = document.getElementById('searchInput');
        const searchBtn = document.querySelector('.search-btn');

        if (searchInput) {
            // Debounced search
            let searchTimeout;
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    this.performSearch(e.target.value);
                }, 300);
            });

            // Search on Enter key
            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.performSearch(e.target.value);
                }
            });
        }

        if (searchBtn) {
            searchBtn.addEventListener('click', (e) => {
                e.preventDefault();
                const searchValue = searchInput ? searchInput.value : '';
                this.performSearch(searchValue);
            });
        }
    }

    setupSort() {
        // Sort functionality handled in event listeners
        this.currentSort = 'default';
    }

    setupMobileFilters() {
        const mobileFilterToggle = document.getElementById('mobileFilterToggle');
        const filtersSidebar = document.querySelector('.filters-sidebar');

        if (mobileFilterToggle && filtersSidebar) {
            mobileFilterToggle.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleMobileFilters();
            });

            // Create overlay for mobile filters
            const overlay = document.createElement('div');
            overlay.className = 'filter-overlay';
            overlay.addEventListener('click', () => {
                this.closeMobileFilters();
            });
            document.body.appendChild(overlay);

            // Close mobile filters on window resize if screen becomes large
            window.addEventListener('resize', () => {
                if (window.innerWidth > 768) {
                    this.closeMobileFilters();
                }
            });
        }
    }

    setupPagination() {
        // Pagination will be handled by MVC backend
        // This is just for client-side interactions
        const paginationButtons = document.querySelectorAll('.pagination .btn');
        paginationButtons.forEach(btn => {
            btn.addEventListener('click', (e) => {
                if (!btn.disabled) {
                    e.preventDefault();
                    // In MVC implementation, this would trigger a page change
                    console.log('Pagination clicked:', btn.textContent.trim());
                }
            });
        });
    }

    setupCertificateActions() {
        const certificateCards = document.querySelectorAll('.certificate-card');

        certificateCards.forEach(card => {
            // Download button
            const downloadBtn = card.querySelector('button[title="Download Certificate"]');
            if (downloadBtn) {
                downloadBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.downloadCertificate(card);
                });
            }

            // View details button
            const viewBtn = card.querySelector('button[title="View Details"]');
            if (viewBtn) {
                viewBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.viewCertificateDetails(card);
                });
            }

            // Expand button
            const expandBtn = card.querySelector('button[title="Expand"]');
            if (expandBtn) {
                expandBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.expandCertificate(card);
                });
            }

            // Card hover effects
            card.addEventListener('mouseenter', () => {
                card.style.transform = 'translateY(-4px)';
            });

            card.addEventListener('mouseleave', () => {
                card.style.transform = 'translateY(0)';
            });
        });
    }

    // Sort handling
    handleSortChange(sortOption) {
        const sortValue = sortOption.dataset.sort;
        const sortText = sortOption.textContent.trim();

        // Update button text
        const sortBtn = document.getElementById('sortDropdownBtn');
        if (sortBtn) {
            sortBtn.querySelector('span').textContent = sortText;
        }

        // Close dropdown
        const sortDropdown = sortOption.closest('.sort-dropdown');
        if (sortDropdown) {
            sortDropdown.classList.remove('open');
        }

        // Apply sort (in MVC, this would trigger a server request)
        this.applySorting(sortValue);
        this.currentSort = sortValue;

        // Show loading state
        this.showLoadingState();

        // Simulate API call delay
        setTimeout(() => {
            this.hideLoadingState();
            this.showToast(`Sorted by ${sortText}`, 'success');
        }, 500);
    }

    // Filter handling
    handleFilterChange(checkbox) {
        const filterValue = checkbox.value;
        const isChecked = checkbox.checked;

        // In MVC implementation, this would update the filter state and trigger a request
        console.log(`Filter ${filterValue} ${isChecked ? 'checked' : 'unchecked'}`);

        // Update results count (simulated)
        this.updateResultsCount();

        // Apply filters with loading state
        this.showLoadingState();
        setTimeout(() => {
            this.hideLoadingState();
        }, 300);
    }

    handleFilterSearch(searchInput) {
        const searchTerm = searchInput.value.toLowerCase();
        const filterPanel = searchInput.closest('.filter-panel');
        const filterOptions = filterPanel.querySelectorAll('.filter-option');

        filterOptions.forEach(option => {
            const optionText = option.textContent.toLowerCase();
            const shouldShow = optionText.includes(searchTerm);
            option.style.display = shouldShow ? 'flex' : 'none';
        });
    }

    clearAllFilters() {
        // Uncheck all filter checkboxes
        const filterCheckboxes = document.querySelectorAll('.filter-option input[type="checkbox"]');
        filterCheckboxes.forEach(checkbox => {
            checkbox.checked = false;
        });

        // Clear all filter search inputs
        const filterSearchInputs = document.querySelectorAll('.filter-search-input');
        filterSearchInputs.forEach(input => {
            input.value = '';
            // Trigger search to show all options
            this.handleFilterSearch(input);
        });

        // Update results
        this.updateResultsCount();
        this.showToast('All filters cleared', 'info');
    }

    // Search handling
    performSearch(searchTerm) {
        console.log('Searching for:', searchTerm);

        // Show loading state
        this.showLoadingState();

        // In MVC implementation, this would trigger a server request
        // Simulate API delay
        setTimeout(() => {
            this.hideLoadingState();

            if (searchTerm.trim()) {
                this.showToast(`Searching for "${searchTerm}"`, 'info');
            }

            // Update results count (simulated)
            this.updateResultsCount();
        }, 500);
    }

    // Sorting
    applySorting(sortValue) {
        console.log('Applying sort:', sortValue);
        // In MVC implementation, this would trigger a server request with sort parameters
    }

    // Mobile filter controls
    toggleMobileFilters() {
        const filtersSidebar = document.querySelector('.filters-sidebar');
        const overlay = document.querySelector('.filter-overlay');

        if (filtersSidebar && overlay) {
            const isOpen = filtersSidebar.classList.contains('open');

            if (isOpen) {
                this.closeMobileFilters();
            } else {
                this.openMobileFilters();
            }
        }
    }

    openMobileFilters() {
        const filtersSidebar = document.querySelector('.filters-sidebar');
        const overlay = document.querySelector('.filter-overlay');

        if (filtersSidebar && overlay) {
            filtersSidebar.classList.add('open');
            overlay.classList.add('show');
            document.body.style.overflow = 'hidden';
        }
    }

    closeMobileFilters() {
        const filtersSidebar = document.querySelector('.filters-sidebar');
        const overlay = document.querySelector('.filter-overlay');

        if (filtersSidebar && overlay) {
            filtersSidebar.classList.remove('open');
            overlay.classList.remove('show');
            document.body.style.overflow = '';
        }
    }

    // Certificate actions
    downloadCertificate(card) {
        const certificateNumber = card.querySelector('.certificate-number .value')?.textContent;
        console.log('Downloading certificate:', certificateNumber);

        // In MVC implementation, this would trigger a download
        this.showToast(`Downloading certificate ${certificateNumber}`, 'success');
    }

    viewCertificateDetails(card) {
        const certificateNumber = card.querySelector('.certificate-number .value')?.textContent;
        console.log('Viewing certificate details:', certificateNumber);

        // In MVC implementation, this would navigate to a details page
        this.showToast(`Viewing details for certificate ${certificateNumber}`, 'info');
    }

    expandCertificate(card) {
        const certificateNumber = card.querySelector('.certificate-number .value')?.textContent;
        console.log('Expanding certificate:', certificateNumber);

        // Toggle expanded state
        card.classList.toggle('expanded');
        this.showToast(`Certificate ${certificateNumber} expanded`, 'info');
    }

    // Utility functions
    updateResultsCount() {
        const resultsCountElement = document.getElementById('resultsCount');
        if (resultsCountElement) {
            // Simulate random count for demo
            const randomCount = Math.floor(Math.random() * 300) + 50;
            resultsCountElement.textContent = randomCount;
        }
    }

    showLoadingState() {
        const certificatesGrid = document.getElementById('certificatesGrid');
        if (certificatesGrid) {
            certificatesGrid.classList.add('loading');
        }
    }

    hideLoadingState() {
        const certificatesGrid = document.getElementById('certificatesGrid');
        if (certificatesGrid) {
            certificatesGrid.classList.remove('loading');
        }
    }

    showToast(message, type = 'success') {
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;

        const iconMap = {
            success: 'check-circle',
            error: 'exclamation-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };

        toast.innerHTML = `
            <i class="fas fa-${iconMap[type]}"></i>
            ${message}
        `;

        // Add to page
        document.body.appendChild(toast);

        // Show with animation
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        // Auto remove after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                if (document.body.contains(toast)) {
                    document.body.removeChild(toast);
                }
            }, 300);
        }, 3000);
    }

    // Public API methods for MVC integration
    updateCertificates(certificates) {
        // Method to update the certificates grid with new data from MVC controller
        const grid = document.getElementById('certificatesGrid');
        if (grid && certificates) {
            // Clear existing cards
            grid.innerHTML = '';

            // Add new certificate cards
            certificates.forEach(cert => {
                const cardElement = this.createCertificateCard(cert);
                grid.appendChild(cardElement);
            });

            // Re-setup event listeners for new cards
            this.setupCertificateActions();
        }
    }

    createCertificateCard(certificate) {
        // Helper method to create certificate card HTML from data
        const card = document.createElement('article');
        card.className = 'certificate-card';

        card.innerHTML = `
            <header class="certificate-header">
                <h3>${certificate.certificationType}</h3>
                <span class="badge badge-primary">${certificate.badgeText}</span>
            </header>
            <div class="certificate-content">
                <div class="certificate-number">
                    <span class="label">Certificate #:</span>
                    <span class="value">${certificate.number}</span>
                </div>
                <h4 class="company-name">${certificate.companyName}</h4>
                <h5 class="product-name">${certificate.productName}</h5>
                <div class="product-category">
                    <span class="tag-type External">${certificate.category}</span>
                </div>
                <div class="certification-period">
                    <div class="period-label">Certification Period:</div>
                    <div class="period-dates">${certificate.effectiveDate} - ${certificate.expirationDate}</div>
                </div>
                <div class="certificate-location">
                    <i class="fas fa-map-marker-alt"></i>
                    <span>${certificate.location}</span>
                </div>
            </div>
            <footer class="certificate-actions">
                <button class="btn btn-outline btn-sm" title="Download Certificate">
                    <i class="fas fa-download"></i>
                </button>
                <button class="btn btn-outline btn-sm" title="View Details">
                    <i class="fas fa-eye"></i>
                </button>
                <button class="btn btn-outline btn-sm" title="Expand">
                    <i class="fas fa-expand"></i>
                </button>
            </footer>
        `;

        return card;
    }

    getFilterState() {
        // Method to get current filter state for MVC form submission
        const filterState = {};

        const filterCheckboxes = document.querySelectorAll('.filter-option input[type="checkbox"]:checked');
        filterCheckboxes.forEach(checkbox => {
            const filterPanel = checkbox.closest('.filter-panel');
            const filterType = filterPanel.querySelector('.filter-header h4').textContent.trim();

            if (!filterState[filterType]) {
                filterState[filterType] = [];
            }
            filterState[filterType].push(checkbox.value);
        });

        return filterState;
    }

    getCurrentSort() {
        // Method to get current sort for MVC form submission
        return this.currentSort || 'default';
    }

    getCurrentSearch() {
        // Method to get current search term for MVC form submission
        const searchInput = document.getElementById('searchInput');
        return searchInput ? searchInput.value : '';
    }
}

// Toast styles that need to be added to CSS
const toastStyles = `
.toast {
    position: fixed;
    top: 20px;
    right: 20px;
    background-color: var(--success);
    color: white;
    padding: 15px 20px;
    border-radius: 6px;
    box-shadow: var(--shadow);
    z-index: 9999;
    transition: var(--transition);
    transform: translateX(100%);
    opacity: 0;
    display: flex;
    align-items: center;
    gap: 10px;
    max-width: 300px;
}

.toast.show {
    transform: translateX(0);
    opacity: 1;
}

.toast.error {
    background-color: var(--danger);
}

.toast.warning {
    background-color: #FFE9A3;
    color: var(--blue-dark);
}

.toast.info {
    background-color: #336699;
    color: white;
}
`;

// Add toast styles to page if they don't exist
if (!document.querySelector('style[data-toast-styles]')) {
    const styleElement = document.createElement('style');
    styleElement.setAttribute('data-toast-styles', 'true');
    styleElement.textContent = toastStyles;
    document.head.appendChild(styleElement);
}

// Initialize the certificate list when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.certificateList = new CertificateList();
});

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CertificateList;
}