using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticoN5ServicioApiRest.Data;
using PracticoN5ServicioApiRest.Services;

namespace PracticoN5ServicioApiRest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly ProductosService _servicio;
        public ProductosController(ProductosService servicio)
        {
            _servicio = servicio;
        }
        [HttpGet]
        public async Task<IActionResult> Index() 
        {
            return Ok(await _servicio.GetProductosAsync());
        }


    }
}