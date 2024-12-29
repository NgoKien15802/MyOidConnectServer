using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (builder.Environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
        }

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        // đăng ký openidcn
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
            // Sau khi người dùng được xác thực qua OpenID Connect, thông tin đăng nhập sẽ được lưu vào cookies để sử dụng cho các yêu cầu tiếp theo.
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)

            //Đăng ký cấu hình OpenID Connect cho ứng dụng để giao tiếp với một Identity Provider.
            // khi login bên provider nó sẽ gửi clientid, seret lên cho url Authority
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = builder.Configuration["OpenIdConnect:Authority"];
                options.ClientId = builder.Configuration["OpenIdConnect:ClientId"];
                options.ClientSecret = builder.Configuration["OpenIdConnect:ClientSecret"];
                // openidConnect trả về code
                options.ResponseType = OpenIdConnectResponseType.Code;

                // có lưu access token, refresh token vào cookie hay không?
                options.SaveTokens = true;
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
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}