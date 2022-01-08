namespace Delobytes.Email;

/// <summary>
/// Исключение электропочты.
/// </summary>
public class EmailException : Exception
{
    /// <summary>
    /// Базовый конструктор.
    /// </summary>
    public EmailException()
    {
    }

    /// <summary>
    /// Конструктор с сообщением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public EmailException(string message) : base(message)
    {
    }

    /// <summary>
    /// Конструктор с сообщением и внутренним исключением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public EmailException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
