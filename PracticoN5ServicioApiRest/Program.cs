using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.Infraestructura;
using PracticoN5ServicioApiRest.Services;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// IgnoreCycles evita errores de serialización JSON por referencias circulares entre navegaciones.
builder.Services.AddControllers(options =>
{
    // Auditoría automática de las acciones que modifican datos (ver AuditoriaFilter).
    options.Filters.Add<AuditoriaFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Swagger con esquema Bearer: habilita un botón "Authorize" para enviar el JWT en cada petición.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el JWT obtenido mediante el login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("Database connection not found");
builder.Services.AddDbContext<SistemaVentasDbContext>(options => options.UseSqlServer(connectionString));

// Permite a los servicios conocer al usuario autenticado de la petición actual (UsuarioActual).
builder.Services.AddHttpContextAccessor();

// Registrar servicios de la capa de negocio
builder.Services.AddScoped<UsuarioActual>();
builder.Services.AddSingleton<ZonaHorariaNegocio>();
builder.Services.AddScoped<CategoriasService>();
builder.Services.AddScoped<ProductosService>();
builder.Services.AddScoped<ClientesService>();
builder.Services.AddScoped<ProveedoresService>();
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddScoped<ComprasService>();
builder.Services.AddScoped<DetallesCompraService>();
builder.Services.AddScoped<VentasService>();
builder.Services.AddScoped<DetallesVentaService>();
builder.Services.AddScoped<ImagenesService>();
builder.Services.AddScoped<MovimientosStockService>();
builder.Services.AddScoped<MediosPagoService>();
builder.Services.AddSingleton<PasarelaPagoSimulada>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<ErrorLogService>();
builder.Services.AddScoped<ReportesService>();
builder.Services.AddSingleton<ReportesPdfService>();
builder.Services.AddScoped<EmailService>();

// CORS permisivo: acepta cualquier origen, header y verbo HTTP.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Permissive", policy =>
    {
        // Cualquier origen (scheme + dominio + puerto).
        policy
            .AllowAnyOrigin()
            // Cualquier header, incluido Authorization (necesario para enviar el JWT).
            .AllowAnyHeader()
            // Todos los verbos HTTP (GET, POST, PUT, PATCH, DELETE, etc.).
            .AllowAnyMethod();
    });
});

// Límite de intentos de inicio de sesión por dirección IP (frena los ataques de fuerza bruta sobre las contraseñas).
int intentosLoginPorMinuto = builder.Configuration.GetValue<int?>("RateLimit:LoginPorMinuto") ?? 20;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = intentosLoginPorMinuto,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (contexto, cancellationToken) =>
    {
        await contexto.HttpContext.Response.WriteAsJsonAsync(
            new { mensaje = "Demasiados intentos de inicio de sesión. Espere un minuto e intente nuevamente." },
            cancellationToken);
    };
});

// Autenticación/autorización con JWT Bearer.
// Autenticación: valida QUIÉN es el cliente. Autorización: QUÉ puede hacer.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Reglas de validación que aplica el middleware a cada token recibido.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // El token debe ser emitido por nuestro emisor (Jwt:Issuer).
            ValidateIssuer = true,
            // Deshabilitada: la API no tiene una audiencia fija (la consumen la app móvil y Swagger por igual).
            ValidateAudience = false,
            // El token no debe estar expirado.
            ValidateLifetime = true,
            // La firma debe poder verificarse con la clave conocida (Jwt:Key).
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            // Misma clave con la que se firma en UsuariosService.LoginAsync.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!))
        };

        // Respuestas JSON en español en lugar de los mensajes genéricos de ASP.NET Core.
        options.Events = new JwtBearerEvents
        {
            // Autenticación fallida: token faltante, inválido o expirado en un [Authorize].
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(new
                {
                    mensaje = "Debe iniciar sesión para acceder a este recurso."
                });
            },

            // Autenticado pero sin permisos para la acción solicitada.
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new
                {
                    mensaje = "No tiene permisos para acceder a este recurso."
                });
            }
        };
    });

// Habilita la interpretación de [Authorize] y las políticas de los claims del token.
builder.Services.AddAuthorization();

var app = builder.Build();

//Usa swagger independientemente de si es entorno de desarrollo o de producción
app.UseSwagger();
app.UseSwaggerUI();

// Fuerza el redireccionamiento a HTTPS. En desarrollo se omite: el emulador/dispositivo Android llega por HTTP
// (http://10.0.2.2:5276) y una redirección a https://localhost lo dejaría sin conexión.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Aplica la política de CORS a todo el pipeline. Va antes de la autenticación para que las respuestas
// de CORS lleguen aunque la petición luego sea rechazada por auth.
app.UseCors("Permissive");

// Captura las excepciones no controladas, las guarda en el registro de errores y responde un 500 en JSON.
// Va después de CORS para que esa respuesta de error también lleve los headers de CORS.
app.UseMiddleware<ManejoErroresMiddleware>();

app.UseRateLimiter();

// Orden obligatorio: UseAuthentication valida el token y construye la identidad del usuario;
// UseAuthorization evalúa los [Authorize] en base a esa identidad. Deben ir en ese orden.
app.UseAuthentication();
app.UseAuthorization();

//Para que redireccione a swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

// Migraciones pendientes, administrador inicial y datos de ejemplo (según la configuración).
await DbInitializer.InicializarAsync(app.Services);

app.Run();

// Expone la clase Program a los tests de integración (WebApplicationFactory<Program>).
public partial class Program { }
