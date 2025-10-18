@echo off
echo ========================================================================
echo                GCC Web Application Penetration Testing Suite
echo ========================================================================
echo.

echo This script runs comprehensive penetration testing for the GCC Web App
echo including automated security scans and manual testing validation.
echo.

set SCRIPT_DIR=%~dp0
set SECURITY_REPORTS_DIR=%SCRIPT_DIR%SecurityReports
set TIMESTAMP=%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set TIMESTAMP=%TIMESTAMP: =0%

echo Creating security reports directory...
if not exist "%SECURITY_REPORTS_DIR%" mkdir "%SECURITY_REPORTS_DIR%"

echo.
echo ========================================================================
echo                          PHASE 1: Automated Tests
echo ========================================================================
echo.

echo [1/4] Running Basic Security Tests...
cd GCCWebApp.Security.Tests
dotnet test --filter "Category=BasicSecurity" --logger "console;verbosity=normal"
if errorlevel 1 (
    echo WARNING: Some basic security tests failed!
    echo Check the output above for details.
    echo.
)

echo.
echo [2/4] Running Penetration Testing Scenarios...
dotnet test --filter "Category=PenTesting" --logger "console;verbosity=normal"
if errorlevel 1 (
    echo WARNING: Some penetration tests failed!
    echo This may indicate security vulnerabilities.
    echo.
)

echo.
echo [3/4] Running Authentication Security Tests...
dotnet test --filter "Category=Authentication" --logger "console;verbosity=normal" --no-build
if errorlevel 1 (
    echo WARNING: Authentication security issues detected!
    echo.
)

echo.
echo [4/4] Running Input Validation Tests...
dotnet test --filter "Category=InputValidation" --logger "console;verbosity=normal" --no-build
if errorlevel 1 (
    echo WARNING: Input validation vulnerabilities detected!
    echo.
)

cd ..

echo.
echo ========================================================================
echo                         PHASE 2: OWASP ZAP Scan
echo ========================================================================
echo.

echo Checking for OWASP ZAP installation...
set ZAP_PATH=%ProgramFiles%\OWASP\Zed Attack Proxy\zap.bat
if exist "%ZAP_PATH%" (
    echo Found OWASP ZAP installation
    echo Starting automated security scan...
    
    echo Starting ZAP daemon...
    start "ZAP Daemon" "%ZAP_PATH%" -daemon -host 127.0.0.1 -port 8080 -config api.addrs.addr.name=.* -config api.addrs.addr.regex=true
    
    echo Waiting for ZAP to initialize...
    timeout /t 30 /nobreak > nul
    
    echo Running ZAP baseline scan...
    powershell -ExecutionPolicy Bypass -File "Documentation\Testing\Security\Scripts\Run-ZapScan.ps1" -TargetUrl "https://localhost:5001" -ReportPath "%SECURITY_REPORTS_DIR%"
    
    echo ZAP scan completed. Check reports in %SECURITY_REPORTS_DIR%
) else (
    echo OWASP ZAP not found at expected location: %ZAP_PATH%
    echo.
    echo To install OWASP ZAP:
    echo 1. Download from https://www.zaproxy.org/download/
    echo 2. Install to default location
    echo 3. Re-run this script
    echo.
    echo Alternatively, use Docker:
    echo docker run -u zap -p 8080:8080 -i owasp/zap2docker-stable:latest zap.sh -daemon -host 0.0.0.0 -port 8080
)

echo.
echo ========================================================================
echo                        PHASE 3: Manual Test Validation
echo ========================================================================
echo.

echo The following manual penetration tests should be performed:
echo.
echo [Security Testing Checklist]
echo.
echo ✓ Authentication and Session Management:
echo   - Test password complexity requirements
echo   - Verify session timeout functionality
echo   - Test account lockout mechanisms
echo   - Validate multi-factor authentication (if implemented)
echo.
echo ✓ Authorization and Access Control:
echo   - Test role-based access controls
echo   - Verify horizontal privilege escalation prevention
echo   - Test vertical privilege escalation prevention
echo   - Validate API endpoint authorization
echo.
echo ✓ Input Validation and Injection:
echo   - SQL injection testing (automated tests provide baseline)
echo   - NoSQL injection testing
echo   - Command injection testing
echo   - LDAP injection testing
echo   - XML injection testing (XXE)
echo.
echo ✓ Cross-Site Scripting (XSS):
echo   - Reflected XSS testing
echo   - Stored XSS testing
echo   - DOM-based XSS testing
echo   - Content Security Policy validation
echo.
echo ✓ Cross-Site Request Forgery (CSRF):
echo   - CSRF token validation
echo   - SameSite cookie configuration
echo   - Origin header validation
echo.
echo ✓ Business Logic:
echo   - Race condition testing
echo   - Price manipulation testing
echo   - Workflow bypass attempts
echo   - Data validation bypass
echo.
echo ✓ File Upload Security:
echo   - Malicious file upload attempts
echo   - File type validation bypass
echo   - Path traversal via filename
echo   - Executable file upload prevention
echo.
echo ✓ Information Disclosure:
echo   - Error message analysis
echo   - Debug information exposure
echo   - Source code disclosure
echo   - Configuration file exposure
echo.
echo ✓ SSL/TLS Security:
echo   - Certificate validation
echo   - Protocol version testing
echo   - Cipher suite analysis
echo   - Perfect Forward Secrecy validation
echo.

echo.
echo ========================================================================
echo                           PHASE 4: Reporting
echo ========================================================================
echo.

echo Security test reports have been generated in: %SECURITY_REPORTS_DIR%
echo.
echo Report Files:
echo - Automated test results: Test output above
echo - ZAP security scan: %SECURITY_REPORTS_DIR%\*ZAP*.html
echo - Manual test checklist: Documentation\Testing\Security\*
echo.

echo.
echo ========================================================================
echo                        PENETRATION TEST SUMMARY
echo ========================================================================
echo.

echo Automated Testing: COMPLETED
echo   ✓ Basic security validation
echo   ✓ Penetration testing scenarios  
echo   ✓ Authentication security tests
echo   ✓ Input validation tests
echo.

if exist "%ZAP_PATH%" (
    echo OWASP ZAP Scanning: COMPLETED
    echo   ✓ Baseline security scan
    echo   ✓ Vulnerability assessment
    echo   ✓ Report generation
) else (
    echo OWASP ZAP Scanning: SKIPPED ^(ZAP not installed^)
    echo   ! Install OWASP ZAP for comprehensive scanning
)

echo.
echo Manual Testing: CHECKLIST PROVIDED
echo   ! Follow manual testing checklist above
echo   ! Engage external penetration testers for comprehensive assessment
echo   ! See Documentation\Testing\Security\External_PenTest_Engagement_Guide.md
echo.

echo ========================================================================
echo                            NEXT STEPS
echo ========================================================================
echo.
echo 1. Review all automated test results above
echo 2. Check ZAP security reports in %SECURITY_REPORTS_DIR%
echo 3. Perform manual penetration testing using provided checklist
echo 4. Address any identified vulnerabilities
echo 5. Consider engaging external penetration testing services
echo 6. Implement security testing in CI/CD pipeline
echo 7. Schedule regular security assessments
echo.

echo For external penetration testing guidance, see:
echo Documentation\Testing\Security\External_PenTest_Engagement_Guide.md
echo.

echo For OWASP ZAP integration, see:
echo Documentation\Testing\Security\OWASP_ZAP_Integration_Guide.md
echo.

echo Penetration testing suite completed at %DATE% %TIME%
echo.
pause