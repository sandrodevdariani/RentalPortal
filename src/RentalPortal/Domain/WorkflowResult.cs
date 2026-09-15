namespace RentalPortal.Domain;

public sealed class WorkflowResult
{
    public bool Succeeded { get; }
    public string? Error { get; }

    private WorkflowResult(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public static WorkflowResult Ok() => new(true, null);

    public static WorkflowResult Fail(string error) => new(false, error);
}
