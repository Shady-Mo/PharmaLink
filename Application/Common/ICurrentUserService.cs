using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string? RoleName { get; }
    }
}
