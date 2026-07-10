namespace Infrastructure.Data.Configurations;

internal sealed class PhoneVerificationOtpConfiguration
    : IEntityTypeConfiguration<PhoneVerificationOtp>
{
    public void Configure(EntityTypeBuilder<PhoneVerificationOtp> builder)
    {
        builder.ToTable("PhoneVerificationOtps");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodeHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<PhoneVerificationOtp>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
