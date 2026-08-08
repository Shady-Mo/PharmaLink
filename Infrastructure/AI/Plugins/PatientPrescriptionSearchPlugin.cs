namespace Infrastructure.AI.Plugins
{
    public class PatientPrescriptionSearchPlugin
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PatientPrescriptionSearchPlugin(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        [KernelFunction("search_prescription_history")]
        [Description("Searches the current patient's own prescription history using natural language. " +
                     "Use this to answer questions about past visits, doctors, diagnoses, or medications.")]
        public async Task<string> SearchPrescriptionHistoryAsync(
            [Description("The patient's natural language question about their prescription history")] string query)
        {
            using var scope = _scopeFactory.CreateScope();

            var vectorService = scope.ServiceProvider.GetRequiredService<IPatientPrescriptionVectorService>();
            var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

            var patientId = currentUserService.PatientId
                ?? throw new UnauthorizedAccessException("No patient context available");

            var results = await vectorService.SearchAsync(patientId, query, topK: 3);

            if (results == null || results.Count == 0)
                return "لم يتم العثور على روشتات مطابقة في سجل المريض.";

            return JsonSerializer.Serialize(results);
        }
    }
}