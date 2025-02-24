namespace Estreya.BlishHUD.LiveMap;

using Blish_HUD;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class CustomLogger : ILogger
{
    private Logger _logger = Logger.GetLogger(typeof(CustomLogger));
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return default;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _logger.Info(formatter(state, exception));
    }
}
