namespace AutoTrack.Models;

public class QuotationPartItem
{
    public string? PartName { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}
