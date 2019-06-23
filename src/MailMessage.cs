using System;
using System.Collections.Generic;

namespace Delobytes.Email
{
    public class MailMessage : IMailMessage
    {
        public MailMessage()
        {

        }

        public List<string> Attachments { get; set; } = new List<string>();

        public string Body { get; set; } = string.Empty;
        public List<string> BCC { get; set; } = new List<string>();
        public List<string> CC { get; set; } = new List<string>();
        public Guid Cd { get; set; }
        public List<string> Recipients { get; set; } = new List<string>();
        public string SenderName { get; set; }
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public string Subject { get; set; }
    }
}
