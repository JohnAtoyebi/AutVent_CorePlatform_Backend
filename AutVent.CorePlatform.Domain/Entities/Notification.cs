using AutVent.CorePlatform.Domain.Enums;

namespace AutVent.CorePlatform.Domain.Entities;

public class Notification : BaseEntity
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = null!;

    /// <summary>Optional: ties the notification to a specific business context.</summary>
    public long? BusinessId { get; set; }
    public virtual Business? Business { get; set; }

    /// <summary>Optional: ties the notification to a specific store for multi-store context.</summary>
    public long? StoreId { get; set; }
    public virtual Store? Store { get; set; }

    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Deep-link path the UI can navigate to, e.g. /sales/123.</summary>
    public string? ActionUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
