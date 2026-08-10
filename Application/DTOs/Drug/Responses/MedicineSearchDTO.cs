namespace Application.DTOs.Drug.Responses
{
    public class MedicineSearchDTO
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? GenericName { get; set; }

        public string ArabicName { get; set; } = string.Empty;

        public string? Strength { get; set; }

        public string? DosageForm { get; set; }

        public string? Route { get; set; }

        public string? Category { get; set; }

        public string? Company { get; set; }

        public decimal? Price { get; set; }
    }
}
