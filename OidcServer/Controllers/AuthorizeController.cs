using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OidcServer.Helper;
using OidcServer.Models;
using OidcServer.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OidcServer.Controllers
{
    public class AuthorizeController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly ICodeItemRepository codeItemRepository;

        public AuthorizeController(IUserRepository userRepository, ICodeItemRepository codeItemRepository)
        {
            this.userRepository = userRepository;
            this.codeItemRepository = codeItemRepository;
        }

        public IActionResult Index(AuthenticationRequestModel authenticateRequest)
        {
            ValidateAuthenticateRequestModel(authenticateRequest);
            return View(authenticateRequest);
        }


        // step 2
        [HttpPost]
        public IActionResult Authorize(AuthenticationRequestModel authenticateRequest, string user, string[] scopes)
        {
            if (userRepository.FindByUserName(user) is null) { 
                return View("UserNotFound");
            }

            // step 3: tạo code gửi sang client
            string code = GeneratedCode();
            var model = new CodeFlowResponseViewModel() {
                Code = code,
                State = authenticateRequest.State,
                RedirectUri = authenticateRequest.RedirectUri
            };

            codeItemRepository.Add(code, new CodeItem()
            {
                AuthenticationRequestModel = authenticateRequest,
                User = user,
                Scopes = scopes
            });
            
            return View("SubmitForm", model);
        }

        // step 4: client request token
        // TH1: client gửi code để nhận access_token
        // TH2: client gửi refresh_token để nhận access_token
        [Route("oauth/token")]
        [HttpPost]
        public IActionResult ReturnTokens(string grant_type, string code, string redirect_uri)
        {
            if(grant_type != "authorization_code")
            {
                if (string.IsNullOrEmpty(code))
                {
                    return BadRequest();
                }
            }
            var codeItem = codeItemRepository.FindByCode(code);
            if (codeItem == null) { 
                return BadRequest();
            }

            codeItemRepository.Delete(code);

            if (codeItem.AuthenticationRequestModel.RedirectUri != redirect_uri)
            {
                return BadRequest();
            }

            var jwk = JwkLoader.LoadFromDefault();
            var model = new AuthenticationResponseModel()
            {
                // chứa quyền user
                AccessToken = GenerateAccessToken(codeItem.User,string.Join(' ', codeItem.Scopes), codeItem.AuthenticationRequestModel.ClientId,
                codeItem.AuthenticationRequestModel.Nonce, jwk),
                TokenType = "Bearer",
                ExpiresIn = 3600,
                RefreshToken = GeneratedCode(),
                // chứa thông tin user để đăng ký user đó cho web client
                IdToken = GenerateAccessToken(codeItem.User, string.Join(' ', codeItem.Scopes), codeItem.AuthenticationRequestModel.ClientId,
                codeItem.AuthenticationRequestModel.Nonce, jwk)
            };


            return Json(model);
        }

        private string GenerateIdToken(string userId, string audience, string nonce, JsonWebKey jsonWebKey)
        {
            // https://openid.net/specs/openid-connect-core-1_0.html#IDToken
            // we can return some claims defined here: https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId)
                
            };

            var idToken = JwtGenerator.GenerateJWTToken(
                20 * 60,
                "https://localhost:7219",
                audience,
                nonce,
                claims,
                jsonWebKey
                );


            return idToken;
        }

        private string GenerateAccessToken(string userId, string scope, string audience, string nonce, JsonWebKey jsonWebKey)
        {
            // access_token can be the same as id_token, but here we might have different values for expirySeconds so we use 2 different functions

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new("scope", scope) 
            };
            var idToken = JwtGenerator.GenerateJWTToken(
                 20 * 60,
                "https://localhost:7219",
                audience,
                nonce,
                claims,
                jsonWebKey
                );

            return idToken;
        }


        private string GeneratedCode()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static void ValidateAuthenticateRequestModel(AuthenticationRequestModel authenticateRequest)
        {
            ArgumentNullException.ThrowIfNull(authenticateRequest, nameof(authenticateRequest));

            if (string.IsNullOrEmpty(authenticateRequest.ClientId))
            {
                throw new Exception("client_id required");
            }

            if (string.IsNullOrEmpty(authenticateRequest.ResponseType))
            {
                throw new Exception("response_type required");
            }

            if (string.IsNullOrEmpty(authenticateRequest.Scope))
            {
                throw new Exception("scope required");
            }

            if (string.IsNullOrEmpty(authenticateRequest.RedirectUri))
            {
                throw new Exception("redirect_uri required");
            }
        }

    }
}
