namespace Infrastructure.Services;

public class PharmacyService(AppDbContext context, IMapper mapper) : IPharmacyService
{
    public async Task<Result<PharmacyCreatedResponseDTO>> AddPharmacy(AddPharmacyDTO addPharmacy,
        CancellationToken cancellationToken = default)
    {
        var isLicenseUnique = !await context.Pharmacies
            .AnyAsync(p => p.LicenseNumber == addPharmacy.LicenseNumber, cancellationToken);

        if (!isLicenseUnique)
            return Result.Failure<PharmacyCreatedResponseDTO>(PharmacyErrors.LicenseNumberNotUnique);

        var pharmacy = mapper.Map<Domain.Entities.Pharmacy>(addPharmacy);

        await context.Pharmacies.AddAsync(pharmacy, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        var result = new PharmacyCreatedResponseDTO
        {
            PharmacyId = pharmacy.PharmacyId, Status = pharmacy.VerificationStatus,
            Message = "Pharmacy Added Successfully"
        };

        return Result.Success(result);
    }

    public async Task<Result> DeletePharmacy(Guid Id, CancellationToken cancellationToken = default)
    {
        var pharmacy = await context.Pharmacies.FindAsync([Id], cancellationToken);

        if (pharmacy is null)
            return Result.Failure(PharmacyErrors.PharmacyNotFound);

        context.Pharmacies.Remove(pharmacy);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdatePharmacy(Guid id, UpdatePharmacyDTO updatePharmacy,
        CancellationToken cancellationToken = default)
    {
        var pharmacy = await context.Pharmacies.FindAsync([id], cancellationToken);

        if (pharmacy is null)
            return Result.Failure(PharmacyErrors.PharmacyNotFound);

        bool isLicenseUnique = !await context.Pharmacies
            .AnyAsync(p => p.LicenseNumber == updatePharmacy.LicenseNumber && p.PharmacyId != id
                , cancellationToken);

        if (!isLicenseUnique)
            return Result.Failure<PharmacyCreatedResponseDTO>(PharmacyErrors.LicenseNumberNotUnique);

        mapper.Map(updatePharmacy, pharmacy);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PaginatedList<GetPharmacyDTO>>> GetAllPharmacies(GetPharmaciesRequest request,
        CancellationToken cancellationToken = default)
    {
        var paginatedResult = await context.Pharmacies
            .AsNoTracking()
            .OrderBy(p => p.PharmacyId)
            .ProjectToType<GetPharmacyDTO>()
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<GetPharmacyDTO>> GetPharmacyById(Guid Id,
        CancellationToken cancellationToken = default)
    {
        var pharmacy = await context.Pharmacies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PharmacyId == Id, cancellationToken);

        return pharmacy is null
            ? Result.Failure<GetPharmacyDTO>(PharmacyErrors.PharmacyNotFound)
            : Result.Success(mapper.Map<GetPharmacyDTO>(pharmacy));
    }
}