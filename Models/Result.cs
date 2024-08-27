using System.Collections.Generic;

namespace test_temporal.Models;

public class Result
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();

    public string Data { get; set; }

    public void AddErrorMessage(string error) => Errors.Add(error);
}