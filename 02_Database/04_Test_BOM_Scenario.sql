/* =====================================================================
   LYORA – Smart Luxury Perfume MIS
   Script 4 of 4 : Demo / test of the BOM inventory logic
   Run after Scripts 1-3. Shows the doctor, step by step, that selling
   a perfume deducts its raw materials and raises a low-stock alert.
   To start the demo again from zero: run Scripts 1, 2, 3, then this one.
   ===================================================================== */
USE LyoraDB;
GO

/* ---------- STEP 1: the formula (BOM) of Lyora Royal Oud ---------- */
SELECT p.Name AS Perfume, r.Name AS RawMaterial, b.QuantityPerBottle, r.Unit
FROM dbo.BOMItems b
JOIN dbo.Perfumes p     ON p.PerfumeId = b.PerfumeId
JOIN dbo.RawMaterials r ON r.MaterialId = b.MaterialId
WHERE p.PerfumeId = 4;

/* ---------- STEP 2: stock BEFORE the sale ---------- */
SELECT MaterialId, Name, QuantityInStock, Threshold
FROM dbo.RawMaterials
WHERE MaterialId IN (1, 2, 5, 6, 8, 9);

SELECT * FROM dbo.vw_PerfumeAvailability;

/* ---------- STEP 3: Karim (customer 3) puts 2 bottles of Royal Oud in his cart ---------- */
DECLARE @CartId INT = (SELECT CartId FROM dbo.ShoppingCarts WHERE CustomerId = 3);
DELETE FROM dbo.CartItems WHERE CartId = @CartId;
INSERT INTO dbo.CartItems (CartId, PerfumeId, Quantity) VALUES (@CartId, 4, 2);

EXEC dbo.sp_CheckCartAvailability @CartId = @CartId;   -- empty result = enough stock

/* ---------- STEP 4: place the order (Cash on Delivery) ---------- */
DECLARE @OrderId INT;
EXEC dbo.sp_PlaceOrder @CustomerId = 3,
                       @DeliveryAddress = N'Beirut, Lebanon',
                       @OrderId = @OrderId OUTPUT;     -- returns: Oud Oil is now below 1 liter

SELECT * FROM dbo.Orders     WHERE OrderId = @OrderId;
SELECT * FROM dbo.OrderItems WHERE OrderId = @OrderId;
SELECT * FROM dbo.Payments   WHERE OrderId = @OrderId;

/* ---------- STEP 5: stock AFTER the sale (2 bottles × BOM) ----------
   Alcohol -60 ml, Oud -16 ml, Rose -8 ml, Vanilla -6 ml, Musk -10 ml, Bottle -2 */
SELECT MaterialId, Name, QuantityInStock, Threshold
FROM dbo.RawMaterials
WHERE MaterialId IN (1, 2, 5, 6, 8, 9);

SELECT * FROM dbo.InventoryTransactions WHERE OrderId = @OrderId;
SELECT * FROM dbo.vw_LowStockMaterials;                               -- Oud Oil: 994 < 1000
SELECT * FROM dbo.Notifications WHERE Type = N'LowStock';             -- alert for the administrator

/* ---------- STEP 6: delivery and cash payment ---------- */
EXEC dbo.sp_MarkOutForDelivery @OrderId = @OrderId;
EXEC dbo.sp_ConfirmCashPayment @OrderId = @OrderId;

SELECT o.OrderId, o.Status, pay.Method, pay.Amount, pay.IsReceived, pay.ReceivedAt
FROM dbo.Orders o
JOIN dbo.Payments pay ON pay.OrderId = o.OrderId
WHERE o.OrderId = @OrderId;

/* ---------- STEP 7: a second order that is CANCELLED → materials are restored ---------- */
INSERT INTO dbo.CartItems (CartId, PerfumeId, Quantity) VALUES (@CartId, 1, 3);   -- 3 × Rose Velvet

DECLARE @OrderId2 INT;
EXEC dbo.sp_PlaceOrder @CustomerId = 3, @DeliveryAddress = N'Beirut, Lebanon', @OrderId = @OrderId2 OUTPUT;

SELECT Name, QuantityInStock FROM dbo.RawMaterials WHERE MaterialId IN (1, 2, 8, 9);  -- after deduction

EXEC dbo.sp_CancelOrder @OrderId = @OrderId2;

SELECT Name, QuantityInStock FROM dbo.RawMaterials WHERE MaterialId IN (1, 2, 8, 9);  -- restored
SELECT * FROM dbo.InventoryTransactions WHERE OrderId = @OrderId2;                   -- Sale + CancelReturn

/* ---------- STEP 8: restock Oud Oil (admin received 2 liters) ---------- */
EXEC dbo.sp_RestockRawMaterial @MaterialId = 5, @Quantity = 2000;      -- IsStillLow = 0
SELECT * FROM dbo.vw_LowStockMaterials;                                -- empty again

/* ---------- STEP 9: recommendations and reports ---------- */
EXEC dbo.sp_GetRecommendations @CustomerId = 2;   -- Sara: light + natural, allergic to jasmine
SELECT * FROM dbo.vw_BestSellingPerfumes;
SELECT * FROM dbo.vw_SalesByMonth;
SELECT * FROM dbo.vw_CustomerAgeSegments;
GO
