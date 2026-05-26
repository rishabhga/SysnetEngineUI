using ManageEngineWebApp.Datacontext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 64 * 1024; 
    options.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ManageEngineWebApp.Filters.DynamicAuthorizationFilter>();
});
builder.Services.AddScoped<ManageEngineWebApp.Services.PermissionDiscoveryService>();
builder.Services.AddScoped<ManageEngineWebApp.Services.IEmailService, ManageEngineWebApp.Services.EmailService>();
builder.Services.AddScoped<ManageEngineWebApp.Filters.DynamicAuthorizationFilter>();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient("ManageEngineApi", client =>
{
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(apiBaseUrl);
    // client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Authentication:ApiKey"]);
    client.Timeout = TimeSpan.FromSeconds(120);
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

if (app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
