namespace EventPlatformAPI.Gateway.Middlewares
{
    public class RequestTransformationMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestTransformationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.Headers["gateway"] =
                "gateway";
            Console.WriteLine(
                $"Dodat header gateway");

            await _next(context);
        }
    }
}
