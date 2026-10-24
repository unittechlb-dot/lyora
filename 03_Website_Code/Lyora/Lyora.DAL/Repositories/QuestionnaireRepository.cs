using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

public class QuestionnaireRepository
{
    private readonly Db _db;

    public QuestionnaireRepository(Db db)
    {
        _db = db;
    }

    /// <summary>The active questions in order, each with its options.</summary>
    public List<Question> GetActiveQuestions()
    {
        const string sql = @"
        SELECT QuestionId, Text, Topic, AllowMultiple, SortOrder
        FROM dbo.Questions
        WHERE IsActive = 1
        ORDER BY SortOrder, QuestionId;

        SELECT o.OptionId, o.QuestionId, o.Text, o.MapsToIntensity, o.MapsToStyle, o.MapsToCategoryId,
               o.MapsToMaterialId, o.MapsToSizeMl, o.MinPrice, o.MaxPrice
        FROM dbo.AnswerOptions o
        JOIN dbo.Questions q ON q.QuestionId = o.QuestionId
        WHERE q.IsActive = 1
        ORDER BY o.QuestionId, o.SortOrder, o.OptionId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        var questions = new List<Question>();
        while (reader.Read())
        {
            questions.Add(new Question
            {
                QuestionId = reader.GetInt32(0),
                Text = reader.GetString(1),
                Topic = Enum.Parse<QuestionTopic>(reader.GetString(2)),
                AllowMultiple = reader.GetBoolean(3),
                SortOrder = reader.GetInt32(4)
            });
        }

        reader.NextResult();
        while (reader.Read())
        {
            var option = new AnswerOption
            {
                OptionId = reader.GetInt32(0),
                QuestionId = reader.GetInt32(1),
                Text = reader.GetString(2),
                MapsToIntensity = reader.IsDBNull(3) ? null : Enum.Parse<ScentIntensity>(reader.GetString(3)),
                MapsToStyle = reader.IsDBNull(4) ? null : Enum.Parse<ScentStyle>(reader.GetString(4)),
                MapsToCategoryId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                MapsToMaterialId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                MapsToSizeMl = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                MinPrice = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                MaxPrice = reader.IsDBNull(9) ? null : reader.GetDecimal(9)
            };
            questions.First(q => q.QuestionId == option.QuestionId).Options.Add(option);
        }
        return questions;
    }

    /// <summary>The customer's saved answers, or null if the questionnaire was never answered.</summary>
    public CustomerPreference? GetPreference(int customerId)
    {
        const string sql = @"
        SELECT PreferenceId, CustomerId, UpdatedAt FROM dbo.CustomerPreferences WHERE CustomerId = @CustomerId;

        SELECT ca.OptionId
        FROM dbo.CustomerAnswers ca
        JOIN dbo.CustomerPreferences cp ON cp.PreferenceId = ca.PreferenceId
        WHERE cp.CustomerId = @CustomerId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CustomerId", customerId);
        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }
        var preference = new CustomerPreference
        {
            PreferenceId = reader.GetInt32(0),
            CustomerId = reader.GetInt32(1),
            UpdatedAt = reader.GetDateTime(2)
        };
        reader.NextResult();
        while (reader.Read())
        {
            preference.SelectedOptionIds.Add(reader.GetInt32(0));
        }
        return preference;
    }

    /// <summary>Replaces the customer's answers in one transaction (creates the preference the first time).</summary>
    public void SaveAnswers(int customerId, IReadOnlyCollection<int> optionIds)
    {
        const string sql = @"
        DECLARE @PreferenceId INT = (SELECT PreferenceId FROM dbo.CustomerPreferences WHERE CustomerId = @CustomerId);
        IF @PreferenceId IS NULL
        BEGIN
            INSERT INTO dbo.CustomerPreferences (CustomerId) VALUES (@CustomerId);
            SET @PreferenceId = SCOPE_IDENTITY();
        END
        ELSE
            UPDATE dbo.CustomerPreferences SET UpdatedAt = SYSDATETIME() WHERE PreferenceId = @PreferenceId;

        DELETE FROM dbo.CustomerAnswers WHERE PreferenceId = @PreferenceId;
        SELECT @PreferenceId;";

        using var connection = _db.Open();
        using var transaction = connection.BeginTransaction();

        int preferenceId;
        using (var command = new SqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("@CustomerId", customerId);
            preferenceId = Convert.ToInt32(command.ExecuteScalar());
        }

        foreach (var optionId in optionIds)
        {
            using var insert = new SqlCommand(
                "INSERT INTO dbo.CustomerAnswers (PreferenceId, OptionId) VALUES (@PreferenceId, @OptionId);", connection, transaction);
            insert.Parameters.AddWithValue("@PreferenceId", preferenceId);
            insert.Parameters.AddWithValue("@OptionId", optionId);
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}
