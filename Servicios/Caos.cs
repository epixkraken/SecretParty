using System.Collections.Concurrent;

namespace SecretParty.Servicios
{
    // El error que lanzamos A PROPÓSITO cuando el modo caos decide fallar
    public class FallaInyectadaException : Exception
    {
        public FallaInyectadaException(string servicio)
            : base($"Falla inyectada en {servicio} (modo caos)") { }
    }

    // INYECTOR DE FALLAS ("modo caos").
    // En el paper, los autores provocan fallas a propósito y miran en las trazas
    // cómo reacciona el sistema (si tiene plan B = fallback, o si reintenta = retry).
    // Aquí el admin escoge qué servicio hacer fallar y con qué probabilidad.
    public class Caos
    {
        // Las partes de la app que se pueden hacer fallar
        public static readonly string[] ServiciosDisponibles = { "BaseDeDatos", "GoogleCalendar", "AlmacenFotos" };

        // servicio -> % de probabilidad de fallar (0 = nunca, 100 = siempre).
        // "Concurrent" porque muchas peticiones lo leen al mismo tiempo.
        private readonly ConcurrentDictionary<string, int> _probabilidad = new();

        public int Probabilidad(string servicio) =>
            _probabilidad.TryGetValue(servicio, out var p) ? p : 0;

        public bool Activo => _probabilidad.Values.Any(p => p > 0);

        public void Configurar(string servicio, int porcentaje)
        {
            if (!ServiciosDisponibles.Contains(servicio)) return;
            _probabilidad[servicio] = Math.Clamp(porcentaje, 0, 100);
        }

        public void ApagarTodo() => _probabilidad.Clear();

        // Se llama justo antes de usar un servicio: según la probabilidad, lanza una falla o deja pasar
        public void TalVezFallar(string servicio)
        {
            var p = Probabilidad(servicio);
            if (p > 0 && Random.Shared.Next(100) < p)
                throw new FallaInyectadaException(servicio);
        }
    }
}