namespace DepartmentLibrary.Models
{
    /// <summary>
    /// model for login form property must match with "asp-for"
    /// </summary>
    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
