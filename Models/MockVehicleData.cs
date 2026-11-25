namespace AutoTrack.Models;

/// <summary>
/// Represents a registered vehicle in the AutoTrack system (Option 2).
/// </summary>
public class VehicleRecord
{
    public int Id { get; set; }
    public string RegistrationNo { get; set; } = "";
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; } = DateTime.Today.Year;
    public string? Colour { get; set; }
    public int? Mileage { get; set; }
    /// <summary>FK to CustomerRecord.Id</summary>
    public int OwnerId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Today;
}

/// <summary>
/// Thread-safe in-memory store for vehicles (Option 2).
/// Pre-seeded with vehicles linked to the seeded customers.
/// Replace with EF Core in the database phase.
/// </summary>
public static class MockVehicleData
{
    private static readonly object Sync = new();
    private static readonly List<VehicleRecord> Vehicles = Seed();

    public static List<VehicleRecord> All()
    {
        lock (Sync)
        {
            return Vehicles.OrderBy(v => v.RegistrationNo).Select(Clone).ToList();
        }
    }

    public static VehicleRecord? Get(int id)
    {
        lock (Sync)
        {
            var match = Vehicles.FirstOrDefault(v => v.Id == id);
            return match == null ? null : Clone(match);
        }
    }

    /// <summary>Returns all vehicles belonging to the given customer.</summary>
    public static List<VehicleRecord> GetByOwner(int ownerId)
    {
        lock (Sync)
        {
            return Vehicles
                .Where(v => v.OwnerId == ownerId)
                .OrderBy(v => v.RegistrationNo)
                .Select(Clone)
                .ToList();
        }
    }

    /// <summary>Returns true if at least one vehicle is linked to the customer.</summary>
    public static bool HasVehicles(int customerId)
    {
        lock (Sync) { return Vehicles.Any(v => v.OwnerId == customerId); }
    }

    /// <summary>Saves a vehicle (insert if Id == 0, update otherwise).
    /// Registration number is normalised to upper-case.</summary>
    public static void Save(VehicleRecord vehicle)
    {
        lock (Sync)
        {
            vehicle.RegistrationNo = vehicle.RegistrationNo.Trim().ToUpperInvariant();

            if (vehicle.Id == 0)
            {
                vehicle.Id = Vehicles.Count == 0 ? 1 : Vehicles.Max(v => v.Id) + 1;
                vehicle.CreatedDate = DateTime.Today;
                Vehicles.Add(Clone(vehicle));
            }
            else
            {
                var index = Vehicles.FindIndex(v => v.Id == vehicle.Id);
                if (index >= 0)
                {
                    vehicle.CreatedDate = Vehicles[index].CreatedDate;
                    Vehicles[index] = Clone(vehicle);
                }
            }
        }
    }

    /// <summary>Deletes a vehicle by Id. Returns false if not found.</summary>
    public static bool Delete(int id)
    {
        lock (Sync)
        {
            var v = Vehicles.FirstOrDefault(x => x.Id == id);
            if (v == null) return false;
            Vehicles.Remove(v);
            return true;
        }
    }

    /// <summary>Returns the total number of registered vehicles.</summary>
    public static int Count()
    {
        lock (Sync) { return Vehicles.Count; }
    }

    private static VehicleRecord Clone(VehicleRecord v) => new()
    {
        Id = v.Id,
        RegistrationNo = v.RegistrationNo,
        Make = v.Make,
        Model = v.Model,
        Year = v.Year,
        Colour = v.Colour,
        Mileage = v.Mileage,
        OwnerId = v.OwnerId,
        CreatedDate = v.CreatedDate
    };

    private static List<VehicleRecord> Seed() => new()
    {
        new()
        {
            Id = 1,
            RegistrationNo = "CAB-1234",
            Make = "Mitsubishi",
            Model = "Montero Sport",
            Year = 2018,
            Colour = "Pearl White",
            Mileage = 62000,
            OwnerId = 1,   // Kasun Silva
            CreatedDate = DateTime.Today.AddMonths(-8)
        },
        new()
        {
            Id = 2,
            RegistrationNo = "WP-GH-9981",
            Make = "Toyota",
            Model = "Corolla",
            Year = 2020,
            Colour = "Silver",
            Mileage = 38000,
            OwnerId = 2,   // Nimal Perera
            CreatedDate = DateTime.Today.AddMonths(-6)
        },
        new()
        {
            Id = 3,
            RegistrationNo = "NB-5566",
            Make = "Honda",
            Model = "Vezel",
            Year = 2021,
            Colour = "Gunmetal Grey",
            Mileage = 25000,
            OwnerId = 3,   // Sunethra Jayawardena
            CreatedDate = DateTime.Today.AddMonths(-4)
        },
        new()
        {
            Id = 4,
            RegistrationNo = "CP-4432",
            Make = "Suzuki",
            Model = "Vitara",
            Year = 2019,
            Colour = "Red",
            Mileage = 51000,
            OwnerId = 4,   // Roshan Fernando
            CreatedDate = DateTime.Today.AddMonths(-3)
        },
        new()
        {
            Id = 5,
            RegistrationNo = "WP-LAA-7723",
            Make = "Nissan",
            Model = "Dayz",
            Year = 2022,
            Colour = "Sky Blue",
            Mileage = 12000,
            OwnerId = 5,   // Amali Dissanayake
            CreatedDate = DateTime.Today.AddMonths(-2)
        },
        new()
        {
            Id = 6,
            RegistrationNo = "CAR-8800",
            Make = "Toyota",
            Model = "Prius",
            Year = 2017,
            Colour = "Black",
            Mileage = 88000,
            OwnerId = 1,   // Kasun Silva (second vehicle)
            CreatedDate = DateTime.Today.AddMonths(-1)
        }
    };
}
