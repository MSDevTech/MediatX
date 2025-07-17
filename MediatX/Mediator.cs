using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MediatX;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Mediator> _logger;

    // Cache for handler methods
    private static readonly Dictionary<Type, MethodInfo> _handlerCache = new();
    // Cache for behavior methods
    private static readonly Dictionary<Type, MethodInfo> _behaviorCache = new();

    public Mediator(IServiceProvider serviceProvider, ILogger<Mediator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        // Get base handler
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"Handler not found for {requestType.Name}");

        // Get pipeline behaviors
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var behaviors = (_serviceProvider.GetServices(behaviorType) ?? Array.Empty<object>()).ToList();

        _logger.LogDebug("Found {BehaviorCount} behaviors for {RequestType}",
            behaviors.Count, requestType.Name);

        // Create handler delegate
        var handlerDelegate = CreateHandlerDelegate(handler, request, cancellationToken);

        // Build pipeline
        var pipeline = behaviors.AsEnumerable()
            .Reverse()
            .Aggregate(handlerDelegate,
                (next, behavior) => CreateBehaviorDelegate(behavior, request, next, cancellationToken));

        return await pipeline().ConfigureAwait(false);
    }

    public Task Send(IRequest request,
        CancellationToken cancellationToken = default)
        => Send<Unit>((IRequest<Unit>)request, cancellationToken);

    private RequestHandlerDelegate<TResponse> CreateHandlerDelegate<TResponse>(
        object handler,
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        var handlerType = handler.GetType();
        var cacheKey = handlerType;

        if (!_handlerCache.TryGetValue(cacheKey, out var handleMethod))
        {
            handleMethod = handlerType.GetMethod("Handle")!;
            _handlerCache[cacheKey] = handleMethod;
        }

        return () => (Task<TResponse>)handleMethod.Invoke(
            handler,
            new object[] { request, cancellationToken })!;
    }

    private RequestHandlerDelegate<TResponse> CreateBehaviorDelegate<TResponse>(
        object behavior,
        IRequest<TResponse> request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var behaviorType = behavior.GetType();
        var cacheKey = behaviorType;

        if (!_behaviorCache.TryGetValue(cacheKey, out var handleMethod))
        {
            handleMethod = behaviorType.GetMethod("Handle")!;
            _behaviorCache[cacheKey] = handleMethod;
        }

        return () => (Task<TResponse>)handleMethod.Invoke(
            behavior,
            new object[] { request, next, cancellationToken })!;
    }

    public async Task Publish<TNotification>(TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>();
        var tasks = handlers.Select(handler =>
            handler.Handle(notification, cancellationToken));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}