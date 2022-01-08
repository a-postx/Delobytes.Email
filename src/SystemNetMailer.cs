using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace Delobytes.Email;

/// <summary>
/// Сервис отправки электропочтовых сообщений с использованием встроенной библиотеки System.Net.
/// </summary>
public class SystemNetMailer : GenericSmtpMailer, ISmtpMailer, IDisposable
{
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="options">Настройки сервиса электропочты.</param>
    /// <param name="logger">Логер.</param>
    public SystemNetMailer(SmtpEmailOptions options, ILogger<SystemNetMailer> logger) : base(options)
    {
        _log = logger;
    }

    private readonly ILogger<SystemNetMailer> _log;
    private bool _disposedValue;


    private SmtpClient GetClient()
    {
        try
        {
            SmtpClient client = new SmtpClient(Server, Port);
            client.Timeout = ConnectionSettings.Timeout;
            client.EnableSsl = ConnectionSettings.Security == TransportEncryptionType.SSL;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(Username, Password);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            return client;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "Can't get SMTP client for {Server}", Server);
            return null;
        }
    }

    private void Send(MailMessage message, int retryCount)
    {
        bool sentSuccessfully = false;
        int retryCounter = 0;

        while (!sentSuccessfully && retryCounter < retryCount)
        {
            try
            {
                using (SmtpClient client = GetClient())
                {
                    if (client == null)
                    {
                        _log?.LogError("Error sending email message to {To}: no client", message.To);
                        return;
                    }

                    client.Send(message);

                    sentSuccessfully = true;
                    _log?.LogInformation("Email message has been sent {To}", message.To);
                }
            }
            catch (SmtpException ex)
            {
                throw ex.InnerException == null ? ex : ex.InnerException.InnerException ?? ex.InnerException;
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "Error sending email {To} for {AttemptsCount}", message.To, retryCount);

                retryCounter++;
                Thread.Sleep(retryCount * 15000);
            }
        }
    }

    private MailMessage ConvertToMailMessage(EmailMessage emailMessage)
    {
        MailMessage message = new MailMessage();
        MailAddress from = !string.IsNullOrEmpty(emailMessage.SenderName) ? new MailAddress(Username, emailMessage.SenderName) : new MailAddress(Username);
        message.From = from;

        if (string.IsNullOrEmpty(emailMessage.ReplyTo))
        {
            message.ReplyToList.Add(message.From);
        }
        else
        {
            message.ReplyToList.Add(emailMessage.ReplyTo);
        }

        foreach (string emailMessageRecipient in emailMessage.Recipients)
        {
            message.To.Add(new MailAddress(emailMessageRecipient));
        }

        if (emailMessage.CC.Count > 0)
        {
            foreach (string email in emailMessage.CC)
            {
                message.CC.Add(new MailAddress(email));
            }
        }

        if (emailMessage.BCC.Count > 0)
        {
            foreach (string email in emailMessage.BCC)
            {
                message.Bcc.Add(new MailAddress(email));
            }
        }

        message.Subject = emailMessage.Subject;
        message.Body = emailMessage.Body;
        message.IsBodyHtml = true;
        message.BodyTransferEncoding = TransferEncoding.Base64;

        emailMessage.AttachmentPaths.ForEach(path =>
        {
            if (File.Exists(path))
            {
                message.Attachments.Add(new Attachment(path));
            }
        });

        return message;
    }

    /// <summary>
    /// Послать электропочтовое сообщение.
    /// </summary>
    /// <param name="emailMessage">Сообщение.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    public void Send(EmailMessage emailMessage, int retryCount = 1)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Послать электропочтовые сообщения.
    /// </summary>
    /// <param name="emailMessages">Сообщения.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    public void Send(IEnumerable<EmailMessage> emailMessages, int retryCount = 1)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Послать электропочтовое сообщение асинхронно.
    /// </summary>
    /// <param name="emailMessage">Сообщение.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задачу, которая завершается после отправки сообщения.</returns>
    public Task SendAsync(EmailMessage emailMessage, int retryCount = 1, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Послать электропочтовые сообщения асинхронно.
    /// </summary>
    /// <param name="emailMessages">Сообщения.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задачу, которая завершается после отправки сообщения.</returns>
    public Task SendAsync(IEnumerable<EmailMessage> emailMessages, int retryCount = 1, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Послать сообщение с событием календаря.
    /// </summary>
    /// <param name="emailMessage">Сообщение, к которому необходимо прикрепить событие.</param>
    /// <param name="startTime">Время начала события.</param>
    /// <param name="endTime">Время конца события.</param>
    /// <param name="retryCount">Число попыток отправки (по-умолчанию: 1)</param>
    public void SendCalendarEvent(EmailMessage emailMessage, DateTime startTime, DateTime endTime, int retryCount = 1)
    {
        MailMessage message = ConvertToMailMessage(emailMessage);

        message.Headers.Add("Content-class", "urn:content-classes:calendarmessage");

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("PRODID:- Acumatica Business Cloud event");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("METHOD:REQUEST");
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmssZ}", DateTime.UtcNow));
        sb.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", startTime));
        sb.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", endTime));
        sb.AppendLine("LOCATION: Online");
        sb.AppendLine(string.Format("UID:{0}", emailMessage.Id));
        sb.AppendLine(string.Format("DESCRIPTION:{0}", message.Body));
        sb.AppendLine(string.Format("X-ALT-DESC;FMTTYPE=text/html:{0}", message.Body));
        sb.AppendLine(string.Format("SUMMARY:{0}", message.Subject));
        sb.AppendLine(string.Format("ORGANIZER:MAILTO:{0}", message.From.Address));

        sb.AppendLine(string.Format("ATTENDEE;CN=\"{0}\";RSVP=TRUE:mailto:{1}", message.To[0].DisplayName, message.To[0].Address));

        sb.AppendLine("BEGIN:VALARM");
        sb.AppendLine("TRIGGER:-PT15M");
        sb.AppendLine("ACTION:DISPLAY");
        sb.AppendLine("DESCRIPTION:Reminder");
        sb.AppendLine("END:VALARM");
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");

        ContentType contype = new ContentType("text/calendar");
        contype.Parameters?.Add("method", "REQUEST");
        contype.Parameters?.Add("name", "Meeting.ics");
        AlternateView avCal = AlternateView.CreateAlternateViewFromString(sb.ToString(), contype);
        message.AlternateViews.Add(avCal);

        Send(message, retryCount);
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
                // TODO: dispose managed state (managed objects)
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
