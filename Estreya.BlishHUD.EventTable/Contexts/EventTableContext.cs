﻿namespace Estreya.BlishHUD.EventTable.Contexts;

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
}