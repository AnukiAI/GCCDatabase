#!/usr/bin/env python3
"""
GCC Database Solution - UAT Test Plan HTML Generator
Converts UAT_TEST_PLAN.md to a comprehensive, visually appealing HTML document
"""

import re
import os
from datetime import datetime

def extract_scenarios(markdown_content):
    """Extract all test scenarios from markdown"""
    scenarios = []

    # Pattern to match scenario headers
    pattern = r'### Test Scenario (\d+\.\d+): (.+?) \((.+?)\)'
    matches = list(re.finditer(pattern, markdown_content))

    for i, match in enumerate(matches):
        scenario_id = match.group(1)
        title = match.group(2)
        priority = match.group(3)

        # Extract content between this scenario and the next
        start_pos = match.end()
        if i < len(matches) - 1:
            end_pos = matches[i + 1].start()
        else:
            # Find UAT Sign-Off or end
            signoff_pos = markdown_content.find('## UAT Sign-Off', start_pos)
            end_pos = signoff_pos if signoff_pos != -1 else len(markdown_content)

        content = markdown_content[start_pos:end_pos].strip()

        # Parse content sections
        parsed = parse_scenario_content(content)

        scenarios.append({
            'id': scenario_id,
            'title': title,
            'priority': priority,
            'section': int(scenario_id.split('.')[0]),
            **parsed
        })

    return scenarios

def parse_scenario_content(content):
    """Parse scenario content into structured data"""
    data = {
        'role': '', 'feature': '', 'description': '',
        'preconditions': [], 'steps': [], 'expected': []
    }

    # Extract role
    role_match = re.search(r'\*\*User Role:\*\* (.+)', content)
    if role_match:
        data['role'] = role_match.group(1)

    # Extract feature
    feature_match = re.search(r'\*\*Feature:\*\* (.+)', content)
    if feature_match:
        data['feature'] = feature_match.group(1)

    # Extract scenario description
    scenario_match = re.search(r'\*\*Scenario:\*\* (.+)', content)
    if scenario_match:
        data['description'] = scenario_match.group(1)

    # Extract pre-conditions
    precond_section = re.search(r'\*\*Pre-conditions:\*\*\s*\n((?:- .+\n?)+)', content)
    if precond_section:
        data['preconditions'] = [line.strip('- ').strip() for line in precond_section.group(1).split('\n') if line.strip()]

    # Extract test steps
    steps_section = re.search(r'\*\*Test Steps:\*\*\s*\n([\s\S]+?)(?=\*\*Expected Results:\*\*)', content)
    if steps_section:
        steps_text = steps_section.group(1)
        # Extract numbered steps
        for match in re.finditer(r'(\d+)\.\s+(.+?)(?=\n\d+\.|\Z)', steps_text, re.DOTALL):
            step = match.group(2).strip()
            data['steps'].append(step)

    # Extract expected results
    expected_section = re.search(r'\*\*Expected Results:\*\*\s*\n((?:- \[ \] .+\n?)+)', content)
    if expected_section:
        data['expected'] = [line.strip('- [ ] ').strip() for line in expected_section.group(1).split('\n') if line.strip()]

    return data

def get_priority_class(priority):
    """Get CSS class for priority level"""
    if 'P0' in priority or 'CRITICAL' in priority:
        return 'badge-p0'
    elif 'P1' in priority or 'HIGH' in priority:
        return 'badge-p1'
    elif 'P2' in priority or 'MEDIUM' in priority:
        return 'badge-p2'
    else:
        return 'badge-p3'

def generate_scenario_html(scenario):
    """Generate HTML for a single test scenario"""
    priority_class = get_priority_class(scenario['priority'])

    # Preconditions HTML
    precond_html = ''
    if scenario['preconditions']:
        precond_list = ''.join([f'<li>{p}</li>' for p in scenario['preconditions']])
        precond_html = f'''
        <div class="content-block">
            <h4>Pre-conditions</h4>
            <div class="pre-conditions">
                <ul style="list-style: disc; padding-left: 20px;">
                    {precond_list}
                </ul>
            </div>
        </div>'''

    # Test steps HTML
    steps_html = ''
    if scenario['steps']:
        steps_list = ''.join([f'<li>{s}</li>' for s in scenario['steps']])
        steps_html = f'''
        <div class="content-block">
            <h4>Test Steps</h4>
            <div class="test-steps">
                <ol>
                    {steps_list}
                </ol>
            </div>
        </div>'''

    # Expected results HTML
    expected_html = ''
    if scenario['expected']:
        expected_list = ''.join([f'<li>{e}</li>' for e in scenario['expected']])
        expected_html = f'''
        <div class="content-block">
            <h4>Expected Results</h4>
            <div class="expected-results">
                <ul>
                    {expected_list}
                </ul>
            </div>
        </div>'''

    scenario_id_safe = scenario['id'].replace('.', '-')

    return f'''
    <div class="test-scenario">
        <div class="scenario-header">
            <div class="scenario-title">
                <h3>Test Scenario {scenario['id']}: {scenario['title']}</h3>
            </div>
            <div class="scenario-meta">
                <span class="meta-badge badge-role">{scenario['role']}</span>
                <span class="meta-badge badge-feature">{scenario['feature']}</span>
                <span class="meta-badge badge-priority {priority_class}">{scenario['priority']}</span>
            </div>
        </div>

        <div class="scenario-content">
            <div class="content-block">
                <h4>Scenario Description</h4>
                <p>{scenario['description']}</p>
            </div>

            {precond_html}
            {steps_html}
            {expected_html}

            <div class="results-section">
                <div class="results-grid">
                    <div class="result-field">
                        <label>Actual Results:</label>
                        <textarea placeholder="Describe what actually happened..."></textarea>
                    </div>
                    <div class="result-field">
                        <label>Test Status:</label>
                        <div class="status-options">
                            <div class="status-option">
                                <input type="checkbox" id="s{scenario_id_safe}-not-tested">
                                <label for="s{scenario_id_safe}-not-tested">Not Tested</label>
                            </div>
                            <div class="status-option">
                                <input type="checkbox" id="s{scenario_id_safe}-pass">
                                <label for="s{scenario_id_safe}-pass">Pass</label>
                            </div>
                            <div class="status-option">
                                <input type="checkbox" id="s{scenario_id_safe}-fail">
                                <label for="s{scenario_id_safe}-fail">Fail</label>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="result-field">
                    <label>Notes/Comments:</label>
                    <textarea placeholder="Any additional observations or issues..."></textarea>
                </div>
            </div>
        </div>
    </div>
    '''

def generate_html(scenarios):
    """Generate complete HTML document"""

    # Group scenarios by section
    sections = {
        1: {'title': 'Public User Scenarios', 'scenarios': []},
        2: {'title': 'Analyst User Scenarios', 'scenarios': []},
        3: {'title': 'Admin User Scenarios', 'scenarios': []},
        4: {'title': 'External Partner User Scenarios', 'scenarios': []},
        5: {'title': 'API Integration Scenarios', 'scenarios': []},
        6: {'title': 'Performance & Load Testing', 'scenarios': []},
        7: {'title': 'Security & Error Handling', 'scenarios': []},
        8: {'title': 'Mobile Responsiveness', 'scenarios': []}
    }

    for scenario in scenarios:
        sections[scenario['section']]['scenarios'].append(scenario)

    # Generate sections HTML
    sections_html = ''
    for section_num, section_data in sections.items():
        scenario_htmls = ''.join([generate_scenario_html(s) for s in section_data['scenarios']])

        sections_html += f'''
        <div class="section" id="section{section_num}">
            <div class="section-header">
                <h2>Section {section_num}: {section_data['title']}</h2>
                <div class="section-subtitle">{len(section_data['scenarios'])} test scenarios</div>
            </div>
            {scenario_htmls}
        </div>
        '''

    # Read CSS from template (embedded in script for simplicity)
    css = get_css_styles()

    # Generate complete HTML
    html_content = f'''<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>GCC Database Solution - User Acceptance Test Plan v1.0</title>
    <style>
{css}
    </style>
</head>
<body>
    <div class="container">
        <!-- Header -->
        <div class="header">
            <h1>GCC Database Solution</h1>
            <div class="subtitle">User Acceptance Test Plan</div>
            <div class="meta-info">
                <div class="meta-item">
                    <strong>Release Version:</strong> 1.0 Production Release
                </div>
                <div class="meta-item">
                    <strong>UAT Window:</strong> 7 Business Days
                </div>
                <div class="meta-item">
                    <strong>Document Date:</strong> {datetime.now().strftime('%B %d, %Y')}
                </div>
                <div class="meta-item">
                    <strong>Status:</strong> Ready for UAT Execution
                </div>
            </div>
        </div>

        <!-- Table of Contents -->
        {generate_toc(sections)}

        <!-- Progress Tracker -->
        <div class="progress-tracker">
            <h3 style="color: var(--secondary-color); margin-bottom: 15px;">Overall Progress</h3>
            <div class="progress-bar">
                <div class="progress-fill" id="progressFill">0% Complete (0 of {len(scenarios)} tests)</div>
            </div>
        </div>

        <!-- Objectives Section -->
        {generate_objectives()}

        <!-- Priority Legend -->
        {generate_priority_legend()}

        <!-- Test Scenarios -->
        {sections_html}

        <!-- Sign-Off Section -->
        {generate_signoff(len(scenarios))}

        <!-- Footer -->
        <div style="text-align: center; padding: 30px; color: var(--gray-dark); border-top: 2px solid var(--gray-medium); margin-top: 40px;">
            <p><strong>UAT Test Plan Version:</strong> 1.0</p>
            <p><strong>Document Created:</strong> {datetime.now().strftime('%B %d, %Y')}</p>
            <p style="margin-top: 15px; font-size: 0.9em;">
                © 2025 GreenCircle Certified. All rights reserved.
            </p>
        </div>
    </div>

    <script>
{get_javascript()}
    </script>
</body>
</html>
'''

    return html_content

def generate_toc(sections):
    """Generate table of contents"""
    toc_items = ''
    for section_num, section_data in sections.items():
        toc_items += f'''
        <div class="toc-item">
            <a href="#section{section_num}">
                <span>Section {section_num}: {section_data['title']}</span>
                <span class="toc-badge">{len(section_data['scenarios'])} Tests</span>
            </a>
        </div>'''

    return f'''
    <div class="toc">
        <h2>Table of Contents</h2>
        <div class="toc-grid">
            <div class="toc-item">
                <a href="#objectives"><span>UAT Objectives</span></a>
            </div>
            {toc_items}
            <div class="toc-item">
                <a href="#signoff"><span>UAT Sign-Off</span></a>
            </div>
        </div>
    </div>'''

def generate_objectives():
    """Generate objectives section"""
    return '''
    <div class="overview" id="objectives">
        <h2>UAT Objectives</h2>
        <div class="objectives-grid mt-20">
            <div class="objective-card">
                <h3>1. Validate Functionality</h3>
                <p>Ensure all features work as designed and meet business requirements</p>
            </div>
            <div class="objective-card">
                <h3>2. Ensure Usability</h3>
                <p>Verify the system is user-friendly and intuitive for all user roles</p>
            </div>
            <div class="objective-card">
                <h3>3. Verify Data Accuracy</h3>
                <p>Confirm data accuracy, security, and performance standards are met</p>
            </div>
            <div class="objective-card">
                <h3>4. Identify Defects</h3>
                <p>Discover any defects or issues before production deployment</p>
            </div>
            <div class="objective-card">
                <h3>5. Obtain Sign-Off</h3>
                <p>Secure stakeholder approval for production release</p>
            </div>
        </div>
    </div>'''

def generate_priority_legend():
    """Generate priority legend"""
    return '''
    <div class="priority-legend">
        <h3>Test Priority Levels</h3>
        <div class="priority-grid">
            <div class="priority-item priority-p0">
                <div class="bold">P0 - CRITICAL</div>
                <div style="font-size: 0.9em; margin-top: 5px;">Core functionality that must work for production release</div>
            </div>
            <div class="priority-item priority-p1">
                <div class="bold">P1 - HIGH</div>
                <div style="font-size: 0.9em; margin-top: 5px;">Important features with significant business impact</div>
            </div>
            <div class="priority-item priority-p2">
                <div class="bold">P2 - MEDIUM</div>
                <div style="font-size: 0.9em; margin-top: 5px;">Secondary features that enhance user experience</div>
            </div>
            <div class="priority-item priority-p3">
                <div class="bold">P3 - LOW</div>
                <div style="font-size: 0.9em; margin-top: 5px;">Nice-to-have features or edge cases</div>
            </div>
        </div>
    </div>'''

def generate_signoff(total_tests):
    """Generate sign-off section"""
    return f'''
    <div class="signoff-section" id="signoff">
        <div class="signoff-header">
            <h2>UAT Sign-Off</h2>
        </div>

        <div class="content-block">
            <h3 style="color: var(--secondary-color); margin-bottom: 20px;">Test Execution Summary</h3>

            <div class="summary-stats">
                <div class="stat-card">
                    <div class="stat-value">{total_tests}</div>
                    <div class="stat-label">Total Scenarios</div>
                </div>
                <div class="stat-card">
                    <div class="stat-value">__</div>
                    <div class="stat-label">Passed</div>
                </div>
                <div class="stat-card">
                    <div class="stat-value">__</div>
                    <div class="stat-label">Failed</div>
                </div>
                <div class="stat-card">
                    <div class="stat-value">__%</div>
                    <div class="stat-label">Pass Rate</div>
                </div>
            </div>

            <table class="signature-table" style="margin-top: 30px;">
                <thead>
                    <tr>
                        <th>Category</th>
                        <th>Total Scenarios</th>
                        <th>Passed</th>
                        <th>Failed</th>
                        <th>Pass Rate</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Public User</td>
                        <td>5</td>
                        <td>__</td>
                        <td>__</td>
                        <td>__%</td>
                    </tr>
                    <tr>
                        <td>Analyst User</td>
                        <td>6</td>
                        <td>__</td>
                        <td>__</td>
                        <td>__%</td>
                    </tr>
                    <tr>
                        <td>Admin User</td>
                        <td>6</td>
                        <td>__</td>
                        <td>__</td>
                        <td>__%</td>
                    </tr>
                    <tr>
                        <td>External User</td>
                        <td>3</td>
                        <td>__</td>
                        <td>__</td>
                        <td>__%</td>
                    </tr>
                    <tr>
                        <td>API Integration</td>
                        <td>3</td>
                        <td>__</td>
                        <td>__</td>
                        <td>__%</td>
                    </tr>
                    <tr>
                        <td>Performance</td>
                        <td>2</td>
                        <td>__</td>
                        <td>__</td>
                        <td>__%</td>
                    </tr>
                    <tr>
                        <td>Security</td>
                        <td>3</td>
                        <td>__</td>
                        <td>__</td>
                        <td>__%</td>
                    </tr>
                    <tr>
                        <td>Mobile</td>
                        <td>1</td>
                        <td>__</td>
                        <td>__</td>
                        <td>__%</td>
                    </tr>
                    <tr style="background: var(--gray-light); font-weight: 600;">
                        <td><strong>TOTAL</strong></td>
                        <td><strong>{total_tests}</strong></td>
                        <td><strong>__</strong></td>
                        <td><strong>__</strong></td>
                        <td><strong>__%</strong></td>
                    </tr>
                </tbody>
            </table>
        </div>

        <div class="decision-section">
            <h3 style="color: var(--secondary-color); margin-bottom: 15px;">Go/No-Go Decision</h3>
            <p><strong>Production Readiness Criteria:</strong></p>
            <ul style="margin: 15px 0; padding-left: 25px;">
                <li>All P0 (Critical) test scenarios pass (100%)</li>
                <li>At least 90% of P1 (High) test scenarios pass</li>
                <li>No critical or high-severity defects remain open</li>
                <li>Performance targets are met</li>
                <li>Security vulnerabilities are addressed</li>
                <li>Stakeholder sign-off obtained</li>
            </ul>

            <div class="decision-options">
                <div class="decision-option">
                    <input type="checkbox" id="decision-go">
                    <label for="decision-go"><strong>GO</strong> - Approved for Production</label>
                </div>
                <div class="decision-option">
                    <input type="checkbox" id="decision-nogo">
                    <label for="decision-nogo"><strong>NO-GO</strong> - Additional work required</label>
                </div>
            </div>
        </div>

        <div class="content-block" style="margin-top: 30px;">
            <h3 style="color: var(--secondary-color); margin-bottom: 20px;">Stakeholder Sign-Off</h3>

            <table class="signature-table">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Role</th>
                        <th>Signature</th>
                        <th>Date</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td><input type="text" style="width: 100%; border: none; background: transparent;" placeholder="Enter name"></td>
                        <td>Product Owner</td>
                        <td style="height: 40px;"></td>
                        <td><input type="text" style="width: 100%; border: none; background: transparent;" placeholder="MM/DD/YYYY"></td>
                    </tr>
                    <tr>
                        <td><input type="text" style="width: 100%; border: none; background: transparent;" placeholder="Enter name"></td>
                        <td>Technical Lead</td>
                        <td style="height: 40px;"></td>
                        <td><input type="text" style="width: 100%; border: none; background: transparent;" placeholder="MM/DD/YYYY"></td>
                    </tr>
                    <tr>
                        <td><input type="text" style="width: 100%; border: none; background: transparent;" placeholder="Enter name"></td>
                        <td>QA Manager</td>
                        <td style="height: 40px;"></td>
                        <td><input type="text" style="width: 100%; border: none; background: transparent;" placeholder="MM/DD/YYYY"></td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>'''

def get_css_styles():
    """Return CSS styles (same as preview template)"""
    return '''
        :root {
            --primary-color: #92C83E;
            --secondary-color: #002B76;
            --accent-color: #FFC107;
            --danger-color: #FF5252;
            --success-color: #4CAF50;
            --warning-color: #FF9800;
            --info-color: #2196F3;
            --gray-light: #F5F5F5;
            --gray-medium: #E0E0E0;
            --gray-dark: #666666;
            --text-dark: #333333;
            --border-radius: 8px;
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: var(--text-dark);
            background-color: #FAFAFA;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
        }

        /* Header Section */
        .header {
            background: linear-gradient(135deg, var(--secondary-color) 0%, #004BA0 100%);
            color: white;
            padding: 40px 20px;
            text-align: center;
            border-radius: var(--border-radius);
            margin-bottom: 30px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }

        .header h1 {
            font-size: 2.5em;
            margin-bottom: 10px;
            font-weight: 600;
        }

        .header .subtitle {
            font-size: 1.2em;
            opacity: 0.95;
            margin-bottom: 20px;
        }

        .header .meta-info {
            display: flex;
            justify-content: center;
            gap: 30px;
            flex-wrap: wrap;
            margin-top: 20px;
            font-size: 0.95em;
        }

        .meta-item {
            background: rgba(255, 255, 255, 0.1);
            padding: 10px 20px;
            border-radius: 5px;
            backdrop-filter: blur(10px);
        }

        .meta-item strong {
            color: var(--primary-color);
        }

        /* Table of Contents */
        .toc {
            background: white;
            padding: 30px;
            border-radius: var(--border-radius);
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            margin-bottom: 30px;
        }

        .toc h2 {
            color: var(--secondary-color);
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 3px solid var(--primary-color);
        }

        .toc-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 15px;
            margin-top: 20px;
        }

        .toc-item {
            background: var(--gray-light);
            padding: 15px;
            border-radius: 5px;
            border-left: 4px solid var(--primary-color);
            transition: all 0.3s ease;
        }

        .toc-item:hover {
            background: #E8F5E9;
            transform: translateX(5px);
            box-shadow: 0 2px 8px rgba(146, 200, 62, 0.2);
        }

        .toc-item a {
            color: var(--secondary-color);
            text-decoration: none;
            font-weight: 500;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .toc-badge {
            background: var(--primary-color);
            color: white;
            padding: 3px 10px;
            border-radius: 12px;
            font-size: 0.85em;
            font-weight: 600;
        }

        /* Overview Section */
        .overview {
            background: white;
            padding: 30px;
            border-radius: var(--border-radius);
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            margin-bottom: 30px;
        }

        .overview h2 {
            color: var(--secondary-color);
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 3px solid var(--primary-color);
        }

        .objectives-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }

        .objective-card {
            background: linear-gradient(135deg, #E8F5E9 0%, #F1F8E9 100%);
            padding: 20px;
            border-radius: var(--border-radius);
            border-left: 4px solid var(--primary-color);
        }

        .objective-card h3 {
            color: var(--secondary-color);
            margin-bottom: 10px;
            font-size: 1.1em;
        }

        /* Priority Legend */
        .priority-legend {
            background: white;
            padding: 25px;
            border-radius: var(--border-radius);
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            margin-bottom: 30px;
        }

        .priority-legend h3 {
            color: var(--secondary-color);
            margin-bottom: 15px;
        }

        .priority-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
        }

        .priority-item {
            padding: 15px;
            border-radius: 5px;
            border-left: 4px solid;
            font-weight: 500;
        }

        .priority-p0 {
            background: #FFEBEE;
            border-color: var(--danger-color);
            color: #C62828;
        }

        .priority-p1 {
            background: #FFF3E0;
            border-color: var(--warning-color);
            color: #E65100;
        }

        .priority-p2 {
            background: #FFF9C4;
            border-color: var(--accent-color);
            color: #F57F17;
        }

        .priority-p3 {
            background: #E3F2FD;
            border-color: var(--info-color);
            color: #1565C0;
        }

        /* Test Scenario Section */
        .section {
            background: white;
            padding: 30px;
            border-radius: var(--border-radius);
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            margin-bottom: 30px;
            page-break-inside: avoid;
        }

        .section-header {
            background: linear-gradient(135deg, var(--secondary-color) 0%, #004BA0 100%);
            color: white;
            padding: 20px;
            border-radius: var(--border-radius);
            margin: -30px -30px 30px -30px;
        }

        .section-header h2 {
            font-size: 1.8em;
            margin-bottom: 5px;
        }

        .section-header .section-subtitle {
            opacity: 0.9;
            font-size: 1.05em;
        }

        /* Test Scenario Card */
        .test-scenario {
            background: #FAFAFA;
            border: 2px solid var(--gray-medium);
            border-radius: var(--border-radius);
            padding: 25px;
            margin-bottom: 25px;
            page-break-inside: avoid;
        }

        .scenario-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 20px;
            flex-wrap: wrap;
            gap: 10px;
        }

        .scenario-title {
            flex: 1;
            min-width: 300px;
        }

        .scenario-title h3 {
            color: var(--secondary-color);
            font-size: 1.3em;
            margin-bottom: 10px;
        }

        .scenario-meta {
            display: flex;
            gap: 10px;
            flex-wrap: wrap;
        }

        .meta-badge {
            padding: 5px 15px;
            border-radius: 15px;
            font-size: 0.85em;
            font-weight: 600;
            white-space: nowrap;
        }

        .badge-role {
            background: #E3F2FD;
            color: #1976D2;
        }

        .badge-feature {
            background: #F3E5F5;
            color: #7B1FA2;
        }

        .badge-priority {
            color: white;
        }

        .badge-p0 {
            background: var(--danger-color);
        }

        .badge-p1 {
            background: var(--warning-color);
        }

        .badge-p2 {
            background: var(--accent-color);
        }

        .badge-p3 {
            background: var(--info-color);
        }

        /* Scenario Content */
        .scenario-content {
            margin-top: 20px;
        }

        .content-block {
            margin-bottom: 20px;
        }

        .content-block h4 {
            color: var(--secondary-color);
            margin-bottom: 10px;
            font-size: 1.05em;
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .content-block h4::before {
            content: '';
            width: 4px;
            height: 20px;
            background: var(--primary-color);
            border-radius: 2px;
        }

        .pre-conditions, .test-steps, .expected-results {
            background: white;
            padding: 15px;
            border-radius: 5px;
            border-left: 3px solid var(--primary-color);
        }

        .test-steps ol {
            padding-left: 20px;
        }

        .test-steps li {
            margin-bottom: 10px;
            line-height: 1.6;
        }

        .expected-results ul {
            list-style: none;
            padding-left: 0;
        }

        .expected-results li {
            padding-left: 30px;
            margin-bottom: 8px;
            position: relative;
        }

        .expected-results li::before {
            content: '□';
            position: absolute;
            left: 5px;
            font-size: 1.2em;
            color: var(--primary-color);
        }

        /* Results Section */
        .results-section {
            background: linear-gradient(135deg, #F5F5F5 0%, #EEEEEE 100%);
            padding: 20px;
            border-radius: var(--border-radius);
            margin-top: 20px;
        }

        .results-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 15px;
            margin-bottom: 15px;
        }

        .result-field {
            background: white;
            padding: 15px;
            border-radius: 5px;
            border: 1px solid var(--gray-medium);
        }

        .result-field label {
            display: block;
            font-weight: 600;
            color: var(--secondary-color);
            margin-bottom: 8px;
        }

        .result-field input[type="text"],
        .result-field textarea {
            width: 100%;
            padding: 8px;
            border: 1px solid var(--gray-medium);
            border-radius: 4px;
            font-family: inherit;
        }

        .result-field textarea {
            min-height: 60px;
            resize: vertical;
        }

        .status-options {
            display: flex;
            gap: 15px;
            flex-wrap: wrap;
        }

        .status-option {
            display: flex;
            align-items: center;
            gap: 5px;
        }

        .status-option input[type="checkbox"] {
            width: 18px;
            height: 18px;
            cursor: pointer;
        }

        /* Sign-off Section */
        .signoff-section {
            background: white;
            padding: 30px;
            border-radius: var(--border-radius);
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            margin-top: 40px;
        }

        .signoff-header {
            background: var(--secondary-color);
            color: white;
            padding: 20px;
            border-radius: var(--border-radius);
            margin: -30px -30px 30px -30px;
            text-align: center;
        }

        .summary-stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin: 30px 0;
        }

        .stat-card {
            background: linear-gradient(135deg, #E8F5E9 0%, #F1F8E9 100%);
            padding: 20px;
            border-radius: var(--border-radius);
            text-align: center;
            border-bottom: 4px solid var(--primary-color);
        }

        .stat-value {
            font-size: 2.5em;
            font-weight: 700;
            color: var(--secondary-color);
            margin-bottom: 5px;
        }

        .stat-label {
            color: var(--gray-dark);
            font-size: 0.9em;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .signature-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 30px;
        }

        .signature-table th,
        .signature-table td {
            padding: 15px;
            border: 1px solid var(--gray-medium);
            text-align: left;
        }

        .signature-table th {
            background: var(--secondary-color);
            color: white;
            font-weight: 600;
        }

        .signature-table tr:nth-child(even) {
            background: var(--gray-light);
        }

        /* Decision Section */
        .decision-section {
            background: linear-gradient(135deg, #FFF9C4 0%, #FFF59D 100%);
            padding: 25px;
            border-radius: var(--border-radius);
            margin: 30px 0;
            border-left: 5px solid var(--accent-color);
        }

        .decision-options {
            display: flex;
            gap: 20px;
            margin-top: 15px;
            flex-wrap: wrap;
        }

        .decision-option {
            background: white;
            padding: 15px 25px;
            border-radius: 5px;
            border: 2px solid transparent;
            cursor: pointer;
            transition: all 0.3s ease;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .decision-option:hover {
            border-color: var(--primary-color);
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
        }

        .decision-option input[type="checkbox"] {
            width: 20px;
            height: 20px;
        }

        /* Progress Tracker */
        .progress-tracker {
            background: white;
            padding: 25px;
            border-radius: var(--border-radius);
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            margin-bottom: 30px;
        }

        .progress-bar {
            background: var(--gray-light);
            height: 30px;
            border-radius: 15px;
            overflow: hidden;
            position: relative;
        }

        .progress-fill {
            background: linear-gradient(90deg, var(--primary-color) 0%, #7AB62E 100%);
            height: 100%;
            width: 0%;
            transition: width 0.5s ease;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-weight: 600;
            font-size: 0.9em;
        }

        /* Utility Classes */
        .mt-20 {
            margin-top: 20px;
        }

        .bold {
            font-weight: 600;
        }

        /* Print Styles */
        @media print {
            body {
                background: white;
            }

            .container {
                max-width: 100%;
            }

            .section,
            .test-scenario {
                page-break-inside: avoid;
            }

            .header {
                background: var(--secondary-color);
                -webkit-print-color-adjust: exact;
                print-color-adjust: exact;
            }

            @page {
                margin: 1.5cm;
            }
        }

        /* Responsive Design */
        @media (max-width: 768px) {
            .header h1 {
                font-size: 1.8em;
            }

            .header .meta-info {
                flex-direction: column;
                gap: 10px;
            }

            .toc-grid {
                grid-template-columns: 1fr;
            }

            .scenario-header {
                flex-direction: column;
            }

            .results-grid {
                grid-template-columns: 1fr;
            }
        }'''

def get_javascript():
    """Return JavaScript code"""
    return '''
        // Progress tracking functionality
        function updateProgress() {
            const checkboxes = document.querySelectorAll('.status-option input[type="checkbox"]');
            const passCheckboxes = Array.from(checkboxes).filter(cb => cb.id.includes('-pass'));
            const checkedPasses = passCheckboxes.filter(cb => cb.checked).length;
            const totalTests = passCheckboxes.length;
            const percentage = Math.round((checkedPasses / totalTests) * 100);

            const progressFill = document.getElementById('progressFill');
            progressFill.style.width = percentage + '%';
            progressFill.textContent = percentage + '% Complete (' + checkedPasses + ' of ' + totalTests + ' tests)';
        }

        // Add event listeners to all checkboxes
        document.addEventListener('DOMContentLoaded', function() {
            const checkboxes = document.querySelectorAll('.status-option input[type="checkbox"]');
            checkboxes.forEach(cb => {
                cb.addEventListener('change', updateProgress);
            });

            // Smooth scrolling for navigation
            document.querySelectorAll('a[href^="#"]').forEach(anchor => {
                anchor.addEventListener('click', function (e) {
                    e.preventDefault();
                    const target = document.querySelector(this.getAttribute('href'));
                    if target) {
                        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }
                });
            });

            // Print functionality
            window.printTestPlan = function() {
                window.print();
            };
        });'''

def main():
    """Main function"""
    print("GCC Database Solution - UAT Test Plan HTML Generator")
    print("=" * 60)

    # Read markdown file
    md_file = "UAT_TEST_PLAN.md"
    if not os.path.exists(md_file):
        print(f"Error: {md_file} not found!")
        return

    print(f"Reading {md_file}...")
    with open(md_file, 'r', encoding='utf-8') as f:
        content = f.read()

    # Extract scenarios
    print("Extracting test scenarios...")
    scenarios = extract_scenarios(content)
    print(f"Found {len(scenarios)} test scenarios")

    # Generate HTML
    print("Generating HTML...")
    html = generate_html(scenarios)

    # Write output
    output_file = "UAT_TEST_PLAN_COMPLETE.html"
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(html)

    print(f"\nSuccess! Generated {output_file}")
    print(f"File size: {len(html)} characters")
    print(f"\nOpen the file in your browser to view the comprehensive UAT test plan.")
    print("=" * 60)

if __name__ == "__main__":
    main()
