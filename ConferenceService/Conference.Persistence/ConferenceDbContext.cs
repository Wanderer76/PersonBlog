using Conference.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Conference.Persistence
{
    public class ConferenceDbContext : BaseDbContext
    {
        public DbSet<ConferenceRoom> ConferenceRooms { get; set; }

        public DbSet<Message> Messages { get; set; }
        public DbSet<ConferenceParticipant> ConferenceParticipants { get; set; }
        public DbSet<ConferenceInvitation> ConferenceInvitations { get; set; }
        public DbSet<ConferenceOutboxMessage> OutboxMessages { get; set; }

        public ConferenceDbContext(DbContextOptions<ConferenceDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Conference");
            {
                var entity = modelBuilder.Entity<ConferenceParticipant>();
                entity.HasKey(x => new { x.ConferenceRoomId, x.SessionId });
                entity.HasIndex(x => new { x.ConferenceRoomId, x.UserId }).IsUnique();
                entity.Property(x => x.UserName).HasMaxLength(200);
            }
            {
                var entity = modelBuilder.Entity<ConferenceRoom>();
                entity.HasKey(x => new { x.Id });
                entity.Property(x => x.Id).ValueGeneratedNever();
                entity.Property(x => x.PostId);
            }
            {
                var entity = modelBuilder.Entity<ConferenceInvitation>();
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ConferenceId, x.RecipientUserId }).IsUnique();
                entity.HasOne<ConferenceRoom>()
                    .WithMany()
                    .HasForeignKey(x => x.ConferenceId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
            {
                var entity = modelBuilder.Entity<Message>();
                entity.Property(x => x.MessageText).HasMaxLength(4000);
            }
        }
    }

    //public class WriteConferenceContext : IWriteRepository<IConferenceEntity>
    //{
    //    private readonly List<ConferenceRoom> rooms;
    //    public void Add(IConferenceEntity entity)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Attach(IConferenceEntity entity)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<IDbContextTransaction> BeginTransactionAsync()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task CommitAsync()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Remove(IConferenceEntity entity)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int SaveChanges()
    //    {
    //        return 0;
    //    }

    //    public Task<int> SaveChangesAsync()
    //    {
    //        return Task.FromResult(0);
    //    }
    //}

    public class ReadConferenceContext<TContext, IConferenceEntity> : IReadRepository<IConferenceEntity>
          where TContext : BaseDbContext
    {
        private readonly TContext _conferenceEntities;
        public ReadConferenceContext(TContext conferenceEntities)
        {
            _conferenceEntities = conferenceEntities;
        }
        public IQueryable<TEntity> FromSqlRaw<TEntity>(string sql, params object[] parameters) where TEntity : class, IConferenceEntity => throw new NotImplementedException();

        IQueryable<TEntity> IReadRepository<IConferenceEntity>.Get<TEntity>()
        {
            return _conferenceEntities.Set<TEntity>();
        }
    }
}
