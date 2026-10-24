namespace Lyora.DAL.Entities;

public class Question
{
    public int QuestionId { get; set; }
    public string Text { get; set; } = "";
    public QuestionTopic Topic { get; set; }

    /// <summary>True when the customer can tick several options (allergies, liked notes).</summary>
    public bool AllowMultiple { get; set; }
    public int SortOrder { get; set; }
    public List<AnswerOption> Options { get; set; } = new();
}

/// <summary>One possible answer and what it means for the recommendation (only the useful fields are set).</summary>
public class AnswerOption
{
    public int OptionId { get; set; }
    public int QuestionId { get; set; }
    public string Text { get; set; } = "";
    public ScentIntensity? MapsToIntensity { get; set; }
    public ScentStyle? MapsToStyle { get; set; }
    public int? MapsToCategoryId { get; set; }
    public int? MapsToMaterialId { get; set; }
    public int? MapsToSizeMl { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

/// <summary>The customer's saved answers.</summary>
public class CustomerPreference
{
    public int PreferenceId { get; set; }
    public int CustomerId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public HashSet<int> SelectedOptionIds { get; set; } = new();
}

/// <summary>A recommended perfume with its match score and the reasons for it.</summary>
public class Recommendation
{
    public Perfume Perfume { get; set; } = new();
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public List<string> Reasons { get; set; } = new();

    public int MatchPercent => MaxScore == 0 ? 100 : (int)Math.Round(100.0 * Score / MaxScore);
}
