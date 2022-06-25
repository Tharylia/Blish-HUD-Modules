namespace Estreya.BlishHUD.Shared.UI.Views.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class TypeOverrideAttribute : Attribute
{
    public Type Type { get; }

    public TypeOverrideAttribute(Type type)
    {
        this.Type = type;
    }
}
