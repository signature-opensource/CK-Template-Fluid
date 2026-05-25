using CK.Core;
using System.Text.RegularExpressions;

namespace CK.Template.Fluid;

/// <summary>
/// Parses a <c>Res/Templates/&lt;Name&gt;[.culture].liquid</c> filename
/// into a <see cref="Name"/> / <see cref="Culture"/> pair.
/// </summary>
/// <remarks>
/// <para>
/// The <c>&lt;Name&gt;</c> part is free-form and may contain dots (e.g.
/// <c>"UserInvitation.Body"</c>). The optional culture suffix is detected by
/// matching the last dot-segment (before <c>.liquid</c>) against a simple
/// IETF-language-tag pattern (<c>fr</c>, <c>fr-FR</c>, <c>zh-Hans</c>, …).
/// </para>
/// </remarks>
public readonly record struct FluidTemplateResourceKey( string Name, NormalizedCultureInfo Culture )
{
    const string Extension = ".liquid";

    // Simple 2-letter language + optional script/region tag. Loose enough to
    // cover common tags, strict enough not to accidentally capture template
    // name suffixes like "Body" (too long / wrong casing).
    static readonly Regex CultureTagRegex = new( @"^[a-z]{2}(-[A-Za-z]{2,4})?$", RegexOptions.Compiled );

    /// <summary>
    /// Parses a logical filename (e.g. <c>"UserInvitation.Body.fr.liquid"</c>) into
    /// a key. Returns <c>null</c> if <paramref name="fileName"/> does not end in
    /// <c>.liquid</c> or the base name is empty.
    /// </summary>
    public static FluidTemplateResourceKey? TryParse( string fileName )
    {
        if( !fileName.EndsWith( Extension, StringComparison.OrdinalIgnoreCase ) ) return null;

        var stripped = fileName[..^Extension.Length];
        if( stripped.Length == 0 ) return null;

        var lastDot = stripped.LastIndexOf( '.' );
        if( lastDot > 0 )
        {
            var tail = stripped[(lastDot + 1)..];
            if( CultureTagRegex.IsMatch( tail ) )
            {
                return new FluidTemplateResourceKey( stripped[..lastDot], NormalizedCultureInfo.EnsureNormalizedCultureInfo( tail ) );
            }
        }
        return new FluidTemplateResourceKey( stripped, NormalizedCultureInfo.Invariant );
    }
}
