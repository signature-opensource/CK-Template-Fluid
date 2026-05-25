using CK.Setup;

namespace CK.Template.Fluid;

/// <summary>
/// Marks an <c>IPoco</c> interface as the data model for a named Fluid
/// template. At setup time the engine resolves this attribute into
/// <c>CK.Template.Fluid.Engine.FluidTemplateAttributeImpl</c> which binds the
/// template name to the decorated IPoco type for allow-list generation and
/// build-time validation.
/// </summary>
/// <example>
/// <code>
/// [FluidTemplate( "UserInvitation" )]
/// public interface IUserInvitationModel : IPoco
/// {
///     string FirstName { get; set; }
///     string Token { get; set; }
/// }
/// </code>
/// Resources named <c>&lt;name&gt;.&lt;view&gt;.&lt;culture&gt;.liquid</c> —
/// e.g. <c>UserInvitation.Body.fr.liquid</c>, <c>UserInvitation.Subject.en.liquid</c> —
/// are bound to the decorated IPoco.
/// </example>
/// <remarks>
/// Uses the CK <see cref="ContextBoundDelegationAttribute"/> pattern (mirrors
/// <c>TypeScriptFileAttribute</c>, <c>NgComponentAttribute</c>, etc.) so that
/// engine-side behavior lives in a separate assembly loaded only at setup
/// time.
/// </remarks>
[AttributeUsage( AttributeTargets.Interface, AllowMultiple = false, Inherited = false )]
public class FluidTemplateAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes the attribute with the template name.
    /// </summary>
    /// <param name="name">
    /// The base template name (e.g. <c>"UserInvitation"</c>). Resources named
    /// <c>&lt;name&gt;.&lt;view&gt;[.&lt;culture&gt;].liquid</c> — where
    /// <c>&lt;view&gt;</c> is free-form (e.g. <c>Subject</c>, <c>Body</c>) —
    /// are bound to the decorated IPoco.
    /// </param>
    public FluidTemplateAttribute( string name )
        : base( "CK.Template.Fluid.Engine.FluidTemplateAttributeImpl, CK.Template.Fluid.Engine" )
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new specialized <see cref="FluidTemplateAttribute"/>.
    /// </summary>
    /// <param name="actualAttributeTypeAssemblyQualifiedName">
    /// Assembly Qualified Name of the delegated Impl class that replaces this
    /// attribute during setup.
    /// </param>
    /// <param name="name">The template name.</param>
    protected FluidTemplateAttribute( string actualAttributeTypeAssemblyQualifiedName, string name )
        : base( actualAttributeTypeAssemblyQualifiedName )
    {
        Name = name;
    }

    /// <summary>
    /// Gets the template name.
    /// </summary>
    public string Name { get; }
}
