using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Restaurant_System.Migrations
{
    /// <inheritdoc />
    public partial class AddInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderID",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductID",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_RestaurantTables_RestaurantTableID",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_WaiterUserID",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Orders_OrderID",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryID",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_SubCategories_SubCategoryID",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategories_Categories_CategoryID",
                table: "SubCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WaiterRequests",
                table: "WaiterRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserRole",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubCategories",
                table: "SubCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RestaurantTables",
                table: "RestaurantTables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Payments");

            migrationBuilder.RenameTable(
                name: "WaiterRequests",
                newName: "waiterrequests");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "SubCategories",
                newName: "subcategories");

            migrationBuilder.RenameTable(
                name: "RestaurantTables",
                newName: "restauranttables");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "products");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "payments");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "orders");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "orderitems");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "categories");

            migrationBuilder.RenameColumn(
                name: "TableNumber",
                table: "waiterrequests",
                newName: "tablenumber");

            migrationBuilder.RenameColumn(
                name: "RequestTime",
                table: "waiterrequests",
                newName: "requesttime");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "waiterrequests",
                newName: "message");

            migrationBuilder.RenameColumn(
                name: "WaiterRequestID",
                table: "waiterrequests",
                newName: "waiterrequestid");

            migrationBuilder.RenameColumn(
                name: "UserRole",
                table: "users",
                newName: "userrole");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "users",
                newName: "updatedby");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "users",
                newName: "updatedat");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "users",
                newName: "phonenumber");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "passwordhash");

            migrationBuilder.RenameColumn(
                name: "IsAvailable",
                table: "users",
                newName: "isavailable");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "users",
                newName: "createdby");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "createdat");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "users",
                newName: "userid");

            migrationBuilder.RenameColumn(
                name: "SubCategoryName",
                table: "subcategories",
                newName: "subcategoryname");

            migrationBuilder.RenameColumn(
                name: "CategoryID",
                table: "subcategories",
                newName: "categoryid");

            migrationBuilder.RenameColumn(
                name: "SubCategoryID",
                table: "subcategories",
                newName: "subcategoryid");

            migrationBuilder.RenameIndex(
                name: "IX_SubCategories_CategoryID",
                table: "subcategories",
                newName: "IX_subcategories_categoryid");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "restauranttables",
                newName: "updatedby");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "restauranttables",
                newName: "updatedat");

            migrationBuilder.RenameColumn(
                name: "TableName",
                table: "restauranttables",
                newName: "tablename");

            migrationBuilder.RenameColumn(
                name: "Seats",
                table: "restauranttables",
                newName: "seats");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "restauranttables",
                newName: "createdby");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "restauranttables",
                newName: "createdat");

            migrationBuilder.RenameColumn(
                name: "RestaurantTableID",
                table: "restauranttables",
                newName: "restauranttableid");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "products",
                newName: "updatedby");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "products",
                newName: "updatedat");

            migrationBuilder.RenameColumn(
                name: "SubCategoryID",
                table: "products",
                newName: "subcategoryid");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "products",
                newName: "productname");

            migrationBuilder.RenameColumn(
                name: "ProductDescription",
                table: "products",
                newName: "productdescription");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "products",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "IsVeg",
                table: "products",
                newName: "isveg");

            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "products",
                newName: "imagepath");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "products",
                newName: "createdby");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "products",
                newName: "createdat");

            migrationBuilder.RenameColumn(
                name: "CategoryID",
                table: "products",
                newName: "categoryid");

            migrationBuilder.RenameColumn(
                name: "ProductID",
                table: "products",
                newName: "productid");

            migrationBuilder.RenameIndex(
                name: "IX_Products_SubCategoryID",
                table: "products",
                newName: "IX_products_subcategoryid");

            migrationBuilder.RenameIndex(
                name: "IX_Products_CategoryID",
                table: "products",
                newName: "IX_products_categoryid");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "payments",
                newName: "paymentstatus");

            migrationBuilder.RenameColumn(
                name: "PaymentMethod",
                table: "payments",
                newName: "paymentmethod");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "payments",
                newName: "orderid");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "payments",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "PaymentID",
                table: "payments",
                newName: "paymentid");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_OrderID",
                table: "payments",
                newName: "IX_payments_orderid");

            migrationBuilder.RenameColumn(
                name: "WaiterUserID",
                table: "orders",
                newName: "waiteruserid");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "orders",
                newName: "userid");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "orders",
                newName: "updatedby");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "orders",
                newName: "updatedat");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "orders",
                newName: "totalamount");

            migrationBuilder.RenameColumn(
                name: "Subtotal",
                table: "orders",
                newName: "subtotal");

            migrationBuilder.RenameColumn(
                name: "ServiceCharge",
                table: "orders",
                newName: "servicecharge");

            migrationBuilder.RenameColumn(
                name: "SGST",
                table: "orders",
                newName: "sgst");

            migrationBuilder.RenameColumn(
                name: "RestaurantTableID",
                table: "orders",
                newName: "restauranttableid");

            migrationBuilder.RenameColumn(
                name: "OrderStatus",
                table: "orders",
                newName: "orderstatus");

            migrationBuilder.RenameColumn(
                name: "KitchenStatus",
                table: "orders",
                newName: "kitchenstatus");

            migrationBuilder.RenameColumn(
                name: "IsAssigned",
                table: "orders",
                newName: "isassigned");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "orders",
                newName: "createdby");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "orders",
                newName: "createdat");

            migrationBuilder.RenameColumn(
                name: "CGST",
                table: "orders",
                newName: "cgst");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "orders",
                newName: "orderid");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_WaiterUserID",
                table: "orders",
                newName: "IX_orders_waiteruserid");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_RestaurantTableID",
                table: "orders",
                newName: "IX_orders_restauranttableid");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "orderitems",
                newName: "updatedby");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "orderitems",
                newName: "updatedat");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "orderitems",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "ProductID",
                table: "orderitems",
                newName: "productid");

            migrationBuilder.RenameColumn(
                name: "OrderID",
                table: "orderitems",
                newName: "orderid");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "orderitems",
                newName: "createdby");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "orderitems",
                newName: "createdat");

            migrationBuilder.RenameColumn(
                name: "OrderItemID",
                table: "orderitems",
                newName: "orderitemid");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_ProductID",
                table: "orderitems",
                newName: "IX_orderitems_productid");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderID",
                table: "orderitems",
                newName: "IX_orderitems_orderid");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "categories",
                newName: "updatedat");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "categories",
                newName: "createdat");

            migrationBuilder.RenameColumn(
                name: "CategoryName",
                table: "categories",
                newName: "categoryname");

            migrationBuilder.RenameColumn(
                name: "CategoryID",
                table: "categories",
                newName: "categoryid");

            migrationBuilder.AlterColumn<int>(
                name: "tablenumber",
                table: "waiterrequests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "requesttime",
                table: "waiterrequests",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "message",
                table: "waiterrequests",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<int>(
                name: "waiterrequestid",
                table: "waiterrequests",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "isnotified",
                table: "waiterrequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "restaurantid",
                table: "waiterrequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "restauranttableid",
                table: "waiterrequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "userrole",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "updatedby",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updatedat",
                table: "users",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "phonenumber",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<string>(
                name: "passwordhash",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "isavailable",
                table: "users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "createdby",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdat",
                table: "users",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "userid",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "restaurantid",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "subcategoryname",
                table: "subcategories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "categoryid",
                table: "subcategories",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "subcategoryid",
                table: "subcategories",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "createdat",
                table: "subcategories",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "restaurantid",
                table: "subcategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "updatedat",
                table: "subcategories",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "updatedby",
                table: "restauranttables",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updatedat",
                table: "restauranttables",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "tablename",
                table: "restauranttables",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "seats",
                table: "restauranttables",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "createdby",
                table: "restauranttables",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdat",
                table: "restauranttables",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "restauranttableid",
                table: "restauranttables",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "restaurantid",
                table: "restauranttables",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "updatedby",
                table: "products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updatedat",
                table: "products",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "subcategoryid",
                table: "products",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "productname",
                table: "products",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "productdescription",
                table: "products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<bool>(
                name: "isveg",
                table: "products",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<string>(
                name: "imagepath",
                table: "products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "createdby",
                table: "products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdat",
                table: "products",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "categoryid",
                table: "products",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "productid",
                table: "products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "isavailable",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "restaurantid",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "paymentstatus",
                table: "payments",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "paymentmethod",
                table: "payments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<int>(
                name: "orderid",
                table: "payments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "payments",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "paymentid",
                table: "payments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "completedat",
                table: "payments",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "createdat",
                table: "payments",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "isnotified",
                table: "payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "paymentchannel",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "restaurantid",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "tableno",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "waiteruserid",
                table: "orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "userid",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "updatedby",
                table: "orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updatedat",
                table: "orders",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "totalamount",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0.00m);

            migrationBuilder.AlterColumn<decimal>(
                name: "subtotal",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0.00m);

            migrationBuilder.AlterColumn<decimal>(
                name: "servicecharge",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0.00m);

            migrationBuilder.AlterColumn<decimal>(
                name: "sgst",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0.00m);

            migrationBuilder.AlterColumn<int>(
                name: "restauranttableid",
                table: "orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "orderstatus",
                table: "orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<int>(
                name: "kitchenstatus",
                table: "orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<bool>(
                name: "isassigned",
                table: "orders",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<string>(
                name: "createdby",
                table: "orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdat",
                table: "orders",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "cgst",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0.00m);

            migrationBuilder.AlterColumn<int>(
                name: "orderid",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "appliedofferid",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "closedat",
                table: "orders",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "customerid",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discountamount",
                table: "orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "lastkitchenreadyat",
                table: "orders",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "playsound",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "restaurantid",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "source",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "updatedby",
                table: "orderitems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updatedat",
                table: "orderitems",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "quantity",
                table: "orderitems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "productid",
                table: "orderitems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "orderid",
                table: "orderitems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "createdby",
                table: "orderitems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdat",
                table: "orderitems",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "orderitemid",
                table: "orderitems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "addedtokitchenat",
                table: "orderitems",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "batchid",
                table: "orderitems",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "isprepared",
                table: "orderitems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "preparedat",
                table: "orderitems",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "restaurantid",
                table: "orderitems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "unitprice",
                table: "orderitems",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updatedat",
                table: "categories",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdat",
                table: "categories",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "categoryname",
                table: "categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "categoryid",
                table: "categories",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "restaurantid",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_waiterrequests",
                table: "waiterrequests",
                column: "waiterrequestid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "userid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_subcategories",
                table: "subcategories",
                column: "subcategoryid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_restauranttables",
                table: "restauranttables",
                column: "restauranttableid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_products",
                table: "products",
                column: "productid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payments",
                table: "payments",
                column: "paymentid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_orders",
                table: "orders",
                column: "orderid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_orderitems",
                table: "orderitems",
                column: "orderitemid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_categories",
                table: "categories",
                column: "categoryid");

            migrationBuilder.CreateTable(
                name: "restaurants",
                columns: table => new
                {
                    restaurantid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    logopath = table.Column<string>(type: "text", nullable: true),
                    upi_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    upi_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurants", x => x.restaurantid);
                });

            migrationBuilder.CreateTable(
                name: "analyticssnapshots",
                columns: table => new
                {
                    snapshotid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    snapshotdate = table.Column<DateTime>(type: "date", nullable: false),
                    dailyrevenue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    dailyorders = table.Column<int>(type: "integer", nullable: false),
                    averageordervalue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    cancelledorders = table.Column<int>(type: "integer", nullable: false),
                    newcustomers = table.Column<int>(type: "integer", nullable: false),
                    returningcustomers = table.Column<int>(type: "integer", nullable: false),
                    customersatisfactionscore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    laborcostpercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    foodcostpercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    tableturnoverrate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    lowstockitems = table.Column<int>(type: "integer", nullable: false),
                    inventoryvalue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    weathercondition = table.Column<string>(type: "text", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    weatherimpactscore = table.Column<decimal>(type: "numeric(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analyticssnapshots", x => x.snapshotid);
                    table.ForeignKey(
                        name: "FK_analyticssnapshots_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "budgets",
                columns: table => new
                {
                    budgetid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    monthlybudget = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    actualspent = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budgets", x => x.budgetid);
                    table.ForeignKey(
                        name: "FK_budgets_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competitiveanalyses",
                columns: table => new
                {
                    analysisid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    analysisdate = table.Column<DateTime>(type: "date", nullable: false),
                    competitorname = table.Column<string>(type: "text", nullable: false),
                    competitoravgprice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    competitorrating = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    competitorstrengths = table.Column<string>(type: "text", nullable: false),
                    competitorweaknesses = table.Column<string>(type: "text", nullable: false),
                    marketshare = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    pricecompetitiveness = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    recommendations = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitiveanalyses", x => x.analysisid);
                    table.ForeignKey(
                        name: "FK_competitiveanalyses_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    customerid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    dateofbirth = table.Column<DateTime>(type: "date", nullable: true),
                    totalvisits = table.Column<int>(type: "integer", nullable: false),
                    totalspent = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    firstvisit = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    lastvisit = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    preferences = table.Column<string>(type: "text", nullable: true),
                    allergies = table.Column<string>(type: "text", nullable: true),
                    isvip = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    loyaltypoints = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.customerid);
                    table.ForeignKey(
                        name: "FK_customers_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customizationoptions",
                columns: table => new
                {
                    customizationoptionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    fixedprice = table.Column<decimal>(type: "numeric", nullable: false),
                    productid = table.Column<int>(type: "integer", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customizationoptions", x => x.customizationoptionid);
                    table.ForeignKey(
                        name: "FK_customizationoptions_products_productid",
                        column: x => x.productid,
                        principalTable: "products",
                        principalColumn: "productid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customizationoptions_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    expenseid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    expensedate = table.Column<DateTime>(type: "date", nullable: false),
                    paymentmethod = table.Column<string>(type: "text", nullable: false),
                    vendor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    receiptnumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    isrecurring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    recurringfrequency = table.Column<string>(type: "text", nullable: false),
                    approvedby = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updatedat = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.expenseid);
                    table.ForeignKey(
                        name: "FK_expenses_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventoryitems",
                columns: table => new
                {
                    inventoryitemid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    itemname = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unitofmeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    currentquantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    reorderlevel = table.Column<decimal>(type: "numeric(18,3)", nullable: false, defaultValue: 0m),
                    averageunitcost = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    isactive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    createdat = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updatedat = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: true),
                    updatedby = table.Column<string>(type: "text", nullable: true),
                    restaurantid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventoryitems", x => x.inventoryitemid);
                    table.ForeignKey(
                        name: "FK_inventoryitems_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kitchennotifications",
                columns: table => new
                {
                    notificationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderid = table.Column<int>(type: "integer", nullable: false),
                    tableno = table.Column<int>(type: "integer", nullable: false),
                    notificationtime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    isacknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kitchennotifications", x => x.notificationid);
                    table.ForeignKey(
                        name: "FK_kitchennotifications_orders_orderid",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kitchennotifications_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loyaltyprograms",
                columns: table => new
                {
                    loyaltyid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    programname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pointsperdollar = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 1m),
                    discountperpoint = table.Column<decimal>(type: "numeric(5,4)", nullable: false, defaultValue: 0.01m),
                    pointsforfreeitem = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    isactive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    createdat = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loyaltyprograms", x => x.loyaltyid);
                    table.ForeignKey(
                        name: "FK_loyaltyprograms_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    OfferID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    DiscountPercent = table.Column<float>(type: "real", nullable: true),
                    MinBillAmount = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AutoApply = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.OfferID);
                    table.ForeignKey(
                        name: "FK_Offers_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orderchangehistory",
                columns: table => new
                {
                    orderchangehistoryid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderid = table.Column<int>(type: "integer", nullable: false),
                    changetype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    changedbyuserid = table.Column<int>(type: "integer", nullable: true),
                    changedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    oldvalues = table.Column<string>(type: "text", nullable: false),
                    newvalues = table.Column<string>(type: "text", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orderchangehistory", x => x.orderchangehistoryid);
                    table.ForeignKey(
                        name: "FK_orderchangehistory_orders_orderid",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_orderchangehistory_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_orderchangehistory_users_changedbyuserid",
                        column: x => x.changedbyuserid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "predictivedata",
                columns: table => new
                {
                    predictionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    predictiondate = table.Column<DateTime>(type: "date", nullable: false),
                    predictedrevenue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    predictedorders = table.Column<int>(type: "integer", nullable: false),
                    predictedcustomers = table.Column<int>(type: "integer", nullable: false),
                    peakhours = table.Column<string>(type: "text", nullable: false),
                    recommendedstaffing = table.Column<string>(type: "text", nullable: false),
                    confidencelevel = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    generatedat = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_predictivedata", x => x.predictionid);
                    table.ForeignKey(
                        name: "FK_predictivedata_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    reservationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    restauranttableid = table.Column<int>(type: "integer", nullable: false),
                    customername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customerphone = table.Column<string>(type: "text", nullable: false),
                    customeremail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    reservationtime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    partysize = table.Column<int>(type: "integer", nullable: false),
                    specialrequests = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Confirmed"),
                    createdat = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updatedat = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservations", x => x.reservationid);
                    table.ForeignKey(
                        name: "FK_reservations_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservations_restauranttables_restauranttableid",
                        column: x => x.restauranttableid,
                        principalTable: "restauranttables",
                        principalColumn: "restauranttableid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    reviewid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderid = table.Column<int>(type: "integer", nullable: false),
                    stars = table.Column<int>(type: "integer", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    restaurantid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.reviewid);
                    table.ForeignKey(
                        name: "FK_reviews_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    staffid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    hourlyrate = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    hiredate = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff", x => x.staffid);
                    table.ForeignKey(
                        name: "FK_staff_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tablemanagement",
                columns: table => new
                {
                    tablemanagementid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    restauranttableid = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    section = table.Column<string>(type: "text", nullable: false),
                    currentorderid = table.Column<int>(type: "integer", nullable: true),
                    reservedbycustomerid = table.Column<int>(type: "integer", nullable: true),
                    reservationtime = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    occupiedsince = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    seatingcapacity = table.Column<int>(type: "integer", nullable: false),
                    specialfeatures = table.Column<string>(type: "text", nullable: false),
                    xposition = table.Column<int>(type: "integer", nullable: false),
                    yposition = table.Column<int>(type: "integer", nullable: false),
                    lastupdated = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tablemanagement", x => x.tablemanagementid);
                    table.ForeignKey(
                        name: "FK_tablemanagement_orders_currentorderid",
                        column: x => x.currentorderid,
                        principalTable: "orders",
                        principalColumn: "orderid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tablemanagement_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tablemanagement_restauranttables_restauranttableid",
                        column: x => x.restauranttableid,
                        principalTable: "restauranttables",
                        principalColumn: "restauranttableid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "waiternotifications",
                columns: table => new
                {
                    notificationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderid = table.Column<int>(type: "integer", nullable: false),
                    tableno = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    isacknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waiternotifications", x => x.notificationid);
                    table.ForeignKey(
                        name: "FK_waiternotifications_orders_orderid",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_waiternotifications_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customerfeedbacks",
                columns: table => new
                {
                    feedbackid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customerid = table.Column<int>(type: "integer", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    orderid = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    isresolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    resolutionnotes = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customerfeedbacks", x => x.feedbackid);
                    table.ForeignKey(
                        name: "FK_customerfeedbacks_customers_customerid",
                        column: x => x.customerid,
                        principalTable: "customers",
                        principalColumn: "customerid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customerfeedbacks_orders_orderid",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_customerfeedbacks_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orderitemcustomizations",
                columns: table => new
                {
                    orderitemcustomizationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderitemid = table.Column<int>(type: "integer", nullable: false),
                    customizationoptionid = table.Column<int>(type: "integer", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orderitemcustomizations", x => x.orderitemcustomizationid);
                    table.ForeignKey(
                        name: "FK_orderitemcustomizations_customizationoptions_customizationo~",
                        column: x => x.customizationoptionid,
                        principalTable: "customizationoptions",
                        principalColumn: "customizationoptionid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_orderitemcustomizations_orderitems_orderitemid",
                        column: x => x.orderitemid,
                        principalTable: "orderitems",
                        principalColumn: "orderitemid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_orderitemcustomizations_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productrecipes",
                columns: table => new
                {
                    productrecipeid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    productid = table.Column<int>(type: "integer", nullable: false),
                    inventoryitemid = table.Column<int>(type: "integer", nullable: false),
                    quantityperunit = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productrecipes", x => x.productrecipeid);
                    table.ForeignKey(
                        name: "FK_productrecipes_inventoryitems_inventoryitemid",
                        column: x => x.inventoryitemid,
                        principalTable: "inventoryitems",
                        principalColumn: "inventoryitemid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productrecipes_products_productid",
                        column: x => x.productid,
                        principalTable: "products",
                        principalColumn: "productid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productrecipes_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stocktransactions",
                columns: table => new
                {
                    stocktransactionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inventoryitemid = table.Column<int>(type: "integer", nullable: false),
                    transactiontype = table.Column<int>(type: "integer", nullable: false),
                    quantitychange = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    unitcost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    transactiontime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocktransactions", x => x.stocktransactionid);
                    table.ForeignKey(
                        name: "FK_stocktransactions_inventoryitems_inventoryitemid",
                        column: x => x.inventoryitemid,
                        principalTable: "inventoryitems",
                        principalColumn: "inventoryitemid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stocktransactions_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staffperformances",
                columns: table => new
                {
                    performanceid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    staffid = table.Column<int>(type: "integer", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    performancedate = table.Column<DateTime>(type: "date", nullable: false),
                    ordersserved = table.Column<int>(type: "integer", nullable: false),
                    totalsales = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    averageordervalue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    positivereviews = table.Column<int>(type: "integer", nullable: false),
                    negativereviews = table.Column<int>(type: "integer", nullable: false),
                    efficiencyscore = table.Column<decimal>(type: "numeric(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staffperformances", x => x.performanceid);
                    table.ForeignKey(
                        name: "FK_staffperformances_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staffperformances_staff_staffid",
                        column: x => x.staffid,
                        principalTable: "staff",
                        principalColumn: "staffid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staffshifts",
                columns: table => new
                {
                    shiftid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    staffid = table.Column<int>(type: "integer", nullable: false),
                    restaurantid = table.Column<int>(type: "integer", nullable: false),
                    shiftdate = table.Column<DateTime>(type: "date", nullable: false),
                    starttime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    endtime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hoursworked = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    iscompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staffshifts", x => x.shiftid);
                    table.ForeignKey(
                        name: "FK_staffshifts_restaurants_restaurantid",
                        column: x => x.restaurantid,
                        principalTable: "restaurants",
                        principalColumn: "restaurantid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staffshifts_staff_staffid",
                        column: x => x.staffid,
                        principalTable: "staff",
                        principalColumn: "staffid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_waiterrequests_restaurantid",
                table: "waiterrequests",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_waiterrequests_restauranttableid",
                table: "waiterrequests",
                column: "restauranttableid");

            migrationBuilder.CreateIndex(
                name: "IX_users_restaurantid",
                table: "users",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_subcategories_restaurantid",
                table: "subcategories",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_restauranttables_restaurantid",
                table: "restauranttables",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_products_restaurantid",
                table: "products",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_payments_restaurantid",
                table: "payments",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_orders_appliedofferid",
                table: "orders",
                column: "appliedofferid");

            migrationBuilder.CreateIndex(
                name: "IX_orders_customerid",
                table: "orders",
                column: "customerid");

            migrationBuilder.CreateIndex(
                name: "IX_orders_restaurantid",
                table: "orders",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_orderitems_restaurantid",
                table: "orderitems",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_categories_restaurantid",
                table: "categories",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_analyticssnapshots_restaurantid",
                table: "analyticssnapshots",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_budgets_restaurantid",
                table: "budgets",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_competitiveanalyses_restaurantid",
                table: "competitiveanalyses",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_customerfeedbacks_customerid",
                table: "customerfeedbacks",
                column: "customerid");

            migrationBuilder.CreateIndex(
                name: "IX_customerfeedbacks_orderid",
                table: "customerfeedbacks",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_customerfeedbacks_restaurantid",
                table: "customerfeedbacks",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_customers_restaurantid",
                table: "customers",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_customizationoptions_productid",
                table: "customizationoptions",
                column: "productid");

            migrationBuilder.CreateIndex(
                name: "IX_customizationoptions_restaurantid",
                table: "customizationoptions",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_restaurantid",
                table: "expenses",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryitems_restaurantid_itemname",
                table: "inventoryitems",
                columns: new[] { "restaurantid", "itemname" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventoryitems_restaurantid_sku",
                table: "inventoryitems",
                columns: new[] { "restaurantid", "sku" });

            migrationBuilder.CreateIndex(
                name: "IX_kitchennotifications_orderid",
                table: "kitchennotifications",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_kitchennotifications_restaurantid",
                table: "kitchennotifications",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_loyaltyprograms_restaurantid",
                table: "loyaltyprograms",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_restaurantid",
                table: "Offers",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_orderchangehistory_changedbyuserid",
                table: "orderchangehistory",
                column: "changedbyuserid");

            migrationBuilder.CreateIndex(
                name: "IX_orderchangehistory_orderid",
                table: "orderchangehistory",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_orderchangehistory_restaurantid",
                table: "orderchangehistory",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_orderitemcustomizations_customizationoptionid",
                table: "orderitemcustomizations",
                column: "customizationoptionid");

            migrationBuilder.CreateIndex(
                name: "IX_orderitemcustomizations_orderitemid",
                table: "orderitemcustomizations",
                column: "orderitemid");

            migrationBuilder.CreateIndex(
                name: "IX_orderitemcustomizations_restaurantid",
                table: "orderitemcustomizations",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_predictivedata_restaurantid",
                table: "predictivedata",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_productrecipes_inventoryitemid",
                table: "productrecipes",
                column: "inventoryitemid");

            migrationBuilder.CreateIndex(
                name: "IX_productrecipes_productid",
                table: "productrecipes",
                column: "productid");

            migrationBuilder.CreateIndex(
                name: "IX_productrecipes_restaurantid_productid_inventoryitemid",
                table: "productrecipes",
                columns: new[] { "restaurantid", "productid", "inventoryitemid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservations_restaurantid",
                table: "reservations",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_restauranttableid",
                table: "reservations",
                column: "restauranttableid");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_restaurantid",
                table: "reviews",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_staff_restaurantid",
                table: "staff",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_staffperformances_restaurantid",
                table: "staffperformances",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_staffperformances_staffid",
                table: "staffperformances",
                column: "staffid");

            migrationBuilder.CreateIndex(
                name: "IX_staffshifts_restaurantid",
                table: "staffshifts",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_staffshifts_staffid",
                table: "staffshifts",
                column: "staffid");

            migrationBuilder.CreateIndex(
                name: "IX_stocktransactions_inventoryitemid",
                table: "stocktransactions",
                column: "inventoryitemid");

            migrationBuilder.CreateIndex(
                name: "IX_stocktransactions_restaurantid_inventoryitemid_transactiont~",
                table: "stocktransactions",
                columns: new[] { "restaurantid", "inventoryitemid", "transactiontime" });

            migrationBuilder.CreateIndex(
                name: "IX_tablemanagement_currentorderid",
                table: "tablemanagement",
                column: "currentorderid");

            migrationBuilder.CreateIndex(
                name: "IX_tablemanagement_restaurantid",
                table: "tablemanagement",
                column: "restaurantid");

            migrationBuilder.CreateIndex(
                name: "IX_tablemanagement_restauranttableid",
                table: "tablemanagement",
                column: "restauranttableid");

            migrationBuilder.CreateIndex(
                name: "IX_waiternotifications_orderid",
                table: "waiternotifications",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_waiternotifications_restaurantid",
                table: "waiternotifications",
                column: "restaurantid");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_restaurants_restaurantid",
                table: "categories",
                column: "restaurantid",
                principalTable: "restaurants",
                principalColumn: "restaurantid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orderitems_orders_orderid",
                table: "orderitems",
                column: "orderid",
                principalTable: "orders",
                principalColumn: "orderid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orderitems_products_productid",
                table: "orderitems",
                column: "productid",
                principalTable: "products",
                principalColumn: "productid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orderitems_restaurants_restaurantid",
                table: "orderitems",
                column: "restaurantid",
                principalTable: "restaurants",
                principalColumn: "restaurantid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_Offers_appliedofferid",
                table: "orders",
                column: "appliedofferid",
                principalTable: "Offers",
                principalColumn: "OfferID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_customers_customerid",
                table: "orders",
                column: "customerid",
                principalTable: "customers",
                principalColumn: "customerid");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_restaurants_restaurantid",
                table: "orders",
                column: "restaurantid",
                principalTable: "restaurants",
                principalColumn: "restaurantid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_restauranttables_restauranttableid",
                table: "orders",
                column: "restauranttableid",
                principalTable: "restauranttables",
                principalColumn: "restauranttableid");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_users_waiteruserid",
                table: "orders",
                column: "waiteruserid",
                principalTable: "users",
                principalColumn: "userid");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_orders_orderid",
                table: "payments",
                column: "orderid",
                principalTable: "orders",
                principalColumn: "orderid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_restaurants_restaurantid",
                table: "payments",
                column: "restaurantid",
                principalTable: "restaurants",
                principalColumn: "restaurantid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_products_categories_categoryid",
                table: "products",
                column: "categoryid",
                principalTable: "categories",
                principalColumn: "categoryid");

            migrationBuilder.AddForeignKey(
                name: "FK_products_restaurants_restaurantid",
                table: "products",
                column: "restaurantid",
                principalTable: "restaurants",
                principalColumn: "restaurantid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_products_subcategories_subcategoryid",
                table: "products",
                column: "subcategoryid",
                principalTable: "subcategories",
                principalColumn: "subcategoryid");

            migrationBuilder.AddForeignKey(
                name: "FK_restauranttables_restaurants_restaurantid",
                table: "restauranttables",
                column: "restaurantid",
                principalTable: "restaurants",
                principalColumn: "restaurantid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_subcategories_categories_categoryid",
                table: "subcategories",
                column: "categoryid",
                principalTable: "categories",
                principalColumn: "categoryid");

            migrationBuilder.AddForeignKey(
                name: "FK_subcategories_restaurants_restaurantid",
                table: "subcategories",
                column: "restaurantid",
                principalTable: "restaurants",
                principalColumn: "restaurantid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_restaurants_restaurantid",
                table: "users",
                column: "restaurantid",
                principalTable: "restaurants",
                principalColumn: "restaurantid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_waiterrequests_restaurants_restaurantid",
                table: "waiterrequests",
                column: "restaurantid",
                principalTable: "restaurants",
                principalColumn: "restaurantid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_waiterrequests_restauranttables_restauranttableid",
                table: "waiterrequests",
                column: "restauranttableid",
                principalTable: "restauranttables",
                principalColumn: "restauranttableid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_restaurants_restaurantid",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "FK_orderitems_orders_orderid",
                table: "orderitems");

            migrationBuilder.DropForeignKey(
                name: "FK_orderitems_products_productid",
                table: "orderitems");

            migrationBuilder.DropForeignKey(
                name: "FK_orderitems_restaurants_restaurantid",
                table: "orderitems");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_Offers_appliedofferid",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_customers_customerid",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_restaurants_restaurantid",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_restauranttables_restauranttableid",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_users_waiteruserid",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_orders_orderid",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_restaurants_restaurantid",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_products_categories_categoryid",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_products_restaurants_restaurantid",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_products_subcategories_subcategoryid",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_restauranttables_restaurants_restaurantid",
                table: "restauranttables");

            migrationBuilder.DropForeignKey(
                name: "FK_subcategories_categories_categoryid",
                table: "subcategories");

            migrationBuilder.DropForeignKey(
                name: "FK_subcategories_restaurants_restaurantid",
                table: "subcategories");

            migrationBuilder.DropForeignKey(
                name: "FK_users_restaurants_restaurantid",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_waiterrequests_restaurants_restaurantid",
                table: "waiterrequests");

            migrationBuilder.DropForeignKey(
                name: "FK_waiterrequests_restauranttables_restauranttableid",
                table: "waiterrequests");

            migrationBuilder.DropTable(
                name: "analyticssnapshots");

            migrationBuilder.DropTable(
                name: "budgets");

            migrationBuilder.DropTable(
                name: "competitiveanalyses");

            migrationBuilder.DropTable(
                name: "customerfeedbacks");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "kitchennotifications");

            migrationBuilder.DropTable(
                name: "loyaltyprograms");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "orderchangehistory");

            migrationBuilder.DropTable(
                name: "orderitemcustomizations");

            migrationBuilder.DropTable(
                name: "predictivedata");

            migrationBuilder.DropTable(
                name: "productrecipes");

            migrationBuilder.DropTable(
                name: "reservations");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "staffperformances");

            migrationBuilder.DropTable(
                name: "staffshifts");

            migrationBuilder.DropTable(
                name: "stocktransactions");

            migrationBuilder.DropTable(
                name: "tablemanagement");

            migrationBuilder.DropTable(
                name: "waiternotifications");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "customizationoptions");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "inventoryitems");

            migrationBuilder.DropTable(
                name: "restaurants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_waiterrequests",
                table: "waiterrequests");

            migrationBuilder.DropIndex(
                name: "IX_waiterrequests_restaurantid",
                table: "waiterrequests");

            migrationBuilder.DropIndex(
                name: "IX_waiterrequests_restauranttableid",
                table: "waiterrequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_restaurantid",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_subcategories",
                table: "subcategories");

            migrationBuilder.DropIndex(
                name: "IX_subcategories_restaurantid",
                table: "subcategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_restauranttables",
                table: "restauranttables");

            migrationBuilder.DropIndex(
                name: "IX_restauranttables_restaurantid",
                table: "restauranttables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_products",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_restaurantid",
                table: "products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payments",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_restaurantid",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_appliedofferid",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_customerid",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_restaurantid",
                table: "orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_orderitems",
                table: "orderitems");

            migrationBuilder.DropIndex(
                name: "IX_orderitems_restaurantid",
                table: "orderitems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_categories",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_restaurantid",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "isnotified",
                table: "waiterrequests");

            migrationBuilder.DropColumn(
                name: "restaurantid",
                table: "waiterrequests");

            migrationBuilder.DropColumn(
                name: "restauranttableid",
                table: "waiterrequests");

            migrationBuilder.DropColumn(
                name: "restaurantid",
                table: "users");

            migrationBuilder.DropColumn(
                name: "createdat",
                table: "subcategories");

            migrationBuilder.DropColumn(
                name: "restaurantid",
                table: "subcategories");

            migrationBuilder.DropColumn(
                name: "updatedat",
                table: "subcategories");

            migrationBuilder.DropColumn(
                name: "restaurantid",
                table: "restauranttables");

            migrationBuilder.DropColumn(
                name: "isavailable",
                table: "products");

            migrationBuilder.DropColumn(
                name: "restaurantid",
                table: "products");

            migrationBuilder.DropColumn(
                name: "completedat",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "createdat",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "isnotified",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "paymentchannel",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "restaurantid",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "tableno",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "appliedofferid",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "closedat",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "customerid",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "discountamount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "lastkitchenreadyat",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "playsound",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "restaurantid",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "source",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "addedtokitchenat",
                table: "orderitems");

            migrationBuilder.DropColumn(
                name: "batchid",
                table: "orderitems");

            migrationBuilder.DropColumn(
                name: "isprepared",
                table: "orderitems");

            migrationBuilder.DropColumn(
                name: "preparedat",
                table: "orderitems");

            migrationBuilder.DropColumn(
                name: "restaurantid",
                table: "orderitems");

            migrationBuilder.DropColumn(
                name: "unitprice",
                table: "orderitems");

            migrationBuilder.DropColumn(
                name: "restaurantid",
                table: "categories");

            migrationBuilder.RenameTable(
                name: "waiterrequests",
                newName: "WaiterRequests");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "subcategories",
                newName: "SubCategories");

            migrationBuilder.RenameTable(
                name: "restauranttables",
                newName: "RestaurantTables");

            migrationBuilder.RenameTable(
                name: "products",
                newName: "Products");

            migrationBuilder.RenameTable(
                name: "payments",
                newName: "Payments");

            migrationBuilder.RenameTable(
                name: "orders",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "orderitems",
                newName: "OrderItems");

            migrationBuilder.RenameTable(
                name: "categories",
                newName: "Categories");

            migrationBuilder.RenameColumn(
                name: "tablenumber",
                table: "WaiterRequests",
                newName: "TableNumber");

            migrationBuilder.RenameColumn(
                name: "requesttime",
                table: "WaiterRequests",
                newName: "RequestTime");

            migrationBuilder.RenameColumn(
                name: "message",
                table: "WaiterRequests",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "waiterrequestid",
                table: "WaiterRequests",
                newName: "WaiterRequestID");

            migrationBuilder.RenameColumn(
                name: "userrole",
                table: "Users",
                newName: "UserRole");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "Users",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "updatedby",
                table: "Users",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updatedat",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "phonenumber",
                table: "Users",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "passwordhash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "isavailable",
                table: "Users",
                newName: "IsAvailable");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "createdby",
                table: "Users",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "createdat",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "userid",
                table: "Users",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "subcategoryname",
                table: "SubCategories",
                newName: "SubCategoryName");

            migrationBuilder.RenameColumn(
                name: "categoryid",
                table: "SubCategories",
                newName: "CategoryID");

            migrationBuilder.RenameColumn(
                name: "subcategoryid",
                table: "SubCategories",
                newName: "SubCategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_subcategories_categoryid",
                table: "SubCategories",
                newName: "IX_SubCategories_CategoryID");

            migrationBuilder.RenameColumn(
                name: "updatedby",
                table: "RestaurantTables",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updatedat",
                table: "RestaurantTables",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "tablename",
                table: "RestaurantTables",
                newName: "TableName");

            migrationBuilder.RenameColumn(
                name: "seats",
                table: "RestaurantTables",
                newName: "Seats");

            migrationBuilder.RenameColumn(
                name: "createdby",
                table: "RestaurantTables",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "createdat",
                table: "RestaurantTables",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "restauranttableid",
                table: "RestaurantTables",
                newName: "RestaurantTableID");

            migrationBuilder.RenameColumn(
                name: "updatedby",
                table: "Products",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updatedat",
                table: "Products",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "subcategoryid",
                table: "Products",
                newName: "SubCategoryID");

            migrationBuilder.RenameColumn(
                name: "productname",
                table: "Products",
                newName: "ProductName");

            migrationBuilder.RenameColumn(
                name: "productdescription",
                table: "Products",
                newName: "ProductDescription");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "Products",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "isveg",
                table: "Products",
                newName: "IsVeg");

            migrationBuilder.RenameColumn(
                name: "imagepath",
                table: "Products",
                newName: "ImagePath");

            migrationBuilder.RenameColumn(
                name: "createdby",
                table: "Products",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "createdat",
                table: "Products",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "categoryid",
                table: "Products",
                newName: "CategoryID");

            migrationBuilder.RenameColumn(
                name: "productid",
                table: "Products",
                newName: "ProductID");

            migrationBuilder.RenameIndex(
                name: "IX_products_subcategoryid",
                table: "Products",
                newName: "IX_Products_SubCategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_products_categoryid",
                table: "Products",
                newName: "IX_Products_CategoryID");

            migrationBuilder.RenameColumn(
                name: "paymentstatus",
                table: "Payments",
                newName: "PaymentStatus");

            migrationBuilder.RenameColumn(
                name: "paymentmethod",
                table: "Payments",
                newName: "PaymentMethod");

            migrationBuilder.RenameColumn(
                name: "orderid",
                table: "Payments",
                newName: "OrderID");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "Payments",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "paymentid",
                table: "Payments",
                newName: "PaymentID");

            migrationBuilder.RenameIndex(
                name: "IX_payments_orderid",
                table: "Payments",
                newName: "IX_Payments_OrderID");

            migrationBuilder.RenameColumn(
                name: "waiteruserid",
                table: "Orders",
                newName: "WaiterUserID");

            migrationBuilder.RenameColumn(
                name: "userid",
                table: "Orders",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "updatedby",
                table: "Orders",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updatedat",
                table: "Orders",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "totalamount",
                table: "Orders",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "subtotal",
                table: "Orders",
                newName: "Subtotal");

            migrationBuilder.RenameColumn(
                name: "sgst",
                table: "Orders",
                newName: "SGST");

            migrationBuilder.RenameColumn(
                name: "servicecharge",
                table: "Orders",
                newName: "ServiceCharge");

            migrationBuilder.RenameColumn(
                name: "restauranttableid",
                table: "Orders",
                newName: "RestaurantTableID");

            migrationBuilder.RenameColumn(
                name: "orderstatus",
                table: "Orders",
                newName: "OrderStatus");

            migrationBuilder.RenameColumn(
                name: "kitchenstatus",
                table: "Orders",
                newName: "KitchenStatus");

            migrationBuilder.RenameColumn(
                name: "isassigned",
                table: "Orders",
                newName: "IsAssigned");

            migrationBuilder.RenameColumn(
                name: "createdby",
                table: "Orders",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "createdat",
                table: "Orders",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "cgst",
                table: "Orders",
                newName: "CGST");

            migrationBuilder.RenameColumn(
                name: "orderid",
                table: "Orders",
                newName: "OrderID");

            migrationBuilder.RenameIndex(
                name: "IX_orders_waiteruserid",
                table: "Orders",
                newName: "IX_Orders_WaiterUserID");

            migrationBuilder.RenameIndex(
                name: "IX_orders_restauranttableid",
                table: "Orders",
                newName: "IX_Orders_RestaurantTableID");

            migrationBuilder.RenameColumn(
                name: "updatedby",
                table: "OrderItems",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updatedat",
                table: "OrderItems",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "OrderItems",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "productid",
                table: "OrderItems",
                newName: "ProductID");

            migrationBuilder.RenameColumn(
                name: "orderid",
                table: "OrderItems",
                newName: "OrderID");

            migrationBuilder.RenameColumn(
                name: "createdby",
                table: "OrderItems",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "createdat",
                table: "OrderItems",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "orderitemid",
                table: "OrderItems",
                newName: "OrderItemID");

            migrationBuilder.RenameIndex(
                name: "IX_orderitems_productid",
                table: "OrderItems",
                newName: "IX_OrderItems_ProductID");

            migrationBuilder.RenameIndex(
                name: "IX_orderitems_orderid",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderID");

            migrationBuilder.RenameColumn(
                name: "updatedat",
                table: "Categories",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "createdat",
                table: "Categories",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "categoryname",
                table: "Categories",
                newName: "CategoryName");

            migrationBuilder.RenameColumn(
                name: "categoryid",
                table: "Categories",
                newName: "CategoryID");

            migrationBuilder.AlterColumn<int>(
                name: "TableNumber",
                table: "WaiterRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RequestTime",
                table: "WaiterRequests",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "WaiterRequests",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<int>(
                name: "WaiterRequestID",
                table: "WaiterRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "UserRole",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Users",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "varchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAvailable",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Users",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "SubCategoryName",
                table: "SubCategories",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryID",
                table: "SubCategories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SubCategoryID",
                table: "SubCategories",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "RestaurantTables",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "RestaurantTables",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<string>(
                name: "TableName",
                table: "RestaurantTables",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Seats",
                table: "RestaurantTables",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "RestaurantTables",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "RestaurantTables",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<int>(
                name: "RestaurantTableID",
                table: "RestaurantTables",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Products",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Products",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<int>(
                name: "SubCategoryID",
                table: "Products",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "Products",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProductDescription",
                table: "Products",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<bool>(
                name: "IsVeg",
                table: "Products",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Products",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Products",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryID",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductID",
                table: "Products",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentStatus",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Payments",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "OrderID",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentID",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "Payments",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "WaiterUserID",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Orders",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Orders",
                type: "timestamp",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Subtotal",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "SGST",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ServiceCharge",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "RestaurantTableID",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrderStatus",
                table: "Orders",
                type: "longtext",
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KitchenStatus",
                table: "Orders",
                type: "longtext",
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAssigned",
                table: "Orders",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Orders",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Orders",
                type: "timestamp",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<decimal>(
                name: "CGST",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "OrderID",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "OrderItems",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "OrderItems",
                type: "timestamp",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "OrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ProductID",
                table: "OrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "OrderID",
                table: "OrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "OrderItems",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OrderItems",
                type: "timestamp",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<int>(
                name: "OrderItemID",
                table: "OrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Categories",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Categories",
                type: "timestamp",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "Categories",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryID",
                table: "Categories",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WaiterRequests",
                table: "WaiterRequests",
                column: "WaiterRequestID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubCategories",
                table: "SubCategories",
                column: "SubCategoryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RestaurantTables",
                table: "RestaurantTables",
                column: "RestaurantTableID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "ProductID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "PaymentID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "OrderID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "OrderItemID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserRole",
                table: "Users",
                sql: "UserRole IN ('customer', 'waiter', 'kitchen', 'admin')");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderID",
                table: "OrderItems",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "OrderID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductID",
                table: "OrderItems",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_RestaurantTables_RestaurantTableID",
                table: "Orders",
                column: "RestaurantTableID",
                principalTable: "RestaurantTables",
                principalColumn: "RestaurantTableID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_WaiterUserID",
                table: "Orders",
                column: "WaiterUserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Orders_OrderID",
                table: "Payments",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "OrderID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryID",
                table: "Products",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_SubCategories_SubCategoryID",
                table: "Products",
                column: "SubCategoryID",
                principalTable: "SubCategories",
                principalColumn: "SubCategoryID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategories_Categories_CategoryID",
                table: "SubCategories",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
