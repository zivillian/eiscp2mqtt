// See https://aka.ms/new-console-template for more information

using System.Text;
using eiscp;
using Mono.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;

string mqttHost = null;
string mqttUsername = null;
string mqttPassword = null;
string mqttPrefix = "eiscp";
bool showHelp = false;
var hosts = new List<string>();
var clients = new Dictionary<string, EiscpClient>();
bool debug = false;
var subscriptions = new List<MqttTopicFilter>();
var options = new OptionSet
{
    {"m|mqttServer=", "MQTT Server", x => mqttHost = x},
    {"mqttuser=", "MQTT username", x => mqttUsername = x},
    {"mqttpass=", "MQTT password", x => mqttPassword = x},
    {"host=", "Receiver hostname or ip", x => hosts.Add(x)},
    {"p|prefix=", $"MQTT topic prefix - defaults to {mqttPrefix}", x => mqttPrefix = x.TrimEnd('/')},
    {"d|debug", "enable debug logging", x => debug = x != null},
    {"h|help", "show help", x => showHelp = x != null},
};
try
{
    if (options.Parse(args).Count > 0)
    {
        showHelp = true;
    }
}
catch (OptionException ex)
{
    Console.Error.Write("eiscp2mqtt: ");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Try 'eiscp2mqtt --help' for more information");
    return;
}
if (showHelp || mqttHost is null || hosts.Count == 0)
{
    options.WriteOptionDescriptions(Console.Out);
    return;
}
using (var cts = new CancellationTokenSource())
{
    Console.CancelKeyPress += (s, e) =>
    {
        cts.Cancel();
        e.Cancel = true;
    };
    using (var mqttClient = new MqttFactory().CreateMqttClient())
    {
        var mqttOptionBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttHost)
            .WithClientId($"eiscp2mqtt");
        if (!String.IsNullOrEmpty(mqttUsername) || !String.IsNullOrEmpty(mqttPassword))
        {
            mqttOptionBuilder = mqttOptionBuilder.WithCredentials(mqttUsername, mqttPassword);
        }
        var mqttOptions = mqttOptionBuilder.Build();
        mqttClient.DisconnectedAsync += async e =>
        {
            Console.Error.WriteLine("mqtt disconnected - reconnecting in 5 seconds");
            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
            try
            {
                await mqttClient.ConnectAsync(mqttOptions, cts.Token);
                await mqttClient.SubscribeAsync(new MqttClientSubscribeOptions { TopicFilters = subscriptions }, cts.Token);
            }
            catch
            {
                Console.Error.WriteLine("reconnect failed");
            }
        };
        await mqttClient.ConnectAsync(mqttOptions, cts.Token);
        mqttClient.ApplicationMessageReceivedAsync += x=>MqttMessageReceived(x, cts.Token);
        await RunConnectionsAsync(mqttClient, cts.Token);
    }
}

Task RunConnectionsAsync(IMqttClient mqttClient, CancellationToken cancellationToken)
{
    var tasks = new List<Task>();
    foreach (var host in hosts)
    {
        tasks.Add(RunConnectionAsync(mqttClient, host, cancellationToken));
    }
    return Task.WhenAll(tasks);
}

async Task RunConnectionAsync(IMqttClient mqttClient,string hostname, CancellationToken cancellationToken)
{
    var subscription = new MqttClientSubscribeOptions
    {
        TopicFilters =
        {
            new MqttTopicFilter{Topic = $"{mqttPrefix}/{hostname}"}
        }
    };
    subscriptions.AddRange(subscription.TopicFilters);
    await mqttClient.SubscribeAsync(subscription, cancellationToken);
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            using (var client = new EiscpClient(hostname))
            {
                if (debug)
                {
                    client.Debug += (s, e) => Console.Error.WriteLine($"{hostname}: {(e.Send ? '>' : '<')} {e.Command}");
                }
                client.RawCommand += (s, e) => OnRawCommand(mqttClient, $"{mqttPrefix}/{hostname}/raw", e.Command);
                client.EiscpCommand += (s, e) => OnEiscpCommand(mqttClient, hostname, e);
                await client.ConnectAsync(cancellationToken);
                clients[hostname] = client;
                
                await PublishAsync(mqttClient, $"{mqttPrefix}/{hostname}/state", "connected", cancellationToken);
                await client.RunAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await PublishAsync(mqttClient, $"{mqttPrefix}/{hostname}/error", ex.Message, cancellationToken);
        }
        await PublishAsync(mqttClient, $"{mqttPrefix}/{hostname}/state", "disconnected", cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
    }
}

void OnEiscpCommand(IMqttClient mqttClient, string hostname, EiscpCommandEventArgs e)
{
    var topic = $"{mqttPrefix}/{hostname}/{e.Zone.ToString().ToLowerInvariant()}/{e.Command}";
    PublishAsync(mqttClient, topic, e.Parameter, CancellationToken.None);
}

void OnRawCommand(IMqttClient mqttClient, string topic, string command)
{
    PublishAsync(mqttClient, topic, command, CancellationToken.None);
}

async Task MqttMessageReceived(MqttApplicationMessageReceivedEventArgs args, CancellationToken cancellationToken)
{
    if (args.ApplicationMessage.Payload is null) return;
    var topic = args.ApplicationMessage.Topic.Substring(mqttPrefix.Length + 1);
    var host = topic;
    var index = host.IndexOf('/');
    if (index != -1)
    {
        host = topic.Substring(0, index);
    }
    if (clients.TryGetValue(host, out var client))
    {
        var command = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
        if (debug)
            Console.Error.WriteLine($"Received '{command}' on '{args.ApplicationMessage.Topic}'");
        await client.SendCommandAsync(command, cancellationToken);
    }

}

static Task PublishAsync(IMqttClient client, string topic, string message, CancellationToken cancellationToken)
{
    if (!client.IsConnected)
    {
        Console.Error.WriteLine($"MQTT disconnected - dropping '{topic}': '{message}'");
        return Task.CompletedTask;
    }
    var payload = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(message)
        .WithContentType("text/plain")
        .Build();
    return client.PublishAsync(payload, cancellationToken);
}