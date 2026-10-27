using System.Globalization;
using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>Administrator: Manage Discounts &amp; Promotions and Send Targeted Notifications.</summary>
public class PromotionService
{
    public static readonly string[] AgeGroups = { "18-24", "25-34", "35-44", "45+" };
    public static readonly string[] Genders = { "Female", "Male" };

    private readonly PromotionRepository _promotions;
    private readonly PerfumeRepository _perfumes;
    private readonly NotificationRepository _notifications;

    public PromotionService(PromotionRepository promotions, PerfumeRepository perfumes, NotificationRepository notifications)
    {
        _promotions = promotions;
        _perfumes = perfumes;
        _notifications = notifications;
    }

    public List<Promotion> GetPromotions() => _promotions.GetAll();

    public Promotion? GetPromotion(int promotionId) => _promotions.GetById(promotionId);

    /// <summary>Creates or updates a promotion. The discount is applied automatically between the two dates.</summary>
    public ServiceResult Save(Promotion promotion)
    {
        promotion.Title = promotion.Title.Trim();

        if (promotion.DiscountPercent < 1 || promotion.DiscountPercent > 90)
        {
            return ServiceResult.Fail("The discount must be between 1% and 90%.");
        }
        if (promotion.EndDate.Date < promotion.StartDate.Date)
        {
            return ServiceResult.Fail("The end date must be on or after the start date.");
        }
        if (promotion.PerfumeIds.Count == 0)
        {
            return ServiceResult.Fail("Choose at least one perfume.");
        }
        var existing = _perfumes.GetAllForAdmin().Select(p => p.PerfumeId).ToHashSet();
        if (promotion.PerfumeIds.Any(id => !existing.Contains(id)))
        {
            return ServiceResult.Fail("Some perfumes are not valid.");
        }

        if (promotion.PromotionId == 0)
        {
            _promotions.Create(promotion);
        }
        else
        {
            _promotions.Update(promotion);
        }
        return ServiceResult.Ok();
    }

    public void Delete(int promotionId) => _promotions.Delete(promotionId);

    /// <summary>
    /// Sends the promotion as a notification to the chosen customers.
    /// Returns the number of customers who received it.
    /// </summary>
    public (ServiceResult Result, int Sent) Send(int promotionId, PromotionAudience audience, string? ageGroup, string? gender)
    {
        var promotion = _promotions.GetById(promotionId);
        if (promotion == null)
        {
            return (ServiceResult.Fail("Promotion not found."), 0);
        }
        if (promotion.EndDate.Date < DateTime.Today)
        {
            return (ServiceResult.Fail("This promotion has ended – it cannot be sent."), 0);
        }
        if (audience == PromotionAudience.AgeGroup && !AgeGroups.Contains(ageGroup))
        {
            return (ServiceResult.Fail("Please choose an age group."), 0);
        }
        if (audience == PromotionAudience.Gender && !Genders.Contains(gender))
        {
            return (ServiceResult.Fail("Please choose a gender."), 0);
        }

        var date = promotion.EndDate.ToString("d MMMM", CultureInfo.InvariantCulture);
        var message = $"{promotion.DiscountPercent:0.##}% off {string.Join(", ", promotion.PerfumeNames)} until {date}.";
        var sent = _notifications.SendToCustomers(promotion.Title, message, audience,
            audience == PromotionAudience.AgeGroup ? ageGroup : null,
            audience == PromotionAudience.Gender ? gender : null,
            promotionId);

        return (ServiceResult.Ok(), sent);
    }
}
