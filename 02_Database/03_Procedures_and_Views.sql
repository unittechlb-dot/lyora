/* =====================================================================
   LYORA – Smart Luxury Perfume MIS
   Script 3 of 4 : Stored procedures (BOM / inventory logic) and views
   Run after Script 2. Safe to run again (CREATE OR ALTER).
   ===================================================================== */
USE LyoraDB;
GO

/* =====================================================================
   VIEWS
   ===================================================================== */

/* Raw materials under their threshold (e.g. under 1 liter) */
CREATE OR ALTER VIEW dbo.vw_LowStockMaterials
AS
SELECT MaterialId, Name, Unit, QuantityInStock, Threshold
FROM dbo.RawMaterials
WHERE QuantityInStock < Threshold;
GO

/* How many bottles of each perfume can still be produced from current stock
   = the smallest (stock ÷ quantity per bottle) among its BOM materials */
CREATE OR ALTER VIEW dbo.vw_PerfumeAvailability
AS
SELECT p.PerfumeId,
       p.Name,
       p.Price,
       p.SizeMl,
       ISNULL(MIN(CAST(FLOOR(r.QuantityInStock / b.QuantityPerBottle) AS INT)), 0) AS ProducibleBottles
FROM dbo.Perfumes p
LEFT JOIN dbo.BOMItems b     ON b.PerfumeId = p.PerfumeId
LEFT JOIN dbo.RawMaterials r ON r.MaterialId = b.MaterialId
GROUP BY p.PerfumeId, p.Name, p.Price, p.SizeMl;
GO

/* ---------- Analytics (reports module) ---------- */
CREATE OR ALTER VIEW dbo.vw_SalesByMonth
AS
SELECT YEAR(o.OrderDate)                 AS SalesYear,
       MONTH(o.OrderDate)                AS SalesMonth,
       COUNT(DISTINCT o.OrderId)         AS Orders,
       SUM(oi.Quantity)                  AS BottlesSold,
       SUM(oi.Quantity * oi.UnitPrice)   AS Revenue
FROM dbo.Orders o
JOIN dbo.OrderItems oi ON oi.OrderId = o.OrderId
WHERE o.Status <> N'Cancelled'
GROUP BY YEAR(o.OrderDate), MONTH(o.OrderDate);
GO

CREATE OR ALTER VIEW dbo.vw_BestSellingPerfumes
AS
SELECT p.PerfumeId,
       p.Name,
       SUM(oi.Quantity)                AS BottlesSold,
       SUM(oi.Quantity * oi.UnitPrice) AS Revenue
FROM dbo.OrderItems oi
JOIN dbo.Orders o   ON o.OrderId = oi.OrderId
JOIN dbo.Perfumes p ON p.PerfumeId = oi.PerfumeId
WHERE o.Status <> N'Cancelled'
GROUP BY p.PerfumeId, p.Name;
GO

CREATE OR ALTER VIEW dbo.vw_CustomerAgeSegments
AS
SELECT seg.AgeGroup,
       COUNT(DISTINCT seg.CustomerId) AS Customers,
       COUNT(o.OrderId)               AS Orders
FROM (
    SELECT c.CustomerId,
           CASE WHEN a.Age < 25 THEN N'18-24'
                WHEN a.Age < 35 THEN N'25-34'
                WHEN a.Age < 45 THEN N'35-44'
                ELSE N'45+' END AS AgeGroup
    FROM dbo.Customers c
    CROSS APPLY (SELECT DATEDIFF(YEAR, c.DateOfBirth, GETDATE())
                        - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, c.DateOfBirth, GETDATE()), c.DateOfBirth) > GETDATE()
                               THEN 1 ELSE 0 END AS Age) a
    WHERE c.DateOfBirth IS NOT NULL
) seg
LEFT JOIN dbo.Orders o ON o.CustomerId = seg.CustomerId AND o.Status <> N'Cancelled'
GROUP BY seg.AgeGroup;
GO

/* =====================================================================
   STORED PROCEDURES – INVENTORY & BOM
   ===================================================================== */

/* Returns the raw materials that are NOT enough for the customer's cart.
   Empty result = the cart can be produced. */
CREATE OR ALTER PROCEDURE dbo.sp_CheckCartAvailability
    @CartId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.MaterialId,
           r.Name,
           r.Unit,
           SUM(ci.Quantity * b.QuantityPerBottle) AS Required,
           r.QuantityInStock                      AS InStock
    FROM dbo.CartItems ci
    JOIN dbo.BOMItems b     ON b.PerfumeId = ci.PerfumeId
    JOIN dbo.RawMaterials r ON r.MaterialId = b.MaterialId
    WHERE ci.CartId = @CartId
    GROUP BY r.MaterialId, r.Name, r.Unit, r.QuantityInStock
    HAVING SUM(ci.Quantity * b.QuantityPerBottle) > r.QuantityInStock;
END;
GO

/* Converts the customer's cart into an order (Cash on Delivery) and
   deducts the raw materials according to the BOM – all in ONE transaction.
   Returns the raw materials that just went below their threshold
   (the website emails these to the administrator). */
CREATE OR ALTER PROCEDURE dbo.sp_PlaceOrder
    @CustomerId       INT,
    @DeliveryAddress  NVARCHAR(250),
    @OrderId          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;   -- any error rolls back the whole transaction

    DECLARE @CartId INT = (SELECT CartId FROM dbo.ShoppingCarts WHERE CustomerId = @CustomerId);

    IF @CartId IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.CartItems WHERE CartId = @CartId)
        THROW 50001, N'The shopping cart is empty.', 1;

    IF EXISTS (SELECT 1 FROM dbo.CartItems ci
               WHERE ci.CartId = @CartId
                 AND NOT EXISTS (SELECT 1 FROM dbo.BOMItems b WHERE b.PerfumeId = ci.PerfumeId))
        THROW 50002, N'A perfume in the cart has no BOM formula.', 1;

    /* Total quantity needed of each raw material for the whole cart */
    DECLARE @Needs TABLE (MaterialId INT PRIMARY KEY, Required DECIMAL(12,2) NOT NULL);

    BEGIN TRANSACTION;

        INSERT INTO @Needs (MaterialId, Required)
        SELECT b.MaterialId, SUM(ci.Quantity * b.QuantityPerBottle)
        FROM dbo.CartItems ci
        JOIN dbo.BOMItems b ON b.PerfumeId = ci.PerfumeId
        WHERE ci.CartId = @CartId
        GROUP BY b.MaterialId;

        /* 1. Check stock (rows are locked so two orders cannot use the same stock) */
        IF EXISTS (SELECT 1
                   FROM dbo.RawMaterials r WITH (UPDLOCK, HOLDLOCK)
                   JOIN @Needs n ON n.MaterialId = r.MaterialId
                   WHERE r.QuantityInStock < n.Required)
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50003, N'Not enough raw materials to produce this order.', 1;
        END;

        /* 2. Create the order, its lines and the cash-on-delivery payment */
        INSERT INTO dbo.Orders (CustomerId, DeliveryAddress, Status, TotalAmount)
        SELECT @CustomerId, @DeliveryAddress, N'Pending', SUM(ci.Quantity * p.Price)
        FROM dbo.CartItems ci
        JOIN dbo.Perfumes p ON p.PerfumeId = ci.PerfumeId
        WHERE ci.CartId = @CartId;

        SET @OrderId = SCOPE_IDENTITY();

        INSERT INTO dbo.OrderItems (OrderId, PerfumeId, Quantity, UnitPrice)
        SELECT @OrderId, ci.PerfumeId, ci.Quantity, p.Price
        FROM dbo.CartItems ci
        JOIN dbo.Perfumes p ON p.PerfumeId = ci.PerfumeId
        WHERE ci.CartId = @CartId;

        INSERT INTO dbo.Payments (OrderId, Amount)
        SELECT OrderId, TotalAmount FROM dbo.Orders WHERE OrderId = @OrderId;

        /* 3. Deduct raw materials by BOM and log each movement */
        UPDATE r
        SET r.QuantityInStock = r.QuantityInStock - n.Required
        FROM dbo.RawMaterials r
        JOIN @Needs n ON n.MaterialId = r.MaterialId;

        INSERT INTO dbo.InventoryTransactions (MaterialId, OrderId, Type, QuantityChange)
        SELECT MaterialId, @OrderId, N'Sale', -Required
        FROM @Needs;

        /* 4. Low-stock alert for materials that crossed the threshold with this order */
        INSERT INTO dbo.Notifications (UserId, Title, Message, Type)
        SELECT a.AdminId,
               N'Low stock: ' + r.Name,
               N'Only ' + CONVERT(NVARCHAR(30), r.QuantityInStock) + N' ' + r.Unit
                 + N' left (threshold ' + CONVERT(NVARCHAR(30), r.Threshold) + N').',
               N'LowStock'
        FROM dbo.RawMaterials r
        JOIN @Needs n ON n.MaterialId = r.MaterialId
        CROSS JOIN dbo.Administrators a
        WHERE r.QuantityInStock < r.Threshold
          AND r.QuantityInStock + n.Required >= r.Threshold;

        /* 5. Empty the cart */
        DELETE FROM dbo.CartItems WHERE CartId = @CartId;

    COMMIT TRANSACTION;

    SELECT r.MaterialId, r.Name, r.Unit, r.QuantityInStock, r.Threshold
    FROM dbo.RawMaterials r
    JOIN @Needs n ON n.MaterialId = r.MaterialId
    WHERE r.QuantityInStock < r.Threshold
      AND r.QuantityInStock + n.Required >= r.Threshold;
END;
GO

/* Pending → Out for delivery */
CREATE OR ALTER PROCEDURE dbo.sp_MarkOutForDelivery
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Orders SET Status = N'OutForDelivery'
    WHERE OrderId = @OrderId AND Status = N'Pending';

    IF @@ROWCOUNT = 0
        THROW 50010, N'Only a Pending order can be sent out for delivery.', 1;
END;
GO

/* Administrator confirms the cash was received → Delivered – Paid */
CREATE OR ALTER PROCEDURE dbo.sp_ConfirmCashPayment
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderId = @OrderId AND Status = N'OutForDelivery')
        THROW 50011, N'Only an order that is out for delivery can be paid.', 1;

    BEGIN TRANSACTION;
        UPDATE dbo.Payments SET IsReceived = 1, ReceivedAt = SYSDATETIME() WHERE OrderId = @OrderId;
        UPDATE dbo.Orders   SET Status = N'DeliveredPaid'                  WHERE OrderId = @OrderId;
    COMMIT TRANSACTION;
END;
GO

/* Cancels an order and RESTORES the exact raw materials that were deducted */
CREATE OR ALTER PROCEDURE dbo.sp_CancelOrder
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Orders
                   WHERE OrderId = @OrderId AND Status IN (N'Pending', N'OutForDelivery'))
        THROW 50012, N'This order cannot be cancelled.', 1;

    DECLARE @Returned TABLE (MaterialId INT PRIMARY KEY, Quantity DECIMAL(12,2) NOT NULL);

    BEGIN TRANSACTION;

        /* Quantities come from the Sale transactions of this order (not the current BOM) */
        INSERT INTO @Returned (MaterialId, Quantity)
        SELECT MaterialId, SUM(-QuantityChange)
        FROM dbo.InventoryTransactions
        WHERE OrderId = @OrderId AND Type = N'Sale'
        GROUP BY MaterialId;

        UPDATE r
        SET r.QuantityInStock = r.QuantityInStock + t.Quantity
        FROM dbo.RawMaterials r
        JOIN @Returned t ON t.MaterialId = r.MaterialId;

        INSERT INTO dbo.InventoryTransactions (MaterialId, OrderId, Type, QuantityChange)
        SELECT MaterialId, @OrderId, N'CancelReturn', Quantity
        FROM @Returned;

        UPDATE dbo.Orders SET Status = N'Cancelled' WHERE OrderId = @OrderId;

    COMMIT TRANSACTION;
END;
GO

/* Administrator adds received quantity of a raw material */
CREATE OR ALTER PROCEDURE dbo.sp_RestockRawMaterial
    @MaterialId INT,
    @Quantity   DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantity IS NULL OR @Quantity <= 0
        THROW 50020, N'The received quantity must be greater than zero.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.RawMaterials WHERE MaterialId = @MaterialId)
        THROW 50021, N'Raw material not found.', 1;

    BEGIN TRANSACTION;
        UPDATE dbo.RawMaterials
        SET QuantityInStock = QuantityInStock + @Quantity
        WHERE MaterialId = @MaterialId;

        INSERT INTO dbo.InventoryTransactions (MaterialId, OrderId, Type, QuantityChange)
        VALUES (@MaterialId, NULL, N'Restock', @Quantity);
    COMMIT TRANSACTION;

    SELECT MaterialId, Name, Unit, QuantityInStock, Threshold,
           CAST(CASE WHEN QuantityInStock < Threshold THEN 1 ELSE 0 END AS BIT) AS IsStillLow
    FROM dbo.RawMaterials
    WHERE MaterialId = @MaterialId;
END;
GO

/* =====================================================================
   QUESTIONNAIRE
   The recommendation itself is calculated in C# (RecommendationEngine,
   Business Logic Layer). This view only shows the saved answers clearly.
   ===================================================================== */

DROP PROCEDURE IF EXISTS dbo.sp_GetRecommendations;   -- replaced by RecommendationEngine (C#)
GO

CREATE OR ALTER VIEW dbo.vw_CustomerAnswers
AS
SELECT cp.CustomerId,
       u.FullName,
       q.SortOrder AS QuestionNo,
       q.Topic,
       q.Text      AS Question,
       o.Text      AS Answer,
       cp.UpdatedAt
FROM dbo.CustomerPreferences cp
JOIN dbo.Users u            ON u.UserId = cp.CustomerId
JOIN dbo.CustomerAnswers ca ON ca.PreferenceId = cp.PreferenceId
JOIN dbo.AnswerOptions o    ON o.OptionId = ca.OptionId
JOIN dbo.Questions q        ON q.QuestionId = o.QuestionId;
GO

PRINT N'LyoraDB: views and stored procedures created successfully.';
