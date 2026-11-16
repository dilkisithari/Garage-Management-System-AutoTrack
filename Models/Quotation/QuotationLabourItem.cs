namespace AutoTrack.Models;

public class QuotationLabourItem
{
    public string? Description { get; set; }
    public decimal Hours { get; set; } = 1;
    public decimal Rate { get; set; } = 2500;
    public decimal LineTotal => Hours * Rate;
}
