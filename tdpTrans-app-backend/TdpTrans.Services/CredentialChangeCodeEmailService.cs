using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TdpTrans.Services
{
    public class CredentialChangeCodeEmailService : ICredentialChangeCodeEmailService
    {
        private readonly CredentialChangeEmailOptions _options;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<CredentialChangeCodeEmailService> _logger;

        public CredentialChangeCodeEmailService(
            IOptions<CredentialChangeEmailOptions> options,
            IHostEnvironment hostEnvironment,
            ILogger<CredentialChangeCodeEmailService> logger)
        {
            _options = options.Value;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public async Task SendCredentialChangeCode(string email, string credentialChangeCode, DateTime expiresAtUtc)
        {
            if (ShouldUsePickupMode())
            {
                await WriteDevelopmentEmail(email, credentialChangeCode, expiresAtUtc);
                return;
            }

            EnsureSmtpIsConfigured();

            try
            {
                await SendViaSmtp(email, credentialChangeCode, expiresAtUtc);
            }
            catch (Exception exception) when (exception is SmtpException or FormatException or InvalidOperationException)
            {
                throw new InvalidOperationException(
                    "Codul de confirmare nu a putut fi trimis pe email. Verifica setarile SMTP din backend si contul expeditor configurat.",
                    exception);
            }
        }

        private async Task SendViaSmtp(string email, string credentialChangeCode, DateTime expiresAtUtc)
        {
            using var message = BuildMessage(email, credentialChangeCode, expiresAtUtc);
            using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = _options.EnableSsl,
                UseDefaultCredentials = false,
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                smtpClient.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            await smtpClient.SendMailAsync(message);
        }

        private async Task WriteDevelopmentEmail(string email, string credentialChangeCode, DateTime expiresAtUtc)
        {
            var pickupDirectory = ResolvePickupDirectory(_options.PickupDirectory);
            Directory.CreateDirectory(pickupDirectory);

            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{SanitizeFileName(email)}.eml";
            var filePath = Path.Combine(pickupDirectory, fileName);

            await File.WriteAllTextAsync(filePath, BuildRawEmail(email, credentialChangeCode, expiresAtUtc), Encoding.UTF8);
            _logger.LogInformation("Credential change email for {Email} was written to {FilePath}.", email, filePath);
        }

        private MailMessage BuildMessage(string email, string credentialChangeCode, DateTime expiresAtUtc)
        {
            var fromAddress = ResolveFromAddress();
            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, _options.FromDisplayName),
                Subject = _options.Subject,
                Body = BuildEmailBody(credentialChangeCode, expiresAtUtc),
                IsBodyHtml = false,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
            };

            message.To.Add(email);
            return message;
        }

        private string BuildRawEmail(string email, string credentialChangeCode, DateTime expiresAtUtc)
        {
            var fromAddress = ResolveFromAddress();
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"From: {_options.FromDisplayName} <{fromAddress}>");
            messageBuilder.AppendLine($"To: {email}");
            messageBuilder.AppendLine($"Subject: {_options.Subject}");
            messageBuilder.AppendLine("Content-Type: text/plain; charset=utf-8");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine(BuildEmailBody(credentialChangeCode, expiresAtUtc));
            return messageBuilder.ToString();
        }

        private static string BuildEmailBody(string credentialChangeCode, DateTime expiresAtUtc)
        {
            var minutesUntilExpiry = Math.Max(1, (int)Math.Ceiling((expiresAtUtc - DateTime.UtcNow).TotalMinutes));

            return $"""
Salut,

Codul tau de confirmare pentru actualizarea credentialelor TDP Transport este: {credentialChangeCode}

Codul expira in aproximativ {minutesUntilExpiry} minute. Daca nu ai initiat tu aceasta cerere, ignora acest email.

TDP Transport
""";
        }

        private string ResolvePickupDirectory(string configuredPickupDirectory)
        {
            return Path.IsPathRooted(configuredPickupDirectory)
                ? configuredPickupDirectory
                : Path.Combine(_hostEnvironment.ContentRootPath, configuredPickupDirectory);
        }

        private bool ShouldUsePickupMode()
        {
            return string.Equals(_options.DeliveryMode?.Trim(), "pickup", StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureSmtpIsConfigured()
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpHost))
            {
                throw new InvalidOperationException(
                    "Trimiterea emailurilor nu este configurata. Completeaza CredentialChangeEmail:SmtpHost, UserName, Password si FromAddress in backend.");
            }

            if (_options.SmtpPort <= 0)
            {
                throw new InvalidOperationException("CredentialChangeEmail:SmtpPort trebuie sa fie un port SMTP valid.");
            }

            _ = ResolveFromAddress();
        }

        private string ResolveFromAddress()
        {
            var configuredFromAddress = _options.FromAddress?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(configuredFromAddress)
                && !string.Equals(configuredFromAddress, "no-reply@tdptrans.local", StringComparison.OrdinalIgnoreCase))
            {
                return configuredFromAddress;
            }

            var configuredUserName = _options.UserName?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(configuredUserName) && configuredUserName.Contains('@'))
            {
                return configuredUserName;
            }

            throw new InvalidOperationException(
                "CredentialChangeEmail:FromAddress trebuie sa fie o adresa reala sau CredentialChangeEmail:UserName trebuie sa fie un email valid.");
        }

        private static string SanitizeFileName(string value)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            return string.Concat(value.Select(character => invalidCharacters.Contains(character) ? '_' : character));
        }
    }
}
