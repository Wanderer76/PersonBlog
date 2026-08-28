using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PlayListService.Domain.Entities;
using PlayListService.Persistence;

namespace PlayListServiceTests;

public sealed class PlayListPersistenceTests
{
    [Fact]
    public void CurrentModelMatchesLastMigration()
    {
        using var context = CreateContext();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var modelDiffer = context.GetService<IMigrationsModelDiffer>();
        var runtimeInitializer = context.GetService<IModelRuntimeInitializer>();
        var currentModel = context.GetService<IDesignTimeModel>().Model;
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;

        Assert.NotNull(snapshotModel);
        snapshotModel = runtimeInitializer.Initialize(snapshotModel, designTime: true);

        var differences = modelDiffer.GetDifferences(
            snapshotModel.GetRelationalModel(),
            currentModel.GetRelationalModel());

        Assert.Empty(differences);
    }

    [Fact]
    public void StagedThumbnailCanExistBeforePlaylistIsCreated()
    {
        using var context = CreateContext();
        var fileEntity = context.Model.FindEntityType(typeof(PlayListFile));

        Assert.NotNull(fileEntity);
        var playlistForeignKey = Assert.Single(
            fileEntity.GetForeignKeys(),
            x => x.PrincipalEntityType.ClrType == typeof(PlayList));
        Assert.False(playlistForeignKey.IsRequired);
    }

    [Fact]
    public void ThumbnailMigrationCanGenerateUpgradeScript()
    {
        using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();

        var script = migrator.GenerateScript(
            "20251224061930_Init",
            "20260828000000_AlignThumbnailModel");

        Assert.Contains("ALTER COLUMN \"ThumbnailId\" TYPE uuid", script);
        Assert.Contains("CREATE TABLE \"PlayList\".\"PlayListFile\"", script);
    }

    private static PlayListDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlayListDbContext>()
            .UseNpgsql("Host=localhost;Database=playlist-tests;Username=test;Password=test")
            .Options;

        return new PlayListDbContext(options);
    }
}
