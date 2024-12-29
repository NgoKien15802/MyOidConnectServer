var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// trả về các oidc config về cho client
app.MapGet("/.well-known/openid-configuration", () => {
    return Results.File(Path.Combine(builder.Environment.ContentRootPath, "OidcDiscovery", "openid-configuration.json"), contentType: "application/json");
});

// trả về key verify chữ ký số
app.MapGet("/.well-known/jwks.json", () => {
    return Results.File(Path.Combine(builder.Environment.ContentRootPath, "OidcDiscovery", "jwks.json"), contentType: "application/json");
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
