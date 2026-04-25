using System.Net.Http.Json;
using System.Text.Json;

namespace FrontBlazor_AppiGenericaCsharp.Services
{
    // Servicio generico que consume la API REST para cualquier tabla.
    // Se inyecta en las paginas Blazor con @inject ApiService Api
    public class ApiService
     {
        // HttpClient configurado en Program.cs con la URL base de la API
        private readonly HttpClient _http;

        // Opciones para deserializar JSON sin distinguir mayusculas/minusculas
        // La API devuelve "datos", "estado", etc. en minuscula
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // El constructor recibe el HttpClient inyectado por DI
        public ApiService(HttpClient http)
        {
            _http = http;
        }

        // ──────────────────────────────────────────────
        // LISTAR: GET /api/{tabla}
        // Devuelve la lista de registros como diccionarios
        // ──────────────────────────────────────────────
        public async Task<List<Dictionary<string, object?>>> ListarAsync(string tabla)
        {
            try
            {
                                var respuesta = await _http.GetAsync($"/api/{tabla}");

                                                if (!respuesta.IsSuccessStatusCode)
                {
                    return new List<Dictionary<string, object?>>();
                }

                string cuerpo = await respuesta.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(cuerpo))
                {
                    // Caso tipico: API responde 200/204 sin contenido cuando no hay datos
                    return new List<Dictionary<string, object?>>();
                }

                if (!TryParseJson(cuerpo, out JsonElement json))
                {
                    return new List<Dictionary<string, object?>>();
                }

                // Soporta tanto { datos: [...] } como [...]
                if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("datos", out JsonElement datos))
                {
                    return ConvertirDatos(datos);
                }

                if (json.ValueKind == JsonValueKind.Array)
                {
                    return ConvertirDatos(json);
                }

                return new List<Dictionary<string, object?>>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al listar {tabla}: {ex.Message}");
                return new List<Dictionary<string, object?>>();
            }
        }

        // ──────────────────────────────────────────────
        // CREAR: POST /api/{tabla}
        // Envia los datos del formulario como JSON
        // Devuelve (exito, mensaje) para mostrar al usuario
        // ──────────────────────────────────────────────
        public async Task<(bool exito, string mensaje)> CrearAsync(
            string tabla, Dictionary<string, object?> datos)
        {
            try
            {
                var respuesta = await _http.PostAsJsonAsync($"/api/{tabla}", datos);
                                string mensaje = await ObtenerMensajeAsync(respuesta);
                return (respuesta.IsSuccessStatusCode, mensaje);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de conexion: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────
        // ACTUALIZAR: PUT /api/{tabla}/{clave}/{valor}
        // Envia los campos a modificar como JSON
        // ──────────────────────────────────────────────
        public async Task<(bool exito, string mensaje)> ActualizarAsync(
            string tabla, string nombreClave, string valorClave,
            Dictionary<string, object?> datos)
        {
            try
            {
                var respuesta = await _http.PutAsJsonAsync(
                    $"/api/{tabla}/{nombreClave}/{valorClave}", datos);
                                    string mensaje = await ObtenerMensajeAsync(respuesta);
                return (respuesta.IsSuccessStatusCode, mensaje);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de conexion: {ex.Message}");
            }
        }

                public async Task<(bool exito, string mensaje)> ActualizarAsync(
            string tabla, Dictionary<string, string> claves,
            Dictionary<string, object?> datos)
        {
            var ruta = ConstruirRutaConClaves(tabla, claves);

            try
            {
                var respuesta = await _http.PutAsJsonAsync(ruta, datos);
                string mensaje = await ObtenerMensajeAsync(respuesta);
                return (respuesta.IsSuccessStatusCode, mensaje);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de conexion: {ex.Message}");
            }
        }


        // ──────────────────────────────────────────────
        // ELIMINAR: DELETE /api/{tabla}/{clave}/{valor}
        // Solo necesita la clave primaria para identificar el registro
        // ──────────────────────────────────────────────
        public async Task<(bool exito, string mensaje)> EliminarAsync(
            string tabla, string nombreClave, string valorClave)
        {
            try
            {
                var respuesta = await _http.DeleteAsync(
                    $"/api/{tabla}/{nombreClave}/{valorClave}");
                                    string mensaje = await ObtenerMensajeAsync(respuesta);
                return (respuesta.IsSuccessStatusCode, mensaje);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de conexion: {ex.Message}");
            }
        }

                public async Task<(bool exito, string mensaje)> EliminarAsync(
            string tabla, Dictionary<string, string> claves)
        {
            var ruta = ConstruirRutaConClaves(tabla, claves);

            try
            {
                var respuesta = await _http.DeleteAsync(ruta);
                string mensaje = await ObtenerMensajeAsync(respuesta);
                return (respuesta.IsSuccessStatusCode, mensaje);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de conexion: {ex.Message}");
            }
        }


        // ──────────────────────────────────────────────
        // METODO AUXILIAR: Convierte JsonElement a lista de diccionarios
                // ──────────────────────────────────────────────
        private List<Dictionary<string, object?>> ConvertirDatos(JsonElement datos)
        {
            var lista = new List<Dictionary<string, object?>>();

            if (datos.ValueKind != JsonValueKind.Array)
            {
                return lista;
            }

            foreach (var fila in datos.EnumerateArray())
            {
                if (fila.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var diccionario = new Dictionary<string, object?>();

                foreach (var propiedad in fila.EnumerateObject())
                {
                                        diccionario[propiedad.Name] = propiedad.Value.ValueKind switch
                    {
                        JsonValueKind.String => propiedad.Value.GetString(),
                        JsonValueKind.Number => propiedad.Value.TryGetInt32(out int i) ? i : propiedad.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => propiedad.Value.GetRawText()
                    };
                }

                lista.Add(diccionario);
            }

            return lista;
        }

        // ──────────────────────────────────────────────
        // METODOS AUXILIARES: lectura segura de mensajes de API
        // ──────────────────────────────────────────────
        private async Task<string> ObtenerMensajeAsync(HttpResponseMessage respuesta)
        {
            string cuerpo = await respuesta.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(cuerpo))
            {
                return respuesta.IsSuccessStatusCode
                    ? "Operacion completada."
                    : $"Error HTTP {(int)respuesta.StatusCode}.";
            }

            if (TryParseJson(cuerpo, out JsonElement json))
            {
                if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("mensaje", out JsonElement msg))
                {
                    return msg.GetString() ?? "Operacion completada.";
                }

                return respuesta.IsSuccessStatusCode
                    ? "Operacion completada."
                    : $"Error HTTP {(int)respuesta.StatusCode}.";
            }

            // Si no viene JSON, devolvemos texto plano para poder mostrarlo en UI
            return cuerpo;
        }

        private static string ConstruirRutaConClaves(string tabla, Dictionary<string, string> claves)
        {
            var segmentos = new List<string> { $"/api/{tabla}" };

            foreach (var clave in claves)
            {
                segmentos.Add(Uri.EscapeDataString(clave.Key));
                segmentos.Add(Uri.EscapeDataString(clave.Value));
            }

            return string.Join("/", segmentos).Replace("//", "/");
        }

        private bool TryParseJson(string contenido, out JsonElement json)
        {
            try
            {
                using var doc = JsonDocument.Parse(contenido);
                json = doc.RootElement.Clone();
                return true;
            }
            catch (JsonException)
            {
                json = default;
                return false;
            }
        }
    }
}
