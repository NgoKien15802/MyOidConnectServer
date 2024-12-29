using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OidcWebClient;
using OidcWebClient.Helper;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (builder.Environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
        }

        var options = builder.Configuration.GetSection("OpenIdConnect").Get<ClientOptions>() ?? throw new Exception("Could not get ClientOptions");

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        // đăng ký openidcn
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
            // Sau khi người dùng được xác thực qua OpenID Connect, thông tin đăng nhập sẽ được lưu vào cookies để sử dụng cho các yêu cầu tiếp theo.
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)

            //Đăng ký cấu hình OpenID Connect cho ứng dụng để giao tiếp với một Identity Provider.
            // khi login bên provider nó sẽ gửi clientid, seret lên cho url Authority
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, openIdOptions =>
            {
                openIdOptions.ClientId = options.ClientId;
                openIdOptions.Authority = options.Issuer;
                // openidConnect trả về code
                openIdOptions.ResponseType = OpenIdConnectResponseType.Code;
                openIdOptions.CallbackPath = options.CallbackPath;
                // có lưu access token, refresh token vào cookie hay không?
                openIdOptions.SaveTokens = true;
                openIdOptions.AccessDeniedPath = options.AccessDeniedPath;


                foreach (var scope in openIdOptions.Scope)
                {
                    openIdOptions.Scope.Add(scope);
                }

                openIdOptions.TokenValidationParameters.ValidIssuer = options.Issuer;
                openIdOptions.TokenValidationParameters.ValidAudience = options.ClientId;
                openIdOptions.TokenValidationParameters.ValidAlgorithms = new[] { "RS256" };
                openIdOptions.TokenValidationParameters.IssuerSigningKey = JwkLoader.LoadFromPublic();

                openIdOptions.Events.OnAuthorizationCodeReceived = (context) =>
                {
                    Console.WriteLine($"authorization_code: {context.ProtocolMessage.Code}");

                    return Task.CompletedTask;
                };

                openIdOptions.Events.OnTokenResponseReceived = (context) =>
                {
                    Console.WriteLine($"OnTokenResponseReceived.access_token: {context.TokenEndpointResponse.AccessToken}");
                    Console.WriteLine($"OnTokenResponseReceived.refresh_token: {context.TokenEndpointResponse.RefreshToken}");

                    return Task.CompletedTask;
                };

                openIdOptions.Events.OnTokenValidated = (context) =>
                {
                    Console.WriteLine($"OnTokenValidated.access_token: {context.TokenEndpointResponse?.AccessToken}");
                    Console.WriteLine($"OnTokenValidated.refresh_token: {context.TokenEndpointResponse?.RefreshToken}");

                    return Task.CompletedTask;
                };

                openIdOptions.Events.OnTicketReceived = (context) =>
                {
                    return Task.CompletedTask;
                };

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