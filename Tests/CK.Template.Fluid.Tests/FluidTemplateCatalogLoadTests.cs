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
        // 4 .liquid fixtures on disk, but Greeting.liquid (the culture-less file) ships
        // under both resource-name conventions (standard .NET dotted + CK ck@ uplift),
        // so LoadFromAssemblies performs 5 register operations total. The two
        // (Greeting, Invariant) registrations target the same key — the second
        // overwrites the first — leaving 2 distinct names with 4 distinct (name, culture)
        // entries in the catalog.
        count.ShouldBe( 5 );
        catalog.Names.OrderBy( n => n ).ShouldBe( new[] { "Greeting", "UserInvitation.Body" } );
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
    public void LoadFromAssemblies_does_not_throw_on_full_AppDomain_scan()
    {
        // The NUnit/test-host AppDomain includes dynamic assemblies (Reflection.Emit,
        // anonymous-type generators) which the production code skips via the
        // assembly.IsDynamic check. This test pins down that the full scan stays
        // exception-free even with those assemblies present.
        var catalog = new FluidTemplateCatalog();
        Should.NotThrow( () =>
            catalog.LoadFromAssemblies( TestHelper.Monitor, AppDomain.CurrentDomain.GetAssemblies() ) );
    }
}
