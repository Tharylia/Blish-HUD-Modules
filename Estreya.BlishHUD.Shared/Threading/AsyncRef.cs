namespace Estreya.BlishHUD.Shared.Threading;

public class AsyncRef<T>
{
    public AsyncRef() { }

    public AsyncRef(T value) { this.Value = value; }

    public T Value { get; set; }

    public override string ToString()
    {
        T value = this.Value;
        return value == null ? "" : value.ToString();
    }
}