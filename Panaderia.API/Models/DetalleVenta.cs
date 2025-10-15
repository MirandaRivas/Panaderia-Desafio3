using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Panaderia.API.Models
{
    public class DetalleVenta
    {
        public int Id { get; set; }

        [Required]
        public int VentaId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "La cantidad debe ser entre 1 y 1000")]
        public int Cantidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; } // Precio unitario al momento de la venta

        // Propiedad calculada (opcional, no se guarda en BD)
        [NotMapped]
        public decimal Subtotal => Cantidad * Precio;

        // Relaciones
        public Venta? Venta { get; set; }
        public Producto? Producto { get; set; }
    }
}