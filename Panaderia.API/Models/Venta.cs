using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Panaderia.API.Models
{
    public class Venta
    {
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now; // Fecha automática

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "El total debe ser mayor a 0")]
        public decimal Total { get; set; }

        [Required(ErrorMessage = "Debe especificar el usuario que realiza la venta")]
        public int UsuarioId { get; set; }

        // Relaciones
        public Usuario? Usuario { get; set; } // Navegación a Usuario

        public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>(); // Inicializar para evitar null
    }
}