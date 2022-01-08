namespace Delobytes.Email;

/// <summary>
/// Настройки сервиса электропочты.
/// </summary>
public class SmtpEmailOptions
{
    /// <summary>
    /// Время жизни сервиса, с которым он регистрируется во внедрении зависимостей.
    /// </summary>
    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Scoped;
    
    /// <summary>
    /// <para>
    /// SMTP-сервер.
    /// </para>
    /// </summary>
    public string SmtpServer { get; set; }

    /// <summary>
    /// <para>
    /// Порт.
    /// </para>
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// <para>
    /// Пользователь.
    /// </para>
    /// </summary>
    public string SmtpUsername { get; set; }

    /// <summary>
    /// <para>
    /// Пароль.
    /// </para>
    /// </summary>
    public string SmtpPassword { get; set; }

    /// <summary>
    /// Настройки соеднинения.
    /// </summary>
    public ConnectionSettings ConnectionSettings { get; set; } = new ConnectionSettings();
}
