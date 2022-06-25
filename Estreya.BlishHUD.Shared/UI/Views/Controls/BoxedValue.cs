namespace Estreya.BlishHUD.Shared.UI.Views.Controls;

using System;
using System.Linq.Expressions;

public class BoxedValue<T>
{
    private T _value;
    public T Value
    {
        get => this._value;
        set
        {
            this._value = value;
            this.ValueUpdatedAction?.Invoke(this._value);
        }
    }

    private Action<T> ValueUpdatedAction { get; }

    public BoxedValue(T value, Action<T> valueUpdatedAction)
    {
        this.Value = value;
        this.ValueUpdatedAction = valueUpdatedAction;
    }
}
