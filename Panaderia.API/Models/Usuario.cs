using System.ComponentModel.DataAnnotations;

namespace Panaderia.API.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "La contraseña debe tener entre 4 y 100 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        [StringLength(20)]
        public string Rol { get; set; } = "Vendedor"; // Valor por defecto

        // Relación: Un usuario puede tener muchas ventas
        public ICollection<Venta>? Ventas { get; set; }
    }
}