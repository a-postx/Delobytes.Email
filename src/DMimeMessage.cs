using System;
using MimeKit;

namespace Delobytes.Email
{
    public class DMimeMessage : MimeMessage
    {
        public Guid MessageCd { get; set; }
    }
}
