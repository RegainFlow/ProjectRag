using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectRag.Domain.Entities;

namespace ProjectRag.Infrastructure.Configurations.Persistence;

internal sealed class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourcePath).HasMaxLength(2048);
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
    }
}
