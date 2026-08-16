using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class AppNotification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? Url { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = "System"; // System, Order, Reminder, MedicalInquiry

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? RelatedEntityId { get; set; } // e.g. OrderId, ReminderId

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; }
}
