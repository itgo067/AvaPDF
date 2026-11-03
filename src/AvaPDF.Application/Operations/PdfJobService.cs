using AvaPDF.Domain;

namespace AvaPDF.Application.Operations;

/// <summary>Single application entry point shared by interactive and API hosts.</summary>
public sealed class PdfJobService
{
    private readonly IReadOnlyDictionary<PdfOperationKind, IPdfOperationHandler> _handlers;

    public PdfJobService(IEnumerable<IPdfOperationHandler> handlers)
    {
        _handlers = handlers.ToDictionary(handler => handler.Kind);
    }

    public Task<PdfOperationResult> ExecuteAsync(
        PdfOperationRequest request,
        IProgress<PdfOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_handlers.TryGetValue(request.Kind, out var handler))
        {
            throw new NotSupportedException($"尚未注册 PDF 操作：{request.Kind}。");
        }

        return handler.HandleAsync(request, progress, cancellationToken);
    }
}
