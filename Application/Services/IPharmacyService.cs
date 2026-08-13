namespace Application.Services
{
    public interface IPharmacyService
    {
        public Task<Result<PharmacyCreatedResponseDTO>> 
            AddPharmacy(AddPharmacyDTO addPharmacy, CancellationToken cancellationToken = default);

        public Task<Result<PaginatedList<GetPharmacyDTO>>> 
            GetAllPharmacies(GetPharmaciesRequest request, CancellationToken cancellationToken = default);

        public Task<Result<GetPharmacyDTO>> 
            GetPharmacyById(Guid Id, CancellationToken cancellationToken = default);

        public Task<Result>
            UpdatePharmacy(Guid Id, UpdatePharmacyDTO updatePharmacy, CancellationToken cancellationToken = default);

        public Task<Result> DeletePharmacy(Guid Id, CancellationToken cancellationToken = default);
    }
}
