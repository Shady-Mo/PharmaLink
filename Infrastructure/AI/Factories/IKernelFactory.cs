using Infrastructure.AI.Models;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Factories;

public interface IKernelFactory
{
    Kernel GetKernel(AIProvider provider, ModelRole role, string? modelId = null);
}
