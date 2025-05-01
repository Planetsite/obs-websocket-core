using System;
using System.Text;

namespace WebSocketSharp;

/// <summary>
/// Represents a log data used by the <see cref="Logger"/> class.
/// </summary>
public sealed class LogData
{
    //private StackFrame _caller;
    private DateTime _date;
    private LogLevel _level;
    private string _message;

    internal LogData(LogLevel level, string message)
    {
        _level = level;
        _message = message ?? String.Empty;
        _date = DateTime.Now;
    }

    /// <summary>
    /// Gets the information of the logging method caller.
    /// </summary>
    /// <value>
    /// A <see cref="StackFrame"/> that provides the information of the logging method caller.
    /// </value>
    //public StackFrame Caller => _caller;

    /// <summary>
    /// Gets the date and time when the log data was created.
    /// </summary>
    /// <value>
    /// A <see cref="DateTime"/> that represents the date and time when the log data was created.
    /// </value>
    public DateTime Date => _date;

    /// <summary>
    /// Gets the logging level of the log data.
    /// </summary>
    /// <value>
    /// One of the <see cref="LogLevel"/> enum values, indicates the logging level of the log data.
    /// </value>
    public LogLevel Level => _level;

    /// <summary>
    /// Gets the message of the log data.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the message of the log data.
    /// </value>
    public string Message => _message;

    /// <summary>
    /// Returns a <see cref="string"/> that represents the current <see cref="LogData"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> that represents the current <see cref="LogData"/>.
    /// </returns>
    public override string ToString()
    {
        var header = $"{_date}|{_level,-5}|";
        //var method = _caller.GetMethod();
        //var type = method.DeclaringType;
        //var lineNum = _caller.GetFileLineNumber();
        var headerAndCaller = $"{header}|";
        var msgs = _message.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        if (msgs.Length <= 1)
            return $"{headerAndCaller}{_message}";

        var buff = new StringBuilder($"{headerAndCaller}{msgs[0]}\n", 64);
        var fmt = $"{{0,{header.Length}}}{{1}}\n";
        for (var i = 1; i < msgs.Length; i++)
            buff.AppendFormat(fmt, "", msgs[i]);

        buff.Length--;
        return buff.ToString();
    }
}
