using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SonicWave8D;
using SonicWave8D.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<AudioService>();
builder.Services.AddScoped<AuthService>();

var host = builder.Build();

// Initialize services
var storageService = host.Services.GetRequiredService<StorageService>();
await storageService.InitializeAsync();

var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

await host.RunAsync();
