namespace SecretParty.Models
{
    // El catálogo de hobbies que se pueden elegir en "configura tu cuenta".
    // En la base se guardan en la MISMA columna de siempre (Usuario.Intereses), separados por coma:
    //   "baile, música, vinilos"
    // Así no hizo falta migración, y los perfiles viejos que escribieron texto libre se siguen viendo.
    public static class Hobbies
    {
        public const int Maximo = 10; // para que el perfil no sea una lista eterna

        // Agrupados solo para que el formulario se vea ordenado
        public static readonly Dictionary<string, string[]> Catalogo = new()
        {
            ["Música"] = new[] { "música", "baile", "karaoke", "vinilos", "conciertos", "techno", "reggaetón", "rock", "indie", "salsa", "DJ" },
            ["Planes"] = new[] { "playa", "viajar", "camping", "brunch", "cócteles", "vino", "café", "comida", "cocinar", "festivales" },
            ["Arte"] = new[] { "cine", "series", "anime", "fotografía", "moda", "dibujo", "teatro", "museos", "lectura", "escribir" },
            ["Activo"] = new[] { "gym", "yoga", "fútbol", "running", "bici", "surf", "escalada", "skate", "pilates" },
            ["Chill"] = new[] { "videojuegos", "juegos de mesa", "astrología", "plantas", "mascotas", "tarot", "podcasts", "memes" },
        };

        public static IEnumerable<string> Todos => Catalogo.Values.SelectMany(x => x);

        // "baile, música" -> ["baile", "música"]  (sirve también para el texto libre viejo)
        public static List<string> Separar(string? texto) =>
            string.IsNullOrWhiteSpace(texto)
                ? new List<string>()
                : texto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList();

        // Lo que llega del formulario: SOLO dejamos pasar lo que está en el catálogo
        // (alguien podría mandar cualquier cosa editando el HTML), sin repetidos y máximo 10.
        // "anteriores" = los que la persona YA tenía escritos a mano antes del catálogo: esos se respetan.
        public static string? Limpiar(IEnumerable<string>? elegidos, IEnumerable<string>? anteriores = null)
        {
            if (elegidos == null) return null;
            var permitidos = Todos.Concat(anteriores ?? Enumerable.Empty<string>()).ToHashSet();
            var validos = elegidos
                .Where(e => permitidos.Contains(e))
                .Distinct()
                .Take(Maximo)
                .ToList();
            return validos.Count == 0 ? null : string.Join(", ", validos);
        }
    }
}
