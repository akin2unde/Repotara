using Microsoft.Extensions.DependencyInjection;
using Repotara.Output;
using Repotara.Providers;
using Repotara.Providers.Mongo;
using Repotara.Providers.Sql;

namespace Repotara;

/// <summary>
/// Registers Repotara with dependency injection. Call
/// <c>services.AddRepotara(options => ...)</c> once at startup.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ReportEngine"/>, the configured
    /// <see cref="IReportDataProvider"/> (Sql or MongoDb per
    /// <see cref="RepotaraOptions.Provider"/>), and all built-in output writers.
    /// </summary>
    public static IServiceCollection AddRepotara(this IServiceCollection services, Action<RepotaraOptions> configure)
    {
        services.Configure(configure);

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RepotaraOptions>>().Value;
            return ReportableTypeRegistry.Build(options);
        });

        services.AddSingleton<IReportDataProvider>(serviceProvider =>
        {
            var optionsAccessor = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RepotaraOptions>>();

            if (optionsAccessor.Value.Provider == ProviderType.Sql)
            {
                return new SqlReportProvider(optionsAccessor);
            }

            if (optionsAccessor.Value.Provider == ProviderType.MongoDb)
            {
                return new MongoReportProvider(optionsAccessor);
            }

            throw new NotSupportedException("Unsupported provider: " + optionsAccessor.Value.Provider);
        });

        services.AddSingleton<IReportWriter, JsonReportWriter>();
        services.AddSingleton<IReportWriter, XmlReportWriter>();
        services.AddSingleton<IReportWriter, HtmlReportWriter>();
        services.AddSingleton<IReportWriter, ChartReportWriter>();

        services.AddScoped<ReportEngine>();

        return services;
    }
}
