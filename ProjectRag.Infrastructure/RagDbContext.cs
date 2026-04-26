using Microsoft.EntityFrameworkCore;
using ProjectRag.Domain.Entities;

namespace ProjectRag.Infrastructure;

public sealed class RagDbContext : DbContext
{
    public RagDbContext(DbContextOptions<RagDbContext> options) :
        base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RagDbContext).Assembly);
    }
}
