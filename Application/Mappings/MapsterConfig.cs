using Application.DTOs.Addresses.Requests;
using Application.DTOs.Addresses.Response;
using Application.DTOs.Pharmacy.Responses;
using Application.DTOs.PharmacyBranch.Request;
using Application.DTOs.PharmacyBranch.Response;
using Application.DTOs.PharmacyInventory.Request;
using Application.DTOs.PharmacyInventory.Response;
using Application.DTOs.PharmacyOwner.Responses;

namespace Application.Mappings;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Drug Mappings
        config.NewConfig<Drug, DrugDto>();
        config.NewConfig<CreateDrugDto, Drug>();
        config.NewConfig<UpdateDrugDto, Drug>();
        
        // Fix for circular reference in Projection
        config.NewConfig<DrugCategory, DrugCategoryDto>()
            .MaxDepth(2);

        // Patient Mappings
        config.NewConfig<RegisterRequestDTO, Patient>()
            .Map(dest => dest.UserName, src => src.Email.ToLowerInvariant())
            .Map(dest => dest.Email, src => src.Email.ToLowerInvariant());

        // Pharmacy Inventory Mappings
        config.NewConfig<AddPharmacyInventoryDto, PharmacyInventory>()
            .Map(dest => dest.LastSyncedAt, src => DateTime.UtcNow);

        config.NewConfig<UpdatePharmacyInventoryDto, PharmacyInventory>()
            .Map(dest => dest.LastSyncedAt, src => DateTime.UtcNow);

        config.NewConfig<PharmacyInventory, PharmacyInventoryDto>()
            .Map(dest => dest.BranchName, src => src.Branch.BranchName)
            .Map(dest => dest.DrugName, src => src.Drug.BrandName)
            .Map(dest => dest.GenericName, src => src.Drug.GenericName)
            .Map(dest => dest.ArabicName, src => src.Drug.ArabicName)
            .Map(dest => dest.ImageUrl, src => src.Drug.ImageUrl)
            .Map(dest => dest.AvailableQuantity, src => src.StockQuantity - src.ReservedQuantity)
            .Map(dest => dest.StockStatus, src => src.StockQuantity == 0
                ? InventoryStockStatus.OutOfStock
                : src.StockQuantity <= 10 ? InventoryStockStatus.LowStock : InventoryStockStatus.Available);

        config.NewConfig<PharmacyInventory, GetPharmacyInventoryDTO>()
            .Map(dest => dest.BranchName, src => src.Branch.BranchName)
            .Map(dest => dest.ArabicName, src => src.Drug.ArabicName)
            .Map(dest => dest.DrugName, src => src.Drug.BrandName)
            .Map(dest => dest.StockStatus, src => src.StockQuantity == 0
                ? InventoryStockStatus.OutOfStock
                : src.StockQuantity <= 10 ? InventoryStockStatus.LowStock : InventoryStockStatus.Available);

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
            .Map(dest => dest.IsOpenNow, src => true) // Default mock for now
            .Map(dest => dest.Latitude, src => src.Branch.GeoLocation != null ? src.Branch.GeoLocation.Y : 0)
            .Map(dest => dest.Longitude, src => src.Branch.GeoLocation != null ? src.Branch.GeoLocation.X : 0)
            .Map(dest => dest.GoogleMapsUrl, src => src.Branch.GeoLocation != null && src.Order.DeliveryAddress.GeoLocation != null ? $"https://www.google.com/maps/dir/?api=1&origin={src.Order.DeliveryAddress.GeoLocation.Y},{src.Order.DeliveryAddress.GeoLocation.X}&destination={src.Branch.GeoLocation.Y},{src.Branch.GeoLocation.X}" : src.Branch.GeoLocation != null ? $"https://www.google.com/maps/dir/?api=1&destination={src.Branch.GeoLocation.Y},{src.Branch.GeoLocation.X}" : string.Empty)
            .Map(dest => dest.SupportsDelivery, src => src.Branch.SupportsDelivery)
            .Map(dest => dest.SupportsPickup, src => src.Branch.SupportsPickup)
            // Prefer the OSRM driving distance captured on the leg at split time (same value the
            // order-routing preview returns). Fall back to a straight-line estimate only for legacy
            // legs created before the distance was persisted.
            .Map(dest => dest.DistanceKm, src => src.DistanceKm != null
                ? src.DistanceKm
                : (src.Branch.GeoLocation != null && src.Order.DeliveryAddress.GeoLocation != null)
                    ? src.Branch.GeoLocation.Distance(src.Order.DeliveryAddress.GeoLocation) / 1000.0
                    : (double?)null)

            .Map(dest => dest.IsReady, src => src.LegStatus == LegStatus.ReadyForPickup || src.LegStatus == LegStatus.OutForDelivery)
            .Map(dest => dest.IsCompleted, src => src.LegStatus == LegStatus.Delivered)
            .Map(dest => dest.EstimatedPreparationMinutes, src => (int)((src.ReadyByEstimate - DateTime.UtcNow).TotalMinutes))
            .Map(dest => dest.Items, src => src.Order.Items.Where(i => i.BranchId == src.BranchId));

        config.NewConfig<OrderItem, OrderItemResponseDTO>()
            .Map(dest => dest.DrugName, src => src.Drug.BrandName)
            .Map(dest => dest.GenericName, src => src.Drug.GenericName)
            .Map(dest => dest.ArabicName, src => src.Drug.ArabicName)
            .Map(dest => dest.ImageUrl, src => src.Drug.ImageUrl)
            .Map(dest => dest.Strength, src => src.Drug.Strength)
            .Map(dest => dest.DosageForm, src => src.Drug.Form)
            .Map(dest => dest.UnitPrice, src => src.Drug.Price);
        config.NewConfig<Address, CreateAddressRequestDTO>();
        config.NewConfig<Address, AddressResponseDTO>()
            .Map(dest => dest.Longitude, src => src.GeoLocation != null ? src.GeoLocation.X : 0)
            .Map(dest => dest.Latitude, src => src.GeoLocation != null ? src.GeoLocation.Y : 0);

        // Cart Mappings
        config.NewConfig<CartItem, CartItemResponseDTO>()
            .Map(dest => dest.DrugBrandName, src => src.Drug != null ? src.Drug.BrandName : string.Empty)
            .Map(dest => dest.DrugGenericName, src => src.Drug != null ? src.Drug.GenericName : string.Empty)
            .Map(dest => dest.DrugArabicName, src => src.Drug != null ? src.Drug.ArabicName : string.Empty)
            .Map(dest => dest.DrugImageUrl, src => src.Drug != null ? src.Drug.ImageUrl : null)
            .Map(dest => dest.RequiresPrescription, src => src.Drug != null ? src.Drug.RequiresPrescription : false);
            
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

        // CreatePharmacistRequestDTO -> Pharmacist
        config.NewConfig<CreatePharmacistRequestDTO, Pharmacist>()
            .Map(dest => dest.UserName, src => src.Email.ToLowerInvariant())
            .Map(dest => dest.Email, src => src.Email.ToLowerInvariant());

        // PharmacistAssignment -> AssignmentHistoryItemDTO
        config.NewConfig<PharmacistAssignment, AssignmentHistoryItemDTO>()
            .Map(dest => dest.AssignmentId, src => src.Id)
            .Map(dest => dest.PharmacyLegalName, src => src.Pharmacy != null
                ? src.Pharmacy.LegalName
                : string.Empty);

        // Pharmacist -> PharmacistResponseDTO
        config.NewConfig<Pharmacist, PharmacistResponseDTO>()
            .Map(dest => dest.PharmacistId, src => src.Id)
            .Map(dest => dest.Status, src => src.Status.ToString());

        // Pharmacist -> PharmacistSummaryDTO
        config.NewConfig<Pharmacist, PharmacistSummaryDTO>()
            .Map(dest => dest.PharmacistId, src => src.Id)
            .Map(dest => dest.Email, src => src.Email ?? string.Empty)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber ?? string.Empty);

        // Admin Pharmacy Mappings
        config.NewConfig<PharmacyAdmin, PharmacyOwnerDTO>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FullName, src => src.FullName)
            .Map(dest => dest.Email, src => src.Email ?? string.Empty)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber ?? string.Empty);

        config.NewConfig<PharmacyBranch, AdminPharmacyBranchDTO>()
            .Map(dest => dest.BranchId, src => src.BranchId)
            .Map(dest => dest.BranchName, src => src.BranchName)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.Governorate, src => src.Governorate)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
            .Map(dest => dest.Latitude, src => src.GeoLocation != null ? src.GeoLocation.Y : 0)
            .Map(dest => dest.Longitude, src => src.GeoLocation != null ? src.GeoLocation.X : 0)
            .Map(dest => dest.ServiceRadiusKm, src => src.ServiceRadiusKm)
            .Map(dest => dest.SupportsDelivery, src => src.SupportsDelivery)
            .Map(dest => dest.SupportsPickup, src => src.SupportsPickup);

        config.NewConfig<PharmacyBranch, PharmacyBranchResponseDTO>()
            .Map(dest => dest.Latitude, src => src.GeoLocation != null ? src.GeoLocation.Y : 0)
            .Map(dest => dest.Longitude, src => src.GeoLocation != null ? src.GeoLocation.X : 0);

        config.NewConfig<Pharmacy, AdminPharmacySummaryDTO>()
            .Map(dest => dest.PharmacyId, src => src.PharmacyId)
            .Map(dest => dest.LegalName, src => src.LegalName)
            .Map(dest => dest.LicenseNumber, src => src.LicenseNumber)
            .Map(dest => dest.LogoUrl, src => src.LogoUrl)
            .Map(dest => dest.VerificationStatus, src => src.VerificationStatus)
            .Map(dest => dest.BranchesCount, src => src.Branches.Count)
            .Map(dest => dest.DrugsCount, src => src.Branches.SelectMany(b => b.Inventories).Select(i => i.DrugId).Distinct().Count())
            .Map(dest => dest.Owner, src => src.Admins.FirstOrDefault(a => a.IsSuperAdmin == true))
            .Map(dest => dest.Branches, src => src.Branches);

        config.NewConfig<Pharmacy, AdminPharmacyDetailDTO>()
            .Map(dest => dest.PharmacyId, src => src.PharmacyId)
            .Map(dest => dest.LegalName, src => src.LegalName)
            .Map(dest => dest.LicenseNumber, src => src.LicenseNumber)
            .Map(dest => dest.LogoUrl, src => src.LogoUrl)
            .Map(dest => dest.VerificationStatus, src => src.VerificationStatus)
            .Map(dest => dest.BranchesCount, src => src.Branches.Count)
            .Map(dest => dest.DrugsCount, src => src.Branches.SelectMany(b => b.Inventories).Select(i => i.DrugId).Distinct().Count())
            .Map(dest => dest.Owner, src => src.Admins.FirstOrDefault(a => a.IsSuperAdmin == true))
            .Map(dest => dest.Branches, src => src.Branches);

        config.NewConfig<PharmacyAdmin, PharmacyOwnerResponseDTO>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FullName, src => src.FullName)
            .Map(dest => dest.Email, src => src.Email ?? string.Empty)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber ?? string.Empty)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.PharmacyId, src => src.PharmacyId)
            .Map(dest => dest.IsSuperAdmin, src => src.IsSuperAdmin)
            .Map(dest => dest.Pharmacy, src => src.Pharmacy);

        config.NewConfig<Pharmacy, PharmacyOwnerDetailsDTO>()
            .Map(dest => dest.PharmacyId, src => src.PharmacyId)
            .Map(dest => dest.LegalName, src => src.LegalName)
            .Map(dest => dest.LicenseNumber, src => src.LicenseNumber)
            .Map(dest => dest.LogoUrl, src => src.LogoUrl);

        config.NewConfig<PharmacistAssignment, GetPharmacyProfileResponseDTO>()
            .Map(dest => dest.Id, src => src.Pharmacist.Id)
            .Map(dest => dest.FullName, src => src.Pharmacist.FullName)
            .Map(dest => dest.Email, src => src.Pharmacist.Email)
            .Map(dest => dest.PhoneNumber, src => src.Pharmacist.PhoneNumber)
            .Map(dest => dest.AdministeredPharmacies, src => new[] { src.Pharmacy });
    }
}