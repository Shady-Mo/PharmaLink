using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IDrugService
    {
        Task SeedDrugsAsync(string jsonFilePath, CancellationToken cancellationToken = default);
    }
}
