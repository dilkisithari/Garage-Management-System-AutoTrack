namespace AutoTrack.Models.Parts;

/// <summary>
/// In-memory, thread-safe store for parts (Option 6). No database yet:
/// data lives only while the app is running.
/// </summary>
public static class MockPartData
{
    public static readonly string[] Categories =
    {
        "Engine Oil & Fluids", "Filters", "Brakes", "Electrical", "Ignition", "Suspension", "Clutch & Drivetrain", "Body & Styling"
    };

    // Supplier names/phones mirror the garage's supplier directory so Option 6 works on its own.
    public static readonly (string Name, string Phone)[] Suppliers =
    {
        ("Ceylon Lubricants & Fluids Ltd", "+94 11 287 4100"),
        ("Colombo Motor Spares (Pvt) Ltd", "+94 11 243 8921"),
        ("Pettah Auto Parts Center", "+94 11 232 1555"),
        ("Lanka Auto Traders", "+94 11 258 7720"),
        ("Exide Lanka Battery Distributors", "+94 11 278 6300"),
        ("Apex Performance & Wrap Supplies", "+94 77 345 8890")
    };

    // NOTE: static fields initialise in file order, so the stores must come after the arrays above.
    private static readonly object Sync = new();
    private static readonly List<PartRecord> Parts = SeedParts();
    private static readonly List<ReorderRecord> Reorders = new();

    public static List<string> SupplierNames() => Suppliers.Select(s => s.Name).ToList();

    public static List<PartRecord> All()
    {
        lock (Sync) { return Parts.OrderBy(p => p.Code).Select(Clone).ToList(); }
    }

    public static PartRecord? Get(int id)
    {
        lock (Sync)
        {
            var match = Parts.FirstOrDefault(p => p.Id == id);
            return match == null ? null : Clone(match);
        }
    }

    public static bool CodeExists(string code, int excludeId)
    {
        lock (Sync)
        {
            return Parts.Any(p => p.Id != excludeId && p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static void Save(PartRecord part)
    {
        lock (Sync)
        {
            part.SupplierPhone = Suppliers.FirstOrDefault(s => s.Name == part.Supplier).Phone ?? "";
            if (part.Id == 0)
            {
                part.Id = Parts.Count == 0 ? 1 : Parts.Max(p => p.Id) + 1;
                Parts.Add(Clone(part));
            }
            else
            {
                var index = Parts.FindIndex(p => p.Id == part.Id);
                if (index >= 0) Parts[index] = Clone(part);
            }
        }
    }

    public static bool Delete(int id)
    {
        lock (Sync)
        {
            var match = Parts.FirstOrDefault(p => p.Id == id);
            if (match == null) return false;
            Parts.Remove(match);
            return true;
        }
    }

    public static ReorderRecord? AddReorder(int partId, int quantity)
    {
        lock (Sync)
        {
            var part = Parts.FirstOrDefault(p => p.Id == partId);
            if (part == null) return null;
            var reorder = new ReorderRecord
            {
                Id = Reorders.Count == 0 ? 1 : Reorders.Max(r => r.Id) + 1,
                PartId = part.Id,
                PartCode = part.Code,
                PartName = part.Name,
                Supplier = part.Supplier,
                Quantity = quantity,
                EstimatedCost = quantity * part.UnitCost,
                RequestedOn = DateTime.Now
            };
            Reorders.Add(reorder);
            return reorder;
        }
    }

    public static List<ReorderRecord> RecentReorders(int take = 5)
    {
        lock (Sync) { return Reorders.OrderByDescending(r => r.RequestedOn).Take(take).ToList(); }
    }

    public static bool HasOpenReorder(int partId)
    {
        lock (Sync) { return Reorders.Any(r => r.PartId == partId); }
    }

    private static PartRecord Clone(PartRecord p) => new()
    {
        Id = p.Id, Code = p.Code, Name = p.Name, Category = p.Category, Stock = p.Stock,
        ReorderLevel = p.ReorderLevel, UnitCost = p.UnitCost, Price = p.Price,
        Supplier = p.Supplier, SupplierPhone = p.SupplierPhone, IsActive = p.IsActive
    };

    private static PartRecord P(int id, string code, string name, string category, int stock, int reorder,
        decimal cost, decimal price, int supplierIndex) => new()
    {
        Id = id, Code = code, Name = name, Category = category, Stock = stock, ReorderLevel = reorder,
        UnitCost = cost, Price = price,
        Supplier = Suppliers[supplierIndex].Name, SupplierPhone = Suppliers[supplierIndex].Phone
    };

    private static List<PartRecord> SeedParts() => new()
    {
        P(1,  "OIL-5W30", "Engine Oil 5W-30 Synthetic (4L)", "Engine Oil & Fluids", 24, 10, 9800m,  12500m, 0),
        P(2,  "OIL-FLT1", "Oil Filter - Toyota Premio/Allion", "Filters",            6,  12, 1450m,  2200m,  1),
        P(3,  "AIR-FLT2", "Air Filter - Suzuki Alto/Wagon R", "Filters",             15, 8,  1800m,  2750m,  1),
        P(4,  "BRK-PAD1", "Front Brake Pads - Honda Fit",     "Brakes",              4,  10, 6200m,  8900m,  2),
        P(5,  "BRK-DSC3", "Brake Disc Rotor (pair) - Nissan Sunny", "Brakes",        9,  4,  14500m, 19800m, 2),
        P(6,  "BAT-45AH", "Car Battery 45Ah (NS60)",          "Electrical",          3,  5,  17500m, 22900m, 4),
        P(7,  "BAT-65AH", "Car Battery 65Ah (N50)",           "Electrical",          8,  4,  24800m, 31500m, 4),
        P(8,  "SPK-NGK4", "NGK Spark Plug Iridium (set of 4)", "Ignition",           0,  6,  7600m,  10400m, 3),
        P(9,  "CLT-KIT2", "Clutch Plate Kit - Toyota Axio",   "Clutch & Drivetrain", 5,  3,  28500m, 38000m, 3),
        P(10, "SUS-BSH1", "Front Suspension Bush Set",        "Suspension",          11, 6,  4300m,  6500m,  3),
        P(11, "WRP-MBK5", "Matte Black Vinyl Wrap (5m roll)", "Body & Styling",      2,  3,  18900m, 26500m, 5),
        P(12, "COL-5050", "Radiator Coolant 50/50 (4L)",      "Engine Oil & Fluids", 20, 8,  3200m,  4800m,  0)
    };
}
