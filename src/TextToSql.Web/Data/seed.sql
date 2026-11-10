-- Sample retail database for Text-to-SQL. Re-runnable: drops and recreates its own five tables with the same deterministic data every time.
-- One batch (no GO separators), run inside a transaction so a failure leaves the previous data in place.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;

CREATE TABLE dbo.Categories (
    CategoryId int NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name nvarchar(50) NOT NULL
);

CREATE TABLE dbo.Products (
    ProductId int NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    CategoryId int NOT NULL CONSTRAINT FK_Products_Categories REFERENCES dbo.Categories (CategoryId),
    UnitPrice decimal(10, 2) NOT NULL,
    Discontinued bit NOT NULL
);

CREATE TABLE dbo.Customers (
    CustomerId int NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    FirstName nvarchar(50) NOT NULL,
    LastName nvarchar(50) NOT NULL,
    Email nvarchar(100) NOT NULL,
    City nvarchar(50) NOT NULL,
    Country nvarchar(50) NOT NULL,
    CreatedDate date NOT NULL
);

CREATE TABLE dbo.Orders (
    OrderId int NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId int NOT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers (CustomerId),
    OrderDate date NOT NULL,
    Status nvarchar(20) NOT NULL
);

CREATE TABLE dbo.OrderItems (
    OrderItemId int NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
    OrderId int NOT NULL CONSTRAINT FK_OrderItems_Orders REFERENCES dbo.Orders (OrderId),
    ProductId int NOT NULL CONSTRAINT FK_OrderItems_Products REFERENCES dbo.Products (ProductId),
    Quantity int NOT NULL,
    UnitPrice decimal(10, 2) NOT NULL
);

INSERT dbo.Categories (CategoryId, Name) VALUES
    (1, N'Beverages'), (2, N'Snacks'), (3, N'Household'), (4, N'Electronics'), (5, N'Books'), (6, N'Garden');

INSERT dbo.Products (ProductId, Name, CategoryId, UnitPrice, Discontinued) VALUES
    (1, N'Green Tea', 1, 4.50, 0), (2, N'Espresso Beans', 1, 12.99, 0), (3, N'Sparkling Water', 1, 1.25, 0), (4, N'Orange Juice', 1, 3.75, 0), (5, N'Cold Brew Coffee', 1, 5.50, 1),
    (6, N'Salted Almonds', 2, 6.25, 0), (7, N'Dark Chocolate Bar', 2, 2.99, 0), (8, N'Potato Chips', 2, 1.99, 0), (9, N'Granola Bites', 2, 4.25, 0), (10, N'Rice Crackers', 2, 2.49, 1),
    (11, N'Dish Soap', 3, 3.49, 0), (12, N'Paper Towels', 3, 8.99, 0), (13, N'Laundry Detergent', 3, 14.50, 0), (14, N'Sponges (6 pack)', 3, 4.99, 0), (15, N'Trash Bags', 3, 9.75, 0),
    (16, N'USB-C Cable', 4, 11.99, 0), (17, N'Wireless Mouse', 4, 24.99, 0), (18, N'Bluetooth Speaker', 4, 49.00, 0), (19, N'Phone Charger', 4, 19.99, 0), (20, N'Headphones', 4, 89.00, 1),
    (21, N'Cookbook', 5, 22.00, 0), (22, N'Mystery Novel', 5, 14.99, 0), (23, N'Travel Guide', 5, 18.50, 0), (24, N'Children''s Atlas', 5, 16.75, 0), (25, N'Poetry Collection', 5, 12.00, 0),
    (26, N'Garden Gloves', 6, 7.99, 0), (27, N'Seed Starter Kit', 6, 15.49, 0), (28, N'Watering Can', 6, 13.25, 0), (29, N'Pruning Shears', 6, 21.99, 0), (30, N'Plant Food', 6, 6.49, 1);

-- 100 customers: 10 first names x 10 last names, cities assigned round robin.
WITH f AS (SELECT * FROM (VALUES (1, N'Alice'), (2, N'Ben'), (3, N'Chloe'), (4, N'David'), (5, N'Emma'), (6, N'Felix'), (7, N'Grace'), (8, N'Hugo'), (9, N'Isla'), (10, N'Jack')) v (i, n)),
     l AS (SELECT * FROM (VALUES (1, N'Anderson'), (2, N'Brown'), (3, N'Chen'), (4, N'Diaz'), (5, N'Evans'), (6, N'Fischer'), (7, N'Garcia'), (8, N'Hughes'), (9, N'Ito'), (10, N'Jones')) v (i, n)),
     c AS (SELECT * FROM (VALUES (0, N'Seattle', N'USA'), (1, N'Chicago', N'USA'), (2, N'Toronto', N'Canada'), (3, N'London', N'UK'), (4, N'Berlin', N'Germany'), (5, N'Paris', N'France'), (6, N'Sydney', N'Australia'), (7, N'Austin', N'USA')) v (i, city, country)),
     fl AS (SELECT (f.i - 1) * 10 + l.i AS id, f.n AS first, l.n AS last FROM f CROSS JOIN l)
INSERT dbo.Customers (CustomerId, FirstName, LastName, Email, City, Country, CreatedDate)
SELECT fl.id, fl.first, fl.last, LOWER(fl.first + N'.' + fl.last + N'@example.com'), c.city, c.country, DATEADD(day, fl.id * 7, CAST('2023-01-01' AS date))
FROM fl JOIN c ON c.i = fl.id % 8;

-- 300 orders spread over 2024-2025 across customers.
WITH d AS (SELECT v FROM (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9)) x (v)),
     n AS (SELECT a.v * 100 + b.v * 10 + c.v + 1 AS i FROM d a CROSS JOIN d b CROSS JOIN d c)
INSERT dbo.Orders (OrderId, CustomerId, OrderDate, Status)
SELECT i, (i * 37) % 100 + 1, DATEADD(day, (i * 3) % 700, CAST('2024-01-01' AS date)),
       CASE i % 10 WHEN 0 THEN N'Cancelled' WHEN 1 THEN N'Pending' ELSE N'Shipped' END
FROM n WHERE i <= 300;

-- 1 to 4 line items per order (750 in total), distinct products within an order, price copied from the product.
WITH lines AS (SELECT v FROM (VALUES (1), (2), (3), (4)) x (v)),
     items AS (
        SELECT o.OrderId, ln.v AS line, (o.OrderId * 7 + ln.v * 13) % 30 + 1 AS ProductId, 1 + (o.OrderId + ln.v) % 5 AS Quantity
        FROM dbo.Orders o JOIN lines ln ON ln.v <= 1 + o.OrderId % 4)
INSERT dbo.OrderItems (OrderItemId, OrderId, ProductId, Quantity, UnitPrice)
SELECT ROW_NUMBER() OVER (ORDER BY i.OrderId, i.line), i.OrderId, i.ProductId, i.Quantity, p.UnitPrice
FROM items i JOIN dbo.Products p ON p.ProductId = i.ProductId;

COMMIT TRANSACTION;

SELECT N'Categories' AS TableName, COUNT(*) AS TotalRows FROM dbo.Categories
UNION ALL SELECT N'Products', COUNT(*) FROM dbo.Products
UNION ALL SELECT N'Customers', COUNT(*) FROM dbo.Customers
UNION ALL SELECT N'Orders', COUNT(*) FROM dbo.Orders
UNION ALL SELECT N'OrderItems', COUNT(*) FROM dbo.OrderItems;
