using Xunit;

namespace AliyunDdns.Tests;

public sealed class RepositoryPolicyTests
{
    [Fact]
    public void PublishWorkflow_RestrictsReferencesAndSerializesReleases()
    {
        var workflow = ReadRepositoryFile(".github/workflows/dotnet-core.yml");
        Assert.Contains("github.ref == 'refs/heads/master'", workflow, StringComparison.Ordinal);
        Assert.Contains("environment: production", workflow, StringComparison.Ordinal);
        Assert.Contains("group: aliyun-ddns-release", workflow, StringComparison.Ordinal);
        Assert.Contains("PUBLISH_LATEST", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("tags=\"$image:$version,$image:latest\"", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void VulnerabilityAudit_IsConfiguredToFail()
    {
        var props = ReadRepositoryFile("Directory.Build.props");
        Assert.Contains("<NuGetAuditMode>all</NuGetAuditMode>", props, StringComparison.Ordinal);
        Assert.Contains("NU1901;NU1902;NU1903;NU1904", props, StringComparison.Ordinal);
        Assert.Contains("dotnet list aliyun-ddns.sln package --vulnerable --include-transitive",
            ReadRepositoryFile(".github/workflows/dotnet-core.yml"), StringComparison.Ordinal);
    }

    [Fact]
    public void Docker_SdkRunsOnNativeBuildPlatform()
    {
        var dockerfile = ReadRepositoryFile("Dockerfile");
        Assert.Contains("FROM --platform=$BUILDPLATFORM ", dockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void Docker_BaseImagesArePinnedAndAutomaticallyUpdated()
    {
        var fromLines = ReadRepositoryFile("Dockerfile").Split('\n')
            .Where(line => line.StartsWith("FROM ", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, fromLines.Length);
        Assert.All(fromLines, line => Assert.Contains("@sha256:", line, StringComparison.Ordinal));
        var dependabot = ReadRepositoryFile(".github/dependabot.yml");
        Assert.Contains("package-ecosystem: docker", dependabot, StringComparison.Ordinal);
        Assert.Contains("interval: weekly", dependabot, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "aliyun-ddns.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
