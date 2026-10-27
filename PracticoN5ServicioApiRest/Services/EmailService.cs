using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PracticoN5ServicioApiRest.DTOs;

namespace PracticoN5ServicioApiRest.Services
{
    /// <summary>No se pudo entregar el correo al servidor SMTP (se informa como 502 Bad Gateway).</summary>
    public class EnvioCorreoException : Exception
    {
        public EnvioCorreoException(string mensaje, Exception? interna = null) : base(mensaje, interna) { }
    }

    /// <summary>
    /// Envía correos con adjunto por SMTP (sección "Smtp" de la configuración). Si no hay servidor configurado,
    /// el mensaje se guarda como archivo .eml en App_Data/Correos: sirve para probar sin credenciales reales.
    /// </summary>
    public class EmailService
    {
        private readonly IConfiguration _configuracion;
        private readonly IWebHostEnvironment _entorno;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuracion, IWebHostEnvironment entorno, ILogger<EmailService> logger)
        {
            _configuracion = configuracion;
            _entorno = entorno;
            _logger = logger;
        }

        public async Task<ResultadoEnvioDTO> EnviarAsync(
            string destinatario,
            string asunto,
            string cuerpo,
            string nombreAdjunto,
            byte[] adjunto,
            CancellationToken cancellationToken = default)
        {
            if (!MailboxAddress.TryParse(destinatario, out var direccionDestino))
            {
                throw new ArgumentException("La dirección de correo del destinatario no es válida.", nameof(destinatario));
            }

            string? host = _configuracion["Smtp:Host"];
            string remitente = _configuracion["Smtp:Remitente"] ?? "no-responder@tienda.local";
            string nombreRemitente = _configuracion["Smtp:NombreRemitente"] ?? _configuracion["Negocio:Nombre"] ?? "Tienda Móvil";

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(nombreRemitente, remitente));
            mensaje.To.Add(direccionDestino);
            mensaje.Subject = asunto;

            var constructor = new BodyBuilder { TextBody = cuerpo };
            constructor.Attachments.Add(nombreAdjunto, adjunto, ContentType.Parse("application/pdf"));
            mensaje.Body = constructor.ToMessageBody();

            if (string.IsNullOrWhiteSpace(host))
            {
                return await GuardarComoArchivoAsync(mensaje, cancellationToken);
            }

            try
            {
                using var cliente = new SmtpClient();

                int puerto = _configuracion.GetValue<int?>("Smtp:Puerto") ?? 587;
                bool usarSsl = _configuracion.GetValue<bool?>("Smtp:UsarSsl") ?? true;

                await cliente.ConnectAsync(
                    host,
                    puerto,
                    usarSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                    cancellationToken);

                string? usuario = _configuracion["Smtp:Usuario"];
                if (!string.IsNullOrWhiteSpace(usuario))
                {
                    await cliente.AuthenticateAsync(usuario, _configuracion["Smtp:Password"] ?? string.Empty, cancellationToken);
                }

                await cliente.SendAsync(mensaje, cancellationToken);
                await cliente.DisconnectAsync(true, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "No se pudo enviar el correo a {Destinatario} por SMTP.", destinatario);
                throw new EnvioCorreoException("No se pudo enviar el correo: el servidor de correo no respondió o rechazó el mensaje.", ex);
            }

            return new ResultadoEnvioDTO
            {
                Enviado = true,
                Modo = "SMTP",
                Mensaje = $"Correo enviado a {destinatario}."
            };
        }

        private async Task<ResultadoEnvioDTO> GuardarComoArchivoAsync(MimeMessage mensaje, CancellationToken cancellationToken)
        {
            string carpeta = Path.Combine(_entorno.ContentRootPath, "App_Data", "Correos");
            Directory.CreateDirectory(carpeta);

            string nombre = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.eml";
            string ruta = Path.Combine(carpeta, nombre);

            await using (var flujo = File.Create(ruta))
            {
                await mensaje.WriteToAsync(flujo, cancellationToken);
            }

            _logger.LogWarning("Smtp:Host no está configurado: el correo se guardó en {Ruta} en lugar de enviarse.", ruta);

            return new ResultadoEnvioDTO
            {
                Enviado = false,
                Modo = "Archivo",
                Mensaje = $"No hay un servidor SMTP configurado: el correo no se envió, se guardó en App_Data/Correos/{nombre}."
            };
        }
    }
}
