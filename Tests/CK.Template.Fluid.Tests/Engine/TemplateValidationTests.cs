using CK.Setup;
using CK.Template.Fluid.Tests.Engine.Fixtures;
using CK.Testing;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Template.Fluid.Tests.Engine;

[TestFixture]
public class TemplateValidationTests
{
    [Test]
    public async Task the_templates_of_a_package_are_parsed_by_the_engine()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.Types.Add( typeof( TestFluidTemplatePackage ) );
        engineConfig.FirstBinPath.Types.Add( typeof( IGreetingModel ) );

        await engineConfig.RunSuccessfullyAsync();
    }
}
