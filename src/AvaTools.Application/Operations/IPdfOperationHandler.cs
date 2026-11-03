using AvaTools.Domain;

namespace AvaTools.Application.Operations;

public interface IPdfOperationHandler
{
    PdfOperationKind Kind { get; }

    Task<PdfOperationResult> HandleAsync(
        PdfOperationRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken);
}

public abstract class PdfOperationHandler<TRequest> : IPdfOperationHandler
    where TRequest : PdfOperationRequest
{
    public abstract PdfOperationKind Kind { get; }

    public Task<PdfOperationResult> HandleAsync(
        PdfOperationRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (request is not TRequest typedRequest)
        {
            throw new ArgumentException($"操作 {Kind} 收到了不匹配的请求类型 {request.GetType().Name}。", nameof(request));
        }

        return HandleAsync(typedRequest, progress, cancellationToken);
    }

    protected abstract Task<PdfOperationResult> HandleAsync(
        TRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken);
}
