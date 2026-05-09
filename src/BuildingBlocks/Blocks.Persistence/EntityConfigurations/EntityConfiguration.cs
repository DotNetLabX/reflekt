using Blocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blocks.Persistence.EntityConfigurations;

public abstract class EntityConfiguration<T, TId> : IEntityTypeConfiguration<T>
    where T : Entity<TId>
    where TId : notnull
{
    protected virtual bool HasGeneratedId => true;

    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);
        if (HasGeneratedId)
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
    }
}

public abstract class EntityConfiguration<T> : EntityConfiguration<T, int> where T : Entity<int> { }
