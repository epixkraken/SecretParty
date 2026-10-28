namespace SecretParty.Models
{
    // DATO SENSIBLE: Secret Party es solo para mayores de 18.
    // Esta regla se revisa en el SERVIDOR (registro, editar perfil y admin)
    // antes de guardar en la base, así que no se puede saltar desde el navegador.
    public static class Edad
    {
        public const int Minima = 18;
        public const int Maxima = 100;

        // Años cumplidos: si este año todavía no llega su cumpleaños, se resta 1
        public static int Calcular(DateTime nacimiento)
        {
            var hoy = DateTime.Today;
            var edad = hoy.Year - nacimiento.Year;
            if (nacimiento.Date > hoy.AddYears(-edad)) edad--;
            return edad;
        }

        // Devuelve el mensaje de error, o null si la fecha está bien
        public static string? Validar(DateTime? nacimiento)
        {
            if (nacimiento == null)
                return "La fecha de nacimiento es obligatoria";

            if (nacimiento.Value.Date > DateTime.Today)
                return "La fecha de nacimiento no puede estar en el futuro";

            var edad = Calcular(nacimiento.Value);

            if (edad > Maxima)
                return "Revisa la fecha de nacimiento";

            if (edad < Minima)
                return $"Secret Party es solo para mayores de {Minima} años";

            return null;
        }
    }
}
