// Copyright 2025 Ben Voß
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files
// (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;

namespace NSwag4SignalR.Example.Hubs;

/// <summary>
/// Doc comment in the summary of the IStrongTypedHub interface.
/// </summary>
public interface IStrongTypedHub {
    /// <summary>
    /// Doc comment in the summary of the Receive method.
    /// </summary>
    /// <remarks>
    /// Doc comment in the remarks of the Receive method.
    /// </remarks>
    /// <param name="message">The message argument.</param>
    /// <returns>A task.</returns>
    Task Receive(string message);
}

/// <summary>
/// Doc comment in the summary of the StronglyTypedHub class.
/// </summary>
public class StronglyTypedHub : Hub<IStrongTypedHub> {
    private readonly ConcurrentDictionary<Task, Task> _tasks = new();

    /// <summary>
    /// Doc comment in the summary of the Send method.
    /// </summary>
    /// <remarks>
    /// Doc comment in the remarks of the Send method.
    /// </remarks>
    /// <param name="message">The message to send.</param>
    /// <returns>Returns a task.</returns>
    public async Task Send(string message)
    {
        await Clients.All.Receive(message);
    }

    /// <summary>
    /// Returns the message parameter
    /// </summary>
    public string Echo(string message)
    {
        return message;
    }

    /// <summary>
    /// Throws an error
    /// </summary>
    public void Error()
    {
        throw new ApplicationException("Test exeption");
    }

    /// <summary>
    /// Returns a non-null structured object
    /// </summary>
    public TestDto Structured()
    {
        return new TestDto
        {
            StringProperty = "Hello World",
            IntProperty = 42
        };
    }

    /// <summary>
    /// Returns a nullable structured object
    /// </summary>
    public TestDto? Structured2() {
        return new TestDto {
            StringProperty = "Hello World",
            IntProperty = 42
        };
    }

    /// <summary>
    /// Ensures we can invoke a method with a variety of data types
    /// </summary>
    public bool ComplexInputs(string str, int intValue, bool value, float floatValue, Decimal decimalValue, DateTime dateTimeValue, Guid guidValue, TestDto testDto, string optional = "test") {
        return true;
    }
    
    /// <summary>
    /// Listens to a stream of changes to the current time.
    /// </summary>
    public IAsyncEnumerable<DateTime> Listen(CancellationToken cancellationToken) {
        var channel = Channel.CreateBounded<DateTime>(options: new BoundedChannelOptions(1) {
            FullMode = BoundedChannelFullMode.Wait
        });

        var task = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await channel.Writer.WriteAsync(DateTime.UtcNow);

                await Task.Delay(1000, cancellationToken);
            }
        }, cancellationToken);

        _tasks.TryAdd(task, task);

        cancellationToken.Register(() => _tasks.Remove(task, out var _));

        return channel.Reader.ReadAllAsync();
    }
}

public class TestDto {
    public string? StringProperty { get; set; }

    public int IntProperty { get; set; }
}