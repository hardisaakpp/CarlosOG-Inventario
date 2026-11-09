# Gestión de Inventarios

Aplicación web para controlar el inventario de una empresa. Permite registrar productos, registrar las compras y ventas que modifican su stock y consultar el historial de movimientos de cada producto.

En pocas palabras:

- Cada **compra** suma unidades al stock del producto y cada **venta** las descuenta.
- El sistema **no permite vender más unidades de las disponibles** y lo informa con un mensaje claro.
- Toda la información queda guardada en una base de datos, y las listas se pueden filtrar y paginar.

Tecnologías utilizadas:

- **Backend:** dos microservicios en .NET 10 (ASP.NET Core Web API) que se comunican de forma síncrona por REST.
- **Frontend:** Angular 21 con Angular Material.
- **Base de datos:** PostgreSQL.
- **Despliegue local:** Docker Compose (opcional).

## Estructura del repositorio

```
.
├── src/ProductService       Microservicio de productos (API REST)
├── src/TransactionService   Microservicio de transacciones (API REST)
├── src/frontend             Aplicación web en Angular
├── script.sql               Script de creación de la base de datos y datos de ejemplo
├── docker-compose.yml       Ejecución completa con un solo comando
└── capturas/                Capturas de pantalla usadas en este documento
```

## Arquitectura

```
                    ┌────────────────────┐
                    │  Frontend Angular  │  http://localhost:4200
                    └─────────┬──────────┘
              ┌───────────────┴────────────────┐
              ▼                                ▼
   ┌────────────────────┐   REST (síncrono)  ┌────────────────────┐
   │   ProductService   │◄───────────────────│ TransactionService │
   │  :5079  /api/products                   │  :5103  /api/transactions
   └─────────┬──────────┘                    └─────────┬──────────┘
             │ tabla Products                          │ tabla Transactions
             └────────────────┬────────────────────────┘
                              ▼
                  PostgreSQL · base "inventario"
          Transactions.ProductId  →  FK  →  Products.Id
```

- **ProductService** administra el catálogo (alta, edición, baja y consulta con filtros y paginación). Es el único servicio que modifica el stock, mediante un ajuste atómico.
- **TransactionService** registra las compras y ventas. Por cada operación consulta a ProductService por REST, valida el stock disponible y solicita el ajuste. Si después de ajustar el stock no logra guardar la transacción, revierte el ajuste para no dejar datos inconsistentes.
- Los servicios se comunican únicamente por API REST y cada uno accede solo a su propia tabla.

### Decisiones técnicas

- **Una sola base de datos con clave foránea.** Los dos servicios usan la base `inventario`, cada uno con su tabla. La relación `Transactions.ProductId → Products.Id` se garantiza con una clave foránea (`ON DELETE RESTRICT`), por lo que la base de datos impide eliminar un producto que ya tiene movimientos.
- **El stock solo cambia mediante transacciones.** Al editar un producto no se modifica su stock. Así se evita que una edición pise el stock actualizado por una venta que se registró mientras tanto.
- **Ajuste de stock atómico.** La condición `stock + ajuste >= 0` se evalúa dentro de la misma sentencia `UPDATE`, de modo que dos ventas simultáneas no pueden dejar el stock en negativo.
- **Interfaz en español**, con formato de fechas y moneda de Ecuador, y modo claro y oscuro (por defecto sigue el tema del sistema operativo).

## Requisitos

Para ejecutar el proyecto sin Docker se necesita:

| Herramienta | Versión |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 |
| [Node.js](https://nodejs.org/) | 20.19+, 22.12+ o 24+ (incluye npm) |
| [PostgreSQL](https://www.postgresql.org/download/) | 12 o superior (probado con 17) |

Con Docker no es necesario instalar nada de lo anterior: solo [Docker](https://docs.docker.com/get-docker/) con Docker Compose.

## Ejecución con Docker (opción rápida)

Desde la raíz del repositorio, un solo comando levanta PostgreSQL (con `script.sql` ya aplicado), los dos microservicios y el frontend:

```bash
docker compose up -d --build
```

| Servicio | URL |
|---|---|
| Frontend | http://localhost:4200 |
| ProductService (Swagger) | http://localhost:5079/swagger |
| TransactionService (Swagger) | http://localhost:5103/swagger |
| PostgreSQL | `localhost:5433` · base `inventario` · usuario `postgres` · contraseña `inventario` |

Consideraciones:

- La primera ejecución tarda algunos minutos porque descarga las imágenes base y compila los proyectos.
- Los puertos 4200, 5079 y 5103 deben estar libres. Si los servicios están corriendo de forma manual, hay que detenerlos antes.
- La contraseña de la base es `inventario` por defecto. Para cambiarla, copiar `.env.example` como `.env` y modificar `POSTGRES_PASSWORD`.
- `script.sql` solo se ejecuta cuando la base está vacía. Para volver a cargar los datos de ejemplo: `docker compose down -v` y luego `docker compose up -d --build`.
- Para detener todo: `docker compose down`.

Las secciones siguientes describen la ejecución sin Docker.

## Base de datos

El script [`script.sql`](script.sql) crea las tablas `Products` y `Transactions`, la clave foránea entre ambas y datos de ejemplo (12 productos y 8 transacciones). Se puede ejecutar varias veces sin duplicar datos.

La base de datos debe existir antes de ejecutarlo.

**Con PostgreSQL instalado:**

```bash
createdb -U postgres inventario
psql -U postgres -d inventario -f script.sql
```

**Con PostgreSQL en un contenedor Docker:**

```bash
docker run --name inventario-db -e POSTGRES_PASSWORD=TU_CLAVE -p 5432:5432 -d postgres:17
docker exec inventario-db psql -U postgres -c 'CREATE DATABASE inventario'
docker exec -i inventario-db psql -U postgres -d inventario < script.sql
```

## Ejecución del backend

### 1. Configurar la conexión a la base de datos

Los `appsettings.json` incluyen una clave de ejemplo (`TU_CLAVE`). Para no guardar contraseñas en el repositorio, la cadena de conexión se configura con *user secrets* (una vez por servicio):

```bash
dotnet user-secrets set "ConnectionStrings:inventario" "Host=localhost;Port=5432;Database=inventario;Username=postgres;Password=TU_CLAVE" --project src/ProductService
dotnet user-secrets set "ConnectionStrings:inventario" "Host=localhost;Port=5432;Database=inventario;Username=postgres;Password=TU_CLAVE" --project src/TransactionService
```

Reemplazar `TU_CLAVE` por la contraseña de PostgreSQL.

### 2. Levantar los servicios

Cada servicio en su propia terminal, desde la raíz del repositorio. **ProductService debe iniciarse primero**, porque TransactionService depende de él:

```bash
dotnet run --project src/ProductService       # http://localhost:5079
dotnet run --project src/TransactionService   # http://localhost:5103
```

Documentación interactiva de las APIs (Swagger):

- ProductService: http://localhost:5079/swagger
- TransactionService: http://localhost:5103/swagger

## Ejecución del frontend

```bash
cd src/frontend
npm install
npm start
```

La aplicación queda disponible en http://localhost:4200. Los dos servicios del backend deben estar en ejecución.

Las URLs de las APIs que consume el frontend están en [`src/frontend/src/app/core/api-urls.ts`](src/frontend/src/app/core/api-urls.ts) (puertos 5079 y 5103).

## Funcionalidades

- **Productos:** alta, edición, eliminación y consulta, con imagen, categoría, precio y stock.
  - Tabla paginada con filtros dinámicos por nombre, categoría, rango de precio y rango de stock.
  - La categoría se elige entre las existentes, con la opción de crear una nueva.
  - El stock se muestra con un indicador de color: verde (disponible), ámbar (bajo, 10 unidades o menos) y rojo (agotado).
- **Transacciones:** registro de compras y ventas, con edición y eliminación.
  - Tabla paginada con filtros dinámicos por producto, tipo de transacción y rango de fechas.
  - Al filtrar por un producto se muestra su nombre y su stock actual, lo que permite consultar el historial de cada producto.
- **Validaciones** en los formularios, con el mensaje de error bajo cada campo. El botón Guardar permanece deshabilitado mientras el formulario no sea válido.
- **Mensajes de éxito o error** en cada operación.

## Reglas de negocio

- Una **compra suma** stock y una **venta resta** stock, siempre de forma automática.
- No se puede vender más de lo que hay: si el stock es insuficiente, la transacción se rechaza con un mensaje claro.
- Al **editar o eliminar** una transacción, el stock se corrige. Si revertirla dejaría el stock en negativo, la operación se rechaza.
- El **stock inicial** se define al crear el producto. Al editarlo no se modifica: cambia únicamente mediante transacciones.
- **No se puede eliminar un producto que tenga transacciones.** La clave foránea lo impide y la API responde con un error 409.

## Endpoints

### ProductService (`/api/products`)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/products` | Lista paginada. Filtros: `name`, `category`, `minPrice`, `maxPrice`, `minStock`, `maxStock`, `page`, `pageSize` |
| GET | `/api/products/{id}` | Obtiene un producto |
| GET | `/api/products/by-ids?ids=1&ids=2` | Obtiene varios productos |
| GET | `/api/products/categories` | Lista las categorías existentes |
| POST | `/api/products` | Crea un producto |
| PUT | `/api/products/{id}` | Actualiza un producto (no modifica el stock) |
| DELETE | `/api/products/{id}` | Elimina un producto (409 si tiene transacciones) |
| POST | `/api/products/{id}/stock-adjustments` | Ajuste atómico de stock (lo usa TransactionService) |

### TransactionService (`/api/transactions`)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/transactions` | Historial paginado. Filtros: `productId`, `type` (`Purchase` o `Sale`), `from`, `to`, `page`, `pageSize` |
| GET | `/api/transactions/{id}` | Obtiene una transacción |
| POST | `/api/transactions` | Registra una compra o una venta |
| PUT | `/api/transactions/{id}` | Edita una transacción |
| DELETE | `/api/transactions/{id}` | Elimina una transacción y revierte su efecto en el stock |

## Evidencias

Capturas de la aplicación en ejecución, en modo claro.

### Listado de productos

Tabla con imagen, nombre, descripción, categoría, precio y stock. Cada fila incluye las acciones para editar y eliminar el producto.

![Listado de productos](capturas/productos-listado.png)

### Listado de transacciones

Historial de movimientos con fecha, producto, tipo (compra o venta), cantidad, precio unitario y total. Cada fila permite editar o eliminar la transacción.

![Listado de transacciones](capturas/transacciones-listado.png)

### Creación de productos

Formulario de alta. En la captura se muestra la validación del campo obligatorio "Nombre": mientras el formulario no sea válido, el botón Guardar permanece deshabilitado.

![Crear producto](capturas/producto-crear.png)

### Edición de productos

Formulario con los datos del producto cargados. El campo Stock aparece bloqueado porque solo se modifica mediante transacciones.

![Editar producto](capturas/producto-editar.png)

### Creación de transacciones

Formulario de registro de compra o venta: tipo, producto, cantidad, precio unitario y detalle. Si el tipo es venta, la cantidad no puede superar el stock disponible.

![Crear transacción](capturas/transaccion-crear.png)

### Edición de transacciones

Transacción existente con sus datos cargados y el total calculado automáticamente (cantidad por precio unitario).

![Editar transacción](capturas/transaccion-editar.png)

### Filtros dinámicos

Listado de productos filtrado por la categoría "Accesorios". Los resultados se actualizan al cambiar cualquiera de los filtros. En la parte inferior de la tabla se ve el paginador, con la cantidad de elementos por página y el rango mostrado. El producto sin unidades disponibles se marca con el indicador rojo.

![Filtros dinámicos](capturas/filtros.png)