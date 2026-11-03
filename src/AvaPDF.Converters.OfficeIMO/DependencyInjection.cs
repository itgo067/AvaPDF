using AvaPDF.Application.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace AvaPDF.Converters.OfficeIMO;

public static class DependencyInjection
{
    public static IServiceCollection AddOfficeImoDocumentConverters(this IServiceCollection services)
    {
        services.AddSingleton<IPdfOperationHandler, WordToPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, ExcelToPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, PowerPointToPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, MarkdownToPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, HtmlToPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, RtfToPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, TxtToPdfHandler>();
        return services;
    }
}
