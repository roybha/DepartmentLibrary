using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MongoDB.Driver;
using DepartmentLibrary.Settings;
using Microsoft.CodeAnalysis.Scripting;
using YourApp.Models;
namespace DepartmentLibrary.Services

{
    public class AuthService
    {
        private readonly IMongoCollection<User> _users;
        private readonly JwtSettings _jwtSettings;
        private readonly List<string> _adminEmails = new() { "admin@example.com" }; // <- список адмінів

        public AuthService(IMongoClient mongoClient, JwtSettings jwtSettings)
        {
            var database = mongoClient.GetDatabase("your_db_name");
            _users = database.GetCollection<User>("Users");
            _jwtSettings = jwtSettings;
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials");

            return GenerateJwt(user);
        }

        public async Task RegisterAsync(string email, string password, string role)
        {
            if (!_adminEmails.Contains(email))
                throw new UnauthorizedAccessException("Only admins can register users");

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            };

            await _users.InsertOneAsync(user);
        }

        private string GenerateJwt(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}