using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;

namespace Profile.Persistence;

public class ProfileDbContext : BaseDbContext
{
    public DbSet<AppProfile> Profiles { get; set; }
    public DbSet<Subscriptions> Subscriptions { get; set; }
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
    //public DbSet<FileMetadata> VideoMetadata { get; set; }
    public DbSet<VideoUploadEvent> VideoUploadEvents { get; set; }
    public DbSet<VideoMetadata> VideoMetadata { get; set; }
    public DbSet<CombineFileChunksEvent> CombineFileChunksEvents { get; set; }

    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("Profile");
        {
            {
                var entity = modelBuilder.Entity<AppProfile>();
                entity.HasIndex(x => new { x.UserId, x.IsDeleted }).IsUnique();

                entity.HasData(new[]
                {
                    new AppProfile
                    {
                        Id = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d2e"),
                        UserId = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                        Email = "ateplinsky@mail.ru",
                        ProfileState = ProfileState.Active,
                        FirstName = "Артём",
                        SurName="Теплинский",
                    }
                });
            }
            {
                var entity = modelBuilder.Entity<Blog>();
                entity.HasIndex(x => x.ProfileId).IsUnique();
                entity.HasData(new[]
               {
                    new Blog
                    {
                        Id = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                        Title = "Тест",
                        ProfileId =Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d2e"),
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                });

            }
            {
                var entity = modelBuilder.Entity<Subscriptions>();
                entity.HasIndex(x => new { x.ProfileId, x.BlogId }).IsUnique();
            }
            {
                var entity = modelBuilder.Entity<Post>();

                //entity.HasData(new[]
                //{
                //    new Post(Guid.Parse("42c113cc-b4a7-41b5-b0c8-2e059087124f"),Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                //    PostType.Video,DateTimeOffset.Now,null,Guid.Parse("5ce1c7bb-d7e7-497c-8a20-2b8c503d4426"),false,"")
                //});
            }
            {
                var entity = modelBuilder.Entity<VideoMetadata>();

                entity.HasIndex(x => new
                {
                    x.PostId,
                    x.Resolution,
                    x.ContentType
                }).IsUnique();

                //entity.HasData(new[]
                //{
                //    new FileMetadata
                //    {
                //        Id = Guid.Parse("5ce1c7bb-d7e7-497c-8a20-2b8c503d4426"),
                //      //  FileId = Guid.Parse("5ce1c7bb-d7e7-497c-8a20-2b8c503d4426"),
                //        FileExtension=".mp4",
                //        ContentType = "video/mp4",
                //        Length=18851336,
                //        Name="sdjfoasdfop.mp4",
                //        CreatedAt = DateTimeOffset.UtcNow,
                //        PostId = Guid.Parse("42c113cc-b4a7-41b5-b0c8-2e059087124f"),
                //        ObjectName = "video-22d92e9a-4ff1-4c58-8d07-2deb94e78bbb-0",

                //    }
                //});
            }
            {
                var entity = modelBuilder.Entity<VideoUploadEvent>();
                // entity.HasData(new[]
                //{
                //     new VideoUploadEvent
                //     {
                //         Id = Guid.Parse("5ce1c7bb-d7e7-497c-8a20-2b8c503d4427"),
                //         ObjectName="video-fa312c68-5c1d-49cd-a63e-2e8fbdbcdb31-0",
                //         FileUrl = "http://127.0.0.1:9000/09f3c24e-6e70-48ea-a5c5-60727af95d2e/video-fa312c68-5c1d-49cd-a63e-2e8fbdbcdb31-0?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=minioadmin%2F20241227%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241227T083304Z&X-Amz-Expires=604800&X-Amz-SignedHeaders=host&X-Amz-Signature=60b2b9963717a47fc10ac3bbc3cf5975f929ed9ece4c937515a894ba10752fa9",
                //         IsCompleted=false,
                //         UserProfileId = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d2e"),
                //         FileId = Guid.Parse("5ce1c7bb-d7e7-497c-8a20-2b8c503d4426"),
                //     }
                // });
            }
        }
    }
}