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

    // NEW: Staff Management DbSets
    public DbSet<Staff> Staff { get; set; }
    public DbSet<StaffShift> StaffShifts { get; set; }
    public DbSet<StaffPerformance> StaffPerformances { get; set; }

    // NEW: Table Management DbSets
    public DbSet<TableManagement> TableManagement { get; set; }
    public DbSet<Reservation> Reservations { get; set; }

    // NEW: Expense Tracking DbSets
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Budget> Budgets { get; set; }

    // NEW: Customer Relationship DbSets
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerFeedback> CustomerFeedbacks { get; set; }
    public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; }

    // NEW: Advanced Analytics DbSets
    public DbSet<AnalyticsSnapshot> AnalyticsSnapshots { get; set; }
    public DbSet<PredictiveData> PredictiveData { get; set; }
    public DbSet<CompetitiveAnalysis> CompetitiveAnalyses { get; set; }

    public DbSet<OrderChangeHistory> OrderChangeHistory { get; set; }

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

        // NEW: STAFF
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("staff");
            entity.HasKey(e => e.StaffID);
            entity.Property(e => e.StaffID).HasColumnName("staffid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).HasColumnName("role").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.HourlyRate).HasColumnName("hourlyrate").HasColumnType("decimal(10,2)");
            entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);
            entity.Property(e => e.HireDate).HasColumnName("hiredate").HasColumnType("timestamptz");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // NEW: STAFF SHIFTS
        modelBuilder.Entity<StaffShift>(entity =>
        {
            entity.ToTable("staffshifts");
            entity.HasKey(e => e.ShiftID);
            entity.Property(e => e.ShiftID).HasColumnName("shiftid");
            entity.Property(e => e.StaffID).HasColumnName("staffid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.ShiftDate).HasColumnName("shiftdate").HasColumnType("date");
            entity.Property(e => e.StartTime).HasColumnName("starttime");
            entity.Property(e => e.EndTime).HasColumnName("endtime");
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50);
            entity.Property(e => e.HoursWorked).HasColumnName("hoursworked").HasColumnType("decimal(5,2)");
            entity.Property(e => e.IsCompleted).HasColumnName("iscompleted").HasDefaultValue(false);
            entity.Property(e => e.Notes).HasColumnName("notes");

            entity.HasOne(e => e.Staff)
                  .WithMany(s => s.Shifts)
                  .HasForeignKey(e => e.StaffID)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderChangeHistory>(entity =>
        {
            entity.ToTable("OrderChangeHistory");

            entity.HasKey(e => e.OrderChangeHistoryID);

            entity.Property(e => e.OrderChangeHistoryID)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            entity.Property(e => e.ChangeType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.ChangedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            entity.Property(e => e.OldValues)
                .HasColumnType("text");

            entity.Property(e => e.NewValues)
                .HasColumnType("text");

            // Foreign key relationships
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

        // NEW: STAFF PERFORMANCE
        modelBuilder.Entity<StaffPerformance>(entity =>
        {
            entity.ToTable("staffperformances");
            entity.HasKey(e => e.PerformanceID);
            entity.Property(e => e.PerformanceID).HasColumnName("performanceid");
            entity.Property(e => e.StaffID).HasColumnName("staffid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.PerformanceDate).HasColumnName("performancedate").HasColumnType("date");
            entity.Property(e => e.OrdersServed).HasColumnName("ordersserved");
            entity.Property(e => e.TotalSales).HasColumnName("totalsales").HasColumnType("decimal(10,2)");
            entity.Property(e => e.AverageOrderValue).HasColumnName("averageordervalue").HasColumnType("decimal(10,2)");
            entity.Property(e => e.PositiveReviews).HasColumnName("positivereviews");
            entity.Property(e => e.NegativeReviews).HasColumnName("negativereviews");
            entity.Property(e => e.EfficiencyScore).HasColumnName("efficiencyscore").HasColumnType("decimal(5,2)");

            entity.HasOne(e => e.Staff)
                  .WithMany(s => s.Performances)
                  .HasForeignKey(e => e.StaffID)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // NEW: TABLE MANAGEMENT
        modelBuilder.Entity<TableManagement>(entity =>
        {
            entity.ToTable("tablemanagement");
            entity.HasKey(e => e.TableManagementID);
            entity.Property(e => e.TableManagementID).HasColumnName("tablemanagementid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.RestaurantTableID).HasColumnName("restauranttableid");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.Section).HasColumnName("section").HasConversion<string>();
            entity.Property(e => e.CurrentOrderID).HasColumnName("currentorderid");
            entity.Property(e => e.ReservedByCustomerID).HasColumnName("reservedbycustomerid");
            entity.Property(e => e.ReservationTime).HasColumnName("reservationtime").HasColumnType("timestamptz");
            entity.Property(e => e.OccupiedSince).HasColumnName("occupiedsince").HasColumnType("timestamptz");
            entity.Property(e => e.SeatingCapacity).HasColumnName("seatingcapacity");
            entity.Property(e => e.SpecialFeatures).HasColumnName("specialfeatures");
            entity.Property(e => e.XPosition).HasColumnName("xposition");
            entity.Property(e => e.YPosition).HasColumnName("yposition");
            entity.Property(e => e.LastUpdated).HasColumnName("lastupdated").HasColumnType("timestamptz");

            entity.HasOne(e => e.RestaurantTable)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantTableID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CurrentOrder)
                  .WithMany()
                  .HasForeignKey(e => e.CurrentOrderID)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // NEW: RESERVATIONS
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(e => e.ReservationID);
            entity.Property(e => e.ReservationID).HasColumnName("reservationid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.RestaurantTableID).HasColumnName("restauranttableid");
            entity.Property(e => e.CustomerName).HasColumnName("customername").IsRequired().HasMaxLength(100);
            entity.Property(e => e.CustomerPhone).HasColumnName("customerphone");
            entity.Property(e => e.CustomerEmail).HasColumnName("customeremail").HasMaxLength(255);
            entity.Property(e => e.ReservationTime).HasColumnName("reservationtime").HasColumnType("timestamptz");
            entity.Property(e => e.PartySize).HasColumnName("partysize");
            entity.Property(e => e.SpecialRequests).HasColumnName("specialrequests");
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue("Confirmed");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedat").HasColumnType("timestamptz");

            entity.HasOne(e => e.RestaurantTable)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantTableID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
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

        // NEW: CUSTOMERS
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(e => e.CustomerID);
            entity.Property(e => e.CustomerID).HasColumnName("customerid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.DateOfBirth).HasColumnName("dateofbirth").HasColumnType("date");
            entity.Property(e => e.TotalVisits).HasColumnName("totalvisits");
            entity.Property(e => e.TotalSpent).HasColumnName("totalspent").HasColumnType("decimal(10,2)");
            entity.Property(e => e.FirstVisit).HasColumnName("firstvisit").HasColumnType("timestamptz");
            entity.Property(e => e.LastVisit).HasColumnName("lastvisit").HasColumnType("timestamptz");
            entity.Property(e => e.Preferences).HasColumnName("preferences");
            entity.Property(e => e.Allergies).HasColumnName("allergies");
            entity.Property(e => e.IsVIP).HasColumnName("isvip").HasDefaultValue(false);
            entity.Property(e => e.LoyaltyPoints).HasColumnName("loyaltypoints");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // NEW: CUSTOMER FEEDBACK
        modelBuilder.Entity<CustomerFeedback>(entity =>
        {
            entity.ToTable("customerfeedbacks");
            entity.HasKey(e => e.FeedbackID);
            entity.Property(e => e.FeedbackID).HasColumnName("feedbackid");
            entity.Property(e => e.CustomerID).HasColumnName("customerid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.OrderID).HasColumnName("orderid");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Comments).HasColumnName("comments");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.IsResolved).HasColumnName("isresolved").HasDefaultValue(false);
            entity.Property(e => e.ResolutionNotes).HasColumnName("resolutionnotes");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Feedbacks)
                  .HasForeignKey(e => e.CustomerID)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderID)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // NEW: LOYALTY PROGRAMS
        modelBuilder.Entity<LoyaltyProgram>(entity =>
        {
            entity.ToTable("loyaltyprograms");
            entity.HasKey(e => e.LoyaltyID);
            entity.Property(e => e.LoyaltyID).HasColumnName("loyaltyid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.ProgramName).HasColumnName("programname").IsRequired().HasMaxLength(100);
            entity.Property(e => e.PointsPerDollar).HasColumnName("pointsperdollar").HasColumnType("decimal(5,2)").HasDefaultValue(1);
            entity.Property(e => e.DiscountPerPoint).HasColumnName("discountperpoint").HasColumnType("decimal(5,4)").HasDefaultValue(0.01m);
            entity.Property(e => e.PointsForFreeItem).HasColumnName("pointsforfreeitem").HasDefaultValue(100);
            entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasColumnType("timestamptz");

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

        // NEW: PREDICTIVE DATA
        modelBuilder.Entity<PredictiveData>(entity =>
        {
            entity.ToTable("predictivedata");
            entity.HasKey(e => e.PredictionID);
            entity.Property(e => e.PredictionID).HasColumnName("predictionid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.PredictionDate).HasColumnName("predictiondate").HasColumnType("date");
            entity.Property(e => e.PredictedRevenue).HasColumnName("predictedrevenue").HasColumnType("decimal(10,2)");
            entity.Property(e => e.PredictedOrders).HasColumnName("predictedorders");
            entity.Property(e => e.PredictedCustomers).HasColumnName("predictedcustomers");
            entity.Property(e => e.PeakHours).HasColumnName("peakhours");
            entity.Property(e => e.RecommendedStaffing).HasColumnName("recommendedstaffing");
            entity.Property(e => e.ConfidenceLevel).HasColumnName("confidencelevel").HasColumnType("decimal(5,4)");
            entity.Property(e => e.GeneratedAt).HasColumnName("generatedat").HasColumnType("timestamptz");

            entity.HasOne(e => e.Restaurant)
                  .WithMany()
                  .HasForeignKey(e => e.RestaurantID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // NEW: COMPETITIVE ANALYSIS
        modelBuilder.Entity<CompetitiveAnalysis>(entity =>
        {
            entity.ToTable("competitiveanalyses");
            entity.HasKey(e => e.AnalysisID);
            entity.Property(e => e.AnalysisID).HasColumnName("analysisid");
            entity.Property(e => e.RestaurantID).HasColumnName("restaurantid");
            entity.Property(e => e.AnalysisDate).HasColumnName("analysisdate").HasColumnType("date");
            entity.Property(e => e.CompetitorName).HasColumnName("competitorname");
            entity.Property(e => e.CompetitorAvgPrice).HasColumnName("competitoravgprice").HasColumnType("decimal(10,2)");
            entity.Property(e => e.CompetitorRating).HasColumnName("competitorrating").HasColumnType("decimal(3,2)");
            entity.Property(e => e.CompetitorStrengths).HasColumnName("competitorstrengths");
            entity.Property(e => e.CompetitorWeaknesses).HasColumnName("competitorweaknesses");
            entity.Property(e => e.MarketShare).HasColumnName("marketshare").HasColumnType("decimal(5,2)");
            entity.Property(e => e.PriceCompetitiveness).HasColumnName("pricecompetitiveness").HasColumnType("decimal(5,2)");
            entity.Property(e => e.Recommendations).HasColumnName("recommendations");

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
