using CK.Core;
using CK.Setup;
using CK.Template.Fluid;
using CK.Testing;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Template.Fluid.Tests.Engine;

[TestFixture]
public class AttributeValidationTests
{
    // --- Negative fixtures ---------------------------------------------

    // NOTE: A [FluidTemplate("...")] fixture on a *class* cannot be written
    // because FluidTemplateAttribute has AttributeTargets.Interface — the C#
    // compiler rejects the decoration before the StObj engine ever runs.
    // The `if( !type.IsInterface )` branch in FluidTemplateAttributeImpl is
    // therefore unreachable in practice (guarded at compile time). The
    // "class" test case has been omitted; this was surfaced to the author.

    public interface IPlainInterface
    {
    }

    [FluidTemplate( "BadOnPlainInterface" )]
    public interface IFluidTemplateOnNonPocoFixture : IPlainInterface
    {
    }

    [FluidTemplatePackage]
    public sealed class FluidTemplatePackageOnWrongBaseFixture
    {
        // INTENTIONALLY does NOT derive from FluidTemplatePackage.
    }

    // --- Tests --------------------------------------------------------

    [Test]
    public async Task FluidTemplate_on_a_non_IPoco_interface_fails_the_build()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.EnsureAspect<FluidAspectConfiguration>();
        engineConfig.FirstBinPath.Types.Add( typeof( IFluidTemplateOnNonPocoFixture ) );

        await engineConfig.GetFailedAutomaticServicesAsync( "does not derive from IPoco" );
    }

    [Test]
    public async Task FluidTemplatePackage_on_a_class_that_does_not_derive_from_FluidTemplatePackage_fails_the_build()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.EnsureAspect<FluidAspectConfiguration>();
        engineConfig.FirstBinPath.Types.Add( typeof( FluidTemplatePackageOnWrongBaseFixture ) );

        await engineConfig.GetFailedAutomaticServicesAsync(
            "does not derive from FluidTemplatePackage" );
    }
}
