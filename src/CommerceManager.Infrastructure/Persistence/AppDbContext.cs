using CommerceManager.Domain.Integrations;
using CommerceManager.Domain.Inventory;
using CommerceManager.Domain.Orders;
using CommerceManager.Domain.Returns;
using Microsoft.EntityFrameworkCore;
using CommerceManager.Domain.Products;
using CommerceManager.Domain.Users;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderReturn> OrderReturns => Set<OrderReturn>();

    public DbSet<ChannelStock> ChannelStocks => Set<ChannelStock>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<MarketplaceIntegrationStatus> MarketplaceIntegrationStatuses
        => Set<MarketplaceIntegrationStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();

        order.HasKey(x => x.Id);

        order.Property(x => x.Channel)
            .HasConversion<int>();

        order.Property(x => x.ExternalOrderId)
            .HasMaxLength(150);

        order.Property(x => x.OrderNumber)
            .HasMaxLength(100);

        order.Property(x => x.Status)
            .HasMaxLength(60);

        order.Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        order.HasIndex(x => new
        {
            x.Channel,
            x.ExternalOrderId
        })
        .IsUnique()
        .HasFilter("[ExternalOrderId] IS NOT NULL");

        order.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);


        var item = modelBuilder.Entity<OrderItem>();

        item.HasKey(x => x.Id);

        item.Property(x => x.ExternalLineId)
            .HasMaxLength(150);

        item.Property(x => x.Barcode)
            .HasMaxLength(150);

        item.Property(x => x.ProductName)
            .HasMaxLength(500);

        item.Property(x => x.ImageUrl)
            .HasMaxLength(2000);

        item.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        item.HasIndex(x => new
        {
            x.OrderId,
            x.ExternalLineId
        })
        .IsUnique();


        var orderReturn = modelBuilder.Entity<OrderReturn>();

        orderReturn.HasKey(x => x.Id);

        orderReturn.Property(x => x.ExternalReturnId)
            .HasMaxLength(200);

        orderReturn.Property(x => x.Status)
            .HasConversion<int>();

        orderReturn.HasIndex(x => x.ExternalReturnId)
            .IsUnique();

        orderReturn.HasIndex(x => x.OrderId);

        orderReturn.HasIndex(x => x.OrderItemId);

        orderReturn.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        orderReturn.HasOne<OrderItem>()
            .WithMany()
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.NoAction);


        var product = modelBuilder.Entity<Product>();

        product.HasKey(x => x.Id);

        product.Property(x => x.Channel)
            .HasConversion<int>();

        product.Property(x => x.Barcode)
            .HasMaxLength(100)
            .IsRequired();

        product.Property(x => x.StockCode)
            .HasMaxLength(200);

        product.Property(x => x.ExternalProductId)
            .HasMaxLength(100);

        product.Property(x => x.ProductMainId)
            .HasMaxLength(200);

        product.Property(x => x.Name)
            .HasMaxLength(1000)
            .IsRequired();

        product.Property(x => x.ImageUrl)
            .HasMaxLength(2000);

        product.HasIndex(x => new
        {
            x.Channel,
            x.Barcode
        }).IsUnique();

        var channelStock = modelBuilder.Entity<ChannelStock>();

        channelStock.HasKey(x => x.Id);

        channelStock.Property(x => x.Channel)
            .HasConversion<int>();

        channelStock.Property(x => x.Barcode)
            .HasMaxLength(150);

        channelStock.HasIndex(x => new
        {
            x.Channel,
            x.Barcode
        })
        .IsUnique();


        var movement = modelBuilder.Entity<StockMovement>();

        movement.HasKey(x => x.Id);

        movement.Property(x => x.Channel)
            .HasConversion<int>();

        movement.Property(x => x.Type)
            .HasConversion<int>();

        movement.Property(x => x.Barcode)
            .HasMaxLength(150);

        movement.Property(x => x.ReferenceType)
            .HasMaxLength(50);

        movement.Property(x => x.ReferenceId)
            .HasMaxLength(200);

        movement.HasIndex(x => new
        {
            x.Channel,
            x.ReferenceType,
            x.ReferenceId,
            x.Barcode,
            x.Type
        })
        .IsUnique();


        var role = modelBuilder.Entity<Role>();

        role.HasKey(x => x.Id);

        role.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        role.Property(x => x.Description)
            .HasMaxLength(500);

        role.HasIndex(x => x.Name)
            .IsUnique();


        var user = modelBuilder.Entity<User>();

        user.HasKey(x => x.Id);

        user.Property(x => x.UserName)
            .HasMaxLength(100)
            .IsRequired();

        user.Property(x => x.Email)
            .HasMaxLength(250)
            .IsRequired();

        user.Property(x => x.PasswordHash)
            .HasMaxLength(1000)
            .IsRequired();

        user.Property(x => x.FullName)
            .HasMaxLength(200)
            .IsRequired();

        user.Property(x => x.IsActive)
            .IsRequired();

        user.Property(x => x.CreatedAt)
            .IsRequired();

        user.HasIndex(x => x.UserName)
            .IsUnique();

        user.HasIndex(x => x.Email)
            .IsUnique();

        user.HasIndex(x => x.RoleId);

        user.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);


        var integrationStatus =
            modelBuilder.Entity<MarketplaceIntegrationStatus>();

        integrationStatus.HasKey(x => x.Id);

        integrationStatus.Property(x => x.Channel)
            .HasConversion<int>();

        integrationStatus.Property(x => x.LastError)
            .HasMaxLength(2000);

        integrationStatus.HasIndex(x => x.Channel)
            .IsUnique();
    }
}
