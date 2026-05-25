namespace CK.Template.Fluid.Tests;

[TestFixture]
public class FluidTemplateResourceKeyTests
{
    [Test]
    public void Simple_name_without_culture_falls_back_to_invariant()
    {
        var key = FluidTemplateResourceKey.TryParse( "Greeting.liquid" );
        key.HasValue.ShouldBeTrue();
        key!.Value.Name.ShouldBe( "Greeting" );
        key.Value.Culture.ShouldBe( NormalizedCultureInfo.Invariant );
    }

    [Test]
    public void Two_letter_culture_tag_is_detected()
    {
        var key = FluidTemplateResourceKey.TryParse( "Greeting.fr.liquid" );
        key!.Value.Name.ShouldBe( "Greeting" );
        key.Value.Culture.Name.ShouldBe( "fr" );
    }

    [Test]
    public void Five_letter_region_culture_tag_is_detected()
    {
        var key = FluidTemplateResourceKey.TryParse( "Welcome.fr-FR.liquid" );
        key!.Value.Name.ShouldBe( "Welcome" );
        key.Value.Culture.Name.ShouldBe( "fr-fr" );
    }

    [Test]
    public void Script_subtag_culture_is_detected()
    {
        var key = FluidTemplateResourceKey.TryParse( "Welcome.zh-Hans.liquid" );
        key!.Value.Name.ShouldBe( "Welcome" );
        key.Value.Culture.Name.ShouldBe( "zh-hans" );
    }

    [Test]
    public void Dotted_name_is_preserved_when_last_segment_is_culture()
    {
        var key = FluidTemplateResourceKey.TryParse( "UserInvitation.Body.fr.liquid" );
        key!.Value.Name.ShouldBe( "UserInvitation.Body" );
        key.Value.Culture.Name.ShouldBe( "fr" );
    }

    [Test]
    public void Dotted_name_without_culture_keeps_the_full_name_invariant()
    {
        var key = FluidTemplateResourceKey.TryParse( "UserInvitation.Body.liquid" );
        key!.Value.Name.ShouldBe( "UserInvitation.Body" );
        key.Value.Culture.ShouldBe( NormalizedCultureInfo.Invariant );
    }

    [Test]
    public void Uppercase_two_letter_suffix_is_not_treated_as_culture()
    {
        // The culture regex (^[a-z]{2}(-[A-Za-z]{2,4})?$) requires the language
        // portion to be lower-case. "EN" is two letters but uppercased, so the
        // parser must treat it as part of the name, not as a culture tag.
        var key = FluidTemplateResourceKey.TryParse( "UserInvitation.EN.liquid" );
        key!.Value.Name.ShouldBe( "UserInvitation.EN" );
        key.Value.Culture.ShouldBe( NormalizedCultureInfo.Invariant );
    }

    [Test]
    public void Non_liquid_extension_returns_null()
    {
        FluidTemplateResourceKey.TryParse( "Greeting.txt" ).HasValue.ShouldBeFalse();
        FluidTemplateResourceKey.TryParse( "Greeting" ).HasValue.ShouldBeFalse();
    }

    [Test]
    public void Empty_basename_returns_null()
    {
        FluidTemplateResourceKey.TryParse( ".liquid" ).HasValue.ShouldBeFalse();
    }

    [Test]
    public void Extension_match_is_case_insensitive()
    {
        var key = FluidTemplateResourceKey.TryParse( "Greeting.LIQUID" );
        key.HasValue.ShouldBeTrue();
        key!.Value.Name.ShouldBe( "Greeting" );
    }
}
