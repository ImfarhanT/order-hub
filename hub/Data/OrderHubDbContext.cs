using Microsoft.EntityFrameworkCore;
using HubApi.Models;

namespace HubApi.Data;

public class OrderHubDbContext : DbContext
{
    public OrderHubDbContext(DbContextOptions<OrderHubDbContext> options) : base(options)
    {
    }

    public DbSet<Site> Sites { get; set; }
    public DbSet<Partner> Partners { get; set; }
    public DbSet<PaymentGateway> PaymentGateways { get; set; }
    public DbSet<SitePartner> SitePartners { get; set; }
    public DbSet<SiteGateway> SiteGateways { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<ShippingUpdate> ShippingUpdates { get; set; }
    public DbSet<RevenueShare> RevenueShares { get; set; }
    public DbSet<RequestNonce> RequestNonces { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure unique constraints
        modelBuilder.Entity<Site>()
            .HasIndex(s => s.BaseUrl)
            .IsUnique();

        modelBuilder.Entity<Site>()
            .HasIndex(s => s.ApiKey)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(o => new { o.SiteId, o.WcOrderId })
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.PlacedAt);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.PaymentGatewayCode);

        modelBuilder.Entity<RequestNonce>()
            .HasIndex(rn => new { rn.SiteId, rn.Nonce })
            .IsUnique();

        // Configure relationships
        modelBuilder.Entity<SitePartner>()
            .HasOne(sp => sp.Site)
            .WithMany(s => s.SitePartners)
            .HasForeignKey(sp => sp.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SitePartner>()
            .HasOne(sp => sp.Partner)
            .WithMany(p => p.SitePartners)
            .HasForeignKey(sp => sp.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SiteGateway>()
            .HasOne(sg => sg.Site)
            .WithMany(s => s.SiteGateways)
            .HasForeignKey(sg => sg.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SiteGateway>()
            .HasOne(sg => sg.Gateway)
            .WithMany(g => g.SiteGateways)
            .HasForeignKey(sg => sg.GatewayId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Site)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShippingUpdate>()
            .HasOne(su => su.Order)
            .WithMany(o => o.ShippingUpdates)
            .HasForeignKey(su => su.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RevenueShare>()
            .HasOne(rs => rs.Order)
            .WithMany(o => o.RevenueShares)
            .HasForeignKey(rs => rs.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RevenueShare>()
            .HasOne(rs => rs.Partner)
            .WithMany(p => p.RevenueShares)
            .HasForeignKey(rs => rs.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RequestNonce>()
            .HasOne(rn => rn.Site)
            .WithMany()
            .HasForeignKey(rn => rn.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
