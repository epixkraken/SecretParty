using System.Globalization;

namespace SecretParty.Servicios
{
    // Todo lo de fechas en un solo lugar.
    public static class Fechas
    {
        // Para que los meses y días salgan en español ("sáb 22 nov")
        public static readonly CultureInfo Espanol = new("es-EC");

        // El servidor de MonsterASP puede estar en otra zona horaria (ej. Europa o UTC).
        // Los eventos se escriben en hora de Ecuador, así que "ahora" también debe ser hora de Ecuador.
        private static readonly TimeZoneInfo? ZonaEcuador = BuscarZona();

        public static DateTime Ahora =>
            ZonaEcuador == null ? DateTime.Now : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaEcuador);

        private static TimeZoneInfo? BuscarZona()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); } // nombre en Windows viejo
                catch { return null; }
            }
        }

        // "sáb 22 nov · 21:00"
        public static string Corta(DateTime fecha) =>
            fecha.ToString("ddd d MMM · HH:mm", Espanol).Replace(".", "");

        // "sábado 22 de noviembre de 2026, 21:00"
        public static string Larga(DateTime fecha) =>
            fecha.ToString("dddd d 'de' MMMM 'de' yyyy, HH:mm", Espanol);

        // Para el cuadrito de fecha: "22" y "nov"
        public static string Dia(DateTime fecha) => fecha.Day.ToString();
        public static string Mes(DateTime fecha) => fecha.ToString("MMM", Espanol).Replace(".", "");
    }
}
