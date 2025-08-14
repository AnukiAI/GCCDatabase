# Testing Login Notes

## Overview
This document provides testing information for the GCC Web App authentication system during development.

## Test Environment Setup

### Starting the Application
1. Navigate to the project directory
2. Run: `dotnet run`
3. Application will start on: `http://localhost:5101`
4. Navigate to: `http://localhost:5101/Account/Login`

## Test User Accounts

The following test accounts are available for development and testing purposes:

### Available Test Users

| Email | Role | Access Level |
|-------|------|-------------|
| `admin@test.com` | Admin | Full system access |
| `analyst@test.com` | Analyst | Data analysis and reporting |
| `external@test.com` | External | Limited external access |
| `user@test.com` | External | Limited external access |

### Password Policy (Test Environment)
- **Any non-empty password** will work for all test accounts
- Examples: `password`, `123`, `test`, etc.
- Empty passwords will be rejected

## Login Testing Scenarios

### 1. Standard Login Process
1. Go to `/Account/Login`
2. Enter any test email from the table above
3. Enter any non-empty password
4. Click "Login"
5. Should redirect to the appropriate dashboard based on role

### 2. Quick Login (Development Feature)
- Quick login buttons are available for rapid role switching during development
- Located in the AccountController for testing different user roles

### 3. Forgot Password Flow
1. Go to `/Account/Login`
2. Click "Forgot Password"
3. Enter any email address
4. System will display success message (simulated)
5. In production, this would send a real password reset email

### 4. Password Reset Flow
1. Access via: `/Account/ResetPassword?token=test&email=admin@test.com`
2. Enter new password (minimum 6 characters)
3. Confirm password
4. Click "Reset Password"
5. Success message should appear

## User Role Access Testing

### Admin Role (`admin@test.com`)
- Access to all admin functions
- User management capabilities
- System configuration access

### Analyst Role (`analyst@test.com`)
- Data analysis tools
- Reporting capabilities
- Certificate management

### External Role (`external@test.com` or `user@test.com`)
- Limited access to public certificate views
- Basic search functionality

## Authentication Features

### Security Features Implemented
- Cookie-based authentication
- Anti-forgery tokens on forms
- Role-based authorization
- Session management
- Secure logout functionality

### Session Management
- Sessions expire after 30 minutes of inactivity
- "Remember me" option extends session (if checked during login)
- Manual logout available via logout button

## Error Scenarios to Test

### Invalid Login Attempts
1. Empty email field → Validation error
2. Invalid email format → Validation error  
3. Empty password → "Invalid email or password" error
4. Non-existent email → "Invalid email or password" error

### Navigation Testing
- Accessing protected pages without login → Redirect to login page
- Successful login → Redirect to originally requested page or Home
- Logout → Clear session and redirect to login page

## Browser Compatibility
The login pages have been tested and styled for:
- Chrome/Edge (recommended)
- Firefox
- Safari
- Mobile browsers (responsive design)

## Production Notes

⚠️ **Important**: This testing setup is for development only!

In production, you must:
1. Replace test accounts with real user authentication
2. Implement proper password hashing and validation
3. Set up real email functionality for password resets
4. Configure proper session security
5. Set up SSL/HTTPS certificates
6. Implement proper database user management

## Troubleshooting

### Common Issues
- **Login button not responding**: Check browser console for JavaScript errors
- **Styles not loading**: Verify CSS files are properly referenced
- **Session expires quickly**: Check authentication configuration in Program.cs
- **Role access denied**: Verify user role claims are properly set

### Debug Information
- Check application logs for authentication events
- Use browser developer tools to inspect network requests
- Verify cookies are being set properly

## Quick Reference Commands

```bash
# Start application
dotnet run

# Build application
dotnet build

# View running processes
dotnet --info
```

---
*Last Updated: August 8, 2025*
*Document Version: 1.0*