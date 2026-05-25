# CK.Template.Fluid — Test Suite Design

**Date:** 2026-05-25
**Author:** Arthur (auto-mode session)
**Scope:** Bring the empty `Tests/CK.Template.Fuild.Tests/` project to life as a real test suite covering the runtime library `CK.Template.Fluid` and the engine aspect `CK.Template.Fluid.Engine`.

## Goal

Cover the behaviors that consumers (notably `Lacoste.App` and `CK.Mail.SharedLayout`) rely on, plus the engine-side guarantees the aspect promises (build-time parse validation, attribute type checks). Catch regressions before they reach Lacoste's `MailService`.

## Out of scope

- TypeScript / Angular integration (none today).
- Custom Fluid filters (none yet — Lacoste uses built-ins only).
- Future allow-list / strict-mode work the source files reference — those are TODOs in the engine, not test targets.
- Code-generation tests for `G0.cs` emission (also future work, currently a no-op `ICSCodeGenerator.Implement`).

## House-keeping decisions (auto-mode, will revert if user disagrees)

1. **Rename the test project** from `CK.Template.Fuild.Tests` → `CK.Template.Fluid.Tests` (folder, csproj, root namespace). The typo is in the path, csproj filename, and namespace.
2. **Add it to `CK-Templating.slnx`** (currently missing).
3. **Delete the placeholder `UnitTest1.cs`** once real tests exist.

## Two-layer strategy

### Layer A — Unit tests (no engine harness)

Pure NUnit. Exercises every public surface of `CK.Template.Fluid` without spinning up StObj. ~80% of the value, near-zero infrastructure risk.

- **`FluidTemplateResourceKey.TryParse`** — happy paths, culture-tag detection, dotted base names (`UserInvitation.Body.fr.liquid`), invariant fallback (no culture suffix), wrong extension → `null`, empty input → `null`, ambiguous suffix (e.g. `Body` not a culture tag).
- **`FluidTemplateCatalog`** — register/contains/names; `TryGet` with exact culture, fallback culture, invariant fallback, miss; re-registration overwrites (consumer-package override pattern); argument-null/empty throws.
- **`FluidTemplateCatalog.LoadFromAssemblies`** — load from the *test assembly itself* (embeds a tiny set of `.liquid` resources under `Res/Templates/`); count returned is correct; standard-dotted resource name format is parsed; `ck@Res/Templates/` format (CK.EmbeddedResources uplift) is also parsed; malformed template throws `InvalidOperationException` with an informative message.
- **`FluidTemplateService.RenderInlineAsync`** — renders simple `{{ var }}`, applies camelCase→PascalCase member mapping (`{{ firstName }}` reaches `FirstName`), null model renders an empty-context template, parse error throws `ArgumentException` referring to `source`.
- **`FluidTemplateService.RenderAsync`** — finds a template via catalog and renders it; passes culture through; falls back to invariant when requested culture is missing; ambient bindings appear alongside the model (`{{ branding.brandName }}` works); unknown template name throws `InvalidOperationException` with the available names listed; cancellation token throws after registration.

### Layer B — Engine integration tests (`FluidAspect`)

Uses `CK.Testing.StObjEngine` (the same harness `CK.TypeScript.Tests` uses, pattern from `BasicGenerationTests.cs`). Smaller suite — confirms the build-time guarantees.

- **`RunPreCode` succeeds on valid templates** — well-formed `.liquid` in `Res/Templates/`, package marker present → engine `RunStatus.Succeed`.
- **`RunPreCode` fails on a malformed template** — assembly with a `.liquid` resource that has bad Liquid syntax → engine emits an `Error` log mentioning the resource path and `Fluid parse error`.
- **`FluidTemplateAttributeImpl` rejects non-interface targets** — `[FluidTemplate]` on a class → error logged, services fail to compose. Use `GetFailedAutomaticServicesAsync(...)` with a substring assertion.
- **`FluidTemplateAttributeImpl` rejects non-IPoco interfaces** — `[FluidTemplate]` on a plain interface → same failure path.
- **`FluidTemplatePackageAttributeImpl` rejects non-`FluidTemplatePackage` classes** — `[FluidTemplatePackage]` on a `class` that doesn't derive from `FluidTemplatePackage` → failure with a clear message.
- **End-to-end runtime render** — after a successful engine run, load the StObjMap, build the `IServiceProvider`, resolve `IFluidTemplateService`, render a template that lives in this test assembly's `Res/Templates/`, assert output. This is the smoke test that proves the catalog is populated by `StObjInitialize` and the service is wired as `ISingletonAutoService`.

## File layout

```
Tests/
└── CK.Template.Fluid.Tests/                  # renamed
    ├── CK.Template.Fluid.Tests.csproj
    ├── FluidTemplateResourceKeyTests.cs      # Layer A
    ├── FluidTemplateCatalogTests.cs          # Layer A
    ├── FluidTemplateServiceTests.cs          # Layer A
    ├── Engine/
    │   ├── FluidAspectTests.cs               # Layer B - aspect run
    │   ├── FluidTemplateAttributeTests.cs    # Layer B - validation errors
    │   ├── EndToEndRenderTests.cs            # Layer B - DI resolution + render
    │   └── Fixtures/
    │       ├── TestFluidTemplatePackage.cs   # [FluidTemplatePackage] marker
    │       ├── IGreetingModel.cs             # [FluidTemplate("Greeting")] IPoco
    │       └── (malformed package fixtures inline in test methods)
    └── Res/
        └── Templates/
            ├── Greeting.liquid               # invariant
            ├── Greeting.fr.liquid            # culture variant
            └── Greeting.en.liquid            # culture variant
```

`.liquid` files end up as embedded resources via the default `EmbeddedResource` glob (CK convention — same as Lacoste).

## Tooling

### Layer A packages (already present)

- NUnit 3.14.0
- NUnit3TestAdapter 4.5.0
- Microsoft.NET.Test.Sdk 17.8.0

Add a project reference to `CK.Template.Fluid` so we exercise the real types.

### Layer B packages (to add)

- `CK.Testing.StObjEngine` (or `CK.Testing.Nunit` if it transitively pulls the engine harness) — at the v33 ci.4 era to match `CK.StObj.Engine 33.0.1--ci.4` already referenced. **Known unknown:** exact compatible version. Two-step plan:
  1. Try `CK.Testing.StObjEngine` at the latest `33.x--ci.*` version; if mismatch, downgrade Layer B to skipped (`[Ignore]`) tests and surface to user.
  2. Add a `ProjectReference` to `CK.Template.Fluid.Engine` so the aspect is resolvable.

If Layer B turns out to be infeasible at the current package versions, the spec still delivers Layer A in full — that's the bulk of the coverage.

## Patterns mirrored from existing CK tests

- Embedded `.liquid` fixture files in `Res/Templates/` of the test project (mirrors how `CK.TS.Angular.Tests` embeds `*.html` files).
- Engine assertions use the helpers from `CK.Testing.StObjEngine` (`EnsureAspect<FluidAspectConfiguration>()`, `RunSuccessfullyAsync()`, `GetFailedAutomaticServicesAsync("substring")`).
- One assembly per malformed-fixture case is avoided by using *inline source code* attribute targets — i.e. the malformed fixtures are normal types in the test project that we add or omit from `engineConfig.FirstBinPath.Types` per test, so a single test assembly can host multiple negative tests without an embedded broken `.liquid` file in the default resource set.

For the **malformed `.liquid` parse-error test**, we embed the file under a *non-default* path (e.g. `Broken/Templates/`) so the default scan doesn't see it, then in the test we copy it into a temporary on-disk assembly OR write it to a temporary directory and point the aspect at it. Decision: simplest path is **a separate embedded resource sub-folder + an opt-in test fixture assembly under `Tests/CK.Template.Fluid.Tests.BrokenTemplates/`** — keeps the default test assembly clean.

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| `CK.Testing.StObjEngine` version drift vs. ci.4 packages | Try latest matching prerelease; fall back to Layer A only |
| Malformed-`.liquid` fixture breaks the default `dotnet build` | Put broken templates in a separate test-fixture assembly OR write them to a temp directory at test time, not as embedded resources of the main test project |
| Test assembly is auto-discovered by the runtime catalog (`AppDomain.CurrentDomain.GetAssemblies()`) and pollutes Layer A | Layer A doesn't instantiate `StObjInitialize` directly — it calls `LoadFromAssemblies(monitor, [typeof(X).Assembly])` with the assembly we choose, so isolation is explicit |
| The `Fuild` typo rename breaks something external | Search the workspace for references first; the empty test project is brand new and not referenced anywhere yet (slnx omission confirms this) |

## Acceptance criteria

- `dotnet test` from the solution root runs and passes (Layer A at minimum).
- The test project appears in `CK-Templating.slnx`.
- Project name and namespace no longer say `Fuild`.
- Every public method on `IFluidTemplateService` has at least one happy-path and one error-path test.
- The `FluidAspect.RunPreCode` parse-error code path is covered (Layer B), or explicitly deferred with a `TODO` comment + a follow-up note in the spec if Layer B is blocked by package versions.
