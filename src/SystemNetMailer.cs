using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Delobytes.Email
{
    public class SystemNetMailer : Mailer, IMailer
    {
        public SystemNetMailer(string outboundHost, string address, string username, string password, int port = 587) : base(outboundHost, address, username, password, port)
        {
            Init();
        }

        private SmtpClient Client { get; set; }
        
        private static readonly ConcurrentDictionary<string, object> MailerLockersByMailAccount = new ConcurrentDictionary<string, object>();
        private object _accountLocker => MailerLockersByMailAccount.GetOrAdd(Username, new object());

        public ManualResetEventSlim DiscoveringCompleted { get; } = new ManualResetEventSlim(false);

        private void Init()
        {

        }

        public Task DiscoverAsync(CancellationToken cancellationToken)
        {
            return DiscoverSystem();
        }

        private Task DiscoverSystem()
        {
            return Task.Run(() =>
            {
                SmtpClient client = CheckConnection();

                if (client == null)
                {
                    throw new Exception("SystemNetMailer system is not avalable: please check credentials");
                }

                DiscoveringCompleted.Set();
            });
        }

        private SmtpClient CheckConnection()
        {
            throw new NotImplementedException();
        }

        private void GetClientAndRun(Action action)
        {
            try
            {
                lock (_accountLocker)
                {
                    using (Client = new SmtpClient(OutboundHost, Port))
                    {
                        Client.EnableSsl = true;
                        Client.Credentials = new NetworkCredential { UserName = Username, Password = Password };

                        try
                        {
                            action();
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error running email sending action: ", e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Can't get SMTP client for " + OutboundHost + ": ", e);
            }
        }

        private void Send(System.Net.Mail.MailMessage message, int attempts = 1)
        {
            GetClientAndRun(() =>
            {
                try
                {
                    Client.Send(message);
                }
                catch (Exception e)
                {
                    if (attempts < RetryCount)
                    {
                        Thread.Sleep(attempts * 15000);

                        Send(message, attempts + 1);
                    }
                    else
                    {
                        throw new Exception($"Error sending email to {message.To} ({attempts} attempts taken).", e);
                    }
                }
            });
        }

        private System.Net.Mail.MailMessage ConvertToMailMessage(MailMessage emailMessage)
        {
            System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
            MailAddress from = emailMessage.SenderName != null ? new MailAddress(Username, emailMessage.SenderName) : new MailAddress(Username);
            message.From = from;

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

            emailMessage.Attachments.ForEach(path =>
            {
                if (File.Exists(path))
                {
                    message.Attachments.Add(new Attachment(path));
                }
            });

            return message;
        }

        public void Send(IMailMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<bool,string>> SendAsync(IMailMessage message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void SendBulk(List<IMailMessage> messages)
        {
            throw new NotImplementedException();
        }

        public Task SendBulkAsync(List<IMailMessage> messages, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void SendCalendarEvent(IMailMessage emailMessage, DateTime startTime, DateTime endTime)
        {
            try
            {
                System.Net.Mail.MailMessage message = ConvertToMailMessage((MailMessage)emailMessage);

                message.Headers.Add("Content-class", "urn:content-classes:calendarmessage");

                StringBuilder str = new StringBuilder();
                str.AppendLine("BEGIN:VCALENDAR");
                str.AppendLine("PRODID:- Acumatica Business Cloud event");
                str.AppendLine("VERSION:2.0");
                str.AppendLine("METHOD:REQUEST");
                str.AppendLine("BEGIN:VEVENT");
                str.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmssZ}", DateTime.UtcNow));
                str.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", startTime));
                str.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", endTime));
                str.AppendLine("LOCATION: Online");
                str.AppendLine(string.Format("UID:{0}", emailMessage.Cd.ToString()));
                str.AppendLine(string.Format("DESCRIPTION:{0}", message.Body));
                str.AppendLine(string.Format("X-ALT-DESC;FMTTYPE=text/html:{0}", message.Body));
                str.AppendLine(string.Format("SUMMARY:{0}", message.Subject));
                str.AppendLine(string.Format("ORGANIZER:MAILTO:{0}", message.From.Address));

                str.AppendLine(string.Format("ATTENDEE;CN=\"{0}\";RSVP=TRUE:mailto:{1}", message.To[0].DisplayName, message.To[0].Address));

                str.AppendLine("BEGIN:VALARM");
                str.AppendLine("TRIGGER:-PT15M");
                str.AppendLine("ACTION:DISPLAY");
                str.AppendLine("DESCRIPTION:Reminder");
                str.AppendLine("END:VALARM");
                str.AppendLine("END:VEVENT");
                str.AppendLine("END:VCALENDAR");

                System.Net.Mime.ContentType contype = new System.Net.Mime.ContentType("text/calendar");
                contype.Parameters?.Add("method", "REQUEST");
                contype.Parameters?.Add("name", "Meeting.ics");
                AlternateView avCal = AlternateView.CreateAlternateViewFromString(str.ToString(), contype);
                message.AlternateViews.Add(avCal);

                Send(message);
            }
            catch (Exception e)
            {
                throw new Exception("Error sending calendar event: ", e);
            }
        }
    }
}
