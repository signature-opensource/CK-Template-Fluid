# CK.Template.Fluid — Test Suite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the empty placeholder test project with a real test suite covering `CK.Template.Fluid` (runtime catalog + service) and `CK.Template.Fluid.Engine` (aspect + attribute validation), so Lacoste's `MailService` consumption path is regression-protected.

**Architecture:** Two-layer tests in a single test assembly. Layer A = pure NUnit unit tests against the runtime library (no engine). Layer B = integration tests using `CK.Testing.StObjEngine` to drive `FluidAspect` end-to-end. The same `.liquid` fixtures under `Res/Templates/` serve both layers.

**Tech Stack:** NUnit 3, Shouldly (via `CK.Testing.NUnit`), `CK.Testing.StObjEngine 33.0.1--ci.4`, `CK.Testing.NUnit 15.0.1--ci.1`, .NET 8, `Fluid.Core 2.31.0`.

**Reference design doc:** `docs/superpowers/specs/2026-05-25-templating-tests-design.md`

**Working directory assumption:** `D:\dev-ckli\CK\Misc\CK-Templating\` (run all commands from here unless noted).

---

## Task 0: Pre-flight — clean slate

**Files:**
- Delete: `Tests/CK.Template.Fuild.Tests/` (entire folder; recreated under correct name in Task 1)

- [ ] **Step 1: Confirm the old test project is not referenced anywhere**

Run:
```bash
grep -r "Fuild" --include="*.csproj" --include="*.slnx" --include="*.cs" .
```
Expected: matches only inside `Tests/CK.Template.Fuild.Tests/` (folder, its csproj, and `UnitTest1.cs`).
If any match lies outside that folder, STOP and report — the rename has external impact and the user must decide.

- [ ] **Step 2: Delete the placeholder project folder**

```bash
rm -rf Tests/CK.Template.Fuild.Tests
```

- [ ] **Step 3: Commit the deletion**

```bash
git add -A
git commit -m "test: remove empty placeholder test project (typo'd as Fuild)"
```

---

## Task 1: Scaffold the renamed test project

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/CK.Template.Fluid.Tests.csproj`
- Create: `Tests/CK.Template.Fluid.Tests/Usings.cs`
- Modify: `CK-Templating.slnx`

- [ ] **Step 1: Create the csproj**

`Tests/CK.Template.Fluid.Tests/CK.Template.Fluid.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CK.Testing.NUnit" Version="15.0.1--ci.1" />
    <PackageReference Include="CK.Testing.StObjEngine" Version="33.0.1--ci.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\CK.Template.Fluid\CK.Template.Fluid.csproj" />
    <ProjectReference Include="..\..\CK.Template.Fluid.Engine\CK.Template.Fluid.Engine.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create a global-usings file**

`Tests/CK.Template.Fluid.Tests/Usings.cs`:
```csharp
global using CK.Core;
global using CK.Template.Fluid;
global using NUnit.Framework;
global using Shouldly;
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
```

- [ ] **Step 3: Add the test project to the .slnx**

Open `CK-Templating.slnx` and add the new `<Project>` line so the file reads:
```xml
<Solution>
  <Project Path="CK.Template.Fluid.Engine/CK.Template.Fluid.Engine.csproj" />
  <Project Path="CK.Template.Fluid/CK.Template.Fluid.csproj" />
  <Project Path="Tests/CK.Template.Fluid.Tests/CK.Template.Fluid.Tests.csproj" />
</Solution>
```

- [ ] **Step 4: Restore + build**

```bash
dotnet restore CK-Templating.slnx
dotnet build CK-Templating.slnx -c Debug
```
Expected: `Build succeeded.` with 0 warnings, 0 errors. If `CK.Testing.NUnit` complains about a missing transitive (e.g. `Shouldly`), it should still pull it; if not, add `<PackageReference Include="Shouldly" Version="4.2.1" />` to the csproj.

- [ ] **Step 5: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests CK-Templating.slnx
git commit -m "test: scaffold CK.Template.Fluid.Tests project"
```

---

## Task 2: Layer A — `FluidTemplateResourceKey` parser tests

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/FluidTemplateResourceKeyTests.cs`

The parser splits `<Name>[.culture].liquid` filenames. The culture tag must match `^[a-z]{2}(-[A-Za-z]{2,4})?$`. Names may contain dots (e.g. `UserInvitation.Body`).

- [ ] **Step 1: Write the test fixture**

`Tests/CK.Template.Fluid.Tests/FluidTemplateResourceKeyTests.cs`:
```csharp
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
    public void Pascal_cased_last_segment_is_not_treated_as_culture()
    {
        // "Body" looks 4 letters but is uppercased — the regex requires lower-case.
        var key = FluidTemplateResourceKey.TryParse( "UserInvitation.Body.liquid" );
        key!.Value.Name.ShouldBe( "UserInvitation.Body" );
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
```

- [ ] **Step 2: Run the fixture and verify all green**

```bash
dotnet test Tests/CK.Template.Fluid.Tests --filter "FullyQualifiedName~FluidTemplateResourceKeyTests" -c Debug --no-restore
```
Expected: 10 tests, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests/FluidTemplateResourceKeyTests.cs
git commit -m "test: cover FluidTemplateResourceKey filename parser"
```

---

## Task 3: Layer A — `FluidTemplateCatalog` register & lookup tests

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/FluidTemplateCatalogTests.cs`

Catalog responsibilities: register (`name, culture, template`), `TryGet` with culture-fallback chain, `Contains`, `Names`, last-write-wins semantics on re-register.

- [ ] **Step 1: Write the fixture**

`Tests/CK.Template.Fluid.Tests/FluidTemplateCatalogTests.cs`:
```csharp
using Fluid;

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
```

- [ ] **Step 2: Run + verify**

```bash
dotnet test Tests/CK.Template.Fluid.Tests --filter "FullyQualifiedName~FluidTemplateCatalogTests" -c Debug --no-restore
```
Expected: 8 tests, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests/FluidTemplateCatalogTests.cs
git commit -m "test: cover FluidTemplateCatalog register and culture fallback"
```

---

## Task 4: Layer A — Embedded `.liquid` fixtures + `LoadFromAssemblies`

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/Res/Templates/Greeting.liquid`
- Create: `Tests/CK.Template.Fluid.Tests/Res/Templates/Greeting.fr.liquid`
- Create: `Tests/CK.Template.Fluid.Tests/Res/Templates/Greeting.en.liquid`
- Create: `Tests/CK.Template.Fluid.Tests/Res/Templates/UserInvitation.Body.fr.liquid`
- Create: `Tests/CK.Template.Fluid.Tests/FluidTemplateCatalogLoadTests.cs`
- Modify: `Tests/CK.Template.Fluid.Tests/CK.Template.Fluid.Tests.csproj`

The default `Sdk` glob does NOT auto-embed `.liquid`. We must opt in via `<EmbeddedResource>` items.

- [ ] **Step 1: Add the `EmbeddedResource` glob to the csproj**

In `CK.Template.Fluid.Tests.csproj`, add this `<ItemGroup>` (sibling of the existing ItemGroups):
```xml
<ItemGroup>
  <EmbeddedResource Include="Res\Templates\**\*.liquid" />
</ItemGroup>
```

- [ ] **Step 2: Create the fixture templates**

`Tests/CK.Template.Fluid.Tests/Res/Templates/Greeting.liquid`:
```liquid
Hi {{ name }}!
```

`Tests/CK.Template.Fluid.Tests/Res/Templates/Greeting.fr.liquid`:
```liquid
Salut {{ name }} !
```

`Tests/CK.Template.Fluid.Tests/Res/Templates/Greeting.en.liquid`:
```liquid
Hello {{ name }}!
```

`Tests/CK.Template.Fluid.Tests/Res/Templates/UserInvitation.Body.fr.liquid`:
```liquid
Bonjour {{ firstName }}, voici votre invitation : {{ token }}.
```

- [ ] **Step 3: Write the load fixture**

`Tests/CK.Template.Fluid.Tests/FluidTemplateCatalogLoadTests.cs`:
```csharp
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
        // 4 fixtures: Greeting (invariant, fr, en) + UserInvitation.Body.fr.
        count.ShouldBe( 4 );
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
```

- [ ] **Step 4: Run + verify**

```bash
dotnet test Tests/CK.Template.Fluid.Tests --filter "FullyQualifiedName~FluidTemplateCatalogLoadTests" -c Debug
```
Expected: 4 tests, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests/Res Tests/CK.Template.Fluid.Tests/FluidTemplateCatalogLoadTests.cs Tests/CK.Template.Fluid.Tests/CK.Template.Fluid.Tests.csproj
git commit -m "test: cover FluidTemplateCatalog.LoadFromAssemblies with embedded liquid fixtures"
```

---

## Task 5: Layer A — `FluidTemplateService.RenderInlineAsync`

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/FluidTemplateServiceInlineTests.cs`

- [ ] **Step 1: Write the fixture**

`Tests/CK.Template.Fluid.Tests/FluidTemplateServiceInlineTests.cs`:
```csharp
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
```

Notes for the implementer:
- `ValueTask` doesn't expose `Should.ThrowAsync` cleanly in older Shouldly; `.AsTask()` converts it.
- If `Should.ThrowAsync` is unavailable, use `Assert.ThrowsAsync<T>(async () => await svc.RenderInlineAsync(...).AsTask())`.

- [ ] **Step 2: Run + verify**

```bash
dotnet test Tests/CK.Template.Fluid.Tests --filter "FullyQualifiedName~FluidTemplateServiceInlineTests" -c Debug
```
Expected: 6 tests, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests/FluidTemplateServiceInlineTests.cs
git commit -m "test: cover FluidTemplateService.RenderInlineAsync"
```

---

## Task 6: Layer A — `FluidTemplateService.RenderAsync` (catalog + ambient + fallback)

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/FluidTemplateServiceCatalogTests.cs`

Uses the same embedded fixtures from Task 4. Builds the catalog manually (no engine) — the goal is to exercise `RenderAsync(name, culture, model, ambient, cancel)`.

- [ ] **Step 1: Write the fixture**

`Tests/CK.Template.Fluid.Tests/FluidTemplateServiceCatalogTests.cs`:
```csharp
using System.Reflection;
using CK.Testing;
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
        var parser = new global::Fluid.FluidParser();
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
        await Should.ThrowAsync<OperationCanceledException>( () =>
            svc.RenderAsync( "Greeting", NormalizedCultureInfo.Invariant, new { name = "Ada" }, cts.Token ).AsTask() );
    }
}
```

- [ ] **Step 2: Run + verify**

```bash
dotnet test Tests/CK.Template.Fluid.Tests --filter "FullyQualifiedName~FluidTemplateServiceCatalogTests" -c Debug
```
Expected: 6 tests, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests/FluidTemplateServiceCatalogTests.cs
git commit -m "test: cover FluidTemplateService.RenderAsync catalog lookup, fallback, ambient, cancel"
```

---

## Task 7: Layer B — Engine fixtures + `FluidAspect.RunPreCode` happy path

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/Engine/Fixtures/TestFluidTemplatePackage.cs`
- Create: `Tests/CK.Template.Fluid.Tests/Engine/Fixtures/IGreetingModel.cs`
- Create: `Tests/CK.Template.Fluid.Tests/Engine/FluidAspectTests.cs`

The engine harness pattern is documented in `D:\dev-ckli\CK\Web\CK-TypeScript\Tests\CK.TypeScript.Tests\BasicGenerationTests.cs` and `TypeScriptPackageTests.cs`. Replicate the same idiom.

- [ ] **Step 1: Create the marker package fixture**

`Tests/CK.Template.Fluid.Tests/Engine/Fixtures/TestFluidTemplatePackage.cs`:
```csharp
namespace CK.Template.Fluid.Tests.Engine.Fixtures;

[FluidTemplatePackage]
public sealed class TestFluidTemplatePackage : FluidTemplatePackage
{
}
```

- [ ] **Step 2: Create the IPoco model fixture**

`Tests/CK.Template.Fluid.Tests/Engine/Fixtures/IGreetingModel.cs`:
```csharp
namespace CK.Template.Fluid.Tests.Engine.Fixtures;

/// <summary>Bound to the <c>Greeting.*.liquid</c> templates under Res/Templates/.</summary>
[FluidTemplate( "Greeting" )]
public interface IGreetingModel : IPoco
{
    string Name { get; set; }
}
```

- [ ] **Step 3: Write the aspect happy-path test**

`Tests/CK.Template.Fluid.Tests/Engine/FluidAspectTests.cs`:
```csharp
using CK.Setup;
using CK.Template.Fluid.Tests.Engine.Fixtures;
using CK.Testing;
using static CK.Testing.MonitorTestHelper;

namespace CK.Template.Fluid.Tests.Engine;

[TestFixture]
public class FluidAspectTests
{
    [Test]
    public async Task RunPreCode_succeeds_when_every_liquid_resource_is_well_formed()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.EnsureAspect<FluidAspectConfiguration>();
        engineConfig.FirstBinPath.Types.Add( typeof( TestFluidTemplatePackage ) );
        engineConfig.FirstBinPath.Types.Add( typeof( IGreetingModel ) );

        await engineConfig.RunSuccessfullyAsync();
    }
}
```

Notes:
- `CreateDefaultEngineConfiguration` is the same extension `CK.TypeScript.Tests` uses. It comes from `CK.Testing.StObjEngine` via `using CK.Testing;` + `using static CK.Testing.MonitorTestHelper;`.
- `RunSuccessfullyAsync()` runs the engine and asserts `RunStatus.Succeed`. Any monitor `Error` log fails the test.

- [ ] **Step 4: Run + verify**

```bash
dotnet test Tests/CK.Template.Fluid.Tests --filter "FullyQualifiedName~FluidAspectTests" -c Debug
```
Expected: 1 test, 0 failed.
If this fails with "unable to find a matching `EngineConfiguration` constructor" or "could not load CK.StObj.Engine", the test runner cannot resolve the engine. Re-run with `-v normal` to see the load error and surface it — DO NOT push past this failure; it's a real signal.

- [ ] **Step 5: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests/Engine
git commit -m "test: cover FluidAspect.RunPreCode happy path with marker package + IPoco model"
```

---

## Task 8: Layer B — Fluid parse-error canary test

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/Engine/Broken/BrokenTemplateAssetTests.cs`

**Rationale (read before writing the test):** the `FluidAspect.RunPreCode`, `FluidTemplateCatalog.LoadFromAssemblies`, and `FluidTemplateService.RenderInlineAsync` all funnel through the same `FluidParser.TryParse(source, out template, out error)` call. The wrapper layers differ in *how* they react to a failure:
- `RenderInlineAsync` → `ArgumentException` (already covered in Task 5)
- `LoadFromAssemblies` / `RunPreCode` → throw / log error

A direct aspect-level test would require dropping a broken `.liquid` resource into a separate fixture assembly (or building one at runtime via `AssemblyBuilder`, which doesn't reliably expose `GetManifestResourceStream` for in-memory assemblies). Both paths are heavyweight for marginal value. Instead, we add a **parser-level canary** that pins down the underlying contract and document the transitivity explicitly. If the canary regresses, the fix-it call site is obvious.

A sidecar broken-template assembly is a worthwhile follow-up (see "Follow-ups" at end of this plan) but is **out of scope** for this task.

- [ ] **Step 1: Write the canary test**

`Tests/CK.Template.Fluid.Tests/Engine/Broken/BrokenTemplateAssetTests.cs`:
```csharp
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
```

- [ ] **Step 2: Run + verify**

```bash
dotnet test Tests/CK.Template.Fluid.Tests --filter "FullyQualifiedName~BrokenTemplateAssetTests" -c Debug
```
Expected: 1 test, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests/Engine/Broken
git commit -m "test: parser-level canary for the Fluid parse-error path shared by catalog and aspect"
```

---

## Task 9: Layer B — Attribute validation negative tests

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/Engine/AttributeValidationTests.cs`

`FluidTemplateAttributeImpl` logs errors for non-interface or non-IPoco types. `FluidTemplatePackageAttributeImpl` logs errors for classes that don't derive from `FluidTemplatePackage`. We assert via `GetFailedAutomaticServicesAsync(substring)`, mirroring `TypeScriptPackageTests.single_TypeScriptPackage_attribute_is_allowed_Async`.

- [ ] **Step 1: Write the fixture**

`Tests/CK.Template.Fluid.Tests/Engine/AttributeValidationTests.cs`:
```csharp
using CK.Setup;
using CK.Testing;
using static CK.Testing.MonitorTestHelper;

namespace CK.Template.Fluid.Tests.Engine;

[TestFixture]
public class AttributeValidationTests
{
    // --- Negative fixtures ---------------------------------------------

    [FluidTemplate( "BadOnClass" )]
    public class FluidTemplateOnClassFixture
    {
    }

    public interface IPlainInterface
    {
    }

    [FluidTemplate( "BadOnPlainInterface" )]
    public interface IFluidTemplateOnNonPocoFixture : IPlainInterface
    {
    }

    [FluidTemplatePackage]
    public sealed class FluidTemplatePackageOnWrongBaseFixture
    {
        // INTENTIONALLY does NOT derive from FluidTemplatePackage.
    }

    // --- Tests --------------------------------------------------------

    [Test]
    public async Task FluidTemplate_on_a_class_fails_the_build()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.EnsureAspect<FluidAspectConfiguration>();
        engineConfig.FirstBinPath.Types.Add( typeof( FluidTemplateOnClassFixture ) );

        await engineConfig.GetFailedAutomaticServicesAsync( "can only decorate an interface" );
    }

    [Test]
    public async Task FluidTemplate_on_a_non_IPoco_interface_fails_the_build()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.EnsureAspect<FluidAspectConfiguration>();
        engineConfig.FirstBinPath.Types.Add( typeof( IFluidTemplateOnNonPocoFixture ) );

        await engineConfig.GetFailedAutomaticServicesAsync( "does not derive from IPoco" );
    }

    [Test]
    public async Task FluidTemplatePackage_on_a_class_that_does_not_derive_from_FluidTemplatePackage_fails_the_build()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.EnsureAspect<FluidAspectConfiguration>();
        engineConfig.FirstBinPath.Types.Add( typeof( FluidTemplatePackageOnWrongBaseFixture ) );

        await engineConfig.GetFailedAutomaticServicesAsync(
            "does not derive from FluidTemplatePackage" );
    }
}
```

Notes:
- These negative-fixture types are nested types inside the test fixture so they don't pollute the StObj type-discovery of OTHER engine tests in this assembly (other tests only `Add` the specific positive fixtures they need).
- If `GetFailedAutomaticServicesAsync` is not available in this version of `CK.Testing.StObjEngine`, fall back to:
  ```csharp
  var result = await engineConfig.RunAsync();
  result.Status.ShouldBe( RunStatus.Failed );
  ```
  and inspect `TestHelper.Monitor`'s logs via `TestHelper.Monitor.WithCollector(...)` if a substring assertion is needed. Surface this to the user — don't silently weaken the test.

- [ ] **Step 2: Run + verify**

```bash
dotnet test Tests/CK.Template.Fluid.Tests --filter "FullyQualifiedName~AttributeValidationTests" -c Debug
```
Expected: 3 tests, 0 failed.

- [ ] **Step 3: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests/Engine/AttributeValidationTests.cs
git commit -m "test: cover FluidTemplate*AttributeImpl validation errors"
```

---

## Task 10: Layer B — End-to-end DI render test

**Files:**
- Create: `Tests/CK.Template.Fluid.Tests/Engine/EndToEndRenderTests.cs`

After a successful engine run, build a real `IServiceProvider` via the StObj map, resolve `IFluidTemplateService`, render a template that lives in the test assembly. Proves: (1) catalog is populated by `StObjInitialize` automatically, (2) service is wired as `ISingletonAutoService`, (3) end-to-end through Lacoste's surface works.

- [ ] **Step 1: Write the fixture**

`Tests/CK.Template.Fluid.Tests/Engine/EndToEndRenderTests.cs`:
```csharp
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
```

Notes:
- `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Hosting.Abstractions` should arrive transitively via `CK.Testing.StObjEngine`. If `IHostedService` resolution fails to compile, add `<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="8.0.0" />` to the test csproj.
- `PocoDirectory` lives in `CK.Core` and is resolvable from the StObj-built provider.
- This test depends on the `Greeting.*.liquid` fixtures created in Task 4 — they are loaded by `FluidTemplateCatalog.StObjInitialize` walking `AppDomain.CurrentDomain.GetAssemblies()`.

- [ ] **Step 2: Run + verify**

```bash
dotnet test Tests/CK.Template.Fluid.Tests --filter "FullyQualifiedName~EndToEndRenderTests" -c Debug
```
Expected: 1 test, 0 failed.
If you get `No Fluid template registered under name 'Greeting'`, the catalog's `StObjInitialize` did not run — verify the runtime test-bench is invoking the IRealObject initializers (the `GetServices<IHostedService>()` line is the canonical trigger).

- [ ] **Step 3: Commit**

```bash
git add Tests/CK.Template.Fluid.Tests/Engine/EndToEndRenderTests.cs
git commit -m "test: end-to-end render via StObj DI container (catalog populated by StObjInitialize)"
```

---

## Task 11: Full-suite verification + final commit

**Files:**
- None (verification step only)

- [ ] **Step 1: Clean rebuild**

```bash
dotnet clean CK-Templating.slnx
dotnet restore CK-Templating.slnx
dotnet build CK-Templating.slnx -c Debug
```
Expected: 0 warnings, 0 errors.

- [ ] **Step 2: Run the full suite**

```bash
dotnet test CK-Templating.slnx -c Debug
```
Expected output summary: **39 tests** (10 + 8 + 4 + 6 + 6 + 1 + 1 + 2 + 1 across 9 fixtures — see follow-up #4 below for why Task 9 ships 2 tests instead of 3), **0 failed**.

- [ ] **Step 3: Spot-check the `$StObjGen/G0.cs`**

After Layer B tests run, a `Tests/CK.Template.Fluid.Tests/$StObjGen/G0.cs` file may have been generated. Per CLAUDE.md test-project G0.cs rule, this file is regenerated at test execution — leave it alone. Verify nothing in the diff touches engine source.

```bash
git status
git diff --stat
```
Expected: no uncommitted production-code changes.

- [ ] **Step 4: Final summary commit (only if anything still uncommitted)**

If `git status` shows nothing, skip this step. Otherwise:
```bash
git add -A
git commit -m "test: stabilize Layer A + Layer B suite"
```

---

## Out-of-scope reminders

- No production code changes. The aspect's no-op `ICSCodeGenerator.Implement`, the future-allow-list TODOs in the `*AttributeImpl` files, and the strict `MemberAccessStrategy` switch all stay untouched.
- No new CK package extraction — the spec calls out that some of these test patterns (engine-aspect harness + IPoco model fixture) could become a `CK.Testing.Fluid` helper, but that's a follow-up.

## Follow-ups worth flagging to the user

1. The aspect's malformed-`.liquid` build-failure path is covered only at the parser level (Task 8), not via an actual aspect run. If a stronger regression net is needed, a sidecar test-fixture assembly project that ships a broken `.liquid` is the cleanest next step.
2. If `CK.Testing.NUnit` 15.0.1--ci.1 and `CK.Testing.StObjEngine` 33.0.1--ci.4 have conflicting transitive deps, downgrading `CK.Testing.NUnit` to a matching v33 series (if one exists) may be needed.
3. The two-pass layout-composition pattern Lacoste uses (`_DefaultMailLayout` wrapping a body template) is NOT directly tested — it's emergent from `RenderAsync` + ambient bindings, which are covered separately. A combined integration test could be added if regressions surface.
4. **Task 9 deviation:** The plan called for a third negative test — `[FluidTemplate]` on a class — but `FluidTemplateAttribute` is declared `AttributeUsage(AttributeTargets.Interface)`, which the C# compiler enforces before the StObj engine ever sees the type (CS0592). The `if( !type.IsInterface )` branch in `FluidTemplateAttributeImpl.cs:41-44` is therefore dead from C#. Possible decisions: (a) delete the dead branch from the Impl, (b) widen the attribute to `Interface | Class` so the engine becomes the validator (defense in depth), or (c) leave as-is and accept the redundancy. Final test count is **39** instead of the planned 40.
5. **Task 10 addition:** the plan's verbatim snippet omitted explicit type registration for `FluidTemplateCatalog` (IRealObject) and `FluidTemplateService` (ISingletonAutoService). `CreateDefaultEngineConfiguration` does not auto-discover these from referenced project assemblies, so without explicit `engineConfig.FirstBinPath.Types.Add(typeof(FluidTemplateCatalog))` and `...Add(typeof(FluidTemplateService))`, the StObj map builds but contains no service registrations and DI resolution throws. The implemented test adds these two lines.
