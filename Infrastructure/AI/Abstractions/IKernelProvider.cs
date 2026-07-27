using Infrastructure.AI.Models;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Abstractions;

public interface IKernelProvider
{
    AIProvider Provider { get; }
    Kernel GetKernel(ModelRole role, string? modelId = null);
}
