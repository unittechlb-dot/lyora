/* =====================================================================
   LYORA – Smart Luxury Perfume MIS
   Script 1 of 4 : Create the database and all tables
   Run in SQL Server Management Studio (SSMS) → press F5 (Execute)
   Re-running this script deletes all data and rebuilds the tables.
   ===================================================================== */

IF DB_ID(N'LyoraDB') IS NULL
    CREATE DATABASE LyoraDB;
GO

USE LyoraDB;
GO

/* Drop old tables (children first) so the script can be run again */
DROP TABLE IF EXISTS
    dbo.InventoryTransactions, dbo.Payments, dbo.OrderItems, dbo.Orders,
    dbo.CartItems, dbo.ShoppingCarts, dbo.CustomerAnswers, dbo.CustomerAllergies, dbo.CustomerPreferences,
    dbo.AnswerOptions, dbo.Questions, dbo.BOMItems, dbo.PromotionPerfumes,
    dbo.Promotions, dbo.Perfumes, dbo.Categories, dbo.RawMaterials,
    dbo.Notifications, dbo.Customers, dbo.Administrators, dbo.Users;
GO

/* ---------------------------------------------------------------------
   1. USERS  (User is the parent of Customer and Administrator)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Users (
    UserId        INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    FullName      NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(150) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash  NVARCHAR(256) NOT NULL,
    Phone         NVARCHAR(20)  NULL,
    Role          NVARCHAR(20)  NOT NULL
                  CONSTRAINT CK_Users_Role CHECK (Role IN (N'Customer', N'Administrator')),
    CreatedAt     DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME()
);

CREATE TABLE dbo.Administrators (
    AdminId    INT NOT NULL CONSTRAINT PK_Administrators PRIMARY KEY
               CONSTRAINT FK_Administrators_Users REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
    AdminRole  NVARCHAR(50) NOT NULL CONSTRAINT DF_Administrators_Role DEFAULT N'Manager'
);

CREATE TABLE dbo.Customers (
    CustomerId   INT NOT NULL CONSTRAINT PK_Customers PRIMARY KEY
                 CONSTRAINT FK_Customers_Users REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
    DateOfBirth  DATE          NULL,          -- used for age segmentation reports
    Gender       NVARCHAR(10)  NULL CONSTRAINT CK_Customers_Gender CHECK (Gender IN (N'Male', N'Female')),
    Address      NVARCHAR(250) NULL
);

CREATE TABLE dbo.Notifications (
    NotificationId  INT IDENTITY(1,1) CONSTRAINT PK_Notifications PRIMARY KEY,
    UserId          INT NOT NULL CONSTRAINT FK_Notifications_Users REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
    Title           NVARCHAR(150) NOT NULL,
    Message         NVARCHAR(500) NOT NULL,
    Type            NVARCHAR(20)  NOT NULL
                    CONSTRAINT CK_Notifications_Type CHECK (Type IN (N'Promotion', N'Order', N'LowStock')),
    SentAt          DATETIME2 NOT NULL CONSTRAINT DF_Notifications_SentAt DEFAULT SYSDATETIME(),
    IsRead          BIT       NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0
);

/* ---------------------------------------------------------------------
   2. PRODUCTS, RAW MATERIALS AND BOM  (core of the project)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Categories (
    CategoryId   INT IDENTITY(1,1) CONSTRAINT PK_Categories PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
    Description  NVARCHAR(300) NULL
);

CREATE TABLE dbo.Perfumes (
    PerfumeId    INT IDENTITY(1,1) CONSTRAINT PK_Perfumes PRIMARY KEY,
    CategoryId   INT NOT NULL CONSTRAINT FK_Perfumes_Categories REFERENCES dbo.Categories(CategoryId),
    Name         NVARCHAR(100) NOT NULL,
    Description  NVARCHAR(500) NULL,
    Price        DECIMAL(10,2) NOT NULL CONSTRAINT CK_Perfumes_Price CHECK (Price > 0),
    SizeMl       INT           NOT NULL CONSTRAINT CK_Perfumes_Size CHECK (SizeMl > 0),
    Intensity    NVARCHAR(10)  NOT NULL CONSTRAINT CK_Perfumes_Intensity CHECK (Intensity IN (N'Light', N'Strong')),
    Style        NVARCHAR(10)  NOT NULL CONSTRAINT CK_Perfumes_Style CHECK (Style IN (N'Natural', N'Bold', N'Simple')),
    ImageUrl     NVARCHAR(300) NULL,
    IsActive     BIT NOT NULL CONSTRAINT DF_Perfumes_IsActive DEFAULT 1
);

CREATE TABLE dbo.RawMaterials (
    MaterialId       INT IDENTITY(1,1) CONSTRAINT PK_RawMaterials PRIMARY KEY,
    Name             NVARCHAR(100) NOT NULL CONSTRAINT UQ_RawMaterials_Name UNIQUE,
    Unit             NVARCHAR(10)  NOT NULL CONSTRAINT CK_RawMaterials_Unit CHECK (Unit IN (N'ml', N'g', N'pcs')),
    QuantityInStock  DECIMAL(12,2) NOT NULL CONSTRAINT CK_RawMaterials_Stock CHECK (QuantityInStock >= 0),
    Threshold        DECIMAL(12,2) NOT NULL CONSTRAINT CK_RawMaterials_Threshold CHECK (Threshold >= 0),
    CostPerUnit      DECIMAL(10,4) NOT NULL CONSTRAINT CK_RawMaterials_Cost CHECK (CostPerUnit >= 0),
    IsAllergen       BIT NOT NULL CONSTRAINT DF_RawMaterials_IsAllergen DEFAULT 0
);

/* BOM = Bill of Materials: the formula of each perfume (quantity per ONE bottle) */
CREATE TABLE dbo.BOMItems (
    BomItemId          INT IDENTITY(1,1) CONSTRAINT PK_BOMItems PRIMARY KEY,
    PerfumeId          INT NOT NULL CONSTRAINT FK_BOMItems_Perfumes REFERENCES dbo.Perfumes(PerfumeId) ON DELETE CASCADE,
    MaterialId         INT NOT NULL CONSTRAINT FK_BOMItems_RawMaterials REFERENCES dbo.RawMaterials(MaterialId),
    QuantityPerBottle  DECIMAL(10,2) NOT NULL CONSTRAINT CK_BOMItems_Qty CHECK (QuantityPerBottle > 0),
    CONSTRAINT UQ_BOMItems_Perfume_Material UNIQUE (PerfumeId, MaterialId)
);

CREATE TABLE dbo.Promotions (
    PromotionId      INT IDENTITY(1,1) CONSTRAINT PK_Promotions PRIMARY KEY,
    Title            NVARCHAR(150) NOT NULL,
    DiscountPercent  DECIMAL(5,2)  NOT NULL CONSTRAINT CK_Promotions_Discount CHECK (DiscountPercent BETWEEN 0 AND 100),
    StartDate        DATE NOT NULL,
    EndDate          DATE NOT NULL,
    CONSTRAINT CK_Promotions_Dates CHECK (EndDate >= StartDate)
);

CREATE TABLE dbo.PromotionPerfumes (
    PromotionId  INT NOT NULL CONSTRAINT FK_PromotionPerfumes_Promotions REFERENCES dbo.Promotions(PromotionId) ON DELETE CASCADE,
    PerfumeId    INT NOT NULL CONSTRAINT FK_PromotionPerfumes_Perfumes REFERENCES dbo.Perfumes(PerfumeId) ON DELETE CASCADE,
    CONSTRAINT PK_PromotionPerfumes PRIMARY KEY (PromotionId, PerfumeId)
);

/* ---------------------------------------------------------------------
   3. QUESTIONNAIRE AND CUSTOMER PREFERENCES
   --------------------------------------------------------------------- */
/* 8 simple preference questions (no psychological questions) */
CREATE TABLE dbo.Questions (
    QuestionId     INT IDENTITY(1,1) CONSTRAINT PK_Questions PRIMARY KEY,
    Text           NVARCHAR(300) NOT NULL,
    Topic          NVARCHAR(20)  NOT NULL CONSTRAINT CK_Questions_Topic
                   CHECK (Topic IN (N'Allergy', N'Intensity', N'Style', N'Family', N'Notes', N'Occasion', N'Size', N'Budget')),
    AllowMultiple  BIT NOT NULL CONSTRAINT DF_Questions_AllowMultiple DEFAULT 0,   -- 1 = the customer can tick several options
    SortOrder      INT NOT NULL CONSTRAINT DF_Questions_SortOrder DEFAULT 0,
    IsActive       BIT NOT NULL CONSTRAINT DF_Questions_IsActive DEFAULT 1
);

/* Each option says what it means for the recommendation (only the columns that apply are filled) */
CREATE TABLE dbo.AnswerOptions (
    OptionId          INT IDENTITY(1,1) CONSTRAINT PK_AnswerOptions PRIMARY KEY,
    QuestionId        INT NOT NULL CONSTRAINT FK_AnswerOptions_Questions REFERENCES dbo.Questions(QuestionId) ON DELETE CASCADE,
    Text              NVARCHAR(200) NOT NULL,
    SortOrder         INT NOT NULL CONSTRAINT DF_AnswerOptions_SortOrder DEFAULT 0,
    MapsToIntensity   NVARCHAR(10) NULL CONSTRAINT CK_AnswerOptions_Intensity CHECK (MapsToIntensity IN (N'Light', N'Strong')),
    MapsToStyle       NVARCHAR(10) NULL CONSTRAINT CK_AnswerOptions_Style CHECK (MapsToStyle IN (N'Natural', N'Bold', N'Simple')),
    MapsToCategoryId  INT NULL CONSTRAINT FK_AnswerOptions_Categories REFERENCES dbo.Categories(CategoryId),
    MapsToMaterialId  INT NULL CONSTRAINT FK_AnswerOptions_RawMaterials REFERENCES dbo.RawMaterials(MaterialId),   -- allergy or liked note
    MapsToSizeMl      INT NULL,
    MinPrice          DECIMAL(10,2) NULL,
    MaxPrice          DECIMAL(10,2) NULL
);

CREATE TABLE dbo.CustomerPreferences (
    PreferenceId  INT IDENTITY(1,1) CONSTRAINT PK_CustomerPreferences PRIMARY KEY,
    CustomerId    INT NOT NULL CONSTRAINT UQ_CustomerPreferences_Customer UNIQUE
                  CONSTRAINT FK_CustomerPreferences_Customers REFERENCES dbo.Customers(CustomerId) ON DELETE CASCADE,
    CreatedAt     DATETIME2 NOT NULL CONSTRAINT DF_CustomerPreferences_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt     DATETIME2 NOT NULL CONSTRAINT DF_CustomerPreferences_UpdatedAt DEFAULT SYSDATETIME()
);

/* The options the customer chose (one row per ticked option) */
CREATE TABLE dbo.CustomerAnswers (
    PreferenceId  INT NOT NULL CONSTRAINT FK_CustomerAnswers_Preferences REFERENCES dbo.CustomerPreferences(PreferenceId) ON DELETE CASCADE,
    OptionId      INT NOT NULL CONSTRAINT FK_CustomerAnswers_Options REFERENCES dbo.AnswerOptions(OptionId),
    CONSTRAINT PK_CustomerAnswers PRIMARY KEY (PreferenceId, OptionId)
);

/* ---------------------------------------------------------------------
   4. CART, ORDERS AND PAYMENT (Cash on Delivery)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.ShoppingCarts (
    CartId      INT IDENTITY(1,1) CONSTRAINT PK_ShoppingCarts PRIMARY KEY,
    CustomerId  INT NOT NULL CONSTRAINT UQ_ShoppingCarts_Customer UNIQUE
                CONSTRAINT FK_ShoppingCarts_Customers REFERENCES dbo.Customers(CustomerId) ON DELETE CASCADE,
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_ShoppingCarts_CreatedAt DEFAULT SYSDATETIME()
);

CREATE TABLE dbo.CartItems (
    CartItemId  INT IDENTITY(1,1) CONSTRAINT PK_CartItems PRIMARY KEY,
    CartId      INT NOT NULL CONSTRAINT FK_CartItems_ShoppingCarts REFERENCES dbo.ShoppingCarts(CartId) ON DELETE CASCADE,
    PerfumeId   INT NOT NULL CONSTRAINT FK_CartItems_Perfumes REFERENCES dbo.Perfumes(PerfumeId),
    Quantity    INT NOT NULL CONSTRAINT CK_CartItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT UQ_CartItems_Cart_Perfume UNIQUE (CartId, PerfumeId)
);

CREATE TABLE dbo.Orders (
    OrderId          INT IDENTITY(1,1) CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId       INT NOT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers(CustomerId),
    OrderDate        DATETIME2 NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT SYSDATETIME(),
    Status           NVARCHAR(20) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Pending'
                     CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Pending', N'OutForDelivery', N'DeliveredPaid', N'Cancelled')),
    DeliveryAddress  NVARCHAR(250) NOT NULL,
    TotalAmount      DECIMAL(10,2) NOT NULL CONSTRAINT CK_Orders_Total CHECK (TotalAmount >= 0)
);

CREATE TABLE dbo.OrderItems (
    OrderItemId  INT IDENTITY(1,1) CONSTRAINT PK_OrderItems PRIMARY KEY,
    OrderId      INT NOT NULL CONSTRAINT FK_OrderItems_Orders REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
    PerfumeId    INT NOT NULL CONSTRAINT FK_OrderItems_Perfumes REFERENCES dbo.Perfumes(PerfumeId),
    Quantity     INT NOT NULL CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    UnitPrice    DECIMAL(10,2) NOT NULL CONSTRAINT CK_OrderItems_Price CHECK (UnitPrice >= 0)
);

CREATE TABLE dbo.Payments (
    PaymentId   INT IDENTITY(1,1) CONSTRAINT PK_Payments PRIMARY KEY,
    OrderId     INT NOT NULL CONSTRAINT UQ_Payments_Order UNIQUE
                CONSTRAINT FK_Payments_Orders REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
    Method      NVARCHAR(30) NOT NULL CONSTRAINT DF_Payments_Method DEFAULT N'Cash on Delivery'
                CONSTRAINT CK_Payments_Method CHECK (Method = N'Cash on Delivery'),
    Amount      DECIMAL(10,2) NOT NULL CONSTRAINT CK_Payments_Amount CHECK (Amount >= 0),
    IsReceived  BIT NOT NULL CONSTRAINT DF_Payments_IsReceived DEFAULT 0,
    ReceivedAt  DATETIME2 NULL
);

/* Every raw-material movement: Sale (negative), Restock (positive), CancelReturn (positive) */
CREATE TABLE dbo.InventoryTransactions (
    TransactionId    INT IDENTITY(1,1) CONSTRAINT PK_InventoryTransactions PRIMARY KEY,
    MaterialId       INT NOT NULL CONSTRAINT FK_InventoryTransactions_RawMaterials REFERENCES dbo.RawMaterials(MaterialId),
    OrderId          INT NULL CONSTRAINT FK_InventoryTransactions_Orders REFERENCES dbo.Orders(OrderId),
    Type             NVARCHAR(20) NOT NULL
                     CONSTRAINT CK_InventoryTransactions_Type CHECK (Type IN (N'Sale', N'Restock', N'CancelReturn')),
    QuantityChange   DECIMAL(12,2) NOT NULL CONSTRAINT CK_InventoryTransactions_Qty CHECK (QuantityChange <> 0),
    TransactionDate  DATETIME2 NOT NULL CONSTRAINT DF_InventoryTransactions_Date DEFAULT SYSDATETIME()
);

/* Indexes on the foreign keys used most by the BOM logic and reports */
CREATE INDEX IX_BOMItems_MaterialId ON dbo.BOMItems(MaterialId);
CREATE INDEX IX_InventoryTransactions_MaterialId ON dbo.InventoryTransactions(MaterialId);
CREATE INDEX IX_InventoryTransactions_OrderId ON dbo.InventoryTransactions(OrderId);
CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
CREATE INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId);
GO

PRINT N'LyoraDB: 20 tables created successfully.';
