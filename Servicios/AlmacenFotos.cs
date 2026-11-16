namespace SecretParty.Servicios
{
    // Guarda y borra las fotos que suben los usuarios (perfil, eventos...).
    // Las fotos quedan en wwwroot/uploads/<carpeta>/ y en la base solo guardamos la RUTA,
    // por ejemplo "/uploads/perfiles/3f2a...jpg".
    public class AlmacenFotos
    {
        public const long TamanoMaximo = 5 * 1024 * 1024; // 5 MB

        // Solo aceptamos estos formatos de imagen
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private readonly IWebHostEnvironment _env;

        // IWebHostEnvironment nos dice dónde está wwwroot en el disco (en la compu o en el servidor)
        public AlmacenFotos(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Revisa que el archivo sea una imagen válida.
        // Devuelve el mensaje de error, o null si está todo bien.
        public string? Validar(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return "No elegiste ninguna foto";

            if (archivo.Length > TamanoMaximo)
                return "La foto no puede pesar más de 5 MB";

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                return "Solo se aceptan fotos JPG, PNG, WEBP o GIF";

            // La extensión se puede falsificar (renombrar virus.exe a foto.jpg).
            // Por eso leemos los primeros bytes del archivo: cada formato de imagen
            // empieza con una "firma" fija (los "magic numbers").
            if (!TieneFirmaDeImagen(archivo))
                return "El archivo no es una imagen de verdad";

            return null;
        }

        // Guarda la foto con un nombre NUEVO y aleatorio, y devuelve su ruta pública.
        // Nunca usamos el nombre original: podría traer "../" o pisar la foto de otra persona.
        public async Task<string> Guardar(IFormFile archivo, string carpeta)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var nombre = $"{Guid.NewGuid():N}{extension}";

            var directorio = Path.Combine(_env.WebRootPath, "uploads", carpeta);
            Directory.CreateDirectory(directorio); // si la carpeta no existe, la crea

            var rutaFisica = Path.Combine(directorio, nombre);
            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            return $"/uploads/{carpeta}/{nombre}";
        }

        // Borra una foto a partir de su ruta pública ("/uploads/perfiles/abc.jpg").
        public void Borrar(string? rutaPublica)
        {
            // Por seguridad, solo borramos cosas que estén dentro de /uploads/
            if (string.IsNullOrWhiteSpace(rutaPublica) || !rutaPublica.StartsWith("/uploads/"))
                return;

            var rutaFisica = Path.Combine(_env.WebRootPath, rutaPublica.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(rutaFisica))
                File.Delete(rutaFisica);
        }

        private static bool TieneFirmaDeImagen(IFormFile archivo)
        {
            var cabecera = new byte[12];
            using (var stream = archivo.OpenReadStream())
            {
                if (stream.Read(cabecera, 0, cabecera.Length) < cabecera.Length)
                    return false;
            }

            // JPG: FF D8 FF
            if (cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF) return true;
            // PNG: 89 50 4E 47  (".PNG")
            if (cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47) return true;
            // GIF: "GIF8"
            if (cabecera[0] == 'G' && cabecera[1] == 'I' && cabecera[2] == 'F' && cabecera[3] == '8') return true;
            // WEBP: "RIFF" .... "WEBP"
            if (cabecera[0] == 'R' && cabecera[1] == 'I' && cabecera[2] == 'F' && cabecera[3] == 'F'
                && cabecera[8] == 'W' && cabecera[9] == 'E' && cabecera[10] == 'B' && cabecera[11] == 'P') return true;

            return false;
        }
    }
}
