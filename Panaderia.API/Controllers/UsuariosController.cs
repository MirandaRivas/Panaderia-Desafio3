using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Panaderia.API.Data;
using Panaderia.API.Models;
using Panaderia.API.Services;

namespace Panaderia.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly PanaderiaContext _context;
        private readonly JwtService _jwtService;

        public UsuariosController(PanaderiaContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // ========================================
        // POST: api/Usuarios/login
        // ========================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email y contraseña son requeridos" });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);

            if (usuario == null)
            {
                return Unauthorized(new { message = "Credenciales inválidas" });
            }

            var token = _jwtService.GenerateToken(usuario);

            return Ok(new
            {
                token = token,
                usuario = new
                {
                    id = usuario.Id,
                    email = usuario.Email,
                    rol = usuario.Rol
                }
            });
        }

        // ========================================
        // GET: api/Usuarios
        // ========================================
        [HttpGet]
        [Authorize(Roles = "Admin")] // Solo Admin puede ver usuarios
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // ========================================
        // GET: api/Usuarios/5
        // ========================================
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }

        // ========================================
        // POST: api/Usuarios
        // ========================================
        [HttpPost]
        [Authorize(Roles = "Admin")] // Solo Admin puede crear usuarios
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
        }

        // ========================================
        // PUT: api/Usuarios/5
        // ========================================
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            if (id != usuario.Id)
            {
                return BadRequest();
            }

            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // ========================================
        // DELETE: api/Usuarios/5
        // ========================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }

    // ========================================
    // MODELO PARA LOGIN REQUEST
    // ========================================
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}