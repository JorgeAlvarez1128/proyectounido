using System.Security.Claims;
using System.Text.Json;

namespace FrontBlazor_AppiGenericaCsharp.Services;

public sealed class LoginRequest
{
    public string Tabla { get; set; } = "usuario";
    public string CampoUsuario { get; set; } = "email";
    public string CampoContrasena { get; set; } = "contrasena";
    public string Usuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public int Estado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime Expiracion { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Rutas { get; set; } = new();
    public List<string> Permisos { get; set; } = new();
}

public static class JwtClaimsParser
{
    public static List<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes)
                            ?? new Dictionary<string, JsonElement>();

        var claims = new List<Claim>();

        foreach (var kvp in keyValuePairs)
        {
            if (kvp.Key == ClaimTypes.Role || kvp.Key == "role")
            {
                if (kvp.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in kvp.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, kvp.Value.GetString() ?? string.Empty));
                }

                continue;
            }

            claims.Add(new Claim(kvp.Key, kvp.Value.ToString()));
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}