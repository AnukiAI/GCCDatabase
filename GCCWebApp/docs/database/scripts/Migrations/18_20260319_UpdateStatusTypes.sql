-- Migration: Update Certificate Status Types
-- Date: 2026-03-19
-- Description: Migrate from 14-state status system to 8 client-defined statuses
--
-- New statuses:
--   Draft, Client Approved - Not Paid, Client Approved - Confidential,
--   Client Approved - Paid, Hold, Expired, Republished, Expired - No Annual Verification
--
-- IMPORTANT: Review mappings before running in production.
--            Adjust mappings based on actual data in your environment.

BEGIN TRANSACTION;

-- Map old statuses to new statuses
-- Active/active -> Client Approved - Paid (these were publicly visible)
UPDATE Certificates SET Status = 'Client Approved - Paid'
WHERE Status IN ('Active', 'active', 'Renewed', 'renewal_in_progress', 'expiring_soon');

-- Approved -> Client Approved - Not Paid (approved but not yet paid/published)
UPDATE Certificates SET Status = 'Client Approved - Not Paid'
WHERE Status IN ('Approved');

-- PendingReview, UnderReview -> Draft (still in progress)
UPDATE Certificates SET Status = 'Draft'
WHERE Status IN ('PendingReview', 'UnderReview', 'PendingRenewal');

-- Suspended, OnHold -> Hold
UPDATE Certificates SET Status = 'Hold'
WHERE Status IN ('Suspended', 'OnHold');

-- Expired stays Expired
UPDATE Certificates SET Status = 'Expired'
WHERE Status IN ('Expired', 'expired', 'Revoked');

-- Cancelled, Archived -> Expired - No Annual Verification
UPDATE Certificates SET Status = 'Expired - No Annual Verification'
WHERE Status IN ('Cancelled', 'Archived');

-- Rejected -> Draft (can be reworked)
UPDATE Certificates SET Status = 'Draft'
WHERE Status IN ('Rejected');

-- Verify: Show counts by new status
SELECT Status, COUNT(*) AS Count
FROM Certificates
GROUP BY Status
ORDER BY Status;

COMMIT;
