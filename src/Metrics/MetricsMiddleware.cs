using Prometheus;

namespace IdentityService.Metrics;

public class MetricsMiddleware(RequestDelegate next)
{
    private static readonly Counter LoginAttempts = Prometheus.Metrics
        .CreateCounter("identity_login_attempts_total", "Total de tentativas de login",
            new CounterConfiguration { LabelNames = ["result"] });

    private static readonly Counter RegisterAttempts = Prometheus.Metrics
        .CreateCounter("identity_register_attempts_total", "Total de tentativas de registro",
            new CounterConfiguration { LabelNames = ["result"] });

    private static readonly Histogram RequestDuration = Prometheus.Metrics
        .CreateHistogram("identity_http_request_duration_seconds", "Duração das requisições HTTP",
            new HistogramConfiguration { LabelNames = ["method", "path", "status"] });

    public async Task InvokeAsync(HttpContext context)
    {
        var timer = RequestDuration
            .WithLabels(context.Request.Method, context.Request.Path, "pending")
            .NewTimer();

        await next(context);

        timer.ObserveDuration();

        var path = context.Request.Path.Value ?? "";
        var status = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300
            ? "success" : "error";

        if (path.Contains("/login", StringComparison.OrdinalIgnoreCase))
            LoginAttempts.WithLabels(status).Inc();

        if (path.Contains("/register", StringComparison.OrdinalIgnoreCase))
            RegisterAttempts.WithLabels(status).Inc();
    }
}
