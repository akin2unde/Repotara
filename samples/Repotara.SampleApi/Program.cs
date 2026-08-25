using Repotara;
using Repotara.Providers;
using Repotara.Providers.Sql;
using Repotara.SampleApi.Models;
using Repotara.SampleApi.Tenancy;
using Repotara.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Wire Repotara using discrete Host/Port/DatabaseName fields from the
// "Repotara" configuration section -- no raw connection string needed.
builder.Services.AddRepotara(options =>
{
    var section = builder.Configuration.GetSection("Repotara");

    options.Provider = Enum.Parse<ProviderType>(section["Provider"]!);
    options.Options = Enum.Parse<SqlOption>(section["Options"] ?? "PostgreSql");
    options.Host = section["Host"]!;
    options.Port = int.Parse(section["Port"]!);
    options.DatabaseName = section["DatabaseName"]!;
    options.Username = section["Username"];
    options.Password = section["Password"];
    options.EnableMultiTenancy = bool.Parse(section["EnableMultiTenancy"] ?? "false");
    options.TenantColumn = section["TenantColumn"] ?? "TenantId";
    options.DefaultRowLimit = int.Parse(section["DefaultRowLimit"] ?? "10000");

    // --- Reportable class discovery ---------------------------------------
    // Any one of these three is enough on its own; they're all shown together
    // here to demonstrate that they combine freely in a single registration.

    // 1) Base class: Order and Customer both inherit DbModel, so this single
    //    line finds every [Reportable] class deriving from it. This is the
    //    pattern to reach for when most of your models share a common base
    //    (e.g. an EF Core entity base class).
    options.RegisterDerivedFrom<DbModel>();

    // 2) Whole assembly: scans every type in the given assembly for
    //    [Reportable], regardless of base type. Shown here for completeness --
    //    in this sample it would already find Order/Customer/Region on its
    //    own, since they all live in one assembly. It earns its keep in a
    //    real project where reportable models live in a separate class
    //    library from the Web API project itself.
    options.RegisterAssembly(typeof(Order).Assembly);

    // 3) One-off type: Region does not derive from DbModel, so it's
    //    registered individually instead of being swept up by RegisterDerivedFrom.
    options.RegisterType<Region>();
});

// Demo tenant context -- a real project would resolve this from the
// authenticated user's claims, not an HTTP header.
builder.Services.AddScoped<ITenantContext, DemoTenantContext>();

var app = builder.Build();

app.MapControllers();

app.Run();
