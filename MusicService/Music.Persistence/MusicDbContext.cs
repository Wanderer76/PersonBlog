using Microsoft.EntityFrameworkCore;
using Music.Domain.Entities;
using Shared.Persistence;

namespace Music.Persistence
{
    public class MusicDbContext : BaseDbContext
    {
        public DbSet<Artist> Artists { get; set; }
        public DbSet<ArtistTrackLink> ArtistTrackLinks { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<TrackGenre> TrackGenres { get; set; }
        public DbSet<TrackMetadata> TrackMetadata { get; set; }
        public DbSet<ThumbnailMetadata> ThumbnailMetadata { get; set; }
        public DbSet<AvatarMetadata> AvatarMetadata { get; set; }

        public MusicDbContext(DbContextOptions<MusicDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Music");
            {
                var entity = modelBuilder.Entity<ArtistTrackLink>();
                entity.HasKey(x => new { x.TrackId, x.ArtistId });
            }
            {
                var entity = modelBuilder.Entity<TrackGenre>();
                entity.HasKey(x => new { x.TrackId, x.GenreId });
            }
            {
                var entity = modelBuilder.Entity<TrackMetadata>();
                entity.HasKey(x => x.Id);
            }
            {
                var entity = modelBuilder.Entity<ThumbnailMetadata>();
                entity.HasKey(x => x.Id);
            }
            {
                var entity = modelBuilder.Entity<Genre>();
                entity.HasData(new GenreList().GetGenres());
            }
            {
                var entity = modelBuilder.Entity<Artist>();
                entity.HasOne(x => x.AvatarMetadata)
                    .WithOne()
                    .HasForeignKey<AvatarMetadata>(x => x.Id);
            }
        }
    }
}
