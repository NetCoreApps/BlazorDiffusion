using System.Net;
using Microsoft.AspNetCore.Components.Authorization;
using ServiceStack;
using ServiceStack.Blazor;
using Microsoft.Net.Http.Headers;
using BlazorDiffusion.UI;
using BlazorDiffusion.ServiceModel;
//using Ljbc1994.Blazor.IntersectionObserver;

AppHost.RegisterKey();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var baseUrl = builder.Configuration["ApiBaseUrl"] ??
    (builder.Environment.IsDevelopment() ? "https://localhost:5001" : "http://" + IPAddress.Loopback);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseUrl) });
builder.Services.AddBlazorApiClient(baseUrl);

builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<ServiceStackStateProvider>());
builder.Services.AddScoped<ServiceStackStateProvider>();
builder.Services.AddScoped<KeyboardNavigation>();
builder.Services.AddScoped<UserState>();
//builder.Services.AddIntersectionObserver();


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
    JSParseObject = JS.ParseObject,
    EnableLogging = app.Environment.IsDevelopment(),
    EnableVerboseLogging = app.Environment.IsDevelopment(),
    AssetsBasePath = AppConfig.Instance.AssetsBasePath,
});

app.Run();
