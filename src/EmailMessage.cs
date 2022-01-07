using System;
using System.Collections.Generic;

namespace Delobytes.Email;

/// <summary>
/// Электропочтовое сообщение.
/// </summary>
public class EmailMessage
{
    /// <summary>
    /// Конструктор.
    /// </summary>
    public EmailMessage()
    {

    }

    /// <summary>
    /// Список вложений.
    /// </summary>
    public List<string> AttachmentPaths { get; set; } = new List<string>();
    /// <summary>
    /// Тело сообщения.
    /// </summary>
    public string Body { get; set; } = string.Empty;
    /// <summary>
    /// Список получателей теневой копии сообщения (BCC).
    /// </summary>
    public List<string> BCC { get; set; } = new List<string>();
    /// <summary>
    /// Список полчателей копии сообщения (CC).
    /// </summary>
    public List<string> CC { get; set; } = new List<string>();
    /// <summary>
    /// Идентификатор сообщения.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>
    /// Список получателей сообщения.
    /// </summary>
    public List<string> Recipients { get; set; } = new List<string>();
    /// <summary>
    /// Поле адресата для ответа сообщения.
    /// </summary>
    public string ReplyTo { get; set; } = string.Empty;
    /// <summary>
    /// Имя источника сообщения.
    /// </summary>
    public string SenderName { get; set; }
    /// <summary>
    /// Тема сообщения.
    /// </summary>
    public string Subject { get; set; } = string.Empty;
}
