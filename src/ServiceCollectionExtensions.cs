namespace Delobytes.Email;

/// <summary>
/// Методы расширения <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет классичекий сервис посылки сообщений электропочты по протоколу SMTP.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="optionsFunc">Настройки.</param>
    /// <returns></returns>
    public static IServiceCollection AddSmtpMailer(this IServiceCollection services, Func<SmtpEmailOptions> optionsFunc)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(optionsFunc, nameof(optionsFunc));

        SmtpEmailOptions options = optionsFunc.Invoke();
        services.AddSingleton(x => options);

        switch (options.ServiceLifetime)
        {
            case ServiceLifetime.Transient:
                services.AddTransient<ISmtpMailer, MailKitMailer>();
                break;
            default:
            case ServiceLifetime.Scoped:
                services.AddScoped<ISmtpMailer, MailKitMailer>();
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<ISmtpMailer, MailKitMailer>();
                break;
        }

        return services;
    }
}
