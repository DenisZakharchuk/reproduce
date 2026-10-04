-- Seed data for dbo.Customer (~100 rows).
-- Name and Email are always present but intentionally NOT unique (drawn from
-- small pools so values repeat). ConsumtionGroup, PaymentData and Status are
-- sometimes NULL. Idempotent-ish: clears existing rows first so re-running is safe.
USE DataProviderDb;


GO
DELETE dbo.Customer;


GO
WITH Numbers
AS   (SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
      FROM   sys.all_objects),
     NamePool
AS   (SELECT idx,
             val
      FROM   (VALUES (0, N'John Smith'), (1, N'Jane Doe'), (2, N'Carlos Ruiz'), (3, N'Mia Wong'), (4, N'Liam Murphy'), (5, N'Olivia Brown'), (6, N'Noah Kim'), (7, N'Emma Garcia'), (8, N'Lucas Rossi'), (9, N'Ava Nguyen')) AS v(idx, val)),
     EmailPool
AS   (SELECT idx,
             val
      FROM   (VALUES (0, N'john@example.com'), (1, N'jane@example.com'), (2, N'carlos@example.com'), (3, N'mia@example.com'), (4, N'contact@example.com'), (5, N'info@example.com'), (6, N'support@example.com')) AS v(idx, val)),
     GroupPool
AS   (SELECT idx,
             val
      FROM   (VALUES (0, N'Residential'), (1, N'Commercial'), (2, N'Industrial'), (3, N'Agricultural')) AS v(idx, val)),
     StatusPool
AS   (SELECT idx,
             val
      FROM   (VALUES (0, N'Active'), (1, N'Inactive'), (2, N'Pending')) AS v(idx, val))
INSERT INTO dbo.Customer (
    Name,
    Email,
    ConsumtionGroup,
    PaymentData,
    Status
)
SELECT np.val,
       ep.val,
       CASE WHEN nu.n % 3 = 0 THEN NULL ELSE gp.val END,
       CASE WHEN nu.n % 4 = 1 THEN NULL ELSE CONCAT(N'IBAN', RIGHT(N'0000000000' + CAST (nu.n * 7919 % 1000000000 AS NVARCHAR (10)), 10)) END,
       CASE WHEN nu.n % 5 = 0 THEN NULL ELSE sp.val END
FROM   Numbers AS nu
       INNER JOIN
       NamePool AS np
       ON np.idx = nu.n % 10
       INNER JOIN
       EmailPool AS ep
       ON ep.idx = nu.n % 7
       INNER JOIN
       GroupPool AS gp
       ON gp.idx = nu.n % 4
       INNER JOIN
       StatusPool AS sp
       ON sp.idx = nu.n % 3;