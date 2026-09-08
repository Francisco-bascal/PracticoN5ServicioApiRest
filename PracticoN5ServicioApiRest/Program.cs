using PracticoN5ServicioApiRest.Models;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.Services;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
