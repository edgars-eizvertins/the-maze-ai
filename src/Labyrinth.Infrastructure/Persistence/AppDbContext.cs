using Microsoft.EntityFrameworkCore;

namespace Labyrinth.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var p = b.Entity<PlayerEntity>();
        p.HasKey(x => x.Name);
        p.Property(x => x.Name).HasMaxLength(32);
        p.Property(x => x.PinHash).IsRequired();
        p.Property(x => x.StateJson).IsRequired();
    }
}
