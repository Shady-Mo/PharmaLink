using Domain.Enums;

namespace Application.DTOs.Pharmacist.Requests;

public class UpdatePharmacistStatusRequestDTO
{
    public UserStatus Status { get; set; }
}
