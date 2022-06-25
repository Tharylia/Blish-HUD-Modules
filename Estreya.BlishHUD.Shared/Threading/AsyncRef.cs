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

    public static implicit operator T(AsyncRef<T> r) { return r.Value; }
    public static implicit operator AsyncRef<T>(T value) { return new AsyncRef<T>(value); }
}
