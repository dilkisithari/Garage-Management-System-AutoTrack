namespace AutoTrack.Models;

public class QuotationAuditItem
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Action { get; set; } = "";
    public string ChangedBy { get; set; } = "";
    public string? Details { get; set; }
}
