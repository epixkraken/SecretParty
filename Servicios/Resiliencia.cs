namespace SecretParty.Servicios
{
    // Los dos patrones del paper "Mining design patterns from distributed execution traces":
    //   RETRY:    si un servicio falla, lo volvemos a intentar (el MISMO servicio), máximo N veces,
    //             esperando cada vez un poco más (backoff) para no saturarlo.
    //   FALLBACK: si se acaban los intentos, usamos un PLAN B (OTRO servicio).
    // El paper advierte que reintentar sin límite y sin plan B es un ANTIPATRÓN: por eso siempre hay los dos.
    // Cada intento queda como traza, así después podemos "minar" estos patrones con SQL.
    public class Resiliencia
    {
        private readonly Rastreador _rastreador;
        private readonly Caos _caos;

        public Resiliencia(Rastreador rastreador, Caos caos)
        {
            _rastreador = rastreador;
            _caos = caos;
        }

        public async Task<T> EjecutarAsync<T>(
            string operacion,
            string servicio, Func<Task<T>> principal,
            string servicioPlanB, Func<Task<T>> planB,
            int intentos = 3)
        {
            var espera = 100; // milisegundos antes del siguiente intento

            for (var intento = 1; intento <= intentos; intento++)
            {
                try
                {
                    return await _rastreador.MedirAsync(operacion, servicio, async () =>
                    {
                        _caos.TalVezFallar(servicio); // si el modo caos está prendido, aquí puede "fallar"
                        return await principal();
                    });
                }
                catch (Exception) when (intento < intentos)
                {
                    // RETRY: esperamos y volvemos a intentar el MISMO servicio
                    await Task.Delay(espera);
                    espera *= 2; // backoff: 100 ms, 200 ms, 400 ms...
                }
                catch (Exception)
                {
                    // Se acabaron los intentos: salimos del for y vamos al plan B
                }
            }

            // FALLBACK: plan B con OTRO servicio (también queda como traza)
            return await _rastreador.MedirAsync(operacion, servicioPlanB, planB);
        }
    }
}