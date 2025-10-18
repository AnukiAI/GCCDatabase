-- Check if tables have data
SELECT COUNT(*) AS CustomFieldsCount FROM CustomFields;
SELECT COUNT(*) AS TagsCount FROM Tags;
SELECT COUNT(*) AS CSFAttributesCount FROM Tags WHERE TagType = 'csf';
