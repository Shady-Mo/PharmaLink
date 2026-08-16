using System;
using System.Collections.Generic;

namespace Application.Services.AI.Models
{
    public class PrescriptionAnalyticsEmbeddingRecord
    {
        public required Guid PrescriptionId { get; init; }
        public Guid? OrderId { get; init; }
        public string? DoctorName { get; init; }
        public string? Specialty { get; init; }
        public string? ClinicOrHospital { get; init; }
        public DateTime VisitDate { get; init; }
        public string? DiagnosisNotes { get; init; }
        public string? PatientAddress { get; init; }
        public List<string> DrugNames { get; init; } = [];
        public List<Guid> DrugIds { get; init; } = [];
        public string ImageUrl { get; init; } = string.Empty;

        public string BuildEmbeddingSourceText()
        {
            var drugs = string.Join(", ", DrugNames);
            var addressText = !string.IsNullOrWhiteSpace(PatientAddress) ? $"العنوان: {PatientAddress}. " : "";
            return $"روشتة من {DoctorName ?? "دكتور غير معروف"} " +
                   $"({Specialty ?? "تخصص غير محدد"}) - " +
                   $"{ClinicOrHospital ?? ""} - بتاريخ {VisitDate:d MMMM yyyy}. " +
                   addressText +
                   $"التشخيص: {DiagnosisNotes ?? "غير مسجل"}. " +
                   $"الأدوية: {drugs}.";
        }
    }

    public class PrescriptionAnalyticsSearchResult
    {
        public required Guid PrescriptionId { get; init; }
        public string? DoctorName { get; init; }
        public string? Specialty { get; init; }
        public string? ClinicOrHospital { get; init; }
        public DateTime VisitDate { get; init; }
        public string? DiagnosisNotes { get; init; }
        public string? PatientAddress { get; init; }
        public List<string> DrugNames { get; init; } = [];
        public List<Guid> DrugIds { get; init; } = [];
        public string ImageUrl { get; init; } = string.Empty;
        public double Score { get; init; }
    }
}
