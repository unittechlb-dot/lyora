namespace Lyora.BLL.Services;

/// <summary>Success, or a message the page can show to the user.</summary>
public class ServiceResult
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }

    public static ServiceResult Ok() => new() { Succeeded = true };
    public static ServiceResult Fail(string error) => new() { Succeeded = false, Error = error };
}
