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
    public void PublishWorkflow_PushesDockerHubAndGhcr()
    {
        var workflow = ReadRepositoryFile(".github/workflows/dotnet-core.yml");
        Assert.Contains("packages: write", workflow, StringComparison.Ordinal);
        Assert.Contains("registry: ghcr.io", workflow, StringComparison.Ordinal);
        Assert.Contains("ghcr_image=\"ghcr.io/${GITHUB_REPOSITORY,,}\"", workflow, StringComparison.Ordinal);
        Assert.Contains("password: ${{ secrets.GITHUB_TOKEN }}", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Docker_ImageContainsRepositoryMetadata()
    {
        var dockerfile = ReadRepositoryFile("Dockerfile");
        Assert.Contains("org.opencontainers.image.source=\"https://github.com/sanjusss/aliyun-ddns\"", dockerfile, StringComparison.Ordinal);
        Assert.Contains("org.opencontainers.image.licenses=\"BSD-3-Clause\"", dockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void Docker_SdkRunsOnNativeBuildPlatform()
    {
        var dockerfile = ReadRepositoryFile("Dockerfile");
        Assert.Contains("FROM --platform=$BUILDPLATFORM ", dockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void Docker_BaseImagesFollowTagsAndDependabotSkipsThem()
    {
        foreach (var dockerfile in new[] { "Dockerfile", "Dockerfile.alpine" })
        {
            var fromLines = ReadRepositoryFile(dockerfile).Split('\n')
                .Where(line => line.StartsWith("FROM ", StringComparison.Ordinal)).ToArray();
            Assert.Equal(2, fromLines.Length);
            Assert.All(fromLines, line => Assert.DoesNotContain("@sha256:", line, StringComparison.Ordinal));
        }

        var dependabot = ReadRepositoryFile(".github/dependabot.yml");
        Assert.Contains("package-ecosystem: docker", dependabot, StringComparison.Ordinal);
        Assert.Contains("dependency-name: \"dotnet/*\"", dependabot, StringComparison.Ordinal);
        // 不带 update-types 才表示忽略 .NET 镜像的全部更新，补丁和 digest 更新也不会开 PR。
        Assert.DoesNotContain("version-update:", dependabot, StringComparison.Ordinal);
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
