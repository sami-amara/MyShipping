using DataAccessLayer;
using DataAccessLayer.Models;
using DataAccessLayer.UserModels;
using Domains;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;

namespace DataAccessLayer.DbContext;

public partial class ShippingContext : IdentityDbContext<ApplicationUser>
{
    public ShippingContext()
    {
    }

    public ShippingContext(DbContextOptions<ShippingContext> options)
        : base(options)
    {
    }



    public virtual DbSet<CustomersWhoditnotcreateShippment> CustomersWhoditnotcreateShippments { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<TbCarrier> TbCarriers { get; set; }

    public virtual DbSet<TbCity> TbCities { get; set; }

    public virtual DbSet<TbCountry> TbCountries { get; set; }

    public virtual DbSet<TbPaymentMethod> TbPaymentMethods { get; set; }

    public virtual DbSet<TbPaymentTransaction> TbPaymentTransactions { get; set; }

    public virtual DbSet<TbPaymentWebhookEvent> TbPaymentWebhookEvents { get; set; }

    public virtual DbSet<TbRefreshToken> TbRefreshTokens { get; set; }


    public virtual DbSet<TbSetting> TbSettings { get; set; }

    public virtual DbSet<TbShipingPackging> TbShipingPackgings { get; set; }

    public virtual DbSet<TbShippingType> TbShippingTypes { get; set; }

    public virtual DbSet<TbShippment> TbShippments { get; set; }

    public virtual DbSet<TbShippmentStatus> TbShippmentStatuses { get; set; }

    public virtual DbSet<TbSubscriptionPackage> TbSubscriptionPackages { get; set; }

    public virtual DbSet<TbUserReceiver> TbUserReceivers { get; set; }

    public virtual DbSet<TbUserSender> TbUserSenders { get; set; }

    public virtual DbSet<TbUserSubscription> TbUserSubscriptions { get; set; }

    public virtual DbSet<Testing> Testings { get; set; }

    public virtual DbSet<VwCities> VwCities { get; set; }

    public virtual DbSet<VwDeleverdShippin> VwDeleverdShippins { get; set; }

    public virtual DbSet<Vwhighest10ShippingRate> Vwhighest10ShippingRates { get; set; }

    public virtual DbSet<Vwlowest10ShippingRatesView> Vwlowest10ShippingRatesViews { get; set; }


    public DbSet<AccountAuditLog> AccountAuditLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder
                .UseSqlServer("Server=localhost;Database=Shipping;Trusted_Connection=True;TrustServerCertificate=True;")
                .EnableSensitiveDataLogging()   // only in dev
                .EnableDetailedErrors()
                .LogTo(Console.WriteLine, new[] { Microsoft.EntityFrameworkCore.DbLoggerCategory.Database.Command.Name }, Microsoft.Extensions.Logging.LogLevel.Information);
        }
    }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);



        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");
        modelBuilder.Entity<CustomersWhoditnotcreateShippment>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("CustomersWhoditnotcreateShippments");

            entity.Property(e => e.CustomerEmail).HasMaxLength(256);
            entity.Property(e => e.CustomerId).HasMaxLength(450);
            entity.Property(e => e.CustomerName).HasMaxLength(256);
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.ToTable("Log");

            entity.Property(e => e.TimeStamp).HasColumnType("datetime");
        });

        modelBuilder.Entity<TbCarrier>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CarrierName).HasMaxLength(200);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TbCity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CityAname)
                .HasMaxLength(200)
                .HasColumnName("CityAName");
            entity.Property(e => e.CityEname)
                .HasMaxLength(200)
                .HasColumnName("CityEName");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Country).WithMany(p => p.TbCities)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TbCities_TbCountries");
        });

        modelBuilder.Entity<TbCountry>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CountryAname)
                .HasMaxLength(200)
                .HasColumnName("CountryAName");
            entity.Property(e => e.CountryEname)
                .HasMaxLength(200)
                .HasColumnName("CountryEName");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TbPaymentMethod>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.MethdAname)
                .HasMaxLength(200)
                .HasColumnName("MethdAName");
            entity.Property(e => e.MethodEname)
                .HasMaxLength(200)
                .HasDefaultValue("")
                .HasColumnName("MethodEName");
            entity.Property(e => e.PaymentMethodToken)
                .HasMaxLength(200)
                .HasDefaultValue(null);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

        });

        // Payment Transaction Configuration - Educational Example
        modelBuilder.Entity<TbPaymentTransaction>(entity =>
        {
            entity.ToTable("TbPaymentTransaction");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.Property(e => e.ProcessedDate).HasColumnType("datetime2");

            // Decimal precision for money values
            entity.Property(e => e.ShippingRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CommissionAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.Property(e => e.IdempotencyKey).HasMaxLength(150);
            entity.Property(e => e.ProviderName).HasMaxLength(50);
            entity.Property(e => e.ProviderEventId).HasMaxLength(150);
            entity.Property(e => e.TransactionReference).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            // Foreign key relationships
            entity.HasOne(d => d.Shipment)
                .WithMany()
                .HasForeignKey(d => d.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TbPaymentTransaction_TbShippments");

            entity.HasOne(d => d.PaymentMethod)
                .WithMany()
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TbPaymentTransaction_TbPaymentMethods");

            // Index for fast lookup by shipment
            entity.HasIndex(e => e.ShipmentId).HasDatabaseName("IX_TbPaymentTransaction_ShipmentId");
            entity.HasIndex(e => e.TransactionReference).HasDatabaseName("IX_TbPaymentTransaction_TransactionReference");
            entity.HasIndex(e => e.IdempotencyKey).HasDatabaseName("IX_TbPaymentTransaction_IdempotencyKey");
            entity.HasIndex(e => e.ProviderEventId).HasDatabaseName("IX_TbPaymentTransaction_ProviderEventId");
        });

        modelBuilder.Entity<TbPaymentWebhookEvent>(entity =>
        {
            entity.ToTable("TbPaymentWebhookEvents");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.ReceivedAt).HasColumnType("datetime");
            entity.Property(e => e.ProviderName).HasMaxLength(50);
            entity.Property(e => e.ProviderEventId).HasMaxLength(150);
            entity.Property(e => e.EventType).HasMaxLength(200);
            entity.Property(e => e.TransactionReference).HasMaxLength(150);
            entity.Property(e => e.Payload).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProcessingNotes).HasMaxLength(1000);

            entity.HasIndex(e => new { e.ProviderName, e.ProviderEventId })
                .IsUnique()
                .HasDatabaseName("IX_TbPaymentWebhookEvents_Provider_EventId");
        });


        modelBuilder.Entity<TbSetting>(entity =>
        {
            entity.ToTable("TbSetting");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
        });

        modelBuilder.Entity<TbShipingPackging>(entity =>
        {
            entity.ToTable("TbShipingPackging");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.TbShipingPackginAname).HasMaxLength(200);
            entity.Property(e => e.TbShipingPackginEname).HasMaxLength(200);
        });

        modelBuilder.Entity<TbShippingType>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ShippingTypeAname)
                .HasMaxLength(200)
                .HasColumnName("ShippingTypeAName");
            entity.Property(e => e.ShippingTypeEname)
                .HasMaxLength(200)
                .HasDefaultValue("")
                .HasColumnName("ShippingTypeEName");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TbShippment>(entity =>
        {
            entity.HasIndex(e => e.CarrierId, "IX_TbShippments_CarrierId");

            entity.HasIndex(e => e.ShipingPackgingId, "IX_TbShippments_ShipingPackgingId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.PackageValue).HasColumnType("decimal(8, 4)");
            entity.Property(e => e.ShippingDate).HasColumnType("datetime");
            entity.Property(e => e.ShippingRate).HasColumnType("decimal(8, 4)");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.IsPaid).HasDefaultValue(false).IsRequired();

            entity.HasOne(d => d.Carrier).WithMany(p => p.TbShippments)
                .HasForeignKey(d => d.CarrierId)
                .HasConstraintName("FK_TbShippmentStatus_TbCarriers");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.TbShippments)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("FK_TbShippments_TbPaymentMethods");

            entity.HasOne(d => d.Receiver).WithMany(p => p.TbShippments)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TbShippments_TbUserReceivers");

            entity.HasOne(d => d.Sender).WithMany(p => p.TbShippments)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TbShippments_TbUserSenders");

            entity.HasOne(d => d.ShipingPackging).WithMany(p => p.TbShippments).HasForeignKey(d => d.ShipingPackgingId);

            entity.HasOne(d => d.ShippingType).WithMany(p => p.TbShippments)
                .HasForeignKey(d => d.ShippingTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TbShippments_TbShippingTypes");
        });


       

            modelBuilder.Entity<TbShippmentStatus>(entity =>
            {
                entity.ToTable("TbShippmentStatus");

                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");
                entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

                // Carrier navigation removed from TbShippmentStatus (CarrierId/Carrier properties removed from domain model)
                // If you later re-add the Carrier relation, restore the line below and the domain properties.
                // entity.HasOne(d => d.Carrier).WithMany(p => p.TbShippmentStatuses).HasForeignKey(d => d.CarrierId);

                entity.HasOne(d => d.Shippment).WithMany(p => p.TbShippmentStatuses)
                    .HasForeignKey(d => d.ShippmentId)
                    .HasConstraintName("FK_TbShippmentStatus_TbShippments");
            });

        modelBuilder.Entity<TbSubscriptionPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_TbSubscriptionPackages");

            entity.ToTable("TbSubscriptionPackage");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.PackageName).HasMaxLength(200);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TbUserReceiver>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Contact).HasDefaultValue("");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.OtherAddress).HasDefaultValue("");
            entity.Property(e => e.Phone).HasMaxLength(200);
            entity.Property(e => e.PostalCode).HasDefaultValue("");
            entity.Property(e => e.ReceiverName).HasMaxLength(200);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.City).WithMany(p => p.TbUserReceivers)
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TbUserReceivers_TbCities");
        });

        modelBuilder.Entity<TbUserSender>(entity =>
        {
            // explicit table mapping to avoid convention mismatch
            entity.ToTable("TbUserSenders");

            entity.HasKey(e => e.Id).HasName("PK_TbUserSenders");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Contact).HasDefaultValue("");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.OtherAddress).HasDefaultValue("");
            entity.Property(e => e.Phone).HasMaxLength(200);
            entity.Property(e => e.PostalCode).HasDefaultValue("");
            entity.Property(e => e.SenderName).HasMaxLength(200);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.City).WithMany(p => p.TbUserSenders)
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TbUserSenders_TbCities");
        });

        modelBuilder.Entity<TbUserSubscription>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.SubscriptionDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Testing>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("testing");

            entity.Property(e => e.CarrierName).HasMaxLength(200);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ShippingDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<VwCities>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwCities");

            entity.Property(e => e.CityAname)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("CityAName");
            entity.Property(e => e.CityEname)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("CityEName");
            entity.Property(e => e.CountryAname)
                .HasMaxLength(200)
                .HasColumnName("CountryAName");
            entity.Property(e => e.CountryEname)
                .HasMaxLength(200)
                .HasColumnName("CountryEName");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<VwDeleverdShippin>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwDeleverdShippins");

            entity.Property(e => e.CarrierName).HasMaxLength(200);
            entity.Property(e => e.ShippingDate).HasColumnType("datetime");
            entity.Property(e => e.ShippingTypeEname)
                .HasMaxLength(200)
                .HasColumnName("ShippingTypeEName");
        });

        modelBuilder.Entity<Vwhighest10ShippingRate>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Vwhighest10ShippingRates");

            entity.Property(e => e.HighestShippingRate).HasColumnType("decimal(8, 4)");
        });

        modelBuilder.Entity<Vwlowest10ShippingRatesView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Vwlowest10ShippingRatesView");

            entity.Property(e => e.LowestShippingRate).HasColumnType("decimal(8, 4)");
        });

        modelBuilder.Entity<TbRefreshToken>(entity =>
        {
            // Set Id as Guid and configure it as the primary key
            entity.HasKey(e => e.Id);

            // Set default value for Id as Guid
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");

            // Configure CurrentState as an integer (e.g., 0 = Active, 1 = Revoked)
            entity.Property(e => e.CurrentState).HasDefaultValue(1) // Set default value to 0 (active)
                .IsRequired();

            // Configure CreatedBy, CreatedDate, UpdatedBy, and UpdatedDate
            entity.Property(e => e.CreatedBy).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedDate).HasDefaultValueSql("GETDATE()");



            entity.Property(e => e.Token)
              .IsRequired()
              .HasMaxLength(256);
            // ✅ Define foreign key constraint without navigation property
            entity.HasOne<ApplicationUser>()  // No navigation, just constraint
                .WithMany()  // User can have many tokens
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);  // Delete tokens when user is deleted

            // Index for faster lookups
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);

        });

        // ✅ NEW: Configure AccountAuditLog entity
        modelBuilder.Entity<AccountAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Reason)
                .HasMaxLength(500);

            entity.Property(e => e.InitiatedBy)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45); // IPv6 max length

            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("GETUTCDATE()");

            // Foreign key relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create index for efficient querying
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);
        });


        modelBuilder.Entity<VwCities>().ToView("VwCities");


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}









