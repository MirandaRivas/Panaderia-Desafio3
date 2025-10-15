using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Panaderia.API.Data;
using Panaderia.API.Models;

namespace Panaderia.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly PanaderiaContext _context;

        public ProductosController(PanaderiaContext context)
        {
            _context = context;
        }

        // ========================================
        // GET: api/Productos
        // Público - Cualquiera puede ver productos
        // ========================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _context.Productos
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToListAsync();
        }

        // ========================================
        // GET: api/Productos/5
        // Público - Ver detalle de producto
        // ========================================
        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound(new { error = "Producto no encontrado" });
            }

            return Ok(producto);
        }

        // ========================================
        // GET: api/Productos/Categoria/{categoria}
        // Filtrar por categoría
        // ========================================
        [HttpGet("Categoria/{categoria}")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductosPorCategoria(string categoria)
        {
            var productos = await _context.Productos
                .Where(p => p.Categoria.ToLower() == categoria.ToLower())
                .ToListAsync();

            return Ok(productos);
        }

        // ========================================
        // POST: api/Productos
        // ✅ SOLO ADMIN puede crear productos
        // ========================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Producto>> PostProducto(Producto producto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validaciones adicionales
            if (producto.Precio <= 0)
            {
                return BadRequest(new { error = "El precio debe ser mayor a 0" });
            }

            if (producto.Stock < 0)
            {
                return BadRequest(new { error = "El stock no puede ser negativo" });
            }

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, producto);
        }

        // ========================================
        // PUT: api/Productos/5
        // ✅ SOLO ADMIN puede editar productos
        // ========================================
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutProducto(int id, Producto producto)
        {
            if (id != producto.Id)
            {
                return BadRequest(new { error = "El ID del producto no coincide" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validaciones adicionales
            if (producto.Precio <= 0)
            {
                return BadRequest(new { error = "El precio debe ser mayor a 0" });
            }

            if (producto.Stock < 0)
            {
                return BadRequest(new { error = "El stock no puede ser negativo" });
            }

            _context.Entry(producto).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(id))
                {
                    return NotFound(new { error = "Producto no encontrado" });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // ========================================
        // PATCH: api/Productos/5/Stock
        // Actualizar solo el stock (usado internamente por ventas)
        // ========================================
        [HttpPatch("{id}/Stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateRequest request)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound(new { error = "Producto no encontrado" });
            }

            if (request.NuevoStock < 0)
            {
                return BadRequest(new { error = "El stock no puede ser negativo" });
            }

            producto.Stock = request.NuevoStock;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Stock actualizado correctamente", producto });
        }

        // ========================================
        // DELETE: api/Productos/5
        // ✅ SOLO ADMIN puede eliminar productos
        // ========================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound(new { error = "Producto no encontrado" });
            }

            // Verificar si el producto tiene ventas
            var tieneVentas = await _context.DetallesVenta
                .AnyAsync(d => d.ProductoId == id);

            if (tieneVentas)
            {
                return BadRequest(new
                {
                    error = "No se puede eliminar el producto porque tiene ventas registradas",
                    sugerencia = "Considere desactivar el producto en lugar de eliminarlo"
                });
            }

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Producto eliminado correctamente" });
        }

        // ========================================
        // GET: api/Productos/Categorias
        // Obtener lista de categorías únicas
        // ========================================
        [HttpGet("Categorias")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategorias()
        {
            var categorias = await _context.Productos
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categorias);
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }

    // ========================================
    // DTO para actualizar stock
    // ========================================
    public class StockUpdateRequest
    {
        public int NuevoStock { get; set; }
    }
}