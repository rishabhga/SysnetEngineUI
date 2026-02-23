using ManageEngineWebApp.Datacontext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ManageEngineWebApp.Filters.DynamicAuthorizationFilter>();
});
builder.Services.AddScoped<ManageEngineWebApp.Services.PermissionDiscoveryService>();
builder.Services.AddScoped<ManageEngineWebApp.Filters.DynamicAuthorizationFilter>();
builder.Services.AddRazorPages();
// Register named HttpClient with SSL bypass for API calls
builder.Services.AddHttpClient("ManageEngineApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7225");
    // client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Authentication:ApiKey"]);
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
});

// Also register default IHttpClientFactory for DI
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

// Configure RoleHelper with IHttpClientFactory
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
ManageEngineWebApp.Datacontext.RoleHelper.Configure(app.Configuration, httpClientFactory);

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// IMPORTANT: Map MVC routes BEFORE Razor Pages
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
