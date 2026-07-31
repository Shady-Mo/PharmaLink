using Application.Services.AI.Models;

namespace Application.Services.AI;

public interface IAIResponseValidator<in T>
{
    Models.ValidationResult Validate(T response);
}
