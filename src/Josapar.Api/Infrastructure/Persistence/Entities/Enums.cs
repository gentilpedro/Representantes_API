namespace Josapar.Api.Infrastructure.Persistence.Entities;

public enum ProductBadge
{
    None,
    Offer,
    OutOfStock,
}

public enum StockLevel
{
    High,
    Medium,
    Critical,
}

public enum ClientTier
{
    Regular,
    Gold,
    UnderReview,
    Blocked,
}

public enum OrderStatus
{
    Pending,
    Sent,
    Error,
    Draft,
}

public enum LeadStatus
{
    New,
    Contacted,
    Qualified,
    Converted,
    Lost,
}

public enum VisitStatus
{
    Pending,
    InProgress,
    Completed,
}

public enum NotificationCategory
{
    Order,
    Promotion,
    Announcement,
    Launch,
}

public enum PermissionStatus
{
    Granted,
    Restricted,
}
