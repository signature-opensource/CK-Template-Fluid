namespace CK.Template.Fluid;

/// <summary>
/// Base class for Fluid template packages.
/// <para>
/// Types that specialize this must be decorated with
/// <see cref="FluidTemplatePackageAttribute"/> (or a specialization). The
/// <c>[FluidTemplatePackage]</c> attribute declares the type as a contributor
/// of Fluid templates located under the <c>Res/Templates/</c> folder of its
/// containing assembly.
/// </para>
/// </summary>
/// <remarks>
/// Mirrors the <c>CK.TypeScript.TypeScriptPackage</c> pattern. A consumer
/// package typically has exactly one subclass of <see cref="FluidTemplatePackage"/>,
/// acting as the marker that declares "this assembly ships Fluid templates".
/// </remarks>
public abstract class FluidTemplatePackage : IFluidTemplatePackage
{
    void IFluidTemplatePackage.LocalImplementationOnly() { }
}
