# Azure AD B2C Setup Guide for GCC Web Application

This guide explains how to configure Azure Active Directory B2C (Azure AD B2C) authentication for the GCC Web Application.

## Overview

The application is designed to work with both Azure AD B2C authentication (for production) and fallback cookie authentication (for development/testing). The system automatically detects whether Azure AD B2C is properly configured and switches accordingly.

## Azure AD B2C Configuration Steps

### 1. Create Azure AD B2C Tenant

1. Sign in to the Azure portal
2. Create a new Azure AD B2C tenant or use an existing one
3. Note your tenant name (e.g., `contoso.onmicrosoft.com`)

### 2. Register the Application

1. In your Azure AD B2C tenant, go to **App registrations**
2. Click **New registration**
3. Configure:
   - **Name**: `GCC Web Application`
   - **Supported account types**: Accounts in any identity provider or organizational directory
   - **Redirect URI**: 
     - Type: `Web`
     - URL: `https://your-domain.com/signin-oidc` (for production)
     - URL: `https://localhost:5001/signin-oidc` (for development)
4. Click **Register**
5. Note the **Application (client) ID**

### 3. Configure Authentication

1. In your app registration, go to **Authentication**
2. Add logout URL: `https://your-domain.com/signout-callback-oidc`
3. Under **Implicit grant and hybrid flows**, enable:
   - Access tokens
   - ID tokens
4. Click **Save**

### 4. Create Client Secret (if needed)

1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Add description and set expiration
4. Copy the secret **Value** (not the Secret ID)

### 5. Create User Flows

Create the following user flows in your Azure AD B2C tenant:

#### Sign-up and Sign-in Flow
1. Go to **User flows** > **New user flow**
2. Select **Sign up and sign in** > **Recommended**
3. Name: `B2C_1_SignUpSignIn`
4. Configure identity providers (Local Account, Social providers)
5. Configure user attributes and application claims
6. Create the flow

#### Password Reset Flow
1. Create new user flow: **Password reset** > **Recommended**
2. Name: `B2C_1_PasswordReset`
3. Configure as needed

#### Profile Editing Flow
1. Create new user flow: **Profile editing** > **Recommended**
2. Name: `B2C_1_ProfileEdit`
3. Configure user attributes for editing

## Application Configuration

### Update appsettings.json

Replace the placeholder values in `appsettings.json`:

```json
{
  "AzureAdB2C": {
    "Instance": "https://yourtenantname.b2clogin.com/",
    "ClientId": "your-application-client-id",
    "ClientSecret": "your-client-secret",
    "Domain": "yourtenantname.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "SignUpSignInPolicyId": "B2C_1_SignUpSignIn",
    "ResetPasswordPolicyId": "B2C_1_PasswordReset",
    "EditProfilePolicyId": "B2C_1_ProfileEdit",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### Configuration Values

- **Instance**: Your B2C tenant URL (format: `https://{tenant-name}.b2clogin.com/`)
- **ClientId**: Application (client) ID from app registration
- **ClientSecret**: Client secret created in step 4 (optional for public clients)
- **Domain**: Your B2C tenant domain (format: `{tenant-name}.onmicrosoft.com`)
- **TenantId**: Your tenant ID (GUID) or tenant name
- **SignUpSignInPolicyId**: Name of your sign-up/sign-in user flow
- **ResetPasswordPolicyId**: Name of your password reset user flow
- **EditProfilePolicyId**: Name of your profile editing user flow

## Development vs Production

### Development Mode
- If Azure AD B2C is not configured (placeholder values contain `{}`), the application falls back to cookie-based authentication
- Test users are available:
  - `admin@greencirclecertified.com` / `admin123` (Admin role)
  - `analyst@greencirclecertified.com` / `analyst123` (Analyst role)
  - `partner@amazon.com` / `partner123` (External role)
  - `user@test.com` / `user123` (Public role)

### Production Mode
- When Azure AD B2C is properly configured, all authentication goes through Azure AD B2C
- Users are automatically redirected to B2C sign-in pages
- User roles are determined based on email domains (configurable in `AccountController.DetermineUserRole`)

## User Flow URLs

The application will redirect users to these Azure AD B2C URLs:

- **Sign-in**: `/Account/SignIn`
- **Sign-out**: `/Account/SignOut` (POST)
- **Password Reset**: `/Account/ResetPassword`
- **Profile Edit**: `/Account/EditProfile`

## Role Mapping

The application maps users to roles based on email domains:

- **Admin**: `@greencirclecertified.com`
- **Analyst**: `@contractor.com`, `@analyst.com`
- **External**: Contains "partner", `@amazon.com`
- **Public**: All other users

You can customize this logic in the `AccountController.DetermineUserRole` method.

## Troubleshooting

### Common Issues

1. **Redirect URI Mismatch**: Ensure the redirect URIs in Azure AD B2C match your application URLs
2. **Policy Not Found**: Verify user flow names match the configuration
3. **Authentication Fails**: Check client ID and tenant configuration
4. **Claims Missing**: Ensure required claims are configured in user flows

### Logging

The application logs authentication events. Check application logs for:
- Sign-in/sign-out events
- Authentication errors
- Role assignments

### Testing

1. **Development**: Use the fallback test accounts
2. **Azure AD B2C**: Create test users in your B2C tenant
3. **Verify user flows work correctly**
4. **Test role-based access to different areas**

## Security Considerations

1. **Client Secrets**: Store securely, rotate regularly
2. **HTTPS**: Always use HTTPS in production
3. **Session Management**: Configure appropriate session timeouts
4. **CORS**: Configure CORS policies if needed
5. **User Flows**: Regularly review and update user flow configurations

## Support

For issues with:
- **Azure AD B2C Setup**: Check Azure documentation
- **Application Integration**: Review application logs and configuration
- **User Flows**: Test in Azure portal before deploying