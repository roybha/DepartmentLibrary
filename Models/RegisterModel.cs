namespace DepartmentLibrary.Models
{
    /// <summary>
    /// model for register form property must match with "asp-for"
    /// </summary>
    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}
