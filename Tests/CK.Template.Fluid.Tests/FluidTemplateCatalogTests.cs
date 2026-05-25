using CK.Core;
using CK.Template.Fluid;
using Fluid;
using NUnit.Framework;
using Shouldly;

namespace CK.Template.Fluid.Tests;

[TestFixture]
public class FluidTemplateCatalogTests
{
    static IFluidTemplate Parse( string source )
    {
        var parser = new FluidParser();
        if( !parser.TryParse( source, out var template, out var error ) )
        {
            throw new InvalidOperationException( $"Test setup: bad Fluid source: {error}" );
        }
        return template;
    }

    [Test]
    public void Contains_returns_true_after_register()
    {
        var c = new FluidTemplateCatalog();
        c.Register( "Greet", NormalizedCultureInfo.Invariant, Parse( "Hi" ) );
        c.Contains( "Greet" ).ShouldBeTrue();
        c.Names.ShouldContain( "Greet" );
    }

    [Test]
    public void TryGet_returns_the_exact_culture_match()
    {
        var c = new FluidTemplateCatalog();
        var fr = Parse( "Salut" );
        c.Register( "Greet", NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ), fr );
        c.TryGet( "Greet", NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ) ).ShouldBeSameAs( fr );
    }

    [Test]
    public void TryGet_walks_culture_fallback_chain()
    {
        var c = new FluidTemplateCatalog();
        var fr = Parse( "Salut" );
        // Register only the parent "fr" culture; ask for "fr-FR" → fallback to "fr".
        c.Register( "Greet", NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ), fr );
        var frFr = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-FR" );
        c.TryGet( "Greet", frFr ).ShouldBeSameAs( fr );
    }

    [Test]
    public void TryGet_falls_back_to_invariant_when_no_culture_chain_matches()
    {
        var c = new FluidTemplateCatalog();
        var inv = Parse( "Hello" );
        c.Register( "Greet", NormalizedCultureInfo.Invariant, inv );
        var de = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "de" );
        c.TryGet( "Greet", de ).ShouldBeSameAs( inv );
    }

    [Test]
    public void TryGet_returns_null_when_name_is_unknown()
    {
        var c = new FluidTemplateCatalog();
        c.Register( "Greet", NormalizedCultureInfo.Invariant, Parse( "Hi" ) );
        c.TryGet( "Unknown", NormalizedCultureInfo.Invariant ).ShouldBeNull();
    }

    [Test]
    public void TryGet_returns_null_when_no_culture_in_chain_matches_and_no_invariant()
    {
        var c = new FluidTemplateCatalog();
        // Only "fr" registered, asking for "en" → no fallback hits.
        c.Register( "Greet", NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ), Parse( "Salut" ) );
        c.TryGet( "Greet", NormalizedCultureInfo.EnsureNormalizedCultureInfo( "en" ) ).ShouldBeNull();
    }

    [Test]
    public void Register_overwrites_existing_key()
    {
        var c = new FluidTemplateCatalog();
        var first = Parse( "First" );
        var second = Parse( "Second" );
        c.Register( "Greet", NormalizedCultureInfo.Invariant, first );
        c.Register( "Greet", NormalizedCultureInfo.Invariant, second );
        c.TryGet( "Greet", NormalizedCultureInfo.Invariant ).ShouldBeSameAs( second );
    }

    [Test]
    public void Register_validates_arguments()
    {
        var c = new FluidTemplateCatalog();
        var t = Parse( "x" );
        Should.Throw<ArgumentException>( () => c.Register( "", NormalizedCultureInfo.Invariant, t ) );
        Should.Throw<ArgumentException>( () => c.Register( "  ", NormalizedCultureInfo.Invariant, t ) );
        Should.Throw<ArgumentNullException>( () => c.Register( null!, NormalizedCultureInfo.Invariant, t ) );
        Should.Throw<ArgumentNullException>( () => c.Register( "x", null!, t ) );
        Should.Throw<ArgumentNullException>( () => c.Register( "x", NormalizedCultureInfo.Invariant, null! ) );
    }
}
