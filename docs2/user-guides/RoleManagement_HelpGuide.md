# Role Management Help Guide

This guide explains the role-based access control (RBAC) system in the GCC Database application, including available roles, permissions, and how to configure user access.

---

## Overview

The GCC Database uses a granular permission system organized into 6 categories with 24 individual permissions. Roles are assigned to users and determine what actions they can perform within the system.

### Permission Categories

| Category | Icon | Description |
|----------|------|-------------|
| **Certificates** | Certificate | View, create, edit, delete certificates and manage workflow |
| **Data** | Database | Export, import data and access reports |
| **Administration** | Cog | Manage users, roles, companies, tags, and partners |
| **API Access** | Plug | Programmatic access for integrations |
| **Products** | Box | View and manage product catalog |
| **System** | Shield | Global configuration and system settings |

---

## Role Types

### System Roles (Built-in)

These roles come pre-configured and represent common access patterns. The Admin role cannot be deleted.

### Custom Roles

Administrators can create custom roles with specific permission combinations for specialized use cases like Partner access or API-only integrations.

---

## Role Descriptions

### Administrator

**Full system access and control**

| Category | Access Level |
|----------|--------------|
| Certificates | Full Access |
| Data | Full Access |
| Administration | Full Access |
| API Access | Full Access |
| Products | Full Access |
| System | Full Access |

**Key Permissions:**
- All certificate operations (view, create, edit, delete, workflow)
- All administrative functions (users, roles, companies, partners)
- All API access (read, write, delete)
- System configuration and settings

**Use Cases:**
- System administrators
- IT staff managing the application
- Senior management requiring full oversight

---

### Analyst

**Certificate management and workflow**

| Category | Access Level |
|----------|--------------|
| Certificates | Create/Edit (no delete) |
| Data | Export/Import/Reports |
| Administration | No Access |
| API Access | No Access |
| Products | View Only |
| System | No Access |

**Key Permissions:**
- View, create, and edit certificates
- Manage certification workflow (approve/reject)
- Export and import data
- View reports
- View products

**Denied:**
- Delete certificates
- Administrative functions
- API access

**Ideal For:**
- Internal certification team members
- Quality assurance staff
- Certificate reviewers

---

### Partner

**Amazon, Novi, and other partners**

| Category | Access Level |
|----------|--------------|
| Certificates | View Only |
| Data | Export/Reports |
| Administration | No Access |
| API Access | Read Only |
| Products | View Only |
| System | No Access |

**Key Permissions:**
- View certificates
- Export data
- View reports
- API read access
- View products

**Denied:**
- Create, edit, or delete certificates
- Administrative functions
- API write access

**Ideal For:**
- Amazon Climate Pledge team
- Novi Connect integrations
- Third-party verification services

---

### External Client

**Company-specific certificate access**

| Category | Access Level |
|----------|--------------|
| Certificates | Own Company Only |
| Data | Export Own Data |
| Administration | No Access |
| API Access | No Access |
| Products | Own Company Only |
| System | No Access |

**Key Permissions:**
- View certificates for their associated company
- View products for their associated company
- Export their own company's data
- View reports

**Denied:**
- View other companies' data
- Create or edit certificates
- Administrative functions
- API access

**Important:** External users can ONLY see data associated with their company/partner.

**Ideal For:**
- Client company representatives
- Brand managers viewing their products
- Compliance officers (external)

---

### API Integration

**Programmatic access only, no UI**

| Category | Access Level |
|----------|--------------|
| Certificates | API Only |
| Data | No UI Access |
| Administration | No Access |
| API Access | Read/Write |
| Products | API Only |
| System | No Access |

**Key Permissions:**
- API read access
- API write access
- No web UI access

**Denied:**
- Web interface access
- Administrative functions
- API delete operations

**Ideal For:**
- Automated data sync services
- Third-party integrations
- Reporting dashboards
- Partner API consumers

---

### Public

**Read-only public access**

| Category | Access Level |
|----------|--------------|
| Certificates | Public Only |
| Data | No Access |
| Administration | No Access |
| API Access | No Access |
| Products | View Only |
| System | No Access |

**Key Permissions:**
- View publicly available certificates
- View products

**Denied:**
- Create, edit, or delete anything
- Export or import data
- API access
- Administrative functions

---

## Permission Matrix

Quick reference showing which roles have which permissions:

### Certificate Permissions

| Permission | Admin | Analyst | Partner | External | API Only | Public |
|------------|:-----:|:-------:|:-------:|:--------:|:--------:|:------:|
| View Certificates | Yes | Yes | Yes | Yes | Partial | Yes |
| Create Certificates | Yes | Yes | No | No | No | No |
| Edit Certificates | Yes | Yes | No | No | No | No |
| Delete Certificates | Yes | No | No | No | No | No |
| Manage Workflow | Yes | Yes | No | No | No | No |

### Data Permissions

| Permission | Admin | Analyst | Partner | External | API Only | Public |
|------------|:-----:|:-------:|:-------:|:--------:|:--------:|:------:|
| Export Data | Yes | Yes | Yes | Partial | No | No |
| Import Data | Yes | Yes | No | No | No | No |
| View Reports | Yes | Yes | Yes | Yes | No | No |
| View Audit Logs | Yes | No | No | No | No | No |

### Administration Permissions

| Permission | Admin | Analyst | Partner | External | API Only | Public |
|------------|:-----:|:-------:|:-------:|:--------:|:--------:|:------:|
| Manage Users | Yes | No | No | No | No | No |
| Manage Roles | Yes | No | No | No | No | No |
| Manage Companies | Yes | No | No | No | No | No |
| Manage Tags | Yes | No | No | No | No | No |
| Manage Partners | Yes | No | No | No | No | No |

### API Permissions

| Permission | Admin | Analyst | Partner | External | API Only | Public |
|------------|:-----:|:-------:|:-------:|:--------:|:--------:|:------:|
| API Read | Yes | No | Yes | No | Yes | No |
| API Write | Yes | No | No | No | Yes | No |
| API Delete | Yes | No | No | No | No | No |

### Product Permissions

| Permission | Admin | Analyst | Partner | External | API Only | Public |
|------------|:-----:|:-------:|:-------:|:--------:|:--------:|:------:|
| View Products | Yes | Yes | Yes | Yes | Yes | Yes |
| Manage Products | Yes | No | No | No | No | No |
| Manage Product Types | Yes | No | No | No | No | No |

### System Permissions

| Permission | Admin | Analyst | Partner | External | API Only | Public |
|------------|:-----:|:-------:|:-------:|:--------:|:--------:|:------:|
| System Settings | Yes | No | No | No | No | No |
| Manage Cert Types | Yes | No | No | No | No | No |
| Manage Custom Fields | Yes | No | No | No | No | No |

---

## Managing Roles

### Creating a New Role

1. Navigate to **Admin > Role Management**
2. Click **Add Role**
3. Enter a **Role Name** and optional **Description**
4. Select the required permissions by category
5. Use **Select All** or **Clear All** buttons for quick selection
6. Click **Save** to create the role

### Editing an Existing Role

1. Navigate to **Admin > Role Management**
2. Click the **Edit** button next to the role
3. Modify the name, description, or permissions as needed
4. Click **Save** to apply changes

### Deleting a Role

1. Navigate to **Admin > Role Management**
2. Click the **Delete** button next to the role
3. Confirm the deletion in the confirmation dialog

**Note:** The Admin role cannot be deleted. Roles with assigned users should be reassigned before deletion.

---

## Best Practices

### Principle of Least Privilege
Assign users the minimum permissions necessary to perform their job functions.

### Use Custom Roles for Specific Needs
Instead of granting excessive permissions with a built-in role, create a custom role with only the required permissions.

### Regular Access Reviews
Periodically review user role assignments to ensure they remain appropriate.

### Document Custom Roles
When creating custom roles, use clear names and descriptions so other administrators understand their purpose.

---

## Common Scenarios

### Scenario 1: New Certification Analyst
**Recommended Role:** Analyst
- Can create and edit certificates
- Can manage workflow states
- Cannot delete certificates (prevents accidental data loss)
- Cannot access administrative functions

### Scenario 2: Amazon Climate Pledge Integration
**Recommended Role:** Partner (or custom "Amazon Partner" role)
- View-only access to certificates
- API read access for data sync
- Export capability for reporting
- No write or delete permissions

### Scenario 3: Client Company Representative
**Recommended Role:** External
- View only their company's certificates
- View only their company's products
- Can export their own data
- Cannot see other companies' information

### Scenario 4: Automated Sync Service
**Recommended Role:** API Integration
- API read/write access
- No web UI access needed
- Programmatic certificate lookups
- Data synchronization capabilities

---

## Troubleshooting

### User Cannot Access Expected Features
1. Verify the user's assigned role in User Management
2. Check the role's permissions in Role Management
3. Ensure the permission for the desired feature is enabled

### Permission Changes Not Taking Effect
1. User may need to log out and log back in
2. Check if the change was saved successfully
3. Verify no conflicting permissions exist

### Cannot Delete a Role
1. Ensure no users are currently assigned to the role
2. Verify you have the "Manage Roles" permission
3. Note that the Admin role cannot be deleted

---

## Related Documentation

- [Requirements Document](Requirements.md)
- [User Management Guide](UserManagement_HelpGuide.md)
- [API Reference](../claude-ref/API-REFERENCE.md)

---

*Last Updated: January 2026*
