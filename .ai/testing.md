# Testing

## Frameworks

- **xUnit v3** as the test runner (`xunit` 2.9.3 + `xunit.runner.visualstudio` 3.1.5).
- **Shouldly** for assertions (`Shouldly` 4.3.0).
- **Microsoft.AspNetCore.Mvc.Testing** for in-process API tests via `Umea.se.TestToolkit`.
- **Coverlet** for coverage collection.

## Commands

Run all tests: `dotnet test`
Run a single project: `dotnet test src/ume-app-estateservice/Umea.se.EstateService.Test/`
Run a single test: `dotnet test --filter "FullyQualifiedName~InMemorySearchServiceAddressTests.Search_FindsDocumentsByAddressTokens"`
Run by class: `dotnet test --filter "FullyQualifiedName~InMemorySearchServiceAddressTests"`
With coverage: `dotnet test --collect:"XPlat Code Coverage"`

## Test Structure

- Tests live in `src/ume-app-estateservice/Umea.se.EstateService.Test/`, mirroring the source tree (e.g., `Logic/Search/InMemorySearchService.cs` → `Test/Search/InMemorySearchServiceAddressTests.cs`).
- Integration tests use `TestApiFactory` from `Umea.se.TestToolkit` and run in the `IntegrationTest` ASP.NET environment (see the `IsEnvironment("IntegrationTest")` branches in `Program.cs`).
- File naming: one `*Tests.cs` per scenario, not per source class.

## Conventions

- **No `// Arrange / // Act / // Assert` comments.** Let the structure speak.
- Use Shouldly (`result.ShouldBe(...)`, `collection.ShouldContain(...)`).
- Match nearby files for test method naming (`MethodOrBehavior_StateUnderTest_ExpectedResult`).
- Build test data inline with object initializers; do not introduce builders for one-off cases.
- Prefer `[Theory]` + `[InlineData]` for branch/permutation tables over copy-pasted `[Fact]`s.

## Gotchas

- **`ShouldContain(predicate)` returns `void`** — you can't assign its result. Assert first, then re-fetch:
  ```csharp
  diagnostics.TopScoreBreakdowns.ShouldContain(b => b.DocId == 1);
  DocumentScoreBreakdown breakdown = diagnostics.TopScoreBreakdowns.First(b => b.DocId == 1);
  ```
- **`Should.ThrowAsync<T>` / `Should.NotThrowAsync`** are the async assertion entry points (e.g. `await Should.ThrowAsync<BusinessValidationException>(async () => await validator.ValidateAsync(files))`).
- **`double.NaN` can't be a compile-time constant**, so it's not usable in `[InlineData]`. Write a dedicated `[Fact]` for the NaN case.

## Building config in tests

Construct `ApplicationConfig` from an in-memory `ConfigurationBuilder`. Keys use colon paths, and arrays/lists bind via indexed keys:

```csharp
Dictionary<string, string?> data = new()
{
    ["WorkOrder:FileValidation:MaxFileCount"] = "3",
    ["WorkOrder:FileValidation:AllowedContentTypes:0"] = "application/pdf",
    ["WorkOrder:FileValidation:AllowedContentTypes:1"] = "image/*",
};
ApplicationConfig config = new(new ConfigurationBuilder().AddInMemoryCollection(data).Build());
```

## HTTP client testing

Test `PythagorasClient` and other `HttpClient` consumers with a custom `HttpMessageHandler` stub plus an `IHttpClientFactory` fake — no live HTTP. For methods that resolve multiple named clients (e.g. `PythagorasImages`, `PythagorasBlueprints`), use a factory that serves every name and records which were requested:

```csharp
private sealed class MultiClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public List<string> RequestedNames { get; } = [];
    public HttpClient CreateClient(string name)
    {
        RequestedNames.Add(name);
        return new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://example.org/") };
    }
}
```

The stub handler should capture the last request/content for assertions and accept either a fixed `(HttpStatusCode, content)` or a `Func<HttpRequestMessage, HttpResponseMessage>` responder for per-request behavior. See `Test/Pythagoras/PythagorasClientWorkOrderTests.cs`.

## Measuring coverage

Collect and inspect coverage to find under-tested code:

```
dotnet test src/ume-app-estateservice/Umea.se.EstateService.Test/ --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

Parse the newest `TestResults/**/coverage.cobertura.xml`. When ranking by missed lines, **aggregate async state-machine subclasses** (`<MethodName>d__N`, `<>c__DisplayClassN_0`) back into their declaring class, and filter generated/boilerplate noise (Migrations, `*Dto`, `*Request`, `*Response`, enums, JSON converters, `Program`, `obj/`, and the test files themselves) so the ranking reflects real logic.

## Example

From `src/ume-app-estateservice/Umea.se.EstateService.Test/Search/InMemorySearchServiceAddressTests.cs`:

```csharp
[Fact]
public void Search_FindsDocumentsByAddressTokens()
{
    DateTimeOffset now = DateTimeOffset.UtcNow;

    PythagorasDocument building = new()
    {
        Id = 1,
        Type = NodeType.Building,
        Name = "Library",
        PopularName = "Central Library",
        Address = new AddressModel("Skolgatan 31A", "901 84", "Umeå", string.Empty, string.Empty),
        Ancestors = [],
        UpdatedAt = now,
        RankScore = 1
    };

    InMemorySearchService service = new([building, /* ... */]);
    // ... call service.Search(...) and assert with Shouldly
}
```
