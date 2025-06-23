using System;
using System.Threading.Tasks;


namespace WayCoolStuff.MessagingFramework;

public sealed class DispatcherError : Exception
{
    public DispatcherError(string message)
        : base(message)
    {
    }
}

public interface IMessageDispatcher
{
    Task DispatchAsync(object message);
}
