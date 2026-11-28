using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models.Parts;

/// <summary>A stocked spare part (Option 6 - Parts inventory).</summary>
public class PartRecord
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public int Stock { get; set; }
    public int ReorderLevel { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Price { get; set; }
    public string Supplier { get; set; } = "";
    public string SupplierPhone { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public bool IsLowStock => Stock < ReorderLevel;
    public bool IsOutOfStock => Stock <= 0;
    public decimal MarginPercent => UnitCost <= 0 ? 0 : Math.Round((Price - UnitCost) / UnitCost * 100m, 1);
    public decimal StockValue => Stock * UnitCost;
}

/// <summary>View model for the Add / Edit part form.</summary>
public class PartFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Part code is required.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Part code must be 3 to 20 characters.")]
    [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "Use letters, numbers and dashes only (e.g. OIL-5W30).")]
    [Display(Name = "Part code")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Part name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Name must be 2 to 120 characters.")]
    [Display(Name = "Part name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public string Category { get; set; } = "";

    [Required(ErrorMessage = "Current stock is required.")]
    [Range(0, 100000, ErrorMessage = "Stock must be between 0 and 100,000.")]
    [Display(Name = "Current stock")]
    public int? Stock { get; set; }

    [Required(ErrorMessage = "Reorder level is required.")]
    [Range(0, 100000, ErrorMessage = "Reorder level must be between 0 and 100,000.")]
    [Display(Name = "Reorder level")]
    public int? ReorderLevel { get; set; }

    [Required(ErrorMessage = "Unit cost is required.")]
    [Range(0.01, 10000000, ErrorMessage = "Unit cost must be between LKR 0.01 and LKR 10,000,000.")]
    [Display(Name = "Unit cost (LKR)")]
    public decimal? UnitCost { get; set; }

    [Required(ErrorMessage = "Selling price is required.")]
    [Range(0.01, 10000000, ErrorMessage = "Price must be between LKR 0.01 and LKR 10,000,000.")]
    [Display(Name = "Selling price (LKR)")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Please select a supplier.")]
    [Display(Name = "Supplier")]
    public string Supplier { get; set; } = "";

    [Display(Name = "Active part")]
    public bool IsActive { get; set; } = true;

    public List<string> CategoryOptions { get; set; } = new();
    public List<string> SupplierOptions { get; set; } = new();
}

/// <summary>View model for the parts list page.</summary>
public class PartListViewModel
{
    public List<PartRecord> Items { get; set; } = new();
    public string Search { get; set; } = "";
    public string Category { get; set; } = "";
    public string Stock { get; set; } = "";
    public List<string> Categories { get; set; } = new();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 8;
    public int TotalPages { get; set; } = 1;
    public int FilteredCount { get; set; }

    public int TotalCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal StockValue { get; set; }
}

/// <summary>A reorder request raised from the low-stock page.</summary>
public class ReorderRecord
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public string PartCode { get; set; } = "";
    public string PartName { get; set; } = "";
    public string Supplier { get; set; } = "";
    public int Quantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTime RequestedOn { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Requested";
}

/// <summary>Posted from the "Create reorder" popup.</summary>
public class ReorderFormModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid part.")]
    public int PartId { get; set; }

    [Required(ErrorMessage = "Enter the quantity to order.")]
    [Range(1, 100000, ErrorMessage = "Quantity must be between 1 and 100,000.")]
    public int? Quantity { get; set; }
}

/// <summary>View model for the low-stock alert page.</summary>
public class LowStockViewModel
{
    public List<PartRecord> Items { get; set; } = new();
    public List<ReorderRecord> RecentReorders { get; set; } = new();
    public int OutOfStockCount { get; set; }
    public decimal EstimatedReorderCost { get; set; }
}
