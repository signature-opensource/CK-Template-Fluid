using CK.Template.Fluid;
using NUnit.Framework;
using Shouldly;

namespace CK.Template.Fluid.Tests;

[TestFixture]
public class FluidTemplateServiceInlineTests
{
    public sealed class Person
    {
        public string FirstName { get; set; } = "";
        public string LastName  { get; set; } = "";
    }

    static IFluidTemplateService CreateService() => new FluidTemplateService( new FluidTemplateCatalog() );

    [Test]
    public async Task RenderInlineAsync_substitutes_simple_variables()
    {
        var svc = CreateService();
        var output = await svc.RenderInlineAsync( "Hello {{ name }}!", new { name = "World" } );
        output.ShouldBe( "Hello World!" );
    }

    [Test]
    public async Task RenderInlineAsync_maps_camelCase_template_to_PascalCase_clr()
    {
        var svc = CreateService();
        var p = new Person { FirstName = "Ada", LastName = "Lovelace" };
        var output = await svc.RenderInlineAsync( "{{ firstName }} {{ lastName }}", p );
        output.ShouldBe( "Ada Lovelace" );
    }

    [Test]
    public async Task RenderInlineAsync_with_null_model_renders_text_only_templates()
    {
        var svc = CreateService();
        var output = await svc.RenderInlineAsync( "Static text.", model: null );
        output.ShouldBe( "Static text." );
    }

    [Test]
    public async Task RenderInlineAsync_throws_ArgumentException_on_parse_error()
    {
        var svc = CreateService();
        var ex = await Should.ThrowAsync<ArgumentException>( () => svc.RenderInlineAsync( "{% if %}", null ).AsTask() );
        ex.ParamName.ShouldBe( "source" );
        ex.Message.ShouldContain( "Fluid parse error" );
    }

    [Test]
    public async Task RenderInlineAsync_throws_ArgumentNullException_on_null_source()
    {
        var svc = CreateService();
        await Should.ThrowAsync<ArgumentNullException>( () => svc.RenderInlineAsync( null!, null ).AsTask() );
    }

    [Test]
    public async Task RenderInlineAsync_supports_control_flow_and_filters()
    {
        var svc = CreateService();
        var output = await svc.RenderInlineAsync(
            "{% if name %}{{ name | upcase }}{% else %}anonymous{% endif %}",
            new { name = "ada" } );
        output.ShouldBe( "ADA" );
    }
}
