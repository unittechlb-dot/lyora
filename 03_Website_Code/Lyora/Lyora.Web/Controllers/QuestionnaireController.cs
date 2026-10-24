using Lyora.BLL.Services;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>
/// Take Fragrance Preference Questionnaire → Generate Recommendations → View Recommendations.
/// Customers only: the answers are saved in the customer's profile.
/// </summary>
public class QuestionnaireController : CustomerControllerBase
{
    private readonly QuestionnaireService _questionnaireService;
    private readonly RecommendationEngine _recommendationEngine;

    public QuestionnaireController(QuestionnaireService questionnaireService, RecommendationEngine recommendationEngine)
    {
        _questionnaireService = questionnaireService;
        _recommendationEngine = recommendationEngine;
    }

    /// <summary>Shows the recommendations if the customer already answered, otherwise the questions.</summary>
    public IActionResult Index() =>
        _questionnaireService.GetPreference(CustomerId) == null
            ? RedirectToAction(nameof(Take))
            : RedirectToAction(nameof(Results));

    [HttpGet]
    public IActionResult Take()
    {
        var preference = _questionnaireService.GetPreference(CustomerId);
        return View(new QuestionnaireViewModel
        {
            Questions = _questionnaireService.GetQuestions(),
            Selected = preference?.SelectedOptionIds ?? new HashSet<int>()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Take(IFormCollection form)
    {
        var questions = _questionnaireService.GetQuestions();

        // Each question posts its chosen option ids in a field named "q{QuestionId}".
        var answers = new Dictionary<int, List<int>>();
        foreach (var question in questions)
        {
            answers[question.QuestionId] = form[$"q{question.QuestionId}"]
                .Select(v => int.TryParse(v, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        }

        var result = _questionnaireService.SaveAnswers(CustomerId, answers);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(new QuestionnaireViewModel
            {
                Questions = questions,
                Selected = answers.Values.SelectMany(v => v).ToHashSet()
            });
        }

        return RedirectToAction(nameof(Results));
    }

    public IActionResult Results()
    {
        var preference = _questionnaireService.GetPreference(CustomerId);
        if (preference == null)
        {
            return RedirectToAction(nameof(Take));
        }

        var answers = _questionnaireService.GetQuestions()
            .Select(q => (q.Text, string.Join(", ", q.Options.Where(o => preference.SelectedOptionIds.Contains(o.OptionId)).Select(o => o.Text))))
            .Where(a => a.Item2.Length > 0)
            .ToList();

        return View(new RecommendationsViewModel
        {
            Result = _recommendationEngine.GenerateRecommendations(preference),
            Answers = answers,
            AnsweredAt = preference.UpdatedAt
        });
    }
}
