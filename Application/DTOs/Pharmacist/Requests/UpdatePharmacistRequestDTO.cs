namespace Application.DTOs.Pharmacist.Requests;

public class UpdatePharmacistRequestDTO
{
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public Guid BranchId { set; get; }
    public string Password { get; set; } = string.Empty;
}
