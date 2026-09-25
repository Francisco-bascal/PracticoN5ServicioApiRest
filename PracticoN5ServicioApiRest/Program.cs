using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.Models;
using PracticoN5ServicioApiRest.Services;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// IgnoreCycles evita errores de serialización JSON por referencias circulares entre navegaciones.
builder.Services.AddControllers().AddJsonOptions(options =>
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

// Registrar servicios de la capa de negocio
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
            // Deshabilitada: la API no tiene una audiencia fija (no hay frontend).
            ValidateAudience = false, //no usamos frontend
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
                    mensaje = "Debe iniciar sesi�n para acceder a este recurso."
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

//Usa swagger independientemente de si es entorno de desarrollo o de producci�n
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Aplica la política de CORS a todo el pipeline. Va antes de la autenticación para que las respuestas
// de CORS lleguen aunque la petición luego sea rechazada por auth.
app.UseCors("Permissive");

// Orden obligatorio: UseAuthentication valida el token y construye la identidad del usuario;
// UseAuthorization evalúa los [Authorize] en base a esa identidad. Deben ir en ese orden.
app.UseAuthentication();
app.UseAuthorization();

//Para que redireccione a swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

app.Run();