using System;
using System.Threading.Tasks;
using Temporalio.Common;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using test_temporal.Activity;
using test_temporal.Models;

namespace test_temporal.Workflow;

[Workflow]
public class WorkflowClass
{
    [WorkflowRun]
    public async Task<Result> CreateResult(Request request, RetryPolicy retryPolicy = null)
    {
        var defaultRetryPolicy = new RetryPolicy
        {
            InitialInterval = TimeSpan.FromSeconds(1),
            MaximumInterval = TimeSpan.FromSeconds(100),
            BackoffCoefficient = 2,
            MaximumAttempts = 1,
            // NonRetryableErrorTypes = new[] {  }
        };

        var result = new Result();

        // Create Account
        ActivityResult activityResult = null;
        try
        {
            activityResult = await Temporalio.Workflows.Workflow.ExecuteActivityAsync(
                (ActivitiesClass activities) => activities.ResultFromActivity(request),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromMinutes(5), RetryPolicy = retryPolicy ?? defaultRetryPolicy
                });
        }
        catch (ActivityFailureException ex)
        {
            if (activityResult is null or { IsValid: false })
            {
                result.AddErrorMessage($"Activity failed: {ex.Message}");
                Utils.Utils.PrintResult("With error inside Workflow", result);
                return result;
            }
        }

        result.Data = activityResult.Data;
        Utils.Utils.PrintResult("HappyPath inside Workflow", result);
        return result;
    }
}