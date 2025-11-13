# Partner API Access - User Guide

**Version**: 1.0
**Last Updated**: November 12, 2025
**Target Audience**: System Administrators

---

## Table of Contents

1. [Overview](#overview)
2. [Getting Started](#getting-started)
3. [Viewing Partners](#viewing-partners)
4. [Managing API Keys](#managing-api-keys)
5. [Monitoring Usage](#monitoring-usage)
6. [Security Best Practices](#security-best-practices)
7. [Troubleshooting](#troubleshooting)
8. [FAQ](#faq)

---

## Overview

### What is Partner API Access?

The Partner API Access system allows you to securely manage API keys for external partners (such as Amazon, Wayfair, etc.) who need to access GCC's REST API endpoints to retrieve certificate data.

### Key Features

- **Secure API Key Generation**: Cryptographically secure 256-bit API keys
- **Partner Management**: Organize keys by partner organization
- **Usage Tracking**: Monitor API request counts and last usage times
- **Key Lifecycle Management**: Generate, revoke, and set expiration dates
- **Detailed Statistics**: View comprehensive usage metrics per partner
- **Audit Trail**: Track who created and revoked keys

### Architecture

Partners do **not** push data to GCC. Instead:
- Partners **pull** certificate data from GCC using API keys
- Each partner has dedicated API key(s) for authentication
- All API requests are logged and tracked
- Keys can be revoked instantly if compromised

---

## Getting Started

### Prerequisites

- Admin role access to the GCC Database application
- Modern web browser (Chrome, Edge, Firefox, Safari)
- Understanding of REST API concepts

### Accessing the Partner API Page

1. **Log in** to the GCC Database application with your admin credentials
2. Navigate to **Administration** → **API Management** → **Partner API Access**
3. The Partner API Access page will load

**URL**: `http://your-domain.com/Admin/PartnerAPI`

### Page Layout

The Partner API Access page consists of three main sections:

1. **Partners Panel** (Top): Grid of partner cards
2. **Partner Details Panel** (Middle): Usage statistics and API keys for selected partner
3. **All API Keys Panel** (Bottom): Overview of all API keys across all partners

---

## Viewing Partners

### Partner Cards

When you first access the page, you'll see a grid of partner cards displaying:

- **Partner Name**: e.g., "Amazon", "Wayfair"
- **Description**: Brief description of the partner
- **Selection Indicator**: Green checkmark when selected

### Selecting a Partner

1. **Click** on any partner card
2. The card will be **highlighted** with a blue border
3. The **Partner Details Panel** will appear below showing:
   - Usage statistics for that partner
   - List of API keys for that partner

### Partner Information Displayed

- Partner name and description
- Total number of API keys
- Active vs. revoked keys
- Total API requests made
- Last usage timestamp

---

## Managing API Keys

### Generating a New API Key

#### Step 1: Open the Generation Modal

1. Click the **"Generate API Key"** button in the top-right corner
2. A modal dialog will appear

#### Step 2: Fill in the Details

**Required Fields**:
- **Partner**: Select the partner from the dropdown (e.g., "Amazon")

**Optional Fields**:
- **Description**: Label to identify this key (e.g., "Production Access", "Testing Key")
- **Expiration Date**: When the key should automatically expire (leave blank for no expiration)

#### Step 3: Generate the Key

1. Click **"Generate Key"** button
2. Wait for the system to create the key (typically < 1 second)

#### Step 4: Copy the API Key

**CRITICAL**: This is the **only time** you'll see the full API key!

1. A new modal will display the generated API key
2. The key format: `gcc_partner_[64 character hex string]`
3. Click the **"Copy"** button to copy the key to your clipboard
4. **Save the key securely** - you won't be able to see it again
5. Share the key securely with the partner

**Example API Key**:
```
gcc_partner_a3f5c9d8e2b4f7a1c8d6e9f2b5a8c3d7e1f4a9b2c5d8e3f6a9b4c7d2e5f8a1b6c9
```

#### Step 5: Provide Key to Partner

Send the API key to the partner through a secure channel:
- Encrypted email
- Secure file transfer
- Password-protected document
- **Never** send via plain text email or chat

### Understanding API Key Properties

Each API key has the following properties:

| Property | Description |
|----------|-------------|
| **API Key** | The actual authentication token (masked after creation) |
| **Description** | Label to identify the key's purpose |
| **Partner** | Organization this key belongs to |
| **Created** | Date and time the key was generated |
| **Created By** | Admin user who generated the key |
| **Expires** | Expiration date (if set) |
| **Status** | Active or Revoked |
| **Last Used** | When the key was last used for an API request |
| **Request Count** | Total number of API calls made with this key |

### Viewing API Keys

#### View Keys for a Specific Partner

1. **Select a partner** by clicking their card
2. Scroll to the **"API Keys"** table in the Partner Details panel
3. All keys for that partner will be displayed

#### View All API Keys

Scroll to the **"All API Keys"** panel at the bottom of the page to see:
- All API keys across all partners
- Sortable and searchable table
- Partner name for each key

### Revoking an API Key

**When to Revoke**:
- Key has been compromised or exposed
- Partner relationship has ended
- Replacing an old key with a new one
- Security audit requires key rotation

**How to Revoke**:

1. Locate the API key you want to revoke (either in partner-specific or all keys table)
2. Click the **red "Revoke" button** (trash can icon) in the Actions column
3. A confirmation modal will appear:
   ```
   Revoke API Key

   Are you sure you want to revoke this API key?
   The partner will immediately lose access.

   Key: gcc_partner_a3f5...1b6c9

   [Cancel]  [Revoke]
   ```
4. Click **"Revoke"** to confirm
5. The partner's access is **immediately terminated**
6. The key status changes to "Revoked" (red badge)

**Important Notes**:
- Revocation is **immediate** - all API requests with this key will fail
- Revocation is **permanent** - you cannot un-revoke a key
- Revoked keys remain visible for audit purposes
- The partner must be issued a new key to regain access

---

## Monitoring Usage

### Partner-Level Statistics

When you select a partner, the following statistics are displayed:

| Metric | Description |
|--------|-------------|
| **Total API Keys** | All keys ever created for this partner |
| **Active Keys** | Currently valid keys |
| **Revoked Keys** | Keys that have been terminated |
| **Total Requests** | Cumulative API calls from all keys |
| **Last Used** | Most recent API request from any key |

### Key-Level Metrics

For each individual API key, you can see:

- **Request Count**: Number of times this specific key has been used
- **Last Used**: Timestamp of the most recent API call with this key
- **Created Date**: When the key was generated
- **Expiration Date**: When the key will automatically expire (if set)

### Usage Monitoring Best Practices

1. **Regular Reviews**: Check usage statistics monthly
2. **Identify Inactive Keys**: Look for keys that haven't been used recently
3. **Monitor Anomalies**: Watch for unusual spikes in request counts
4. **Track Expiring Keys**: Note keys approaching expiration dates
5. **Audit Trail**: Document why keys were created and revoked

---

## Security Best Practices

### API Key Generation

✅ **DO**:
- Use descriptive labels for keys (e.g., "Production API - Amazon Q4 2025")
- Set expiration dates for temporary access
- Generate separate keys for different environments (staging, production)
- Document the purpose of each key

❌ **DON'T**:
- Reuse keys across multiple partners
- Share keys via insecure channels (email, chat)
- Generate keys without a clear business need
- Forget to set expiration dates for temporary access

### Key Distribution

✅ **DO**:
- Use encrypted communication channels
- Verify recipient identity before sending keys
- Provide clear instructions on how to use the API
- Include contact information for support

❌ **DON'T**:
- Send keys in plain text email
- Post keys in public forums or documentation
- Store keys in version control systems
- Share keys with unauthorized personnel

### Key Lifecycle Management

✅ **DO**:
- Rotate keys regularly (recommended: every 90 days)
- Revoke keys immediately if compromised
- Remove access when partner relationships end
- Keep audit records of all key operations

❌ **DON'T**:
- Leave unused keys active indefinitely
- Ignore expired keys
- Delay revoking compromised keys
- Delete audit records

### Monitoring and Auditing

✅ **DO**:
- Review usage statistics regularly
- Investigate unusual activity patterns
- Document key creation and revocation reasons
- Set up alerts for high usage thresholds

❌ **DON'T**:
- Ignore zero-usage keys
- Overlook sudden usage spikes
- Skip regular security audits
- Forget to track who created each key

---

## Troubleshooting

### Issue: Partner Can't Access the API

**Symptoms**: Partner reports 401 Unauthorized errors

**Possible Causes**:
1. API key was entered incorrectly
2. API key has been revoked
3. API key has expired
4. API key is not being sent in the correct header

**Resolution**:
1. Verify the key is **Active** (not Revoked) in the Partner API page
2. Check if the key has an expiration date that has passed
3. Confirm the partner is using the correct authentication header:
   ```
   Authorization: Bearer gcc_partner_[key]
   ```
4. If key is compromised, **revoke it** and generate a new one

### Issue: Can't See the Full API Key

**Symptoms**: API key appears as `gcc_partner_a3f5...1b6c9` (masked)

**Explanation**: For security reasons, API keys are only shown **once** when first generated

**Resolution**:
- The key cannot be retrieved again
- If lost, you must:
  1. Revoke the old key
  2. Generate a new key
  3. Securely provide the new key to the partner

### Issue: High Request Count on a Key

**Symptoms**: Request count is unexpectedly high

**Possible Causes**:
1. Legitimate increase in partner activity
2. Partner is making inefficient API calls
3. Potential abuse or automated scraping

**Resolution**:
1. Check with the partner about their usage patterns
2. Review API call logs for unusual patterns
3. Consider implementing rate limiting
4. If abuse is suspected, revoke the key immediately

### Issue: Generate Button Not Working

**Symptoms**: Clicking "Generate API Key" does nothing

**Possible Causes**:
1. JavaScript error in browser console
2. Network connectivity issue
3. Session timeout (not logged in)

**Resolution**:
1. Open browser Developer Tools (F12) and check Console for errors
2. Verify you're still logged in (refresh the page)
3. Try a different browser
4. Contact technical support if issue persists

---

## FAQ

### Q: How many API keys can I create for a partner?

**A**: There is no hard limit. You can create multiple keys per partner. Best practice is to use different keys for different purposes (e.g., production, staging, testing).

### Q: What happens when an API key expires?

**A**: When a key reaches its expiration date:
- The key automatically becomes inactive
- All API requests using that key will receive 401 Unauthorized
- The key's status changes to indicate it's expired
- You must generate a new key for continued access

### Q: Can I extend the expiration date of an existing key?

**A**: No, expiration dates cannot be modified after creation. To extend access:
1. Generate a new key with a later expiration date
2. Provide the new key to the partner
3. Revoke the old key after confirming the new one works

### Q: How do I know if a key has been compromised?

**A**: Signs of compromise include:
- Unusual usage patterns (spikes, unusual times)
- API calls from unexpected IP addresses
- Partner reports they didn't make certain requests
- Security alerts from monitoring systems

If you suspect compromise, **immediately revoke the key**.

### Q: What's the difference between "Active" and "Revoked" status?

**A**:
- **Active**: Key is valid and can be used for API authentication
- **Revoked**: Key has been permanently disabled and cannot be used

### Q: Can I see what API endpoints a key has accessed?

**A**: The current interface shows request counts and last usage time. For detailed access logs including specific endpoints accessed, contact your technical administrator for server log access.

### Q: What should I tell the partner about using their API key?

**A**: Partners should be instructed to:
1. Store the key securely (encrypted, not in version control)
2. Include the key in the Authorization header:
   ```
   Authorization: Bearer gcc_partner_[their_key]
   ```
3. Make requests to the documented API endpoints
4. Contact support if they receive authentication errors
5. Never share their key with unauthorized parties

### Q: How secure are these API keys?

**A**: API keys use 256-bit cryptographic randomness, equivalent to industry-standard security. The key format (`gcc_partner_[64 chars]`) provides:
- 256 bits of entropy
- Resistance to brute-force attacks
- Globally unique identifiers

### Q: Can I generate a key for a partner that doesn't exist in the system?

**A**: No, you can only generate keys for partners that have been created in the Partners management system. If you need to add a new partner, navigate to **Administration** → **Data Management** → **Partners**.

### Q: What happens to existing API keys when I revoke one?

**A**: Revoking a key only affects that specific key. Other active keys for the same partner continue to work normally. This allows you to rotate keys without disrupting service.

---

## Appendix A: API Key Format

### Structure

```
gcc_partner_[64 character hexadecimal string]
```

### Example

```
gcc_partner_a3f5c9d8e2b4f7a1c8d6e9f2b5a8c3d7e1f4a9b2c5d8e3f6a9b4c7d2e5f8a1b6c9
```

### Components

- **Prefix**: `gcc_partner_` (identifies this as a GCC partner API key)
- **Random Data**: 64 hexadecimal characters (32 bytes of random data)
- **Total Length**: 76 characters

### Security Characteristics

- **Entropy**: 256 bits (2^256 possible combinations)
- **Generation**: Cryptographically secure random number generator
- **Storage**: Hashed in database (plain text only shown once)
- **Validation**: Case-insensitive hexadecimal verification

---

## Appendix B: Quick Reference

### Common Tasks

| Task | Steps |
|------|-------|
| **Generate Key** | 1. Click "Generate API Key"<br>2. Select partner<br>3. Add description (optional)<br>4. Click "Generate"<br>5. Copy key immediately |
| **Revoke Key** | 1. Find key in table<br>2. Click red "Revoke" button<br>3. Confirm revocation |
| **View Partner Stats** | 1. Click partner card<br>2. View statistics panel below |
| **Check All Keys** | Scroll to "All API Keys" section at bottom |

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| F5 | Refresh page data |
| Ctrl+F | Search within tables |
| Esc | Close modal dialogs |

### Status Badge Colors

| Color | Status | Meaning |
|-------|--------|---------|
| Green | Active | Key is valid and can be used |
| Red | Revoked | Key has been permanently disabled |
| Yellow | Expired | Key has passed its expiration date |

---

## Support

### Getting Help

If you encounter issues or have questions:

1. **Review this guide**: Check the Troubleshooting section
2. **Check system status**: Verify the application is running
3. **Contact IT Support**: Provide error messages and screenshots
4. **Technical Documentation**: Refer to the API specification document

### Reporting Issues

When reporting issues, include:
- Your username and role
- Partner name and key description (if applicable)
- Error messages or unexpected behavior
- Screenshots of the issue
- Steps to reproduce the problem

### Feedback

We welcome feedback on this system. Contact your system administrator with:
- Feature requests
- Usability improvements
- Documentation suggestions

---

**Document Information**

- **Version**: 1.0
- **Release Date**: November 12, 2025
- **Author**: GCC Development Team
- **Last Reviewed**: November 12, 2025
- **Next Review**: February 12, 2026

---

© 2025 GreenCircle Certified. All rights reserved.
