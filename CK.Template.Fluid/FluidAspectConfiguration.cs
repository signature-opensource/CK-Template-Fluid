using System.Xml.Linq;

namespace CK.Setup;

/// <summary>
/// Configuration for the Fluid templating engine aspect. Register in
/// <c>CKSetup.xml</c> to enable build-time discovery and validation of
/// <c>Res/Templates/*.liquid</c> resources across all packages.
/// </summary>
/// <example>
/// <code>
/// &lt;Aspect Type="CK.Setup.FluidAspectConfiguration, CK.Template.Fluid" /&gt;
/// </code>
/// </example>
public sealed class FluidAspectConfiguration : EngineAspectConfiguration
{
    /// <summary>
    /// Initializes a default configuration.
    /// </summary>
    public FluidAspectConfiguration()
    {
    }

    /// <summary>
    /// Deserialization constructor.
    /// </summary>
    public FluidAspectConfiguration( XElement e )
        : base( e )
    {
    }

    /// <inheritdoc />
    public override string AspectType => "CK.Setup.FluidAspect, CK.Template.Fluid.Engine";

    /// <inheritdoc />
    public override XElement SerializeXml( XElement e ) => e;
}
