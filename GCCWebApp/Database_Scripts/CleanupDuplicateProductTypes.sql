-- Cleanup duplicate ProductTypes before applying unique constraint
-- This script keeps the first entry (lowest ID) and deletes subsequent duplicates

-- First, let's see what duplicates exist
SELECT Name, ParentId, COUNT(*) as DuplicateCount
FROM ProductTypes
GROUP BY Name, ParentId
HAVING COUNT(*) > 1;

-- Delete duplicates, keeping the first one (lowest ID)
WITH CTE AS (
    SELECT Id, Name, ParentId,
           ROW_NUMBER() OVER (PARTITION BY Name, ParentId ORDER BY Id) AS RowNum
    FROM ProductTypes
)
DELETE FROM CTE WHERE RowNum > 1;

PRINT 'Duplicate ProductTypes cleaned up';
