namespace Estreya.BlishHUD.EventTable.Contexts;

using Blish_HUD.Contexts;
using Estreya.BlishHUD.Shared.Threading.Events;
using Estreya.BlishHUD.Shared.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Framework.Utilities.Deflate;
using System.Windows.Input;

public class EventTableContext : BaseContext
{
    internal event AsyncEventHandler<ContextEventArgs<AddCategory>> RequestAddCategory;
    internal event AsyncEventHandler<ContextEventArgs<string>> RequestRemoveCategory;
    internal event AsyncEventHandler<ContextEventArgs<AddEvent>> RequestAddEvent;
    internal event AsyncEventHandler<ContextEventArgs<RemoveEvent>> RequestRemoveEvent;
    internal event AsyncEventHandler<ContextEventArgs> RequestReloadEvents;
    internal event AsyncEventHandler<ContextEventArgs<ShowReminder>> RequestShowReminder;
    internal event AsyncEventHandler<ContextEventArgs<AddDynamicEvent>> RequestAddDynamicEvent;
    internal event AsyncEventHandler<ContextEventArgs<Guid>> RequestRemoveDynamicEvent;
    internal event AsyncReturnEventHandler<ContextEventArgs, IEnumerable<string>> RequestEventSettingKeys;
    internal event AsyncReturnEventHandler<ContextEventArgs, IEnumerable<string>> RequestAreaNames;
    internal event AsyncEventHandler<ContextEventArgs<AddEventState>> RequestAddEventState;
    internal event AsyncEventHandler<ContextEventArgs<RemoveEventState>> RequestRemoveEventState;

    /// <summary>
    /// Adds a new category to the event table module. Shows up for the table and reminders.
    /// </summary>
    /// <param name="newCategory">The category to add</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">Throws if event table has not registered this method.</exception>
    public async Task AddCategory(AddCategory newCategory)
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(AddCategory)}(\"{newCategory.Name} ({newCategory.Key})\").");
        await (this.RequestAddCategory?.Invoke(this, new ContextEventArgs<AddCategory>(caller, newCategory)) ?? Task.FromException(new NotImplementedException()));
    }

    /// <summary>
    /// Removes the category with the specified key.
    /// </summary>
    /// <param name="key">The category key to be removed.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">Throws if event table has not registered this method.</exception>
    public async Task RemoveCategory(string key)
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(RemoveCategory)}(\"{key}\").");
        await (this.RequestRemoveCategory?.Invoke(this, new ContextEventArgs<string>(caller, key)) ?? Task.FromException(new NotImplementedException()));
    }

    /// <summary>
    /// Adds a new event to the category in the event table module. Shows up for the table and reminders.
    /// </summary>
    /// <param name="newEvent"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">Throws if event table has not registered this method.</exception>
    public async Task AddEvent(AddEvent newEvent)
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(AddEvent)}(\"{newEvent.CategoryKey}\", \"{newEvent.Name} ({newEvent.Key})\").");
        await (this.RequestAddEvent?.Invoke(this, new ContextEventArgs<AddEvent>(caller, newEvent)) ?? Task.FromException(new NotImplementedException()));
    }

    /// <summary>
    /// Removes the event from the specified category.
    /// </summary>
    /// <param name="removeEvent"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">Throws if event table has not registered this method.</exception>
    public async Task RemoveEvent(RemoveEvent removeEvent)
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(RemoveEvent)}(\"{removeEvent.CategoryKey}\", \"{removeEvent.EventKey}\").");
        await (this.RequestRemoveEvent?.Invoke(this, new ContextEventArgs<RemoveEvent>(caller, removeEvent)) ?? Task.FromException(new NotImplementedException()));
    }

    /// <summary>
    /// Triggers a reload of the event table module.
    /// <para/>
    /// Use with caution as exessive usage can cause getting rate limited.
    /// </summary>
    /// <exception cref="TimeoutException">Throws if reload does not happen in specified timespan.</exception>
    /// <exception cref="NotImplementedException">Throws if event table has not registered this method.</exception>
    public async Task ReloadEvents()
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(ReloadEvents)}().");
        await (this.RequestReloadEvents?.Invoke(this, new ContextEventArgs(caller)) ?? Task.FromException(new NotImplementedException()));
    }

    /// <summary>
    /// Shows a reminder.
    /// </summary>
    /// <param name="reminder">The reminder information to display.</param>
    /// <exception cref="NotImplementedException">Throws if event table has not registered this method.</exception>
    public async Task ShowReminder(ShowReminder reminder)
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(ShowReminder)}().");
        await (this.RequestShowReminder?.Invoke(this, new ContextEventArgs<ShowReminder>(caller, reminder)) ?? Task.FromException(new NotImplementedException()));
    }

    /// <summary>
    /// Adds a new dynamic event. 
    /// <para/>
    /// All custom dynamic events are persisted between sessions and do not need to be added everytime.<br/>
    /// If you need to add them every time, be sure to call <see cref="RemoveDynamicEvent(string)"/> on unload.
    /// </summary>
    /// <param name="addDynamicEvent"></param>
    public async Task AddDynamicEvent(AddDynamicEvent addDynamicEvent)
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(AddDynamicEvent)}(\"{addDynamicEvent.Name} ({addDynamicEvent.Id})\").");
        await (this.RequestAddDynamicEvent?.Invoke(this, new ContextEventArgs<AddDynamicEvent>(caller, addDynamicEvent)) ?? Task.FromException(new NotImplementedException()));
    }

    /// <summary>
    /// Removes a dynamic event with the specified id.
    /// </summary>
    /// <param name="id">The id of the dynamic event to delete.</param>
    public async Task RemoveDynamicEvent(Guid id)
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(RemoveDynamicEvent)}(\"{id}\").");
        await (this.RequestRemoveDynamicEvent?.Invoke(this, new ContextEventArgs<Guid>(caller, id)) ?? Task.FromException(new NotImplementedException()));
    }

    /// <summary>
    /// Gets a list of all currently loaded event setting keys.
    /// </summary>
    /// <returns>A list of event setting keys.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<IEnumerable<string>> GetEventSettingKeys()
    {
        this.CheckReady();
        var caller = this.GetCaller();
        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(GetEventSettingKeys)}().");

        if (this.RequestEventSettingKeys is null) throw new NotImplementedException();

        var keys = await this.RequestEventSettingKeys.Invoke(this, new ContextEventArgs(caller));

        return keys;
    }

    /// <summary>
    /// Gets the names of all available areas. The enabled state of the areas is not checked.
    /// </summary>
    /// <returns>A list of area names.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<IEnumerable<string>> GetAreaNames()
    {
        this.CheckReady();
        var caller = this.GetCaller();
        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(GetAreaNames)}().");

        if (this.RequestAreaNames is null) throw new NotImplementedException();

        var areaNames = await this.RequestAreaNames.Invoke(this, new ContextEventArgs(caller));

        return areaNames;
    }

    /// <summary>
    /// Adds an event state for a specific event inside the area.
    /// </summary>
    /// <param name="addEventState"></param>
    /// <returns></returns>
    public async Task AddEventState(AddEventState addEventState)
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(AddEventState)}(\"{addEventState.AreaName}\",\"{addEventState.EventKey}\",\"{addEventState.Until.ToUniversalTime()}\").");
        await (this.RequestAddEventState?.Invoke(this, new ContextEventArgs<AddEventState>(caller, addEventState)) ?? Task.FromException(new NotImplementedException()));
    }

    /// <summary>
    /// Removes an event state for a specific event inside the area.
    /// </summary>
    /// <param name="removeEventState"></param>
    /// <returns></returns>
    public async Task RemoveEventState(RemoveEventState removeEventState)
    {
        this.CheckReady();
        var caller = this.GetCaller();

        this.Logger.Info($"\"{caller.FullName}\" triggered a context action: {nameof(RemoveEventState)}(\"{removeEventState.AreaName}\",\"{removeEventState.EventKey}\").");
        await (this.RequestRemoveEventState?.Invoke(this, new ContextEventArgs<RemoveEventState>(caller, removeEventState)) ?? Task.FromException(new NotImplementedException()));
    }
}
