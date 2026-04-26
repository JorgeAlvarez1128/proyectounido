using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace FrontBlazor_AppiGenericaCsharp.Services;

public sealed class AuthSessionService
{
    private readonly HttpClient _http;
    private readonly CustomAuthStateProvider _authStateProvider;

    public LoginResponse? SesionActual { get; private set; }

    public event Action? OnAuthChanged;

    public AuthSessionService(HttpClient http, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _authStateProvider = (CustomAuthStateProvider)authStateProvider;
    }

    public bool EstaAutenticado => !string.IsNullOrWhiteSpace(SesionActual?.Token);

    public async Task<(bool exito, string mensaje)> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/autenticacion/token", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(error))
            {
                try
                {
                    using var doc = JsonDocument.Parse(error);
                    if (doc.RootElement.TryGetProperty("mensaje", out var mensaje))
                    {
                        return (false, mensaje.GetString() ?? "No fue posible iniciar sesión.");
                    }
                }
                catch
                {
                    // Si no es JSON válido, se muestra el texto crudo de error
                }
            }

            return (false, string.IsNullOrWhiteSpace(error)
                ? "No fue posible iniciar sesión."
                : error);
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (login is null || string.IsNullOrWhiteSpace(login.Token))
        {
            return (false, "La API no devolvió un token válido.");
        }

        SesionActual = login;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        _authStateProvider.SetAuthenticatedUser(login.Token);
        OnAuthChanged?.Invoke();

        return (true, "Inicio de sesión exitoso.");
    }

    public void Logout()
    {
        SesionActual = null;
        _http.DefaultRequestHeaders.Authorization = null;
        _authStateProvider.SetAnonymous();
        OnAuthChanged?.Invoke();
    }

    public bool TieneRol(string rol)
        => SesionActual?.Roles.Any(r => r.Equals(rol, StringComparison.OrdinalIgnoreCase)) == true;

    public bool TieneRuta(string ruta)
        => SesionActual?.Rutas.Any(r => r.Equals(ruta, StringComparison.OrdinalIgnoreCase)) == true;

    public bool TienePermiso(string permiso)
        => SesionActual?.Permisos.Any(p => p.Equals(permiso, StringComparison.OrdinalIgnoreCase)) == true
           || TieneRol("Administrador");

    public bool PuedeCrear() => TienePermiso("crear");
    public bool PuedeActualizar() => TienePermiso("actualizar");
    public bool PuedeEliminar() => TienePermiso("eliminar");
}
