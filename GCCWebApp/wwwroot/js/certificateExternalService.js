/**
 * Certificate External Service
 * Handles all API communication for the External Certificate Tile View
 * Implements lazy loading, filtering, searching, and autocomplete
 */

class CertificateExternalService {
    constructor() {
        this.baseUrl = '/api/CertificateTileExternal';
        this.certificates = [];
        this.currentPage = 1;
        this.pageSize = 20;
        this.totalCount = 0;
        this.totalPages = 0;
        this.hasMore = true;
        this.isLoading = false;
        this.filters = {
            search: '',
            certType: [],
            productCategory: [],
            company: [],
            country: [],
            tags: [],
            partner: [],
            sortBy: 'newest'
        };
        this.abortController = null;
    }

    /**
     * Build query string from filters
     */
    buildQueryString(additionalParams = {}) {
        const params = new URLSearchParams();
        
        // Add filters
        if (this.filters.search) {
            params.append('search', this.filters.search);
        }
        
        if (this.filters.certType.length > 0) {
            params.append('certType', this.filters.certType.join(','));
        }
        
        if (this.filters.productCategory.length > 0) {
            params.append('productCategory', this.filters.productCategory.join(','));
        }
        
        if (this.filters.company.length > 0) {
            params.append('company', this.filters.company.join(','));
        }
        
        if (this.filters.country.length > 0) {
            params.append('country', this.filters.country.join(','));
        }
        
        if (this.filters.tags.length > 0) {
            params.append('tags', this.filters.tags.join(','));
        }
        
        if (this.filters.partner.length > 0) {
            params.append('partner', this.filters.partner.join(','));
        }
        
        params.append('sortBy', this.filters.sortBy);
        
        // Add pagination
        params.append('page', this.currentPage);
        params.append('pageSize', this.pageSize);
        
        // Add any additional parameters
        Object.keys(additionalParams).forEach(key => {
            params.append(key, additionalParams[key]);
        });
        
        return params.toString();
    }

    /**
     * Load certificates from API
     * @param {boolean} append - Whether to append to existing certificates or replace
     */
    async loadCertificates(append = false) {
        if (this.isLoading) {
            return null;
        }

        this.isLoading = true;

        // Cancel any pending request
        if (this.abortController) {
            this.abortController.abort();
        }

        this.abortController = new AbortController();

        try {
            const queryString = this.buildQueryString();
            const response = await fetch(`${this.baseUrl}?${queryString}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                signal: this.abortController.signal
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();

            // DEBUG: Log the first certificate to verify API response structure
            if (data.certificates && data.certificates.length > 0) {
                console.log('DEBUG External - First certificate from API:', JSON.stringify(data.certificates[0], null, 2));
                console.log('DEBUG External - certificateNumber value:', data.certificates[0].certificateNumber);
                console.log('DEBUG External - id value:', data.certificates[0].id);
            }

            // Update state
            this.totalCount = data.totalCount;
            this.totalPages = data.totalPages;
            this.hasMore = data.hasMore;

            if (append) {
                this.certificates = [...this.certificates, ...data.certificates];
            } else {
                this.certificates = data.certificates;
            }

            return {
                certificates: this.certificates,
                totalCount: this.totalCount,
                hasMore: this.hasMore,
                currentPage: this.currentPage
            };

        } catch (error) {
            if (error.name === 'AbortError') {
                console.log('Request was cancelled');
                return null;
            }
            console.error('Error loading certificates:', error);
            throw error;
        } finally {
            this.isLoading = false;
            this.abortController = null;
        }
    }

    /**
     * Load more certificates (for infinite scroll)
     */
    async loadMore() {
        if (!this.hasMore || this.isLoading) {
            return null;
        }

        this.currentPage++;
        return await this.loadCertificates(true);
    }

    /**
     * Reset and reload certificates with new filters
     */
    async applyFilters(filters = {}) {
        // Update filters
        Object.assign(this.filters, filters);
        
        // Reset pagination
        this.currentPage = 1;
        this.certificates = [];
        
        // Load with new filters
        return await this.loadCertificates(false);
    }

    /**
     * Search certificates
     */
    async search(searchTerm) {
        this.filters.search = searchTerm;
        return await this.applyFilters();
    }

    /**
     * Get autocomplete suggestions
     */
    async getAutocompleteSuggestions(term) {
        if (!term || term.length < 2) {
            return { suggestions: [] };
        }

        try {
            const response = await fetch(`${this.baseUrl}/autocomplete?term=${encodeURIComponent(term)}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Error getting autocomplete suggestions:', error);
            return { suggestions: [] };
        }
    }

    /**
     * Get available filter options
     */
    async getFilterOptions() {
        try {
            const response = await fetch(`${this.baseUrl}/filters`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Error getting filter options:', error);
            return null;
        }
    }

    /**
     * Download certificate PDF
     */
    async downloadCertificate(certificateId) {
        try {
            const response = await fetch(`${this.baseUrl}/${certificateId}/download`, {
                method: 'GET'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Get the blob from response
            const blob = await response.blob();
            
            // Create a temporary URL for the blob
            const url = window.URL.createObjectURL(blob);
            
            // Create a temporary anchor element and trigger download
            const a = document.createElement('a');
            a.href = url;
            a.download = `Certificate_${certificateId}.pdf`;
            document.body.appendChild(a);
            a.click();
            
            // Clean up
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
            
            return true;
        } catch (error) {
            console.error('Error downloading certificate:', error);
            throw error;
        }
    }

    /**
     * Get shareable link for current filters
     */
    async getShareableLink() {
        try {
            const response = await fetch(`${this.baseUrl}/share-link`, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    search: this.filters.search,
                    certType: this.filters.certType.join(','),
                    productCategory: this.filters.productCategory.join(','),
                    company: this.filters.company.join(','),
                    country: this.filters.country.join(','),
                    tags: this.filters.tags.join(','),
                    partner: this.filters.partner.join(','),
                    sortBy: this.filters.sortBy
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            return data.url;
        } catch (error) {
            console.error('Error getting shareable link:', error);
            throw error;
        }
    }

    /**
     * Export certificates in CSV format (opens in Excel)
     * @param {Array} selectedIds - Optional array of certificate IDs to export (if null, exports all filtered)
     */
    async exportCertificates(selectedIds = null) {
        try {
            const queryString = this.buildQueryString();
            let url = `/Certificate/ExportCertificates?format=csv&${queryString}`;

            // If specific certificates are selected, add their IDs to the query
            if (selectedIds && selectedIds.length > 0) {
                url += `&ids=${selectedIds.join(',')}`;
            }

            const response = await fetch(url, {
                method: 'GET'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Get the blob from response
            const blob = await response.blob();

            // Create a temporary URL for the blob
            const downloadUrl = window.URL.createObjectURL(blob);

            // Create a temporary anchor element and trigger download
            const a = document.createElement('a');
            a.href = downloadUrl;
            a.download = `certificates-export-${new Date().toISOString().split('T')[0]}.csv`;
            document.body.appendChild(a);
            a.click();

            // Clean up
            window.URL.revokeObjectURL(downloadUrl);
            document.body.removeChild(a);

            return true;
        } catch (error) {
            console.error('Error exporting certificates:', error);
            throw error;
        }
    }

    /**
     * Clear all filters and reset
     */
    clearFilters() {
        this.filters = {
            search: '',
            certType: [],
            productCategory: [],
            company: [],
            country: [],
            tags: [],
            partner: [],
            sortBy: 'newest'
        };
        this.currentPage = 1;
        this.certificates = [];
    }

    /**
     * Update sort order
     */
    async updateSort(sortBy) {
        this.filters.sortBy = sortBy;
        return await this.applyFilters();
    }

    /**
     * Get current state for URL
     */
    getUrlState() {
        const state = {};
        
        if (this.filters.search) state.search = this.filters.search;
        if (this.filters.certType.length) state.certType = this.filters.certType.join(',');
        if (this.filters.productCategory.length) state.category = this.filters.productCategory.join(',');
        if (this.filters.company.length) state.company = this.filters.company.join(',');
        if (this.filters.country.length) state.country = this.filters.country.join(',');
        if (this.filters.tags.length) state.tags = this.filters.tags.join(',');
        if (this.filters.partner.length) state.partner = this.filters.partner.join(',');
        if (this.filters.sortBy !== 'newest') state.sort = this.filters.sortBy;
        
        return state;
    }

    /**
     * Load state from URL parameters
     */
    loadFromUrlState(params) {
        this.clearFilters();
        
        if (params.search) this.filters.search = params.search;
        if (params.certType) this.filters.certType = params.certType.split(',');
        if (params.category) this.filters.productCategory = params.category.split(',');
        if (params.company) this.filters.company = params.company.split(',');
        if (params.country) this.filters.country = params.country.split(',');
        if (params.tags) this.filters.tags = params.tags.split(',');
        if (params.partner) this.filters.partner = params.partner.split(',');
        if (params.sort) this.filters.sortBy = params.sort;
    }
}

// Export for use in other files
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CertificateExternalService;
}