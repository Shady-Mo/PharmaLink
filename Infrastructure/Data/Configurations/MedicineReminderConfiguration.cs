using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class MedicineReminderConfiguration : IEntityTypeConfiguration<MedicineReminder>
{
    public void Configure(EntityTypeBuilder<MedicineReminder> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.MedicineName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.ReminderTimesJson).IsRequired();
        builder.HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId);
        builder.HasMany(r => r.Logs).WithOne(l => l.Reminder).HasForeignKey(l => l.ReminderId);
    }
}
