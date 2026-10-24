using Lyora.BLL.Services;
using Lyora.DAL.Entities;

namespace Lyora.Web.ViewModels;

public class QuestionnaireViewModel
{
    public List<Question> Questions { get; set; } = new();

    /// <summary>Options already chosen (when the customer retakes the questionnaire, or after an error).</summary>
    public HashSet<int> Selected { get; set; } = new();
}

public class RecommendationsViewModel
{
    public RecommendationResult Result { get; set; } = new();

    /// <summary>The customer's answers as "question → answers" for the summary box.</summary>
    public List<(string Question, string Answer)> Answers { get; set; } = new();
    public DateTime AnsweredAt { get; set; }
}
