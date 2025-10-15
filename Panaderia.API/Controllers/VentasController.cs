using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Panaderia.API.Data;
using Panaderia.API.Models;
using System.Security.Claims;

namespace Panaderia.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Todos los endpoints requieren autenticación
    public class VentasController : ControllerBase
    {
        private readonly PanaderiaContext _context;

        public VentasController(PanaderiaContext context)
        {
            _context = context;
        }

        // ========================================
        // GET: api/Ventas
        // ========================================
        [HttpGet]
        [Authorize(Roles = "Admin,Vendedor")]
        public async Task<ActionResult<IEnumerable<Venta>>> GetVentas()
        {
            var ventas = await _context.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            return Ok(ventas);
        }

        // ========================================
        // GET: api/Ventas/5
        // ========================================
        [HttpGet("{id}")]
        public async Task<ActionResult<Venta>> GetVenta(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
                return NotFound(new { error = "Venta no encontrada" });

            return Ok(venta);
        }

        // ========================================
        // POST: api/Ventas
        // ✅ CORREGIDO: Obtiene usuario del token JWT
        // ========================================
        [HttpPost]
        [Authorize(Roles = "Admin,Vendedor")]
        public async Task<ActionResult<Venta>> PostVenta(VentaRequest request)
        {
            // ✅ OBTENER ID DEL USUARIO DEL TOKEN JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { error = "Token inválido" });
            }

            int usuarioId = int.Parse(userIdClaim);

            // Validar que hay productos
            if (request.Detalles == null || !request.Detalles.Any())
                return BadRequest(new { error = "La venta debe tener al menos un producto" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var venta = new Venta
                {
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId, // ✅ Usa el ID del token, no del request
                    Total = 0,
                    DetallesVenta = new List<DetalleVenta>()
                };

                decimal total = 0;

                foreach (var detalle in request.Detalles)
                {
                    // Validar cantidad
                    if (detalle.Cantidad <= 0)
                        return BadRequest(new { error = "La cantidad debe ser mayor a 0" });

                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    if (producto == null)
                        return BadRequest(new { error = $"Producto con ID {detalle.ProductoId} no encontrado" });

                    if (producto.Stock < detalle.Cantidad)
                        return BadRequest(new
                        {
                            error = $"Stock insuficiente para {producto.Nombre}",
                            disponible = producto.Stock,
                            solicitado = detalle.Cantidad
                        });

                    // ✅ Actualizar stock
                    producto.Stock -= detalle.Cantidad;

                    // Crear detalle de venta
                    var detalleVenta = new DetalleVenta
                    {
                        ProductoId = detalle.ProductoId,
                        Cantidad = detalle.Cantidad,
                        Precio = producto.Precio // Precio al momento de la venta
                    };

                    venta.DetallesVenta.Add(detalleVenta);
                    total += producto.Precio * detalle.Cantidad;
                }

                venta.Total = total;
                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Cargar relaciones para respuesta
                var ventaConDetalle = await _context.Ventas
                    .Include(v => v.Usuario)
                    .Include(v => v.DetallesVenta)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(v => v.Id == venta.Id);

                return CreatedAtAction(nameof(GetVenta), new { id = venta.Id }, ventaConDetalle);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Error al crear la venta", detalle = ex.Message });
            }
        }

        // ========================================
        // GET: api/Ventas/MisVentas
        // Obtener ventas del usuario autenticado
        // ========================================
        [HttpGet("MisVentas")]
        [Authorize(Roles = "Vendedor,Admin")]
        public async Task<ActionResult<IEnumerable<Venta>>> GetMisVentas()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim);

            var ventas = await _context.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .Where(v => v.UsuarioId == userId)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            return Ok(ventas);
        }

        // ========================================
        // DELETE: api/Ventas/5
        // Solo Admin puede eliminar
        // ========================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVenta(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.DetallesVenta)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
                return NotFound(new { error = "Venta no encontrada" });

            // Restaurar stock
            foreach (var detalle in venta.DetallesVenta)
            {
                var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                if (producto != null)
                {
                    producto.Stock += detalle.Cantidad;
                }
            }

            _context.Ventas.Remove(venta);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Venta eliminada correctamente" });
        }
    }

    // ========================================
    // DTOs - UsuarioId YA NO SE ENVÍA
    // ========================================
    public class VentaRequest
    {
        // ❌ ELIMINADO: public int UsuarioId { get; set; }
        public List<DetalleVentaRequest> Detalles { get; set; } = new();
    }

    public class DetalleVentaRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}