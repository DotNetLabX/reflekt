using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Persistence.EntityConfigurations;

public abstract class EntityConfiguration<T> : IEntityTypeConfiguration<T> where T : Entity
{
    protected virtual bool HasGeneratedId => true;

    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);
        if (HasGeneratedId)
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
    }
}
