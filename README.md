# Delobytes.Email
Простой сервис отправки электропочтовых сообщений на базе .Net.

[RU](README.md), [EN](README.en.md)

## Использование
Добавьте соответствующий сервис, чтобы использовать его в вашем проекте. Если в проекте подключено логирование на базе Microsoft.Extensions.Logging, то сервис также будет логировать свою активность.


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
                ServiceLifetime = ServiceLifetime.Scoped,
                SmtpServer = options.MailServer,
                SmtpUsername = options.MailUser,
                SmtpPassword = options.MailPassword,
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