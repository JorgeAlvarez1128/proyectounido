using ApiGenericaCsharp.Modelos;
using ApiGenericaCsharp.Servicios.Abstracciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiGenericaCsharp.Controllers
{
        [ApiController]
    [Route("api/[controller]")]
    public class AutenticacionController : ControllerBase
    {
                private readonly ConfiguracionJwt _configuracionJwt;
        private readonly IServicioCrud _servicioCrud;

        public AutenticacionController(
                        IOptions<ConfiguracionJwt> opcionesJwt,
            IServicioCrud servicioCrud)
        {
            _configuracionJwt = opcionesJwt.Value;
            _servicioCrud = servicioCrud;
        }

        [HttpPost("token")]
        public async Task<IActionResult> GenerarToken([FromBody] CredencialesGenericas credenciales)
        {
                        if (string.IsNullOrWhiteSpace(credenciales.Tabla) ||
                string.IsNullOrWhiteSpace(credenciales.CampoUsuario) ||
                string.IsNullOrWhiteSpace(credenciales.CampoContrasena) ||
                string.IsNullOrWhiteSpace(credenciales.Usuario) ||
                string.IsNullOrWhiteSpace(credenciales.Contrasena))
            {
                return BadRequest(new
                {
                    estado = 400,
                                        mensaje = "Debe enviar tabla, campos y credenciales completas."
                });
            }

            var (codigo, mensaje) = await _servicioCrud.VerificarContrasenaAsync(
                credenciales.Tabla,
                                null,
                credenciales.CampoUsuario,
                credenciales.CampoContrasena,
                credenciales.Usuario,
                credenciales.Contrasena
            );

            if (codigo == 404)
                return NotFound(new { estado = 404, mensaje = "Usuario no encontrado." });

            if (codigo == 401)
                return Unauthorized(new { estado = 401, mensaje = "Contraseña incorrecta." });

            if (codigo != 200)
                return StatusCode(500, new { estado = 500, mensaje = "Error interno durante la verificación.", detalle = mensaje });

                            var roles = await ObtenerRolesUsuarioAsync(credenciales.Usuario);
            var rutas = await ObtenerRutasPermitidasAsync(roles);
            var permisos = ObtenerPermisosPorRoles(roles);

            var claims = new List<Claim>
            {
                                new(ClaimTypes.Name, credenciales.Usuario),
                new("tabla", credenciales.Tabla),
                new("campoUsuario", credenciales.CampoUsuario)
            };

            claims.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol)));
            claims.AddRange(rutas.Select(ruta => new Claim("ruta", ruta)));
            claims.AddRange(permisos.Select(permiso => new Claim("permiso", permiso)));

                        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracionJwt.Key));
            var credencialesFirma = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
            var duracion = _configuracionJwt.DuracionMinutos > 0 ? _configuracionJwt.DuracionMinutos : 60;

            var token = new JwtSecurityToken(
                                issuer: _configuracionJwt.Issuer,
                audience: _configuracionJwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(duracion),
                signingCredentials: credencialesFirma
            );

            var tokenGenerado = new JwtSecurityTokenHandler().WriteToken(token);

                        return Ok(new
            {
                estado = 200,
                mensaje = "Autenticación exitosa.",
                usuario = credenciales.Usuario,
                roles,
                rutas,
                permisos,
                token = tokenGenerado,
                expiracion = token.ValidTo
            });
        }

        private async Task<List<string>> ObtenerRolesUsuarioAsync(string email)
        {
            var asignaciones = await _servicioCrud.ObtenerPorClaveAsync("rol_usuario", null, "fkemail", email);
            var idsRol = asignaciones
                .Where(x => x.ContainsKey("fkidrol"))
                .Select(x => x["fkidrol"]?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var roles = new List<string>();
            foreach (var id in idsRol)
            {
                var registros = await _servicioCrud.ObtenerPorClaveAsync("rol", null, "id", id!);
                var nombreRol = registros
                    .Select(r => r.ContainsKey("nombre") ? r["nombre"]?.ToString() : null)
                    .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

                if (!string.IsNullOrWhiteSpace(nombreRol))
                {
                    roles.Add(nombreRol!);
                }
            }

            if (roles.Count == 0)
            {
                roles.Add("Usuario");
            }

            return roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task<List<string>> ObtenerRutasPermitidasAsync(List<string> roles)
        {
            var rutas = new List<string>();

            foreach (var rol in roles)
            {
                var permisos = await _servicioCrud.ObtenerPorClaveAsync("rutarol", null, "rol", rol);
                rutas.AddRange(permisos
                    .Select(p => p.ContainsKey("ruta") ? p["ruta"]?.ToString() : null)
                    .Where(r => !string.IsNullOrWhiteSpace(r))!
                    .Select(r => r!));
            }

            return rutas.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> ObtenerPermisosPorRoles(List<string> roles)
        {
            var permisos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "consultar"
            };

            foreach (var rol in roles)
            {
                var normalizado = rol.Trim().ToLowerInvariant();

                if (normalizado.Contains("admin"))
                {
                    permisos.Add("crear");
                    permisos.Add("actualizar");
                    permisos.Add("eliminar");
                    continue;
                }

                if (normalizado.Contains("editor") || normalizado.Contains("gestor") || normalizado.Contains("modifica"))
                {
                    permisos.Add("actualizar");
                }
            }

            return permisos.ToList();
        }
    }

    public class CredencialesGenericas
    {
                public string Tabla { get; set; } = string.Empty;
                        public string CampoUsuario { get; set; } = string.Empty;
                                public string CampoContrasena { get; set; } = string.Empty;
                                        public string Usuario { get; set; } = string.Empty;
                                                public string Contrasena { get; set; } = string.Empty;
    }
}
