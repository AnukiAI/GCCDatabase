# GCC Web Application Mermaid Diagrams

This directory contains individual Mermaid diagram files extracted from the UserFlows.md document for easy viewing and integration.

## Files Overview

### User Flow Diagrams

1. **01-authentication-flow.mermaid** - User authentication and login process with 2FA
2. **02-admin-user-flow.mermaid** - Complete admin user workflow and capabilities
3. **03-analyst-user-flow.mermaid** - Analyst certificate management workflows
4. **04-external-user-flow.mermaid** - External user limited access patterns
5. **05-public-user-flow.mermaid** - Public user certificate viewing flows

### Process & Workflow Diagrams

6. **06-certificate-management-workflow.mermaid** - Certificate CRUD operations workflow
7. **07-company-product-management.mermaid** - Company and product data management
8. **08-error-handling-security-flow.mermaid** - Error handling and security processes
9. **09-system-architecture-flow.mermaid** - Technical system architecture interactions
10. **10-api-flow.mermaid** - API request/response lifecycle

### Data & Document Management

11. **11-data-import-export-flow.mermaid** - Bulk data operations and file handling
12. **12-document-management-flow.mermaid** - Document upload, storage, and retrieval

### Security & Access Control

13. **13-security-comprehensive-flow.mermaid** - Complete security architecture overview
14. **14-rbac-flow.mermaid** - Role-Based Access Control implementation
15. **15-deployment-flow.mermaid** - CI/CD and deployment processes

## Usage

### Viewing in IDEs
- **VS Code**: Install the Mermaid Preview extension
- **JetBrains IDEs**: Use the Mermaid plugin
- **Online**: Copy content to https://mermaid.live/

### Integration
- These files can be embedded in documentation platforms
- Compatible with GitHub, GitLab, and most markdown renderers
- Can be converted to SVG/PNG using mermaid-cli

### Customization
- Each file uses standard Mermaid syntax
- Styling can be modified using Mermaid CSS classes
- Color schemes are already applied for different user roles and processes

## Color Coding Legend

- **Admin Actions**: Purple/Lavender (#f3e5f5)
- **Analyst Actions**: Green (#e8f5e8) 
- **External User**: Orange (#fff3e0)
- **Public User**: Pink (#fce4ec)
- **Error States**: Red (#ffebee)
- **Warning States**: Orange (#fff3e0)
- **Security Elements**: Red/Pink variants
- **Success States**: Green variants

## File Dependencies

All files are standalone and have no dependencies on each other. Each represents a complete flow or process within the GCC Web Application.

## Maintenance

When updating flows:
1. Update the corresponding .mermaid file
2. Test rendering in a Mermaid viewer
3. Update this README if new files are added
4. Consider updating the master UserFlows.md file

---
*Generated from UserFlows.md - Last updated: August 29, 2025*