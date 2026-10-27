using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

/// <summary>Notifications of a user (every query is filtered by the user's id).</summary>
public class NotificationRepository
{
    private readonly Db _db;

    public NotificationRepository(Db db)
    {
        _db = db;
    }

    public List<Notification> GetForUser(int userId, int count = 100)
    {
        const string sql = @"
        SELECT TOP (@Count) NotificationId, UserId, Title, Message, Type, SentAt, IsRead
        FROM dbo.Notifications
        WHERE UserId = @UserId
        ORDER BY IsRead, SentAt DESC, NotificationId DESC;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Count", count);

        var list = new List<Notification>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Notification
            {
                NotificationId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Title = reader.GetString(2),
                Message = reader.GetString(3),
                Type = Enum.Parse<NotificationType>(reader.GetString(4)),
                SentAt = reader.GetDateTime(5),
                IsRead = reader.GetBoolean(6)
            });
        }
        return list;
    }

    public int CountUnread(int userId)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.Notifications WHERE UserId = @UserId AND IsRead = 0;", connection);
        command.Parameters.AddWithValue("@UserId", userId);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int CountUnread(int userId, NotificationType type)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Notifications WHERE UserId = @UserId AND IsRead = 0 AND Type = @Type;", connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Type", type.ToString());
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void MarkRead(int userId, int notificationId)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(
            "UPDATE dbo.Notifications SET IsRead = 1 WHERE NotificationId = @Id AND UserId = @UserId;", connection);
        command.Parameters.AddWithValue("@Id", notificationId);
        command.Parameters.AddWithValue("@UserId", userId);
        command.ExecuteNonQuery();
    }

    public void MarkAllRead(int userId)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("UPDATE dbo.Notifications SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0;", connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.ExecuteNonQuery();
    }

    public void Create(int userId, string title, string message, NotificationType type)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(
            "INSERT INTO dbo.Notifications (UserId, Title, Message, Type) VALUES (@UserId, @Title, @Message, @Type);", connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Title", title);
        command.Parameters.AddWithValue("@Message", message);
        command.Parameters.AddWithValue("@Type", type.ToString());
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Sends a promotion notification to the chosen customers in one INSERT … SELECT.
    /// Returns how many customers received it.
    /// </summary>
    public int SendToCustomers(string title, string message, PromotionAudience audience, string? ageGroup, string? gender, int promotionId)
    {
        const string sql = @"
        INSERT INTO dbo.Notifications (UserId, Title, Message, Type)
        SELECT c.CustomerId, @Title, @Message, N'Promotion'
        FROM dbo.Customers c
        CROSS APPLY (SELECT DATEDIFF(YEAR, c.DateOfBirth, GETDATE())
                            - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, c.DateOfBirth, GETDATE()), c.DateOfBirth) > GETDATE()
                                   THEN 1 ELSE 0 END AS Age) a
        WHERE @Audience = N'AllCustomers'
           OR (@Audience = N'Gender' AND c.Gender = @Gender)
           OR (@Audience = N'AgeGroup' AND c.DateOfBirth IS NOT NULL AND
                 CASE WHEN a.Age < 25 THEN N'18-24' WHEN a.Age < 35 THEN N'25-34'
                      WHEN a.Age < 45 THEN N'35-44' ELSE N'45+' END = @AgeGroup)
           OR (@Audience = N'BoughtPromotedPerfumes' AND EXISTS (
                 SELECT 1
                 FROM dbo.Orders o
                 JOIN dbo.OrderItems oi        ON oi.OrderId = o.OrderId
                 JOIN dbo.PromotionPerfumes pp ON pp.PerfumeId = oi.PerfumeId
                 WHERE o.CustomerId = c.CustomerId AND o.Status <> N'Cancelled' AND pp.PromotionId = @PromotionId));";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Title", title);
        command.Parameters.AddWithValue("@Message", message);
        command.Parameters.AddWithValue("@Audience", audience.ToString());
        command.Parameters.AddWithValue("@AgeGroup", (object?)ageGroup ?? DBNull.Value);
        command.Parameters.AddWithValue("@Gender", (object?)gender ?? DBNull.Value);
        command.Parameters.AddWithValue("@PromotionId", promotionId);
        return command.ExecuteNonQuery();
    }
}
