using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using Serilog;

namespace FastAddress.Api.Infrastructure;

/// <summary>
/// Enriches the per-request Serilog log (emitted by <c>UseSerilogRequestLogging</c>) with MVC action
/// context: the resolved action name, its route values, whether model state is valid, the model-state
/// errors when binding or validation failed, and any unhandled action exception. The properties are
/// pushed onto the request's <see cref="IDiagnosticContext"/>, so they appear on the single request
/// completion event rather than as separate log lines.
/// </summary>
internal sealed class SerilogEnricherMvcFilter(IDiagnosticContext diagnosticContext)
    : IActionFilter, IResultFilter, IExceptionFilter
{
    /// <inheritdoc/>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        diagnosticContext.Set("ActionName", context.ActionDescriptor.DisplayName);
        diagnosticContext.Set("RouteValues", context.ActionDescriptor.RouteValues, destructureObjects: true);
        diagnosticContext.Set("ValidationState", context.ModelState.ValidationState);

        if (context.ModelState.ErrorCount > 0)
        {
            diagnosticContext.Set("ModelState", new SerializableError(context.ModelState), destructureObjects: true);
        }
    }

    /// <inheritdoc/>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Enrichment happens before the action runs and on exception; nothing to add afterwards.
    }

    /// <inheritdoc/>
    public void OnResultExecuting(ResultExecutingContext context)
    {
        // No enrichment needed at the result stage; implemented so the filter spans the full pipeline.
    }

    /// <inheritdoc/>
    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    /// <inheritdoc/>
    public void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        diagnosticContext.SetException(context.Exception);
    }
}
