using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SecretParty.Data;
using SecretParty.Models;

namespace SecretParty.Servicios
{
    // Anota trazas: "en esta petición, la operación X intentó el servicio Y y funcionó / falló".
    public class Rastreador
    {
        private readonly DbContextOptions<AppDbContext> _opciones;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<Rastreador> _log;

        public Rastreador(DbContextOptions<AppDbContext> opciones, IHttpContextAccessor http, ILogger<Rastreador> log)
        {
            _opciones = opciones;
            _http = http;
            _log = log;
        }

        public async Task RegistrarAsync(string operacion, string servicio, ResultadoTraza resultado,
                                         string? detalle = null, long duracionMs = 0)
        {
            var http = _http.HttpContext;
            var idTexto = http?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var traza = new Traza
            {
                // TraceIdentifier = el código único que ASP.NET le pone a cada petición
                Peticion = http?.TraceIdentifier ?? Guid.NewGuid().ToString("N"),
                Operacion = operacion,
                Servicio = servicio,
                Resultado = resultado,
                Detalle = detalle != null && detalle.Length > 300 ? detalle[..300] : detalle,
                DuracionMs = duracionMs,
                UsuarioId = int.TryParse(idTexto, out var id) ? id : (int?)null,
                Fecha = Fechas.Ahora
            };

            _log.LogInformation("TRAZA {Operacion} -> {Servicio}: {Resultado}", operacion, servicio, resultado);

            try
            {
                // Usamos una conexión APARTE para que guardar la traza no se mezcle
                // con lo que la petición esté guardando (ni siquiera si eso falló)
                await using var db = new AppDbContext(_opciones);
                db.Trazas.Add(traza);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Una traza NUNCA debe romper la app: si no se puede guardar, solo se avisa en la consola
                _log.LogWarning(ex, "No se pudo guardar la traza {Operacion}/{Servicio}", operacion, servicio);
            }
        }

        // Ejecuta algo, mide cuánto tarda y anota si funcionó o falló.
        // Si falla, la excepción sigue su camino (quien llama decide qué hacer: reintentar, plan B...).
        public async Task<T> MedirAsync<T>(string operacion, string servicio, Func<Task<T>> accion)
        {
            var reloj = Stopwatch.StartNew();
            try
            {
                var resultado = await accion();
                await RegistrarAsync(operacion, servicio, ResultadoTraza.Exito, null, reloj.ElapsedMilliseconds);
                return resultado;
            }
            catch (Exception ex)
            {
                await RegistrarAsync(operacion, servicio, ResultadoTraza.Falla, ex.Message, reloj.ElapsedMilliseconds);
                throw;
            }
        }
    }
}