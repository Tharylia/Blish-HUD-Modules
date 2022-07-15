namespace Estreya.BlishHUD.Shared.Threading.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);
