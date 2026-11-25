namespace AutoTrack.Models;

/// <summary>
/// Represents a registered customer in the AutoTrack system (Option 2).
/// </summary>
public class CustomerRecord
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Today;
}

/// <summary>
/// Thread-safe in-memory store for customers (Option 2).
/// Pre-seeded with realistic Sri Lankan customer records.
/// Replace with EF Core in the database phase.
/// </summary>
public static class MockCustomerData
{
    private static readonly object Sync = new();
    private static readonly List<CustomerRecord> Customers = Seed();

    public static List<CustomerRecord> All()
    {
        lock (Sync)
        {
            return Customers.OrderBy(c => c.FullName).Select(Clone).ToList();
        }
    }

    public static List<CustomerRecord> Active() => All().Where(c => c.IsActive).ToList();

    public static bool SetActive(int id, bool isActive)
    {
        lock (Sync)
        {
            var customer = Customers.FirstOrDefault(c => c.Id == id);
            if (customer == null) return false;
            customer.IsActive = isActive;
            return true;
        }
    }

    public static CustomerRecord? Get(int id)
    {
        lock (Sync)
        {
            var match = Customers.FirstOrDefault(c => c.Id == id);
            return match == null ? null : Clone(match);
        }
    }

    /// <summary>Returns a list of customers matching the search term (name or phone).</summary>
    public static List<CustomerRecord> Search(string? term)
    {
        lock (Sync)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Customers.OrderBy(c => c.FullName).Select(Clone).ToList();

            var t = term.Trim().ToLower();
            return Customers
                .Where(c => c.FullName.ToLower().Contains(t) || c.Phone.Contains(t))
                .OrderBy(c => c.FullName)
                .Select(Clone)
                .ToList();
        }
    }

    /// <summary>Saves a customer (insert if Id == 0, update otherwise).</summary>
    public static void Save(CustomerRecord customer)
    {
        lock (Sync)
        {
            if (customer.Id == 0)
            {
                customer.Id = Customers.Count == 0 ? 1 : Customers.Max(c => c.Id) + 1;
                customer.CreatedDate = DateTime.Today;
                Customers.Add(Clone(customer));
            }
            else
            {
                var index = Customers.FindIndex(c => c.Id == customer.Id);
                if (index >= 0)
                {
                    customer.CreatedDate = Customers[index].CreatedDate;
                    Customers[index] = Clone(customer);
                }
            }
        }
    }

    /// <summary>Deletes a customer by Id. Returns false if not found.</summary>
    public static bool Delete(int id)
    {
        lock (Sync)
        {
            var c = Customers.FirstOrDefault(x => x.Id == id);
            if (c == null) return false;
            Customers.Remove(c);
            return true;
        }
    }

    /// <summary>Returns the total number of registered customers.</summary>
    public static int Count()
    {
        lock (Sync) { return Customers.Count; }
    }

    private static CustomerRecord Clone(CustomerRecord c) => new()
    {
        Id = c.Id,
        FullName = c.FullName,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        IsActive = c.IsActive,
        CreatedDate = c.CreatedDate
    };

    private static List<CustomerRecord> Seed() => new()
    {
        new()
        {
            Id = 1,
            FullName = "Kasun Silva",
            Phone = "+94 77 123 4567",
            Email = "kasun.silva@gmail.com",
            Address = "15 Flower Road, Colombo 03",
            CreatedDate = DateTime.Today.AddMonths(-8)
        },
        new()
        {
            Id = 2,
            FullName = "Nimal Perera",
            Phone = "+94 71 234 5678",
            Email = "nimal.perera@yahoo.com",
            Address = "42 Galle Road, Moratuwa",
            CreatedDate = DateTime.Today.AddMonths(-6)
        },
        new()
        {
            Id = 3,
            FullName = "Sunethra Jayawardena",
            Phone = "+94 76 345 6789",
            Email = "sunethra.j@outlook.com",
            Address = "88 High Level Road, Nugegoda",
            CreatedDate = DateTime.Today.AddMonths(-4)
        },
        new()
        {
            Id = 4,
            FullName = "Roshan Fernando",
            Phone = "+94 70 456 7890",
            Email = null,
            Address = "23 Duplication Road, Bambalapitiya",
            CreatedDate = DateTime.Today.AddMonths(-3)
        },
        new()
        {
            Id = 5,
            FullName = "Amali Dissanayake",
            Phone = "+94 75 567 8901",
            Email = "amali.d@gmail.com",
            Address = "5 Nawala Road, Rajagiriya",
            CreatedDate = DateTime.Today.AddMonths(-2)
        }
    };
}
