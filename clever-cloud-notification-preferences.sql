-- ============================================================
--  EduNexis - add NotificationPreferences
--  Run once against the Clever Cloud database.
--
--  This replaces the EF-generated script, which wraps everything
--  in DELIMITER // ... stored procedures. Web SQL consoles
--  frequently mangle DELIMITER, so this does the same work as
--  plain statements.
--
--  Safe to run twice: every statement is guarded.
-- ============================================================

-- Clever Cloud's SQL console runs at server level, so nothing is
-- selected by default -- that is the "#1046 No database selected"
-- error. Confirm the name below matches your database, or select
-- the database in the sidebar first and delete this line.
USE `bby7x82je3v1lvwtdsod`;


-- 1. EF's migration ledger. Already exists on a live database;
--    created here only so the script also works on a fresh one.
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId`    varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32)  CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;


-- 2. The new table.
--    The unique index is declared inline rather than as a separate
--    CREATE INDEX: MySQL has no "CREATE INDEX IF NOT EXISTS", so a
--    separate statement would fail on a re-run.
CREATE TABLE IF NOT EXISTS `NotificationPreferences` (
    `Id`         char(36)     COLLATE ascii_general_ci NOT NULL,
    `UserId`     char(36)     COLLATE ascii_general_ci NOT NULL,
    `Type`       varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `InApp`      tinyint(1)   NOT NULL,
    `Email`      tinyint(1)   NOT NULL,
    `CreatedAt`  datetime(6)  NOT NULL,
    `UpdatedAt`  datetime(6)  NULL,
    `IsDeleted`  tinyint(1)   NOT NULL,
    `DeletedAt`  datetime(6)  NULL,
    CONSTRAINT `PK_NotificationPreferences` PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_NotificationPreferences_UserId_Type` (`UserId`, `Type`),
    CONSTRAINT `FK_NotificationPreferences_Users_UserId`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


-- 3. Record the migration so EF does not try to apply it again.
--    INSERT IGNORE skips the row if it is already there.
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260816004739_AddNotificationPreferences', '9.0.3');


-- ============================================================
--  Check it worked.
--
--  Both checks read information_schema only. An earlier version
--  UNION'd information_schema.TABLES with __EFMigrationsHistory,
--  and phpMyAdmin ran that whole statement with information_schema
--  as the current database -- so the unqualified table name
--  resolved there and failed with:
--      #1109 Unknown table '__EFMIGRATIONSHISTORY' in information_schema
--  Never mix information_schema and a normal table in one query here.
-- ============================================================
SELECT
    (SELECT COUNT(*) FROM information_schema.TABLES
      WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'NotificationPreferences')      AS table_created,
    (SELECT COUNT(*) FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'NotificationPreferences'
        AND INDEX_NAME = 'IX_NotificationPreferences_UserId_Type') AS unique_index,
    (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE
      WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'NotificationPreferences'
        AND CONSTRAINT_NAME = 'FK_NotificationPreferences_Users_UserId') AS foreign_key;
-- Expect: table_created = 1, unique_index = 2 (one row per indexed
-- column), foreign_key = 1.


-- Then run this one on its own to confirm the migration was recorded.
-- Kept separate on purpose -- see the note above.
SELECT COUNT(*) AS migration_recorded
FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20260816004739_AddNotificationPreferences';
-- Expect: 1


-- No data migration is needed. A user with no row here gets every
-- notification, so existing accounts keep working unchanged.
