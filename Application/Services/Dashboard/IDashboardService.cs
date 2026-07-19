using Application.DTOs.Dashboard.Responses;

namespace Application.Services.Dashboard;

/// <summary>
/// Service for retrieving patient dashboard information including statistics,
/// current orders, and recent order history.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retrieves the complete dashboard information for an authenticated patient.
    /// </summary>
    /// <param name="patientUserId">The unique identifier of the patient.</param>
    /// <param name="recentOrdersCount">Number of recent orders to retrieve (default: 5).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// A Result containing the PatientDashboardDTO with statistics, current order info,
    /// and recent orders. Returns failure if patient not found or database error occurs.
    /// </returns>
    Task<Result<PatientDashboardDTO>> GetDashboardAsync(
        Guid patientUserId,
        int recentOrdersCount = 5,
        CancellationToken cancellationToken = default);
}
