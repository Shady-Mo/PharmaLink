using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Infrastructure.AI
{
    // Infrastructure/AI/PatientPrescriptionCollectionInitializer.cs
    public class PatientPrescriptionCollectionInitializer
    {
        private readonly QdrantClient _client;
        private readonly ILogger<PatientPrescriptionCollectionInitializer> _logger;

        public PatientPrescriptionCollectionInitializer(
            QdrantClient client,
            ILogger<PatientPrescriptionCollectionInitializer> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var exists = await _client.CollectionExistsAsync("patient_prescriptions", cancellationToken);
            if (exists)
            {
                _logger.LogInformation("Collection 'patient_prescriptions' already exists, skipping creation.");
                return;
            }

            await _client.CreateCollectionAsync(
                "patient_prescriptions",
                new VectorParams { Size = 1536, Distance = Distance.Cosine },
                cancellationToken: cancellationToken);

            await _client.CreatePayloadIndexAsync(
                "patient_prescriptions", "patient_id", PayloadSchemaType.Keyword,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Collection 'patient_prescriptions' created with patient_id index.");
        }
    }
}
