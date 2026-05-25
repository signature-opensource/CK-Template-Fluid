using CK.Core;

namespace CK.Template.Fluid;

/// <summary>
/// Renders Fluid templates. Templates are either supplied inline
/// (<see cref="RenderInlineAsync"/>) or resolved by name from the
/// <see cref="FluidTemplateCatalog"/> (culture-aware).
/// </summary>
public interface IFluidTemplateService : ISingletonAutoService
{
    /// <summary>
    /// Parses and renders a Fluid source string on the fly.
    /// Useful for ad-hoc templates that are not shipped as embedded resources.
    /// </summary>
    /// <param name="source">The Fluid template source.</param>
    /// <param name="model">The data model exposed to the template (may be <c>null</c>).</param>
    /// <param name="cancel">Cancellation token.</param>
    ValueTask<string> RenderInlineAsync( string source, object? model, CancellationToken cancel = default );

    /// <summary>
    /// Resolves a template from the <see cref="FluidTemplateCatalog"/> and renders it.
    /// Applies CK-Globalization culture fallback.
    /// Throws <see cref="InvalidOperationException"/> if no template matches the name.
    /// </summary>
    /// <param name="name">Template name (e.g. <c>"UserInvitation.Body"</c>).</param>
    /// <param name="culture">Requested culture; fallback chain is applied.</param>
    /// <param name="model">The data model exposed to the template.</param>
    /// <param name="cancel">Cancellation token.</param>
    ValueTask<string> RenderAsync( string name, NormalizedCultureInfo culture, object? model, CancellationToken cancel = default );

    /// <summary>
    /// Same as <see cref="RenderAsync(string, NormalizedCultureInfo, object?, CancellationToken)"/>
    /// but also sets ambient variables on the template context. Ambient bindings
    /// let the template access cross-cutting values (e.g. <c>{{ branding.* }}</c>)
    /// alongside the primary <paramref name="model"/> without having to merge
    /// them into a single envelope object per render.
    /// </summary>
    /// <param name="name">Template name.</param>
    /// <param name="culture">Requested culture; fallback chain is applied.</param>
    /// <param name="model">The primary data model.</param>
    /// <param name="ambient">Additional <c>name → value</c> bindings set on the context (may be <c>null</c>).</param>
    /// <param name="cancel">Cancellation token.</param>
    ValueTask<string> RenderAsync( string name,
                                   NormalizedCultureInfo culture,
                                   object? model,
                                   IReadOnlyDictionary<string, object>? ambient,
                                   CancellationToken cancel = default );
}
