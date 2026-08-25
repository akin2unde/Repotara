# Repotara.SampleApi — Usage Guide

Every capability of the Repotara SDK, runnable against this sample API once
you point `appsettings.json` at a real database. Each example is available
two ways:

- `GET /api/reports/examples/{name}` — see the exact `ReportDefinition` JSON, without running it
- `POST /api/reports/examples/{name}/run?format=...` — actually run it

You can also post any custom definition to `POST /api/reports/run`.

## Setup

1. Point `appsettings.json` → `Repotara` section at a real SQL Server,
   PostgreSQL, MySQL, or MongoDB instance (matching `Provider`/`Options`).
2. Create `orders`, `customers`, `regions` tables/collections matching
   `Models/Order.cs`, `Models/Customer.cs`, `Models/Region.cs`.
3. `dotnet run` from `samples/Repotara.SampleApi`.

## Every example, by feature

| Name                | Feature demonstrated                                          |
|----------------------|-----------------------------------------------------------------|
| `basic`              | Single source, no joins, no aggregation                        |
| `joined`             | Inner join between two sources                                 |
| `left-join`          | Left join -- rows with no match on the right side are kept      |
| `grouped-aggregate`  | Three chained sources, GroupBy, Sum, Sort                       |
| `aggregate-showcase` | Sum, Avg, Min, Max, Count side by side on the same field        |
| `filter-and-or`      | Nested AND/OR filter tree                                       |
| `filter-in`          | The `IN` operation                                              |
| `filter-contains`    | The `CONTAINS` operation (case-insensitive partial match)       |
| `having`             | Aggregate-level filter (post-GROUP BY)                          |
| `sort-pagination`    | Multi-field sort, Skip/Take pagination                          |
| `concat`             | Computed field: FirstName + " " + LastName -> "Full Name"      |
| `relative-date`      | `THIS_MONTH` keyword resolved to a concrete date range          |
| `column-to-column`   | Comparing two columns on the same row (`valueProperty`)         |
| `html-template`      | Custom `{{DisplayName}}` HTML template instead of a plain table |
| `chart`              | `{ labels, datasets }` output for a charting library            |
| `invalid`            | Intentionally fails validation -- shows the structured 400 body |

## Try every output format

```bash
# JSON (default)
curl -X POST "https://localhost:5001/api/reports/examples/grouped-aggregate/run"

# XML
curl -X POST "https://localhost:5001/api/reports/examples/grouped-aggregate/run?format=Xml"

# HTML -- default table
curl -X POST "https://localhost:5001/api/reports/examples/grouped-aggregate/run?format=Html"

# HTML -- custom template ({{DisplayName}} substitution)
curl -X POST "https://localhost:5001/api/reports/examples/html-template/run?format=Html"

# Chart -- { labels, datasets } shape
curl -X POST "https://localhost:5001/api/reports/examples/chart/run?format=Chart"
```

## See the validation error response

```bash
curl -X POST "https://localhost:5001/api/reports/examples/invalid/run"
```
```json
{
  "error": "Invalid report definition.",
  "detail": "Order.Total: Aggregate 'Count' is not allowed on this field.; ..."
}
```

## Try multi-tenancy

`EnableMultiTenancy` is `true` in `appsettings.json`, scoped by `CompanyId`.
The sample's `DemoTenantContext` reads it from a header (a real project would
read it from the authenticated user's claims instead):

```bash
curl -X POST "https://localhost:5001/api/reports/examples/basic/run" \
  -H "X-Tenant-Id: 1"

curl -X POST "https://localhost:5001/api/reports/examples/basic/run" \
  -H "X-Tenant-Id: 2"
```
Same definition, different tenant header, different (non-overlapping) rows --
`Region` is unaffected since it's marked `IgnoreTenant = true` in
`Models/Region.cs`.

## Post a custom definition

```bash
curl -X POST "https://localhost:5001/api/reports/run?format=Json" \
  -H "Content-Type: application/json" \
  -d '{
    "sources": ["Order"],
    "fields": [
      { "field": "Order.Id", "displayName": "Order Number" },
      { "field": "Order.Total", "displayName": "Total" }
    ],
    "take": 10
  }'
```

## Switching database provider

Only `appsettings.json` changes -- no code changes needed. To switch from
PostgreSQL to MongoDB:

```json
"Repotara": {
  "Provider": "MongoDb",
  "Host": "localhost",
  "Port": 27017,
  "DatabaseName": "repotara_sample",
  "Username": "admin",
  "Password": "changeme"
}
```
`Options` (the SQL dialect) is ignored when `Provider` is `MongoDb`.
