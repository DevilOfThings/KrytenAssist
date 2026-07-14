namespace KrytenAssist.Avalonia.Skills.Models;

public sealed record SkillResult
{
    private SkillResult(
        bool isSuccess,
        string? message,
        object? data)
    {
        IsSuccess = isSuccess;
        Message = message;
        Data = data;
    }

    public bool IsSuccess { get; }

    public string? Message { get; }

    public object? Data { get; }

    public static SkillResult Success(
        object? data = null,
        string? message = null)
    {
        return new SkillResult(
            isSuccess: true,
            message,
            data);
    }

    public static SkillResult Failure(string message)
    {
        return new SkillResult(
            isSuccess: false,
            message,
            data: null);
    }
}
