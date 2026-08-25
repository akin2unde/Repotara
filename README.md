# Repotara

A provider-agnostic, attribute-driven reporting SDK for .NET 10 Web APIs.

Mark your domain classes as reportable, wire up a database once at startup, and
let any frontend send a JSON **ReportDefinition** describing exactly what
report to run — which fields, joins, filters, grouping, aggregates, and sort
order. Repotara validates it against your class metadata, translates it into a
native SQL query or MongoDB aggregation pipeline, and returns JSON, XML, HTML,
or chart-ready data.

The frontend never needs to know your database schema. Your backend never
needs to hardcode report shapes.

## Why Repotara

- **Attribute opt-out, not opt-in.** Every public property on a `[Reportable]`
  class is reportable by default. Mark the few you want to hide with
  `[ReportIgnore]`.
- **The frontend designs the report.** A `ReportDefinition` — fields, joins,
  filters, grouping, sort, pagination — is sent as plain JSON per request. No
  backend redeploy needed to change what a report shows.
- **Runs where your data lives.** Filters, joins, and aggregation are pushed
  down to the database (SQL Server, PostgreSQL, MySQL, or MongoDB) instead of
  pulling everything into memory first.
- **Multi-tenant by convention.** Turn on one option, name your tenant column
  once, and every query is automatically scoped — the frontend can never see,
  set, or bypass it.
- **Safe by construction.** Every filter value is a bound query parameter,
  never string-concatenated into SQL.

## Install

```bash
dotnet add package Repotara
```

## Quick start

### 1. Mark your classes

```csharp
using Repotara.Attributes;
using Repotara.Aggregation;

[Reportable(Source = "orders")]
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    [ReportField(DisplayName = "Order Total", Column = "order_total",
                 AllowedAggregates = [AggregateType.Sum, AggregateType.Avg])]
    public decimal Total { get; set; }

    public DateTime PlacedOn { get; set; }

    [ReportIgnore]
    public string? InternalNotes { get; set; }
}
```

`Source` is the physical table/collection name. `[ReportField]` is optional —
only needed to customize the display name, the physical column name, or
restrict which aggregates are allowed.

### 2. Register Repotara at startup

```csharp
using Repotara;
using Repotara.Providers;
using Repotara.Providers.Sql;

builder.Services.AddRepotara(options =>
{
    options.Provider = ProviderType.Sql;
    options.Options = SqlOption.PostgreSql;
    options.Host = "localhost";
    options.Port = 5432;
    options.DatabaseName = "app_db";
    options.Username = "app_user";
    options.Password = builder.Configuration["Db:Password"];

    // Register your [Reportable] classes once, here -- ReportEngine resolves
    // whatever a ReportDefinition's "sources" list needs from this registry,
    // so you never pass a type list on every call. Use any combination:
    options.RegisterDerivedFrom<DbModel>();        // everything deriving from a common base
    options.RegisterAssembly(typeof(Order).Assembly); // everything [Reportable] in an assembly
    options.RegisterType<Region>();                // one specific class
});
```

Connection strings are built internally from these discrete fields — you
never write provider-specific connection string syntax.

### 3. Accept a ReportDefinition and run it

```csharp
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportEngine _reportEngine;

    public ReportsController(ReportEngine reportEngine) => _reportEngine = reportEngine;

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] ReportDefinition definition, [FromQuery] OutputFormat format = OutputFormat.Json)
    {
        var result = await _reportEngine.ExecuteAsync(definition, format);
        return Content(result.Content, result.ContentType);
    }
}
```

### 4. The frontend sends a definition

```json
POST /api/reports/run?format=Json
{
  "sources": ["Order"],
  "fields": [
    { "field": "Order.Id", "displayName": "Order Number" },
    { "field": "Order.Total", "displayName": "Total" }
  ],
  "take": 50
}
```

```json
[
  { "Order Number": 1, "Total": 249.99 },
  { "Order Number": 2, "Total": 89.50 }
]
```

See `samples/Repotara.SampleApi` for a full runnable project covering every
feature below, and `GET /api/reports/examples/{name}` in that project for the
exact JSON shape of each one.

## ReportDefinition reference

| Property   | Type                     | Purpose                                                          |
|------------|--------------------------|-------------------------------------------------------------------|
| `sources`  | `string[]`               | Reportable source names involved, e.g. `["Order", "Customer"]`   |
| `joins`    | `JoinDefinition[]`       | Chains sources together (see below)                              |
| `fields`   | `ReportFieldSelection[]` | Fields to return, in output order                                 |
| `filter`   | `SearchParam`            | Row-level filter, applied before aggregation                      |
| `having`   | `SearchParam`            | Aggregate-level filter, applied after grouping                    |
| `groupBy`  | `string[]`               | "Source.Property" paths to group by                               |
| `sort`     | `SortField[]`            | Sort instructions, applied in order                                |
| `skip`     | `int?`                   | Rows to skip (pagination)                                          |
| `take`     | `int?`                   | Max rows to return; falls back to `DefaultRowLimit` if omitted     |
| `template` | `string?`                | HTML template with `{{DisplayName}}` tags (HTML output only)      |

### Field selection

```json
{ "field": "Order.Total", "displayName": "Revenue", "aggregate": "Sum" }
```

`displayName` overrides the attribute's `DisplayName`. Precedence:
**selection's `displayName`** > **`[ReportField(DisplayName=...)]`** > raw
property name.

### Computed fields (Concat)

```json
{
  "displayName": "Full Name",
  "concat": { "fields": ["Customer.FirstName", "Customer.LastName"], "delimiter": " " }
}
```

Joins two or more fields with a single delimiter. Does not support mixing in
literal text beyond the delimiter, and cannot be combined with `aggregate`.

### Joins

```json
{ "left": "Order", "leftKey": "CustomerId", "right": "Customer", "rightKey": "Id", "type": "Left" }
```

Chain any number of joins to connect any number of sources. `type` is
`"Inner"` (default) or `"Left"`.

### Filters and Having

`filter`/`having` are a recursive tree of `SearchParam` nodes:

```json
{
  "operator": "And",
  "conditions": [
    { "property": "Order.PlacedOn", "operation": "GTE", "value": "2026-01-01" },
    { "operator": "Or", "conditions": [
      { "property": "Customer.Name", "operation": "EQ", "value": "Acme" },
      { "property": "Customer.Name", "operation": "EQ", "value": "Globex" }
    ]}
  ]
}
```

**Operations:** `EQ`, `NEQ`, `GT`, `GTE`, `LT`, `LTE`, `IN`, `CONTAINS`

**Comparing two columns instead of a literal:**
```json
{ "property": "Order.ShippedDate", "operation": "GT", "valueProperty": "Order.PromisedDate" }
```

**Relative date values** (for `value`, on date/time properties) resolve to a
full calendar range, not an instant:

| Keyword       | Range                              |
|---------------|-------------------------------------|
| `TODAY`       | Start of today → start of tomorrow  |
| `YESTERDAY`   | Start of yesterday → start of today |
| `THIS_WEEK`   | Start of this week → next week      |
| `LAST_WEEK`   | Start of last week → this week      |
| `THIS_MONTH`  | Start of this month → next month    |
| `LAST_MONTH`  | Start of last month → this month    |

```json
{ "property": "Order.PlacedOn", "operation": "EQ", "value": "THIS_MONTH" }
```

`having` conditions reference output `displayName`s instead of
`Source.Property` paths, since an aggregated value doesn't belong to a single
source.

### Aggregates

`Sum`, `Avg`, `Count`, `Min`, `Max` — every one translates directly to a
native SQL aggregate function and MongoDB accumulator, so nothing runs
in-memory for these. A field with `[ReportField(AllowedAggregates = [...])]`
restricts which of these can be requested against it; an empty array (the
default) allows any of them.

## Registering reportable classes

`ReportEngine.ExecuteAsync` never takes a list of types — it resolves
whatever a `ReportDefinition`'s `sources` list needs from a registry built
once at startup. Three registration methods, freely combinable:

```csharp
options.RegisterDerivedFrom<DbModel>();           // every [Reportable] class deriving from DbModel
options.RegisterAssembly(typeof(Order).Assembly); // every [Reportable] class in that assembly
options.RegisterType<Region>();                   // one specific class
```

Use whichever fits your codebase — most projects reach for `RegisterDerivedFrom`
if their models share a common base (e.g. an EF Core entity base class),
`RegisterAssembly` if reportable models live together in one class library
without a shared base, and `RegisterType` for the occasional one-off
exception. All three can be called together in a single `AddRepotara` block;
registering the same class more than once is harmless.

## Multi-tenancy

```csharp
builder.Services.AddRepotara(options =>
{
    // ...connection settings
    options.EnableMultiTenancy = true;
    options.TenantColumn = "CompanyId"; // applied to every Reportable class
});

builder.Services.AddScoped<ITenantContext, YourTenantContext>();
```

```csharp
public class YourTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _accessor;
    public YourTenantContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public string TenantId => _accessor.HttpContext!.User.FindFirst("tenant_id")!.Value;
}
```

Repotara injects a mandatory `TenantColumn = TenantId` condition into every
query for every joined source that has that column — merged server-side,
before validation or query building, so the frontend's `ReportDefinition` can
never see, set, or remove it.

Classes with no tenant column (shared/global lookup data) opt out explicitly:

```csharp
[Reportable(Source = "regions", IgnoreTenant = true)]
public class Region { ... }
```

## Output formats

Pass `OutputFormat` to `ReportEngine.ExecuteAsync`:

- **`Json`** — array of row objects
- **`Xml`** — `<Report><Row>...</Row></Report>`
- **`Html`** — a plain table by default, or your own template via
  `definition.Template` using `{{DisplayName}}` substitution tags
- **`Chart`** — `{ "labels": [...], "datasets": [{ "label": ..., "data": [...] }] }`,
  directly usable by Chart.js, Recharts, and similar libraries. Requires at
  least one `groupBy` field and one aggregate field.

## SQL Server, PostgreSQL, MySQL, or MongoDB

A project is assumed to use exactly one database throughout, configured once:

```csharp
options.Provider = ProviderType.Sql;      // or ProviderType.MongoDb
options.Options = SqlOption.PostgreSql;   // SqlServer / PostgreSql / MySql
```

`[Reportable(Source = "...")]` names the table (SQL) or collection (MongoDB).
`[ReportField(Column = "...")]` names the physical column/field if it differs
from the C# property name.

## Performance notes

- Type reflection happens once per class, ever — results are cached and
  reused via compiled, reflection-free property accessors.
- Filters, joins, and aggregation run in the database wherever possible; the
  in-memory fallback engines exist only for edge cases a provider can't
  express natively.
- Every `ReportDefinition` gets a row limit (`DefaultRowLimit`, default
  10,000) if it doesn't specify its own `take`, so no report runs unbounded
  by accident.

## Project structure

```
src/Repotara/              the NuGet package
samples/Repotara.SampleApi the full runnable demo
tests/Repotara.Tests       unit tests for the pure-logic components
```

## License

MIT
