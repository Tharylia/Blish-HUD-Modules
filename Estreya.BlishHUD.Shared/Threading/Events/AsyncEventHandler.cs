namespace Estreya.BlishHUD.Shared.Threading.Events;

using System.Threading.Tasks;

public delegate Task AsyncEventHandler(object sender);

public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);