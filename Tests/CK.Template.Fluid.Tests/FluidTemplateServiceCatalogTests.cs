using CK.Core;
using CK.Template.Fluid;
using CK.Testing;
using Fluid;
using NUnit.Framework;
using Shouldly;
using System.Reflection;
using static CK.Testing.MonitorTestHelper;

namespace CK.Template.Fluid.Tests;

[TestFixture]
public class FluidTemplateServiceCatalogTests
{
    static FluidTemplateService BuildServiceWithFixtures()
    {
        var catalog = new FluidTemplateCatalog();
        catalog.LoadFromAssemblies( TestHelper.Monitor,
                                    new[] { typeof( FluidTemplateServiceCatalogTests ).Assembly } );
        return new FluidTemplateService( catalog );
    }

    [Test]
    public async Task RenderAsync_renders_the_template_for_requested_culture()
    {
        var svc = BuildServiceWithFixtures();
        var fr = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" );
        var output = await svc.RenderAsync( "Greeting", fr, new { name = "Ada" } );
        output.Trim().ShouldBe( "Salut Ada !" );
    }

    [Test]
    public async Task RenderAsync_falls_back_to_invariant_when_culture_is_missing()
    {
        var svc = BuildServiceWithFixtures();
        var de = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "de" );
        var output = await svc.RenderAsync( "Greeting", de, new { name = "Ada" } );
        // German not registered → falls back to invariant "Hi {{ name }}!".
        output.Trim().ShouldBe( "Hi Ada!" );
    }

    [Test]
    public async Task RenderAsync_throws_InvalidOperationException_when_template_is_unknown()
    {
        var svc = BuildServiceWithFixtures();
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => svc.RenderAsync( "NoSuchTemplate", NormalizedCultureInfo.Invariant, new { } ).AsTask() );
        ex.Message.ShouldContain( "NoSuchTemplate" );
        // The error message should list known templates to help the developer.
        ex.Message.ShouldContain( "Greeting" );
    }

    [Test]
    public async Task RenderAsync_with_ambient_binds_extra_variables_alongside_the_model()
    {
        var svc = BuildServiceWithFixtures();
        // Inline catalog entry so we can target ambient explicitly without depending on
        // file fixtures evolving — re-use the public Register API.
        var catalog = new FluidTemplateCatalog();
        var parser = new FluidParser();
        parser.TryParse( "{{ name }} ({{ branding.brandName }})", out var template, out _ );
        catalog.Register( "Branded", NormalizedCultureInfo.Invariant, template! );
        var s = new FluidTemplateService( catalog );

        var ambient = new Dictionary<string, object> { ["branding"] = new { brandName = "Lacoste" } };
        var output = await s.RenderAsync( "Branded", NormalizedCultureInfo.Invariant, new { name = "Ada" }, ambient );
        output.ShouldBe( "Ada (Lacoste)" );
    }

    [Test]
    public async Task RenderAsync_overload_without_ambient_routes_to_the_ambient_overload()
    {
        var svc = BuildServiceWithFixtures();
        var output = await svc.RenderAsync( "Greeting", NormalizedCultureInfo.Invariant, new { name = "Ada" } );
        output.Trim().ShouldBe( "Hi Ada!" );
    }

    [Test]
    public async Task RenderAsync_propagates_pre_render_cancellation()
    {
        var svc = BuildServiceWithFixtures();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ex = await Should.ThrowAsync<OperationCanceledException>( () =>
            svc.RenderAsync( "Greeting", NormalizedCultureInfo.Invariant, new { name = "Ada" }, cts.Token ).AsTask() );
        // Pin the throw site: the production code observes the caller's token
        // via cancel.ThrowIfCancellationRequested() between catalog lookup and
        // render. Asserting CancellationToken == cts.Token confirms the exception
        // came from our token, not an internal one.
        ex.CancellationToken.ShouldBe( cts.Token );
    }
}
