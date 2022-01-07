# Delobytes.Email
Простые сервисы посылки электропочтовых сообщений на базе .Net Core.

[RU](README.md), [EN](README.en.md)

## Использование
Добавьте соответствующие сервисы, чтобы использовать их в вашем проекте.


### Классический SMTP-сервер
Для отправки писем с помощью стандартного SMTP-протокола, добавьте соответствующий сервис в коллекцию:

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

После этого вы сможете получать сервис ISmtpMailer с помощью внедрения зависимостей и использовать его в вашем коде:

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


## Лицензия
[МИТ](https://github.com/a-postx/Delobytes.Email/blob/master/LICENSE)