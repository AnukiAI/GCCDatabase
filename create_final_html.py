#!/usr/bin/env python3
import json

# Read the test data
print("Reading test data...")
with open('complete_test_data.json', 'r', encoding='utf-8') as f:
    tests_data = json.load(f)

print(f"Loaded {len(tests_data)} tests")

# Convert to JavaScript format (compact to reduce file size)
js_data = json.dumps(tests_data)

print("Generating HTML...")

# Read the HTML template (first part)
html_start = '''<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>GCC Complete Quality Audit - All 549 Tests</title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css">
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 15px; }
        .container { max-width: 1800px; margin: 0 auto; background: white; border-radius: 10px; box-shadow: 0 10px 40px rgba(0,0,0,0.3); overflow: hidden; }
        .header { background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); color: white; padding: 30px; text-align: center; }
        .header h1 { font-size: 32px; margin-bottom: 10px; }
        .header p { font-size: 16px; opacity: 0.9; }
        .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 15px; padding: 20px; background: #f8f9fa; }
        .stat-card { background: white; padding: 20px; border-radius: 8px; text-align: center; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
        .stat-card h3 { font-size: 12px; color: #666; margin-bottom: 8px; text-transform: uppercase; }
        .stat-card p { font-size: 28px; font-weight: bold; color: #1e3c72; }
        .controls { padding: 15px 20px; background: white; border-bottom: 2px solid #dee2e6; display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }
        .search-box { flex: 1; min-width: 250px; }
        .search-box input { width: 100%; padding: 10px 15px; border: 2px solid #dee2e6; border-radius: 5px; font-size: 14px; }
        .filter-group { display: flex; gap: 5px; flex-wrap: wrap; }
        .btn { padding: 8px 16px; border: 2px solid #1e3c72; background: white; color: #1e3c72; border-radius: 5px; cursor: pointer; font-size: 13px; font-weight: 600; transition: all 0.2s; }
        .btn:hover, .btn.active { background: #1e3c72; color: white; }
        .btn-success { background: #28a745; color: white; border-color: #28a745; }
        .btn-success:hover { background: #218838; border-color: #218838; }
        .table-container { padding: 20px; overflow-x: auto; max-height: calc(100vh - 400px); }
        table { width: 100%; border-collapse: collapse; background: white; font-size: 13px; }
        thead { position: sticky; top: 0; background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); color: white; z-index: 10; }
        th { padding: 12px 10px; text-align: left; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; cursor: pointer; user-select: none; }
        th:hover { background: rgba(255,255,255,0.1); }
        tbody tr { border-bottom: 1px solid #dee2e6; transition: background 0.2s; cursor: pointer; }
        tbody tr:hover { background: #f8f9fa; }
        td { padding: 10px; }
        .badge { padding: 4px 10px; border-radius: 12px; font-size: 11px; font-weight: 600; text-transform: uppercase; display: inline-block; }
        .badge-passed { background: #d4edda; color: #155724; }
        .badge-failed { background: #f8d7da; color: #721c24; }
        .badge-skipped { background: #e2e3e5; color: #383d41; }
        .badge-critical { background: #f8d7da; color: #721c24; }
        .badge-high { background: #fff3cd; color: #856404; }
        .badge-medium { background: #d1ecf1; color: #0c5460; }
        .badge-low { background: #d4edda; color: #155724; }
        .badge-blocker { background: #dc3545; color: white; }
        .badge-major { background: #ffc107; color: #333; }
        .badge-minor { background: #17a2b8; color: white; }
        .modal { display: none; position: fixed; z-index: 1000; left: 0; top: 0; width: 100%; height: 100%; overflow: auto; background-color: rgba(0,0,0,0.6); animation: fadeIn 0.3s; }
        @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
        .modal-content { background-color: white; margin: 2% auto; padding: 0; border-radius: 10px; width: 95%; max-width: 1400px; max-height: 90vh; overflow-y: auto; box-shadow: 0 10px 40px rgba(0,0,0,0.4); animation: slideIn 0.3s; }
        @keyframes slideIn { from { transform: translateY(-50px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
        .modal-header { background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); color: white; padding: 20px 30px; border-radius: 10px 10px 0 0; display: flex; justify-content: space-between; align-items: center; }
        .modal-header h2 { font-size: 20px; margin: 0; }
        .close { color: white; font-size: 32px; font-weight: bold; cursor: pointer; line-height: 1; }
        .close:hover { opacity: 0.7; }
        .modal-body { padding: 30px; }
        .detail-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(350px, 1fr)); gap: 20px; margin-bottom: 20px; }
        .detail-section { background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 4px solid #1e3c72; }
        .detail-section h4 { color: #1e3c72; margin-bottom: 10px; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px; }
        .detail-section p, .detail-section ul { font-size: 14px; color: #555; margin: 0; }
        .detail-section ul { padding-left: 20px; }
        .detail-section li { margin-bottom: 5px; }
        .code-block { background: #2d2d2d; color: #f8f8f2; padding: 15px; border-radius: 5px; font-family: 'Courier New', monospace; font-size: 12px; overflow-x: auto; margin-top: 10px; }
        .req-tag { background: #e7f3ff; color: #004085; padding: 3px 10px; border-radius: 12px; font-size: 11px; font-weight: 600; border: 1px solid #b8daff; display: inline-block; margin-right: 5px; margin-bottom: 5px; }
        .defect-box { background: #fff3cd; border: 2px solid #ffc107; border-radius: 5px; padding: 15px; margin-top: 15px; }
        .defect-box h4 { color: #856404; margin-bottom: 10px; }
        .loading { text-align: center; padding: 40px; font-size: 18px; color: #666; }
        .footer { background: #1e3c72; color: white; padding: 20px; text-align: center; font-size: 13px; }
        @media print { body { background: white; } .controls, .btn { display: none; } .table-container { max-height: none; } }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1><i class="fas fa-clipboard-check"></i> GCC Quality Audit Report</h1>
            <p>Complete Test Coverage - All 549 Test Cases with Full Audit Details</p>
            <p style="font-size: 14px; margin-top: 10px;">Generated: October 14, 2025 | 100% Test Coverage | xUnit 2.8.2 | .NET 8.0</p>
        </div>
        <div class="stats">
            <div class="stat-card"><h3>Total Tests</h3><p id="totalTests">549</p></div>
            <div class="stat-card"><h3>Passed</h3><p id="passedCount" style="color: #28a745;">0</p></div>
            <div class="stat-card"><h3>Failed</h3><p id="failedCount" style="color: #dc3545;">0</p></div>
            <div class="stat-card"><h3>Skipped</h3><p id="skippedCount" style="color: #6c757d;">0</p></div>
            <div class="stat-card"><h3>Pass Rate</h3><p id="passRate" style="color: #1e3c72;">0%</p></div>
            <div class="stat-card"><h3>Categories</h3><p id="categoryCount">17</p></div>
        </div>
        <div class="controls">
            <div class="search-box"><input type="text" id="searchInput" placeholder="Search tests by name, ID, category, requirements..." onkeyup="searchTests()"></div>
            <div class="filter-group">
                <button class="btn active" onclick="filterStatus('all')">All</button>
                <button class="btn" onclick="filterStatus('passed')">Passed</button>
                <button class="btn" onclick="filterStatus('failed')">Failed</button>
                <button class="btn" onclick="filterStatus('skipped')">Skipped</button>
            </div>
            <button class="btn btn-success" onclick="exportToCSV()"><i class="fas fa-file-csv"></i> Export CSV</button>
            <button class="btn btn-success" onclick="window.print()"><i class="fas fa-print"></i> Print</button>
        </div>
        <div class="table-container">
            <div id="loading" class="loading"><i class="fas fa-spinner fa-spin fa-3x"></i><p>Loading 549 test cases...</p></div>
            <table id="testTable" style="display:none;">
                <thead><tr>
                    <th onclick="sortTable(0)">ID <i class="fas fa-sort"></i></th>
                    <th onclick="sortTable(1)">Test Name <i class="fas fa-sort"></i></th>
                    <th onclick="sortTable(2)">Category <i class="fas fa-sort"></i></th>
                    <th onclick="sortTable(3)">Status <i class="fas fa-sort"></i></th>
                    <th onclick="sortTable(4)">Priority <i class="fas fa-sort"></i></th>
                    <th onclick="sortTable(5)">Severity <i class="fas fa-sort"></i></th>
                    <th onclick="sortTable(6)">Time (ms) <i class="fas fa-sort"></i></th>
                    <th>Requirements</th>
                </tr></thead>
                <tbody id="testTableBody"></tbody>
            </table>
        </div>
        <div class="footer">
            <p><strong>GCC Web Application - Complete Quality Audit Report</strong></p>
            <p>Generated: October 14, 2025 | Framework: xUnit 2.8.2 | .NET 8.0 | 549 Total Test Cases with Full Audit Details</p>
            <p>This report documents all 549 test cases with complete audit information including test ID, name, category, status, priority, severity, execution time, actions, expected/actual results, detailed steps, defects, requirements, and use cases.</p>
        </div>
    </div>
    <div id="testModal" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h2 id="modalTestName">Test Case Details</h2>
                <span class="close" onclick="closeModal()">&times;</span>
            </div>
            <div class="modal-body" id="modalBody"></div>
        </div>
    </div>
    <script>
const allTests = '''

html_end = ''';
let currentFilter = 'all', currentCategory = 'all';
function renderTable() {
    const tbody = document.getElementById('testTableBody'), loading = document.getElementById('loading'), table = document.getElementById('testTable');
    tbody.innerHTML = '';
    allTests.forEach(test => {
        const row = tbody.insertRow();
        row.setAttribute('data-status', test.status);
        row.setAttribute('data-category', test.category);
        row.onclick = () => showTestDetails(test);
        const reqTags = test.requirements.map(r => `<span class="req-tag">${r}</span>`).join('');
        row.innerHTML = `<td>${test.id}</td><td>${test.name}</td><td>${test.category}</td><td><span class="badge badge-${test.status}">${test.status}</span></td><td><span class="badge badge-${test.priority}">${test.priority}</span></td><td><span class="badge badge-${test.severity}">${test.severity}</span></td><td>${test.time}</td><td>${reqTags}</td>`;
    });
    loading.style.display = 'none';
    table.style.display = 'table';
    updateStats();
}
function updateStats() {
    const passed = allTests.filter(t => t.status === 'passed').length, failed = allTests.filter(t => t.status === 'failed').length, skipped = allTests.filter(t => t.status === 'skipped').length, passRate = ((passed / allTests.length) * 100).toFixed(1), categories = new Set(allTests.map(t => t.category)).size;
    document.getElementById('totalTests').textContent = allTests.length;
    document.getElementById('passedCount').textContent = passed;
    document.getElementById('failedCount').textContent = failed;
    document.getElementById('skippedCount').textContent = skipped;
    document.getElementById('passRate').textContent = passRate + '%';
    document.getElementById('categoryCount').textContent = categories;
}
function filterStatus(status) {
    currentFilter = status;
    applyFilters();
    document.querySelectorAll('.controls .filter-group:first-of-type .btn').forEach(btn => btn.classList.remove('active'));
    event.target.classList.add('active');
}
function applyFilters() {
    document.querySelectorAll('#testTableBody tr').forEach(row => {
        const status = row.getAttribute('data-status'), category = row.getAttribute('data-category');
        const statusMatch = currentFilter === 'all' || status === currentFilter, categoryMatch = currentCategory === 'all' || category === currentCategory;
        row.style.display = (statusMatch && categoryMatch) ? '' : 'none';
    });
}
function searchTests() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    document.querySelectorAll('#testTableBody tr').forEach(row => {
        row.style.display = row.textContent.toLowerCase().includes(searchTerm) ? '' : 'none';
    });
}
function showTestDetails(test) {
    const modal = document.getElementById('testModal'), modalBody = document.getElementById('modalBody'), modalTestName = document.getElementById('modalTestName');
    modalTestName.textContent = `${test.id}: ${test.name}`;
    const stepsHTML = test.steps.map((step, i) => `<li>${step}</li>`).join(''), reqTags = test.requirements.map(r => `<span class="req-tag">${r}</span>`).join('');
    const defectsHTML = test.defects.length > 0 ? `<div class="defect-box"><h4><i class="fas fa-bug"></i> Related Defects</h4>${test.defects.map(d => `<p>${d}</p>`).join('')}</div>` : '';
    modalBody.innerHTML = `<div class="detail-grid"><div class="detail-section"><h4>Test Information</h4><p><strong>ID:</strong> ${test.id}</p><p><strong>Category:</strong> ${test.category}</p><p><strong>Status:</strong> <span class="badge badge-${test.status}">${test.status}</span></p><p><strong>Priority:</strong> <span class="badge badge-${test.priority}">${test.priority}</span></p><p><strong>Severity:</strong> <span class="badge badge-${test.severity}">${test.severity}</span></p><p><strong>Execution Time:</strong> ${test.time} ms</p><p><strong>Use Case:</strong> ${test.useCase}</p></div><div class="detail-section"><h4>Action</h4><p>${test.action}</p></div><div class="detail-section"><h4>Expected Results</h4><p>${test.expected}</p></div><div class="detail-section"><h4>Actual Results</h4><p>${test.actual}</p></div></div><div class="detail-section"><h4>Detailed Test Steps</h4><ol style="padding-left: 20px;">${stepsHTML}</ol></div><div class="detail-section"><h4>Related Requirements</h4>${reqTags}</div><div class="detail-section"><h4>Test Location</h4><div class="code-block">${test.fullName}</div><p style="margin-top: 10px;"><code>${test.location}</code></p></div>${defectsHTML}`;
    modal.style.display = 'block';
}
function closeModal() { document.getElementById('testModal').style.display = 'none'; }
function sortTable(colIndex) { console.log('Sorting column:', colIndex); }
function exportToCSV() {
    let csv = 'Test ID,Test Name,Category,Status,Priority,Severity,Time (ms),Requirements,Use Case,Action,Expected,Actual,Location\\n';
    allTests.forEach(test => {
        const req = test.requirements.join(';');
        csv += `"${test.id}","${test.name}","${test.category}","${test.status}","${test.priority}","${test.severity}",${test.time},"${req}","${test.useCase}","${test.action}","${test.expected}","${test.actual}","${test.location}"\\n`;
    });
    const blob = new Blob([csv], { type: 'text/csv' }), url = window.URL.createObjectURL(blob), a = document.createElement('a');
    a.href = url;
    a.download = 'gcc_quality_audit_549_tests_complete.csv';
    a.click();
}
window.onclick = function(event) { if (event.target == document.getElementById('testModal')) closeModal(); }
document.addEventListener('DOMContentLoaded', () => { setTimeout(renderTable, 100); });
    </script>
</body>
</html>'''

# Combine and write
html_content = html_start + js_data + html_end

with open('COMPLETE_QUALITY_AUDIT_REPORT.html', 'w', encoding='utf-8') as f:
    f.write(html_content)

print(f"SUCCESS! HTML file created: COMPLETE_QUALITY_AUDIT_REPORT.html")
print(f"File size: {len(html_content) / 1024:.1f} KB")
print(f"Contains {len(tests_data)} test cases with full audit details")
print(f"Pass rate: {sum(1 for t in tests_data if t['status'] == 'passed') / len(tests_data) * 100:.1f}%")
