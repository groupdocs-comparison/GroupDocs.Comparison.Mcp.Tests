using GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Comparison.Mcp.IntegrationTests;

/// <summary>
/// Proves metered licensing actually engages on the real artifact.
/// </summary>
/// <remarks>
/// <para><b>These tests spend real money.</b> Every operation against a metered key consumes
/// credit, so they are excluded from the default run by their <c>Metered</c> category and are
/// triggered deliberately — weekly or on demand — rather than on every push. Use a dedicated CI
/// metered account with a capped balance, never the production key.</para>
///
/// <para>They also do not skip when unconfigured. The audit found license-dependent tests
/// silently no-opping and counting as Passed; the gating lives in the CI job, where it is
/// visible, so here a missing key is a hard failure.</para>
/// </remarks>
[Trait("Category", "Metered")]
public class MeteredLicensingTests : IClassFixture<McpServerFixture>
{
    private const string LicenseStatusTool = "get_license_status";

    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public MeteredLicensingTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        MeteredKeys.RequireConfigured();
        _fixture = fixture;
        _output = output;
    }

    // ---- Tier 1: does metered licensing engage at all? ---------------------
    // The cheapest possible proof, and the most important: it processes no document,
    // yet exercises the whole chain — env var -> SetMeteredKeyCore -> engine accepted
    // the pair -> GetConsumptionQuantity returned a real reading.

    [Fact]
    public async Task GetLicenseStatus_ReportsMeteredMode()
    {
        var response = await _fixture.Client.CallToolAsync(
            LicenseStatusTool, new Dictionary<string, object?>());

        Assert.False(response.IsError ?? false,
            $"[{_fixture.Channel}] {LicenseStatusTool} reported an error: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        Assert.Equal("metered", json.GetProperty("mode").GetString());
        Assert.True(json.GetProperty("licensed").GetBoolean(),
            "Metered is a licensed state, so `licensed` must be true.");

        Assert.True(json.TryGetProperty("consumption", out var consumption),
            "Metered mode must report a consumption block.");

        // An `error` here means the engine could not read usage — often no outbound
        // connectivity, since metered reports back to GroupDocs servers.
        Assert.False(consumption.TryGetProperty("error", out var error),
            $"Consumption reading failed: {(error.ValueKind == System.Text.Json.JsonValueKind.String ? error.GetString() : "unknown")}");

        Assert.True(consumption.TryGetProperty("quantity", out var quantity)
                    && quantity.ValueKind == System.Text.Json.JsonValueKind.Number,
            "Expected a numeric consumption quantity.");
        _output.WriteLine($"quantity={quantity.GetDecimal()}");
    }

    // ---- Tier 2: did the licence actually reach the engine? ----------------
    // Status could in principle report metered while the engine still watermarks.
    // Comparison's evaluation mode injects a notice into extracted text, so its
    // absence is direct evidence the licence took effect on the engine itself.

    [Fact]
    public async Task Compare_UnderMeteredLicence_ProducesNoEvaluationMarkers()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.AnalyzeChanges.Name,
            new Dictionary<string, object?>
            {
                ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
                ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.TargetPdf },
            });

        Assert.False(response.IsError ?? false,
            $"[{_fixture.Channel}] analyze_changes reported an error: {ToolResponse.Text(response)}");

        var text = ToolResponse.Text(response);

        Assert.DoesNotContain("evaluation version", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[Evaluation mode]", text, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Tier 3: is the work actually billed? -------------------------------

    [Fact]
    public async Task Consumption_AfterWork_IsRecorded()
    {
        var before = await ReadQuantityAsync();

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);
        await _fixture.Client.CallToolAsync(
            catalog.AnalyzeChanges.Name,
            new Dictionary<string, object?>
            {
                ["sourceFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.SourcePdf },
                ["targetFile"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.TargetPdf },
            });

        var after = await ReadQuantityAsync();

        _output.WriteLine($"consumption before={before} after={after} delta={after - before}");

        // Asserted, not merely observed. This was left observational until the reporting
        // cadence was known; a real key pair then showed the counter moving immediately,
        // with no batching delay (measured 2026-09-01). So a flat counter here means the
        // work was billed as nothing, which is what this test exists to catch.
        //
        // Only the direction is asserted, never a magnitude: the same comparison produced
        // deltas of 0.00180 and 0.00360 on different runs, so the per-operation charge is
        // not a constant worth pinning a test to.
        Assert.True(after > before,
            $"Consumption did not move after a comparison ({before} -> {after}). Either the " +
            "operation was not billed, or usage reporting has changed to a batched model — " +
            "check before assuming the licence is fine.");
    }

    // ---- Secret hygiene ----------------------------------------------------

    [Fact]
    public async Task LicenseStatus_NeverEchoesTheKeys()
    {
        var response = await _fixture.Client.CallToolAsync(
            LicenseStatusTool, new Dictionary<string, object?>());

        var text = ToolResponse.Text(response);

        // GitHub masks registered secrets in logs, but that is a backstop against
        // disclosure, not a guarantee our own code redacts. Assert the redaction.
        Assert.DoesNotContain(MeteredKeys.PrivateKey!, text, StringComparison.Ordinal);
        Assert.DoesNotContain(MeteredKeys.PublicKey!, text, StringComparison.Ordinal);
    }

    private async Task<decimal> ReadQuantityAsync()
    {
        var response = await _fixture.Client.CallToolAsync(
            LicenseStatusTool, new Dictionary<string, object?>());
        var consumption = ToolResponse.Json(response).GetProperty("consumption");
        return consumption.TryGetProperty("quantity", out var quantity)
               && quantity.ValueKind == System.Text.Json.JsonValueKind.Number
            ? quantity.GetDecimal()
            : 0m;
    }
}
