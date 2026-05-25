using CK.Core;
using Fluid;

namespace CK.Template.Fluid;

/// <summary>
/// Default <see cref="IFluidTemplateService"/> implementation backed by a shared
/// <see cref="FluidParser"/>, a shared <see cref="TemplateOptions"/>, and the
/// <see cref="FluidTemplateCatalog"/>.
/// </summary>
public sealed class FluidTemplateService : IFluidTemplateService
{
    static readonly FluidParser _parser = new();

    // Incubation default: permissive member access + camelCase template-to-CLR
    // name mapping. Templates write "{{ firstName }}" and reach PascalCase C#
    // properties (FirstName) — matches both the legacy tMCResHtml "{firstName}"
    // convention and the IPoco-generated surface used from Phase 2 onwards.
    // The engine will switch this to a strict allow-list in a later phase.
    static readonly TemplateOptions _options = new()
    {
        MemberAccessStrategy = new UnsafeMemberAccessStrategy
        {
            MemberNameStrategy = MemberNameStrategies.CamelCase
        }
    };

    readonly FluidTemplateCatalog _catalog;

    public FluidTemplateService( FluidTemplateCatalog catalog )
    {
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async ValueTask<string> RenderInlineAsync( string source, object? model, CancellationToken cancel = default )
    {
        Throw.CheckNotNullArgument( source );
        if( !_parser.TryParse( source, out var template, out var error ) )
        {
            Throw.ArgumentException( nameof( source ), $"Fluid parse error: {error}" );
        }
        cancel.ThrowIfCancellationRequested();
        var context = CreateContext( model, null );
        return await template.RenderAsync( context );
    }

    /// <inheritdoc />
    public ValueTask<string> RenderAsync( string name, NormalizedCultureInfo culture, object? model, CancellationToken cancel = default )
        => RenderAsync( name, culture, model, null, cancel );

    /// <inheritdoc />
    public async ValueTask<string> RenderAsync( string name,
                                                NormalizedCultureInfo culture,
                                                object? model,
                                                IReadOnlyDictionary<string, object>? ambient,
                                                CancellationToken cancel = default )
    {
        var template = _catalog.TryGet( name, culture );
        if( template == null )
        {
            Throw.InvalidOperationException( $"No Fluid template registered under name '{name}' (requested culture '{culture.Name}'). Known templates: {string.Join( ", ", _catalog.Names )}" );
        }
        cancel.ThrowIfCancellationRequested();
        var context = CreateContext( model, ambient );
        return await template.RenderAsync( context );
    }

    // Fluid's TemplateContext(model, options) rejects a null model; default to an
    // empty object so "cultureless static text" templates still render. Ambient
    // bindings, if any, are set after construction via SetValue.
    static TemplateContext CreateContext( object? model, IReadOnlyDictionary<string, object>? ambient )
    {
        var context = new TemplateContext( model ?? new object(), _options );
        if( ambient != null )
        {
            foreach( var (key, value) in ambient )
            {
                context.SetValue( key, value );
            }
        }
        return context;
    }
}
