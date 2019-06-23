using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Delobytes.Email
{
    public class MailKitMailer : Mailer, IMailer, IDisposable
    {
        public MailKitMailer(string outboundHost, string emailAddress, string username, string password, int port = 587) : base(outboundHost, emailAddress, username, password, port)
        {
            Init();
        }

        private SmtpClient Client { get; set; }

        private static readonly ConcurrentDictionary<string, object> MailerLockersByMailAccount = new ConcurrentDictionary<string, object>();
        private object AccountLocker => MailerLockersByMailAccount.GetOrAdd(Username, new object());

        public ManualResetEventSlim DiscoveringCompleted { get; } = new ManualResetEventSlim(false);

        private void Init()
        {

        }

        public Task DiscoverAsync(CancellationToken cancellationToken)
        {
            return DiscoverSystemAsync(cancellationToken);
        }

        private async Task<SmtpClient> GetNewClientAsync(CancellationToken cancellationToken)
        {
            SmtpClient client = new SmtpClient();

            try
            {
                await client.ConnectAsync(OutboundHost, Port, SecureSocketOptions.Auto, cancellationToken);
            }
            catch (SmtpCommandException e)
            {
                throw new Exception($"SMTP command error connecting to SMTP server {OutboundHost} on port {Port}.", e);
            }
            catch (SmtpProtocolException e)
            {
                throw new Exception($"Protocol error connecting to server {OutboundHost} on port {Port}.", e);
            }

            client.AuthenticationMechanisms.Remove("XOAUTH2");

            try
            {
                await client.AuthenticateAsync(Username, Password, cancellationToken);
            }
            catch (AuthenticationException e)
            {
                throw new Exception($"Error authenticating on server {OutboundHost}: Invalid user name or password.", e);
            }
            catch (SmtpCommandException e)
            {
                throw new Exception($"SMTP command error connecting to SMTP server {OutboundHost}.", e);
            }
            catch (SmtpProtocolException e)
            {
                throw new Exception($"Protocol error authenticating on server {OutboundHost}.", e);
            }

            return client;
        }

        private async Task DiscoverSystemAsync(CancellationToken cancellationToken)
        {
            Client = await GetNewClientAsync(cancellationToken);
            DiscoveringCompleted.Set();
        }

        private void GetClientAndRun(Action action)
        {
            try
            {
                lock (AccountLocker)
                {
                    using (Client = new SmtpClient())
                    {
                        Client.Connect(OutboundHost, Port);
                        Client.AuthenticationMechanisms.Remove("XOAUTH2");

                        try
                        {
                            Client.Authenticate(Username, Password);
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Authentication failed: ", e);
                        }

                        try
                        {
                            action();
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error running email sending action: ", e);
                        }

                        Client.Disconnect(true);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error getting SMTP client for {OutboundHost}", e);
            }
        }
        
        private DMimeMessage ConvertToMimeMessage(MailMessage mailMessage)
        {
            DMimeMessage message = new DMimeMessage { MessageCd = mailMessage.Cd };

            MailboxAddress sender = (mailMessage.SenderName != null) ? new MailboxAddress(mailMessage.SenderName, EmailAddress) : new MailboxAddress(EmailAddress);
            message.From.Add(sender);

            foreach (string email in mailMessage.Recipients)
            {
                message.To.Add(new MailboxAddress(email));
            }

            if (mailMessage.CC.Count > 0)
            {
                foreach (string email in mailMessage.CC)
                {
                    message.Cc.Add(new MailboxAddress(email));
                }
            }

            if (mailMessage.BCC.Count > 0)
            {
                foreach (string email in mailMessage.BCC)
                {
                    message.Bcc.Add(new MailboxAddress(email));
                }
            }

            message.Subject = mailMessage.Subject;

            BodyBuilder builder = new BodyBuilder { HtmlBody = mailMessage.Body };

            mailMessage.Attachments.ForEach(path =>
            {
                if (File.Exists(path))
                {
                    builder.Attachments.Add(path);
                }
            });

            message.Body = builder.ToMessageBody();

            return message;
        }

        private void Send(DMimeMessage message, int attempts = 1)
        {
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    Client.Send(message);
                }
                catch (SmtpCommandException e)
                {
                    throw new Exception("SMTP command error while sending message.", e);
                }
                catch (SmtpProtocolException e)
                {
                    throw new Exception("Protocol error while sending message.", e);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error sending email {message.MessageId} to {message.MessageCd} ({attempts} attempts taken).", e);
                }

                Thread.Sleep((attempts - 1) * 15000);
            }
        }

        private async Task<Tuple<bool,string>> SendAsync(DMimeMessage message, CancellationToken cancellationToken, int attempts = 1)
        {
            Tuple<bool, string> result = new Tuple<bool, string>(false, string.Empty);

            for (int i = 0; i < attempts; i++)
            {
                Tuple<bool, string> tryout = null;

                try
                {
                    if (Client != null)
                    {
                        await Client.SendAsync(message, cancellationToken);
                        
                        tryout = new Tuple<bool, string>(true, message.MessageId);
                    }
                    else
                    {
                        tryout = new Tuple<bool, string>(false, "SmtpClient is not available.");
                    }
                }
                catch (SmtpCommandException e)
                {
                    switch (e.ErrorCode)
                    {
                        case SmtpErrorCode.RecipientNotAccepted:
                            tryout = new Tuple<bool, string>(false, $"Recipient not accepted: {e.Message}.");
                            break;
                        case SmtpErrorCode.SenderNotAccepted:
                            tryout = new Tuple<bool, string>(false, $"Sender not accepted: {e.Message}.");
                            break;
                        case SmtpErrorCode.MessageNotAccepted:
                            tryout = new Tuple<bool, string>(false, $"Message not accepted: {e.Message}.");
                            break;
                        default:
                            tryout = new Tuple<bool, string>(false, $"Error sending email, code {e.StatusCode}, error: {e.Message}");
                            break;
                    }
                }
                catch (SmtpProtocolException e)
                {
                    tryout = new Tuple<bool, string>(false, $"Protocol error while sending message: {e.Message}.");
                }
                catch (Exception e)
                {
                    tryout = new Tuple<bool, string>(false, $"Error sending email {message.MessageId} ({attempts} attempts taken): {e.Message}.");
                }
                finally
                {
                    if (i == attempts - 1)
                    {
                        result = tryout;
                    }
                    else
                    {
                        await Task.Delay(attempts * 15000);
                    }
                }
            }

            return result;
        }

        public void Send(IMailMessage mailMessage)
        {
            try
            {
                DMimeMessage message = ConvertToMimeMessage((MailMessage)mailMessage);

                Send(message);
            }
            catch (Exception e)
            {
                throw new Exception("Error sending email " + mailMessage.Cd + ": ", e);
            }
        }

        public async Task<Tuple<bool,string>> SendAsync(IMailMessage mailMessage, CancellationToken cancellationToken = default)
        {
            Tuple<bool,string> result;

            try
            {
                DMimeMessage message = ConvertToMimeMessage((MailMessage)mailMessage);

                result = await SendAsync(message, cancellationToken);
            }
            catch (Exception e)
            {
                throw new Exception("Error sending email " + mailMessage.Cd + ": ", e);
            }

            return result;
        }

        public void SendBulk(List<IMailMessage> mailMessages)
        {
            GetClientAndRun(() =>
            {
                try
                {
                    List<DMimeMessage> messages = new List<DMimeMessage>();

                    mailMessages.Cast<MailMessage>().ToList().ForEach(m =>
                    {
                        DMimeMessage message = ConvertToMimeMessage(m);
                        messages.Add(message);
                    });

                    int maxMessageCount = 30;

                    //Office365 has a message rate limit of 30 emails per minute
                    if (messages.Count > maxMessageCount)
                    {
                        int batchCounter = 0;

                        List<List<DMimeMessage>> batches = new List<List<DMimeMessage>>(messages.SplitList(maxMessageCount));

                        foreach (List<DMimeMessage> batch in batches)
                        {
                            batchCounter++;

                            batch.ForEach(m => Send(m));

                            if (batchCounter < batches.Count)
                            {
                                Thread.Sleep(61000);
                            }
                        }
                    }
                    else
                    {
                        messages.ForEach(m => Send(m));
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Error bulk sending emails using " + OutboundHost + ": ", e);
                }
            });
        }

        public async Task SendBulkAsync(List<IMailMessage> mailMessages, CancellationToken cancellationToken = default)
        {
            try
            {
                List<DMimeMessage> messages = new List<DMimeMessage>();

                mailMessages.Cast<MailMessage>().ToList().ForEach(m =>
                {
                    DMimeMessage message = ConvertToMimeMessage(m);
                    messages.Add(message);
                });

                int maxMessageCount = 30;

                //Office365 has a message rate limit of 30 emails per minute
                if (messages.Count > maxMessageCount)
                {
                    int batchCounter = 0;

                    List<List<DMimeMessage>> batches = new List<List<DMimeMessage>>(messages.SplitList(maxMessageCount));

                    foreach (List<DMimeMessage> batch in batches)
                    {
                        batchCounter++;

                        foreach (DMimeMessage message in batch)
                        {
                            await SendAsync(message, cancellationToken);
                        }

                        if (batchCounter < batches.Count)
                        {
                            await Task.Delay(61000);
                        }
                    }
                }
                else
                {
                    foreach (DMimeMessage message in messages)
                    {
                        await SendAsync(message, cancellationToken);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error bulk sending emails using " + OutboundHost + ": ", e);
            }
        }

        public void SendCalendarEvent(IMailMessage message, DateTime startTime, DateTime endTime)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (Client != null && Client.IsConnected)
            {
                Client.Disconnect(true);
                Client.Dispose();
            }
        }
    }
}
