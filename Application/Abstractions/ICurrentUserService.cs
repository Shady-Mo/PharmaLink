using System;

namespace Application.Abstractions;

public interface ICurrentUserService
{
    Guid? PatientId { get; }
}
