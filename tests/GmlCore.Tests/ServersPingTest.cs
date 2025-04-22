using System.Net.Sockets;
using Pingo;
using Pingo.Status;

namespace GmlCore.Tests;

public class ServersPingTest
{
    [Test]
    [Order(41)]
    public async Task ServerPing1_20_6_WithDomain()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.20.6
            var options = new MinecraftPingOptions
            {
                Address = "mc.hushcraft.ru",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(42)]
    public async Task ServerPing1_7_10()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.7.10
            var options = new MinecraftPingOptions
            {
                Address = "79.133.181.47",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(43)]
    public async Task ServerPing1_5_2()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.5.2
            var options = new MinecraftPingOptions
            {
                Address = "146.19.48.159",
                Port = 25777,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(44)]
    public async Task ServerPing1_12_2()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.12.2
            var options = new MinecraftPingOptions
            {
                Address = "185.9.145.192",
                Port = 20679,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(45)]
    public async Task ServerPing1_16_5()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.16.5
            var options = new MinecraftPingOptions
            {
                Address = "144.76.61.107",
                Port = 20690,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(46)]
    public async Task ServerPing1_20_1()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.20.1
            var options = new MinecraftPingOptions
            {
                Address = "199.83.103.196",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(46)]
    public async Task ServerPing1_21()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.21
            var options = new MinecraftPingOptions
            {
                Address = "213.152.43.2",
                Port = 25616,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(46)]
    public async Task ServerPing1_21_1()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.21.1
            var options = new MinecraftPingOptions
            {
                Address = "MCR7.GGMINE.RU",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(46)]
    public async Task ServerPing1_21_2()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.21.2
            var options = new MinecraftPingOptions
            {
                Address = "ilp.tridentmc.ru",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(46)]
    public async Task ServerPing1_21_3()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.21.3
            var options = new MinecraftPingOptions
            {
                Address = "130.61.138.213",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(46)]
    public async Task ServerPing1_21_4()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.21.4
            var options = new MinecraftPingOptions
            {
                Address = "185.9.145.101",
                Port = 25748,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }

    [Test]
    [Order(46)]
    public async Task ServerPing1_21_5()
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);

            // 1.21.5
            var options = new MinecraftPingOptions
            {
                Address = "135.181.237.44",
                Port = 25750,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options, cancellationTokenSource.Token) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Server not allowed: {e.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Ping operation was cancelled");
        }
    }
}
