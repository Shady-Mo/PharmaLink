using Application.DTOs.Addresses.Requests;
using Application.DTOs.Addresses.Response;
using Application.DTOs.PharmacyInventory.Response;

namespace Application.Mappings;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Drug Mappings
        config.NewConfig<Drug, DrugDto>();
        config.NewConfig<CreateDrugDto, Drug>();
        config.NewConfig<UpdateDrugDto, Drug>();

        // Patient Mappings
        config.NewConfig<RegisterRequestDTO, Patient>()
            .Map(dest => dest.UserName, src => src.Email.ToLowerInvariant())
            .Map(dest => dest.Email, src => src.Email.ToLowerInvariant());

        // Pharmacy Inventory Mappings
        config.NewConfig<PharmacyInventory, GetPharmacyInventoryDTO>()
            .Map(dest => dest.DrugName, src => src.Drug.BrandName);

        // Order Mappings
        config.NewConfig<Order, GetOrderDTO>()
            .Map(dest => dest.Summary, src => new OrderSummaryDTO
            {
                TotalBranches = src.FulfillmentLegs.Count,
                FulfilledItems = src.Items.Count(i => i.BranchId != null),
                PendingItems = src.Items.Count(i => i.BranchId == null),
                EstimatedReadyAt = src.FulfillmentLegs.Max(l => (DateTime?)l.ReadyByEstimate),
            })
            .Map(dest => dest.FulfillmentLegs, src => src.FulfillmentLegs.OrderBy(l => l.Branch.GeoLocation.Distance(src.DeliveryAddress.GeoLocation)))
            .Map(dest => dest.PendingAssignmentItems, src => src.Items.Where(i => i.BranchId == null));

        config.NewConfig<OrderFulfillmentLeg, OrderFulfillmentLegResponseDTO>()
            .Map(dest => dest.PharmacyId, src => src.Branch.PharmacyId)
            .Map(dest => dest.PharmacyName, src => src.Branch.Pharmacy.LegalName)
            .Map(dest => dest.PharmacyLogoUrl, src => src.Branch.Pharmacy.LogoUrl)
            .Map(dest => dest.BranchName, src => src.Branch.BranchName)
            .Map(dest => dest.City, src => src.Branch.City)
            .Map(dest => dest.Governorate, src => src.Branch.Governorate)
            .Map(dest => dest.BranchAddressLine, src => src.Branch.AddressLine)
            .Map(dest => dest.PhoneNumber, src => src.Branch.PhoneNumber)
            .Map(dest => dest.WorkingHours, src => src.Branch.WorkingHours)
            .Map(dest => dest.IsOpenNow, src => true) // Default mock for now
            .Map(dest => dest.Latitude, src => src.Branch.GeoLocation != null ? src.Branch.GeoLocation.Y : 0)
            .Map(dest => dest.Longitude, src => src.Branch.GeoLocation != null ? src.Branch.GeoLocation.X : 0)
            .Map(dest => dest.GoogleMapsUrl, src => src.Branch.GeoLocation != null ? "https://www.google.com/maps/dir/?api=1&destination=" + src.Branch.GeoLocation.Y + "," + src.Branch.GeoLocation.X : string.Empty)
            .Map(dest => dest.SupportsDelivery, src => src.Branch.SupportsDelivery)
            .Map(dest => dest.SupportsPickup, src => src.Branch.SupportsPickup)
            .Map(dest => dest.DistanceKm, src => (src.Branch.GeoLocation != null && src.Order.DeliveryAddress.GeoLocation != null) 
                ? src.Branch.GeoLocation.Distance(src.Order.DeliveryAddress.GeoLocation) / 1000.0 : (double?)null)
            .Map(dest => dest.IsReady, src => src.LegStatus == LegStatus.ReadyForPickup || src.LegStatus == LegStatus.PickedUpByCourier)
            .Map(dest => dest.IsCompleted, src => src.LegStatus == LegStatus.Completed)
            .Map(dest => dest.EstimatedPreparationMinutes, src => (int)((src.ReadyByEstimate - DateTime.UtcNow).TotalMinutes))
            .Map(dest => dest.Items, src => src.Order.Items.Where(i => i.BranchId == src.BranchId));

        config.NewConfig<OrderItem, OrderItemResponseDTO>()
            .Map(dest => dest.DrugName, src => src.Drug.BrandName)
            .Map(dest => dest.GenericName, src => src.Drug.GenericName)
            .Map(dest => dest.Strength, src => src.Drug.Strength)
            .Map(dest => dest.DosageForm, src => src.Drug.Form)
            .Map(dest => dest.UnitPrice, src => src.Drug.Price);

        // Address Mappings
        config.NewConfig<Address, CreateAddressRequestDTO>();
        config.NewConfig<Address, AddressResponseDTO>()
            .Map(dest => dest.Longitude, src => src.GeoLocation != null ? src.GeoLocation.X : 0)
            .Map(dest => dest.Latitude, src => src.GeoLocation != null ? src.GeoLocation.Y : 0);

        // Cart Mappings
        config.NewConfig<CartItem, CartItemResponseDTO>()
            .Map(dest => dest.DrugBrandName, src => src.Drug != null ? src.Drug.BrandName : string.Empty)
            .Map(dest => dest.DrugGenericName, src => src.Drug != null ? src.Drug.GenericName : string.Empty);

        // PrescriptionReview Mappings
        config.NewConfig<PrescriptionReview, PrescriptionReviewSummaryDTO>()
            .Map(dest => dest.ReviewId, src => src.PrescriptionReviewId)
            .Map(dest => dest.PatientName, src => src.Patient.FullName)
            .Map(dest => dest.Status, src => src.ReviewStatus.ToString())
            .Map(dest => dest.MedicineCount, src => src.Medicines.Count);

        config.NewConfig<PrescriptionReview, PrescriptionReviewDetailDTO>()
            .Map(dest => dest.ReviewId, src => src.PrescriptionReviewId)
            .Map(dest => dest.PatientName, src => src.Patient.FullName)
            .Map(dest => dest.Status, src => src.ReviewStatus.ToString());

        config.NewConfig<PrescriptionReviewMedicine, MedicineDetailDTO>()
            .Map(dest => dest.Id, src => src.PrescriptionReviewMedicineId);

        config.NewConfig<PrescriptionReviewMedicine, ExtractedMedicineSummaryDTO>()
            .Map(dest => dest.Id, src => src.PrescriptionReviewMedicineId)
            .Map(dest => dest.Name, src => src.MedicineName);
    }
}