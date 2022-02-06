using System;
using System.Threading;
using System.Threading.Tasks;

namespace eiscp;

public class EiscpClient : RawClient
{
    private readonly CommandRegistry _commandRegistry;

    public EiscpClient(string hostname) : base(hostname)
    {
        _commandRegistry = new CommandRegistry();
    }

    public event EventHandler<EiscpCommandEventArgs> EiscpCommand;

    protected override void OnRawCommand(string eiscp)
    {
        var handler = EiscpCommand;
        if (handler is not null)
        {
            var parsed = ParseCommand(eiscp);
            if (_commandRegistry.TryGetEiscp(parsed, out var command))
            {
                if (command.TryGetArgument(parsed, out var arg))
                {
                    var e = new EiscpCommandEventArgs
                    {
                        Zone = command.Zone,
                        Command = command.Name,
                        Parameter = arg.Name[0]
                    };
                    handler.Invoke(this, e);
                    return;
                }
            }
        }
        base.OnRawCommand(eiscp);
    }

    public override Task SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        command = _commandRegistry.TranslateToEiscp(command);
        return base.SendCommandAsync(command, cancellationToken);
    }
}