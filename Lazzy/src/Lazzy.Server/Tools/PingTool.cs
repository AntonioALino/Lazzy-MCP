using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;

namespace Lazzy.Server.Tools;

[McpServerToolType]
internal static class PingTool
{
    [McpServerTool(Name = "ping", ReadOnly = true)]
    [Description(
        "Checks that the Lazzy MCP server is running and returns its current date and time with the UTC offset.")]
    public static string Ping(TimeProvider timeProvider) =>
        timeProvider.GetLocalNow().ToString("O", CultureInfo.InvariantCulture);
}