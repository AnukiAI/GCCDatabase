// Toggle filter sections
document.querySelectorAll('.filter-header').forEach(header => {
    header.addEventListener('click', () => {
        const content = header.nextElementSibling;
        if (content && content.classList.contains('filter-content')) {
            content.style.display = content.style.display === 'none' ? 'block' : 'none';
            header.querySelector('div:last-child').textContent =
                content.style.display === 'none' ? '▼' : '▲';
        }
    });
});

// Clear filters functionality
document.querySelector('.clear-filters').addEventListener('click', () => {
    document.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
        checkbox.checked = false;
    });
});

// View options toggle
document.querySelectorAll('.view-option').forEach(option => {
    option.addEventListener('click', () => {
        document.querySelectorAll('.view-option').forEach(opt => {
            opt.classList.remove('active');
        });
        option.classList.add('active');
    });
});