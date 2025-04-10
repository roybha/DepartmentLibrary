namespace DepartmentLibrary.Settings
{
    public class JwtSettings
    {
        public string Secret { get; set; }
        public int ExpirationInMinutes { get; set; }
    }
}
