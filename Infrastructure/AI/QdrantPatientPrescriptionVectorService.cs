using Application.Abstractions;
using Infrastructure.AI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Infrastructure.AI
{
    public class QdrantPatientPrescriptionVectorService : IPatientPrescriptionVectorService
    {
        private const string CollectionName = "patient_prescriptions";
        private readonly QdrantClient _client;
        private readonly EmbeddingService _embeddingService;
        private readonly ILogger<QdrantPatientPrescriptionVectorService> _logger;

        public QdrantPatientPrescriptionVectorService(
            QdrantClient client,
            EmbeddingService embeddingService,
            ILogger<QdrantPatientPrescriptionVectorService> logger)
        {
            _client = client;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task UpsertPrescriptionAsync(
            PatientPrescriptionEmbeddingRecord record,
            CancellationToken cancellationToken = default)
        {
            var sourceText = record.BuildEmbeddingSourceText();
            var vector = await _embeddingService.GenerateEmbeddingAsync(sourceText);

            var point = new PointStruct
            {
                Id = new PointId { Uuid = record.PrescriptionId.ToString() },
                Vectors = vector.ToArray(),
                Payload =
                {
                    ["patient_id"] = record.PatientId.ToString(),
                    ["prescription_id"] = record.PrescriptionId.ToString(),
                    ["order_id"] = record.OrderId?.ToString() ?? "",
                    ["doctor_name"] = record.DoctorName ?? "",
                    ["specialty"] = record.Specialty ?? "",
                    ["clinic_or_hospital"] = record.ClinicOrHospital ?? "",
                    ["visit_date"] = record.VisitDate.ToString("O"),
                    ["diagnosis_notes"] = record.DiagnosisNotes ?? "",
                    ["drug_names"] = string.Join("|", record.DrugNames),
                    ["drug_ids"] = string.Join("|", record.DrugIds),
                    ["image_url"] = record.ImageUrl,
                    ["embedding_source_text"] = sourceText
                }
            };

            await _client.UpsertAsync(CollectionName, [point], cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Upserted embedding for prescription {PrescriptionId} (patient {PatientId})",
                record.PrescriptionId, record.PatientId);
        }

        public async Task<List<PatientPrescriptionSearchResult>> SearchAsync(
            Guid patientId,
            string query,
            int topK = 5,
            CancellationToken cancellationToken = default)
        {
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(query);

            var filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "patient_id",
                            Match = new Qdrant.Client.Grpc.Match { Keyword = patientId.ToString() }
                        }
                    }
                }
            };

            var results = await _client.SearchAsync(
                CollectionName,
                queryVector.ToArray(),
                filter: filter,
                limit: (ulong)topK,
                cancellationToken: cancellationToken);

            return results.Select(r => new PatientPrescriptionSearchResult
            {
                PrescriptionId = Guid.Parse(r.Payload["prescription_id"].StringValue),
                PatientId = patientId,
                DoctorName = r.Payload["doctor_name"].StringValue,
                Specialty = r.Payload["specialty"].StringValue,
                VisitDate = DateTime.Parse(r.Payload["visit_date"].StringValue),
                DiagnosisNotes = r.Payload["diagnosis_notes"].StringValue,
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
