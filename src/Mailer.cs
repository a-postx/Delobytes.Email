namespace Delobytes.Email
{
    public class Mailer
    {
        protected Mailer(string outboundHost, string emailAddress, string username, string password, int port)
        {
            OutboundHost = outboundHost;
            EmailAddress = emailAddress;
            Username = username;
            Password = password;
            Port = port;
        }

        protected string EmailAddress { get; } 
        protected string OutboundHost { get; }
        protected string Username { get; }
        protected string Password { get; }
        protected int Port { get; set; }
        protected int RetryCount { get; set; } = 3;
    }
}
