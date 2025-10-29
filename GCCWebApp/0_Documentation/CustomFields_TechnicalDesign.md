# CustomFields Feature - Technical Design Specification

**Project**: GCC Database Solution
**Feature**: Dynamic Custom Fields for Certificates
**Version**: 1.0
**Date**: October 28, 2025
**Status**: Design Phase

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [API Design](#api-design)
3. [Database Design](#database-design)
4. [Frontend Design](#frontend-design)
5. [Backend Design](#backend-design)
6. [Validation Framework](#validation-framework)
7. [Export Integration](#export-integration)
8. [Performance Optimization](#performance-optimization)
9. [Testing Strategy](#testing-strategy)
10. [Security Considerations](#security-considerations)

---

## Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Presentation Layer                       │
├─────────────────────────────────────────────────────────────────┤
│  Certificate Create/Edit Views                                   │
│  - Razor Pages with dynamic field rendering                      │
│  - JavaScript for AJAX field loading                            │
│  - Partial view: _CustomFieldInput.cshtml                       │
│                                                                   │
│  Certificate Details View                                        │
│  - Read-only custom field display                               │
│  - Grouped by category                                          │
│                                                                   │
│  PDF/CSV Export                                                  │
│  - QuestPDF for PDF generation                                  │
│  - Dynamic CSV columns                                          │
└─────────────────────────────────────────────────────────────────┘
                              ↓ ↑
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer (Controllers)                  │
├─────────────────────────────────────────────────────────────────┤
│  CertificateController                                           │
│  - Create/Edit actions with custom field binding                │
│  - CustomFieldValue mapping                                     │
│                                                                   │
│  CustomFieldsApiController                                       │
│  - GET /api/CustomFields/ByCertificationType/{id}              │
│  - GET /api/CustomFields/Values/{certificateId}                │
└─────────────────────────────────────────────────────────────────┘
                              ↓ ↑
┌─────────────────────────────────────────────────────────────────┐
│                         Business Logic Layer                     │
├─────────────────────────────────────────────────────────────────┤
│  CustomFieldService                                              │
│  - GetFieldsByCertificationType()                               │
│  - SaveCustomFieldValues()                                      │
│  - ValidateFieldValue()                                         │
│                                                                   │
│  CustomFieldValueMapper                                          │
│  - MapFormDataToValues()                                        │
│  - MapValuesToViewModel()                                       │
│                                                                   │
│  CustomFieldValidator                                            │
│  - ParseValidationRules()                                       │
│  - ApplyValidation()                                            │
└─────────────────────────────────────────────────────────────────┘
                              ↓ ↑
┌─────────────────────────────────────────────────────────────────┐
│                         Data Access Layer                        │
├─────────────────────────────────────────────────────────────────┤
│  ApplicationDbContext                                            │
│  - DbSet<CustomField>                                           │
│  - DbSet<CustomFieldValue>                                      │
│                                                                   │
│  Repositories (if needed)                                        │
│  - CustomFieldRepository                                        │
│  - CustomFieldValueRepository                                   │
└─────────────────────────────────────────────────────────────────┘
                              ↓ ↑
┌─────────────────────────────────────────────────────────────────┐
│                         Database (SQL Server)                    │
├─────────────────────────────────────────────────────────────────┤
│  Tables: CustomFields, CustomFieldValues                        │
│  Indexes: CertificateId, FieldId, CertificateTypeId            │
└─────────────────────────────────────────────────────────────────┘
```

### Design Patterns
- **Repository Pattern**: Abstraction over data access (optional, can use DbContext directly)
- **Mapper Pattern**: Convert between domain models and DTOs/ViewModels
- **Strategy Pattern**: Different renderers for different field types
- **Builder Pattern**: Construct complex validation rules
- **Factory Pattern**: Create field controls based on FieldType

### Technology Stack
- **Backend**: ASP.NET Core 8.0 MVC, Entity Framework Core 9.0.9
- **Frontend**: Razor Pages, jQuery, Bootstrap 5
- **Database**: SQL Server
- **PDF Generation**: QuestPDF
- **Excel Export**: ClosedXML
- **Validation**: FluentValidation

---

## API Design

### REST API Endpoints

#### 1. Get Custom Fields by Certification Type
```http
GET /api/CustomFields/ByCertificationType/{certificationTypeId}
Authorization: Cookie (GCCAuth) or API Key
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Certificate Holder Company Name",
      "fieldType": "text",
      "isRequired": true,
      "isVisible": true,
      "isPublic": true,
      "sortOrder": 1,
      "options": "",
      "defaultValue": "",
      "validationRules": "{\"maxLength\":255}",
      "section": "Basic Certificate Information"
    },
    {
      "id": 16,
      "name": "Third-Party Certifications",
      "fieldType": "multiselect",
      "isRequired": false,
      "isVisible": true,
      "isPublic": true,
      "sortOrder": 16,
      "options": "LEED,Energy Star,GREENGUARD,Cradle to Cradle,FSC,PEFC,SFI,Carbon Neutral,ISO 14001,Other",
      "defaultValue": "",
      "validationRules": "{}",
      "section": "Environmental and Sustainability"
    }
  ]
}
```

**Controller Code:**
```csharp
[HttpGet("api/CustomFields/ByCertificationType/{certificationTypeId}")]
public async Task<IActionResult> GetByCertificationType(int certificationTypeId)
{
    try
    {
        var fields = await _customFieldService.GetFieldsByCertificationTypeAsync(certificationTypeId);
        return Json(new ApiResponse<List<CustomFieldDto>>
        {
            Success = true,
            Data = fields
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading custom fields for certification type {TypeId}", certificationTypeId);
        return Json(new ApiResponse<List<CustomFieldDto>>
        {
            Success = false,
            Message = "Failed to load custom fields"
        });
    }
}
```

---

#### 2. Get Custom Field Values for Certificate
```http
GET /api/CustomFields/Values/{certificateId}
Authorization: Cookie (GCCAuth) or API Key
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "fieldId": 1,
      "fieldName": "Certificate Holder Company Name",
      "fieldType": "text",
      "value": "ACME Corporation"
    },
    {
      "fieldId": 17,
      "fieldName": "Carbon Footprint (kg CO2e)",
      "fieldType": "number",
      "value": "125.5"
    },
    {
      "fieldId": 14,
      "fieldName": "Manufacturing Date",
      "fieldType": "date",
      "value": "2025-03-15"
    }
  ]
}
```

---

#### 3. Save Custom Field Values (via Form POST)
```http
POST /Certificate/Create
POST /Certificate/Edit/{id}
Content-Type: application/x-www-form-urlencoded

CertificationTypeId=1
&CompanyId=5
&ProductId=12
&customField_1=ACME%20Corporation
&customField_17=125.5
&customField_14=2025-03-15
&__RequestVerificationToken=...
```

**Processing Logic:**
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CertificateEditViewModel model, IFormCollection form)
{
    if (!ModelState.IsValid)
        return View(model);

    // Save standard certificate data
    var certificate = await _certificateService.CreateCertificateAsync(model);

    // Extract and save custom field values
    var customFieldValues = _customFieldMapper.ExtractFromForm(form, certificate.Id, model.CertificationTypeId);
    await _customFieldService.SaveCustomFieldValuesAsync(customFieldValues);

    return RedirectToAction("Details", new { id = certificate.Id });
}
```

---

## Database Design

### Entity Relationship Diagram
```
┌──────────────────────┐
│ CertificationTypes   │
│──────────────────────│
│ Id (PK)              │
│ Name                 │
│ ...                  │
└──────────────────────┘
          ↑
          │ 1
          │
          │ N
┌──────────────────────┐
│ CustomFields         │
│──────────────────────│
│ Id (PK)              │◄───┐
│ Name                 │    │
│ FieldType            │    │ 1
│ IsRequired           │    │
│ IsVisible            │    │
│ IsPublic             │    │
│ SortOrder            │    │
│ Options              │    │
│ DefaultValue         │    │
│ ValidationRules      │    │
│ CertificateTypeId(FK)│    │
│ Section              │    │
│ IsDeleted            │    │ N
│ DeletedAt            │    │
└──────────────────────┘    │
                            │
                            │
┌──────────────────────┐    │
│ Certificates         │    │
│──────────────────────│    │
│ Id (PK)              │◄───┼───┐
│ Number               │    │   │
│ CertificationTypeId  │    │   │
│ ...                  │    │   │ 1
└──────────────────────┘    │   │
                            │   │
                            │   │ N
                     ┌──────────────────────┐
                     │ CustomFieldValues    │
                     │──────────────────────│
                     │ Id (PK)              │
                     │ CertificateId (FK)   │───┘
                     │ FieldId (FK)         │───┘
                     │ TextValue            │
                     │ NumberValue          │
                     │ DateValue            │
                     │ BooleanValue         │
                     │ CreatedAt            │
                     │ UpdatedAt            │
                     └──────────────────────┘
```

### Table Modifications

#### CustomFields Table (Add Columns)
```sql
ALTER TABLE CustomFields ADD Section NVARCHAR(100);
ALTER TABLE CustomFields ADD IsDeleted BIT NOT NULL DEFAULT 0;
ALTER TABLE CustomFields ADD DeletedAt DATETIME2;
```

#### Indexes for Performance
```sql
-- Primary lookup: fields by certification type, ordered by sort
CREATE INDEX IX_CustomFields_Type_Sort
ON CustomFields(CertificateTypeId, SortOrder)
WHERE IsDeleted = 0;

-- Values by certificate (most common query)
CREATE INDEX IX_CustomFieldValues_Certificate
ON CustomFieldValues(CertificateId)
INCLUDE (FieldId, TextValue, NumberValue, DateValue, BooleanValue);

-- Values by field (for analytics)
CREATE INDEX IX_CustomFieldValues_Field
ON CustomFieldValues(FieldId);

-- Composite for joins
CREATE INDEX IX_CustomFieldValues_Composite
ON CustomFieldValues(CertificateId, FieldId);
```

---

## Frontend Design

### Partial View: _CustomFieldInput.cshtml
```cshtml
@model CustomFieldDto

@{
    var fieldName = $"customField_{Model.Id}";
    var isRequired = Model.IsRequired ? "required" : "";
    var requiredMark = Model.IsRequired ? "<span class='text-danger'>*</span>" : "";
}

<div class="form-group custom-field-group" data-field-type="@Model.FieldType">
    <label for="@fieldName">
        @Model.Name @Html.Raw(requiredMark)
        @if (!string.IsNullOrEmpty(Model.HelpText))
        {
            <i class="fas fa-info-circle" data-toggle="tooltip" title="@Model.HelpText"></i>
        }
    </label>

    @switch (Model.FieldType)
    {
        case "text":
        case "email":
        case "phone":
            <input type="@(Model.FieldType == "text" ? "text" : Model.FieldType)"
                   id="@fieldName"
                   name="@fieldName"
                   value="@Model.Value"
                   class="form-control"
                   @isRequired
                   data-validation-rules='@Model.ValidationRules' />
            break;

        case "number":
            var rules = JsonSerializer.Deserialize<ValidationRulesDto>(Model.ValidationRules ?? "{}");
            <input type="number"
                   id="@fieldName"
                   name="@fieldName"
                   value="@Model.Value"
                   class="form-control"
                   min="@rules.Min"
                   max="@rules.Max"
                   step="@(rules.Step ?? "any")"
                   @isRequired />
            break;

        case "date":
            <input type="date"
                   id="@fieldName"
                   name="@fieldName"
                   value="@Model.Value"
                   class="form-control"
                   @isRequired />
            break;

        case "textarea":
            <textarea id="@fieldName"
                      name="@fieldName"
                      class="form-control"
                      rows="4"
                      @isRequired>@Model.Value</textarea>
            break;

        case "select":
            var options = Model.Options?.Split(',') ?? Array.Empty<string>();
            <select id="@fieldName"
                    name="@fieldName"
                    class="form-control"
                    @isRequired>
                <option value="">-- Select --</option>
                @foreach (var option in options)
                {
                    var selected = option == Model.Value ? "selected" : "";
                    <option value="@option" @selected>@option</option>
                }
            </select>
            break;

        case "multiselect":
            var multioptions = Model.Options?.Split(',') ?? Array.Empty<string>();
            var selectedValues = Model.Value?.Split(',') ?? Array.Empty<string>();
            <select id="@fieldName"
                    name="@fieldName"
                    class="form-control"
                    multiple
                    @isRequired>
                @foreach (var option in multioptions)
                {
                    var selected = selectedValues.Contains(option) ? "selected" : "";
                    <option value="@option" @selected>@option</option>
                }
            </select>
            break;

        case "boolean":
            var isChecked = Model.Value?.ToLower() == "true" ? "checked" : "";
            <div class="form-check">
                <input type="checkbox"
                       id="@fieldName"
                       name="@fieldName"
                       class="form-check-input"
                       value="true"
                       @isChecked />
                <label class="form-check-label" for="@fieldName">Yes</label>
            </div>
            break;
    }

    <span class="text-danger field-validation-error" data-field="@fieldName"></span>
</div>
```

### JavaScript: Dynamic Field Loading

**File**: `wwwroot/js/certificate-create-customfields.js`

```javascript
class CustomFieldsManager {
    constructor() {
        this.fields = [];
        this.container = document.getElementById('custom-fields-container');
        this.certTypeSelect = document.getElementById('CertificationTypeId');
        this.init();
    }

    init() {
        if (this.certTypeSelect) {
            this.certTypeSelect.addEventListener('change', () => this.loadFields());

            // Load fields if certification type already selected (edit mode)
            if (this.certTypeSelect.value) {
                this.loadFields();
            }
        }
    }

    async loadFields() {
        const certTypeId = this.certTypeSelect.value;
        if (!certTypeId) {
            this.clearFields();
            return;
        }

        this.showLoading();

        try {
            const response = await fetch(`/api/CustomFields/ByCertificationType/${certTypeId}`);
            const result = await response.json();

            if (result.success) {
                this.fields = result.data;
                this.renderFields();
            } else {
                this.showError('Failed to load custom fields');
            }
        } catch (error) {
            console.error('Error loading custom fields:', error);
            this.showError('Error loading custom fields');
        }
    }

    renderFields() {
        this.container.innerHTML = '';

        if (this.fields.length === 0) {
            this.container.innerHTML = '<p class="text-muted">No additional fields required for this certification type.</p>';
            return;
        }

        // Group fields by section
        const sections = this.groupBySection(this.fields);

        sections.forEach(section => {
            const sectionHtml = this.renderSection(section);
            this.container.insertAdjacentHTML('beforeend', sectionHtml);
        });

        // Initialize tooltips
        $('[data-toggle="tooltip"]').tooltip();
    }

    groupBySection(fields) {
        const grouped = {};

        fields.forEach(field => {
            const section = field.section || this.inferSection(field.name);
            if (!grouped[section]) {
                grouped[section] = [];
            }
            grouped[section].push(field);
        });

        return Object.entries(grouped).map(([name, fields]) => ({
            name,
            fields: fields.sort((a, b) => a.sortOrder - b.sortOrder)
        }));
    }

    inferSection(fieldName) {
        // Infer section from field name patterns
        if (fieldName.toLowerCase().includes('company') || fieldName.toLowerCase().includes('holder')) {
            return 'Basic Certificate Information';
        }
        if (fieldName.toLowerCase().includes('product') || fieldName.toLowerCase().includes('brand')) {
            return 'Product Information';
        }
        if (fieldName.toLowerCase().includes('manufacturing') || fieldName.toLowerCase().includes('facility')) {
            return 'Manufacturing Information';
        }
        if (fieldName.toLowerCase().includes('environmental') || fieldName.toLowerCase().includes('carbon')) {
            return 'Environmental and Sustainability';
        }
        if (fieldName.toLowerCase().includes('test') || fieldName.toLowerCase().includes('compliance')) {
            return 'Testing and Compliance';
        }
        return 'Additional Information';
    }

    renderSection(section) {
        const fieldsHtml = section.fields.map(field => this.renderField(field)).join('');

        return `
            <div class="custom-fields-section">
                <div class="form-section-header">
                    <i class="fas fa-cog"></i>
                    <span>${section.name}</span>
                </div>
                <div class="form-grid">
                    ${fieldsHtml}
                </div>
            </div>
        `;
    }

    renderField(field) {
        const fieldName = `customField_${field.id}`;
        const isRequired = field.isRequired ? 'required' : '';
        const requiredMark = field.isRequired ? '<span class="text-danger">*</span>' : '';

        let inputHtml = '';

        switch (field.fieldType) {
            case 'text':
            case 'email':
            case 'phone':
                inputHtml = `<input type="${field.fieldType === 'text' ? 'text' : field.fieldType}"
                    id="${fieldName}"
                    name="${fieldName}"
                    class="form-control"
                    ${isRequired}
                    data-validation-rules='${field.validationRules || '{}'}' />`;
                break;

            case 'number':
                const rules = JSON.parse(field.validationRules || '{}');
                inputHtml = `<input type="number"
                    id="${fieldName}"
                    name="${fieldName}"
                    class="form-control"
                    min="${rules.min || ''}"
                    max="${rules.max || ''}"
                    step="${rules.step || 'any'}"
                    ${isRequired} />`;
                break;

            case 'date':
                inputHtml = `<input type="date"
                    id="${fieldName}"
                    name="${fieldName}"
                    class="form-control"
                    ${isRequired} />`;
                break;

            case 'textarea':
                inputHtml = `<textarea id="${fieldName}"
                    name="${fieldName}"
                    class="form-control"
                    rows="4"
                    ${isRequired}></textarea>`;
                break;

            case 'select':
                const options = (field.options || '').split(',');
                const optionsHtml = options.map(opt =>
                    `<option value="${opt.trim()}">${opt.trim()}</option>`
                ).join('');
                inputHtml = `<select id="${fieldName}"
                    name="${fieldName}"
                    class="form-control"
                    ${isRequired}>
                    <option value="">-- Select --</option>
                    ${optionsHtml}
                </select>`;
                break;

            case 'multiselect':
                const multiopts = (field.options || '').split(',');
                const multioptsHtml = multiopts.map(opt =>
                    `<option value="${opt.trim()}">${opt.trim()}</option>`
                ).join('');
                inputHtml = `<select id="${fieldName}"
                    name="${fieldName}"
                    class="form-control"
                    multiple
                    ${isRequired}>
                    ${multioptsHtml}
                </select>`;
                break;

            case 'boolean':
                inputHtml = `<div class="form-check">
                    <input type="checkbox"
                        id="${fieldName}"
                        name="${fieldName}"
                        class="form-check-input"
                        value="true" />
                    <label class="form-check-label" for="${fieldName}">Yes</label>
                </div>`;
                break;
        }

        return `
            <div class="form-group">
                <label for="${fieldName}">
                    ${field.name} ${requiredMark}
                </label>
                ${inputHtml}
                <span class="text-danger field-validation-error" data-field="${fieldName}"></span>
            </div>
        `;
    }

    showLoading() {
        this.container.innerHTML = '<div class="text-center"><i class="fas fa-spinner fa-spin"></i> Loading custom fields...</div>';
    }

    showError(message) {
        this.container.innerHTML = `<div class="alert alert-danger">${message}</div>`;
    }

    clearFields() {
        this.container.innerHTML = '';
        this.fields = [];
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    new CustomFieldsManager();
});
```

### CSS Styles

**File**: `wwwroot/css/custom-fields.css`

```css
/* Custom Fields Section */
.custom-fields-section {
    margin-top: 32px;
    margin-bottom: 32px;
}

.custom-fields-section .form-section-header {
    background-color: #f8f9fa;
    border-left: 4px solid #002B76;
    padding: 12px 16px;
    margin-bottom: 24px;
    display: flex;
    align-items: center;
    gap: 12px;
    font-weight: 600;
    font-size: 16px;
}

.custom-fields-section .form-section-header i {
    color: #002B76;
}

/* Custom field group */
.custom-field-group {
    margin-bottom: 20px;
}

.custom-field-group label {
    font-weight: 500;
    margin-bottom: 8px;
    display: block;
}

.custom-field-group .text-danger {
    color: #dc3545;
    margin-left: 4px;
}

.custom-field-group .fa-info-circle {
    color: #6c757d;
    cursor: help;
    margin-left: 6px;
}

/* Multiselect styling */
select[multiple].form-control {
    height: auto;
    min-height: 120px;
}

/* Field validation error */
.field-validation-error {
    display: block;
    margin-top: 6px;
    font-size: 14px;
    color: #dc3545;
}

.field-validation-error:empty {
    display: none;
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .custom-fields-section {
        margin-top: 24px;
    }

    .custom-fields-section .form-section-header {
        font-size: 14px;
        padding: 10px 12px;
    }
}
```

---

## Backend Design

### Service Layer

**File**: `Services/CustomFieldService.cs`

```csharp
public interface ICustomFieldService
{
    Task<List<CustomFieldDto>> GetFieldsByCertificationTypeAsync(int certificationTypeId);
    Task<List<CustomFieldValueDto>> GetValuesByCertificateAsync(int certificateId);
    Task SaveCustomFieldValuesAsync(int certificateId, List<CustomFieldValueDto> values);
    Task DeleteCustomFieldValuesAsync(int certificateId);
    Task<bool> ValidateFieldValue(CustomFieldDto field, string value);
}

public class CustomFieldService : ICustomFieldService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomFieldService> _logger;

    public CustomFieldService(ApplicationDbContext context, ILogger<CustomFieldService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CustomFieldDto>> GetFieldsByCertificationTypeAsync(int certificationTypeId)
    {
        var fields = await _context.CustomFields
            .Where(cf => cf.CertificateTypeId == certificationTypeId &&
                         !cf.IsDeleted &&
                         cf.IsVisible)
            .OrderBy(cf => cf.SortOrder)
            .ThenBy(cf => cf.Name)
            .Select(cf => new CustomFieldDto
            {
                Id = cf.Id,
                Name = cf.Name,
                FieldType = cf.FieldType,
                IsRequired = cf.IsRequired,
                IsVisible = cf.IsVisible,
                IsPublic = cf.IsPublic,
                SortOrder = cf.SortOrder,
                Options = cf.Options,
                DefaultValue = cf.DefaultValue,
                ValidationRules = cf.ValidationRules,
                Section = cf.Section
            })
            .ToListAsync();

        return fields;
    }

    public async Task<List<CustomFieldValueDto>> GetValuesByCertificateAsync(int certificateId)
    {
        var values = await _context.CustomFieldValues
            .Include(cfv => cfv.Field)
            .Where(cfv => cfv.CertificateId == certificateId)
            .Select(cfv => new CustomFieldValueDto
            {
                FieldId = cfv.FieldId,
                FieldName = cfv.Field.Name,
                FieldType = cfv.Field.FieldType,
                Value = GetFieldValue(cfv)
            })
            .ToListAsync();

        return values;
    }

    public async Task SaveCustomFieldValuesAsync(int certificateId, List<CustomFieldValueDto> values)
    {
        // Delete existing values
        var existing = await _context.CustomFieldValues
            .Where(cfv => cfv.CertificateId == certificateId)
            .ToListAsync();

        _context.CustomFieldValues.RemoveRange(existing);

        // Insert new values
        foreach (var valueDto in values)
        {
            var field = await _context.CustomFields.FindAsync(valueDto.FieldId);
            if (field == null) continue;

            var fieldValue = new CustomFieldValue
            {
                CertificateId = certificateId,
                FieldId = valueDto.FieldId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Store in appropriate column based on field type
            switch (field.FieldType.ToLower())
            {
                case "text":
                case "email":
                case "phone":
                case "textarea":
                case "select":
                case "multiselect":
                    fieldValue.TextValue = valueDto.Value;
                    break;

                case "number":
                    if (decimal.TryParse(valueDto.Value, out var numValue))
                        fieldValue.NumberValue = numValue;
                    break;

                case "date":
                    if (DateTime.TryParse(valueDto.Value, out var dateValue))
                        fieldValue.DateValue = dateValue;
                    break;

                case "boolean":
                    fieldValue.BooleanValue = valueDto.Value?.ToLower() == "true";
                    break;
            }

            _context.CustomFieldValues.Add(fieldValue);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteCustomFieldValuesAsync(int certificateId)
    {
        var values = await _context.CustomFieldValues
            .Where(cfv => cfv.CertificateId == certificateId)
            .ToListAsync();

        _context.CustomFieldValues.RemoveRange(values);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ValidateFieldValue(CustomFieldDto field, string value)
    {
        if (field.IsRequired && string.IsNullOrWhiteSpace(value))
            return false;

        if (string.IsNullOrEmpty(field.ValidationRules))
            return true;

        try
        {
            var rules = JsonSerializer.Deserialize<ValidationRulesDto>(field.ValidationRules);
            return await ApplyValidationRules(field.FieldType, value, rules);
        }
        catch
        {
            return true; // Invalid JSON, skip validation
        }
    }

    private async Task<bool> ApplyValidationRules(string fieldType, string value, ValidationRulesDto rules)
    {
        // Implementation of specific validation logic
        // MaxLength, MinLength, Pattern, Range, etc.
        return true; // Simplified for documentation
    }

    private string GetFieldValue(CustomFieldValue cfv)
    {
        return cfv.Field.FieldType.ToLower() switch
        {
            "number" => cfv.NumberValue?.ToString(),
            "date" => cfv.DateValue?.ToString("yyyy-MM-dd"),
            "boolean" => cfv.BooleanValue?.ToString().ToLower(),
            _ => cfv.TextValue
        };
    }
}
```

### DTOs

**File**: `Models/CustomFieldDto.cs`

```csharp
public class CustomFieldDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; }
    public bool IsPublic { get; set; }
    public int SortOrder { get; set; }
    public string Options { get; set; }
    public string DefaultValue { get; set; }
    public string ValidationRules { get; set; }
    public string Section { get; set; }
    public string Value { get; set; } // For edit mode
    public string HelpText { get; set; }
}

public class CustomFieldValueDto
{
    public int FieldId { get; set; }
    public string FieldName { get; set; }
    public string FieldType { get; set; }
    public string Value { get; set; }
}

public class ValidationRulesDto
{
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public decimal? Step { get; set; }
    public string Pattern { get; set; }
    public bool Email { get; set; }
    public bool Phone { get; set; }
    public DateRangeDto DateRange { get; set; }
}

public class DateRangeDto
{
    public string Min { get; set; }
    public string Max { get; set; }
}
```

---

## Validation Framework

### Client-Side Validation (JavaScript)

```javascript
class CustomFieldValidator {
    constructor() {
        this.rules = new Map();
    }

    registerField(fieldName, validationRules) {
        this.rules.set(fieldName, JSON.parse(validationRules));
    }

    validate(fieldName, value) {
        const rules = this.rules.get(fieldName);
        if (!rules) return { valid: true };

        const errors = [];

        // Required
        if (rules.required && !value) {
            errors.push('This field is required');
        }

        // Min/Max Length
        if (rules.minLength && value.length < rules.minLength) {
            errors.push(`Minimum length is ${rules.minLength} characters`);
        }
        if (rules.maxLength && value.length > rules.maxLength) {
            errors.push(`Maximum length is ${rules.maxLength} characters`);
        }

        // Numeric Range
        if (rules.min !== undefined && parseFloat(value) < rules.min) {
            errors.push(`Value must be at least ${rules.min}`);
        }
        if (rules.max !== undefined && parseFloat(value) > rules.max) {
            errors.push(`Value must not exceed ${rules.max}`);
        }

        // Pattern
        if (rules.pattern && !new RegExp(rules.pattern).test(value)) {
            errors.push('Value does not match required format');
        }

        // Email
        if (rules.email && !this.isValidEmail(value)) {
            errors.push('Please enter a valid email address');
        }

        // Phone
        if (rules.phone && !this.isValidPhone(value)) {
            errors.push('Please enter a valid phone number');
        }

        return {
            valid: errors.length === 0,
            errors: errors
        };
    }

    isValidEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    isValidPhone(phone) {
        return /^[\d\s\-\+\(\)]+$/.test(phone);
    }

    showFieldError(fieldName, errors) {
        const errorSpan = document.querySelector(`[data-field="${fieldName}"]`);
        if (errorSpan) {
            errorSpan.textContent = errors.join(', ');
        }
    }

    clearFieldError(fieldName) {
        const errorSpan = document.querySelector(`[data-field="${fieldName}"]`);
        if (errorSpan) {
            errorSpan.textContent = '';
        }
    }
}
```

### Server-Side Validation (FluentValidation)

**File**: `Validators/CustomFieldValueValidator.cs`

```csharp
public class CustomFieldValueValidator : AbstractValidator<CustomFieldValueDto>
{
    private readonly ICustomFieldService _customFieldService;

    public CustomFieldValueValidator(ICustomFieldService customFieldService)
    {
        _customFieldService = customFieldService;

        RuleFor(x => x.Value)
            .MustAsync(async (dto, value, cancellation) =>
                await ValidateFieldAsync(dto, value))
            .WithMessage("Field value does not meet validation requirements");
    }

    private async Task<bool> ValidateFieldAsync(CustomFieldValueDto dto, string value)
    {
        var field = await _customFieldService.GetFieldByIdAsync(dto.FieldId);
        if (field == null) return false;

        return await _customFieldService.ValidateFieldValue(field, value);
    }
}
```

---

## Export Integration

### PDF Export (QuestPDF)

**File**: `Services/PdfExportService.cs` (Extension)

```csharp
private void RenderCustomFields(Container container, Certificate certificate)
{
    if (!certificate.CustomFieldValues.Any())
        return;

    container.Column(column =>
    {
        column.Item().PaddingTop(20).Text("Additional Information")
            .FontSize(16)
            .Bold()
            .FontColor("#002B76");

        column.Item().PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1); // Field name
                columns.RelativeColumn(2); // Value
            });

            // Group by section
            var grouped = certificate.CustomFieldValues
                .GroupBy(cfv => cfv.Field.Section ?? "Other")
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                // Section header
                table.Cell().Row(++row).ColumnSpan(2)
                    .Padding(5)
                    .Background("#f0f0f0")
                    .Text(group.Key)
                    .FontSize(12)
                    .Bold();

                // Fields in section
                foreach (var cfv in group.OrderBy(c => c.Field.SortOrder))
                {
                    table.Cell().Row(++row).Column(1)
                        .Padding(5)
                        .Text(cfv.Field.Name)
                        .FontSize(10);

                    table.Cell().Row(row).Column(2)
                        .Padding(5)
                        .Text(FormatValue(cfv))
                        .FontSize(10);
                }
            }
        });
    });
}

private string FormatValue(CustomFieldValue cfv)
{
    return cfv.Field.FieldType.ToLower() switch
    {
        "number" => cfv.NumberValue?.ToString("N2") ?? "N/A",
        "date" => cfv.DateValue?.ToString("MM/dd/yyyy") ?? "N/A",
        "boolean" => cfv.BooleanValue == true ? "Yes" : "No",
        _ => cfv.TextValue ?? "Not Provided"
    };
}
```

### CSV Export

**File**: `Services/CsvExportService.cs` (Extension)

```csharp
public async Task<byte[]> ExportCertificatesWithCustomFieldsAsync(List<int> certificateIds)
{
    var certificates = await _context.Certificates
        .Include(c => c.Company)
        .Include(c => c.Product)
        .Include(c => c.CertificationType)
        .Include(c => c.CustomFieldValues)
        .ThenInclude(cfv => cfv.Field)
        .Where(c => certificateIds.Contains(c.Id))
        .ToListAsync();

    // Get all unique custom fields across all certificates
    var customFields = certificates
        .SelectMany(c => c.CustomFieldValues.Select(cfv => cfv.Field))
        .DistinctBy(f => f.Id)
        .OrderBy(f => f.SortOrder)
        .ToList();

    var csv = new StringBuilder();

    // Header row: standard columns + custom field columns
    var headers = new List<string>
    {
        "Certificate Number",
        "Certification Type",
        "Company",
        "Product",
        "Status",
        "Effective Date",
        "Expiration Date"
    };
    headers.AddRange(customFields.Select(f => f.Name));
    csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

    // Data rows
    foreach (var cert in certificates)
    {
        var row = new List<string>
        {
            cert.Number,
            cert.CertificationType.Name,
            cert.Company.Name,
            cert.Product.Name,
            cert.Status,
            cert.EffectiveDate.ToString("yyyy-MM-dd"),
            cert.ExpirationDate.ToString("yyyy-MM-dd")
        };

        // Add custom field values
        foreach (var field in customFields)
        {
            var value = cert.CustomFieldValues
                .FirstOrDefault(cfv => cfv.FieldId == field.Id);

            row.Add(value != null ? FormatCsvValue(value) : "");
        }

        csv.AppendLine(string.Join(",", row.Select(r => $"\"{r}\"")));
    }

    return Encoding.UTF8.GetBytes(csv.ToString());
}

private string FormatCsvValue(CustomFieldValue cfv)
{
    return cfv.Field.FieldType.ToLower() switch
    {
        "number" => cfv.NumberValue?.ToString() ?? "",
        "date" => cfv.DateValue?.ToString("yyyy-MM-dd") ?? "",
        "boolean" => cfv.BooleanValue == true ? "Yes" : "No",
        "multiselect" => cfv.TextValue?.Replace(",", ";") ?? "", // Avoid CSV delimiter conflict
        _ => cfv.TextValue ?? ""
    };
}
```

---

## Performance Optimization

### Caching Strategy
```csharp
public class CachedCustomFieldService : ICustomFieldService
{
    private readonly ICustomFieldService _innerService;
    private readonly IMemoryCache _cache;
    private const int CacheExpirationMinutes = 5;

    public async Task<List<CustomFieldDto>> GetFieldsByCertificationTypeAsync(int certificationTypeId)
    {
        var cacheKey = $"CustomFields_CertType_{certificationTypeId}";

        if (_cache.TryGetValue(cacheKey, out List<CustomFieldDto> cachedFields))
        {
            return cachedFields;
        }

        var fields = await _innerService.GetFieldsByCertificationTypeAsync(certificationTypeId);

        _cache.Set(cacheKey, fields, TimeSpan.FromMinutes(CacheExpirationMinutes));

        return fields;
    }

    // Invalidate cache when fields are modified
    public async Task InvalidateCacheForCertificationType(int certificationTypeId)
    {
        _cache.Remove($"CustomFields_CertType_{certificationTypeId}");
    }
}
```

### Query Optimization
- Use `.AsNoTracking()` for read-only queries
- Project to DTOs directly in queries (avoid loading full entities)
- Batch save operations
- Use compiled queries for frequently executed operations

---

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task SaveCustomFieldValues_ShouldStoreInCorrectColumns()
{
    // Arrange
    var service = new CustomFieldService(_context, _logger);
    var values = new List<CustomFieldValueDto>
    {
        new() { FieldId = 1, FieldType = "text", Value = "Test Company" },
        new() { FieldId = 2, FieldType = "number", Value = "123.45" },
        new() { FieldId = 3, FieldType = "date", Value = "2025-01-15" }
    };

    // Act
    await service.SaveCustomFieldValuesAsync(certificateId: 1, values);

    // Assert
    var saved = await _context.CustomFieldValues
        .Where(cfv => cfv.CertificateId == 1)
        .ToListAsync();

    Assert.Equal("Test Company", saved.First(s => s.FieldId == 1).TextValue);
    Assert.Equal(123.45m, saved.First(s => s.FieldId == 2).NumberValue);
    Assert.Equal(new DateTime(2025, 1, 15), saved.First(s => s.FieldId == 3).DateValue);
}
```

### Integration Tests
```csharp
[Fact]
public async Task CreateCertificate_WithCustomFields_ShouldSaveSuccessfully()
{
    // Test full flow: form submission → controller → service → database
}
```

### E2E Tests (Playwright)
```csharp
[Fact]
public async Task CustomFields_ShouldLoadWhenCertificationTypeSelected()
{
    await Page.GotoAsync("/Certificate/Create");
    await Page.SelectOptionAsync("#CertificationTypeId", "1");

    // Wait for custom fields to load
    await Page.WaitForSelectorAsync("#custom-fields-container .form-group");

    var fieldCount = await Page.Locator("#custom-fields-container .form-group").CountAsync();
    Assert.True(fieldCount > 0);
}
```

---

## Security Considerations

### XSS Prevention
- Sanitize all custom field values before display
- Use `@Html.Raw()` only for trusted content
- Encode user input in Razor views (automatic)
- Validate HTML in textarea fields (strip tags)

### SQL Injection Prevention
- Use parameterized queries (EF Core does this)
- Validate field IDs before database operations
- Prevent dynamic SQL generation

### Access Control
- Enforce role-based visibility (IsPublic flag)
- Check authentication before loading/saving custom fields
- Audit log all custom field changes

### Input Validation
- Maximum length limits to prevent DoS
- Whitelist allowed field types
- Validate JSON parsing (ValidationRules, Options)
- Rate limit API endpoints

---

**Document Status**: READY FOR IMPLEMENTATION
**Next Steps**: Begin Phase 1 Implementation
