namespace AutoTrack.Models;

/// <summary>
/// In-memory thread-safe store for suppliers (Option 7).
/// Pre-seeded with automotive parts suppliers matching workshop inventory.
/// </summary>
public static class MockSupplierData
{
    private static readonly object Sync = new();
    private static readonly List<SupplierViewModel> Suppliers = Seed();

    public static List<SupplierViewModel> All()
    {
        lock (Sync)
        {
            return Suppliers.OrderByDescending(s => s.IsActive).ThenBy(s => s.Name).Select(Clone).ToList();
        }
    }

    public static SupplierViewModel? Get(int id)
    {
        lock (Sync)
        {
            var match = Suppliers.FirstOrDefault(s => s.Id == id);
            return match == null ? null : Clone(match);
        }
    }

    public static void Save(SupplierViewModel supplier)
    {
        lock (Sync)
        {
            if (supplier.Id == 0)
            {
                supplier.Id = Suppliers.Count == 0 ? 1 : Suppliers.Max(s => s.Id) + 1;
                supplier.CreatedDate = DateTime.Today;
                Suppliers.Add(Clone(supplier));
            }
            else
            {
                var index = Suppliers.FindIndex(s => s.Id == supplier.Id);
                if (index >= 0)
                {
                    supplier.CreatedDate = Suppliers[index].CreatedDate;
                    Suppliers[index] = Clone(supplier);
                }
            }
        }
    }

    public static bool Delete(int id)
    {
        lock (Sync)
        {
            var s = Suppliers.FirstOrDefault(x => x.Id == id);
            if (s == null) return false;
            Suppliers.Remove(s);
            return true;
        }
    }

    public static bool ToggleActive(int id)
    {
        lock (Sync)
        {
            var s = Suppliers.FirstOrDefault(x => x.Id == id);
            if (s == null) return false;
            s.IsActive = !s.IsActive;
            return true;
        }
    }

    private static SupplierViewModel Clone(SupplierViewModel s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address,
        PartsSupplied = s.PartsSupplied,
        ContactPerson = s.ContactPerson,
        IsActive = s.IsActive,
        CreatedDate = s.CreatedDate,
        Notes = s.Notes
    };

    private static List<SupplierViewModel> Seed() => new()
    {
        new()
        {
            Id = 1,
            Name = "Ceylon Lubricants & Fluids Ltd",
            Phone = "+94 11 287 4100",
            Email = "sales@ceylonlubes.lk",
            Address = "45 Nawala Road, Rajagiriya, Colombo",
            PartsSupplied = "Engine oil 5W-30, Transmission fluid, Brake fluid, Radiator coolant",
            ContactPerson = "Sanjeewa Dissanayake",
            IsActive = true,
            CreatedDate = DateTime.Today.AddMonths(-6),
            Notes = "Official distributor for premium synthetic motor oils and coolants."
        },
        new()
        {
            Id = 2,
            Name = "Colombo Motor Spares (Pvt) Ltd",
            Phone = "+94 11 243 8921",
            Email = "orders@colombomotorspares.com",
            Address = "112 Panchikawatta Road, Colombo 10",
            PartsSupplied = "Oil filters, Air filters, Cabin filters, Timing belt kits, Drive belts",
            ContactPerson = "Rohan Jayawardena",
            IsActive = true,
            CreatedDate = DateTime.Today.AddMonths(-5),
            Notes = "Next-day delivery across Western Province for OEM filtration kits."
        },
        new()
        {
            Id = 3,
            Name = "Pettah Auto Parts Center",
            Phone = "+94 11 232 1555",
            Email = "contact@pettahautoparts.lk",
            Address = "88 Sri Ramanathan Road, Colombo 13",
            PartsSupplied = "Front brake pads, Rear brake shoes, Brake discs, Wiper blades",
            ContactPerson = "Mohamed Farook",
            IsActive = true,
            CreatedDate = DateTime.Today.AddMonths(-4),
            Notes = "Brembo and Akebono OEM brake pads specialist."
        },
        new()
        {
            Id = 4,
            Name = "Lanka Auto Traders",
            Phone = "+94 11 258 7720",
            Email = "info@lankaautotraders.lk",
            Address = "210 Highlevel Road, Nugegoda",
            PartsSupplied = "NGK Spark plugs, Clutch plate kits, Alternators, Starters, Suspension bushings",
            ContactPerson = "Chathura Wickramasinghe",
            IsActive = true,
            CreatedDate = DateTime.Today.AddMonths(-3),
            Notes = "Japanese and European ignition and drivetrain components."
        },
        new()
        {
            Id = 5,
            Name = "Exide Lanka Battery Distributors",
            Phone = "+94 11 278 6300",
            Email = "inquiries@exidelanka.lk",
            Address = "15 Pannipitiya Road, Battaramulla",
            PartsSupplied = "Car battery 45Ah, Car battery 65Ah, Heavy duty battery 90Ah, Battery terminals",
            ContactPerson = "Janaka Senanayake",
            IsActive = true,
            CreatedDate = DateTime.Today.AddMonths(-2),
            Notes = "Warranty replacement authorized depot within 5km of garage."
        },
        new()
        {
            Id = 6,
            Name = "Apex Performance & Wrap Supplies",
            Phone = "+94 77 345 8890",
            Email = "apex@performancewrap.lk",
            Address = "72 Thalawathugoda Road, Kotte",
            PartsSupplied = "Matte black vinyl wrap, Carbon fiber wrap film, Exhaust tips, LED projector headlights",
            ContactPerson = "Dilshan Fernando",
            IsActive = true,
            CreatedDate = DateTime.Today.AddMonths(-1),
            Notes = "Custom performance styling materials for vehicle modifications."
        }
    };
}
