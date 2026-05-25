using CK.Core;
using CK.Template.Fluid.Engine;
using Fluid;
using System.Reflection;

namespace CK.Setup;

/// <summary>
/// Engine aspect that discovers <c>Res/Templates/*.liquid</c> resources across
/// all packages and validates them at build time. Parse errors fail the build
/// with file path + error location.
/// </summary>
/// <remarks>
/// <para>
/// Also implements <see cref="ICSCodeGenerator"/> — currently a no-op
/// participant in the CS code generation pass. Mirrors the shape of
/// <c>CK.TypeScript.Engine.TypeScriptAspect : IStObjEngineAspect, ICSCodeGeneratorWithFinalization</c>
/// so future work (<c>G0.cs</c> catalog emission, IPoco surface allow-list
/// generation) can grow into <see cref="Implement"/> and/or an upgraded
/// <c>ICSCodeGeneratorWithFinalization</c> without further structural change.
/// </para>
/// </remarks>
public sealed class FluidAspect : IStObjEngineAspect, ICSCodeGenerator
{
    readonly FluidAspectConfiguration _config;
    readonly FluidParser _parser = new();

    // Resource name markers that identify Fluid templates. Two formats coexist:
    //   - Standard .NET embedded resources:       "<ns>.Res.Templates.<file>"
    //   - CK.EmbeddedResources (culture-uplift):  "ck@Res/Templates/<file>"
    static readonly string[] ResourceMarkers = { ".Res.Templates.", "Res/Templates/" };
    const string TemplateExtension = ".liquid";

    /// <summary>
    /// Constructor invoked by the engine from the <see cref="FluidAspectConfiguration.AspectType"/> AQN.
    /// </summary>
    public FluidAspect( FluidAspectConfiguration config )
    {
        _config = config;
    }

    bool IStObjEngineAspect.Configure( IActivityMonitor monitor, IStObjEngineConfigureContext context )
    {
        monitor.Info( "FluidAspect configured." );
        return true;
    }

    bool IStObjEngineAspect.OnSkippedRun( IActivityMonitor monitor ) => true;

    bool IStObjEngineAspect.RunPreCode( IActivityMonitor monitor, IStObjEngineRunContext context )
    {
        using( monitor.OpenInfo( "Discovering and validating Fluid templates." ) )
        {
            var templates = CollectTemplateResources( monitor );
            int total = 0, failed = 0;
            foreach( var (assembly, resourceName, source) in templates )
            {
                ++total;
                if( !_parser.TryParse( source, out _, out var error ) )
                {
                    ++failed;
                    monitor.Error( $"Fluid parse error in {assembly.GetName().Name}!{resourceName}: {error}" );
                }
            }
            if( failed > 0 )
            {
                monitor.Error( $"{failed} of {total} Fluid template(s) failed to parse." );
                return false;
            }
            monitor.Info( $"{total} Fluid template(s) parsed successfully." );
        }
        return true;
    }

    bool IStObjEngineAspect.RunPostCode( IActivityMonitor monitor, IStObjEnginePostCodeRunContext context ) => true;

    bool IStObjEngineAspect.Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context ) => true;

    /// <summary>
    /// <see cref="ICSCodeGenerator"/> entry point — currently a no-op.
    /// </summary>
    /// <remarks>
    /// Placeholder for future G0.cs emission. At that point the aspect will
    /// enumerate <see cref="FluidTemplatePackageAttributeImpl"/> and
    /// <see cref="FluidTemplateAttributeImpl"/> instances and emit:
    /// (1) pre-parsed template registrations into the <c>FluidTemplateCatalog</c>
    /// (letting us drop the runtime scan in <c>FluidTemplateCatalog.StObjInitialize</c>),
    /// and (2) <c>MemberAccessStrategy.Register&lt;TGeneratedPoco&gt;()</c>
    /// calls so Fluid can switch from <c>UnsafeMemberAccessStrategy</c> to a
    /// strict allow-list.
    /// </remarks>
    CSCodeGenerationResult ICSCodeGenerator.Implement( IActivityMonitor monitor, ICSCodeGenerationContext codeGenContext )
    {
        return CSCodeGenerationResult.Success;
    }

    /// <summary>
    /// Walks loaded assemblies and yields (assembly, resourceName, sourceText)
    /// triples for every embedded resource under a <c>Res/Templates/</c> folder
    /// ending in <c>.liquid</c>.
    /// </summary>
    static IEnumerable<(Assembly Assembly, string ResourceName, string Source)> CollectTemplateResources( IActivityMonitor monitor )
    {
        foreach( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
        {
            if( assembly.IsDynamic ) continue;
            string[] names;
            try { names = assembly.GetManifestResourceNames(); }
            catch( Exception ex )
            {
                monitor.Warn( $"Could not enumerate resources of assembly '{assembly.GetName().Name}'.", ex );
                continue;
            }
            foreach( var name in names )
            {
                if( !name.EndsWith( TemplateExtension, StringComparison.OrdinalIgnoreCase ) ) continue;
                if( !IsInTemplatesFolder( name ) ) continue;

                using var stream = assembly.GetManifestResourceStream( name );
                if( stream == null ) continue;
                using var reader = new StreamReader( stream );
                var source = reader.ReadToEnd();
                yield return (assembly, name, source);
            }

            static bool IsInTemplatesFolder( string resourceName )
            {
                foreach( var marker in ResourceMarkers )
                {
                    if( resourceName.IndexOf( marker, StringComparison.OrdinalIgnoreCase ) >= 0 ) return true;
                }
                return false;
            }
        }
    }
}
