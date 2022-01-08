using System.IO;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Delobytes.Email;

/// <summary>
/// Сервис отправки электропочтовых сообщений с использованием библиотеки MailKit.
/// </summary>
public class MailKitMailer : GenericSmtpMailer, ISmtpMailer, IDisposable
{
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="options">Настройки сервиса электропочты.</param>
    /// <param name="logger">Логер.</param>
    public MailKitMailer(SmtpEmailOptions options, ILogger<MailKitMailer> logger = null) : base(options)
    {
        _log = logger;
    }

    private readonly ILogger<MailKitMailer> _log;
    private bool _disposedValue;
    private SmtpClient _client;


    private SmtpClient GetClient()
    {
        SmtpClient client = new SmtpClient();
        client.Timeout = ConnectionSettings.Timeout;

        SecureSocketOptions secureSocketOptions;

        switch (ConnectionSettings.Security)
        {
            case TransportEncryptionType.None:
                secureSocketOptions = SecureSocketOptions.None;
                break;
            case TransportEncryptionType.SSL:
                secureSocketOptions = SecureSocketOptions.SslOnConnect;
                break;
            case TransportEncryptionType.TLS:
                secureSocketOptions = SecureSocketOptions.StartTls;
                break;
            default:
                secureSocketOptions = SecureSocketOptions.Auto;
                break;
        }

        try
        {
            client.Connect(Server, Port, secureSocketOptions);
        }
        catch (SmtpCommandException ex)
        {
            _log?.LogError(ex, "Error connecting to SMTP server: {SmtpStatusCode}", ex.StatusCode);
            return null;
        }
        catch (SmtpProtocolException ex)
        {
            _log?.LogError(ex, "Protocol error while trying to connect");
            return null;
        }

        client.AuthenticationMechanisms.Remove("XOAUTH2");

        try
        {
            client.Authenticate(Username, Password);
        }
        catch (AuthenticationException ex)
        {
            _log?.LogError(ex, "Error getting new SMTP client: Invalid user name or password.");
            return null;
        }
        catch (SmtpCommandException ex)
        {
            _log?.LogError(ex, "Error connecting to SMTP server: {SmtpStatusCode}", ex.StatusCode);
            return null;
        }
        catch (SmtpProtocolException ex)
        {
            _log?.LogError(ex, "Protocol error while trying to authenticate");
            return null;
        }

        return client;
    }

    private async Task<SmtpClient> GetClientAsync(CancellationToken cancellationToken)
    {
        SmtpClient client = new SmtpClient();
        client.Timeout = ConnectionSettings.Timeout;

        SecureSocketOptions secureSocketOptions;

        switch (ConnectionSettings.Security)
        {
            case TransportEncryptionType.None:
                secureSocketOptions = SecureSocketOptions.None;
                break;
            case TransportEncryptionType.SSL:
                secureSocketOptions = SecureSocketOptions.SslOnConnect;
                break;
            case TransportEncryptionType.TLS:
                secureSocketOptions = SecureSocketOptions.StartTls;
                break;
            default:
                secureSocketOptions = SecureSocketOptions.Auto;
                break;
        }

        try
        {
            await client.ConnectAsync(Server, Port, secureSocketOptions, cancellationToken);
        }
        catch (SmtpCommandException ex)
        {
            _log?.LogError(ex,"Error connecting to SMTP server: {SmtpStatusCode}", ex.StatusCode);
            return null;
        }
        catch (SmtpProtocolException ex)
        {
            _log?.LogError(ex, "Protocol error while trying to connect");
            return null;
        }

        client.AuthenticationMechanisms.Remove("XOAUTH2");

        try
        {
            await client.AuthenticateAsync(Username, Password, cancellationToken);
        }
        catch (AuthenticationException ex)
        {
            _log?.LogError(ex, "Error getting new SMTP client: Invalid user name or password.");
            return null;
        }
        catch (SmtpCommandException ex)
        {
            _log?.LogError(ex, "Error connecting to SMTP server: {SmtpStatusCode}", ex.StatusCode);
            return null;
        }
        catch (SmtpProtocolException ex)
        {
            _log?.LogError(ex, "Protocol error while trying to authenticate");
            return null;
        }

        return client;
    }

    private void CheckPreconditions(EmailMessage emailMessage)
    {
        if (emailMessage.Recipients.Count == 0)
        {
            throw new EmailException("At least one recipient must be specified for email message.");
        }
    }

    private void CheckPreconditions(IEnumerable<EmailMessage> emailMessages)
    {
        foreach (EmailMessage emailMessage in emailMessages)
        {
            if (emailMessage.Recipients.Count == 0)
            {
                throw new EmailException("At least one recipient must be specified for email message.");
            }
        }
    }

    private MimeMessage ConvertToMimeMessage(EmailMessage emailMessage)
    {
        MimeMessage message = new MimeMessage();
        MailboxAddress sender = !string.IsNullOrEmpty(emailMessage.SenderName) ? new MailboxAddress(emailMessage.SenderName, Username) : MailboxAddress.Parse(Username);
        message.From.Add(sender);

        if (string.IsNullOrEmpty(emailMessage.ReplyTo))
        {
            message.ReplyTo.Add(message.From.First());
        }
        else
        {
            message.ReplyTo.Add(InternetAddress.Parse(emailMessage.ReplyTo));
        }

        foreach (string email in emailMessage.Recipients)
        {
            message.To.Add(MailboxAddress.Parse(email));
        }

        if (emailMessage.CC.Count > 0)
        {
            foreach (string email in emailMessage.CC)
            {
                message.Cc.Add(MailboxAddress.Parse(email));
            }
        }

        if (emailMessage.BCC.Count > 0)
        {
            foreach (string email in emailMessage.BCC)
            {
                message.Bcc.Add(MailboxAddress.Parse(email));
            }
        }

        message.Subject = emailMessage.Subject;

        BodyBuilder builder = new BodyBuilder { HtmlBody = emailMessage.Body };

        emailMessage.AttachmentPaths.ForEach(path =>
        {
            if (File.Exists(path))
            {
                builder.Attachments.Add(path);
            }
        });

        message.Body = builder.ToMessageBody();

        return message;
    }

    private void Send(MimeMessage message, int retryCount)
    {
        bool sentSuccessfully = false;
        int retryCounter = 0;

        while (!sentSuccessfully && retryCounter < retryCount)
        {
            try
            {
                if (_client == null)
                {
                    using (SmtpClient client = GetClient())
                    {
                        if (client == null)
                        {
                            _log?.LogError("Error sending email message {MessageId} to {To}: no client", message.MessageId, message.To);
                            return;
                        }

                        client.Send(message);
                        client.Disconnect(true);

                        sentSuccessfully = true;
                        _log?.LogInformation("Email message {MessageId} has been sent {To}", message.MessageId, message.To);
                    }
                }
                else
                {
                    _client.Send(message);

                    sentSuccessfully = true;
                    _log?.LogInformation("Email message {MessageId} has been sent to {To}", message.MessageId, message.To);
                }
            }
            catch (SmtpCommandException ex)
            {
                _log?.LogError(ex, "Smtp error while sending email message");

                retryCounter++;
                Thread.Sleep(retryCount * 15000);
            }
            catch (SmtpProtocolException ex)
            {
                _log?.LogError(ex, "Protocol error while sending email message");

                retryCounter++;
                Thread.Sleep(retryCount * 15000);
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Error sending email {MessageId} {To} for {AttemptsCount}", message.MessageId, message.To, retryCount);

                retryCounter++;
                Thread.Sleep(retryCount * 15000);
            }
        }
    }

    /// <summary>
    /// Послать электропочтовое сообщение.
    /// </summary>
    /// <param name="emailMessage">Сообщение.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    public void Send(EmailMessage emailMessage, int retryCount = 1)
    {
        ArgumentNullException.ThrowIfNull(emailMessage);

        CheckPreconditions(emailMessage);

        MimeMessage message = ConvertToMimeMessage(emailMessage);

        Send(message, retryCount);
    }

    /// <summary>
    /// Послать электропочтовые сообщения.
    /// </summary>
    /// <param name="emailMessages">Сообщения.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    public void Send(IEnumerable<EmailMessage> emailMessages, int retryCount = 1)
    {
        ArgumentNullException.ThrowIfNull(emailMessages);

        CheckPreconditions(emailMessages);

        List<MimeMessage> messages = new List<MimeMessage>();

        emailMessages.ToList().ForEach(message =>
        {
            MimeMessage mimeMessage = ConvertToMimeMessage(message);
            messages.Add(mimeMessage);
        });

        using (SmtpClient client = GetClient())
        {
            if (client == null)
            {
                _log?.LogError("Error sending email messages: unable to create client");
                return;
            }

            _client = client;

            //Office365 has a message rate limit of 30 emails per minute
            if (messages.Count > 30)
            {
                int batchCounter = 0;

                IEnumerable<MimeMessage[]> batches = messages.Chunk(30);

                foreach (MimeMessage[] batch in batches)
                {
                    batchCounter++;

                    Array.ForEach(batch, message => Send(message, retryCount));

                    if (batchCounter < batches.Count())
                    {
                        Thread.Sleep(61000);
                    }
                }
            }
            else
            {
                messages.ForEach(message => Send(message, retryCount));
            }
        }
    }

    private async Task SendAsync(MimeMessage message, int retryCount, CancellationToken cancellationToken = default)
    {
        bool sentSuccessfully = false;
        int retryCounter = 0;

        while (!sentSuccessfully && retryCounter < retryCount)
        {
            try
            {
                if (_client == null)
                {
                    using (SmtpClient client = await GetClientAsync(cancellationToken))
                    {
                        if (client == null)
                        {
                            _log?.LogError("Error sending email message {MessageId} to {To}: no client", message.MessageId, message.To);
                            return;
                        }

                        await client.SendAsync(message, cancellationToken);
                        await client.DisconnectAsync(true, cancellationToken);

                        sentSuccessfully = true;
                        _log?.LogInformation("Email message {MessageId} has been sent {To}", message.MessageId, message.To);
                    }
                }
                else
                {
                    await _client.SendAsync(message, cancellationToken);

                    sentSuccessfully = true;
                    _log?.LogInformation("Email message {MessageId} has been sent {To}", message.MessageId, message.To);
                }
            }
            catch (SmtpCommandException ex)
            {
                _log?.LogError(ex, "Smtp error while sending email message");

                retryCounter++;

                await Task.Delay(retryCounter * 15000, cancellationToken);
            }
            catch (SmtpProtocolException ex)
            {
                _log?.LogError(ex, "Protocol error while sending email message");

                retryCounter++;

                await Task.Delay(retryCounter * 15000, cancellationToken);
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Error sending email {MessageId} {To} for {AttemptsCount}", message.MessageId, message.To, retryCount);

                retryCounter++;

                await Task.Delay(retryCounter * 15000, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Послать электропочтовое сообщение асинхронно.
    /// </summary>
    /// <param name="emailMessage">Сообщение.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задачу, которая завершается после отправки сообщения.</returns>
    public async Task SendAsync(EmailMessage emailMessage, int retryCount = 1, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emailMessage);

        CheckPreconditions(emailMessage);

        MimeMessage message = ConvertToMimeMessage(emailMessage);

        await SendAsync(message, retryCount, cancellationToken);
    }

    /// <summary>
    /// Послать электропочтовые сообщения асинхронно.
    /// </summary>
    /// <param name="emailMessages">Сообщения.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задачу, которая завершается после отправки сообщения.</returns>
    public async Task SendAsync(IEnumerable<EmailMessage> emailMessages, int retryCount = 1, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emailMessages);

        CheckPreconditions(emailMessages);

        List<MimeMessage> messages = new List<MimeMessage>();

        emailMessages.ToList().ForEach(message =>
        {
            MimeMessage mimeMessage = ConvertToMimeMessage(message);
            messages.Add(mimeMessage);
        });

        using (SmtpClient client = await GetClientAsync(cancellationToken))
        {
            if (client == null)
            {
                _log?.LogError("Error sending email messages: unable to create client");
                return;
            }

            _client = client;

            //Office365 has a message rate limit of 30 emails per minute
            if (messages.Count > 30)
            {
                int batchCounter = 0;

                IEnumerable<MimeMessage[]> batches = messages.Chunk(30);

                foreach (MimeMessage[] batch in batches)
                {
                    batchCounter++;

                    foreach (MimeMessage message in batch)
                    {
                        await SendAsync(message, 1, cancellationToken);
                    }

                    if (batchCounter < batches.Count())
                    {
                        await Task.Delay(61000, cancellationToken);
                    }
                }
            }
            else
            {
                foreach (MimeMessage message in messages)
                {
                    await SendAsync(message, retryCount, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Метод освобождения ресурсов.
    /// </summary>
    /// <param name="disposing">Флаг, который определяет необходимость освобождения управляемых ресурсов.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (_client.IsConnected)
                {
                    _client.Disconnect(true);
                }

                _client?.Dispose();
            }

            _disposedValue = true;
        }
    }

    /// <summary>
    /// Метод освобождения ресурсов.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
