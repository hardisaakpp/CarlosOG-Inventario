/* =====================================================================
  Gestión de Inventarios - Script de creación inicial (PostgreSQL)

   Crea las tablas Products y Transactions con datos de ejemplo.
   Se puede ejecutar varias veces sin duplicar nada.

   La base de datos hay que crearla antes.

   Cada microservicio usa solo su tabla (ProductService -> Products,
   TransactionService -> Transactions).
   ===================================================================== */

-- Products (ProductService)
CREATE TABLE IF NOT EXISTS "Products" (
    "Id"          INT GENERATED ALWAYS AS IDENTITY,
    "Name"        VARCHAR(150)   NOT NULL,
    "Description" VARCHAR(500),
    "Category"    VARCHAR(100)   NOT NULL,
    "ImageUrl"    VARCHAR(500),
    "Price"       NUMERIC(18,2)  NOT NULL,
    "Stock"       INT            NOT NULL,
    "CreatedAt"   TIMESTAMP      NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    "UpdatedAt"   TIMESTAMP,
    CONSTRAINT "PK_Products" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Products_Price" CHECK ("Price" > 0),
    CONSTRAINT "CK_Products_Stock" CHECK ("Stock" >= 0)
);

CREATE INDEX IF NOT EXISTS "IX_Products_Name"     ON "Products" ("Name");
CREATE INDEX IF NOT EXISTS "IX_Products_Category" ON "Products" ("Category");

-- Transactions (TransactionService)
CREATE TABLE IF NOT EXISTS "Transactions" (
    "Id"         INT GENERATED ALWAYS AS IDENTITY,
    "Date"       TIMESTAMP(0)   NOT NULL,
    "Type"       VARCHAR(10)    NOT NULL,
    "ProductId"  INT            NOT NULL,  -- referencia lógica a Products.Id (otro microservicio)
    "Quantity"   INT            NOT NULL,
    "UnitPrice"  NUMERIC(18,2)  NOT NULL,
    "TotalPrice" NUMERIC(18,2)  NOT NULL,
    "Detail"     VARCHAR(500),
    CONSTRAINT "PK_Transactions" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Transactions_Type" CHECK ("Type" IN ('Purchase', 'Sale')),
    CONSTRAINT "CK_Transactions_Quantity" CHECK ("Quantity" > 0),
    CONSTRAINT "CK_Transactions_UnitPrice" CHECK ("UnitPrice" > 0),
    CONSTRAINT "CK_Transactions_TotalPrice" CHECK ("TotalPrice" = "Quantity" * "UnitPrice")
);

CREATE INDEX IF NOT EXISTS "IX_Transactions_Product_Date" ON "Transactions" ("ProductId", "Date");

-- Datos de ejemplo
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Products") THEN
        INSERT INTO "Products" ("Name", "Description", "Category", "ImageUrl", "Price", "Stock") VALUES
        ('Laptop Pro 14',         'Portátil 14" 16GB RAM 512GB SSD',     'Computación', 'https://picsum.photos/seed/laptop/300/200',   1250.00, 25),
        ('Mouse Inalámbrico',     'Mouse ergonómico 2.4GHz',             'Accesorios',  'https://picsum.photos/seed/mouse/300/200',      18.50, 150),
        ('Teclado Mecánico',      'Teclado mecánico retroiluminado',     'Accesorios',  'https://picsum.photos/seed/keyboard/300/200',   65.00, 80),
        ('Monitor 27" 4K',        'Monitor IPS 27 pulgadas 4K',          'Computación', 'https://picsum.photos/seed/monitor/300/200',   389.99, 40),
        ('Auriculares Bluetooth', 'Cancelación de ruido activa',         'Audio',       'https://picsum.photos/seed/headset/300/200',   129.90, 60),
        ('Parlante Portátil',     'Resistente al agua, 12h de batería',  'Audio',       'https://picsum.photos/seed/speaker/300/200',    49.99, 100),
        ('Smartphone X12',        'Pantalla AMOLED 6.5", 128GB',         'Telefonía',   'https://picsum.photos/seed/phone/300/200',     599.00, 35),
        ('Cargador USB-C 65W',    'Carga rápida GaN',                    'Accesorios',  'https://picsum.photos/seed/charger/300/200',    29.90, 200),
        ('Tablet 10"',            'Tablet 10 pulgadas 64GB',             'Telefonía',   'https://picsum.photos/seed/tablet/300/200',    279.00, 30),
        ('Webcam Full HD',        'Webcam 1080p con micrófono',          'Computación', 'https://picsum.photos/seed/webcam/300/200',     45.00, 70),
        ('Disco SSD 1TB',         'SSD NVMe 1TB',                        'Computación', 'https://picsum.photos/seed/ssd/300/200',        89.00, 90),
        ('Hub USB 7 puertos',     'Hub USB 3.0 alimentado',              'Accesorios',  'https://picsum.photos/seed/hub/300/200',        22.00, 0);
    END IF;

    -- Estas transacciones cuadran con el stock inicial de arriba
    -- (el stock de Products ya es el resultado de estos movimientos).
    IF NOT EXISTS (SELECT 1 FROM "Transactions") THEN
        INSERT INTO "Transactions" ("Date", "Type", "ProductId", "Quantity", "UnitPrice", "TotalPrice", "Detail") VALUES
        (NOW() AT TIME ZONE 'utc' - INTERVAL '20 days', 'Purchase', 1,  30, 1100.00, 33000.00, 'Compra inicial al proveedor'),
        (NOW() AT TIME ZONE 'utc' - INTERVAL '15 days', 'Sale',     1,   5, 1250.00,  6250.00, 'Venta a cliente corporativo'),
        (NOW() AT TIME ZONE 'utc' - INTERVAL '12 days', 'Purchase', 2, 200,   12.00,  2400.00, 'Compra por volumen'),
        (NOW() AT TIME ZONE 'utc' - INTERVAL '9 days',  'Sale',     2,  50,   18.50,   925.00, 'Venta mayorista'),
        (NOW() AT TIME ZONE 'utc' - INTERVAL '7 days',  'Purchase', 5,  70,   95.00,  6650.00, 'Reposición de inventario'),
        (NOW() AT TIME ZONE 'utc' - INTERVAL '3 days',  'Sale',     5,  10,  129.90,  1299.00, 'Venta en tienda'),
        (NOW() AT TIME ZONE 'utc' - INTERVAL '5 days',  'Purchase', 8, 220,   20.00,  4400.00, 'Compra de cargadores'),
        (NOW() AT TIME ZONE 'utc' - INTERVAL '1 day',   'Sale',     8,  20,   29.90,   598.00, 'Venta online');
    END IF;
END $$;
