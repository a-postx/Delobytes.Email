namespace Delobytes.Email;

/// <summary>
/// Тип протокола безопасности для соединения с сервером.
/// </summary>
public enum SecurityProtocolType
{
    /// <summary>
    /// Без шифрования.
    /// </summary>
    None,
    /// <summary>
    /// SSL-шифрование.
    /// </summary>
    SSL,
    /// <summary>
    /// TLS-шифрование.
    /// </summary>
    TLS
}

/// <summary>
/// Настройки SMTP-соеднинения.
/// </summary>
public class ConnectionSettings
{
    /// <summary>
    /// Настройки SMTP-соеднинения.
    /// </summary>
    public ConnectionSettings()
    {

    }
    /// <summary>
    /// Настройки SMTP-соеднинения.
    /// </summary>
    /// <param name="security">Тип протокола безопасности для соединения с сервером.</param>
    /// <param name="timeout">Таймаут соединения (миллисекунды).</param>
    public ConnectionSettings(SecurityProtocolType security, int timeout)
    {
        Security = security;
        Timeout = timeout;
    }

    /// <summary>
    /// Тип протокола безопасности для соединения с сервером.
    /// </summary>
    public SecurityProtocolType Security { get; } = SecurityProtocolType.TLS;

    /// <summary>
    /// Таймаут соединения (миллисекунды).
    /// </summary>
    public int Timeout { get; } = 60000;
}
