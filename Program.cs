using ManageEngineWebApp.Datacontext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 64 * 1024; 
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ManageEngineWebApp.Filters.DynamicAuthorizationFilter>();
});
builder.Services.AddScoped<ManageEngineWebApp.Services.PermissionDiscoveryService>();
builder.Services.AddScoped<ManageEngineWebApp.Filters.DynamicAuthorizationFilter>();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient("ManageEngineApi", client =>
{
    //client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7225");
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://172.16.15.15:4431");
    // client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Authentication:ApiKey"]);
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
});

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4); // Increased timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".ManageEngine.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
});

var app = builder.Build();

var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
ManageEngineWebApp.Datacontext.RoleHelper.Configure(app.Configuration, httpClientFactory);

if (!app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
