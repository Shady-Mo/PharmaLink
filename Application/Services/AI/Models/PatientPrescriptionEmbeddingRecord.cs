using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.AI.Models
{
    public class PatientPrescriptionEmbeddingRecord
    {
        public required Guid PrescriptionId { get; init; }
        public required Guid PatientId { get; init; }
        public Guid? OrderId { get; init; }
        public string? DoctorName { get; init; }
        public string? Specialty { get; init; }
        public string? ClinicOrHospital { get; init; }
        public DateTime VisitDate { get; init; }
        public string? DiagnosisNotes { get; init; }
        public List<string> DrugNames { get; init; } = [];
        public List<Guid> DrugIds { get; init; } = [];
        public string ImageUrl { get; init; } = string.Empty;

        // بيتبني في الـ builder قبل ما يتبعت للـ embedding model
        public string BuildEmbeddingSourceText()
        {
            var drugs = string.Join(", ", DrugNames);
            return $"روشتة من {DoctorName ?? "دكتور غير معروف"} " +
                   $"({Specialty ?? "تخصص غير محدد"}) - " +
                   $"{ClinicOrHospital ?? ""} - بتاريخ {VisitDate:d MMMM yyyy}. " +
                   $"التشخيص: {DiagnosisNotes ?? "غير مسجل"}. " +
                   $"الأدوية: {drugs}.";
        }
    }

    public class PatientPrescriptionSearchResult
    {
        public required Guid PrescriptionId { get; init; }
        public required Guid PatientId { get; init; }
        public string? DoctorName { get; init; }
        public string? Specialty { get; init; }
        public DateTime VisitDate { get; init; }
        public string? DiagnosisNotes { get; init; }
        public List<string> DrugNames { get; init; } = [];
        public List<Guid> DrugIds { get; init; } = [];
        public string ImageUrl { get; init; } = string.Empty;
        public double Score { get; init; }
    }
}
