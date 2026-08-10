using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Infrastructure.AI
{
    // Infrastructure/AI/PatientPrescriptionCollectionInitializer.cs
    public class PatientPrescriptionCollectionInitializer
    {
        public const string CollectionName = "patient_prescriptions_v2";
        public const ulong VectorSize = 3072;

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
            var exists = await _client.CollectionExistsAsync(CollectionName, cancellationToken);
            if (exists)
            {
                _logger.LogInformation("Collection '{CollectionName}' already exists, skipping creation.", CollectionName);
                return;
            }

            await _client.CreateCollectionAsync(
                CollectionName,
                new VectorParams { Size = VectorSize, Distance = Distance.Cosine },
                cancellationToken: cancellationToken);

            await _client.CreatePayloadIndexAsync(
                CollectionName, "patient_id", PayloadSchemaType.Keyword,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Collection '{CollectionName}' created with {VectorSize}-dimensional vectors and a patient_id index.",
                CollectionName,
                VectorSize);
        }
    }
}
