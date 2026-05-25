using CK.Core;
using CK.Setup;
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
/// </list>
/// Future responsibilities (when <see cref="FluidAspect"/> grows into
/// <c>ICSCodeGenerator</c> G0 emission):
/// <list type="bullet">
///   <item>Register the owning assembly for <c>Res/Templates/*.liquid</c> discovery.</item>
///   <item>Emit catalog population code into <c>G0.cs</c>.</item>
/// </list>
/// </remarks>
public class FluidTemplatePackageAttributeImpl : IAttributeContextBoundInitializer
{
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
    }

    /// <summary>Gets the runtime attribute instance.</summary>
    public FluidTemplatePackageAttribute Attribute => _attr;

    /// <summary>Gets the decorated type.</summary>
    public Type DecoratedType => _type;

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        // No-op for now. Future work: register the owning assembly / caller-file
        // folder into a FluidTemplateContext so the aspect can walk it during
        // G0.cs emission (parallel to the TypeScript context pattern).
    }
}
