namespace Estreya.BlishHUD.Shared.Exceptions;

using System;

/// <summary>
///     An exception used to indicate an invalid module state.
/// </summary>
public class ModuleInvalidException : Exception
{
    public ModuleInvalidException(string message) : base(message ?? "This module is invalid due to unknown reasons. (This is a custom module exception)") { }
}