namespace Estreya.BlishHUD.Shared.Threading.Events;

using System.Threading.Tasks;

public delegate Task AsyncEventHandler(object sender);

public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

public delegate Task<TReturn> AsyncReturnEventHandler<TReturn>(object sender);

public delegate Task<TReturn> AsyncReturnEventHandler<TEventArgs, TReturn>(object sender, TEventArgs e);