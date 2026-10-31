using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// No arguments on purpose: Lazzy's own flags (like --demo) are not configuration keys.
var builder = Host.CreateApplicationBuilder();

// With the stdio transport, stdout is the MCP protocol channel.
// Every log level goes to stderr, otherwise the client disconnects.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// The only clock in the application. Nothing reads DateTime.Now directly.
builder.Services.AddSingleton(TimeProvider.System);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();