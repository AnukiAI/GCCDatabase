/**
 * Certificate Custom Fields Manager
 * Handles dynamic rendering and management of custom fields in certificate forms
 * Version: 1.0
 * Date: 2025-11-13
 */

class CertificateCustomFields {
    constructor() {
        this.fields = [];
        this.values = {};
        this.certificateTypeId = null;
        this.containerId = 'custom-fields-container';
    }

    /**
     * Loads custom fields for a specific certificate type
     * @param {number} certificateTypeId - The certificate type ID
     */
    async loadFieldsForType(certificateTypeId) {
        try {
            this.certificateTypeId = certificateTypeId;

            const response = await fetch(`/Certificate/GetCustomFields?certificateTypeId=${certificateTypeId}`);
            const result = await response.json();

            if (result.success) {
                this.fields = result.data || [];
                this.renderFields();
            } else {
                console.error('Failed to load fields:', result.message);
                this.showMessage('Failed to load custom fields', 'error');
            }
        } catch (error) {
            console.error('Error loading fields:', error);
            this.showMessage('Error loading custom fields', 'error');
        }
    }

    /**
     * Loads existing field values for a certificate
     * @param {number} certificateId - The certificate ID
     */
    async loadFieldValues(certificateId) {
        try {
            const response = await fetch(`/Certificate/GetCustomFieldValues?certificateId=${certificateId}`);
            const result = await response.json();

            if (result.success) {
                this.values = result.data || {};
                this.populateValues();
                return this.values;
            } else {
                console.error('Failed to load values:', result.message);
                return {};
            }
        } catch (error) {
            console.error('Error loading values:', error);
            return {};
        }
    }

    /**
     * Renders all custom fields grouped by section
     */
    renderFields() {
        const container = document.getElementById(this.containerId);
        if (!container) {
            // Silently return on pages without custom fields container
            // This is expected behavior on non-certificate pages
            return;
        }

        if (this.fields.length === 0) {
            container.innerHTML = '<p class="text-muted">No custom fields defined for this certificate type.</p>';
            return;
        }

        // Group fields by section
        const sections = {};
        this.fields.forEach(field => {
            const section = field.section || 'Additional Information';
            if (!sections[section]) {
                sections[section] = [];
            }
            sections[section].push(field);
        });

        // Render each section
        let html = '';
        Object.keys(sections).forEach(sectionName => {
            html += `
                <div class="custom-field-section">
                    <h4 class="section-header">${sectionName}</h4>
                    <div class="custom-fields-grid">
                        ${sections[sectionName].map(field => this.renderField(field)).join('')}
                    </div>
                </div>
            `;
        });

        container.innerHTML = html;

        // Apply visibility rules
        this.applyVisibilityRules();
    }

    /**
     * Renders a single field based on its type
     * @param {object} field - The field definition
     */
    renderField(field) {
        const value = this.values[field.id] || field.defaultValue || '';
        const required = field.isRequired ? 'required' : '';
        const requiredMark = field.isRequired ? '<span class="text-danger">*</span>' : '';

        let control = '';

        switch (field.fieldType.toLowerCase()) {
            case 'text':
                control = `<input type="text" class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    value="${this.escapeHtml(value)}" ${required} data-field-id="${field.id}">`;
                break;

            case 'email':
                control = `<input type="email" class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    value="${this.escapeHtml(value)}" ${required} data-field-id="${field.id}">`;
                break;

            case 'phone':
                control = `<input type="tel" class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    value="${this.escapeHtml(value)}" ${required} data-field-id="${field.id}">`;
                break;

            case 'number':
                control = `<input type="number" class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    value="${value}" ${required} data-field-id="${field.id}">`;
                break;

            case 'date':
                control = `<input type="date" class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    value="${value}" ${required} data-field-id="${field.id}">`;
                break;

            case 'checkbox':
            case 'boolean':
                const checked = value === 'true' || value === true ? 'checked' : '';
                control = `<div class="form-check">
                    <input type="checkbox" class="form-check-input custom-field-input"
                        id="field_${field.id}" name="field_${field.id}"
                        ${checked} ${required} data-field-id="${field.id}">
                </div>`;
                break;

            case 'select':
            case 'dropdown':
                const options = this.parseOptions(field.options);
                control = `<select class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    ${required} data-field-id="${field.id}">
                    <option value="">-- Select --</option>
                    ${options.map(opt =>
                        `<option value="${opt}" ${opt === value ? 'selected' : ''}>${opt}</option>`
                    ).join('')}
                </select>`;
                break;

            case 'multi-select':
            case 'multiselect':
                const multiOptions = this.parseOptions(field.options);
                const selectedValues = value ? value.split(',') : [];
                control = `<select multiple class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    ${required} data-field-id="${field.id}" size="4">
                    ${multiOptions.map(opt =>
                        `<option value="${opt}" ${selectedValues.includes(opt) ? 'selected' : ''}>${opt}</option>`
                    ).join('')}
                </select>`;
                break;

            case 'textarea':
                control = `<textarea class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    ${required} data-field-id="${field.id}" rows="3">${this.escapeHtml(value)}</textarea>`;
                break;

            case 'file':
                control = `<input type="file" class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    ${required} data-field-id="${field.id}">`;
                break;

            default:
                control = `<input type="text" class="form-control custom-field-input"
                    id="field_${field.id}" name="field_${field.id}"
                    value="${this.escapeHtml(value)}" ${required} data-field-id="${field.id}">`;
        }

        return `
            <div class="form-group custom-field-group" data-field-id="${field.id}">
                <label for="field_${field.id}">
                    ${field.name} ${requiredMark}
                    ${field.notes ? `<small class="text-muted d-block">${field.notes}</small>` : ''}
                </label>
                ${control}
            </div>
        `;
    }

    /**
     * Populates field values into rendered controls
     */
    populateValues() {
        Object.keys(this.values).forEach(fieldId => {
            const input = document.getElementById(`field_${fieldId}`);
            if (input) {
                if (input.type === 'checkbox') {
                    input.checked = this.values[fieldId] === 'true' || this.values[fieldId] === true;
                } else if (input.multiple) {
                    const values = this.values[fieldId].split(',');
                    Array.from(input.options).forEach(option => {
                        option.selected = values.includes(option.value);
                    });
                } else {
                    input.value = this.values[fieldId];
                }
            }
        });
    }

    /**
     * Gets all field values from the form
     * @returns {object} Dictionary of field ID to value
     */
    getFieldValues() {
        const values = {};

        this.fields.forEach(field => {
            const input = document.getElementById(`field_${field.id}`);
            if (input) {
                if (input.type === 'checkbox') {
                    values[field.id] = input.checked ? 'true' : 'false';
                } else if (input.multiple) {
                    const selected = Array.from(input.selectedOptions).map(opt => opt.value);
                    values[field.id] = selected.join(',');
                } else {
                    values[field.id] = input.value;
                }
            }
        });

        return values;
    }

    /**
     * Validates all required fields and custom validation rules
     * @returns {object} {isValid: boolean, errors: array}
     */
    validateFields() {
        const errors = [];

        this.fields.forEach(field => {
            const input = document.getElementById(`field_${field.id}`);
            if (!input) return;

            const value = input.type === 'checkbox' ? input.checked : input.value;
            let fieldErrors = [];

            // 1. Required field validation
            if (field.isRequired && (!value || value === '')) {
                fieldErrors.push(`${field.name} is required`);
            }

            // 2. Type-specific validation (only if value is present)
            if (value && value !== '') {
                switch (field.fieldType.toLowerCase()) {
                    case 'number':
                        if (isNaN(value)) {
                            fieldErrors.push(`${field.name} must be a valid number`);
                        }
                        break;
                    case 'date':
                        const date = new Date(value);
                        if (isNaN(date.getTime())) {
                            fieldErrors.push(`${field.name} must be a valid date`);
                        }
                        break;
                }
            }

            // 3. Custom validation rules (JSON format)
            if (value && value !== '' && field.validationRules) {
                try {
                    const rules = typeof field.validationRules === 'string'
                        ? JSON.parse(field.validationRules)
                        : field.validationRules;

                    // Min length validation
                    if (rules.minLength && value.length < rules.minLength) {
                        fieldErrors.push(`${field.name} must be at least ${rules.minLength} characters`);
                    }

                    // Max length validation
                    if (rules.maxLength && value.length > rules.maxLength) {
                        fieldErrors.push(`${field.name} must be at most ${rules.maxLength} characters`);
                    }

                    // Min value validation (for numbers)
                    if (rules.minValue && parseFloat(value) < rules.minValue) {
                        fieldErrors.push(`${field.name} must be at least ${rules.minValue}`);
                    }

                    // Max value validation (for numbers)
                    if (rules.maxValue && parseFloat(value) > rules.maxValue) {
                        fieldErrors.push(`${field.name} must be at most ${rules.maxValue}`);
                    }

                    // Regex pattern validation
                    if (rules.pattern) {
                        const regex = new RegExp(rules.pattern);
                        if (!regex.test(value)) {
                            const errorMsg = rules.patternMessage || `${field.name} format is invalid`;
                            fieldErrors.push(errorMsg);
                        }
                    }
                } catch (ex) {
                    console.warn(`Invalid validation rules for field ${field.name}:`, ex);
                }
            }

            // Apply validation state to input
            if (fieldErrors.length > 0) {
                input.classList.add('is-invalid');
                errors.push(...fieldErrors);
            } else {
                input.classList.remove('is-invalid');
            }
        });

        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    /**
     * Saves field values for a certificate
     * @param {number} certificateId - The certificate ID
     */
    async saveFieldValues(certificateId) {
        try {
            const validation = this.validateFields();
            if (!validation.isValid) {
                this.showMessage('Please fix validation errors: ' + validation.errors.join(', '), 'error');
                return false;
            }

            const values = this.getFieldValues();
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const response = await fetch('/Certificate/SaveCustomFieldValues', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    certificateId: certificateId,
                    fieldValues: values
                })
            });

            const result = await response.json();

            if (result.success) {
                this.showMessage('Custom fields saved successfully', 'success');
                return true;
            } else {
                this.showMessage(result.message || 'Failed to save custom fields', 'error');
                return false;
            }
        } catch (error) {
            console.error('Error saving field values:', error);
            this.showMessage('Error saving custom fields', 'error');
            return false;
        }
    }

    /**
     * Applies visibility rules based on field dependencies
     * Reads visibility rules from ValidationRules JSON in format:
     * { "showIf": { "fieldId": 123, "value": "someValue" } }
     */
    applyVisibilityRules() {
        this.fields.forEach(field => {
            const fieldGroup = document.querySelector(`.custom-field-group[data-field-id="${field.id}"]`);
            if (!fieldGroup) return;

            // Default: show field
            let shouldShow = true;

            // Check if field has visibility rules
            if (field.validationRules) {
                try {
                    const rules = typeof field.validationRules === 'string'
                        ? JSON.parse(field.validationRules)
                        : field.validationRules;

                    // Check "showIf" condition
                    if (rules.showIf) {
                        const dependentFieldId = rules.showIf.fieldId;
                        const dependentInput = document.getElementById(`field_${dependentFieldId}`);

                        if (dependentInput) {
                            const dependentValue = dependentInput.type === 'checkbox'
                                ? (dependentInput.checked ? 'true' : 'false')
                                : dependentInput.value;

                            // Show only if dependent field matches expected value
                            shouldShow = (dependentValue === rules.showIf.value);

                            // Add change listener to dependent field
                            if (!dependentInput.dataset.visibilityListenerAdded) {
                                dependentInput.addEventListener('change', () => this.applyVisibilityRules());
                                dependentInput.dataset.visibilityListenerAdded = 'true';
                            }
                        }
                    }

                    // Check "hideIf" condition
                    if (rules.hideIf) {
                        const dependentFieldId = rules.hideIf.fieldId;
                        const dependentInput = document.getElementById(`field_${dependentFieldId}`);

                        if (dependentInput) {
                            const dependentValue = dependentInput.type === 'checkbox'
                                ? (dependentInput.checked ? 'true' : 'false')
                                : dependentInput.value;

                            // Hide if dependent field matches expected value
                            if (dependentValue === rules.hideIf.value) {
                                shouldShow = false;
                            }

                            // Add change listener to dependent field
                            if (!dependentInput.dataset.visibilityListenerAdded) {
                                dependentInput.addEventListener('change', () => this.applyVisibilityRules());
                                dependentInput.dataset.visibilityListenerAdded = 'true';
                            }
                        }
                    }
                } catch (ex) {
                    console.warn(`Invalid visibility rules for field ${field.name}:`, ex);
                }
            }

            // Apply visibility
            fieldGroup.style.display = shouldShow ? '' : 'none';

            // If hidden, clear validation errors and optionally clear value
            if (!shouldShow) {
                const input = document.getElementById(`field_${field.id}`);
                if (input) {
                    input.classList.remove('is-invalid');
                    // Optionally clear value when hidden
                    // input.value = '';
                }
            }
        });
    }

    /**
     * Escapes HTML to prevent XSS
     * @param {string} str - The string to escape
     * @returns {string} The escaped string
     */
    escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    /**
     * Parses options string (comma-separated or JSON array)
     * @param {string} optionsStr - The options string
     * @returns {array} Array of option values
     */
    parseOptions(optionsStr) {
        if (!optionsStr) return [];

        // Try JSON parse first
        try {
            const parsed = JSON.parse(optionsStr);
            if (Array.isArray(parsed)) return parsed;
        } catch (e) {
            // Not JSON, try comma-separated
        }

        // Try comma-separated
        return optionsStr.split(',').map(o => o.trim()).filter(o => o);
    }

    /**
     * Shows a message to the user
     * @param {string} message - The message text
     * @param {string} type - Message type (success, error, info, warning)
     */
    showMessage(message, type) {
        // Use AdminCore if available, otherwise console
        if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
            AdminCore.showNotification(message, type);
        } else {
            console.log(`[${type}] ${message}`);
            alert(message);
        }
    }
}

// Initialize global instance
window.certificateCustomFields = new CertificateCustomFields();
