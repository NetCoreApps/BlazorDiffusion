using System.Net;
using Microsoft.AspNetCore.Components.Authorization;
using ServiceStack.Blazor;
using BlazorDiffusion.UI;
using BlazorDiffusion.ServiceModel;
using Ljbc1994.Blazor.IntersectionObserver;

AppHost.RegisterKey();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddScoped<ServiceStackStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<ServiceStackStateProvider>());

var baseUrl = // builder.Configuration["oauth.RedirectUrl"] ?? // trying out UseInProcessClient
    (builder.Environment.IsDevelopment() ? "https://localhost:5001" : "http://" + IPAddress.Loopback);

builder.Services.AddLocalStorage();
builder.Services.AddBlazorServerApiClient(baseUrl);

builder.Services.AddScoped<KeyboardNavigation>();
builder.Services.AddScoped<UserState>();
builder.Services.AddIntersectionObserver();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.UseServiceStack(new AppHost());

BlazorConfig.Set(new()
{
    // This changes Blazor Components + ApiAsync APIs to use InProcessGateway instead of JsonApiClient
    UseInProcessClient = true,

    Services = app.Services,
    JSParseObject = JS.ParseObject,
    EnableLogging = app.Environment.IsDevelopment(),
    EnableVerboseLogging = app.Environment.IsDevelopment(),
    AssetsBasePath = AppConfig.Instance.AssetsBasePath,
    FallbackAssetsBasePath = AppConfig.Instance.FallbackAssetsBasePath,
    DefaultProfileUrl = Icons.AnonUserUri,
    OnApiErrorAsync = (request,apiError) => 
    {
        BlazorConfig.Instance.GetLog()?.LogDebug("\n\nOnApiErrorAsync(): {0}", apiError.Error.GetDetailedError());
        return Task.CompletedTask;
    }
});

app.Run();
