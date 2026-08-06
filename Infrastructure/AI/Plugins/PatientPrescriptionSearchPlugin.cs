namespace Infrastructure.AI.Plugins
{
    public class PatientPrescriptionSearchPlugin
    {
        private readonly IPatientPrescriptionVectorService _vectorService;
        private readonly Guid _patientId; 

        public PatientPrescriptionSearchPlugin(
            IPatientPrescriptionVectorService vectorService,
            ICurrentUserService currentUserService)
        {
            _vectorService = vectorService;
            _patientId = currentUserService.PatientId
                ?? throw new UnauthorizedAccessException("No patient context available");
        }

        [KernelFunction("search_prescription_history")]
        [Description("Searches the current patient's own prescription history using natural language. " +
                     "Use this to answer questions about past visits, doctors, diagnoses, or medications.")]
        public async Task<string> SearchPrescriptionHistoryAsync(
            [Description("The patient's natural language question about their prescription history")] string query)
        {
            var results = await _vectorService.SearchAsync(_patientId, query, topK: 3);

            if (results.Count == 0)
                return "لم يتم العثور على روشتات مطابقة في سجل المريض.";

            return JsonSerializer.Serialize(results);
        }
    }
}
