using System;
using System.IO;
using System.Threading.Tasks;
#nullable enable
namespace WebSocketSharp;

/// <summary>
/// Provides a set of methods and properties for logging.
/// </summary>
/// <remarks>
///   <para>
///   If you output a log with lower than the value of the <see cref="Logger.Level"/> property,
///   it cannot be outputted.
///   </para>
///   <para>
///   The default output action writes a log to the standard output stream and the log file
///   if the <see cref="Logger.File"/> property has a valid path to it.
///   </para>
///   <para>
///   If you would like to use the custom output action, you should set
///   the <see cref="Logger.OutputAsync"/> property to any <c>Action&lt;LogData, string&gt;</c>
///   delegate.
///   </para>
/// </remarks>
public sealed class Logger
{
    public Func<string, Task>? OutputExceptionAsync;

    private volatile string _file;
    private volatile LogLevel _loggerLevel;
    private Func<LogData, string, Task> _outputAsync;
    private object _sync;

    /// <summary>
    /// Initializes a new instance of the <see cref="Logger"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor initializes the current logging level with <see cref="LogLevel.Error"/>.
    /// </remarks>
    public Logger() : this(LogLevel.Error, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Logger"/> class with
    /// the specified logging <paramref name="level"/>.
    /// </summary>
    /// <param name="level">
    /// One of the <see cref="LogLevel"/> enum values.
    /// </param>
    public Logger(LogLevel level) : this(level, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Logger"/> class with
    /// the specified logging <paramref name="level"/>, path to the log <paramref name="file"/>,
    /// and <paramref name="outputAsync"/> action.
    /// </summary>
    /// <param name="level">
    /// One of the <see cref="LogLevel"/> enum values.
    /// </param>
    /// <param name="file">
    /// A <see cref="string"/> that represents the path to the log file.
    /// </param>
    /// <param name="outputAsync">
    /// A <c>Func&lt;LogData, string, Task&gt;</c> delegate that references the method(s) used to
    /// output a log. A <see cref="string"/> parameter passed to this delegate is
    /// <paramref name="file"/>.
    /// </param>
    public Logger(LogLevel level, string file, Func<LogData, string, Task> outputAsync)
    {
        _loggerLevel = level;
        _file = file;
        _outputAsync = outputAsync ?? DefaultOutputAsync;
        _sync = new object();
    }

    /// <summary>
    /// Gets or sets the current path to the log file.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the current path to the log file if any.
    /// </value>
    public string File
    {
        get => _file;

        set
        {
            lock (_sync)
            {
                _file = value;
                Warn($"The current path to the log file has been changed to {_file}.");
            }
        }
    }

    /// <summary>
    /// Gets or sets the current logging level.
    /// </summary>
    /// <remarks>
    /// A log with lower than the value of this property cannot be outputted.
    /// </remarks>
    /// <value>
    /// One of the <see cref="LogLevel"/> enum values, specifies the current logging level.
    /// </value>
    public LogLevel Level
    {
        get => _loggerLevel;

        set
        {
            lock (_sync)
            {
                _loggerLevel = value;
                Warn($"The current logging level has been changed to {_loggerLevel}.");
            }
        }
    }

    /// <summary>
    /// Gets or sets the current output action used to output a log.
    /// </summary>
    /// <value>
    ///   <para>
    ///   An <c>Action&lt;LogData, string&gt;</c> delegate that references the method(s) used to
    ///   output a log. A <see cref="string"/> parameter passed to this delegate is the value of
    ///   the <see cref="Logger.File"/> property.
    ///   </para>
    ///   <para>
    ///   If the value to set is <see langword="null"/>, the current output action is changed to
    ///   the default output action.
    ///   </para>
    /// </value>
    public Func<LogData, string, Task> OutputAsync
    {
        get => _outputAsync;

        set
        {
            lock (_sync)
            {
                _outputAsync = value ?? DefaultOutputAsync;
                Warn("The current output action has been changed.");
            }
        }
    }

    private static async Task DefaultOutputAsync(LogData data, string path)
    {
        var log = data.ToString();
        if (path != null && path.Length > 0)
            await WriteToFileAsync(log, path);
    }

    private async Task InternalOutputAsync(string message, LogLevel messageLevel)
    {
        //lock (_sync)
        {
            if (_loggerLevel > messageLevel)
                return;

            LogData data;
            try
            {
                data = new LogData(messageLevel, message);
                await _outputAsync(data, _file);
            }
            catch (Exception writeErr)
            {
                data = new LogData(LogLevel.Fatal, writeErr.Message);
                if (OutputExceptionAsync != null)
                    await OutputExceptionAsync(data.ToString());
            }
        }
    }

    private static async Task WriteToFileAsync(string value, string path)
    {
        using var writer = new StreamWriter(path, true);
        using var syncWriter = TextWriter.Synchronized(writer);
        await syncWriter.WriteLineAsync(value);
    }

    /// <summary>
    /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Debug"/>.
    /// </summary>
    /// <remarks>
    /// If the current logging level is higher than <see cref="LogLevel.Debug"/>,
    /// this method doesn't output <paramref name="message"/> as a log.
    /// </remarks>
    /// <param name="message">
    /// A <see cref="string"/> that represents the message to output as a log.
    /// </param>
    public void Debug(string message)
    {
        if (_loggerLevel > LogLevel.Debug)
            return;

        _ = InternalOutputAsync(message, LogLevel.Debug);
    }

    /// <summary>
    /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Error"/>.
    /// </summary>
    /// <remarks>
    /// If the current logging level is higher than <see cref="LogLevel.Error"/>,
    /// this method doesn't output <paramref name="message"/> as a log.
    /// </remarks>
    /// <param name="message">
    /// A <see cref="string"/> that represents the message to output as a log.
    /// </param>
    public void Error(string message)
    {
        if (_loggerLevel > LogLevel.Error)
            return;

        _ = InternalOutputAsync(message, LogLevel.Error);
    }

    /// <summary>
    /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Fatal"/>.
    /// </summary>
    /// <param name="message">
    /// A <see cref="string"/> that represents the message to output as a log.
    /// </param>
    public void Fatal(string message)
        => _ = InternalOutputAsync(message, LogLevel.Fatal);

    /// <summary>
    /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Info"/>.
    /// </summary>
    /// <remarks>
    /// If the current logging level is higher than <see cref="LogLevel.Info"/>,
    /// this method doesn't output <paramref name="message"/> as a log.
    /// </remarks>
    /// <param name="message">
    /// A <see cref="string"/> that represents the message to output as a log.
    /// </param>
    public void Info(string message)
    {
        if (_loggerLevel > LogLevel.Info)
            return;

        _ = InternalOutputAsync(message, LogLevel.Info);
    }

    /// <summary>
    /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Trace"/>.
    /// </summary>
    /// <remarks>
    /// If the current logging level is higher than <see cref="LogLevel.Trace"/>,
    /// this method doesn't output <paramref name="message"/> as a log.
    /// </remarks>
    /// <param name="message">
    /// A <see cref="string"/> that represents the message to output as a log.
    /// </param>
    public void Trace(string message)
    {
        if (_loggerLevel > LogLevel.Trace)
            return;

        _ = InternalOutputAsync(message, LogLevel.Trace);
    }

    /// <summary>
    /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Warn"/>.
    /// </summary>
    /// <remarks>
    /// If the current logging level is higher than <see cref="LogLevel.Warn"/>,
    /// this method doesn't output <paramref name="message"/> as a log.
    /// </remarks>
    /// <param name="message">
    /// A <see cref="string"/> that represents the message to output as a log.
    /// </param>
    public void Warn(string message)
    {
        if (_loggerLevel > LogLevel.Warn)
            return;

        _ = InternalOutputAsync(message, LogLevel.Warn);
    }
}
