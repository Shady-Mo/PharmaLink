namespace Application.DTOs.Pharmacy.Responses
{
    public class PharmacyCreatedResponseDTO
    {
        public Guid PharmacyId { get; set; }
        public VerificationStatus Status { get; set; }
        public string Message { get; set; } = default!;
    }
}
