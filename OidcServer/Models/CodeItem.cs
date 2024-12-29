namespace OidcServer.Models
{
    public class CodeItem
    {
        public required AuthenticationRequestModel AuthenticationRequestModel{ get; set; }
        public required string User { get; set; }
        public required string[] Scopes{ get; set; }
    }
}
