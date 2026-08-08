namespace Application.DTOs.MedicalInquiry.Responses;

public class MedicalInquiryMetricsResponse
{
    public int TotalInquiries { get; set; }
    public int PendingInquiries { get; set; }
    public int AnsweredInquiries { get; set; }
    public int ClosedInquiries { get; set; }
    public int AnsweredToday { get; set; }
}
