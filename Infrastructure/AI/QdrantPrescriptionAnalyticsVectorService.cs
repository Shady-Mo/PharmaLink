using Application.Abstractions;
using Application.Services.AI.Models;
using Infrastructure.AI.Models;
using Infrastructure.AI.Services;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.AI
{
    public class QdrantPrescriptionAnalyticsVectorService : IPrescriptionAnalyticsVectorService
    {
        private const string CollectionName = PrescriptionAnalyticsCollectionInitializer.CollectionName;
        private readonly QdrantClient _client;
        private readonly EmbeddingService _embeddingService;
        private readonly ILogger<QdrantPrescriptionAnalyticsVectorService> _logger;

        public QdrantPrescriptionAnalyticsVectorService(
            QdrantClient client,
            EmbeddingService embeddingService,
            ILogger<QdrantPrescriptionAnalyticsVectorService> logger)
        {
            _client = client;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task UpsertPrescriptionAsync(
            PrescriptionAnalyticsEmbeddingRecord record,
            CancellationToken cancellationToken = default)
        {
            var sourceText = record.BuildEmbeddingSourceText();
            var vector = await _embeddingService.GenerateEmbeddingAsync(sourceText, AIProvider.Gemini);

            var point = new PointStruct
            {
                Id = new PointId { Uuid = record.PrescriptionId.ToString() },
                Vectors = vector.ToArray(),
                Payload =
                {
                    ["prescription_id"] = record.PrescriptionId.ToString(),
                    ["order_id"] = record.OrderId?.ToString() ?? "",
                    ["doctor_name"] = record.DoctorName ?? "",
                    ["specialty"] = record.Specialty ?? "",
                    ["clinic_or_hospital"] = record.ClinicOrHospital ?? "",
                    ["visit_date"] = record.VisitDate.ToString("O"),
                    ["diagnosis_notes"] = record.DiagnosisNotes ?? "",
                    ["patient_address"] = record.PatientAddress ?? "",
                    ["drug_names"] = string.Join("|", record.DrugNames),
                    ["drug_ids"] = string.Join("|", record.DrugIds),
                    ["image_url"] = record.ImageUrl,
                    ["embedding_source_text"] = sourceText
                }
            };

            await _client.UpsertAsync(CollectionName, [point], cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Upserted analytics embedding for prescription {PrescriptionId}",
                record.PrescriptionId);
        }

        public async Task<List<PrescriptionAnalyticsSearchResult>> SearchAsync(
            string query,
            int topK = 30,
            CancellationToken cancellationToken = default)
        {
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(query, AIProvider.Gemini);

            var results = await _client.SearchAsync(
                CollectionName,
                queryVector.ToArray(),
                limit: (ulong)topK,
                cancellationToken: cancellationToken);

            return results.Select(r => new PrescriptionAnalyticsSearchResult
            {
                PrescriptionId = Guid.Parse(r.Payload["prescription_id"].StringValue),
                DoctorName = r.Payload["doctor_name"].StringValue,
                Specialty = r.Payload["specialty"].StringValue,
                ClinicOrHospital = r.Payload.TryGetValue("clinic_or_hospital", out var ch) ? ch.StringValue : null,
                VisitDate = DateTime.Parse(r.Payload["visit_date"].StringValue),
                DiagnosisNotes = r.Payload["diagnosis_notes"].StringValue,
                PatientAddress = r.Payload.TryGetValue("patient_address", out var addr) ? addr.StringValue : null,
                DrugNames = r.Payload["drug_names"].StringValue
                    .Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
                DrugIds = r.Payload["drug_ids"].StringValue
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse).ToList(),
                ImageUrl = r.Payload["image_url"].StringValue,
                Score = r.Score
            }).ToList();
        }

        public async Task DeleteAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
        {
            await _client.DeleteAsync(
                CollectionName,
                new PointId { Uuid = prescriptionId.ToString() },
                cancellationToken: cancellationToken);
        }
    }
}
