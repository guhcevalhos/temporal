using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Temporalio.Client;
using Temporalio.Common;
using Temporalio.Testing;
using Temporalio.Worker;
using test_temporal.Activity;
using test_temporal.Models;
using test_temporal.Workflow;

namespace test_temporal;

public class Tests
{

    private ActivitiesClass _activitiesClass;
    private WorkflowEnvironment Env { get; set; }
    private static readonly Mock<IService> ServiceMock = new();

    [OneTimeSetUp]
    public async Task Setup()
    {
        Env = await WorkflowEnvironment
            .StartLocalAsync(new WorkflowEnvironmentStartLocalOptions()
            {
                UI = false
            });
    }

    [Test]
    public async Task HappyPath()
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
                }),
                new WorkflowOptions(id: $"wf-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.Data, Is.Not.Null, "Data should not be null");
            Assert.That(result.Errors, Is.Empty, "Errors should be null");
            Assert.That(result.IsValid, Is.True, "Result should be valid");
        });
        
    }

    [Test]
    public async Task WithError()
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
                }),
                new WorkflowOptions(id: $"wf-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!));
        });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.Data, Is.Null, "Data should be null");
            Assert.That(result.Errors, Is.Not.Empty, "Errors should not be null");
            Assert.That(result.Errors.First(), Is.EqualTo("Activity failed: Activity task failed"), "First errors should contain error message");
            Assert.That(result.IsValid, Is.False, "Result should not be valid");
        });
    }
}