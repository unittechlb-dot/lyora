using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>Take Fragrance Preference Questionnaire use case.</summary>
public class QuestionnaireService
{
    private readonly QuestionnaireRepository _questionnaire;

    public QuestionnaireService(QuestionnaireRepository questionnaire)
    {
        _questionnaire = questionnaire;
    }

    public List<Question> GetQuestions() => _questionnaire.GetActiveQuestions();

    public CustomerPreference? GetPreference(int customerId) => _questionnaire.GetPreference(customerId);

    /// <summary>
    /// Checks and saves the answers. <paramref name="answers"/> = question id → the option ids the customer chose.
    /// </summary>
    public ServiceResult SaveAnswers(int customerId, IDictionary<int, List<int>> answers)
    {
        var chosen = new List<int>();

        foreach (var question in GetQuestions())
        {
            var picked = answers.TryGetValue(question.QuestionId, out var ids) ? ids.Distinct().ToList() : new List<int>();

            // Every option must belong to this question (protects against a changed form).
            if (picked.Any(id => question.Options.All(o => o.OptionId != id)))
            {
                return ServiceResult.Fail("Some answers are not valid. Please answer again.");
            }

            switch (question.Topic)
            {
                case QuestionTopic.Notes:
                    break;                                        // optional: any number of liked notes

                case QuestionTopic.Allergy:
                    if (picked.Count == 0)
                    {
                        return ServiceResult.Fail($"Please answer: {question.Text}");
                    }
                    var noAllergy = question.Options.Where(o => o.MapsToMaterialId == null).Select(o => o.OptionId);
                    if (picked.Count > 1 && picked.Intersect(noAllergy).Any())
                    {
                        return ServiceResult.Fail("Choose \"No allergies\" or the ingredients, not both.");
                    }
                    break;

                default:
                    if (picked.Count != 1)
                    {
                        return ServiceResult.Fail($"Please answer: {question.Text}");
                    }
                    break;
            }
            chosen.AddRange(picked);
        }

        _questionnaire.SaveAnswers(customerId, chosen);
        return ServiceResult.Ok();
    }
}
