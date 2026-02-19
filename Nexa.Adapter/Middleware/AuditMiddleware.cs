namespace Nexa.Adapter.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var traceId = context.TraceIdentifier;
            var endpoint = context.Request.Path;
            var method = context.Request.Method;
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation(
                    "REQUEST → TraceId: {TraceId}, Method: {Method}, Endpoint: {Endpoint}",
                    traceId, method, endpoint);

                await _next(context);

                var duration = DateTime.UtcNow - startTime;

                _logger.LogInformation(
                    "RESPONSE → TraceId: {TraceId}, StatusCode: {StatusCode}, DurationMs: {Duration}",
                    traceId,
                    context.Response.StatusCode,
                    duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;

                _logger.LogError(
                    ex,
                    "ERROR → TraceId: {TraceId}, Endpoint: {Endpoint}, DurationMs: {Duration}",
                    traceId,
                    endpoint,
                    duration.TotalMilliseconds);

                throw;
            }
        }
    }
}
