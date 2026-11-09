/* =====================================================================
   Gestión de Inventarios - Script de creación inicial (PostgreSQL)

   Crea las tablas "Products" y "Transactions" e inserta datos de ejemplo.
   Se puede ejecutar varias veces sin duplicar datos.

   La base de datos debe existir antes de ejecutarlo (PostgreSQL no tiene
   CREATE DATABASE IF NOT EXISTS). Ejemplo:
       createdb -U postgres inventario
       psql -U postgres -d inventario -f script.sql

   Cada microservicio usa solo su tabla (ProductService -> Products,
   TransactionService -> Transactions) y se comunican por API REST.
   Transactions.ProductId es clave foránea de Products.Id con ON DELETE
   RESTRICT: la base impide borrar un producto que ya tiene transacciones.

   Las fechas se guardan en UTC. Los identificadores van entre comillas
   dobles, así que al consultar con psql hay que respetarlas.
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
    "CreatedAt"   TIMESTAMPTZ      NOT NULL DEFAULT (NOW()),
    "UpdatedAt"   TIMESTAMPTZ,
    CONSTRAINT "PK_Products" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Products_Price" CHECK ("Price" > 0),
    CONSTRAINT "CK_Products_Stock" CHECK ("Stock" >= 0)
);

CREATE INDEX IF NOT EXISTS "IX_Products_Name"     ON "Products" ("Name");
CREATE INDEX IF NOT EXISTS "IX_Products_Category" ON "Products" ("Category");

-- Transactions (TransactionService)
CREATE TABLE IF NOT EXISTS "Transactions" (
    "Id"         INT GENERATED ALWAYS AS IDENTITY,
    "Date"       TIMESTAMPTZ(0)   NOT NULL,
    "Type"       VARCHAR(10)    NOT NULL,
    "ProductId"  INT            NOT NULL,  -- clave foránea a Products.Id (ver más abajo)
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

-- Clave foránea Transactions -> Products. Va en un bloque aparte para que también
-- se aplique si el script se ejecuta sobre tablas que ya existían sin ella.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Transactions_Products') THEN
        ALTER TABLE "Transactions"
            ADD CONSTRAINT "FK_Transactions_Products"
            FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT;
    END IF;
END $$;

-- Datos de ejemplo
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Products") THEN
        INSERT INTO "Products" ("Name", "Description", "Category", "ImageUrl", "Price", "Stock") VALUES
        ('Laptop Pro 14',         'Portátil 14" 16GB RAM 512GB SSD',     'Computación', 'https://upload.wikimedia.org/wikipedia/commons/thumb/3/37/Schenker_VIA14_Laptop_asv2021-01.jpg/330px-Schenker_VIA14_Laptop_asv2021-01.jpg',   1250.00, 25),
        ('Mouse Inalámbrico',     'Mouse ergonómico 2.4GHz',             'Accesorios',  'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f1/A_black_wireless_computer_mouse.jpg/330px-A_black_wireless_computer_mouse.jpg',      18.50, 150),
        ('Teclado Mecánico',      'Teclado mecánico retroiluminado',     'Accesorios',  'https://upload.wikimedia.org/wikipedia/commons/thumb/5/5a/Mechanical_Keyboard.jpg/330px-Mechanical_Keyboard.jpg',   65.00, 80),
        ('Monitor 27" 4K',        'Monitor IPS 27 pulgadas 4K',          'Computación', 'https://upload.wikimedia.org/wikipedia/commons/thumb/2/24/Computer_monitor_samsung.jpg/330px-Computer_monitor_samsung.jpg',   389.99, 40),
        ('Auriculares Bluetooth', 'Cancelación de ruido activa',         'Audio',       'https://upload.wikimedia.org/wikipedia/commons/thumb/2/21/Sony-WH-1000XM3-kabellose-Bluetooth-Noise-Cancelling-Kopfhoerer.5.jpg/330px-Sony-WH-1000XM3-kabellose-Bluetooth-Noise-Cancelling-Kopfhoerer.5.jpg',   129.90, 60),
        ('Parlante Portátil',     'Resistente al agua, 12h de batería',  'Audio',       'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a4/JBL_Pulse_5_Portable_Bluetooth_Speaker.jpg/330px-JBL_Pulse_5_Portable_Bluetooth_Speaker.jpg',    49.99, 100),
        ('Smartphone X12',        'Pantalla AMOLED 6.5", 128GB',         'Telefonía',   'https://upload.wikimedia.org/wikipedia/commons/thumb/6/63/Nexus_4-2249.jpg/330px-Nexus_4-2249.jpg',     599.00, 35),
        ('Cargador USB-C 65W',    'Carga rápida GaN',                    'Accesorios',  'https://upload.wikimedia.org/wikipedia/commons/thumb/4/4a/Silicon_vs_GaN_30W_USB-C_chargers.jpg/330px-Silicon_vs_GaN_30W_USB-C_chargers.jpg',    29.90, 200),
        ('Tablet 10"',            'Tablet 10 pulgadas 64GB',             'Telefonía',   'https://upload.wikimedia.org/wikipedia/commons/thumb/7/78/Tablet_computer.jpg/330px-Tablet_computer.jpg',    279.00, 30),
        ('Webcam Full HD',        'Webcam 1080p con micrófono',          'Computación', 'https://upload.wikimedia.org/wikipedia/commons/thumb/7/79/Webcam_%28Logitech_c922%29.jpg/330px-Webcam_%28Logitech_c922%29.jpg',     45.00, 70),
        ('Disco SSD 1TB',         'SSD NVMe 1TB',                        'Computación', 'https://upload.wikimedia.org/wikipedia/commons/thumb/4/45/2023_Dysk_SSD_Patriot_P210_1TB.jpg/330px-2023_Dysk_SSD_Patriot_P210_1TB.jpg',        89.00, 90),
        ('Hub USB 7 puertos',     'Hub USB 3.0 alimentado',              'Accesorios',  'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/USB_hub_1_2019-02-16.jpg/330px-USB_hub_1_2019-02-16.jpg',        22.00, 0);
    END IF;

    -- Estas transacciones cuadran con el stock inicial de arriba
    -- (el stock de Products ya es el resultado de estos movimientos).
    IF NOT EXISTS (SELECT 1 FROM "Transactions") THEN
        INSERT INTO "Transactions" ("Date", "Type", "ProductId", "Quantity", "UnitPrice", "TotalPrice", "Detail") VALUES
        (NOW() - INTERVAL '20 days', 'Purchase', 1,  30, 1100.00, 33000.00, 'Compra inicial al proveedor'),
        (NOW() - INTERVAL '12 days', 'Purchase', 2, 200,   12.00,  2400.00, 'Compra por volumen'),
        (NOW() - INTERVAL '15 days', 'Sale',     1,   5, 1250.00,  6250.00, 'Venta a cliente corporativo'),
        (NOW() - INTERVAL '9 days',  'Sale',     2,  50,   18.50,   925.00, 'Venta mayorista'),
        (NOW() - INTERVAL '7 days',  'Purchase', 5,  70,   95.00,  6650.00, 'Reposición de inventario'),
        (NOW() - INTERVAL '3 days',  'Sale',     5,  10,  129.90,  1299.00, 'Venta en tienda'),
        (NOW() - INTERVAL '5 days',  'Purchase', 8, 220,   20.00,  4400.00, 'Compra de cargadores'),
        (NOW() - INTERVAL '1 day',   'Sale',     8,  20,   29.90,   598.00, 'Venta online');
    END IF;
END $$;
