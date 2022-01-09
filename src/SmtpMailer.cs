namespace Delobytes.Email;

/// <summary>
/// Сервис посылки электропочтовых сообщений с помощью протокола SMTP.
/// </summary>
public class SmtpMailer
{
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="options">Настройки сервиса электропочты.</param>
    protected SmtpMailer(SmtpEmailOptions options)
    {
        Server = options.SmtpServer;
        Username = options.SmtpUsername;
        Password = options.SmtpPassword;
        Port = options.SmtpPort;
        ConnectionSettings = options.ConnectionSettings;
    }

    /// <summary>
    /// Имя хоста сервера.
    /// </summary>
    protected string Server { get; }
    /// <summary>
    /// Пользователь.
    /// </summary>
    protected string Username { get; }
    /// <summary>
    /// Пароль.
    /// </summary>
    protected string Password { get; }
    /// <summary>
    /// Порт.
    /// </summary>
    protected int Port { get; set; }
    /// <summary>
    /// Настройки SMTP-соеднинения.
    /// </summary>
    protected ConnectionSettings ConnectionSettings { get; set; }
}
