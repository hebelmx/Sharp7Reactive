using System;
namespace IndTrace7.Rx;

public class SystemDateTime : IDateTime
{
    private readonly TimeProvider timeProvider;

    private SystemDateTime(TimeProvider? timeProvider)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DateTime Now => timeProvider.GetLocalNow().DateTime;
    public DateTime UtcNow => timeProvider.GetUtcNow().DateTime;
}
