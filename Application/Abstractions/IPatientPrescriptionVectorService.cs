namespace Application.Abstractions
{
    public interface IPatientPrescriptionVectorService
    {

        Task UpsertPrescriptionAsync(
            PatientPrescriptionEmbeddingRecord record,
            CancellationToken cancellationToken = default);

        Task<List<PatientPrescriptionSearchResult>> SearchAsync(
            Guid patientId,
            string query,
            int topK = 5,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid prescriptionId,
            CancellationToken cancellationToken = default);
    }
}
