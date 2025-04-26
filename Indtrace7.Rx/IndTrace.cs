using Sharp7.Rx.Interfaces;
using System;

namespace IndTrace7.Rx;
public interface IndTrace : IPlc
{
    Task<TValue> GetValuePlc<TValue>(string variableName, CancellationToken token = default);

}
