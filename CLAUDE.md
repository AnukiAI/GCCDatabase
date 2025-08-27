- put export notes and documentation documents created in C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\0_Documentation\docs
- Remember to refer to this document for tasks. Update the file as we progress through the changes. adding notes and resolution.
- remember the C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\0_Documentation\pages\ this is the mock design for all of the pages.
- 0_Documentation/docs/Mockup-vs-Implementation-Gap-Analysis-2025-08-27.md


- This application does not use pagination on the Certificates pages. It will have lazy loading as the user scrolls down the page. This is to improve performance and user experience when dealing with large datasets.
- Search Functionality should provide Autocomplete suggestions.
- How should the search work?
  - The search should be case-insensitive and should match any part of the certificate name or description.
  - It should also support partial matches, so if a user types "cert", it should return results like "Certificate of Completion" and "Certainty in Data".
  - The search results should be displayed in a dropdown below the search bar, allowing users to quickly select a result.
  - If no results are found, a message should be displayed indicating that no matches were found.
	- 
	- 
- C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\Views\Certificate\CertificateTileView.cshtml:
- This is the landing page (https://db.greencirclecertified.com/certificate) on the current website. 
- Clicking anywhere on the tile of a certificate will take the user to the Certificate View, separate page that opens into a new window, that shows details about the selected certificate. 
- Search bar, autocomplete suggestions, filters on search terms within results, mobile compatibility, and the ability to narrow down search results based on specific criteria within categories, allowing users to easily find specific content on a site. 
- Ability to sort tiles on all Public database columns (Certificate #, Certification Type, Effective Date, Expiration Date, Company, Company Contact Email/URL, Product Name, Product Category, Product Type, Product Type Subcategory, Location, Country, External Tag) (Striked out does not need sorting)
- Ability to filter by search categories
- All Public data fields to be displayed: Certificate #, Certification Type, Effective Date, Expiration Date, Company, Company Contact Email/URL, Product Name, Product Category, Product Type, Product Type Subcategory, Location, Country, External Tag
- Need to make sure a unique URL can be created based on search criteria so clients can directly link to DB with just listing of their certifications (SCS does this as you add search criteria). Unique URL per Certification View for direct cert access. 
- Ability to view # Results
- Ability to Clear filters
- Certification download capability. Ability to download certificate directly from tile via download icon. 



