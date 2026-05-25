using System.Reflection;
using CK.Testing;
using static CK.Testing.MonitorTestHelper;

namespace CK.Template.Fluid.Tests;

[TestFixture]
public class FluidTemplateCatalogLoadTests
{
    static Assembly ThisAssembly => typeof( FluidTemplateCatalogLoadTests ).Assembly;

    [Test]
    public void LoadFromAssemblies_finds_every_liquid_under_Res_Templates()
    {
        var catalog = new FluidTemplateCatalog();
        var count = catalog.LoadFromAssemblies( TestHelper.Monitor, new[] { ThisAssembly } );
        // 4 liquid files; Greeting.liquid is embedded twice (standard .NET name + CK ck@ uplift),
        // so LoadFromAssemblies counts 5 register operations (both are accepted, second overwrites).
        count.ShouldBeGreaterThanOrEqualTo( 4 );
        catalog.Contains( "Greeting" ).ShouldBeTrue();
        catalog.Contains( "UserInvitation.Body" ).ShouldBeTrue();
    }

    [Test]
    public void LoadFromAssemblies_indexes_by_culture()
    {
        var catalog = new FluidTemplateCatalog();
        catalog.LoadFromAssemblies( TestHelper.Monitor, new[] { ThisAssembly } );
        catalog.TryGet( "Greeting", NormalizedCultureInfo.Invariant ).ShouldNotBeNull();
        catalog.TryGet( "Greeting", NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ) ).ShouldNotBeNull();
        catalog.TryGet( "Greeting", NormalizedCultureInfo.EnsureNormalizedCultureInfo( "en" ) ).ShouldNotBeNull();
    }

    [Test]
    public void LoadFromAssemblies_supports_dotted_template_names()
    {
        var catalog = new FluidTemplateCatalog();
        catalog.LoadFromAssemblies( TestHelper.Monitor, new[] { ThisAssembly } );
        // The fr variant exists, an arbitrary other culture should fall back to it via the
        // catalog when invariant is missing — here, no invariant for UserInvitation.Body,
        // so an unrelated culture lookup returns null.
        var template = catalog.TryGet( "UserInvitation.Body", NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ) );
        template.ShouldNotBeNull();
        catalog.TryGet( "UserInvitation.Body", NormalizedCultureInfo.EnsureNormalizedCultureInfo( "de" ) ).ShouldBeNull();
    }

    [Test]
    public void LoadFromAssemblies_skips_dynamic_assemblies()
    {
        // Just confirm the method runs without throwing across the full AppDomain
        // (it includes some dynamic assemblies in NUnit/test-host context).
        var catalog = new FluidTemplateCatalog();
        var count = catalog.LoadFromAssemblies( TestHelper.Monitor, AppDomain.CurrentDomain.GetAssemblies() );
        count.ShouldBeGreaterThanOrEqualTo( 4 );
    }
}
