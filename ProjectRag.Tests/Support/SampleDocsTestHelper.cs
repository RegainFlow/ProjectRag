namespace ProjectRag.Tests.Support;

internal static class SampleDocsTestHelper
{
    public static void CopySampleDocs(string targetDirectory)
    {
        var sourceDirectory = FindSampleDocsDirectory();

        foreach (var filePath in Directory.EnumerateFiles(sourceDirectory, "*.md"))
        {
            var targetPath = Path.Combine(targetDirectory, Path.GetFileName(filePath));
            File.Copy(filePath, targetPath, overwrite: true);
        }
    }

    private static string FindSampleDocsDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "ProjectRag.Api",
                "samples",
                "docs");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find ProjectRag.Api/samples/docs.");
    }
}