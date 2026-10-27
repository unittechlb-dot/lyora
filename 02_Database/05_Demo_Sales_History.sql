/* =====================================================================
   LYORA – Smart Luxury Perfume MIS
   Script 5 (optional) : Demo sales history for the Analytics page
   Run after Scripts 1-3 INSTEAD of Script 4 (or reset with 1-2-3 first).
   - adds 6 customers of different ages and genders (password: Customer@123)
   - places 16 orders over the last 6 months with sp_PlaceOrder, so every
     sale really deducts its raw materials by BOM; 14 are delivered and paid
   ===================================================================== */
USE LyoraDB;
GO
SET NOCOUNT ON;

/* ---------- 1. Six more customers ---------- */
DECLARE @Hash NVARCHAR(256) = (SELECT PasswordHash FROM dbo.Users WHERE Email = N'karim@example.com');

DECLARE @NewCustomers TABLE (FullName NVARCHAR(100), Email NVARCHAR(150), DateOfBirth DATE, Gender NVARCHAR(10), Address NVARCHAR(250));
INSERT INTO @NewCustomers VALUES
 (N'Lina Saad',   N'lina@example.com',  '2003-02-10', N'Female', N'Tripoli, Lebanon'),
 (N'Omar Khaled', N'omar@example.com',  '1999-07-21', N'Male',   N'Beirut, Lebanon'),
 (N'Maya Fares',  N'maya@example.com',  '1992-09-05', N'Female', N'Byblos, Lebanon'),
 (N'Hadi Nassar', N'hadi@example.com',  '1979-12-30', N'Male',   N'Saida, Lebanon'),
 (N'Rana Daher',  N'rana@example.com',  '1988-04-17', N'Female', N'Zahle, Lebanon'),
 (N'Ziad Haddad', N'ziad@example.com',  '1968-01-08', N'Male',   N'Tripoli, Lebanon');

INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role)
SELECT n.FullName, n.Email, @Hash, N'Customer'
FROM @NewCustomers n
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.Email = n.Email);

INSERT INTO dbo.Customers (CustomerId, DateOfBirth, Gender, Address)
SELECT u.UserId, n.DateOfBirth, n.Gender, n.Address
FROM @NewCustomers n
JOIN dbo.Users u ON u.Email = n.Email
WHERE NOT EXISTS (SELECT 1 FROM dbo.Customers c WHERE c.CustomerId = u.UserId);

INSERT INTO dbo.ShoppingCarts (CustomerId)
SELECT u.UserId
FROM @NewCustomers n
JOIN dbo.Users u ON u.Email = n.Email
WHERE NOT EXISTS (SELECT 1 FROM dbo.ShoppingCarts s WHERE s.CustomerId = u.UserId);

/* ---------- 2. Past orders (perfume ids: 1 Rose Velvet, 2 Jasmine Night, 3 Sandal Woods, 4 Royal Oud, 5 Citrus Breeze) ---------- */
DECLARE @Demo TABLE (Id INT IDENTITY(1,1), Email NVARCHAR(150), PerfumeId INT, Quantity INT, DaysAgo INT, Delivered BIT);
INSERT INTO @Demo (Email, PerfumeId, Quantity, DaysAgo, Delivered) VALUES
 (N'lina@example.com',  1, 2, 170, 1),
 (N'omar@example.com',  4, 1, 160, 1),
 (N'maya@example.com',  1, 1, 150, 1),
 (N'hadi@example.com',  3, 2, 140, 1),
 (N'rana@example.com',  2, 2, 120, 1),
 (N'ziad@example.com',  3, 1, 110, 1),
 (N'sara@example.com',  5, 2,  95, 1),
 (N'karim@example.com', 2, 1,  80, 1),
 (N'lina@example.com',  5, 3,  65, 1),
 (N'omar@example.com',  2, 2,  50, 1),
 (N'maya@example.com',  1, 2,  40, 1),
 (N'hadi@example.com',  4, 1,  30, 1),   -- Oud Oil goes below 1 liter here → low-stock alert
 (N'rana@example.com',  5, 1,  20, 1),
 (N'ziad@example.com',  1, 1,  12, 1),
 (N'lina@example.com',  3, 1,   6, 0),   -- still pending
 (N'maya@example.com',  4, 1,   2, 0);   -- still pending

DECLARE @LowStock TABLE (MaterialId INT, Name NVARCHAR(100), Unit NVARCHAR(10), QuantityInStock DECIMAL(12,2), Threshold DECIMAL(12,2));
DECLARE @i INT = 1, @Count INT = (SELECT COUNT(*) FROM @Demo);
DECLARE @Email NVARCHAR(150), @PerfumeId INT, @Quantity INT, @DaysAgo INT, @Delivered BIT;
DECLARE @CustomerId INT, @CartId INT, @Address NVARCHAR(250), @OrderId INT, @When DATETIME2;

WHILE @i <= @Count
BEGIN
    SELECT @Email = Email, @PerfumeId = PerfumeId, @Quantity = Quantity, @DaysAgo = DaysAgo, @Delivered = Delivered
    FROM @Demo WHERE Id = @i;

    SELECT @CustomerId = u.UserId, @Address = ISNULL(c.Address, N'Lebanon')
    FROM dbo.Users u JOIN dbo.Customers c ON c.CustomerId = u.UserId
    WHERE u.Email = @Email;
    SELECT @CartId = CartId FROM dbo.ShoppingCarts WHERE CustomerId = @CustomerId;

    DELETE FROM dbo.CartItems WHERE CartId = @CartId;
    INSERT INTO dbo.CartItems (CartId, PerfumeId, Quantity) VALUES (@CartId, @PerfumeId, @Quantity);

    -- the real order procedure: stock check + order + payment + BOM deduction + low-stock alert
    INSERT INTO @LowStock
    EXEC dbo.sp_PlaceOrder @CustomerId = @CustomerId, @DeliveryAddress = @Address, @OrderId = @OrderId OUTPUT;

    -- move the order back in time (for the monthly sales chart)
    SET @When = DATEADD(DAY, -@DaysAgo, SYSDATETIME());
    UPDATE dbo.Orders SET OrderDate = @When WHERE OrderId = @OrderId;
    UPDATE dbo.InventoryTransactions SET TransactionDate = @When WHERE OrderId = @OrderId;

    IF @Delivered = 1
    BEGIN
        EXEC dbo.sp_MarkOutForDelivery @OrderId = @OrderId;
        EXEC dbo.sp_ConfirmCashPayment @OrderId = @OrderId;
        UPDATE dbo.Payments SET ReceivedAt = DATEADD(DAY, 2, @When) WHERE OrderId = @OrderId;
    END;

    SET @i += 1;
END;

SELECT * FROM dbo.vw_SalesByMonth ORDER BY SalesYear, SalesMonth;
SELECT * FROM dbo.vw_BestSellingPerfumes ORDER BY BottlesSold DESC;
SELECT * FROM dbo.vw_CustomerAgeSegments;
SELECT * FROM dbo.vw_CustomerGenderSegments;
SELECT * FROM dbo.vw_LowStockMaterials;
GO

PRINT N'LyoraDB: demo sales history created (16 orders, 8 customers).';
