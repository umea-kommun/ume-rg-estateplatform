using Microsoft.Extensions.Configuration;

namespace Umea.se.EstateService.Test.Infrastructure;

public class AppSettingsSecretGuardTests
{
    // Keys that hold a secret in a deployed environment. In a tracked file they must be empty,
    // an @KeyVault(...) placeholder, or one of the harmless local values below. Real values for
    // local runs belong in `dotnet user-secrets`, never in appsettings*.json.
    private static readonly string[] SecretKeyPrefixes =
    [
        "ConnectionStrings:",
        "Api:Keys:",
        "Pythagoras:ApiKey",
        "Pythagoras:BaseUrl",
        "OpenAI:Endpoint",
        "ApplicationInsights:ConnectionString",
        "ImageCache:BlobConnectionString",
    ];

    private static readonly string[] HarmlessValues =
    [
        "UseDevelopmentStorage=true",
        "DataSource=:memory:",
        "test-api-key-for-integration-tests",
    ];

    // Credential fragments that must not appear in any value, whatever the key is called.
    private static readonly string[] CredentialMarkers =
    [
        "AccountKey=",
        "SharedAccessSignature=",
        "SharedAccessKey=",
        "Password=",
        "Pwd=",
        "?sig=",
        "&sig=",
        "-----BEGIN",
    ];

    [Fact]
    public void TrackedAppSettings_ContainNoSecrets()
    {
        // The API's appsettings*.json are copied next to the test assembly via the project reference.
        string[] files = Directory.GetFiles(AppContext.BaseDirectory, "appsettings*.json");
        files.Select(Path.GetFileName).ShouldContain("appsettings.json");

        List<string> violations = [];

        foreach (string file in files)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile(file, optional: false)
                .Build();

            foreach ((string key, string? value) in configuration.AsEnumerable())
            {
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                bool isSecretKey = SecretKeyPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                bool hasCredentialMarker = CredentialMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));

                if (hasCredentialMarker || (isSecretKey && !IsAllowedSecretValue(value)))
                {
                    // Deliberately not printing the value - test output ends up in pipeline logs.
                    violations.Add($"{Path.GetFileName(file)}: {key}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "Secrets must not be committed. Use an @KeyVault(...) placeholder, and `dotnet user-secrets` for local overrides.");
    }

    private static bool IsAllowedSecretValue(string value) =>
        value.StartsWith("@KeyVault(", StringComparison.Ordinal)
        || HarmlessValues.Contains(value)
        || value.Contains("InstrumentationKey=00000000-0000-0000-0000-000000000000", StringComparison.Ordinal);
}
