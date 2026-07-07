namespace Application.DTOs.Drug.Requests;

public class CreateDrugDto
{
    public string GenericName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string DrugBankId { get; set; } = string.Empty;
    public string RxNormCui { get; set; } = string.Empty;
    public string NdcCode { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string DrugClass { get; set; } = string.Empty;
    public bool RequiresPrescription { get; set; }
}