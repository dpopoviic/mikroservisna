using EventPlatformAPI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IEventsApiClient, EventsApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiEndpoints:EventsApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("ApiEndpoints:EventsApiBaseUrl nije podešen.");
    }

    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient<IReferencesApiClient, ReferencesApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiEndpoints:ReferencesApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("ApiEndpoints:ReferencesApiBaseUrl nije podešen.");
    }

    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
