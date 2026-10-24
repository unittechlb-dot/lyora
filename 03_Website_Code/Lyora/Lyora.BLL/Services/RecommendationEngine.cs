using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>
/// Turns the customer's questionnaire answers into perfume recommendations (Class Diagram: RecommendationEngine).
///  1. ExcludeAllergensAndBudget – removes perfumes whose BOM contains an allergen, or outside the budget.
///  2. RankByMatch – points: intensity 2, style 2, family 2, each liked note in the BOM 1, occasion 1, size 1.
///  3. If nothing matches, the best-selling allowed perfumes are suggested instead.
/// </summary>
public class RecommendationEngine
{
    public const int MaxResults = 6;

    private readonly PerfumeRepository _perfumes;
    private readonly QuestionnaireRepository _questionnaire;

    public RecommendationEngine(PerfumeRepository perfumes, QuestionnaireRepository questionnaire)
    {
        _perfumes = perfumes;
        _questionnaire = questionnaire;
    }

    public RecommendationResult GenerateRecommendations(CustomerPreference preference)
    {
        var options = _questionnaire.GetActiveQuestions()
            .SelectMany(q => q.Options.Select(o => (Question: q, Option: o)))
            .Where(x => preference.SelectedOptionIds.Contains(x.Option.OptionId))
            .ToList();

        AnswerOption? One(QuestionTopic topic) => options.FirstOrDefault(x => x.Question.Topic == topic).Option;
        List<AnswerOption> Many(QuestionTopic topic) => options.Where(x => x.Question.Topic == topic).Select(x => x.Option).ToList();

        var profile = new MatchProfile
        {
            AllergenIds = Many(QuestionTopic.Allergy).Where(o => o.MapsToMaterialId != null).Select(o => o.MapsToMaterialId!.Value).ToHashSet(),
            LikedNotes = Many(QuestionTopic.Notes).Where(o => o.MapsToMaterialId != null).ToList(),
            Intensity = One(QuestionTopic.Intensity)?.MapsToIntensity,
            Style = One(QuestionTopic.Style)?.MapsToStyle,
            Family = One(QuestionTopic.Family),
            OccasionIntensity = One(QuestionTopic.Occasion)?.MapsToIntensity,
            SizeMl = One(QuestionTopic.Size)?.MapsToSizeMl,
            Budget = One(QuestionTopic.Budget)
        };

        var perfumes = _perfumes.Search(null, null, null, null).Where(p => p.IsAvailable).ToList();
        var bom = _perfumes.GetBomMaterialsOfActivePerfumes();

        var (allowed, hiddenByAllergy, hiddenByBudget) = ExcludeAllergensAndBudget(perfumes, bom, profile);
        var ranked = RankByMatch(allowed, bom, profile);

        var result = new RecommendationResult { HiddenByAllergy = hiddenByAllergy, HiddenByBudget = hiddenByBudget };
        result.Recommendations = ranked.Where(r => r.Score > 0).Take(MaxResults).ToList();

        if (result.Recommendations.Count == 0)
        {
            var sold = _perfumes.GetBottlesSold();
            result.IsFallback = true;
            result.Recommendations = ranked
                .OrderByDescending(r => sold.GetValueOrDefault(r.Perfume.PerfumeId))
                .ThenBy(r => r.Perfume.Name)
                .Take(MaxResults)
                .ToList();
        }
        return result;
    }

    private static (List<Perfume> Allowed, int HiddenByAllergy, int HiddenByBudget) ExcludeAllergensAndBudget(
        List<Perfume> perfumes, Dictionary<int, HashSet<int>> bom, MatchProfile profile)
    {
        var allowed = new List<Perfume>();
        int byAllergy = 0, byBudget = 0;

        foreach (var perfume in perfumes)
        {
            var materials = bom.GetValueOrDefault(perfume.PerfumeId) ?? new HashSet<int>();
            if (materials.Overlaps(profile.AllergenIds))
            {
                byAllergy++;
            }
            else if (profile.Budget != null &&
                     ((profile.Budget.MinPrice != null && perfume.Price < profile.Budget.MinPrice) ||
                      (profile.Budget.MaxPrice != null && perfume.Price > profile.Budget.MaxPrice)))
            {
                byBudget++;
            }
            else
            {
                allowed.Add(perfume);
            }
        }
        return (allowed, byAllergy, byBudget);
    }

    private static List<Recommendation> RankByMatch(List<Perfume> perfumes, Dictionary<int, HashSet<int>> bom, MatchProfile profile)
    {
        var maxScore = (profile.Intensity != null ? 2 : 0)
                     + (profile.Style != null ? 2 : 0)
                     + (profile.Family?.MapsToCategoryId != null ? 2 : 0)
                     + profile.LikedNotes.Count
                     + (profile.OccasionIntensity != null ? 1 : 0)
                     + (profile.SizeMl != null ? 1 : 0);

        var list = new List<Recommendation>();
        foreach (var perfume in perfumes)
        {
            var r = new Recommendation { Perfume = perfume, MaxScore = maxScore };
            var materials = bom.GetValueOrDefault(perfume.PerfumeId) ?? new HashSet<int>();

            if (profile.Intensity == perfume.Intensity) { r.Score += 2; r.Reasons.Add($"{perfume.Intensity} scent"); }
            if (profile.Style == perfume.Style) { r.Score += 2; r.Reasons.Add($"{perfume.Style} style"); }
            if (profile.Family?.MapsToCategoryId == perfume.CategoryId) { r.Score += 2; r.Reasons.Add($"{perfume.CategoryName} family"); }
            foreach (var note in profile.LikedNotes.Where(n => materials.Contains(n.MapsToMaterialId!.Value)))
            {
                r.Score += 1;
                r.Reasons.Add($"Contains {note.Text.ToLowerInvariant()}");
            }
            if (profile.OccasionIntensity == perfume.Intensity)
            {
                r.Score += 1;
                r.Reasons.Add(perfume.Intensity == ScentIntensity.Light ? "Good for daytime" : "Good for evening");
            }
            if (profile.SizeMl == perfume.SizeMl) { r.Score += 1; r.Reasons.Add($"{perfume.SizeMl} ml bottle"); }

            list.Add(r);
        }
        return list.OrderByDescending(r => r.Score).ThenBy(r => r.Perfume.Price).ToList();
    }

    private class MatchProfile
    {
        public HashSet<int> AllergenIds { get; init; } = new();
        public List<AnswerOption> LikedNotes { get; init; } = new();
        public ScentIntensity? Intensity { get; init; }
        public ScentStyle? Style { get; init; }
        public AnswerOption? Family { get; init; }
        public ScentIntensity? OccasionIntensity { get; init; }
        public int? SizeMl { get; init; }
        public AnswerOption? Budget { get; init; }
    }
}

public class RecommendationResult
{
    public List<Recommendation> Recommendations { get; set; } = new();

    /// <summary>True when nothing matched and best-sellers are shown instead.</summary>
    public bool IsFallback { get; set; }
    public int HiddenByAllergy { get; set; }
    public int HiddenByBudget { get; set; }
}
