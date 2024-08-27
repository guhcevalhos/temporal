#region Using
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Temporalio.Workflows;
using Temporalio.Common;
using Temporalio.Exceptions;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Testing;
using Temporalio.Worker;
using Moq;
#endregion

#region Run
var env = await WorkflowEnvironment
    .StartLocalAsync(new WorkflowEnvironmentStartLocalOptions()
    {
        UI = false
    });

var tests = new Tests(env);
Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-=- Starting Happy Path =-=-=-=-=-=-=-=-=-=-=-=-=-");
var happyPath = await tests.HappyPath();
Utils.PrintResult("Happy Path after finishing", happyPath);

Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-=- Starting WithError =-=-=-=-=-=-=-=-=-=-=-=-=-");
var withError = await tests.WithError();
Utils.PrintResult("With Error after finishing", withError);

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
#endregion

#region Tests definition
internal class Tests
{

    private ActivitiesClass _activitiesClass;
    private WorkflowEnvironment Env { get; set; }
    private static readonly Mock<IService> ServiceMock = new();

    public Tests(WorkflowEnvironment env)
    {
        Env = env;
    }

    public async Task<Result> HappyPath()
    {
        // Arrange
        ServiceMock.Setup(service =>
                service.GetData())
            .ReturnsAsync("Good Data");
        var request = new Request() { Data = "Good Data" };

        _activitiesClass = new ActivitiesClass(ServiceMock.Object);

        // Act
        // Create a worker
        using var worker = new TemporalWorker(
            Env.Client,
            new TemporalWorkerOptions($"task-queue-{Guid.NewGuid()}")
                .AddWorkflow<WorkflowClass>()
                .AddAllActivities(_activitiesClass)
        );

        Result result = new Result();
        await worker.ExecuteAsync(async () =>
        {
            // Execute the workflow and confirm the result
            result = await Env.Client.ExecuteWorkflowAsync(
                (WorkflowClass wf) => wf.CreateResult(request, new RetryPolicy
                {
                    InitialInterval = TimeSpan.FromSeconds(1),
                    MaximumInterval = TimeSpan.FromSeconds(100),
                    BackoffCoefficient = 2,
                    MaximumAttempts = 1,
                    // NonRetryableErrorTypes = new[] {  }
                }),
                new(id: $"wf-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!));
        });

        return result;
    }

    public async Task<Result> WithError()
    {
        // Arrange
        ServiceMock.Setup(service =>
                service.GetData())
            .ThrowsAsync(new HttpRequestException("There was an error"));
        var request = new Request() { Data = "Bad Data" };

        _activitiesClass = new ActivitiesClass(ServiceMock.Object);

        // Act
        // Create a worker
        using var worker = new TemporalWorker(
            Env.Client,
            new TemporalWorkerOptions($"task-queue-{Guid.NewGuid()}")
                .AddWorkflow<WorkflowClass>()
                .AddAllActivities(_activitiesClass)
        );

        Result result = new Result();
        await worker.ExecuteAsync(async () =>
        {
            // Execute the workflow and confirm the result
            result = await Env.Client.ExecuteWorkflowAsync(
                (WorkflowClass wf) => wf.CreateResult(request, new RetryPolicy
                {
                    InitialInterval = TimeSpan.FromSeconds(1),
                    MaximumInterval = TimeSpan.FromSeconds(100),
                    BackoffCoefficient = 2,
                    MaximumAttempts = 1,
                    // NonRetryableErrorTypes = new[] {  }
                }),
                new(id: $"wf-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!));
        });

        return result;
    }
}
#endregion

#region Temporal
public class ActivitiesClass
{
    private IService _service;

    public ActivitiesClass(IService service)
    {
        _service = service;
    }

    [Activity]
    public async Task<ActivityResult> ResultFromActivity(Request request)
    {
        var dataFromService = await _service.GetData();

        return dataFromService == request.Data
            ? new ActivityResult() { Data = dataFromService }
            : new ActivityResult();
    }
}

[Workflow]
public class WorkflowClass
{
    [WorkflowRun]
    public async Task<Result> CreateResult(Request request, RetryPolicy? retryPolicy = null)
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
            activityResult = await Workflow.ExecuteActivityAsync(
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
                Utils.PrintResult("With error inside Workflow", result);
                return result;
            }
        }

        result.Data = activityResult.Data;
        Utils.PrintResult("HappyPath inside Workflow", result);
        return result;
    }
}

#endregion

#region Models, Interfaces and Utilities
public class Request
{
    public string Data { get; set; }
}

public class Result
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();

    public string Data { get; set; }

    public void AddErrorMessage(string error) => Errors.Add(error);
}

public class ActivityResult
{
    public bool IsValid => !string.IsNullOrEmpty(Data);
    public string Data { get; set; }
}

public interface IService
{
    Task<string> GetData();
}

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
#endregion
