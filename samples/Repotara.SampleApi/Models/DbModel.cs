namespace Repotara.SampleApi.Models;

/// <summary>
/// A common base for reportable models, used to demonstrate
/// <c>RepotaraOptions.RegisterDerivedFrom&lt;DbModel&gt;()</c> -- every
/// [Reportable] class deriving from this is picked up automatically.
/// </summary>
public abstract class DbModel
{
    public int Id { get; set; }
}
