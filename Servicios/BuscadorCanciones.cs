using System.Text.Json;

namespace SecretParty.Servicios
{
    // Una canción tal como la mostramos en la app
    public record Cancion(string Titulo, string Artista, string Portada, string? Preview);

    // Busca canciones en la API pública de iTunes (de Apple).
    //   - Es GRATIS y NO pide clave ni registro.
    //   - Por cada canción devuelve: nombre, artista, portada del disco y un preview de 30 segundos.
    //   Ej: https://itunes.apple.com/search?term=miel&media=music&entity=song&limit=6
    //
    // ¿Por qué la llamamos desde el SERVIDOR y no desde el JavaScript?
    //   1. Así podemos usar Retry + Fallback + trazas (Resiliencia), igual que el resto de la app.
    //   2. El navegador solo habla con NUESTRO servidor (no depende de que Apple permita CORS).
    public class BuscadorCanciones
    {
        private readonly HttpClient _http;
        private readonly Resiliencia _resiliencia;

        public BuscadorCanciones(HttpClient http, Resiliencia resiliencia)
        {
            _http = http;
            _resiliencia = resiliencia;
        }

        public async Task<List<Cancion>> Buscar(string texto)
        {
            var url = "https://itunes.apple.com/search?media=music&entity=song&limit=6&country=EC&term="
                      + Uri.EscapeDataString(texto);

            // RETRY: hasta 3 intentos a iTunes. FALLBACK: si no responde, lista vacía (la pantalla avisa).
            return await _resiliencia.EjecutarAsync("BuscarCancion",
                "iTunes", async () => await PedirAItunes(url),
                "SinResultados", () => Task.FromResult(new List<Cancion>()));
        }

        private async Task<List<Cancion>> PedirAItunes(string url)
        {
            using var respuesta = await _http.GetAsync(url);
            respuesta.EnsureSuccessStatusCode(); // si Apple responde error, esto lanza excepción -> reintento

            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStreamAsync());
            var canciones = new List<Cancion>();

            foreach (var r in json.RootElement.GetProperty("results").EnumerateArray())
            {
                var titulo = Texto(r, "trackName");
                var artista = Texto(r, "artistName");
                var portada = Texto(r, "artworkUrl100");
                if (titulo == null || artista == null || portada == null) continue;

                // La portada viene de 100x100; pidiendo 300x300 se ve nítida
                portada = portada.Replace("100x100bb", "300x300bb");
                canciones.Add(new Cancion(titulo, artista, portada, Texto(r, "previewUrl")));
            }
            return canciones;
        }

        private static string? Texto(JsonElement e, string campo) =>
            e.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        // SEGURIDAD: la portada y el preview llegan desde el formulario (campos escondidos),
        // así que alguien podría cambiarlos por CUALQUIER link. Solo aceptamos links https de Apple.
        public static bool EsLinkDeApple(string? link)
        {
            if (string.IsNullOrEmpty(link)) return true; // vacío está bien (sin preview)
            return Uri.TryCreate(link, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && (uri.Host.EndsWith(".mzstatic.com") || uri.Host.EndsWith(".apple.com"));
        }
    }
}
