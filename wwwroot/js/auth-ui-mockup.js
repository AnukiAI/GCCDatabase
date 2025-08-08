function showScreen(event, screenId) {
    event.preventDefault();
    document.querySelectorAll('.screen').forEach(screen => screen.classList.remove('active'));
    document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
    document.getElementById(screenId).classList.add('active');
    event.target.classList.add('active');
}

function generateTempPassword() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@@raw(Html.Raw("#"))$%';
    let password = '';

    // Use crypto.getRandomValues for better security if available
    if (window.crypto && window.crypto.getRandomValues) {
        const array = new Uint32Array(12);
        window.crypto.getRandomValues(array);
        for (let i = 0; i < 12; i++) {
            password += chars.charAt(array@@raw(Html.Raw("[")) i@@raw(Html.Raw("]")) % chars.length);
        }
    } else {
        // Fallback to Math.random
        for (let i = 0; i < 12; i++) {
            password += chars.charAt(Math.floor(Math.random() * chars.length));
        }
    }

    const pwdField = document.getElementById('temp-password');
    if (pwdField) pwdField.value = password;
}

function checkPasswordStrength(password) {
    let strength = 0;
    if (password.length >= 8) strength++;
    if (/@@raw(Html.Raw("["))A-Z@@raw(Html.Raw("]"))/.test(password)) strength++;
    if (/@@raw(Html.Raw("["))a-z@@raw(Html.Raw("]"))/.test(password)) strength++;
    if (/@@raw(Html.Raw("["))0-9@@raw(Html.Raw("]"))/.test(password)) strength++;
    if (/@@raw(Html.Raw("["))^A-Za-z0-9@@raw(Html.Raw("]"))/.test(password)) strength++;
    return Math.min(strength, 4); // Cap at 4 for display purposes
}

document.addEventListener('DOMContentLoaded', function () {
    const userRoleField = document.getElementById('user-role');
    if (userRoleField) {
        userRoleField.addEventListener('change', function () {
            const companyAccess = document.getElementById('company-access');
            if (companyAccess) {
                if (this.value === 'external') {
                    companyAccess.style.display = 'block';
                } else {
                    companyAccess.style.display = 'none';
                }
            }
        });
    }

    const tempPasswordField = document.getElementById('temp-password');
    if (tempPasswordField) {
        const generateBtn = document.createElement('button');
        generateBtn.type = 'button';
        generateBtn.className = 'btn btn-secondary btn-sm';
        generateBtn.textContent = 'Generate';
        generateBtn.onclick = generateTempPassword;
        generateBtn.style.marginLeft = '10px';

        const wrapper = document.createElement('div');
        wrapper.className = 'd-flex align-items-center';
        tempPasswordField.parentNode.insertBefore(wrapper, tempPasswordField);
        wrapper.appendChild(tempPasswordField);
        wrapper.appendChild(generateBtn);
    }

    // Enhanced form handling for ASP.NET Core
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            // Show loading state
            const submitBtn = this.querySelector('button@@raw(Html.Raw("["))type="submit"@@raw(Html.Raw("]"))');
            const originalText = submitBtn ? submitBtn.textContent : '';
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.textContent = 'Processing...';
            }

            // Simulate async operation
            setTimeout(() => {
                const message = document.createElement('div');
                message.className = 'alert alert-success alert-dismissible fade show';
                message.innerHTML = '<strong>Success!</strong> Action completed successfully.<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>';
                this.parentNode.insertBefore(message, this.nextSibling);

                // Reset button state
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.textContent = originalText;
                }

                // Auto-remove message after 5 seconds
                setTimeout(() => {
                    if (message.parentNode) {
                        message.remove();
                    }
                }, 5000);
            }, 1000);
        });
    });

    const otpInput = document.getElementById('otp-code');
    if (otpInput) {
        otpInput.addEventListener('input', function () {
            this.value = this.value.replace(/@@raw(Html.Raw("["))^0-9@@raw(Html.Raw("]"))/g, '');
            if (this.value.length > 6) {
                this.value = this.value.slice(0, 6);
            }
        });
    }

    const newPasswordField = document.getElementById('new-password');
    if (newPasswordField) {
        const strengthIndicator = document.createElement('div');
        strengthIndicator.className = 'password-strength mt-1';
        strengthIndicator.style.fontSize = '12px';
        newPasswordField.parentNode.appendChild(strengthIndicator);

        newPasswordField.addEventListener('input', function () {
            const strength = checkPasswordStrength(this.value);
            const colors = @@raw(Html.Raw("[")) '@@raw(Html.Raw("#"))dc3545', '@@raw(Html.Raw("#"))fd7e14', '@@raw(Html.Raw("#"))ffc107', '@@raw(Html.Raw("#"))198754', '@@raw(Html.Raw("#"))0d6efd'@@raw(Html.Raw("]"));
            const labels = @@raw(Html.Raw("[")) 'Very Weak', 'Weak', 'Fair', 'Good', 'Strong'@@raw(Html.Raw("]"));
            const widths = @@raw(Html.Raw("[")) '20%', '40%', '60%', '80%', '100%'@@raw(Html.Raw("]"));

            if (this.value.length > 0) {
                strengthIndicator.innerHTML = '<div class="mb-1">Password Strength: <span style="color: ' + colors@@raw(Html.Raw("[")) strength @@raw(Html.Raw("]")) + '">' + labels@@raw(Html.Raw("[")) strength @@raw(Html.Raw("]")) + '</span></div><div class="progress" style="height: 4px;"><div class="progress-bar" role="progressbar" style="width: ' + widths@@raw(Html.Raw("[")) strength @@raw(Html.Raw("]")) + '; background-color: ' + colors@@raw(Html.Raw("[")) strength @@raw(Html.Raw("]")) + '" aria-valuenow="' + (strength * 25) + '" aria-valuemin="0" aria-valuemax="100"></div></div>';
            } else {
                strengthIndicator.innerHTML = '';
            }
        });
    }

    // Add real-time validation feedback
    document.querySelectorAll('input@@raw(Html.Raw("["))required@@raw(Html.Raw("]"))').forEach(input => {
        input.addEventListener('blur', function () {
            if (this.value.trim() === '') {
                this.classList.add('is-invalid');
            } else {
                this.classList.remove('is-invalid');
                this.classList.add('is-valid');
            }
        });
    });

    // Email validation
    document.querySelectorAll('input@@raw(Html.Raw("["))type="email"@@raw(Html.Raw("]"))').forEach(input => {
        input.addEventListener('blur', function () {
            const emailRegex = /^@@raw(Html.Raw("["))^\\s@@raw(Html.Raw("@"))@@raw(Html.Raw("]"))+@@raw(Html.Raw("@"))@@raw(Html.Raw("["))^\\s@@raw(Html.Raw("@"))@@raw(Html.Raw("]"))+\\.@@raw(Html.Raw("["))^\\s@@raw(Html.Raw("@"))@@raw(Html.Raw("]"))+$/;
            if (this.value && !emailRegex.test(this.value)) {
                this.classList.add('is-invalid');
                this.classList.remove('is-valid');
            } else if (this.value) {
                this.classList.remove('is-invalid');
                this.classList.add('is-valid');
            }
        });
    });
});