using AutoTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Data;

public class AutoTrackDbContext(DbContextOptions<AutoTrackDbContext> options)
    : IdentityDbContext<AutoTrackUser>(options)
{
    public DbSet<CustomerVehicle> CustomerVehicles => Set<CustomerVehicle>();
    public DbSet<CustomerAppointment> CustomerAppointments => Set<CustomerAppointment>();
    public DbSet<ServiceJobRecord> ServiceJobs => Set<ServiceJobRecord>();
    public DbSet<InvoiceRecord> Invoices => Set<InvoiceRecord>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<StockRecord> StockItems => Set<StockRecord>();
    public DbSet<AuditLogRecord> AuditLogs => Set<AuditLogRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AutoTrackUser>().ToTable("USER_ACCOUNT");
        builder.Entity<CustomerVehicle>(entity =>
        {
            entity.ToTable("CUSTOMER_VEHICLE");
            entity.HasKey(vehicle => vehicle.Id);
            entity.HasIndex(vehicle => vehicle.VehicleNo).IsUnique();
            entity.Property(vehicle => vehicle.VehicleNo).HasMaxLength(15).IsRequired();
            entity.Property(vehicle => vehicle.Model).HasMaxLength(120);
            entity.HasOne(vehicle => vehicle.Customer)
                .WithMany()
                .HasForeignKey(vehicle => vehicle.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<CustomerAppointment>(entity =>
        {
            entity.ToTable("CUSTOMER_APPOINTMENT");
            entity.HasKey(appointment => appointment.Id);
            entity.HasIndex(appointment => appointment.AppointmentNo).IsUnique();
            entity.HasIndex(appointment => new { appointment.AppointmentDate, appointment.TimeSlot });
            entity.Property(appointment => appointment.AppointmentNo).HasMaxLength(40).IsRequired();
            entity.Property(appointment => appointment.TimeSlot).HasMaxLength(40).IsRequired();
            entity.Property(appointment => appointment.ServiceType).HasMaxLength(120).IsRequired();
            entity.Property(appointment => appointment.Status).HasMaxLength(30).IsRequired();
            entity.HasOne(appointment => appointment.Customer)
                .WithMany()
                .HasForeignKey(appointment => appointment.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(appointment => appointment.Vehicle)
                .WithMany()
                .HasForeignKey(appointment => appointment.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ServiceJobRecord>(entity =>
        {
            entity.ToTable("JOB_CARD");
            entity.HasKey(job => job.Id);
            entity.HasIndex(job => job.JobNo).IsUnique();
            entity.HasIndex(job => new { job.CustomerId, job.VehicleNo, job.DateOpened });
            entity.Property(job => job.JobNo).HasMaxLength(30).IsRequired();
            entity.Property(job => job.VehicleNo).HasMaxLength(15).IsRequired();
            entity.Property(job => job.Complaint).HasMaxLength(500).IsRequired();
            entity.Property(job => job.WorkDone).HasMaxLength(2000).IsRequired();
            entity.Property(job => job.PartsJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(job => job.LabourJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(job => job.TotalCost).HasPrecision(18, 2);
            entity.HasOne(job => job.Customer)
                .WithMany()
                .HasForeignKey(job => job.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<InvoiceRecord>(entity =>
        {
            entity.ToTable("INVOICE");
            entity.HasKey(invoice => invoice.Id);
            entity.HasIndex(invoice => invoice.InvoiceNo).IsUnique();
            entity.Property(invoice => invoice.InvoiceNo).HasMaxLength(30).IsRequired();
            entity.Property(invoice => invoice.TotalAmount).HasPrecision(18, 2);
            entity.Property(invoice => invoice.CustomerId).HasMaxLength(450);
        });
        builder.Entity<PaymentRecord>(entity =>
        {
            entity.ToTable("PAYMENT");
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Method).HasMaxLength(40).IsRequired();
            entity.HasOne(payment => payment.Invoice)
                .WithMany()
                .HasForeignKey(payment => payment.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockRecord>(entity =>
        {
            entity.ToTable("STOCK");
            entity.HasKey(stock => stock.Id);
            entity.Property(stock => stock.PartName).HasMaxLength(120).IsRequired();
            entity.Property(stock => stock.Quantity).HasPrecision(18, 2);
            entity.Property(stock => stock.UnitPrice).HasPrecision(18, 2);
        });
        builder.Entity<AuditLogRecord>(entity =>
        {
            entity.ToTable("AUDIT_LOG");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.ActorUsername).HasMaxLength(256).IsRequired();
            entity.Property(log => log.Action).HasMaxLength(10).IsRequired();
            entity.Property(log => log.TableName).HasMaxLength(128).IsRequired();
            entity.Property(log => log.OldValues).HasColumnType("nvarchar(max)");
            entity.Property(log => log.NewValues).HasColumnType("nvarchar(max)");
            entity.HasIndex(log => log.OccurredAt);
            entity.HasIndex(log => log.ActorUsername);
            entity.HasIndex(log => log.Action);
        });
    }
}
