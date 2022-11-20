using System;
using System.Collections.Generic;
using System.Linq;

namespace eiscp;

public partial class CommandRegistry
{
    private readonly IList<EiscpCommandBase> _commands = new List<EiscpCommandBase>();

    public CommandRegistry()
    {
        RegisterCommands();
    }

    partial void RegisterCommands();

    public bool TryGetEiscp(string eiscp, out EiscpCommandBase command)
    {
        var group = eiscp.Substring(0, 3);
        command = _commands.Where(x => x.Eiscp == group)
            .OrderBy(x => x.Zone)
            .FirstOrDefault();
        return command is not null;
    }

    private static readonly char[] CommandSeparator = new[] { ' ', '\t', '.', '=', ':' };
    public string TranslateToEiscp(string command)
    {
        var parts = command.Split(CommandSeparator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IEnumerable<EiscpCommandBase> query = _commands;
        int offset = 0;
        if (parts.Length == 3)
        {
            if (Enum.TryParse(parts[0], true, out EiscpZone zone))
            {
                query = _commands.Where(x => x.Zone == zone);
                offset = 1;
            }
            else
            {
                return command;
            }
        }
        else if (parts.Length != 2)
        {
            return command;
        }
        query = query.Where(x => x.Name.Equals(parts[offset], StringComparison.OrdinalIgnoreCase) ||
                                 x.Aliases.Any(a => a.Equals(parts[offset], StringComparison.OrdinalIgnoreCase)));

        var argument = parts[offset + 1];
        foreach (var eiscpCommand in query)
        {
            if (eiscpCommand.TryTranslateToEiscp(argument, out var eiscp))
            {
                return eiscp;
            }
        }
        return command;
    }
}