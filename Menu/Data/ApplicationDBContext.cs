using Microsoft.EntityFrameworkCore;
using Restaurant_Menu.Models;
using Restaurant_System.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<RestaurantTable> RestaurantTables { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<SubCategory> SubCategories { get; set; }

    public DbSet<RestaurantPrinter> RestaurantPrinters { get; set; }

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

   
 

    // NEW: Expense Tracking DbSets
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Budget> Budgets { get; set; }

 
    public DbSet<OrderChangeHistory> OrderChangeHistory { get; set; }

    // NEW: Inventory DbSets
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }
    public DbSet<ProductRecipe> ProductRecipes { get; set; }

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
            entity.Property(e => e.TableNo).HasColumnName("tableno"); // ✅ NEW COLUMN

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

    
        modelBuilder.Entity<OrderChangeHistory>(entity =>
        {
            // ✅ FIX 1: Use the exact lowercase table name
            entity.ToTable("orderchangehistory");

            entity.HasKey(e => e.OrderChangeHistoryID);

            // ✅ FIX 2: Map every property to its lowercase column name
            entity.Property(e => e.OrderChangeHistoryID)
                .HasColumnName("orderchangehistoryid")
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            entity.Property(e => e.OrderID).HasColumnName("orderid");
            entity.Property(e => e.ChangeType).HasColumnName("changetype").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.ChangedByUserID).HasColumnName("changedbyuserid");
            entity.Property(e => e.ChangedAt).HasColumnName("changedat").IsRequired();
            entity.Property(e => e.OldValues).HasColumnName("oldvalues").HasColumnType("text");
            entity.Property(e => e.NewValues).HasColumnName("newvalues").HasColumnType("text");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            // Foreign key relationships (these are likely correct already)
            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserID)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Restaurant)
                .WithMany()
                .HasForeignKey(e => e.RestaurantID)
                .OnDelete(DeleteBehavior.Cascade);
        });

     

        // NEW: EXPENSES
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("expenses");
            entity.HasKey(e => e.ExpenseID);
            entity.Property(e => e.ExpenseID).HasColumnName("expenseid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.Category).HasColumnName("category").HasConversion<string>();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(10,2)");
            entity.Property(e => e.ExpenseDate).HasColumnName("expensedate").HasColumnType("date");
            entity.Property(e => e.PaymentMethod).HasColumnName("paymentmethod").HasConversion<string>();
            entity.Property(e => e.Vendor).HasColumnName("vendor").HasMaxLength(50);
            entity.Property(e => e.ReceiptNumber).HasColumnName("receiptnumber").HasMaxLength(100);
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.IsRecurring).HasColumnName("isrecurring").HasDefaultValue(false);
            entity.Property(e => e.RecurringFrequency).HasColumnName("recurringfrequency");
            entity.Property(e => e.ApprovedBy).HasColumnName("approvedby");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // NEW: BUDGETS
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("budgets");
            entity.HasKey(e => e.BudgetID);
            entity.Property(e => e.BudgetID).HasColumnName("budgetid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.Category).HasColumnName("category").HasConversion<string>();
            entity.Property(e => e.MonthlyBudget).HasColumnName("monthlybudget").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.ActualSpent).HasColumnName("actualspent").HasColumnType("decimal(10,2)");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

       

        // NEW: ANALYTICS SNAPSHOTS
        modelBuilder.Entity<AnalyticsSnapshot>(entity =>
        {
            entity.ToTable("analyticssnapshots");
            entity.HasKey(e => e.SnapshotID);
            entity.Property(e => e.SnapshotID).HasColumnName("snapshotid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.SnapshotDate).HasColumnName("snapshotdate").HasColumnType("date");
            entity.Property(e => e.DailyRevenue).HasColumnName("dailyrevenue").HasColumnType("decimal(10,2)");
            entity.Property(e => e.DailyOrders).HasColumnName("dailyorders");
            entity.Property(e => e.AverageOrderValue).HasColumnName("averageordervalue").HasColumnType("decimal(10,2)");
            entity.Property(e => e.CancelledOrders).HasColumnName("cancelledorders");
            entity.Property(e => e.NewCustomers).HasColumnName("newcustomers");
            entity.Property(e => e.ReturningCustomers).HasColumnName("returningcustomers");
            entity.Property(e => e.CustomerSatisfactionScore).HasColumnName("customersatisfactionscore").HasColumnType("decimal(5,2)");
            entity.Property(e => e.LaborCostPercentage).HasColumnName("laborcostpercentage").HasColumnType("decimal(5,2)");
            entity.Property(e => e.FoodCostPercentage).HasColumnName("foodcostpercentage").HasColumnType("decimal(5,2)");
            entity.Property(e => e.TableTurnoverRate).HasColumnName("tableturnoverrate").HasColumnType("decimal(5,2)");
            entity.Property(e => e.LowStockItems).HasColumnName("lowstockitems");
            entity.Property(e => e.InventoryValue).HasColumnName("inventoryvalue").HasColumnType("decimal(10,2)");
            entity.Property(e => e.WeatherCondition).HasColumnName("weathercondition");
            entity.Property(e => e.Temperature).HasColumnName("temperature").HasColumnType("decimal(5,2)");
            entity.Property(e => e.WeatherImpactScore).HasColumnName("weatherimpactscore").HasColumnType("decimal(5,2)");

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

            entity.Property(e => e.CustomerID)
          .HasColumnName("customerid")
          .IsRequired(false);  

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

            entity.Property(e => e.OrderNumber).HasColumnName("ordernumber");

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

            // ✅ ADD THIS MAPPING
            entity.Property(e => e.PaymentChannel)
                  .HasColumnName("paymentchannel") // Use snake_case for consistency
                  .HasConversion<int>();          // Store the enum as an integer

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
            entity.ToTable("Offers"); // Explicitly set the table name
            entity.HasKey(o => o.OfferID);

            // ✅ CRITICAL: Explicitly map to the correct uppercase column
            entity.Property(o => o.RestaurantID)
                .HasColumnName("RestaurantID") // Map to "RestaurantID" not "restaurantid"
                .IsRequired();

            // ✅ Configure the relationship
            entity.HasOne(o => o.Restaurant)
                  .WithMany()
                  .HasForeignKey(o => o.RestaurantID)
                  .OnDelete(DeleteBehavior.Cascade);

            // Map other properties explicitly
            entity.Property(o => o.Code).HasColumnName("Code");
            entity.Property(o => o.Description).HasColumnName("Description").IsRequired();
            entity.Property(o => o.DiscountAmount).HasColumnName("DiscountAmount");
            entity.Property(o => o.DiscountPercent).HasColumnName("DiscountPercent");
            entity.Property(o => o.MinBillAmount).HasColumnName("MinBillAmount").HasDefaultValue(0);
            entity.Property(o => o.ValidFrom).HasColumnName("ValidFrom");
            entity.Property(o => o.ValidTo).HasColumnName("ValidTo");
            entity.Property(o => o.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
            entity.Property(o => o.AutoApply).HasColumnName("AutoApply").HasDefaultValue(true);
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
            entity.Property(e => e.KotPrinterName).HasColumnName("kotprintername").HasMaxLength(200);
            entity.Property(e => e.Address) // ✅ ADDED
      .HasColumnName("address")
      .HasMaxLength(500); // Adjust length as needed
            entity.Property(e => e.BillPrinterName).HasColumnName("billprintername").HasMaxLength(200);
            entity.Property(e => e.LocalPrintServiceUrl).HasColumnName("localprintserviceurl").HasMaxLength(200);

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

        // INVENTORY ITEMS
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("inventoryitems");
            entity.HasKey(e => e.InventoryItemID);

            entity.Property(e => e.InventoryItemID).HasColumnName("inventoryitemid");
            entity.Property(e => e.ItemName).HasColumnName("itemname").IsRequired().HasMaxLength(150);
            entity.Property(e => e.SKU).HasColumnName("sku").HasMaxLength(50);
            entity.Property(e => e.UnitOfMeasure).HasColumnName("unitofmeasure").HasMaxLength(50);
            entity.Property(e => e.CurrentQuantity).HasColumnName("currentquantity").HasColumnType("decimal(18,3)");
            entity.Property(e => e.ReorderLevel).HasColumnName("reorderlevel").HasColumnType("decimal(18,3)").HasDefaultValue(0);
            entity.Property(e => e.AverageUnitCost).HasColumnName("averageunitcost").HasColumnType("decimal(18,2)").HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");
            entity.Property(e => e.UpdatedBy).HasColumnName("updatedby");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.RestaurantID, e.ItemName }).IsUnique();
            entity.HasIndex(e => new { e.RestaurantID, e.SKU });
        });

        // STOCK TRANSACTIONS
        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.ToTable("stocktransactions");
            entity.HasKey(e => e.StockTransactionID);

            entity.Property(e => e.StockTransactionID).HasColumnName("stocktransactionid");
            entity.Property(e => e.InventoryItemID).HasColumnName("inventoryitemid");
            entity.Property(e => e.TransactionType).HasColumnName("transactiontype").HasConversion<int>();
            entity.Property(e => e.QuantityChange).HasColumnName("quantitychange").HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitCost).HasColumnName("unitcost").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Reference).HasColumnName("reference");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.TransactionTime).HasColumnName("transactiontime").HasColumnType("timestamptz");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.CreatedBy).HasColumnName("createdby");

            entity.HasOne(e => e.InventoryItem)
                  .WithMany()
                  .HasForeignKey(e => e.InventoryItemID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.RestaurantID, e.InventoryItemID, e.TransactionTime });
        });

        // PRODUCT RECIPES
        modelBuilder.Entity<ProductRecipe>(entity =>
        {
            entity.ToTable("productrecipes");
            entity.HasKey(e => e.ProductRecipeID);

            entity.Property(e => e.ProductRecipeID).HasColumnName("productrecipeid");
            entity.Property(e => e.ProductID).HasColumnName("productid");
            entity.Property(e => e.InventoryItemID).HasColumnName("inventoryitemid");
            entity.Property(e => e.QuantityPerUnit).HasColumnName("quantityperunit").HasColumnType("decimal(18,3)");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");

            entity.HasOne<InventoryItem>()
                  .WithMany()
                  .HasForeignKey(e => e.InventoryItemID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Product>()
                  .WithMany()
                  .HasForeignKey(e => e.ProductID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.RestaurantID, e.ProductID, e.InventoryItemID }).IsUnique();
        });
    }
}
