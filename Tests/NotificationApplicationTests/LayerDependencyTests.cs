using System.Xml.Linq;

namespace NotificationApplicationTests;

public sealed class LayerDependencyTests
{
    [Theory]
    [InlineData("Notification.Domain", 0)]
    [InlineData("Notification.Application", 1)]
    public void CoreProjectsOnlyReferenceTheInnerLayer(string project, int expectedCount)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "PersonBlog.sln")))
            root = root.Parent;

        Assert.NotNull(root);
        var document = XDocument.Load(Path.Combine(root.FullName, "NotificationService", project, project + ".csproj"));
        Assert.Empty(document.Descendants("PackageReference"));
        Assert.Empty(document.Descendants("FrameworkReference"));
        var references = document.Descendants("ProjectReference").ToArray();
        Assert.Equal(expectedCount, references.Length);
        if (expectedCount > 0)
            Assert.Equal("../Notification.Domain/Notification.Domain.csproj",
                references[0].Attribute("Include")!.Value.Replace('\\', '/'));
    }
}
