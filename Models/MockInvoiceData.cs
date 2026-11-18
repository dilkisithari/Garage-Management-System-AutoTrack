namespace AutoTrack.Models;

/// <summary>
/// In-memory thread-safe store for Invoices and Payments (Option 7).
/// </summary>
public static class MockInvoiceData
{
    private static readonly object Sync = new();
    private static readonly List<InvoiceViewModel> Invoices = Seed();

    public static List<InvoiceViewModel> All()
    {
        lock (Sync)
        {
            return Invoices.OrderByDescending(i => i.IssueDate)
                           .ThenByDescending(i => i.Id)
                           .Select(Clone)
                           .ToList();
        }
    }

    public static InvoiceViewModel? Get(int id)
    {
        lock (Sync)
        {
            var inv = Invoices.FirstOrDefault(i => i.Id == id);
            return inv == null ? null : Clone(inv);
        }
    }

    public static InvoiceViewModel? GetByInvoiceNo(string invoiceNo)
    {
        lock (Sync)
        {
            var inv = Invoices.FirstOrDefault(i => string.Equals(i.InvoiceNo, invoiceNo, StringComparison.OrdinalIgnoreCase));
            return inv == null ? null : Clone(inv);
        }
    }

    public static void Save(InvoiceViewModel model)
    {
        lock (Sync)
        {
            if (model.Id == 0)
            {
                model.Id = Invoices.Count == 0 ? 1 : Invoices.Max(i => i.Id) + 1;
                model.InvoiceNo = $"INV-2026-{model.Id:000}";
                Invoices.Add(Clone(model));
            }
            else
            {
                var idx = Invoices.FindIndex(i => i.Id == model.Id);
                if (idx >= 0)
                {
                    model.InvoiceNo = Invoices[idx].InvoiceNo;
                    Invoices[idx] = Clone(model);
                }
            }
        }
    }

    public static InvoiceViewModel? CreateFromJobCard(int jobCardId)
    {
        var job = MockJobData.Get(jobCardId);
        if (job == null) return null;

        lock (Sync)
        {
            // Check if an invoice already exists for this job
            var existing = Invoices.FirstOrDefault(i => i.JobCardNo == job.JobNo);
            if (existing != null)
                return Clone(existing);

            var nextId = Invoices.Count == 0 ? 1 : Invoices.Max(i => i.Id) + 1;
            var invoice = new InvoiceViewModel
            {
                Id = nextId,
                InvoiceNo = $"INV-2026-{nextId:000}",
                JobCardNo = job.JobNo,
                IssueDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(7),
                CustomerName = job.CustomerName,
                CustomerPhone = "+94 77 123 4567",
                CustomerEmail = $"{job.CustomerName.ToLower().Replace(" ", ".")}@gmail.com",
                CustomerAddress = "Colombo, Sri Lanka",
                VehicleNo = job.VehicleNo,
                VehicleModel = "Toyota Prius / Axio",
                Mileage = 74500,
                Notes = $"Generated from Job Card {job.JobNo}. Scope: {job.Complaint}",
                Parts = job.Parts.Select(p => new InvoicePartItem
                {
                    PartName = p.PartName ?? "Spare Part",
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice
                }).ToList(),
                Labour = job.Labour.Select(l => new InvoiceLabourItem
                {
                    Description = l.Description ?? "Workshop Labour",
                    Hours = l.Hours,
                    HourlyRate = l.Rate
                }).ToList()
            };

            Invoices.Add(Clone(invoice));
            return invoice;
        }
    }

    public static bool AddPayment(PaymentEntryViewModel payment, string recordedBy)
    {
        lock (Sync)
        {
            var invoice = Invoices.FirstOrDefault(i => i.Id == payment.InvoiceId);
            if (invoice == null) return false;

            if (payment.Amount <= 0) return false;

            var nextPaymentId = invoice.Payments.Count == 0 ? 1 : invoice.Payments.Max(p => p.Id) + 1;
            var paymentRecord = new InvoicePaymentRecord
            {
                Id = nextPaymentId,
                Amount = Math.Round(payment.Amount, 2),
                PaymentMethod = payment.PaymentMethod,
                PaymentDate = payment.PaymentDate,
                ReferenceNo = payment.ReferenceNo,
                RecordedBy = recordedBy,
                Notes = payment.Notes
            };

            invoice.Payments.Add(paymentRecord);
            return true;
        }
    }

    private static InvoiceViewModel Clone(InvoiceViewModel src) => new()
    {
        Id = src.Id,
        InvoiceNo = src.InvoiceNo,
        JobCardNo = src.JobCardNo,
        IssueDate = src.IssueDate,
        DueDate = src.DueDate,
        CustomerName = src.CustomerName,
        CustomerPhone = src.CustomerPhone,
        CustomerEmail = src.CustomerEmail,
        CustomerAddress = src.CustomerAddress,
        VehicleNo = src.VehicleNo,
        VehicleModel = src.VehicleModel,
        Mileage = src.Mileage,
        Notes = src.Notes,
        Parts = src.Parts.Select(p => new InvoicePartItem
        {
            PartName = p.PartName,
            Quantity = p.Quantity,
            UnitPrice = p.UnitPrice
        }).ToList(),
        Labour = src.Labour.Select(l => new InvoiceLabourItem
        {
            Description = l.Description,
            Hours = l.Hours,
            HourlyRate = l.HourlyRate
        }).ToList(),
        Payments = src.Payments.Select(pm => new InvoicePaymentRecord
        {
            Id = pm.Id,
            Amount = pm.Amount,
            PaymentMethod = pm.PaymentMethod,
            PaymentDate = pm.PaymentDate,
            ReferenceNo = pm.ReferenceNo,
            RecordedBy = pm.RecordedBy,
            Notes = pm.Notes
        }).ToList()
    };

    private static List<InvoiceViewModel> Seed()
    {
        var today = DateTime.Today;

        return new List<InvoiceViewModel>
        {
            // Invoice 1: Fully Paid (via Bank Transfer)
            new()
            {
                Id = 1,
                InvoiceNo = "INV-2026-001",
                JobCardNo = "JC-005",
                IssueDate = today.AddDays(-4),
                DueDate = today.AddDays(3),
                CustomerName = "Sanduni Rathnayake",
                CustomerPhone = "+94 77 987 6543",
                CustomerEmail = "sanduni.r@yahoo.com",
                CustomerAddress = "14 Temple Road, Battaramulla",
                VehicleNo = "CAD-7788",
                VehicleModel = "Toyota Aqua 2017",
                Mileage = 62000,
                Notes = "Wheel alignment, computerized wheel balancing and suspension inspection completed.",
                Parts = new List<InvoicePartItem>(),
                Labour = new List<InvoiceLabourItem>
                {
                    new() { Description = "Computerized 4-wheel alignment and balancing", Hours = 1, HourlyRate = 3500 }
                },
                Payments = new List<InvoicePaymentRecord>
                {
                    new()
                    {
                        Id = 1,
                        Amount = 3500,
                        PaymentMethod = "Bank Transfer",
                        PaymentDate = today.AddDays(-4),
                        ReferenceNo = "BOC-TXN-994821",
                        RecordedBy = "Nimali Perera",
                        Notes = "Online transfer received via Bank of Ceylon"
                    }
                }
            },

            // Invoice 2: Partially Paid (Deposit paid with Card, balance remaining)
            new()
            {
                Id = 2,
                InvoiceNo = "INV-2026-002",
                JobCardNo = "JC-006",
                IssueDate = today.AddDays(-3),
                DueDate = today.AddDays(4),
                CustomerName = "Pradeep Gunawardena",
                CustomerPhone = "+94 71 456 7890",
                CustomerEmail = "pradeep.guna@gmail.com",
                CustomerAddress = "82 Kaduwela Road, Malabe",
                VehicleNo = "BEH-3302",
                VehicleModel = "Honda Vezel RU3 2015",
                Mileage = 91400,
                Notes = "Replaced 12V auxiliary battery and alternator charge test performed.",
                Parts = new List<InvoicePartItem>
                {
                    new() { PartName = "Car battery 65Ah Maintenance-Free", Quantity = 1, UnitPrice = 24500 }
                },
                Labour = new List<InvoiceLabourItem>
                {
                    new() { Description = "Battery installation and terminal cleanup", Hours = 0.5m, HourlyRate = 2000 }
                },
                Payments = new List<InvoicePaymentRecord>
                {
                    new()
                    {
                        Id = 1,
                        Amount = 15000,
                        PaymentMethod = "Card",
                        PaymentDate = today.AddDays(-3),
                        ReferenceNo = "POS-VISA-7841",
                        RecordedBy = "Nimali Perera",
                        Notes = "Visa Card payment at front counter"
                    }
                }
            },

            // Invoice 3: Unpaid (Brand new invoice)
            new()
            {
                Id = 3,
                InvoiceNo = "INV-2026-003",
                JobCardNo = "JC-001",
                IssueDate = today.AddDays(-1),
                DueDate = today.AddDays(6),
                CustomerName = "Kasun Silva",
                CustomerPhone = "+94 76 890 1234",
                CustomerEmail = "kasun.s@outlook.com",
                CustomerAddress = "25 Parliament Drive, Sri Jayawardenepura Kotte",
                VehicleNo = "CAB-1234",
                VehicleModel = "Mitsubishi Montero Sport 2018",
                Mileage = 118000,
                Notes = "Front disc brake inspection and ceramic pad replacement.",
                Parts = new List<InvoicePartItem>
                {
                    new() { PartName = "Front brake pads set (Ceramic)", Quantity = 1, UnitPrice = 8500 }
                },
                Labour = new List<InvoiceLabourItem>
                {
                    new() { Description = "Brake system overhaul and caliper servicing", Hours = 1.5m, HourlyRate = 2000 }
                },
                Payments = new List<InvoicePaymentRecord>()
            }
        };
    }
}
