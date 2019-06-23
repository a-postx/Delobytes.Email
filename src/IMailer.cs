using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Delobytes.Email
{
    public interface IMailer
    {
        ManualResetEventSlim DiscoveringCompleted { get; }

        Task DiscoverAsync(CancellationToken cancellationToken);

        void Send(IMailMessage message);
        Task<Tuple<bool,string>> SendAsync(IMailMessage message, CancellationToken cancellationToken = default);
        void SendBulk(List<IMailMessage> messages);
        Task SendBulkAsync(List<IMailMessage> messages, CancellationToken cancellationToken = default);
        void SendCalendarEvent(IMailMessage message, DateTime startTime, DateTime endTime);
    }
}
