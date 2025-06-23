using System;
using WayCoolStuff.MessagingFramework;


namespace SourceGenExperiment.Sample;

public sealed class SomeDumbError : Exception
{
    public SomeDumbError(string message)
        : base(message)
    {
    }
}

internal partial class SampleMessageDispatcher : IMessageDispatcher
{
    private readonly IServiceProvider _services;

    public SampleMessageDispatcher(IServiceProvider services)
    {
        _services = services;
    }
}
