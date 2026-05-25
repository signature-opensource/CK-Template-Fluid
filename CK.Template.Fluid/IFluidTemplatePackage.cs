namespace CK.Template.Fluid;

/// <summary>
/// Category interface for <see cref="FluidTemplatePackage"/> — marks types
/// that own a <c>Res/Templates/*.liquid</c> resource set in their containing
/// assembly.
/// </summary>
/// <remarks>
/// Mirrors the <c>CK.TypeScript.ITypeScriptPackage</c> convention: the
/// <c>LocalImplementationOnly</c> internal method prevents external
/// implementations of this interface — consumers must inherit the abstract
/// <see cref="FluidTemplatePackage"/> base class.
/// </remarks>
public interface IFluidTemplatePackage
{
    /// <summary>
    /// Prevents third-party implementations of this interface. See remarks.
    /// </summary>
    internal void LocalImplementationOnly();
}
