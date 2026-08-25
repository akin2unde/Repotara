using Repotara.Tenancy;

namespace Repotara.SampleApi.Tenancy;

/// <summary>
/// Minimal demo implementation of <see cref="ITenantContext"/>. A real project
/// would read the tenant ID from the authenticated user's claims (JWT, cookie,
/// etc.) instead of a raw header -- this sample keeps auth out of scope and
/// just reads "X-Tenant-Id" so the multi-tenancy example is runnable as-is.
/// </summary>
public class DemoTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DemoTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantId
    {
        get
        {
            var header = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            return string.IsNullOrWhiteSpace(header) ? "1" : header;
        }
    }
}
