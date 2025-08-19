using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SeoRender.Web.Components;
using SeoRender.Web.Components.Account;
using SeoRender.Web.Data;
using System.Net.Mime;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddSingleton<PreRenderDbContext>();
builder.Services.AddSingleton<PreRenderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseResponseCompression();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapGet("/api/prerender", async (HttpContext http, string url, PreRenderService service) =>
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out _))
    {
        return Results.BadRequest("Invalid url");
    }

    var result = await service.GetRenderedPageAsync(url);
    http.Response.Headers["Last-Modified"] = result.Meta.Timestamp.ToString("R");
    http.Response.Headers["X-From-SeoRender"] = "true";

    if (http.Request.Headers.AcceptEncoding.ToString().Contains("gzip"))
    {
        http.Response.Headers["Content-Encoding"] = "gzip";
        return TypedResults.Stream(result.GzipStream, MediaTypeNames.Text.Html);
    }
    else
    {
        var stream = new GZipStream(result.GzipStream, CompressionMode.Decompress);
        return TypedResults.Stream(stream, MediaTypeNames.Text.Html);
    }
});

app.Run();
