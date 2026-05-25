using CK.Core;
using CK.Setup;
using System.Reflection;

namespace CK.Template.Fluid.Engine;

/// <summary>
/// Engine-side Impl for <see cref="FluidTemplateAttribute"/>. Created by the
/// StObj engine — one instance per decorated <c>IPoco</c> interface — via the
/// <see cref="ContextBoundDelegationAttribute"/> AQN wiring.
/// </summary>
/// <remarks>
/// Current responsibilities:
/// <list type="bullet">
///   <item>Validate the decorated type is an interface that extends <c>IPoco</c>.</item>
/// </list>
/// Future responsibilities (when the allow-list / strict-mode work from
/// Phase 1 lands):
/// <list type="bullet">
///   <item>Register the template-name → IPoco-type binding.</item>
///   <item>Emit <c>TemplateOptions.MemberAccessStrategy.Register&lt;TGeneratedPoco&gt;()</c> calls into <c>G0.cs</c>.</item>
///   <item>Fail the build if a matching <c>.liquid</c> template references a member absent from the composed IPoco surface.</item>
/// </list>
/// </remarks>
public class FluidTemplateAttributeImpl : IAttributeContextBoundInitializer
{
    readonly FluidTemplateAttribute _attr;
    readonly Type _type;

    /// <summary>
    /// Engine constructor called reflectively via the
    /// <see cref="ContextBoundDelegationAttribute"/> protocol.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="attr">The runtime attribute instance.</param>
    /// <param name="type">The decorated IPoco interface.</param>
    public FluidTemplateAttributeImpl( IActivityMonitor monitor, FluidTemplateAttribute attr, Type type )
    {
        _attr = attr;
        _type = type;
        if( !type.IsInterface )
        {
            monitor.Error( $"[FluidTemplate(\"{attr.Name}\")] can only decorate an interface: '{type.FullName}' is not an interface." );
        }
        else if( !typeof( IPoco ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[FluidTemplate(\"{attr.Name}\")] can only decorate an IPoco interface: '{type.FullName}' does not derive from IPoco." );
        }
    }

    /// <summary>Gets the runtime attribute instance.</summary>
    public FluidTemplateAttribute Attribute => _attr;

    /// <summary>Gets the decorated IPoco interface.</summary>
    public Type DecoratedType => _type;

    /// <summary>Gets the bound template name.</summary>
    public string TemplateName => _attr.Name;

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        // No-op for now. Future work: register (TemplateName, Type) into a
        // central FluidTemplateContext for allow-list generation + build-time
        // member reference validation.
    }
}
