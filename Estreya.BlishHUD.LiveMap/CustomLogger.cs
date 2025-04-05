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
        string message = formatter(state, exception);
        switch (logLevel)
        {
            case LogLevel.Critical:
            case LogLevel.Error:
                _logger.Error(message);
                break;
            case LogLevel.Warning:
                _logger.Warn(message);
                break;
            case LogLevel.Information:
                _logger.Info(message);
                break;
            case LogLevel.Debug:
            case LogLevel.Trace:
                _logger.Debug(message);
                break;
            default:
                _logger.Info(message);
                break;
        }
    }
}
