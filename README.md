# PracticoN5ServicioApiRest

API REST para la gestión de un sistema de ventas y compras creada con ASP.NET Core. Permite administrar categorías, productos, clientes, proveedores y usuarios, así como registrar y anular compras y ventas con control automático de stock. El acceso a los recursos está protegido mediante autenticación con tokens JWT y cuenta con documentación interactiva vía Swagger.

---

## Índice

1. [Stack tecnológico](#stack-tecnológico)
2. [Estructura del proyecto](#estructura-del-proyecto)
3. [Arquitectura](#arquitectura)
4. [Modelo de datos](#modelo-de-datos)
5. [Autenticación JWT](#autenticación-jwt)
6. [Endpoints de la API](#endpoints-de-la-api)
7. [Flujos de negocio clave](#flujos-de-negocio-clave)
8. [Paginación](#paginación)
9. [Manejo de errores](#manejo-de-errores)
10. [Configuración](#configuración)
11. [Cómo ejecutar el proyecto](#cómo-ejecutar-el-proyecto)
12. [Convenciones y deuda técnica conocida](#convenciones-y-deuda-técnica-conocida)

---

## Stack tecnológico

| Tecnología | Versión | Uso |
|---|---|---|
| .NET (ASP.NET Core) | 8.0 | Plataforma del Web API |
| Entity Framework Core | 8.0.30 | ORM y acceso a datos (SQL Server) |
| SQL Server | — | Base de datos relacional |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.30 | Autenticación y validación de tokens JWT |
| Swashbuckle.AspNetCore | 6.6.2 | Documentación interactiva de la API (Swagger/OpenAPI) |

---

## Estructura del proyecto

```
PracticoN5ServicioApiRest/
├── Controllers/          # Capa de presentación: expone los endpoints HTTP
├── Services/             # Capa de aplicación: lógica de negocio
├── Models/               # Entidades del dominio (objetos EF Core)
├── DTOs/                 # Objetos de transferencia de datos (entrada/salida)
├── Data/                 # DbContext y configuración de EF Core
├── Migrations/           # Migraciones de base de datos EF Core
├── Program.cs            # Punto de entrada: DI, autenticación, Swagger, pipeline
└── wwwroot/Uploads/      # Carpeta para archivos subidos (imágenes de productos)
```

---

## Arquitectura

El proyecto sigue una arquitectura en capas con separación clara de responsabilidades:

```
Controller → Service → DbContext (EF Core) → SQL Server
```

- **Controllers**: finos, resuelven cuestiones HTTP (rutas, verbos, códigos de estado) y delegan toda la lógica al servicio correspondiente.
- **Services**: concentran la lógica de negocio, las validaciones, las transacciones y el acceso a datos mediante el `DbContext`.
- **Models**: entidades EF Core que se persisten en la base de datos.
- **DTOs**: contratos de entrada/salida (usados en Productos, Compras, Ventas y Login).

### Convenciones de la capa de servicios

- Todos los servicios son clases concretas registradas como **scoped** en `Program.cs` e inyectadas por constructor (`_contexto`).
- Toda operación de base de datos es **asíncrona** (`async/await`) y propaga `CancellationToken`.
- Las lecturas usan `AsNoTracking()`, con las excepciones del flujo de login (necesita la entidad rastreada para verificar el hash) y de las operaciones de escritura.
- Las operaciones que afectan stock se ejecutan **dentro de transacciones SQL** con rollback ante cualquier error (`ComprasService` y `VentasService`).
- Las validaciones lanzan excepciones (véase [Manejo de errores](#manejo-de-errores)) que los controllers convierten en códigos HTTP.

---

## Modelo de datos

Nueve entidades en `Models/`. Todas usan claves primarias `int` con `[Key]`. El `DbContext` (`SistemaVentasDbContext`) define todas las relaciones con `DeleteBehavior.Restrict`.

### Entidades maestras

| Entidad | Propiedades clave | Validaciones |
|---|---|---|
| `CategoriaProducto` | `CategoriaId`, `Nombre`, `Descripcion` | `Nombre` requerido, longitud 3-50; `Descripcion` máx. 500 |
| `Producto` | `ProductoId`, `Nombre`, `Descripcion`, `Precio`, `Stock`, `ImagenRuta`, `CategoriaId` | `Nombre` requerido 2-100; `Precio`/`Stock` no negativos; `Precio` precisión (18,2) |
| `Cliente` | `ClienteId`, `Nombre`, `Apellido`, `Telefono`, `Email`, `Direccion` | `Nombre` requerido 2-100; `Email` con formato válido |
| `Proveedor` | `ProveedorId`, `Nombre`, `Telefono`, `Email`, `Direccion` | `Nombre` requerido 2-100; `Email` con formato válido |
| `Usuario` | `UsuarioId`, `NombreUsuario`, `Email`, `PasswordHash`, `Rol` | `NombreUsuario` 4-50; `PasswordHash` y `Rol` requeridos |

### Compras y ventas

| Entidad | Propiedades clave | Relaciones |
|---|---|---|
| `Compra` | `CompraId`, `Fecha`, `ProveedorId` | N:1 con `Proveedor`; 1:N con `DetalleCompra` |
| `DetalleCompra` | `DetalleCompraId`, `Cantidad`, `PrecioUnitario`, `CompraId`, `ProductoId` | N:1 con `Compra` y `Producto` |
| `Venta` | `VentaId`, `Fecha`, `ClienteId` | N:1 con `Cliente`; 1:N con `DetalleVenta` |
| `DetalleVenta` | `DetalleVentaId`, `Cantidad`, `PrecioUnitario`, `VentaId`, `ProductoId` | N:1 con `Venta` y `Producto` |

### Relaciones (1:N)

```
CategoriaProducto 1──N Producto
Proveedor         1──N Compra 1──N DetalleCompra N──1 Producto
Cliente           1──N Venta  1──N DetalleVenta  N──1 Producto
```

- Categoría → Productos, Proveedor → Compras y Cliente → Ventas son relaciones 1:N.
- `DetalleCompra` y `DetalleVenta` son las líneas de cada documento; apuntan a `Producto` y al documento padre definiendo la relación N:1.
- Todos los FKs usan `DeleteBehavior.Restrict`; por eso los servicios validan manualmente antes de eliminar (por ejemplo, no se elimina una categoría con productos o un producto con historial en detalles).

---

## Autenticación JWT

La API usa tokens JWT Bearer. La configuración se encuentra en `Program.cs` y la emisión de tokens en `UsuariosService`.

### Configuración (`appsettings.*.json`)

| Clave | Descripción |
|---|---|
| `Jwt:Key` | Clave simétrica de firma (usada para firmar y validar). **No debe exponerse.** |
| `Jwt:Issuer` | Emisor válido del token. |
| `Jwt:Audience` | Se lee en `Program.cs`, pero no está definida en los appsettings; su validación está deshabilitada (`ValidateAudience = false`). |

Parámetros de validación: emisor validado, firma validada, vida útil validada, audiencia no validada (por no usar frontend).

### Flujo de login

1. `POST api/Usuarios/login` recibe `LoginRequestDTO` (`NombreUsuario`, `Password`).
2. `UsuariosService.LoginAsync` busca el usuario por nombre (insensible a mayúsculas). Si no existe → `null` → `401`.
3. La contraseña se verifica con `PasswordHasher<Usuario>.VerifyHashedPassword`. Si falla → `null` → `401`.
4. Si es válida, genera un token firmado con `HmacSha256`, expiración de **1 hora** (`DateTime.UtcNow.AddHours(1)`) y los claims:
   - `NameIdentifier` → `UsuarioId`
   - `Name` → `NombreUsuario`
   - `Role` → `Rol`
5. Devuelve `LoginResponseDTO` con el `Token`.

### Respuestas de autorización personalizadas

- **401** (`OnChallenge`): `{ "mensaje": "Debe iniciar sesión para acceder a este recurso." }`.
- **403** (`OnForbidden`): `{ "mensaje": "No tiene permisos para acceder a este recurso." }`.

### Cómo probar desde Swagger

1. `POST api/Usuarios/login` con un usuario existente.
2. Copiar el `token` de la respuesta.
3. Pulsar **Authorize** en Swagger e ingresar `Bearer <token>`.

---

## Endpoints de la API

Todos los controladores usan la ruta `api/[controller]` y requieren `[Authorize]`, salvo: `UsuariosController` (que mezcla `[AllowAnonymous]` en `test` y `login` con `[Authorize]` por acción) y `WeatherForecastController` (plantilla sin autorizar y sin prefijo `api/`).

### Categorías — `api/Categorias`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `api/Categorias` | Lista todas las categorías |
| GET | `api/Categorias/{id}?incluirProductos=false` | Obtiene una categoría; opcionalmente incluye sus productos |
| POST | `api/Categorias` | Crea una categoría |
| PUT | `api/Categorias/{id}` | Actualiza una categoría |
| DELETE | `api/Categorias/{id}` | Elimina una categoría (409 si tiene productos) |

### Productos — `api/Productos`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `api/Productos?pagina=1&tamanoPagina=10` | Lista paginada de productos |
| GET | `api/Productos/{id}` | Obtiene un producto |
| GET | `api/Productos/categoria/{categoriaId}` | Productos de una categoría |
| POST | `api/Productos` | Crea un producto (DTO `CreateProductoDTO` → responde `ResponseProductoDTO`) |
| PUT | `api/Productos/{id}` | Actualiza un producto |
| PATCH | `api/Productos/{id}/stock` | Ajusta el stock (400 si el resultado queda negativo) |
| DELETE | `api/Productos/{id}` | Elimina un producto (409 si tiene historial en compras/ventas) |

### Clientes — `api/Clientes`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `api/Clientes?pagina=1&tamanoPagina=10` | Lista paginada de clientes |
| GET | `api/Clientes/{id}?incluirVentas=false` | Obtiene un cliente; opcionalmente incluye sus ventas (con detalles y productos) |
| POST | `api/Clientes` | Crea un cliente |
| PUT | `api/Clientes/{id}` | Actualiza un cliente |
| DELETE | `api/Clientes/{id}` | Elimina un cliente (409 si tiene ventas) |

### Proveedores — `api/Proveedores`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `api/Proveedores?pagina=1&tamanoPagina=10` | Lista paginada de proveedores |
| GET | `api/Proveedores/{id}?incluirCompras=false` | Obtiene un proveedor; opcionalmente incluye sus compras |
| POST | `api/Proveedores` | Crea un proveedor |
| PUT | `api/Proveedores/{id}` | Actualiza un proveedor |
| DELETE | `api/Proveedores/{id}` | Elimina un proveedor (409 si tiene compras) |

### Compras — `api/Compras`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `api/Compras?pagina=1&tamanoPagina=10` | Lista paginada de compras |
| GET | `api/Compras/{id}` | Obtiene una compra con detalles y productos |
| GET | `api/Compras/proveedor/{proveedorId}` | Compras de un proveedor |
| POST | `api/Compras` | Registra una compra (DTO `CreateCompraDTO`); **incrementa stock** |
| POST | `api/Compras/{id}/anular` | Anula una compra (409 si el stock ya no alcanza); **decrementa stock** |

### Ventas — `api/Ventas`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `api/Ventas?pagina=1&tamanoPagina=10` | Lista paginada de ventas |
| GET | `api/Ventas/{id}` | Obtiene una venta con detalles y productos |
| GET | `api/Ventas/cliente/{clienteId}` | Ventas de un cliente |
| POST | `api/Ventas` | Registra una venta (DTO `CreateVentaDTO`); **decrementa stock** |
| POST | `api/Ventas/{id}/anular` | Anula una venta; **reincorpora stock** |

### Detalles — `api/DetallesCompra` y `api/DetallesVenta`

Solo lectura (`[Authorize]`, sin mapeo de errores).

| Método | Ruta | Descripción |
|---|---|---|
| GET | `api/DetallesCompra/{id}` / `api/DetallesVenta/{id}` | Detalle por ID |
| GET | `api/DetallesCompra/compra/{compraId}` / `api/DetallesVenta/venta/{ventaId}` | Detalles de un documento |
| GET | `api/DetallesCompra/producto/{productoId}` / `api/DetallesVenta/producto/{productoId}` | Detalles de un producto |

### Usuarios — `api/Usuarios`

| Método | Ruta | Autorización | Descripción |
|---|---|---|---|
| GET | `api/Usuarios/test` | `[AllowAnonymous]` | Comprobación de que la API responde ("API funcionando") |
| POST | `api/Usuarios/login` | `[AllowAnonymous]` | Login, devuelve el JWT |
| GET | `api/Usuarios` | `[Authorize]` | Lista de usuarios |
| GET | `api/Usuarios/{id}` | `[Authorize]` | Usuario por ID |
| GET | `api/Usuarios/usuario/{nombreUsuario}` | `[Authorize]` | Usuario por nombre |
| POST | `api/Usuarios` | `[Authorize]` | Crea un usuario (la contraseña se guarda hasheada) |
| PUT | `api/Usuarios/{id}` | `[Authorize]` | Actualiza un usuario (rehashea solo si se envía nueva contraseña) |
| DELETE | `api/Usuarios/{id}` | `[Authorize]` | Elimina un usuario |

---

## Flujos de negocio clave

### Registro de compra (incremento de stock)

`ComprasService.RegistrarCompraAsync`:

1. Valida que el detalle tenga al menos una línea y que el proveedor exista.
2. Abre una transacción SQL.
3. Por cada línea: valida `Cantidad > 0` y `PrecioUnitario > 0`, busca el producto (404 si no existe) y ejecuta `producto.Stock += Cantidad`.
4. Construye la `Compra` con sus `DetalleCompra`, guarda y hace commit.
5. Después del commit, recarga el proveedor y los detalles con producto para la respuesta.
6. Cualquier error → rollback y re-lanzamiento.

### Anulación de compra (decremento de stock)

`ComprasService.AnularCompraAsync`:

1. Abre transacción y carga la compra con sus detalles.
2. Por cada detalle, recarga el producto y **valida que el stock actual sea suficiente** para revertir (`stock < Cantidad` → 409): si el stock comprado ya se vendió, no se puede anular.
3. `producto.Stock -= Cantidad`.
4. Elimina primero los detalles y luego la compra (por el `DeleteBehavior.Restrict`). Commit.

### Registro de venta (decremento de stock)

`VentasService.RegistrarVentaAsync`:

1. Valida que el detalle tenga al menos una línea y que el cliente exista.
2. Abre transacción.
3. Por cada línea: valida cantidades, **verifica stock suficiente** (`stock < Cantidad` → 409) y ejecuta `producto.Stock -= Cantidad`.
4. Guarda la `Venta` con sus `DetalleVenta`, commit, recarga datos relacionados para la respuesta.

### Anulación de venta (reincorporación de stock)

`VentasService.AnularVentaAsync`:

1. Abre transacción y carga la venta con sus detalles.
2. Por cada detalle recarga el producto y ejecuta `producto.Stock += Cantidad` (no requiere validación de suficiencia porque suma stock).
3. Elimina detalles y venta. Commit.

### Ajuste manual de stock

`ProductosService.ActualizarStockAsync` (PATCH `api/Productos/{id}/stock`) modifica el stock en forma directa; rechaza con `InvalidOperationException` si el resultado quedara negativo.

---

## Paginación

Los endpoints de listado (`Clientes`, `Productos`, `Proveedores`, `Compras`, `Ventas`) aceptan los parámetros de query:

- `pagina` (default `1`)
- `tamanoPagina` (default `10`, máximo `100`)

Devuelven `ResultadoPaginadoDto<T>`:

```json
{
  "paginaActual": 1,
  "tamanoPagina": 10,
  "totalElementos": 57,
  "totalPaginas": 6,
  "elementos": []
}
```

`Categorias` no usa paginación (devuelve la lista completa).

---

## Manejo de errores

Convención general: los servicios lanzan excepciones con mensajes en español y los controllers las convierten en códigos HTTP:

| Excepción | Código HTTP | Significado |
|---|---|---|
| `ArgumentException` | 400 BadRequest | Validación de entrada |
| `KeyNotFoundException` | 404 NotFound | Recurso no encontrado |
| `InvalidOperationException` | 409 Conflict | Regla de negocio: duplicados, referencias existentes, stock insuficiente |
| JWT fallido / credenciales inválidas | 401 Unauthorized | Autenticación |
| Sin permisos | 403 Forbidden | Autorización |

> **Nota conocida**: `Registrar` de Compras y Ventas mapea `InvalidOperationException` → **400**, mientras que el resto de los controllers la mapean → **409**. Es una inconsistencia intencionada o pendiente de unificar según el caso.

---

## Configuración

Se requiere en `appsettings.json` (o variante por entorno):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<cadena de conexión a SQL Server>"
  },
  "Jwt": {
    "Key": "<clave secreta de firma>",
    "Issuer": "MiWebApi"
  }
}
```

- **CORS**: política `"Permissive"` (`AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()`).
- **Swagger**: habilitado en todos los entornos, con esquema de seguridad Bearer. `GET /` redirige a `/swagger`.

---

## Cómo ejecutar el proyecto

1. Configurar `ConnectionStrings:DefaultConnection` y `Jwt:Key` en `appsettings.*.json`.
2. Aplicar la migración de la base de datos:

   ```bash
   dotnet ef database update
   ```

3. Ejecutar la API:

   ```bash
   dotnet run
   ```

4. Abrir Swagger en `https://localhost:<puerto>/swagger`.
5. Crear un usuario inicial (POST `api/Usuarios`) o usar uno existente, hacer login y obtener el token para autorizar las demás llamadas.

> La base de datos se crea con la migración inicial `MigracionInicial` (2026-09-04). No se ejecuta `EnsureCreated` ni se aplican migraciones automáticamente al arrancar.

---

## Convenciones y deuda técnica conocida

**Convenciones**

- Código en español; PascalCase en clases, métodos, propiedades y DTOs; camelCase en variables y parámetros; prefijo `_` en campos privados.
- Arquitectura estricta `Controller → Service → DbContext`.
- Sólo los flujos que tocan stock usan transacciones.
- Mapeo manual de DTOs (no se usa AutoMapper).

**Deuda técnica / pendientes detectados**

- Uso inconsistente de DTOs: `Productos`, `Compras`, `Ventas` y el login usan DTOs; `Categorias`, `Clientes`, `Proveedores` y `Usuarios` (alta/edición) enlazan la entidad de dominio directamente como body (riesgo de sobre-publicación y contrato acoplado a EF).
- `ResponseProductoDTO.Categoria` (string) está declarado pero nunca se rellena en `ProductosService`.
- `InvalidOperationException` mapea a 400 en unos controllers y a 409 en otros (ver [Manejo de errores](#manejo-de-errores)).
- `DetallesCompraController` y `DetallesVentaController` no capturan errores; las excepciones del servicio terminan como 500.
- Gran duplicación: el bloque de paginación está copiado en 5 servicios; `ComprasService`/`VentasService` son casi espejo; igual `DetallesCompraService`/`DetallesVentaService` y `ClientesService`/`ProveedoresService`.
- `WeatherForecastController` y `WeatherForecast` son restos de la plantilla (ruta sin `api/`, sincrónicos, anónimos).
- `CategoriasController` no pagina la lista, a diferencia del resto.
- `ProductosService.GetProductosAsync` es un método "compatibilidad" aparentemente sin uso real.
- Patrón N+1 en los bucles de registro/anulación de Compras y Ventas: los productos se recargan de a uno con `FirstOrDefaultAsync` en lugar de una consulta por lotes.
- `Program.cs` contiene literales con la codificación alterada (`sesi�n`, `producci�n`).
- Los secretos (`Jwt:Key`, contraseñas de base de datos) están versionados en los `appsettings.*.json`. Se recomienda rotarlos y moverlos a secretos de usuario o variables de entorno.