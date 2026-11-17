using Fluid;
using NUnit.Framework;
using Shouldly;

namespace CK.Template.Fluid.Tests.Engine.Broken;

[TestFixture]
public class BrokenTemplateAssetTests
{
    [Test]
    public void Parser_TryParse_returns_false_with_error_text_on_malformed_liquid()
    {
        // This is the same FluidParser.TryParse call used by:
        //   - FluidTemplateService.RenderInlineAsync (wraps as ArgumentException)
        //   - FluidTemplateCatalog.LoadFromAssemblies (wraps as InvalidOperationException)
        //   - FluidTemplatePackageAttributeImpl (logs an Error at the engine run → fails the build)
        // ArgumentException wrapping is exercised in
        // FluidTemplateServiceInlineTests.RenderInlineAsync_throws_ArgumentException_on_parse_error.
        // The engine path is not directly tested with a broken embedded resource: the check parses the
        // templates of the package's assembly, a broken resource here would fail every other engine test
        // of this assembly.
        var parser = new FluidParser();
        var ok = parser.TryParse( "{% if %}oops{% endif %}", out _, out var error );
        ok.ShouldBeFalse();
        error.ShouldNotBeNullOrEmpty();
    }
}
