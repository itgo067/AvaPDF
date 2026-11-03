using AvaPDF.Application.Operations;
using AvaPDF.Pdf.OfficeIMO.Features;
using Microsoft.Extensions.DependencyInjection;

namespace AvaPDF.Pdf.OfficeIMO;

public static class DependencyInjection
{
    public static IServiceCollection AddOfficeImoPdf(this IServiceCollection services)
    {
        services.AddSingleton<IPdfOperationHandler, MergePdfHandler>();
        services.AddSingleton<IPdfOperationHandler, SplitPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, ExtractPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, DeletePdfHandler>();
        services.AddSingleton<IPdfOperationHandler, RotatePdfHandler>();
        services.AddSingleton<IPdfOperationHandler, ReorderPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, OptimizePdfHandler>();
        services.AddSingleton<IPdfOperationHandler, ImagesToPdfHandler>();
        services.AddSingleton<IPdfOperationHandler, ExtractTextPdfHandler>();
        services.AddSingleton<PdfJobService>();
        return services;
    }
}
