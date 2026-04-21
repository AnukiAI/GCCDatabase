/**
 * Admin Pagination Utility
 * Drop-in client-side pagination for admin tables.
 *
 * Usage:
 *   const pager = new AdminPagination({
 *       containerId: 'pagination-container',  // where to render controls
 *       pageSize: 25,
 *       onPageChange: (pageData, pageInfo) => populateTable(pageData)
 *   });
 *
 *   // Call whenever filtered/sorted data changes:
 *   pager.setData(filteredAndSortedArray);
 */
class AdminPagination {
    constructor(options) {
        this.containerId = options.containerId;
        this.pageSize = options.pageSize || 25;
        this.currentPage = 1;
        this.data = [];
        this.onPageChange = options.onPageChange;

        // Create container if it doesn't exist
        if (!document.getElementById(this.containerId)) {
            const table = document.querySelector('.admin-table') || document.querySelector('table');
            if (table) {
                const container = document.createElement('div');
                container.id = this.containerId;
                container.className = 'admin-pagination';
                table.parentNode.insertBefore(container, table.nextSibling);
            }
        }
    }

    setData(data) {
        this.data = data || [];
        // Reset to page 1 if current page is beyond new data
        const totalPages = Math.ceil(this.data.length / this.pageSize);
        if (this.currentPage > totalPages) {
            this.currentPage = 1;
        }
        this.render();
        this.emitPage();
    }

    getPageData() {
        const start = (this.currentPage - 1) * this.pageSize;
        const end = start + this.pageSize;
        return this.data.slice(start, end);
    }

    getPageInfo() {
        const totalItems = this.data.length;
        const totalPages = Math.max(1, Math.ceil(totalItems / this.pageSize));
        const start = Math.min((this.currentPage - 1) * this.pageSize + 1, totalItems);
        const end = Math.min(this.currentPage * this.pageSize, totalItems);
        return { currentPage: this.currentPage, totalPages, totalItems, start, end };
    }

    goToPage(page) {
        const info = this.getPageInfo();
        if (page < 1 || page > info.totalPages) return;
        this.currentPage = page;
        this.render();
        this.emitPage();

        // Scroll table into view
        const container = document.getElementById(this.containerId);
        if (container) {
            const table = container.previousElementSibling;
            if (table) table.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    emitPage() {
        if (this.onPageChange) {
            this.onPageChange(this.getPageData(), this.getPageInfo());
        }
    }

    render() {
        const container = document.getElementById(this.containerId);
        if (!container) return;

        const info = this.getPageInfo();

        // Don't show pagination controls if all items fit on one page
        if (info.totalItems <= this.pageSize) {
            container.innerHTML = '';
            return;
        }

        let html = '<div class="pagination-wrapper">';

        // Page controls
        html += '<div class="pagination-controls">';

        // Previous button
        html += `<button class="pagination-btn${info.currentPage === 1 ? ' disabled' : ''}"
                    data-page="${info.currentPage - 1}" ${info.currentPage === 1 ? 'disabled' : ''}>
                    <i class="fas fa-chevron-left"></i> Prev</button>`;

        // Page numbers
        const pages = this.getPageNumbers(info.currentPage, info.totalPages);
        for (const p of pages) {
            if (p === '...') {
                html += '<span class="pagination-ellipsis">\u2026</span>';
            } else {
                html += `<button class="pagination-btn pagination-num${p === info.currentPage ? ' active' : ''}"
                            data-page="${p}">${p}</button>`;
            }
        }

        // Next button
        html += `<button class="pagination-btn${info.currentPage === info.totalPages ? ' disabled' : ''}"
                    data-page="${info.currentPage + 1}" ${info.currentPage === info.totalPages ? 'disabled' : ''}>
                    Next <i class="fas fa-chevron-right"></i></button>`;

        html += '</div>'; // pagination-controls
        html += '</div>'; // pagination-wrapper

        container.innerHTML = html;

        // Attach click handlers
        container.querySelectorAll('.pagination-btn:not(.disabled)').forEach(btn => {
            btn.addEventListener('click', () => {
                const page = parseInt(btn.dataset.page);
                if (!isNaN(page)) this.goToPage(page);
            });
        });
    }

    getPageNumbers(current, total) {
        if (total <= 7) {
            return Array.from({ length: total }, (_, i) => i + 1);
        }

        const pages = [];
        pages.push(1);

        if (current > 3) pages.push('...');

        const start = Math.max(2, current - 1);
        const end = Math.min(total - 1, current + 1);

        for (let i = start; i <= end; i++) {
            pages.push(i);
        }

        if (current < total - 2) pages.push('...');

        pages.push(total);
        return pages;
    }
}

// Make available globally
window.AdminPagination = AdminPagination;
