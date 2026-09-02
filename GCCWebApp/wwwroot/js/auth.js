// Authentication helper functions
document.addEventListener('DOMContentLoaded', function() {
    // Handle logout form submission
    const logoutForm = document.getElementById('logoutForm');
    if (logoutForm) {
        logoutForm.addEventListener('submit', function(e) {
            // Optionally show a loading state
            const submitButton = this.querySelector('button[type="submit"]');
            if (submitButton) {
                submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Signing out...';
                submitButton.disabled = true;
            }
        });
    }

    // Handle login form submission
    const loginForm = document.querySelector('form[action*="Login"]');
    if (loginForm) {
        loginForm.addEventListener('submit', function(e) {
            const submitButton = this.querySelector('button[type="submit"]');
            if (submitButton) {
                const originalText = submitButton.innerHTML;
                submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Signing in...';
                submitButton.disabled = true;
                
                // Re-enable after a timeout in case of error
                setTimeout(() => {
                    submitButton.innerHTML = originalText;
                    submitButton.disabled = false;
                }, 10000);
            }
        });
    }

    // Auto-fill test credentials in development
    if (window.location.hostname === 'localhost') {
        const emailInput = document.getElementById('Email');
        const passwordInput = document.getElementById('Password');
        const testCredentials = document.querySelector('.test-credentials');
        
        if (testCredentials && emailInput && passwordInput) {
            testCredentials.addEventListener('click', function(e) {
                if (e.target.tagName === 'LI' || e.target.tagName === 'STRONG') {
                    const li = e.target.closest('li');
                    if (li) {
                        const text = li.textContent;
                        const match = text.match(/(\S+@\S+\.\S+)\s*\/\s*(\S+)/);
                        if (match) {
                            emailInput.value = match[1];
                            passwordInput.value = match[2];
                            emailInput.focus();
                        }
                    }
                }
            });
            
            // Add click hint
            const items = testCredentials.querySelectorAll('li');
            items.forEach(item => {
                item.style.cursor = 'pointer';
                item.title = 'Click to auto-fill';
            });
        }
    }

    // ===== Idle session timeout =====
    //
    // There are two 30-minute clocks and they used to disagree:
    //   * the SERVER clock - the auth cookie (Program.cs: ExpireTimeSpan 30 min,
    //     SlidingExpiration true), which counts from the last HTTP request;
    //   * the BROWSER clock below, which counts from the last mousemove/click/scroll.
    // Nothing connected them, so a user who read a page for 20 minutes without navigating was
    // signed out with no warning. Worse, ASP.NET Core only reissues a sliding cookie once MORE
    // than half the window has elapsed, so a request at minute 14 did not renew anything and the
    // real idle timeout landed anywhere between 15 and 30 minutes instead of a flat 30.
    //
    // Fix: real user activity now produces a real HTTP request (POST /Account/KeepAlive), at most
    // once every KEEPALIVE_INTERVAL and ONLY when there has been activity since the last ping. An
    // idle session therefore still expires on schedule - the ping follows the user, it does not
    // keep dead sessions alive.
    //
    // The timers are also timestamp-based rather than a chain of setTimeout calls. Chrome throttles
    // timers in background tabs and freezes them entirely after a few minutes, which is why the
    // warning sometimes never appeared; comparing Date.now() against the last activity means a
    // backgrounded tab reaches the right conclusion the moment it comes back, however late the
    // interval fires.
    const SESSION_TIMEOUT = 30 * 60 * 1000;   // must match Program.cs ExpireTimeSpan
    const WARNING_TIME = 5 * 60 * 1000;       // warn this long before the timeout
    const KEEPALIVE_INTERVAL = 5 * 60 * 1000; // ping no more often than this
    const CHECK_INTERVAL = 30 * 1000;         // re-evaluate the clock this often

    let lastActivityAt = Date.now();
    // One ping is sent 5 minutes after load even with no interaction at all, then the cadence
    // becomes activity-driven. That first ping is not optional: the page load itself may NOT have
    // refreshed the cookie (sliding expiration ignores requests before the half-life), so a page
    // opened and then read without a single click could otherwise have the server expire while the
    // browser was still counting down to its own 30-minute deadline - no warning, straight to the
    // login page. It stays a SINGLE ping: once it has been sent, the lastActivityAt test below is
    // false without further input, so an abandoned tab still times out on schedule.
    let lastKeepAliveAt = Date.now();
    let hasPinged = false;
    let warningShown = false;
    let sessionEnding = false;

    function notify(message, options) {
        // Guard the global: auth.js is loaded before admin-core.js, and a page where AdminCore
        // failed to load must still time out rather than throw and leave the session running.
        if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
            AdminCore.showNotification(message, 'warning', options);
        }
    }

    function markActivity() {
        lastActivityAt = Date.now();
        warningShown = false;
    }

    function sendKeepAlive() {
        // Stamp before the request, not after: a failed ping must not retry on every tick.
        lastKeepAliveAt = Date.now();
        hasPinged = true;
        fetch('/Account/KeepAlive', {
            method: 'POST',
            // Without X-Requested-With an expired session returns the login PAGE at HTTP 200
            // instead of 401 (see OnRedirectToLogin in Program.cs), which would look like success.
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            credentials: 'same-origin',
            cache: 'no-store'
        }).then(function (response) {
            if (response.status === 401 || response.status === 403) {
                // The server has already dropped the session - stop guessing and hand over.
                sessionEnding = true;
                window.location.href = '/Account/SessionExpired';
            }
        }).catch(function () {
            // Transient network failure; the next tick will try again.
        });
    }

    function endSession() {
        sessionEnding = true;
        if (typeof UnsavedChanges !== 'undefined' && UnsavedChanges.initialized && UnsavedChanges.hasChanges) {
            UnsavedChanges.saveToLocalStorage();
            notify('Your session has expired. Form data has been saved and can be restored when you log back in.', { duration: 0 });
        } else {
            notify('Your session has expired. You will be logged out.', { duration: 0 });
        }
        setTimeout(function () {
            const form = document.getElementById('logoutForm');
            if (form) {
                form.submit();
            } else {
                window.location.href = '/Account/SessionExpired';
            }
        }, 3000); // Give the user time to read the message
    }

    function checkSession() {
        if (sessionEnding) return;

        const idle = Date.now() - lastActivityAt;

        if (idle >= SESSION_TIMEOUT) {
            endSession();
            return;
        }

        if (idle >= SESSION_TIMEOUT - WARNING_TIME) {
            if (!warningShown) {
                warningShown = true;
                notify(
                    'Your session will expire in 5 minutes due to inactivity. Click anywhere to stay logged in.',
                    { duration: 10000 }
                );
            }
            return; // Inside the warning window, let the session run down.
        }

        // Active user: keep the server cookie in step with the browser clock. The activity check
        // is what stops an abandoned tab from renewing the session forever.
        if ((!hasPinged || lastActivityAt > lastKeepAliveAt) && Date.now() - lastKeepAliveAt >= KEEPALIVE_INTERVAL) {
            sendKeepAlive();
        }
    }

    // Skip the idle auto-logout entirely when "Remember Me" is active - the server-side
    // auth cookie is good for 14 days; forcibly logging the user out at 30 min defeats it.
    const rememberMeMeta = document.querySelector('meta[name="x-remember-me"]');
    const rememberMeActive = rememberMeMeta && rememberMeMeta.getAttribute('content') === 'true';
    if (document.querySelector('#logoutForm') && !rememberMeActive) {
        ['click', 'mousemove', 'keypress', 'scroll', 'touchstart'].forEach(function (event) {
            document.addEventListener(event, markActivity, { passive: true });
        });
        setInterval(checkSession, CHECK_INTERVAL);
        // A backgrounded tab's interval is throttled or frozen, so re-check the instant it is
        // visible again rather than waiting for a timer that may be minutes behind.
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) checkSession();
        });
    }

    // Handle authentication errors (only if server didn't already render one)
    const urlParams = new URLSearchParams(window.location.search);
    const error = urlParams.get('error');
    if (error && !document.querySelector('.alert-error')) {
        const errorMessages = {
            'authentication_failed': 'Authentication failed. Please check your credentials.',
            'access_denied': 'Access denied. Please contact your administrator.',
            'session_expired': 'Your session has expired. Please sign in again.',
            'invalid_token': 'Invalid authentication token. Please try again.'
        };

        const message = errorMessages[error] || 'An authentication error occurred.';
        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-error';
        alertDiv.innerHTML = `<i class="fas fa-exclamation-circle"></i> ${message}`;

        const container = document.querySelector('.login-form-section') || document.querySelector('main');
        if (container) {
            container.insertBefore(alertDiv, container.firstChild);
        }
    }

    // QC #71: Global fetch interceptor — catch 401 responses from AJAX calls when session expires
    // and redirect to login instead of silently failing or showing broken UI
    const originalFetch = window.fetch;
    window.fetch = async function(...args) {
        const response = await originalFetch.apply(this, args);
        if (response.status === 401 && document.querySelector('#logoutForm')) {
            // Session has expired server-side — redirect to login
            if (typeof UnsavedChanges !== 'undefined' && UnsavedChanges.initialized && UnsavedChanges.hasChanges) {
                UnsavedChanges.saveToLocalStorage();
            }
            window.location.href = '/Account/Login?error=session_expired';
            // Return the response so in-flight code doesn't throw
            return response;
        }
        return response;
    };

    // Smooth redirect after login
    const returnUrl = urlParams.get('returnUrl');
    if (returnUrl && window.location.pathname.includes('/Account/Login')) {
        const returnInfo = document.createElement('div');
        returnInfo.className = 'return-info';
        returnInfo.style.cssText = 'background: #f0f9ff; border: 1px solid #bae6fd; padding: 10px; border-radius: 5px; margin-bottom: 15px; font-size: 14px; color: #0369a1;';
        returnInfo.innerHTML = `<i class="fas fa-info-circle"></i> You will be redirected to your requested page after signing in.`;
        
        const loginHeader = document.querySelector('.login-header');
        if (loginHeader && loginHeader.nextSibling) {
            loginHeader.parentNode.insertBefore(returnInfo, loginHeader.nextSibling);
        }
    }
});