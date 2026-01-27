-- Capitalize Status values in Certificates table
-- Run this script to ensure all status values have proper capitalization

UPDATE Certificates SET Status = 'Active' WHERE LOWER(Status) = 'active';
UPDATE Certificates SET Status = 'Draft' WHERE LOWER(Status) = 'draft';
UPDATE Certificates SET Status = 'Expired' WHERE LOWER(Status) = 'expired';
UPDATE Certificates SET Status = 'Pending' WHERE LOWER(Status) = 'pending';
UPDATE Certificates SET Status = 'Revoked' WHERE LOWER(Status) = 'revoked';
UPDATE Certificates SET Status = 'Valid' WHERE LOWER(Status) = 'valid';

-- Verify the update
SELECT Status, COUNT(*) as Count FROM Certificates GROUP BY Status;
