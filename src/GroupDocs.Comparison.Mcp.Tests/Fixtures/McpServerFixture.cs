using ModelContextProtocol.Client;
using Xunit;

namespace GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;

/// Boots the GroupDocs.Comparison.Mcp server as a child process, wires an MCP stdio
/// client, and seeds a temporary storage folder with sample documents. Shared across
/// all tests in the same xUnit collection.
// Used as IClassFixture<McpServerFixture> — ONE fresh server process PER TEST CLASS,
// so no single process exhausts the engine's evaluation-mode document-open cap.
public sealed class McpServerFixture : IAsyncLifetime
{
    /// Points the suite at a locally built server DLL instead of the published NuGet.
    /// Set it to run these tests before a release is published — same tests, same
    /// protocol, against the build on disk:
    ///   MCP_SERVER_DLL=...\src\GroupDocs.Comparison.Mcp\bin\Debug\net10.0\GroupDocs.Comparison.Mcp.dll
    public const string LocalServerVariable = "MCP_SERVER_DLL";

    public string StoragePath { get; } = Path.Combine(
        Path.GetTempPath(),
        $"gdcomp-mcp-it-{Guid.NewGuid():N}");

    public string PackageVersionUnderTest => PackageVersion.Value;

    /// Which channel the server was launched from — "local" or "dnx". Reported in
    /// failures so a red test never leaves you guessing which artifact was exercised.
    public string Channel { get; private set; } = "dnx";

    public McpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(StoragePath);
        SampleDocuments.WriteAll(StoragePath);
        SampleDocuments.CopyRealSamples(StoragePath, SampleDocuments.ResolveSourceSampleDocs());

        var (command, arguments) = ResolveLaunch();

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "groupdocs-comparison-mcp",
            Command = command,
            Arguments = arguments,
            WorkingDirectory = StoragePath,
            EnvironmentVariables = BuildServerEnv(),
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        Client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
    }

    private (string Command, string[] Arguments) ResolveLaunch()
    {
        var localDll = Environment.GetEnvironmentVariable(LocalServerVariable);
        if (!string.IsNullOrWhiteSpace(localDll))
        {
            if (!File.Exists(localDll))
                throw new InvalidOperationException(
                    $"{LocalServerVariable} is set to '{localDll}', but no such file exists. " +
                    "Build the server project first, or unset the variable to test the published package.");

            Channel = "local";
            return (CommandResolver.Resolve("dotnet"), [localDll]);
        }

        Channel = "dnx";
        var packageSpec = PackageVersion.IsLatest
            ? "GroupDocs.Comparison.Mcp"
            : $"GroupDocs.Comparison.Mcp@{PackageVersion.Value}";
        return (CommandResolver.Resolve("dnx"), [packageSpec, "--yes"]);
    }

    private Dictionary<string, string?> BuildServerEnv()
    {
        var env = new Dictionary<string, string?>
        {
            ["GROUPDOCS_MCP_STORAGE_PATH"] = StoragePath,
            ["DOTNET_NOLOGO"] = "true",
        };

        var licensePath = Environment.GetEnvironmentVariable("GROUPDOCS_LICENSE_PATH");
        if (!string.IsNullOrEmpty(licensePath))
            env["GROUPDOCS_LICENSE_PATH"] = licensePath;

        // Metered keys are forwarded only when both are present. Passing one alone would
        // exercise the server's half-configured fallback rather than metered licensing,
        // which is not what a metered test run is asking for — and the server warns about
        // it, so a partially-configured CI secret would surface as a confusing pass.
        var publicKey = Environment.GetEnvironmentVariable(MeteredKeys.PublicKeyVariable);
        var privateKey = Environment.GetEnvironmentVariable(MeteredKeys.PrivateKeyVariable);
        if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(privateKey))
        {
            env[MeteredKeys.PublicKeyVariable] = publicKey;
            env[MeteredKeys.PrivateKeyVariable] = privateKey;
        }

        return env;
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (Client is not null)
                await Client.DisposeAsync();
        }
        catch
        {
            // Swallow disposal errors — we don't want them to mask test failures.
        }

        try
        {
            if (Directory.Exists(StoragePath))
                Directory.Delete(StoragePath, recursive: true);
        }
        catch
        {
            // Best-effort cleanup on Windows where handles may linger briefly.
        }
    }
}
