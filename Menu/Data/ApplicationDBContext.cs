using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<RestaurantTable> RestaurantTables { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<SubCategory> SubCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<WaiterRequest> WaiterRequests { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Restaurant> Restaurants { get; set; }
    public DbSet<CustomizationOption> CustomizationOptions { get; set; }
    public DbSet<OrderItemCustomization> OrderItemCustomizations { get; set; }
    public DbSet<KitchenNotification> KitchenNotifications { get; set; }
    public DbSet<WaiterNotification> WaiterNotifications { get; set; }
    public DbSet<Offer> Offers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // USERS
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.UserID);
            entity.Property(e => e.UserID).HasColumnName("userid");
            entity.Property(e => e.UserRole).HasColumnName("userrole");
            entity.Property(e => e.UserName).HasColumnName("username");
            entity.Property(e => e.PhoneNumber).HasColumnName("phonenumber");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("passwordhash");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedby");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");
            entity.Property(e => e.IsAvailable).HasColumnName("isavailable");

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

        });

        // RESTAURANT TABLES
        modelBuilder.Entity<RestaurantTable>(entity =>
        {
            entity.ToTable("restauranttables");
            entity.HasKey(e => e.RestaurantTableID);
            entity.Property(e => e.RestaurantTableID).HasColumnName("restauranttableid");
            entity.Property(e => e.TableName).HasColumnName("tablename");
            entity.Property(e => e.Seats).HasColumnName("seats");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedby");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CATEGORIES
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.CategoryID);
            entity.Property(e => e.CategoryID).HasColumnName("categoryid");
            entity.Property(e => e.CategoryName).HasColumnName("categoryname");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

        });

        // SUBCATEGORIES
        modelBuilder.Entity<SubCategory>(entity =>
        {
            entity.ToTable("subcategories");
            entity.HasKey(e => e.SubCategoryID);
            entity.Property(e => e.SubCategoryID).HasColumnName("subcategoryid");
            entity.Property(e => e.SubCategoryName).HasColumnName("subcategoryname");
            entity.Property(e => e.CategoryID).HasColumnName("categoryid");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

        });

        // PRODUCTS
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.ProductID);
            entity.Property(e => e.ProductID).HasColumnName("productid");
            entity.Property(e => e.ProductName).HasColumnName("productname");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.ProductDescription).HasColumnName("productdescription");
            entity.Property(e => e.ImagePath).HasColumnName("imagepath");
            entity.Property(e => e.CategoryID).HasColumnName("categoryid");
            entity.Property(e => e.SubCategoryID).HasColumnName("subcategoryid");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedby");
            entity.Property(e => e.IsVeg).HasColumnName("isveg");
            entity.Property(e => e.IsAvailable).HasColumnName("isavailable");

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

        });

        // ORDERS
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.OrderID);
            entity.Property(e => e.OrderID).HasColumnName("orderid");
            entity.Property(e => e.RestaurantTableID).HasColumnName("restauranttableid");
            entity.Property(e => e.UserID).HasColumnName("userid");
            entity.Property(e => e.CGST).HasColumnName("cgst");
            entity.Property(e => e.SGST).HasColumnName("sgst");
            entity.Property(e => e.ServiceCharge).HasColumnName("servicecharge");
            entity.Property(e => e.Subtotal).HasColumnName("subtotal");
            entity.Property(e => e.TotalAmount).HasColumnName("totalamount");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");
            entity.Property(e => e.ClosedAt).HasColumnName("closedat").HasColumnType("timestamptz");
            entity.Property(e => e.LastKitchenReadyAt).HasColumnName("lastkitchenreadyat").HasColumnType("timestamptz");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedby");
            entity.Property(e => e.OrderStatus).HasColumnName("orderstatus");
            entity.Property(e => e.KitchenStatus).HasColumnName("kitchenstatus");
            entity.Property(e => e.WaiterUserID).HasColumnName("waiteruserid");
            entity.Property(e => e.IsAssigned).HasColumnName("isassigned");
            entity.Property(e => e.PlaySound).HasColumnName("playsound");
            entity.Property(e => e.AppliedOfferID).HasColumnName("appliedofferid");
            entity.Property(e => e.DiscountAmount).HasColumnName("discountamount");


            entity.HasOne(e => e.AppliedOffer)
      .WithMany() // Optional: Or .WithMany(o => o.Orders) if added in Offer.cs
      .HasForeignKey(e => e.AppliedOfferID)
      .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Source)
                  .HasColumnName("source")
                  .HasConversion<int>();


        });

        // ORDER ITEMS
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("orderitems");
            entity.HasKey(e => e.OrderItemID);
            entity.Property(e => e.OrderItemID).HasColumnName("orderitemid");
            entity.Property(e => e.OrderID).HasColumnName("orderid");
            entity.Property(e => e.ProductID).HasColumnName("productid");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice).HasColumnName("unitprice");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedby");
            entity.Property(e => e.IsPrepared).HasColumnName("isprepared").HasDefaultValue(false);
            entity.Property(e => e.AddedToKitchenAt).HasColumnName("addedtokitchenat").HasColumnType("timestamptz").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.BatchID).HasColumnName("batchid").HasDefaultValue(1);
          


            entity.Property(e => e.PreparedAt)
      .HasColumnName("preparedat")
      .HasColumnType("timestamptz");

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);


        });

        // ORDER ITEM CUSTOMIZATIONS
        modelBuilder.Entity<OrderItemCustomization>(entity =>
        {
            entity.ToTable("orderitemcustomizations");
            entity.HasKey(e => e.OrderItemCustomizationID);
            entity.Property(e => e.OrderItemCustomizationID).HasColumnName("orderitemcustomizationid");
            entity.Property(e => e.OrderItemID).HasColumnName("orderitemid");
            entity.Property(e => e.CustomizationOptionID).HasColumnName("customizationoptionid");

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

        });

        // CUSTOMIZATION OPTIONS
        modelBuilder.Entity<CustomizationOption>(entity =>
        {
            entity.ToTable("customizationoptions");
            entity.HasKey(e => e.CustomizationOptionID);
            entity.Property(e => e.CustomizationOptionID).HasColumnName("customizationoptionid");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.FixedPrice).HasColumnName("fixedprice");
            entity.Property(e => e.ProductID).HasColumnName("productid");

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

        });

        // PAYMENTS
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.PaymentID);
            entity.Property(e => e.PaymentID).HasColumnName("paymentid");
            entity.Property(e => e.OrderID).HasColumnName("orderid");
            entity.Property(e => e.TableNo).HasColumnName("tableno");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.PaymentMethod).HasColumnName("paymentmethod");
            entity.Property(e => e.PaymentStatus).HasColumnName("paymentstatus").HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.CompletedAt).HasColumnName("completedat").HasColumnType("timestamptz");
            entity.Property(e => e.IsNotified).HasColumnName("isnotified");

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

        });

        // REVIEWS
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(e => e.ReviewID);
            entity.Property(e => e.ReviewID).HasColumnName("reviewid");
            entity.Property(e => e.OrderID).HasColumnName("orderid");
            entity.Property(e => e.Stars).HasColumnName("stars").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);



        });

        // WAITER REQUESTS
        modelBuilder.Entity<WaiterRequest>(entity =>
        {
            entity.ToTable("waiterrequests");

            entity.HasKey(e => e.WaiterRequestID);
            entity.Property(e => e.WaiterRequestID).HasColumnName("waiterrequestid");

            entity.Property(e => e.Message)
                  .HasColumnName("message")
                  .IsRequired()
                  .HasMaxLength(250);

            entity.Property(e => e.TableNumber)
                  .HasColumnName("tablenumber");

            entity.Property(e => e.RequestTime)
                  .HasColumnName("requesttime")
                  .HasColumnType("timestamptz")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsNotified)
                  .HasColumnName("isnotified")
                  .HasDefaultValue(false);

            entity.Property(e => e.RestaurantID)
                  .HasColumnName("restaurantid");

            entity.Property(e => e.RestaurantTableID) // ✅ You missed this
                  .HasColumnName("restauranttableid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<RestaurantTable>() // ✅ Add FK for RestaurantTable
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantTableID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // WAITER NOTIFICATIONS
        modelBuilder.Entity<WaiterNotification>(entity =>
        {
            entity.ToTable("waiternotifications");

            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.NotificationId).HasColumnName("notificationid");
            entity.Property(e => e.OrderId).HasColumnName("orderid");
            entity.Property(e => e.TableNo).HasColumnName("tableno");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.IsAcknowledged).HasColumnName("isacknowledged");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            // Add these relationships
            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasKey(o => o.OfferID);

            entity.Property(o => o.Description).IsRequired();
            entity.Property(o => o.MinBillAmount).HasDefaultValue(0);
            entity.Property(o => o.IsActive).HasDefaultValue(true);
            entity.Property(o => o.AutoApply).HasDefaultValue(true);

            entity.HasOne(o => o.Restaurant)
                  .WithMany() // or .WithMany(r => r.Offers) if you add ICollection<Offer> in Restaurant
                  .HasForeignKey(o => o.RestaurantID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);


        });

        // RESTAURANT
        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.ToTable("restaurants");
            entity.HasKey(e => e.RestaurantID);
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.LogoPath).HasColumnName("logopath");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.UPI_ID).HasColumnName("upi_id").HasMaxLength(150);
            entity.Property(e => e.UPI_Name).HasColumnName("upi_name").HasMaxLength(150);
        });

        // KITCHEN NOTIFICATIONS
        modelBuilder.Entity<KitchenNotification>(entity =>
        {
            entity.ToTable("kitchennotifications");
            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.NotificationId).HasColumnName("notificationid");
            entity.Property(e => e.OrderId).HasColumnName("orderid");
            entity.Property(e => e.TableNo).HasColumnName("tableno");
            entity.Property(e => e.NotificationTime).HasColumnName("notificationtime").HasColumnType("timestamptz");
            entity.Property(e => e.IsAcknowledged).HasColumnName("isacknowledged");
            entity.Property(e => e.Message).HasColumnName("message");

            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);


        });
    }
}
