using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Infrastructure.AI
{
    public class PrescriptionAnalyticsCollectionInitializer
    {
        public const string CollectionName = "prescription_analytics_v2";
        public const ulong VectorSize = 3072;

        private readonly QdrantClient _client;
        private readonly ILogger<PrescriptionAnalyticsCollectionInitializer> _logger;

        public PrescriptionAnalyticsCollectionInitializer(
            QdrantClient client,
            ILogger<PrescriptionAnalyticsCollectionInitializer> logger)
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

            _logger.LogInformation(
                "Collection '{CollectionName}' created with {VectorSize}-dimensional vectors and Cosine distance.",
                CollectionName,
                VectorSize);
        }
    }
}
