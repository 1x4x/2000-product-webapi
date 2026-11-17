-- ProductAPI additive MySQL 8 schema update. BACK UP YOUR DATABASE FIRST.
-- Apply only once to a database already containing ProductAPI's base tables.
-- Review names and FK types against your actual EF Core schema before executing.
CREATE TABLE IF NOT EXISTS productimage (
  Id bigint NOT NULL AUTO_INCREMENT PRIMARY KEY,
  ProductId int NOT NULL,
  FileName varchar(255) NOT NULL,
  ImageUrl varchar(255) NOT NULL,
  AltText varchar(255) NULL,
  SortOrder int NOT NULL DEFAULT 0,
  IsPrimary tinyint(1) NOT NULL DEFAULT 0,
  CreatedUtc datetime(6) NOT NULL,
  INDEX IX_productimage_ProductId_SortOrder (ProductId, SortOrder),
  CONSTRAINT FK_productimage_product_ProductId FOREIGN KEY (ProductId) REFERENCES product(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS productvariant (
  Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
  ProductId int NOT NULL,
  Sku varchar(80) NOT NULL,
  Color varchar(80) NULL,
  Size varchar(80) NULL,
  Material varchar(80) NULL,
  ResellerPrice decimal(18,2) NOT NULL DEFAULT 0,
  RetailPrice decimal(18,2) NOT NULL DEFAULT 0,
  StockQuantity int NOT NULL DEFAULT 0,
  ReservedQuantity int NOT NULL DEFAULT 0,
  Active tinyint(1) NOT NULL DEFAULT 1,
  UNIQUE KEY IX_productvariant_Sku (Sku),
  INDEX IX_productvariant_ProductId (ProductId),
  CONSTRAINT FK_productvariant_product_ProductId FOREIGN KEY (ProductId) REFERENCES product(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS warehouse (
  Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Name varchar(120) NOT NULL,
  Code varchar(80) NOT NULL,
  Address varchar(255) NULL,
  UNIQUE KEY IX_warehouse_Code (Code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS warehousestock (
  Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
  WarehouseId int NOT NULL,
  ProductId int NOT NULL,
  ProductVariantId int NULL,
  Bin varchar(80) NULL,
  Quantity int NOT NULL DEFAULT 0,
  INDEX IX_warehousestock_WarehouseId (WarehouseId),
  INDEX IX_warehousestock_ProductId (ProductId),
  INDEX IX_warehousestock_ProductVariantId (ProductVariantId),
  CONSTRAINT FK_warehousestock_warehouse FOREIGN KEY (WarehouseId) REFERENCES warehouse(Id) ON DELETE CASCADE,
  CONSTRAINT FK_warehousestock_product FOREIGN KEY (ProductId) REFERENCES product(Id) ON DELETE RESTRICT,
  CONSTRAINT FK_warehousestock_variant FOREIGN KEY (ProductVariantId) REFERENCES productvariant(Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS ordershipment (
  Id bigint NOT NULL AUTO_INCREMENT PRIMARY KEY,
  ResellerOrderId bigint NOT NULL,
  Carrier varchar(120) NULL,
  TrackingNumber varchar(120) NULL,
  ShippedUtc datetime(6) NOT NULL,
  INDEX IX_ordershipment_ResellerOrderId (ResellerOrderId),
  CONSTRAINT FK_ordershipment_ResellerOrders FOREIGN KEY (ResellerOrderId) REFERENCES ResellerOrders(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ProductVariantId is nullable for backward compatibility with existing base-product orders.
-- If the column already exists, skip this statement.
ALTER TABLE ResellerOrderItems ADD COLUMN ProductVariantId int NULL;
CREATE INDEX IX_ResellerOrderItems_ProductVariantId ON ResellerOrderItems(ProductVariantId);
ALTER TABLE ResellerOrderItems ADD CONSTRAINT FK_ResellerOrderItems_productvariant
  FOREIGN KEY (ProductVariantId) REFERENCES productvariant(Id) ON DELETE RESTRICT;
