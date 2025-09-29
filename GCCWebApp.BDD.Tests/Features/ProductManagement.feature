Feature: Product Management
	As a product manager
	I want to manage products and their identifiers
	So that I can maintain accurate product information

Background:
	Given I am logged in as a product manager
	And the following product categories exist:
		| Name         | Description                    |
		| Electronics  | Electronic devices and gadgets |
		| Safety      | Safety equipment and gear      |
		| Medical     | Medical devices and supplies   |

@smoke @product
Scenario: Create a new product with multiple identifiers
	Given I navigate to the product creation page
	When I create a product with the following details:
		| Field          | Value                          |
		| Name           | Advanced Medical Scanner       |
		| Category       | Medical                        |
		| Description    | High-resolution medical scanner|
		| SKU            | AMS-2024-001                  |
		| Model          | Scanner Pro X1                 |
	And I add the following product identifiers:
		| Type    | Value            |
		| UPC     | 123456789012     |
		| ASIN    | B08XYZ12345      |
		| EAN     | 1234567890123    |
		| GTIN    | 12345678901234   |
	Then the product should be created successfully
	And all identifiers should be associated with the product

@hierarchy @product
Scenario: Manage product type hierarchy
	Given I navigate to the product type management page
	When I create a product type hierarchy:
		| Parent Category | Product Type     | Child Types                    |
		| Electronics     | Computers        | Laptops, Desktops, Tablets    |
		| Electronics     | Mobile Devices   | Smartphones, Smartwatches      |
		| Safety         | Personal Safety  | Helmets, Gloves, Goggles      |
	Then the hierarchy should be created with proper relationships
	And products should be assignable to any level in the hierarchy

@validation @product
Scenario: Validate unique product identifiers
	Given a product exists with UPC "123456789012"
	When I try to create another product with the same UPC
	Then I should see an error "UPC 123456789012 is already in use"
	And the product should not be created

@import @product
Scenario: Import products from external systems
	Given I have product data from an external system
	When I initiate the product import process
	Then the system should:
		| Step                    | Result                                |
		| Validate data format    | All fields mapped correctly           |
		| Check for duplicates    | 3 duplicate SKUs found                |
		| Validate identifiers    | All identifiers valid                 |
		| Create new products     | 47 products created successfully      |
		| Update existing products| 3 products updated                    |
	And I should receive an import summary report

@bulk-edit @product
Scenario: Bulk edit product attributes
	Given I have selected 25 products
	When I apply bulk edits:
		| Field     | Action        | Value        |
		| Category  | Change to     | Electronics  |
		| Status    | Set as        | Active       |
		| Tags      | Add           | New Arrival  |
	Then all selected products should be updated
	And the audit log should track all changes

@product-certificate @integration
Scenario: Link products with certificates
	Given a product "Medical Scanner X1" exists
	When I associate the following certificates:
		| Certificate Type | Number        | Expiration   |
		| FDA Approval    | FDA-2024-001  | 2026-12-31   |
		| CE Mark         | CE-2024-002   | 2025-06-30   |
		| ISO 13485       | ISO-2024-003  | 2025-12-31   |
	Then the certificates should be linked to the product
	And the product detail page should display all certificates
	And certificate expiration should trigger product warnings

@search @product
Scenario Outline: Search products with advanced filters
	Given multiple products exist in various categories
	When I search with the following criteria:
		| Filter Type   | Value             |
		| <FilterType>  | <FilterValue>     |
	Then I should see <ExpectedCount> products
	And all results should match the search criteria

	Examples:
		| FilterType      | FilterValue        | ExpectedCount |
		| Category        | Electronics        | 15            |
		| Identifier Type | ASIN              | 8             |
		| Certificate     | ISO 9001          | 12            |
		| Status         | Active            | 45            |
		| Date Range     | Last 30 days      | 5             |