namespace AutoTrack.Models;

/// <summary>
/// In-memory thread-safe store for Appointments (Option 4).
/// Simulates garage scheduling and booked slots without requiring a database.
/// </summary>
public static class MockAppointmentData
{
    public static readonly string[] Statuses = { "Pending", "Confirmed", "Cancelled" };

    public static readonly string[] TimeSlots =
    {
        "08:30 AM - 10:00 AM",
        "10:00 AM - 11:30 AM",
        "11:30 AM - 01:00 PM",
        "01:30 PM - 03:00 PM",
        "03:00 PM - 04:30 PM",
        "04:30 PM - 06:00 PM"
    };

    public static readonly string[] ServiceTypes =
    {
        "Full Periodic Service",
        "Brake Inspection & Pad Replacement",
        "Engine Diagnostics & Tune-up",
        "Wheel Alignment & Suspension Check",
        "Air Conditioning Service & Regas",
        "Vehicle Modification & Body Styling",
        "Transmission Fluid & Gearbox Service",
        "Oil & Filter Express Service",
        "General Safety & Roadworthiness Check"
    };

    private static readonly object Sync = new();
    private static readonly List<AppointmentViewModel> Appointments = Seed();

    public static List<AppointmentViewModel> All()
    {
        lock (Sync)
        {
            return Appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.TimeSlot)
                .Select(Clone)
                .ToList();
        }
    }

    public static AppointmentViewModel? Get(int id)
    {
        lock (Sync)
        {
            var item = Appointments.FirstOrDefault(a => a.Id == id);
            return item == null ? null : Clone(item);
        }
    }

    public static AppointmentViewModel? GetByAppointmentNo(string appointmentNo)
    {
        lock (Sync)
        {
            var item = Appointments.FirstOrDefault(a =>
                string.Equals(a.AppointmentNo, appointmentNo.Trim(), StringComparison.OrdinalIgnoreCase));
            return item == null ? null : Clone(item);
        }
    }

    public static void Save(AppointmentViewModel model)
    {
        lock (Sync)
        {
            if (model.Id == 0)
            {
                model.Id = Appointments.Count == 0 ? 1 : Appointments.Max(a => a.Id) + 1;
                model.AppointmentNo = $"APT-2026-{model.Id:000}";
                model.CreatedAt = DateTime.Now;
                Appointments.Add(Clone(model));
            }
            else
            {
                var index = Appointments.FindIndex(a => a.Id == model.Id);
                if (index >= 0)
                {
                    model.AppointmentNo = Appointments[index].AppointmentNo;
                    Appointments[index] = Clone(model);
                }
            }
        }
    }

    public static bool Confirm(int id)
    {
        lock (Sync)
        {
            var item = Appointments.FirstOrDefault(a => a.Id == id);
            if (item == null) return false;
            item.Status = "Confirmed";
            return true;
        }
    }

    public static bool Cancel(int id, string? reason)
    {
        lock (Sync)
        {
            var item = Appointments.FirstOrDefault(a => a.Id == id);
            if (item == null) return false;
            item.Status = "Cancelled";
            item.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled by Front Desk / Customer" : reason.Trim();
            return true;
        }
    }

    public static bool Reschedule(int id, DateTime newDate, string newSlot)
    {
        lock (Sync)
        {
            var item = Appointments.FirstOrDefault(a => a.Id == id);
            if (item == null) return false;

            // Check if slot is already taken by another active appointment on the target date
            bool slotTaken = Appointments.Any(a =>
                a.Id != id &&
                a.AppointmentDate.Date == newDate.Date &&
                string.Equals(a.TimeSlot, newSlot.Trim(), StringComparison.OrdinalIgnoreCase) &&
                a.Status != "Cancelled");

            if (slotTaken) return false;

            item.AppointmentDate = newDate.Date;
            item.TimeSlot = newSlot.Trim();
            item.Status = "Confirmed"; // Automatically mark as confirmed upon rescheduling
            return true;
        }
    }

    public static List<string> GetBookedSlots(DateTime date)
    {
        lock (Sync)
        {
            return Appointments
                .Where(a => a.AppointmentDate.Date == date.Date && a.Status != "Cancelled")
                .Select(a => a.TimeSlot)
                .Distinct()
                .ToList();
        }
    }

    public static bool IsSlotBooked(DateTime date, string slot, int? excludeId = null)
    {
        lock (Sync)
        {
            return Appointments.Any(a =>
                (!excludeId.HasValue || a.Id != excludeId.Value) &&
                a.AppointmentDate.Date == date.Date &&
                string.Equals(a.TimeSlot, slot.Trim(), StringComparison.OrdinalIgnoreCase) &&
                a.Status != "Cancelled");
        }
    }

    private static AppointmentViewModel Clone(AppointmentViewModel a) => new()
    {
        Id = a.Id,
        AppointmentNo = a.AppointmentNo,
        CustomerName = a.CustomerName,
        CustomerPhone = a.CustomerPhone,
        CustomerEmail = a.CustomerEmail,
        VehicleNo = a.VehicleNo,
        VehicleModel = a.VehicleModel,
        ServiceType = a.ServiceType,
        AppointmentDate = a.AppointmentDate,
        TimeSlot = a.TimeSlot,
        Notes = a.Notes,
        Status = a.Status,
        CreatedAt = a.CreatedAt,
        CancellationReason = a.CancellationReason
    };

    private static List<AppointmentViewModel> Seed()
    {
        var today = DateTime.Today;

        return new List<AppointmentViewModel>
        {
            // Today's appointments
            new()
            {
                Id = 1,
                AppointmentNo = "APT-2026-001",
                CustomerName = "Kasun Silva",
                CustomerPhone = "077-1234567",
                CustomerEmail = "kasun.silva@gmail.com",
                VehicleNo = "CAB-1234",
                VehicleModel = "Mitsubishi Montero Sport 2018",
                ServiceType = "Full Periodic Service",
                AppointmentDate = today,
                TimeSlot = "08:30 AM - 10:00 AM",
                Notes = "Perform 40,000 km general service, replace engine oil and filters. Inspect brake pads.",
                Status = "Confirmed",
                CreatedAt = today.AddDays(-2)
            },
            new()
            {
                Id = 2,
                AppointmentNo = "APT-2026-002",
                CustomerName = "Pradeep Gunawardena",
                CustomerPhone = "071-8899001",
                CustomerEmail = "pradeep.guna@yahoo.com",
                VehicleNo = "BEH-3302",
                VehicleModel = "Honda Vezel RU3 2015",
                ServiceType = "Brake Inspection & Pad Replacement",
                AppointmentDate = today,
                TimeSlot = "11:30 AM - 01:00 PM",
                Notes = "Squeaking sound from front wheels when applying heavy brakes.",
                Status = "Confirmed",
                CreatedAt = today.AddDays(-1)
            },
            new()
            {
                Id = 3,
                AppointmentNo = "APT-2026-003",
                CustomerName = "Sanduni Rathnayake",
                CustomerPhone = "076-3344556",
                CustomerEmail = "sanduni.r@hotmail.com",
                VehicleNo = "CAD-7788",
                VehicleModel = "Toyota Aqua 2017",
                ServiceType = "Air Conditioning Service & Regas",
                AppointmentDate = today,
                TimeSlot = "03:00 PM - 04:30 PM",
                Notes = "AC blower is blowing warm air after 15 minutes of driving.",
                Status = "Pending",
                CreatedAt = today
            },

            // Tomorrow's appointments
            new()
            {
                Id = 4,
                AppointmentNo = "APT-2026-004",
                CustomerName = "Amila Jayasinghe",
                CustomerPhone = "071-2345678",
                CustomerEmail = "amila.j@outlook.com",
                VehicleNo = "KP-5678",
                VehicleModel = "Nissan X-Trail T32",
                ServiceType = "Engine Diagnostics & Tune-up",
                AppointmentDate = today.AddDays(1),
                TimeSlot = "10:00 AM - 11:30 AM",
                Notes = "Check engine indicator is on intermittently. Slight rough idle in the morning.",
                Status = "Pending",
                CreatedAt = today
            },
            new()
            {
                Id = 5,
                AppointmentNo = "APT-2026-005",
                CustomerName = "Ruwan Fernando",
                CustomerPhone = "077-9876543",
                CustomerEmail = "ruwan.fernando@gmail.com",
                VehicleNo = "WP CBD-9021",
                VehicleModel = "Toyota Land Cruiser Prado 150",
                ServiceType = "Vehicle Modification & Body Styling",
                AppointmentDate = today.AddDays(1),
                TimeSlot = "01:30 PM - 03:00 PM",
                Notes = "Fit front nudge bar, LED spot lamps and wheel arch fender flares.",
                Status = "Confirmed",
                CreatedAt = today.AddDays(-1)
            },
            new()
            {
                Id = 6,
                AppointmentNo = "APT-2026-006",
                CustomerName = "Tharindu Bandara",
                CustomerPhone = "072-4455667",
                CustomerEmail = "tharindu.b@gmail.com",
                VehicleNo = "NC-4410",
                VehicleModel = "Suzuki Swift RS 2018",
                ServiceType = "Oil & Filter Express Service",
                AppointmentDate = today.AddDays(1),
                TimeSlot = "04:30 PM - 06:00 PM",
                Notes = "Use Motul 0W-20 fully synthetic engine oil and OEM filter.",
                Status = "Cancelled",
                CancellationReason = "Customer postponed due to overseas travel.",
                CreatedAt = today.AddDays(-3)
            },

            // Day after tomorrow
            new()
            {
                Id = 7,
                AppointmentNo = "APT-2026-007",
                CustomerName = "Chamari Dissanayake",
                CustomerPhone = "078-7788990",
                CustomerEmail = "chamari.d@gmail.com",
                VehicleNo = "CAF-9100",
                VehicleModel = "Hyundai Tucson 2021",
                ServiceType = "Wheel Alignment & Suspension Check",
                AppointmentDate = today.AddDays(2),
                TimeSlot = "08:30 AM - 10:00 AM",
                Notes = "Vehicle pulls slightly to the left on highway speeds. Calibrate wheel alignment.",
                Status = "Pending",
                CreatedAt = today
            },
            new()
            {
                Id = 8,
                AppointmentNo = "APT-2026-008",
                CustomerName = "Ishara Madushanka",
                CustomerPhone = "075-1122334",
                CustomerEmail = "ishara.m@gmail.com",
                VehicleNo = "SP-2201",
                VehicleModel = "Toyota Hilux Revo",
                ServiceType = "Transmission Fluid & Gearbox Service",
                AppointmentDate = today.AddDays(2),
                TimeSlot = "01:30 PM - 03:00 PM",
                Notes = "Automatic transmission fluid flush and differential oil replacement.",
                Status = "Confirmed",
                CreatedAt = today.AddDays(-1)
            },

            // Past appointment
            new()
            {
                Id = 9,
                AppointmentNo = "APT-2026-009",
                CustomerName = "Nuwan Wickramasinghe",
                CustomerPhone = "070-5566778",
                CustomerEmail = "nuwan.wick@gmail.com",
                VehicleNo = "WP CAA-4411",
                VehicleModel = "Honda Civic FK7",
                ServiceType = "Full Periodic Service",
                AppointmentDate = today.AddDays(-1),
                TimeSlot = "10:00 AM - 11:30 AM",
                Notes = "Regular 30k service completed.",
                Status = "Confirmed",
                CreatedAt = today.AddDays(-4)
            }
        };
    }
}

