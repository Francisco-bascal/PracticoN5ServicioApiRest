using PracticoN5ServicioApiRest.Models;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Permissive", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false, //no usamos frontend
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

//Usa swagger independientemente de si es entorno de desarrollo o de producción
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("Permissive");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();