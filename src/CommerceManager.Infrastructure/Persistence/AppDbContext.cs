using CommerceManager.Domain.Cash;
using CommerceManager.Domain.Catalog;
using CommerceManager.Domain.Integrations;
using CommerceManager.Domain.Inventory;
using CommerceManager.Domain.Ledger;
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

    public DbSet<CatalogProduct> CatalogProducts => Set<CatalogProduct>();

    public DbSet<CatalogBarcode> CatalogBarcodes => Set<CatalogBarcode>();

    public DbSet<CatalogStockMovement> CatalogStockMovements => Set<CatalogStockMovement>();

    public DbSet<CatalogChannelAllocation> CatalogChannelAllocations => Set<CatalogChannelAllocation>();

    public DbSet<CurrentAccount> CurrentAccounts => Set<CurrentAccount>();

    public DbSet<CurrentAccountEntry> CurrentAccountEntries => Set<CurrentAccountEntry>();

    public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<CashMovement> CashMovements => Set<CashMovement>();

    public DbSet<RetailSale> RetailSales => Set<RetailSale>();

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


        var catalogProduct = modelBuilder.Entity<CatalogProduct>();

        catalogProduct.HasKey(x => x.Id);

        catalogProduct.Property(x => x.Name)
            .HasMaxLength(300)
            .IsRequired();

        catalogProduct.Property(x => x.Description)
            .HasMaxLength(2000);

        catalogProduct.Property(x => x.SalePrice)
            .HasPrecision(18, 2);

        catalogProduct.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        catalogProduct.HasIndex(x => x.CreatedAt);


        var catalogBarcode = modelBuilder.Entity<CatalogBarcode>();

        catalogBarcode.HasKey(x => x.Id);

        catalogBarcode.Property(x => x.Channel)
            .HasConversion<int>();

        catalogBarcode.Property(x => x.Barcode)
            .HasMaxLength(150)
            .IsRequired();

        catalogBarcode.Property(x => x.Units)
            .HasDefaultValue(1);

        catalogBarcode.Property(x => x.ListPrice)
            .HasPrecision(18, 2);

        catalogBarcode.HasIndex(x => new
        {
            x.Channel,
            x.Barcode
        })
        .IsUnique();

        catalogBarcode.HasIndex(x => x.CatalogProductId);

        catalogBarcode.HasOne<CatalogProduct>()
            .WithMany()
            .HasForeignKey(x => x.CatalogProductId)
            .OnDelete(DeleteBehavior.Cascade);


        var catalogMovement = modelBuilder.Entity<CatalogStockMovement>();

        catalogMovement.HasKey(x => x.Id);

        catalogMovement.Property(x => x.Channel)
            .HasConversion<int>();

        catalogMovement.Property(x => x.Type)
            .HasConversion<int>();

        catalogMovement.Property(x => x.Barcode)
            .HasMaxLength(150)
            .IsRequired();

        catalogMovement.Property(x => x.ReferenceType)
            .HasMaxLength(50)
            .IsRequired();

        catalogMovement.Property(x => x.ReferenceId)
            .HasMaxLength(200)
            .IsRequired();

        catalogMovement.HasIndex(x => new
        {
            x.Channel,
            x.ReferenceType,
            x.ReferenceId,
            x.Barcode,
            x.Type
        })
        .IsUnique();

        catalogMovement.HasIndex(x => x.CatalogProductId);

        catalogMovement.HasOne<CatalogProduct>()
            .WithMany()
            .HasForeignKey(x => x.CatalogProductId)
            .OnDelete(DeleteBehavior.Cascade);


        var catalogAllocation = modelBuilder.Entity<CatalogChannelAllocation>();

        catalogAllocation.HasKey(x => x.Id);

        catalogAllocation.Property(x => x.Channel)
            .HasConversion<int>();

        catalogAllocation.HasIndex(x => new
        {
            x.CatalogProductId,
            x.Channel
        })
        .IsUnique();

        catalogAllocation.HasOne<CatalogProduct>()
            .WithMany()
            .HasForeignKey(x => x.CatalogProductId)
            .OnDelete(DeleteBehavior.Cascade);


        var currentAccount = modelBuilder.Entity<CurrentAccount>();

        currentAccount.HasKey(x => x.Id);

        currentAccount.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        currentAccount.Property(x => x.Kind)
            .HasConversion<int>();

        currentAccount.Property(x => x.Phone)
            .HasMaxLength(30);

        currentAccount.Property(x => x.TaxNumber)
            .HasMaxLength(20);

        currentAccount.HasIndex(x => x.Name);


        var currentAccountEntry = modelBuilder.Entity<CurrentAccountEntry>();

        currentAccountEntry.HasKey(x => x.Id);

        currentAccountEntry.Property(x => x.Direction)
            .HasConversion<int>();

        currentAccountEntry.Property(x => x.Amount)
            .HasPrecision(18, 2);

        currentAccountEntry.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        currentAccountEntry.HasIndex(x => x.CurrentAccountId);

        currentAccountEntry.HasOne<CurrentAccount>()
            .WithMany()
            .HasForeignKey(x => x.CurrentAccountId)
            .OnDelete(DeleteBehavior.Cascade);


        var expenseType = modelBuilder.Entity<ExpenseType>();

        expenseType.HasKey(x => x.Id);

        expenseType.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        expenseType.HasIndex(x => x.Name)
            .IsUnique();

        expenseType.HasData(
            ExpenseTypeSeed.BenzinMazot,
            ExpenseTypeSeed.AracHarcligi,
            ExpenseTypeSeed.Yemek,
            ExpenseTypeSeed.Kira,
            ExpenseTypeSeed.Elektrik,
            ExpenseTypeSeed.Su,
            ExpenseTypeSeed.Dogalgaz,
            ExpenseTypeSeed.Telefon,
            ExpenseTypeSeed.Kargo,
            ExpenseTypeSeed.Personel,
            ExpenseTypeSeed.Mutfak,
            ExpenseTypeSeed.Bakim,
            ExpenseTypeSeed.Vergi,
            ExpenseTypeSeed.Diger);


        var expense = modelBuilder.Entity<Expense>();

        expense.HasKey(x => x.Id);

        expense.Property(x => x.Amount)
            .HasPrecision(18, 2);

        expense.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        expense.HasIndex(x => x.ExpenseTypeId);

        expense.HasIndex(x => x.OccurredAt);

        expense.HasOne<ExpenseType>()
            .WithMany()
            .HasForeignKey(x => x.ExpenseTypeId)
            .OnDelete(DeleteBehavior.Restrict);


        var cashMovement = modelBuilder.Entity<CashMovement>();

        cashMovement.HasKey(x => x.Id);

        cashMovement.Property(x => x.Direction)
            .HasConversion<int>();

        cashMovement.Property(x => x.Amount)
            .HasPrecision(18, 2);

        cashMovement.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        cashMovement.HasIndex(x => x.ExpenseId)
            .IsUnique()
            .HasFilter("[ExpenseId] IS NOT NULL");

        cashMovement.HasIndex(x => x.OccurredAt);

        cashMovement.HasOne<Expense>()
            .WithMany()
            .HasForeignKey(x => x.ExpenseId)
            .OnDelete(DeleteBehavior.Restrict);


        var retailSale = modelBuilder.Entity<RetailSale>();

        retailSale.HasKey(x => x.Id);

        retailSale.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        retailSale.Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        retailSale.Property(x => x.CustomerName)
            .HasMaxLength(200);

        retailSale.Property(x => x.CustomerPhone)
            .HasMaxLength(30);

        retailSale.HasIndex(x => x.CatalogProductId);

        retailSale.HasIndex(x => x.OccurredAt);

        retailSale.HasOne<CatalogProduct>()
            .WithMany()
            .HasForeignKey(x => x.CatalogProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
