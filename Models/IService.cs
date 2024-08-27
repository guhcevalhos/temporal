using System.Threading.Tasks;

namespace test_temporal.Models;

public interface IService
{
    Task<string> GetData();
}