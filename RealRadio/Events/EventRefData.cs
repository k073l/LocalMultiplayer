namespace RealRadio.Events;

public class EventRefData<T>(T? value = default)
{
    public T? Value { get; set; } = value;
}
