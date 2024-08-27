namespace test_temporal.Models;

public class ActivityResult
{
    public bool IsValid => !string.IsNullOrEmpty(Data);
    public string Data { get; init; }
}