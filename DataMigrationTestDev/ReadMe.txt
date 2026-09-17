Attached please find the latest certification and active ASIN lists. This should be uploaded to the DB – this is REAL data for migration. All test certs/ASINs should be removed before uploading as we are preparing for go-live. Please confirm receipt of this email.

We will need to do another upload with differences from now until that timeframe, but this will be the majority. 

GCC Database Migration_Certs_8.12.26
-	Active List 7.31.26 tab
o	Blue columns are new data fields we added to align with new DB
o	The Product Cat/Type/Subcat 
	Should NOT pull through the numbers in front of name (this was used to help with grouping selection in sheet)
•	E.g., 4.3 Cleaning Supplies  the 4.3 does not get loaded into new DB
	N/A is an option that is available and should show (this would be for certs that are not product-level)
o	On the active and expired lists there are in some cases multiple listings with the same cert number – can they be uploaded this way since they already exist 
-	Expired List 2 – 8.11.26 tab
o	Most recent expired list. This group did have new DB fields added
o	This should be uploaded for historical reference
-	Expired List 1 – 4.9.26 tab
o	Original expired list. This group DOES NOT have fields aligning with new DB
	New DB fields not listed in tab should be left blank we did not assign 
o	This should be uploaded for historical reference
o	There will be some certification types listed that we don’t have in our active list because we don’t support anymore. Confirm we can still have these added but not in our dropdown options for selection?
	Biobased, Biodegradable, Renewable Resource Content, Verified Health Product Declaration

GCC ASIN Master List_8.12.26
-	Includes all active ASINs. These should be mapped and added to the appropriate cert numbers
o	NOTE: Company names may be different than cert listing, it should follow cert listing based on #
Header Color Legend
Light Blue = in prior ASIN sheet, will carryover to new DB
Darker Blue = new DB field (or field options)
Green = field for new DB on backend 
issuer = GreenCircle Certified
status = Active
certificationCountries = All
(leave PDF blank – Anuki to create)


Make sure for all the Active and Expired you are pulling associated cert PDFs from current DB for cert listing. Per our prior conversation you were going to write a script to pull and map these from old DB to new.

Please confirm when this upload has been completed and ready for review. 

