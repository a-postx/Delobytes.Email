using System;
using System.Collections.Generic;

namespace Delobytes.Email
{
    public interface IMailMessage
    {
        List<string> Attachments { get; set; }
        string Body { get; set; }
        List<string> BCC { get; set; }
        List<string> CC { get; set; }
        Guid Cd { get; set; }
        List<string> Recipients { get; set; }
        string SenderName { get; set; }
        int StatusCode { get; set; }
        string StatusMessage { get; set; }
        string Subject { get; set; }
    }
}
