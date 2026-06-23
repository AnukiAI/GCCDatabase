You are helping triage Sprint 3 tester-reported defects for the GCC Certification application.

Review the issue list below and produce a structured defect triage report before proposing fixes. Do not start coding until the issues are grouped, prioritized, and root causes are identified.

## Objectives

1. Analyze each reported defect.
2. Group related issues that may share the same root cause.
3. Identify duplicate or dependent issues.
4. Assign a severity and priority to each issue.
5. Recommend the correct fix approach.
6. Identify where regression testing is needed.
7. Create a developer-ready action plan.

## Application Context

The application includes certificate creation, certificate import, public certificate search, internal/admin certificate views, partner view, certificate tiles, PDF viewing/downloading, and CSV exports.

The tester-reported issues include problems with:

* Certificate status validation and date-driven status changes
* Certificate upload/import behavior
* Product category defaulting incorrectly
* Exact-match search behavior
* Autocomplete/filter text behavior
* PDF download errors
* Certificate tile spacing/layout
* Audit metadata on imported certificates
* CSV percentage formatting
* Long URL wrapping on certificate pages

## Triage Instructions

For each issue, provide the following:

### 1. Issue Summary

Summarize the defect in plain language.

### 2. Likely Root Cause

Identify the most likely technical cause. Call out whether it is likely:

* Front-end validation
* Back-end validation
* Data mapping/import parsing
* Database default value issue
* UI layout/CSS issue
* API response issue
* File handling/caching issue
* Export formatting issue
* Search normalization issue

### 3. Related Issues

Identify any related tickets that may need to be fixed together.

Pay special attention to these likely clusters:

#### Status / Date / Upload Validation Cluster

Issues: 188, 194
Investigate whether imported or auto-generated certificate statuses are being saved as display text instead of valid enum/dropdown values. Also verify whether date-based expiration logic is incorrectly blocking later status correction.

#### Product Category Mapping Cluster

Issues: 189, 195, possibly 186
Investigate whether Product Category defaults to “Chemical and Raw Materials” when the upload value is missing, invalid, not matched exactly, or when the field is left blank during manual certificate creation.

#### Import File Handling Cluster

Issue: 190
Investigate whether the upload component is caching file names or failing to reset state when a revised file with the same filename is selected.

#### Public/Internal Layout Cluster

Issue: 192
Investigate long Company URL wrapping behavior on public and internal certificate detail pages.

#### Upload Success State Cluster

Issue: 193
Investigate whether the upload success message state is not cleared when clicking “Add Certificate” after a successful upload.

#### Search / Filter Cluster

Issues: 196, 197
Investigate search normalization and filter/autocomplete behavior. For issue 196, the search should support user-friendly matching where trademark/special characters do not prevent valid matches. For issue 197, autocomplete suggestions should not override the user’s explicit search/filter value.

#### PDF Access Cluster

Issue: 198
Investigate why the PDF exists and can be viewed from the certificate detail page, but download fails from the search screen and elsewhere. Validate file URL generation, permissions, MIME type, storage path, download endpoint, and front-end button behavior.

#### Tile Layout Cluster

Issue: 199
Review certificate tile spacing and reduce vertical whitespace without breaking the authenticated view fields such as Status and Partner.

#### Audit Metadata Cluster

Issue: 200
Investigate why imported Product ID records show Created as “Invalid Date” and Created By as “System” instead of the import date and importing user.

#### CSV Export Formatting Cluster

Issue: 201
Investigate inconsistent Cert Results formatting in partner CSV export where some percentages export as decimals, such as 0.17, and others export as whole numbers, such as 100.

## Severity / Priority Guidance

Use the following triage logic:

### Critical

Blocks core certificate creation, import, search, PDF access, or data correction.

### High

Causes incorrect data, prevents users from correcting records, corrupts imported values, or prevents certificate download.

### Medium

Impacts usability, layout, export consistency, or user confidence but has a workaround.

### Low

Visual polish or minor display issue with limited business impact.

## Expected Output Format

Return the triage in this format:

| ID | Area | Summary | Likely Root Cause | Related Issues | Severity | Priority | Recommended Fix | Regression Tests |
| -- | ---- | ------- | ----------------- | -------------- | -------- | -------- | --------------- | ---------------- |

After the table, provide:

## Recommended Fix Order

Group the work into logical batches. Prioritize fixes that address shared root causes first.

## Developer Action Plan

Provide a step-by-step action plan for the development team.

## Regression Test Plan

Provide specific regression tests for:

* Manual certificate creation
* Certificate import
* Status dropdown behavior
* Expiration date logic
* Product category mapping
* Search behavior with special characters
* Filter/autocomplete behavior
* PDF view and download behavior
* CSV export formatting
* Audit metadata on imported records

## Questions / Clarifications Needed

List any questions that need to be answered by the testers or product owner before development begins. Do not block obvious fixes if the expected behavior is clear.

## Issue List

[PASTE TESTER ISSUE TABLE HERE]
ID	Date Opened	classification	Sprint 3 5-4	Sprint 3 6/23 Notes	Sprint 3 Notes	Anuki Status Sprint 2	4-10-26 comments - SP2	"Date Closed - 
GCC Confirmed Fixed"	Raised By	"Current Status - 
Anuki Marks Complete"	Page	Defect Description	Image # (if needed)	Details - Development Notes
188	6/11/26		New						Gemma	Open	Cert Creation Page	If a certificate date is set to a date in the past, the status will automatically update to Expired. Once revising the date to a date in the future, an error message appears stating that a certificate cannot be changed from Expired to Client Approved - Paid. The same error message appears when the status changes from an invalid entry from upload (see issue ID 194) andattempting to select a Status from the drop-down menu.	86	
189	6/11/26		New						Gemma	Open	Cert Creation Page	"Product Category is generating as ""Chemical and Raw Materials"" even when another category was entered on the Cert Upload Template.
CARLY NOTE: Anuki I assume this is tied to issue going on in #186"		
190	6/11/26		New						Gemma	Open	Import Certificates	When using the import function, when attempting to upload a file, you get an error message if you try to replace the attached file with a revised version of the same file with the same file name (i.e. I changed some data, saved, and attempted to upload the same file within the Import Certificates window)	87	
192	6/11/26		New						Gemma	Open	Cert Creation Page	Public and Internal pages: Company URL is running off the page when it is longer than 1 line. It eventually wraps text, but part of the URL is not visible		
193	6/11/26		New						Gemma	Open	Cert Creation Page	When I've just successfully uploaded a certificate, and go to click the "Add Certificate" button to create another, a pop-up appears saying the last certificate was successfully uploaded. 		
194	6/11/26		New						Gemma	Open	Cert Creation Page	If status name is not entered into upload form exactly (i.e. must have a dash in Client Approved - Paid), the status appears to say Client Approved Paid when viewing on the tile, but when clicking it, the status is blank because it was not an exact match with the drop down options. You cannot then select ANY of the options from the dropdown, you must delete and start again.		
195	6/11/26		New						Gemma	Open	Cert Creation Page	"Product Category is generating as ""Chemical and Raw Materials"" even when you leave blank when creating the certificate.
CARLY NOTE: Anuki I assume this is tied to issue going on in #186"		
196	6/11/26		New						Gemma	Open	Public View	The search function needs to be an exact match; if the search is entered as "Product A" but the actual name is "Product™ A", it will not yield any results		
197	6/15/26		New						Dan	Open	Cert search view	when i search on the certificate page for the word "test" the filter bubble shows test, but the text in the filters column reads "carly test". It seems like it auto populates with other search possibilities, but I cannot get it to stop doing this. Auto populate is good - but I should be able to override like you can in excel for instances... if the autopopulate turns out to not be what the use wants. It actually seems like it is searching correctly... but the text box under the blue 'hide filters' is acting up!	88	
198	6/15/26		New						Dan	Open	Cert search view	this error message is showuing up - but the PDF IS indeed there becuase if I click in to the certificate card instead of try to download from the search screen... I can then actually see the PDF... BUT regardless of where I try.. I cannot dowload the PDF.	89	
199	6/16/26		New						Carly	Open	Cert Tiles	"Reduce spacing in tile to condense size of tile (per 6/15 email). See image 90.
I was thinking the areas with blue highlight below can be tightened up (esp the one after location). The yellow spots may have a little room. 
The top dark blue section I assumed we needed to account for the info that’s in login view for Status/Partner. 
"	90	
200	6/17/26		New						Carly	Open	Cert Creation Page	When importing data for Product ID the Created said 'Invalid Date' and Created By said 'System'. Not sure why it would not list the day I imported and the user log for created by. 	91	
201	6/19/26		New						Gemma	Open	Partner View	When exporting the certification list to CSV, some of the "Cert Results" export as a percentage (i.e. 0.17) and others export as a full number (i.e. 100), when all results are percentages		
