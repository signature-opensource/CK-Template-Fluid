using CK.Core;
using CK.Setup;
using Fluid;
using System.Reflection;

namespace CK.Template.Fluid.Engine;

/// <summary>
/// Engine-side Impl for <see cref="FluidTemplatePackageAttribute"/>. Created
/// by the StObj engine — one instance per decorated
/// <see cref="FluidTemplatePackage"/> specialization — via the
/// <see cref="ContextBoundDelegationAttribute"/> AQN wiring.
/// </summary>
/// <remarks>
/// Mirrors <c>CK.TypeScript.Engine.TypeScriptGroupOrPackageAttributeImpl</c>.
/// Current responsibilities:
/// <list type="bullet">
///   <item>Validate that the decorated type derives from <see cref="FluidTemplatePackage"/>.</item>
///   <item>
///   Parse the <c>Res/Templates/*.liquid</c> resources of the package's assembly: a parse error fails the build (with the
///   resource and the error location). Nothing has to be configured: an application that uses a template package
///   checks its templates.
///   </item>
/// </list>
/// Future responsibilities: emit the catalog population code into <c>G0.cs</c> (pre-parsed templates instead of the run
/// time scan of <c>FluidTemplateCatalog</c>).
/// </remarks>
public class FluidTemplatePackageAttributeImpl : IAttributeContextBoundInitializer
{
    // Resource name markers that identify Fluid templates. Two formats coexist:
    //   - Standard .NET embedded resources:       "<ns>.Res.Templates.<file>"
    //   - CK.EmbeddedResources (culture-uplift):  "ck@Res/Templates/<file>"
    static readonly string[] _resourceMarkers = { ".Res.Templates.", "Res/Templates/" };
    const string _templateExtension = ".liquid";

    readonly FluidTemplatePackageAttribute _attr;
    readonly Type _type;

    /// <summary>
    /// Engine constructor called reflectively via the
    /// <see cref="ContextBoundDelegationAttribute"/> protocol.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="attr">The runtime attribute instance.</param>
    /// <param name="type">The decorated <see cref="FluidTemplatePackage"/> specialization.</param>
    public FluidTemplatePackageAttributeImpl( IActivityMonitor monitor, FluidTemplatePackageAttribute attr, Type type )
    {
        _attr = attr;
        _type = type;
        if( !typeof( FluidTemplatePackage ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[FluidTemplatePackage] can only decorate a {nameof( FluidTemplatePackage )} specialization: '{type.FullName}' does not derive from {nameof( FluidTemplatePackage )}." );
        }
        else
        {
            CheckTemplates( monitor, type.Assembly );
        }
    }

    /// <summary>Gets the runtime attribute instance.</summary>
    public FluidTemplatePackageAttribute Attribute => _attr;

    /// <summary>Gets the decorated type.</summary>
    public Type DecoratedType => _type;

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
    }

    // Parses the Res/Templates/*.liquid resources of the assembly: an error is logged (it fails the build).
    static void CheckTemplates( IActivityMonitor monitor, Assembly assembly )
    {
        var parser = new FluidParser();
        int total = 0, failed = 0;
        foreach( var name in assembly.GetManifestResourceNames() )
        {
            if( !name.EndsWith( _templateExtension, StringComparison.OrdinalIgnoreCase ) || !IsInTemplatesFolder( name ) ) continue;
            using var stream = assembly.GetManifestResourceStream( name );
            if( stream == null ) continue;
            using var reader = new StreamReader( stream );
            ++total;
            if( !parser.TryParse( reader.ReadToEnd(), out _, out var error ) )
            {
                ++failed;
                monitor.Error( $"Fluid parse error in {assembly.GetName().Name}!{name}: {error}" );
            }
        }
        if( failed == 0 ) monitor.Trace( $"{total} Fluid template(s) of '{assembly.GetName().Name}' parsed successfully." );

        static bool IsInTemplatesFolder( string resourceName )
        {
            foreach( var marker in _resourceMarkers )
            {
                if( resourceName.Contains( marker, StringComparison.OrdinalIgnoreCase ) ) return true;
            }
            return false;
        }
    }
}
