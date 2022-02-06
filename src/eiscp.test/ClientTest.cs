using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace eiscp.test
{
    public class ClientTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ClientTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task CanConnect()
        {
            var resetEvent = new ManualResetEventSlim();
            var client = new RawClient("localhost");
            client.RawCommand += (s,e)=>
            {
                _testOutputHelper.WriteLine(e.Command);
                if (e.Command.StartsWith("PWR"))
                    resetEvent.Set();
            };
            using (var cts = new CancellationTokenSource())
            {
                await client.ConnectAsync(cts.Token);
                var connection = client.RunAsync(cts.Token);
                await client.SendCommandAsync("PWRQSTN", cts.Token);
                resetEvent.Wait(cts.Token);
                cts.Cancel();
                await connection;
            }
        }
    }
}