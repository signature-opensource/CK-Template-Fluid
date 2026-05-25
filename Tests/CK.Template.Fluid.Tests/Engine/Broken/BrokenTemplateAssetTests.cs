using Fluid;

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
        //   - FluidAspect.RunPreCode (logs Error and returns false → fails the build)
        // ArgumentException wrapping is exercised in
        // FluidTemplateServiceInlineTests.RenderInlineAsync_throws_ArgumentException_on_parse_error.
        // The aspect path is not directly tested with a broken embedded resource because
        // doing so would pollute every other engine test in this assembly via the
        // AppDomain-wide resource scan. See plan follow-up #1 for the sidecar approach.
        var parser = new FluidParser();
        var ok = parser.TryParse( "{% if %}oops{% endif %}", out _, out var error );
        ok.ShouldBeFalse();
        error.ShouldNotBeNullOrEmpty();
    }
}
