namespace EventPlatformAPI.Gateway.Middlewares
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;

        private const string ApiKeyHeaderName = "api_key";
        private const string ValidApiKey = "sifra";

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsync(
                    "API key nije prosledjen.");

                return;
            }

            if (apiKey != ValidApiKey)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsync(
                    "Neispravan API key.");

                return;
            }

            await _next(context);
        }
    }
}
