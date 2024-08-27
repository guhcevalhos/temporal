using System.Threading.Tasks;
using Temporalio.Activities;
using test_temporal.Models;

namespace test_temporal.Activity;

public class ActivitiesClass
{
    private readonly IService _service;

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