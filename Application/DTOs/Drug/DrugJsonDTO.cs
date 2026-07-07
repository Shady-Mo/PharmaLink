namespace Application.DTOs.Drug
{
    public class DrugSeedRoot
    {
        [JsonPropertyName("data")] public List<DrugJsonDTO> Data { get; set; } = new();
    }

    public class DrugJsonDTO
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("active")] public string ActiveIngredient { get; set; }

        [JsonPropertyName("dosage_form")] public string DosageForm { get; set; }

        [JsonPropertyName("barcode")] public string Barcode { get; set; }
    }
}