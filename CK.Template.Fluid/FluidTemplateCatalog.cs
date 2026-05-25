using CK.Core;
using Fluid;
using System.Reflection;

namespace CK.Template.Fluid;

/// <summary>
/// Catalog of pre-parsed Fluid templates keyed by name and culture.
/// Populated at setup time from embedded <c>Res/Templates/*.liquid</c> resources
/// across all packages. Lookups honor CK-Globalization culture fallback.
/// </summary>
/// <remarks>
/// <para>
/// Template filename convention: <c>&lt;Name&gt;.&lt;culture&gt;.liquid</c> or
/// <c>&lt;Name&gt;.liquid</c> (cultureless — stored under <see cref="NormalizedCultureInfo.Invariant"/>).
/// </para>
/// <para>
/// The catalog is an <see cref="IRealObject"/>: one instance per StObj map.
/// Entries are added during the setup phase by generated code. Once setup
/// completes, the catalog is effectively read-only.
/// </para>
/// </remarks>
public class FluidTemplateCatalog : IRealObject
{
    // Resource name markers identifying template resources. Two formats coexist:
    //   - Standard .NET embedded resources use dot separators:
    //       "MyPackage.Res.Templates.Welcome.fr.liquid"
    //   - CK.EmbeddedResources uplifts culture-bearing files to its own format:
    //       "ck@Res/Templates/Welcome.fr.liquid"
    // We accept either.
    static readonly (string Marker, char Separator)[] ResourceMarkers =
    {
        ( ".Res.Templates.", '.' ),
        ( "Res/Templates/",  '/' ),
    };

    // name → (culture → compiled template). Inner dict is not concurrent because
    // writes happen during setup only.
    readonly Dictionary<string, Dictionary<NormalizedCultureInfo, IFluidTemplate>> _byName = new();

    /// <summary>
    /// Registers a pre-parsed template under the given name and culture.
    /// Called during setup. Subsequent registrations for the same key silently
    /// overwrite — this is how StObj override ordering (consumer packages win
    /// over producers) takes effect.
    /// </summary>
    public void Register( string name, NormalizedCultureInfo culture, IFluidTemplate template )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( name );
        Throw.CheckNotNullArgument( culture );
        Throw.CheckNotNullArgument( template );
        if( !_byName.TryGetValue( name, out var byCulture ) )
        {
            byCulture = new Dictionary<NormalizedCultureInfo, IFluidTemplate>();
            _byName[name] = byCulture;
        }
        byCulture[culture] = template;
    }

    /// <summary>
    /// Tries to resolve a template by name with CK-Globalization fallback chain.
    /// Tries the exact culture, then each fallback culture, then
    /// <see cref="NormalizedCultureInfo.Invariant"/> (cultureless default).
    /// </summary>
    /// <returns>The resolved template, or <c>null</c> if no registration matches.</returns>
    public IFluidTemplate? TryGet( string name, NormalizedCultureInfo culture )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( name );
        Throw.CheckNotNullArgument( culture );
        if( !_byName.TryGetValue( name, out var byCulture ) ) return null;

        if( byCulture.TryGetValue( culture, out var template ) ) return template;
        foreach( var fb in culture.Fallbacks )
        {
            if( byCulture.TryGetValue( fb, out template ) ) return template;
        }
        if( byCulture.TryGetValue( NormalizedCultureInfo.Invariant, out template ) ) return template;
        return null;
    }

    /// <summary>
    /// Gets whether any template is registered under the given name (any culture).
    /// </summary>
    public bool Contains( string name ) => _byName.ContainsKey( name );

    /// <summary>
    /// Gets all registered template names.
    /// </summary>
    public IEnumerable<string> Names => _byName.Keys;

    /// <summary>
    /// Walks every assembly, parses every <c>Res/Templates/*.liquid</c> resource
    /// and registers it. Throws <see cref="InvalidOperationException"/> on any
    /// parse error (the engine has already validated these at build time, so a
    /// runtime failure indicates assembly drift).
    /// </summary>
    /// <returns>The number of templates loaded.</returns>
    public int LoadFromAssemblies( IActivityMonitor monitor, IEnumerable<Assembly> assemblies )
    {
        var parser = new FluidParser();
        int count = 0;
        foreach( var assembly in assemblies )
        {
            if( assembly.IsDynamic ) continue;
            string[] names;
            try { names = assembly.GetManifestResourceNames(); }
            catch( Exception ex )
            {
                monitor.Warn( $"Could not enumerate resources of '{assembly.GetName().Name}'.", ex );
                continue;
            }
            foreach( var resourceName in names )
            {
                if( !TryExtractFileName( resourceName, out var fileName ) ) continue;
                var key = FluidTemplateResourceKey.TryParse( fileName );
                if( key == null ) continue;

                using var stream = assembly.GetManifestResourceStream( resourceName );
                if( stream == null ) continue;
                using var reader = new StreamReader( stream );
                var source = reader.ReadToEnd();

                if( !parser.TryParse( source, out var template, out var error ) )
                {
                    Throw.InvalidOperationException(
                        $"Failed to parse Fluid template '{resourceName}' in '{assembly.GetName().Name}': {error}" );
                }
                Register( key.Value.Name, key.Value.Culture, template );
                ++count;
            }
        }
        return count;
    }

    /// <summary>
    /// Extracts the logical filename (portion after the <c>Res/Templates/</c>
    /// folder) from an embedded resource name. Handles both the standard
    /// .NET format (<c>&lt;ns&gt;.Res.Templates.&lt;file&gt;</c>) and
    /// CK.EmbeddedResources format (<c>ck@Res/Templates/&lt;file&gt;</c>).
    /// </summary>
    static bool TryExtractFileName( string resourceName, out string fileName )
    {
        foreach( var (marker, _) in ResourceMarkers )
        {
            var idx = resourceName.IndexOf( marker, StringComparison.OrdinalIgnoreCase );
            if( idx < 0 ) continue;
            fileName = resourceName[(idx + marker.Length)..];
            return true;
        }
        fileName = string.Empty;
        return false;
    }

    /// <summary>
    /// StObj lifecycle hook — populates the catalog from all assemblies
    /// loaded in the current <see cref="AppDomain"/>. Invoked by the CK
    /// engine once all real objects have been instantiated.
    /// </summary>
    void StObjInitialize( IActivityMonitor monitor, IStObjObjectMap map )
    {
        using( monitor.OpenInfo( "Loading Fluid templates from embedded resources." ) )
        {
            var count = LoadFromAssemblies( monitor, AppDomain.CurrentDomain.GetAssemblies() );
            monitor.Info( $"Loaded {count} Fluid template(s) under {_byName.Count} distinct name(s)." );
        }
    }
}
