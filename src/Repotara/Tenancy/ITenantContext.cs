namespace Repotara.Tenancy;

/// <summary>
/// Supplies the current authenticated user's tenant identifier. Implemented by
/// the consuming Web API project (typically by reading a claim from the current
/// HTTP context), never by Repotara itself. Only used when
/// <c>RepotaraOptions.EnableMultiTenancy</c> is true.
/// </summary>
public interface ITenantContext
{
    /// <summary>The tenant/company identifier for the current request.</summary>
    string TenantId { get; }
}
