-- Schema for the DataProvider Customer entity and its stored procedures.
-- Run this against the DataProviderDb database (see docker-compose.yml).
IF DB_ID('DataProviderDb') IS NULL
    CREATE DATABASE DataProviderDb;


GO
USE DataProviderDb;


GO
IF OBJECT_ID('dbo.Customer', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Customer (
            Id              INT            IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Customer PRIMARY KEY,
            Name            NVARCHAR (200) NOT NULL,
            Email           NVARCHAR (320) NOT NULL,
            ConsumtionGroup NVARCHAR (100) NULL,
            PaymentData     NVARCHAR (MAX) NULL,
            Status          NVARCHAR (50)  NULL
        );
        CREATE INDEX IX_Customer_Email
            ON dbo.Customer(Email);
    END


GO
-- Idempotent add for databases created before Status existed.
IF COL_LENGTH('dbo.Customer', 'Status') IS NULL
    ALTER TABLE dbo.Customer
        ADD Status NVARCHAR (50) NULL;


GO
CREATE OR ALTER PROCEDURE dbo.usp_GetCustomers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT   Id,
             Name,
             Email,
             ConsumtionGroup,
             PaymentData
    FROM     dbo.Customer
    ORDER BY Id;
END


GO
CREATE OR ALTER PROCEDURE dbo.usp_GetCustomerById
@Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,
           Name,
           Email,
           ConsumtionGroup,
           PaymentData
    FROM   dbo.Customer
    WHERE  Id = @Id;
END


GO
CREATE OR ALTER PROCEDURE dbo.usp_CreateCustomer
@Name NVARCHAR (200), @Email NVARCHAR (320), @ConsumtionGroup NVARCHAR (100)=NULL, @PaymentData NVARCHAR (MAX)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO dbo.Customer (
        Name,
        Email,
        ConsumtionGroup,
        PaymentData
    )
    VALUES                    (@Name, @Email, @ConsumtionGroup, @PaymentData);
    SELECT CAST (SCOPE_IDENTITY() AS INT);
END


GO
-- Returns customers "grouped" with two policies:
--   HARD (Name, Email) : exact value (SQL NULLs group together).
--   SOFT (ConsumtionGroup, PaymentData, Status) : null/empty is a wildcard that matches
--     any value; distinct non-null values form separate groups and the non-null value is
--     picked for output. A wildcard row fans out into every compatible group, so it can
--     be counted in more than one MemberCount. Id is ignored entirely.
-- @Status is an optional filter: when supplied, only rows with that Status are considered.
-- Set-based (no cursors): the base table is collapsed to its distinct (Name,Email,CG,PD,ST)
-- combinations ONCE into #Distinct (single scan, carrying the row count). All option /
-- candidate / compatibility work then runs on that compact set, and MemberCount is the
-- summed row count. Per (Name,Email) partition, each soft column's option set is its
-- distinct non-null values (or a single NULL when none exist); candidate groups are the
-- cross product, and an INNER JOIN on compatibility prunes impossible combinations.
CREATE OR ALTER PROCEDURE dbo.CustomersGrouped
@Offset INT, @PageSize INT, @Status NVARCHAR (50)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    CREATE TABLE #Distinct (
        Name  NVARCHAR (200) NOT NULL,
        Email NVARCHAR (320) NOT NULL,
        CG    NVARCHAR (100) NULL,
        PD    NVARCHAR (MAX) NULL,
        ST    NVARCHAR (50)  NULL,
        Cnt   BIGINT         NOT NULL
    );
    INSERT INTO #Distinct (
        Name,
        Email,
        CG,
        PD,
        ST,
        Cnt
    )
    SELECT   Name,
             Email,
             NULLIF (LTRIM(RTRIM(ConsumtionGroup)), N''),
             NULLIF (LTRIM(RTRIM(PaymentData)), N''),
             NULLIF (LTRIM(RTRIM(Status)), N''),
             COUNT_BIG(*)
    FROM     dbo.Customer
    WHERE    @Status IS NULL
             OR Status = @Status
    GROUP BY Name, Email, NULLIF (LTRIM(RTRIM(ConsumtionGroup)), N''), NULLIF (LTRIM(RTRIM(PaymentData)), N''), NULLIF (LTRIM(RTRIM(Status)), N'');
    CREATE NONCLUSTERED INDEX IX_Distinct
        ON #Distinct(Name, Email);
    WITH     Partitions
    AS       (SELECT DISTINCT Name,
                              Email
              FROM   #Distinct),
             CgOptions
    AS       (SELECT DISTINCT Name,
                              Email,
                              CG AS Value
              FROM   #Distinct
              WHERE  CG IS NOT NULL
              UNION ALL
              SELECT p.Name,
                     p.Email,
                     CAST (NULL AS NVARCHAR (100))
              FROM   Partitions AS p
              WHERE  NOT EXISTS (SELECT 1
                                 FROM   #Distinct AS d
                                 WHERE  d.Name = p.Name
                                        AND d.Email = p.Email
                                        AND d.CG IS NOT NULL)),
             PdOptions
    AS       (SELECT DISTINCT Name,
                              Email,
                              PD AS Value
              FROM   #Distinct
              WHERE  PD IS NOT NULL
              UNION ALL
              SELECT p.Name,
                     p.Email,
                     CAST (NULL AS NVARCHAR (MAX))
              FROM   Partitions AS p
              WHERE  NOT EXISTS (SELECT 1
                                 FROM   #Distinct AS d
                                 WHERE  d.Name = p.Name
                                        AND d.Email = p.Email
                                        AND d.PD IS NOT NULL)),
             StOptions
    AS       (SELECT DISTINCT Name,
                              Email,
                              ST AS Value
              FROM   #Distinct
              WHERE  ST IS NOT NULL
              UNION ALL
              SELECT p.Name,
                     p.Email,
                     CAST (NULL AS NVARCHAR (50))
              FROM   Partitions AS p
              WHERE  NOT EXISTS (SELECT 1
                                 FROM   #Distinct AS d
                                 WHERE  d.Name = p.Name
                                        AND d.Email = p.Email
                                        AND d.ST IS NOT NULL)),
             Candidates
    AS       (SELECT cg.Name,
                     cg.Email,
                     cg.Value AS Cg,
                     pd.Value AS Pd,
                     st.Value AS St
              FROM   CgOptions AS cg
                     INNER JOIN
                     PdOptions AS pd
                     ON pd.Name = cg.Name
                        AND pd.Email = cg.Email
                     INNER JOIN
                     StOptions AS st
                     ON st.Name = cg.Name
                        AND st.Email = cg.Email),
             Grouped
    AS       (SELECT   c.Name,
                       c.Email,
                       c.Cg AS ConsumtionGroup,
                       c.Pd AS PaymentData,
                       c.St AS Status,
                       SUM(d.Cnt) AS MemberCount
              FROM     Candidates AS c
                       INNER JOIN
                       #Distinct AS d
                       ON d.Name = c.Name
                          AND d.Email = c.Email
                          AND (d.CG IS NULL
                               OR d.CG = c.Cg)
                          AND (d.PD IS NULL
                               OR d.PD = c.Pd)
                          AND (d.ST IS NULL
                               OR d.ST = c.St)
              GROUP BY c.Name, c.Email, c.Cg, c.Pd, c.St)
    SELECT   Name,
             Email,
             Status,
             ConsumtionGroup,
             PaymentData,
             MemberCount
    FROM     Grouped
    ORDER BY Name, Email, ConsumtionGroup, PaymentData, Status
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
    DROP TABLE #Distinct;
END


GO
-- ============================================================================
-- Materialized-cache prototype for the grouped result.
-- Trades per-call recomputation for a precomputed table paginated by a
-- sequential RefId (keyset pagination). Read cost becomes O(PageSize).
-- ============================================================================
IF OBJECT_ID('dbo.CustomerGroupCache', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CustomerGroupCache (
            -- RefId is the pagination order: the refresh proc assigns it (via IDENTITY
            -- + ORDER BY) in the same order CustomersGrouped sorts by, so keyset paging
            -- on RefId returns groups in that order.
            RefId           INT            IDENTITY (1, 1) NOT NULL CONSTRAINT PK_CustomerGroupCache PRIMARY KEY CLUSTERED,
            Name            NVARCHAR (200) NOT NULL,
            Email           NVARCHAR (320) NOT NULL,
            Status          NVARCHAR (50)  NULL,
            ConsumtionGroup NVARCHAR (100) NULL,
            PaymentData     NVARCHAR (MAX) NULL,
            MemberCount     BIGINT         NOT NULL
        );
        -- Supports status-filtered keyset paging (seek on Status, then RefId).
        CREATE NONCLUSTERED INDEX IX_CustomerGroupCache_Status
            ON dbo.CustomerGroupCache(Status, RefId);
    END


GO
-- Recomputes the whole grouped set (same rules as dbo.CustomersGrouped, no filter)
-- and repopulates the cache. RefId is assigned in pagination order.
CREATE OR ALTER PROCEDURE dbo.CustomerGroupCache_Refresh
AS
BEGIN
    SET NOCOUNT ON;
    CREATE TABLE #Distinct (
        Name  NVARCHAR (200) NOT NULL,
        Email NVARCHAR (320) NOT NULL,
        CG    NVARCHAR (100) NULL,
        PD    NVARCHAR (MAX) NULL,
        ST    NVARCHAR (50)  NULL,
        Cnt   BIGINT         NOT NULL
    );
    INSERT INTO #Distinct (
        Name,
        Email,
        CG,
        PD,
        ST,
        Cnt
    )
    SELECT   Name,
             Email,
             NULLIF (LTRIM(RTRIM(ConsumtionGroup)), N''),
             NULLIF (LTRIM(RTRIM(PaymentData)), N''),
             NULLIF (LTRIM(RTRIM(Status)), N''),
             COUNT_BIG(*)
    FROM     dbo.Customer
    GROUP BY Name, Email, NULLIF (LTRIM(RTRIM(ConsumtionGroup)), N''), NULLIF (LTRIM(RTRIM(PaymentData)), N''), NULLIF (LTRIM(RTRIM(Status)), N'');
    CREATE NONCLUSTERED INDEX IX_Distinct
        ON #Distinct(Name, Email);
    BEGIN TRANSACTION;
    -- Prototype swap: TRUNCATE + reinsert. Blocks readers briefly and resets
    -- RefId. For production prefer building a staging table and swapping it in.
    TRUNCATE TABLE dbo.CustomerGroupCache;
    WITH Partitions
    AS   (SELECT DISTINCT Name,
                          Email
          FROM   #Distinct),
         CgOptions
    AS   (SELECT DISTINCT Name,
                          Email,
                          CG AS Value
          FROM   #Distinct
          WHERE  CG IS NOT NULL
          UNION ALL
          SELECT p.Name,
                 p.Email,
                 CAST (NULL AS NVARCHAR (100))
          FROM   Partitions AS p
          WHERE  NOT EXISTS (SELECT 1
                             FROM   #Distinct AS d
                             WHERE  d.Name = p.Name
                                    AND d.Email = p.Email
                                    AND d.CG IS NOT NULL)),
         PdOptions
    AS   (SELECT DISTINCT Name,
                          Email,
                          PD AS Value
          FROM   #Distinct
          WHERE  PD IS NOT NULL
          UNION ALL
          SELECT p.Name,
                 p.Email,
                 CAST (NULL AS NVARCHAR (MAX))
          FROM   Partitions AS p
          WHERE  NOT EXISTS (SELECT 1
                             FROM   #Distinct AS d
                             WHERE  d.Name = p.Name
                                    AND d.Email = p.Email
                                    AND d.PD IS NOT NULL)),
         StOptions
    AS   (SELECT DISTINCT Name,
                          Email,
                          ST AS Value
          FROM   #Distinct
          WHERE  ST IS NOT NULL
          UNION ALL
          SELECT p.Name,
                 p.Email,
                 CAST (NULL AS NVARCHAR (50))
          FROM   Partitions AS p
          WHERE  NOT EXISTS (SELECT 1
                             FROM   #Distinct AS d
                             WHERE  d.Name = p.Name
                                    AND d.Email = p.Email
                                    AND d.ST IS NOT NULL)),
         Candidates
    AS   (SELECT cg.Name,
                 cg.Email,
                 cg.Value AS Cg,
                 pd.Value AS Pd,
                 st.Value AS St
          FROM   CgOptions AS cg
                 INNER JOIN
                 PdOptions AS pd
                 ON pd.Name = cg.Name
                    AND pd.Email = cg.Email
                 INNER JOIN
                 StOptions AS st
                 ON st.Name = cg.Name
                    AND st.Email = cg.Email),
         Grouped
    AS   (SELECT   c.Name,
                   c.Email,
                   c.St AS Status,
                   c.Cg AS ConsumtionGroup,
                   c.Pd AS PaymentData,
                   SUM(d.Cnt) AS MemberCount
          FROM     Candidates AS c
                   INNER JOIN
                   #Distinct AS d
                   ON d.Name = c.Name
                      AND d.Email = c.Email
                      AND (d.CG IS NULL
                           OR d.CG = c.Cg)
                      AND (d.PD IS NULL
                           OR d.PD = c.Pd)
                      AND (d.ST IS NULL
                           OR d.ST = c.St)
          GROUP BY c.Name, c.Email, c.St, c.Cg, c.Pd)
    INSERT INTO dbo.CustomerGroupCache (
        Name,
        Email,
        Status,
        ConsumtionGroup,
        PaymentData,
        MemberCount
    )
    SELECT   Name,
             Email,
             Status,
             ConsumtionGroup,
             PaymentData,
             MemberCount
    FROM     Grouped
    ORDER BY Name, Email, ConsumtionGroup, PaymentData, Status;
    COMMIT TRANSACTION;
    DROP TABLE #Distinct;
END


GO
-- Keyset (seek) pagination over the cache. Pass the last RefId of the previous
-- page as @AfterRefId (0 for the first page). O(PageSize) via the clustered index.
CREATE OR ALTER PROCEDURE dbo.CustomerGroupCache_Page
@AfterRefId INT=0, @PageSize INT=50, @Status NVARCHAR (50)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT   TOP (@PageSize) RefId,
                             Name,
                             Email,
                             Status,
                             ConsumtionGroup,
                             PaymentData,
                             MemberCount
    FROM     dbo.CustomerGroupCache
    WHERE    RefId > @AfterRefId
             AND (@Status IS NULL
                  OR Status = @Status)
    ORDER BY RefId;
END