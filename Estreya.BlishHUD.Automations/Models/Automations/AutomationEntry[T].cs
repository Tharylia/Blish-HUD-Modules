namespace Estreya.BlishHUD.Automations.Models.Automations;

using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class AutomationEntry<TActionInput> : AutomationEntry
{
    protected AsyncLock _actionLock = new AsyncLock();

    protected List<Func<TActionInput, Task>> Actions { get; set; }

    public AutomationEntry(/*AutomationType type, */string name) : base(/*type,*/ name)
    {
        using (this._actionLock.Lock())
        {
            this.Actions = new List<Func<TActionInput, Task>>();
        }
    }

    public virtual async Task Execute(TActionInput actionInput, IFlurlClient flurlClient, Gw2ApiManager apiManager)
    {
        using (this._actionLock.Lock())
        {
            var tasks = this.Actions.Select(a => a.Invoke(actionInput));
            await Task.WhenAll(tasks);
        }
    }

    public void AddAction(Action<TActionInput> action)
    {
        using (this._actionLock.Lock())
        {
            this.Actions.Add((input) =>
            {
                action?.Invoke(input);
                return Task.CompletedTask;
            });
        }
    }

    public void AddAction(Func<TActionInput, Task> action)
    {
        using (this._actionLock.Lock())
        {
            this.Actions.Add(action);
        }
    }
}
