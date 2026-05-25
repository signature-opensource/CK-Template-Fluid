using CK.Setup;
using CK.Template.Fluid.Tests.Engine.Fixtures;
using CK.Testing;
using Microsoft.Extensions.DependencyInjection;
using static CK.Testing.MonitorTestHelper;

namespace CK.Template.Fluid.Tests.Engine;

[TestFixture]
public class EndToEndRenderTests
{
    [Test]
    public async Task IFluidTemplateService_resolved_from_DI_renders_a_template_from_the_catalog()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
        engineConfig.EnsureAspect<FluidAspectConfiguration>();
        engineConfig.FirstBinPath.Types.Add( typeof( TestFluidTemplatePackage ) );
        engineConfig.FirstBinPath.Types.Add( typeof( IGreetingModel ) );
        // FluidAspectTests (the happy-path aspect test) only validates that the
        // engine reaches Succeed status, so it does NOT need to register the
        // runtime types. This test resolves them from the StObj-built DI map,
        // so both the IRealObject catalog and the ISingletonAutoService
        // implementation must be explicitly added — CreateDefaultEngineConfiguration
        // does not auto-discover them from referenced project assemblies.
        engineConfig.FirstBinPath.Types.Add( typeof( FluidTemplateCatalog ) );
        engineConfig.FirstBinPath.Types.Add( typeof( FluidTemplateService ) );

        var result = await engineConfig.RunAsync();
        result.Status.ShouldBe( RunStatus.Succeed );

        var map = result.FindRequiredBinPath( "First" ).TryLoadMap( TestHelper.Monitor );
        map.ShouldNotBeNull();

        var services = new ServiceCollection();
        var register = new StObjContextRoot.ServiceRegister( TestHelper.Monitor, services );
        register.AddStObjMap( map! ).ShouldBeTrue();
        using var provider = register.Services.BuildServiceProvider();

        // Touch hosted services to fire IRealObject initializers (the catalog
        // populates itself in StObjInitialize).
        provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>().Count();

        var svc = provider.GetRequiredService<IFluidTemplateService>();
        var pocoDir = provider.GetRequiredService<PocoDirectory>();
        var model = pocoDir.Create<IGreetingModel>( m => m.Name = "Ada" );
        var fr = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" );

        var output = await svc.RenderAsync( "Greeting", fr, model );
        output.Trim().ShouldBe( "Salut Ada !" );

        var en = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "en" );
        var outputEn = await svc.RenderAsync( "Greeting", en, model );
        outputEn.Trim().ShouldBe( "Hello Ada!" );

        // Invariant fallback for a culture with no registration.
        var de = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "de" );
        var outputDe = await svc.RenderAsync( "Greeting", de, model );
        outputDe.Trim().ShouldBe( "Hi Ada!" );
    }
}
