using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions
{
    public interface IPrescriptionEmbeddingJob
    {
        Task ProcessAsync(Guid prescriptionReviewId, CancellationToken cancellationToken = default);
    }
}
