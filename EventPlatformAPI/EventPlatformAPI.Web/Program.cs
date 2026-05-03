using EventPlatformAPI.Web.Services;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(5));

var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<OperationCanceledException>()
    .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(2 * attempt),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            var message = $"Retry attempt {retryCount}: waiting {timespan.TotalSeconds}s. " +
                $"Reason: {(outcome.Exception?.GetType().Name ?? outcome.Result?.StatusCode.ToString() ?? "Unknown")}";
            System.Diagnostics.Debug.WriteLine(message);
        });

var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<OperationCanceledException>()
    .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
    .CircuitBreakerAsync<HttpResponseMessage>(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, timespan) =>
        {
            System.Diagnostics.Debug.WriteLine(
                $"Circuit breaker OPEN for {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        },
        onReset: () =>
        {
            System.Diagnostics.Debug.WriteLine("Circuit breaker RESET - service recovered!");
        });


var combinedPolicy = Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);

builder.Services.AddHttpClient<IEventsApiClient, EventsApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiEndpoints:EventsApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("ApiEndpoints:EventsApiBaseUrl nije podešen.");
    }

    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(combinedPolicy);

builder.Services.AddHttpClient<IReferencesApiClient, ReferencesApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiEndpoints:ReferencesApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("ApiEndpoints:ReferencesApiBaseUrl nije podešen.");
    }

    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(combinedPolicy);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
