namespace Application.DTOs.Dashboard.Responses;

/// <summary>
/// Dashboard statistics for a patient including account summary information.
/// </summary>
public class DashboardStatisticsDTO
{
    /// <summary>
    /// Total number of orders placed by the patient.
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Number of prescription reviews pending pharmacist approval.
    /// </summary>
    public int PendingPrescriptionReviews { get; set; }

    /// <summary>
    /// Number of saved addresses in the patient's profile.
    /// </summary>
    public int SavedAddresses { get; set; }

    /// <summary>
    /// Total reward/loyalty points accumulated by the patient.
    /// </summary>
    public int RewardPoints { get; set; }
}
