using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace AccountService.Api.Logging;

public class LogFormatter : ITextFormatter
{
    private readonly JsonFormatter _inner = new(renderMessage: true);

    public void Format(LogEvent logEvent, TextWriter output)
    {
        _inner.Format(logEvent, output);
        output.WriteLine("------");
    }
}