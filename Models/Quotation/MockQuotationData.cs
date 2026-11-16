namespace AutoTrack.Models;

public static class MockQuotationData
{
    private static readonly object Sync = new();
    private static readonly List<QuotationViewModel> Quotations = Seed();

    public static List<QuotationViewModel> All()
    {
        lock (Sync)
        {
            return Quotations.OrderByDescending(q => q.DateCreated).ThenByDescending(q => q.Id).ToList();
        }
    }

    public static QuotationViewModel? Get(int id)
    {
        lock (Sync) { return Quotations.FirstOrDefault(q => q.Id == id); }
    }

    public static void Save(QuotationViewModel quote, string username)
    {
        lock (Sync)
        {
            if (quote.Id == 0)
            {
                quote.Id = Quotations.Count == 0 ? 1 : Quotations.Max(q => q.Id) + 1;
                quote.QuoteNo = $"QT-{quote.Id:000}";
                quote.AuditLogs.Add(new QuotationAuditItem
                {
                    Action = "Created Draft",
                    ChangedBy = username,
                    Details = $"Quotation created with total LKR {quote.GrandTotal:N2}"
                });
                Quotations.Add(quote);
            }
            else
            {
                var existing = Quotations.FirstOrDefault(q => q.Id == quote.Id);
                if (existing != null)
                {
                    quote.QuoteNo = existing.QuoteNo;
                    quote.AuditLogs = existing.AuditLogs;
                    quote.AuditLogs.Add(new QuotationAuditItem
                    {
                        Action = "Updated",
                        ChangedBy = username,
                        Details = $"Quotation modified. New total: LKR {quote.GrandTotal:N2}"
                    });
                    var idx = Quotations.IndexOf(existing);
                    Quotations[idx] = quote;
                }
            }
        }
    }

    public static bool SubmitForApproval(int id, string username)
    {
        lock (Sync)
        {
            var quote = Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null) return false;

            quote.Status = "Pending Owner Approval";
            quote.AuditLogs.Add(new QuotationAuditItem
            {
                Action = "Submitted to Owner",
                ChangedBy = username,
                Details = "Submitted for Owner review and authorization"
            });
            return true;
        }
    }

    public static bool OwnerApprove(int id, string remarks, string username)
    {
        lock (Sync)
        {
            var quote = Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null) return false;

            quote.Status = "Approved by Owner";
            quote.OwnerApprovedDate = DateTime.Now;
            quote.ApprovedBy = username;
            quote.OwnerRemarks = remarks;
            quote.AuditLogs.Add(new QuotationAuditItem
            {
                Action = "Owner Approved",
                ChangedBy = username,
                Details = string.IsNullOrWhiteSpace(remarks) ? "Approved without notes" : $"Remarks: {remarks}"
            });
            return true;
        }
    }

    public static bool OwnerReject(int id, string remarks, string username)
    {
        lock (Sync)
        {
            var quote = Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null) return false;

            quote.Status = "Revision Requested";
            quote.OwnerRemarks = remarks;
            quote.AuditLogs.Add(new QuotationAuditItem
            {
                Action = "Revision Requested",
                ChangedBy = username,
                Details = $"Reason: {remarks}"
            });
            return true;
        }
    }

    public static bool SendToCustomer(int id, string username)
    {
        lock (Sync)
        {
            var quote = Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null) return false;

            quote.Status = "Sent to Customer";
            quote.AuditLogs.Add(new QuotationAuditItem
            {
                Action = "Sent to Customer",
                ChangedBy = username,
                Details = "Quotation issued to customer for final approval"
            });
            return true;
        }
    }

    public static string? ConvertToJobCard(int id, string mechanic, string username)
    {
        lock (Sync)
        {
            var quote = Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null) return null;

            // 1. Create Job Card in MockJobData
            var newJob = new JobCardViewModel
            {
                VehicleNo = quote.VehicleNo,
                CustomerName = quote.CustomerName,
                Complaint = quote.CustomerRequest,
                Mechanic = mechanic,
                Status = "Pending",
                DateOpened = DateTime.Today,
                Notes = $"Converted from Quotation {quote.QuoteNo}. " + (quote.Notes ?? ""),
                Tasks = { new JobTaskItem { Description = "Initial inspection per quote " + quote.QuoteNo } },
                Parts = quote.Parts.Select(p => new JobPartItem
                {
                    PartName = p.PartName,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice
                }).ToList(),
                Labour = quote.Labour.Select(l => new JobLabourItem
                {
                    Description = l.Description,
                    Hours = l.Hours,
                    Rate = l.Rate
                }).ToList()
            };

            MockJobData.Save(newJob);

            // 2. Update Quotation Status
            quote.Status = "Converted";
            quote.ConvertedJobCardNo = newJob.JobNo;
            quote.AuditLogs.Add(new QuotationAuditItem
            {
                Action = "Converted to Job Card",
                ChangedBy = username,
                Details = $"Successfully converted to Job Card {newJob.JobNo} assigned to {mechanic}"
            });

            return newJob.JobNo;
        }
    }

    private static List<QuotationViewModel> Seed()
    {
        var today = DateTime.Today;
        return new List<QuotationViewModel>
        {
            new()
            {
                Id = 1, QuoteNo = "QT-001", VehicleNo = "CAB-1234", VehicleModel = "Toyota Prius 2018", Mileage = 68000,
                CustomerName = "Kasun Silva", CustomerPhone = "0771234567", CustomerEmail = "kasun@gmail.com",
                CustomerRequest = "Full brake overhaul and suspension noise inspection",
                Status = "Pending Owner Approval", DateCreated = today.AddDays(-1), ExpiryDate = today.AddDays(13),
                DiscountPercent = 5, IncludeVat = true,
                Parts = {
                    new() { PartName = "Front brake pads (OEM)", Quantity = 1, UnitPrice = 9500 },
                    new() { PartName = "Brake fluid DOT4 (1L)", Quantity = 1, UnitPrice = 2800 }
                },
                Labour = {
                    new() { Description = "Brake system service & bleeding", Hours = 2.5m, Rate = 2500 }
                },
                AuditLogs = {
                    new() { Timestamp = today.AddDays(-1), Action = "Created Draft", ChangedBy = "desk", Details = "Created by Front Desk" },
                    new() { Timestamp = today.AddDays(-1), Action = "Submitted to Owner", ChangedBy = "desk", Details = "Submitted for discount approval" }
                }
            },
            new()
            {
                Id = 2, QuoteNo = "QT-002", VehicleNo = "WP CAJ-5566", VehicleModel = "Honda Vezel 2016", Mileage = 82000,
                CustomerName = "Dilshan Perera", CustomerPhone = "0719876543", CustomerEmail = "dilshan@yahoo.com",
                CustomerRequest = "Dual clutch calibration and gearbox fluid change",
                Status = "Approved by Owner", DateCreated = today.AddDays(-3), ExpiryDate = today.AddDays(11),
                OwnerRemarks = "Approved with standard labour pricing.", ApprovedBy = "owner", OwnerApprovedDate = today.AddDays(-2),
                Parts = {
                    new() { PartName = "Honda DW-1 ATF (4L)", Quantity = 1, UnitPrice = 18500 }
                },
                Labour = {
                    new() { Description = "Transmission scan and fluid replacement", Hours = 2m, Rate = 3000 }
                },
                AuditLogs = {
                    new() { Timestamp = today.AddDays(-3), Action = "Created Draft", ChangedBy = "desk" },
                    new() { Timestamp = today.AddDays(-2), Action = "Owner Approved", ChangedBy = "owner", Details = "Approved by Owner Dilshan" }
                }
            },
            new()
            {
                Id = 3, QuoteNo = "QT-003", VehicleNo = "BEH-3302", VehicleModel = "Suzuki Wagon R 2017", Mileage = 95000,
                CustomerName = "Pradeep Gunawardena", CustomerPhone = "0754433221",
                CustomerRequest = "AC compressor replacement and cooling gas recharge",
                Status = "Converted", DateCreated = today.AddDays(-7), ExpiryDate = today.AddDays(7),
                ConvertedJobCardNo = "JC-003",
                Parts = {
                    new() { PartName = "AC compressor (reconditioned)", Quantity = 1, UnitPrice = 58000 },
                    new() { PartName = "R134a AC Gas recharge", Quantity = 1, UnitPrice = 6500 }
                },
                Labour = {
                    new() { Description = "AC dismantle and refit", Hours = 3.5m, Rate = 2500 }
                },
                AuditLogs = {
                    new() { Timestamp = today.AddDays(-7), Action = "Created Draft", ChangedBy = "desk" },
                    new() { Timestamp = today.AddDays(-6), Action = "Owner Approved", ChangedBy = "owner" },
                    new() { Timestamp = today.AddDays(-5), Action = "Converted to Job Card", ChangedBy = "desk", Details = "Converted to JC-003" }
                }
            }
        };
    }
}