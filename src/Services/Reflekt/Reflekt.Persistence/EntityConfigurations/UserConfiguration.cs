using Blocks.Persistence.EntityConfigurations;
using Reflekt.Domain.Users;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class UserConfiguration : EntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.ToTable("Users");
        builder.Property(u => u.Email).IsRequired().HasMaxLength(User.EmailMaxLength).UseCollation("NOCASE");
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");
        builder.Property(u => u.Name).IsRequired().HasMaxLength(User.NameMaxLength);
        builder.Property(u => u.GivenName).IsRequired().HasMaxLength(User.GivenNameMaxLength);
        builder.Property(u => u.CreatedAt).IsRequired();
    }
}
