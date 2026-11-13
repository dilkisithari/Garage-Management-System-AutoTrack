namespace AutoTrack.Models;

/// <summary>In-memory fake data so the job card screens work without a database.</summary>
public static class MockJobData
{
    public static readonly string[] Statuses = { "Pending", "In Progress", "Waiting for Parts", "Completed" };
    public static readonly string[] Mechanics = { "Nuwan Perera", "Saman Kumara", "Kasun Silva", "Dilshan Fernando" };

    private static readonly object Sync = new();
    private static readonly List<JobCardViewModel> Jobs = Seed();

    public static List<JobCardViewModel> All()
    {
        lock (Sync) { return Jobs.OrderByDescending(j => j.DateOpened).ThenByDescending(j => j.Id).ToList(); }
    }

    public static JobCardViewModel? Get(int id)
    {
        lock (Sync) { return Jobs.FirstOrDefault(j => j.Id == id); }
    }

    public static void Save(JobCardViewModel job)
    {
        lock (Sync)
        {
            if (job.Id == 0)
            {
                job.Id = Jobs.Count == 0 ? 1 : Jobs.Max(j => j.Id) + 1;
                job.JobNo = $"JC-{job.Id:000}";
                Jobs.Add(job);
            }
            else
            {
                var index = Jobs.FindIndex(j => j.Id == job.Id);
                if (index >= 0)
                {
                    job.JobNo = Jobs[index].JobNo;
                    Jobs[index] = job;
                }
            }
        }
    }

    private static List<JobCardViewModel> Seed()
    {
        var today = DateTime.Today;
        return new List<JobCardViewModel>
        {
            new()
            {
                Id = 1, JobNo = "JC-001", VehicleNo = "CAB-1234", CustomerName = "Kasun Silva",
                Complaint = "Brake noise when stopping", Mechanic = "Nuwan Perera", Status = "In Progress",
                DateOpened = today.AddDays(-1),
                Tasks = { new() { Description = "Inspect brake pads", Done = true }, new() { Description = "Replace front pads" } },
                Parts = { new() { PartName = "Front brake pads", Quantity = 1, UnitPrice = 8500 } },
                Labour = { new() { Description = "Brake service", Hours = 1.5m, Rate = 2000 } }
            },
            new()
            {
                Id = 2, JobNo = "JC-002", VehicleNo = "KP-5678", CustomerName = "Amila Jayasinghe",
                Complaint = "Engine tune-up and oil change", Mechanic = "Saman Kumara", Status = "Pending",
                DateOpened = today,
                Tasks = { new() { Description = "Change engine oil" }, new() { Description = "Replace spark plugs" } },
                Parts = { new() { PartName = "Engine oil 5W-30 (4L)", Quantity = 1, UnitPrice = 12500 },
                          new() { PartName = "Spark plug", Quantity = 4, UnitPrice = 1800 } },
                Labour = { new() { Description = "Tune-up", Hours = 2, Rate = 2000 } }
            },
            new()
            {
                Id = 3, JobNo = "JC-003", VehicleNo = "WP CBD-9021", CustomerName = "Ruwan Fernando",
                Complaint = "AC not cooling", Mechanic = "Kasun Silva", Status = "Waiting for Parts",
                DateOpened = today.AddDays(-3),
                Tasks = { new() { Description = "Gas pressure test", Done = true }, new() { Description = "Replace compressor" } },
                Parts = { new() { PartName = "AC compressor", Quantity = 1, UnitPrice = 65000 } },
                Labour = { new() { Description = "AC repair", Hours = 3, Rate = 2500 } }
            },
            new()
            {
                Id = 4, JobNo = "JC-004", VehicleNo = "NC-4410", CustomerName = "Tharindu Bandara",
                Complaint = "Full body modification wrap", Mechanic = "Dilshan Fernando", Status = "In Progress",
                DateOpened = today.AddDays(-2),
                Tasks = { new() { Description = "Remove old trims", Done = true }, new() { Description = "Apply matte black wrap" } },
                Parts = { new() { PartName = "Wrap film (matte black)", Quantity = 1, UnitPrice = 85000 } },
                Labour = { new() { Description = "Wrap installation", Hours = 12, Rate = 2500 } }
            },
            new()
            {
                Id = 5, JobNo = "JC-005", VehicleNo = "CAD-7788", CustomerName = "Sanduni Rathnayake",
                Complaint = "Wheel alignment and balancing", Mechanic = "Nuwan Perera", Status = "Completed",
                DateOpened = today.AddDays(-4),
                Tasks = { new() { Description = "Alignment", Done = true }, new() { Description = "Balancing", Done = true } },
                Labour = { new() { Description = "Alignment and balancing", Hours = 1, Rate = 3500 } }
            },
            new()
            {
                Id = 6, JobNo = "JC-006", VehicleNo = "BEH-3302", CustomerName = "Pradeep Gunawardena",
                Complaint = "Battery replacement", Mechanic = "Saman Kumara", Status = "Completed",
                DateOpened = today.AddDays(-5),
                Tasks = { new() { Description = "Test alternator", Done = true }, new() { Description = "Install new battery", Done = true } },
                Parts = { new() { PartName = "Car battery 65Ah", Quantity = 1, UnitPrice = 24500 } },
                Labour = { new() { Description = "Battery fitting", Hours = 0.5m, Rate = 2000 } }
            },
            new()
            {
                Id = 7, JobNo = "JC-007", VehicleNo = "CAF-9100", CustomerName = "Chamari Dissanayake",
                Complaint = "Suspension knocking over bumps", Mechanic = "Kasun Silva", Status = "Pending",
                DateOpened = today,
                Tasks = { new() { Description = "Inspect shock absorbers" } }
            },
            new()
            {
                Id = 8, JobNo = "JC-008", VehicleNo = "SP-2201", CustomerName = "Ishara Madushanka",
                Complaint = "Exhaust upgrade and ECU remap", Mechanic = "Dilshan Fernando", Status = "Waiting for Parts",
                DateOpened = today.AddDays(-6),
                Tasks = { new() { Description = "Fit sport exhaust" }, new() { Description = "ECU remap" } },
                Parts = { new() { PartName = "Sport exhaust system", Quantity = 1, UnitPrice = 120000 } },
                Labour = { new() { Description = "Exhaust fitting and remap", Hours = 5, Rate = 3000 } }
            },
        };
    }
}
