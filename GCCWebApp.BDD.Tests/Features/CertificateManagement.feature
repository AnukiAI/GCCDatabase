Feature: Certificate Management
	As a compliance officer
	I want to manage product certificates
	So that I can ensure regulatory compliance

Background:
	Given I am logged in as a compliance officer
	And the following companies exist:
		| Name            | Country | Location   |
		| Tech Corp       | USA     | California |
		| Manufacturing Co| Germany | Berlin     |
	And the following products exist:
		| Name          | Company         | SKU      | Category    |
		| Smart Device  | Tech Corp       | SD-001   | Electronics |
		| Safety Gear   | Manufacturing Co| SG-001   | Safety      |

@smoke @certificate
Scenario: Create a new certificate for a product
	Given I navigate to the certificate creation page
	When I create a certificate with the following details:
		| Field              | Value                |
		| Product            | Smart Device         |
		| Certificate Type   | ISO 9001             |
		| Certificate Number | ISO-2024-001         |
		| Effective Date     | 2024-01-01           |
		| Expiration Date    | 2025-12-31           |
		| Status            | Active               |
	Then the certificate should be created successfully
	And I should see a success message "Certificate created successfully"
	And the certificate should appear in the certificate list

@validation @certificate
Scenario: Validate required fields for certificate creation
	Given I navigate to the certificate creation page
	When I attempt to create a certificate without required fields
	Then I should see validation errors for:
		| Field              | Error Message                          |
		| Product            | Product is required                    |
		| Certificate Type   | Certificate type is required           |
		| Certificate Number | Certificate number is required         |
		| Expiration Date    | Expiration date is required            |

@expiration @certificate
Scenario: Display expiration warning for certificates expiring soon
	Given the following certificates exist:
		| Product      | Certificate Number | Expiration Date | Days Until Expiry |
		| Smart Device | ISO-2024-001      | 30 days from now| 30                |
		| Safety Gear  | CE-2024-001       | 15 days from now| 15                |
	When I view the dashboard
	Then I should see expiration warnings for:
		| Product      | Warning Message                            |
		| Smart Device | Certificate expires in 30 days            |
		| Safety Gear  | Certificate expires in 15 days - Critical |

@search @certificate
Scenario: Search certificates by various criteria
	Given multiple certificates exist in the system
	When I search for certificates by "ISO"
	Then I should only see certificates containing "ISO" in their details
	When I filter by status "Active"
	Then I should only see active certificates
	When I filter by expiration date range
	Then I should only see certificates within that range

@bulk @certificate
Scenario: Bulk upload certificates from CSV
	Given I have a CSV file with certificate data
	When I upload the CSV file
	Then the system should validate all entries
	And show a preview of certificates to be created
	When I confirm the upload
	Then all valid certificates should be created
	And I should see a summary report with:
		| Metric              | Value |
		| Total Processed     | 10    |
		| Successfully Created| 8     |
		| Failed             | 2     |

@audit @certificate
Scenario: Track certificate modification history
	Given a certificate "ISO-2024-001" exists
	When I update the certificate status to "Suspended"
	And I update the expiration date to "2025-06-30"
	Then the audit log should show:
		| Action    | Field           | Old Value   | New Value    | User              | Timestamp |
		| Updated   | Status          | Active      | Suspended    | compliance.officer| Today     |
		| Updated   | Expiration Date | 2025-12-31  | 2025-06-30   | compliance.officer| Today     |