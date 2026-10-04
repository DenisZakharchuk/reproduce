# groupproc-gen

Zero-dependency Node.js generator for the "soft/hard grouping" stored procedures
used in [`db/schema.sql`](../../db/schema.sql). Give it a JSON data model and it
renders the OFFSET/FETCH grouping proc and the materialized-cache objects (table +
refresh proc + keyset page proc).

## Usage

```bash
node generate.js models/customer.model.json                 # both procs -> stdout
node generate.js models/customer.model.json --emit offset   # only the OFFSET proc
node generate.js models/customer.model.json --emit cache    # only the cache objects
node generate.js models/customer.model.json --out out.sql   # write to a file
```

Requires Node.js only (no `npm install`).

## Model

```jsonc
{
  "sourceTable": "dbo.Customer",
  "procName": "dbo.CustomersGrouped",
  "columns": [
    { "name": "Name",  "type": "NVARCHAR(200)", "grouping": "hard", "nullable": false },
    { "name": "Status","type": "NVARCHAR(50)",  "grouping": "soft", "filter": true },
    { "name": "Id",    "type": "INT",           "grouping": "ignore" }
  ],
  "output":  ["Name", "Status"],               // result column order (default: grouping cols)
  "orderBy": ["Name", "Status"],               // ORDER BY (default: grouping cols)
  "cache": {
    "table":       "dbo.CustomerGroupCache",
    "refreshProc": "dbo.CustomerGroupCache_Refresh",
    "pageProc":    "dbo.CustomerGroupCache_Page"
  }
}
```

### Column roles

| `grouping` | Behavior |
|------------|----------|
| `hard`     | Exact match; part of the partition key. Set `nullable: true` for null-safe matching (`IS NOT DISTINCT FROM`, SQL Server 2022+). |
| `soft`     | Null/empty is a wildcard; distinct non-null values split into separate groups; the non-null value is picked for output. Normalized with `NULLIF(LTRIM(RTRIM(x)), N'')` unless `normalize: false`. |
| `ignore`   | Omitted entirely (e.g. identity/`Id`). |

`filter: true` adds an optional `@<col>` parameter. In the OFFSET proc it filters
the base rows **before** grouping; in the cache page proc it filters the cached
rows **after** grouping (semantically different — see `schema.sql` notes).

## Notes

- `MemberCount` is `SUM` of source rows per group; a wildcard row compatible with
  several groups is counted in each (same trade-off as the hand-written procs).
- Validation errors (no hard column, duplicate name, bad `grouping`, missing cache
  config) print a message and exit non-zero.
- Output targets functional equivalence with the hand-written procs, not
  byte-identical formatting.
