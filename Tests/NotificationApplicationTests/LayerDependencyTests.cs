using System.Xml.Linq;

namespace NotificationApplicationTests;

public sealed class LayerDependencyTests
{
    [Theory]
    [InlineData("Notification.Domain")]
    [InlineData("Notification.Application", "../Notification.Domain/Notification.Domain.csproj",
        "../../Common/Infrastructure/Infrastructure.csproj",
        "../../Common/Shared/Shared.csproj")]
    public void CoreProjectsOnlyReferenceAllowedLayers(string project, params string[] expectedReferences)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "PersonBlog.sln")))
            root = root.Parent;

        Assert.NotNull(root);
        var document = XDocument.Load(Path.Combine(root.FullName, "NotificationService", project, project + ".csproj"));
        Assert.Empty(document.Descendants("PackageReference"));
        Assert.Empty(document.Descendants("FrameworkReference"));
        var references = document.Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")!.Value.Replace('\\', '/'))
            .ToArray();
        Assert.Equal(expectedReferences, references);
    }
}
