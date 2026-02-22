using Nexa.Adapter.Models;
using System.Threading.Tasks;

namespace Nexa.Adapter.Services
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        Task<ToolResult> ExecuteAsync(ToolCall call);
    }
}