using CK.Setup;
using CK.Template.Fluid.Tests.Engine.Fixtures;
using CK.Testing;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Template.Fluid.Tests.Engine;

[TestFixture]
public class FluidAspectTests
{
    [Test]
    public async Task RunPreCode_succeeds_when_every_liquid_resource_is_well_formed()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.EnsureAspect<FluidAspectConfiguration>();
        engineConfig.FirstBinPath.Types.Add( typeof( TestFluidTemplatePackage ) );
        engineConfig.FirstBinPath.Types.Add( typeof( IGreetingModel ) );

        await engineConfig.RunSuccessfullyAsync();
    }
}
