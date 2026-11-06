namespace TeamChat.Infrastructure.Persistence;

internal sealed class InvitationConfiguration(string tablePrefix) : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable(tablePrefix + "Invitations");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.SenderId).HasMaxLength(ChatUserId.MaxLength).IsRequired();
        builder.Property(i => i.RecipientId).HasMaxLength(ChatUserId.MaxLength).IsRequired();
        builder.Property(i => i.Message).HasMaxLength(Invitation.MessageMaxLength);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(i => new { i.SenderId, i.RecipientId })
            .IsUnique()
            .HasFilter("\"Status\" = 'Pending'");
        builder.HasIndex(i => new { i.RecipientId, i.Status });
    }
}

internal sealed class ContactConfiguration(string tablePrefix) : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable(tablePrefix + "Contacts");
        builder.HasKey(c => new { c.FirstUserId, c.SecondUserId });

        builder.Property(c => c.FirstUserId).HasMaxLength(ChatUserId.MaxLength);
        builder.Property(c => c.SecondUserId).HasMaxLength(ChatUserId.MaxLength);
        builder.HasIndex(c => c.SecondUserId);
    }
}
