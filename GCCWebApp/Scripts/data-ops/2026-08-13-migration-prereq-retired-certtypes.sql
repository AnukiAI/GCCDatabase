/*==============================================================================
  MIGRATION PREREQ P1 — create the 4 retired certification types (deactivated)
  ------------------------------------------------------------------------------
  Client question (ReadMe.txt): "There will be some certification types listed
  that we don't have in our active list because we don't support anymore.
  Confirm we can still have these added but not in our dropdown options for
  selection?"  -> Yes: CertificationType.IsActive exists for exactly this.

  WHY THIS IS AN INSERT AND NOT A FLAG-FLIP
  Plan §11.3 originally claimed all four already existed, so the work was just
  setting IsActive = 0. That was measured on the LOCAL DEV db (34 types).
  Re-measured on the test environment (32 types): NONE of the four exist, and
  every existing type is IsActive = 1. So they must be CREATED, deactivated.
  See [[feedback_diagnose_on_clients_environment]] — same class as DEF-323.

  WHY IT MATTERS: CertificateImportService.ProcessRowAsync THROWS on an
  unmatched certification type — it does not auto-create. Without this, the 83
  Expired-List-1 rows using these types FAIL the import outright.

  Level 1 / ParentId NULL matches every other top-level type. CertificationTypes
  is hierarchical and the same Name legitimately appears at multiple Levels
  ([[feedback_certtypes_hierarchy]]), so the existence check is on Name+Level,
  not Name alone.

  Idempotent — safe to re-run.

      sqlcmd -S <srv> -d <db> -I -i <thisfile>
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Retired TABLE (Name nvarchar(200) PRIMARY KEY, SourceRows int);
INSERT INTO @Retired (Name, SourceRows) VALUES
    (N'Biodegradable',                       56),
    (N'Biobased',                            16),
    (N'Renewable Resource Content',           8),
    (N'Verified Health Product Declaration',  3);

PRINT '--- before ---';
SELECT r.Name, r.SourceRows,
       CASE WHEN ct.Id IS NULL THEN 'MISSING' ELSE 'exists (Id ' + CAST(ct.Id AS varchar(10)) + ')' END AS State
FROM   @Retired r
LEFT JOIN dbo.CertificationTypes ct ON ct.Name = r.Name AND ct.Level = 1
ORDER BY r.SourceRows DESC;

BEGIN TRANSACTION;

    /* SortOrder is NOT NULL with no default, so it must be supplied explicitly.
       Append after the current max rather than interleaving — these are retired
       types and should sort last wherever an ordered list is rendered. */
    DECLARE @NextSort int = (SELECT ISNULL(MAX(SortOrder), 0) FROM dbo.CertificationTypes);

    INSERT INTO dbo.CertificationTypes (Name, [Level], ParentId, IsActive, SortOrder)
    SELECT r.Name, 1, NULL, 0,
           @NextSort + ROW_NUMBER() OVER (ORDER BY r.SourceRows DESC, r.Name)
    FROM   @Retired r
    WHERE  NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes ct
                       WHERE ct.Name = r.Name AND ct.[Level] = 1);

    PRINT 'inserted: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' retired type(s), IsActive = 0';

    /* Assert: all four resolvable, and none of them selectable in the dropdown. */
    DECLARE @missing int, @active int;
    SELECT @missing = COUNT(*) FROM @Retired r
     WHERE NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes ct WHERE ct.Name = r.Name AND ct.[Level] = 1);
    SELECT @active = COUNT(*) FROM dbo.CertificationTypes ct
     JOIN @Retired r ON r.Name = ct.Name AND ct.[Level] = 1 WHERE ct.IsActive = 1;

    IF @missing > 0 OR @active > 0
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('ROLLED BACK: %d retired type(s) still missing, %d still active.', 16, 1, @missing, @active);
        RETURN;
    END;

COMMIT TRANSACTION;

PRINT '--- after ---';
SELECT ct.Id, ct.Name, ct.[Level], ct.IsActive
FROM   dbo.CertificationTypes ct JOIN @Retired r ON r.Name = ct.Name
WHERE  ct.[Level] = 1
ORDER BY ct.Name;

SELECT COUNT(*) AS TotalCertTypes,
       SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS Deactivated
FROM   dbo.CertificationTypes;

/*  DEF-343 WARNING — verify before letting an analyst open one of these certs.
    A filtered Edit <select> renders BLANK when the stored value sits outside the
    filter, and a required field then silently rewrites on save. An IsActive = 0
    type on an expired cert is precisely that scenario: opening one of the 83
    certs could quietly overwrite its certification type.
    Confirm the DEF-343 fix covers the certification-type dropdown.
    See [[feedback_dropdown_filtered_subset_drops_stored_value]].                */
