namespace PracticoN6.Frontend.Shared.Services
{
    // Capacidades que dependen del dispositivo. La app móvil (MAUI) y el host web las implementan cada una a su manera;
    // el resto de la aplicación solo conoce estas interfaces.

    /// <summary>Almacenamiento cifrado para datos sensibles (el token de sesión). MAUI: SecureStorage.</summary>
    public interface IAlmacenamientoSeguro
    {
        Task<string?> LeerAsync(string clave);
        Task GuardarAsync(string clave, string valor);
        Task EliminarAsync(string clave);
    }

    /// <summary>Almacenamiento simple de preferencias (dirección del servidor, carrito). MAUI: Preferences.</summary>
    public interface IPreferencias
    {
        Task<string?> LeerAsync(string clave);
        Task GuardarAsync(string clave, string valor);
        Task EliminarAsync(string clave);
    }

    /// <summary>Entrega un archivo (por ejemplo un PDF) al usuario. MAUI: hoja de compartir de Android. Web: descarga.</summary>
    public interface IGestorArchivos
    {
        Task GuardarYCompartirAsync(string nombreArchivo, byte[] contenido, string tipoMime);
    }

    public class OpcionesTienda
    {
        /// <summary>Dirección de la API que se usa mientras el usuario no configure otra.</summary>
        public string UrlApiPorDefecto { get; set; } = "http://localhost:5276";
    }
}
