using System;

namespace test_temporal.Utils;

internal static class Utils
{
    public static void PrintResult(string label, dynamic result)
    {
        if (result is not null)
        {
            Console.WriteLine(label);
            Console.WriteLine($"Data: {result.Data}");
            Console.WriteLine($"IsValid: {result.IsValid}");
            if (!result.IsValid)
            {
                Console.WriteLine(result.Errors.Count > 0
                    ? $"Error: {result.Errors[0]}"
                    : "Result is not valid, but no errors found.");
            }
        }
        else
        {
            Console.WriteLine($"{label} is null");
        }
        Console.WriteLine("");
        Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-");
        Console.WriteLine("");
    }
}