namespace Delobytes.Email;

/// <summary>
/// Сервис отправки электропочтовых сообщений, который использует прямое подключение к SMTP-серверу.
/// </summary>
public interface ISmtpMailer
{
    /// <summary>
    /// Послать электропочтовое сообщение.
    /// </summary>
    /// <param name="emailMessage">Сообщение.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    void Send(EmailMessage emailMessage, int retryCount = 1);

    /// <summary>
    /// Послать электропочтовые сообщения.
    /// </summary>
    /// <param name="emailMessages">Сообщения.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    void Send(IEnumerable<EmailMessage> emailMessages, int retryCount = 1);

    /// <summary>
    /// Послать электропочтовое сообщение асинхронно.
    /// </summary>
    /// <param name="emailMessage">Сообщение.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задачу, которая завершается после отправки сообщения.</returns>
    Task SendAsync(EmailMessage emailMessage, int retryCount = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Послать электропочтовые сообщения асинхронно.
    /// </summary>
    /// <param name="emailMessages">Сообщения.</param>
    /// <param name="retryCount">Число повторных попыток отправки (по-умолчанию: 1)</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задачу, которая завершается после отправки сообщения.</returns>
    Task SendAsync(IEnumerable<EmailMessage> emailMessages, int retryCount = 1, CancellationToken cancellationToken = default);
}
