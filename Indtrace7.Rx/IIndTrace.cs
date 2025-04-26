using Sharp7.Rx.Interfaces;

namespace IndTrace7.Rx;

public interface IIndTrace : IPlc
{
    new Task<TValue> GetValue<TValue>(string variableName, CancellationToken token = default);

}
