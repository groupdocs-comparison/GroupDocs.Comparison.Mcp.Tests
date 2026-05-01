using System.Text;

namespace GroupDocs.Comparison.Mcp.IntegrationTests.Fixtures;

/// Builds tiny, self-contained fixture documents on disk so tests don't require
/// committed binary files. For Comparison we ship two synthetic PDFs with
/// different metadata + content so a Compare call has something to diff against.
/// Real samples committed under sample-docs/ are auto-copied alongside.
internal static class SampleDocuments
{
    // Synthetic source vs target — generated at startup with deliberately
    // different titles so a Compare call always finds something to report.
    public const string SourcePdf = "source.pdf";
    public const string TargetPdf = "target.pdf";

    public const string SourceTitle = "Original Document";
    public const string TargetTitle = "Modified Document";
    public const string KnownAuthor = "Integration Test Author";

    // Real samples committed under sample-docs/ — copied from the source folder
    // (env var or csproj-staged copy under bin/) into the test storage directory.
    public const string SamplePdf = "sample.pdf";
    public const string SampleDocx = "sample.docx";
    public const string SampleXlsx = "sample.xlsx";
    public const string SamplePptx = "sample.pptx";
    public const string SamplePng = "sample.png";

    public static IReadOnlyList<string> RealSamples { get; } = new[]
    {
        SamplePdf, SampleDocx, SampleXlsx, SamplePptx, SamplePng,
    };

    public static void WriteAll(string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, SourcePdf), BuildAuthoredPdf(
            SourceTitle, KnownAuthor,
            new[]
            {
                "Original Document Body",
                "Section A: Introduction",
                "Created on Monday morning",
                "For internal review only"
            }));
        File.WriteAllBytes(Path.Combine(directory, TargetPdf), BuildAuthoredPdf(
            TargetTitle, KnownAuthor,
            new[]
            {
                "Modified Document Body",
                "Section A: Revised Introduction",
                "Created on Tuesday afternoon",
                "For external distribution"
            }));
    }

    public static void CopyRealSamples(string targetDirectory, string? sourceDirectory)
    {
        if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return;

        Directory.CreateDirectory(targetDirectory);
        foreach (var name in RealSamples)
        {
            var src = Path.Combine(sourceDirectory, name);
            if (File.Exists(src))
                File.Copy(src, Path.Combine(targetDirectory, name), overwrite: true);
        }
    }

    public static string? ResolveSourceSampleDocs()
    {
        var env = Environment.GetEnvironmentVariable("GROUPDOCS_MCP_SAMPLE_DOCS");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(env))
            return env;

        var staged = Path.Combine(AppContext.BaseDirectory, "sample-docs");
        if (Directory.Exists(staged))
            return staged;

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, "sample-docs");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    /// Minimal PDF 1.4 with an Info dictionary AND a /Contents stream that draws
    /// visible text. Object offsets are computed at build time so the xref table
    /// is byte-accurate. Source and target PDFs differ in the supplied text lines
    /// so GroupDocs.Comparison's content diff finds real changes (not just
    /// metadata-dict differences, which the comparer ignores).
    private static byte[] BuildAuthoredPdf(string title, string author, string[] textLines)
    {
        var body = new StringBuilder();
        var offsets = new List<int>();

        void WriteObj(string obj)
        {
            offsets.Add(body.Length);
            body.Append(obj);
        }

        // Build the content stream first so we know its byte length for the dict.
        var streamBuilder = new StringBuilder();
        var y = 720;
        for (var i = 0; i < textLines.Length; i++)
        {
            var size = i == 0 ? 24 : 14;
            streamBuilder.Append($"BT /F1 {size} Tf 72 {y} Td ({EscapePdfString(textLines[i])}) Tj ET\n");
            y -= i == 0 ? 36 : 22;
        }
        var streamContent = streamBuilder.ToString().TrimEnd('\n');
        var streamLength = Encoding.ASCII.GetByteCount(streamContent);

        body.Append("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");

        WriteObj("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        WriteObj("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        WriteObj("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 6 0 R >>\nendobj\n");
        WriteObj($"4 0 obj\n<< /Title ({EscapePdfString(title)}) /Author ({EscapePdfString(author)}) /Creator (GroupDocs.Comparison.Mcp integration tests) >>\nendobj\n");
        WriteObj("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        WriteObj($"6 0 obj\n<< /Length {streamLength} >>\nstream\n{streamContent}\nendstream\nendobj\n");

        var xrefOffset = body.Length;
        body.Append("xref\n0 7\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            body.Append($"{offset:D10} 00000 n \n");

        body.Append("trailer\n<< /Size 7 /Root 1 0 R /Info 4 0 R >>\n");
        body.Append($"startxref\n{xrefOffset}\n%%EOF");

        return Encoding.ASCII.GetBytes(body.ToString());
    }

    private static string EscapePdfString(string s) =>
        s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
