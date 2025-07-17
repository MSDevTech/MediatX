# MediatX

Enterprise-grade mediator pattern implementation with pipelines, async support, and DI integration. An alternative to MediatR.

## Features

- Implements the mediator pattern for decoupled request/response and notification handling
- Supports pipeline behaviors for cross-cutting concerns (logging, validation, etc.)
- Asynchronous request and notification handling
- Seamless integration with Microsoft.Extensions.DependencyInjection
- Targets .NET 8 and .NET 9

## Installation

Add the NuGet package to your project:

```
dotnet add package MediatX
```

## Usage

1. **Register MediatX in your DI container:**

```csharp
using MediatX;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatX(typeof(Startup).Assembly);
```

2. **Define a Request and Handler:**

```csharp
public class Ping : IRequest<string> { }

public class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        => Task.FromResult("Pong");
}
```

3. **Send a Request:**

```csharp
public class MyService
{
    private readonly IMediator _mediator;
    public MyService(IMediator mediator) => _mediator = mediator;

    public async Task<string> PingAsync()
    {
        return await _mediator.Send(new Ping());
    }
}
```

4. **Add Pipeline Behaviors (optional):**

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Pre-processing
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next();
        // Post-processing
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

## Contributing

Contributions are welcome! Please open issues or submit pull requests.

## License

[MIT](LICENSE)

## Repository

[https://github.com/MSDevTech/MediatX](https://github.com/MSDevTech/MediatX)
