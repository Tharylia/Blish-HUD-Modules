namespace Estreya.BlishHUD.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class IgnoreCopyAttribute : Attribute
{
}
