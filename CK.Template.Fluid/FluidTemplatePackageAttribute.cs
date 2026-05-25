using CK.Setup;
using System.Runtime.CompilerServices;

namespace CK.Template.Fluid;

/// <summary>
/// Required attribute for <see cref="FluidTemplatePackage"/>.
/// <para>
/// Marks a <see cref="FluidTemplatePackage"/> specialization as the entry point
/// for Fluid template discovery in its containing assembly. At setup time the
/// engine resolves this attribute into
/// <c>CK.Template.Fluid.Engine.FluidTemplatePackageAttributeImpl</c> which
/// registers the package for <c>Res/Templates/*.liquid</c> discovery.
/// </para>
/// </summary>
/// <remarks>
/// Mirrors the <c>CK.TypeScript.TypeScriptPackageAttribute</c> pattern —
/// including the <see cref="CallerFilePathAttribute"/> trick so the engine can
/// resolve the <c>Res/</c> folder relative to the source file that declares
/// the package.
/// </remarks>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public class FluidTemplatePackageAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new <see cref="FluidTemplatePackageAttribute"/>.
    /// </summary>
    /// <param name="callerFilePath">
    /// Automatically populated by the Roslyn compiler. Used by the engine to
    /// resolve the <c>Res/Templates/</c> folder relative to the source file
    /// declaring the package.
    /// </param>
    public FluidTemplatePackageAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( "CK.Template.Fluid.Engine.FluidTemplatePackageAttributeImpl, CK.Template.Fluid.Engine" )
    {
        CallerFilePath = callerFilePath;
    }

    /// <summary>
    /// Initializes a new specialized <see cref="FluidTemplatePackageAttribute"/>.
    /// </summary>
    /// <param name="actualAttributeTypeAssemblyQualifiedName">
    /// Assembly Qualified Name of the delegated Impl class that replaces this
    /// attribute during setup.
    /// </param>
    /// <param name="finalCallerFilePath">
    /// Specialized subclasses must forward the <c>[CallerFilePath]</c>
    /// parameter from their own constructors.
    /// </param>
    protected FluidTemplatePackageAttribute( string actualAttributeTypeAssemblyQualifiedName, string? finalCallerFilePath )
        : base( actualAttributeTypeAssemblyQualifiedName )
    {
        CallerFilePath = finalCallerFilePath;
    }

    /// <summary>
    /// Gets the source file path of the decorated type, captured via the
    /// Roslyn <see cref="CallerFilePathAttribute"/>.
    /// </summary>
    public string? CallerFilePath { get; }
}
