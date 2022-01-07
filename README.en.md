# Delobytes.Email
Simple email sending services based on .Net.

[RU](README.md), [EN](README.en.md)

## Usage
Add proper services to use in your project. In case you've added Microsoft.Extensions.Logging based logging then the service will log its activities to a common log.


### Classic SMTP server
Add SMTP service in the service collection to send emails via standard SMTP protocol:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    //...
    services.AddSmtpMailer(() =>
        {
            SmtpEmailOptions emailOptions = new SmtpEmailOptions
            {
                ServiceLifetime = ServiceLifetime.Transient,
                SmtpServer = "smtp.office365.com",
                SmtpUsername = "myuser",
                SmtpPassword = "myPassw0rd"
            };

            return emailOptions;
        });
}
```

Once done, you will be able to inject service ISmtpMailer and use it in your code:

```csharp
[Route("[controller]")]
[ApiController]
public class EmailSendingController : ControllerBase
{
    [HttpPost("sendmessage")]
    public async Task<IActionResult> SendEmailMessageAsync(
        [FromServices] ISmtpMailer mailer,
        [FromBody] EmailMessage emailMessage,
        CancellationToken cancellationToken)
    {
        await mailer.SendAsync(emailMessage, cancellationToken: cancellationToken);
        return new OkResult();
    }
}
```


## License
[МИТ](https://github.com/a-postx/Delobytes.Email/blob/master/LICENSE)