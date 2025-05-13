using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MongoDB.Driver;
using DepartmentLibrary.Settings;
using Microsoft.CodeAnalysis.Scripting;
using DepartmentLibrary.Models;
using DepartmentLibrary.Controllers;
using System.Globalization;
namespace DepartmentLibrary.Services

{
    /// <summary>
    /// class for managing user objects (basiclly a user CRUD operation + jwt token implementation) 
    /// made this service separate, but i think that any other tabels can be managed by one main service class 
    /// </summary>
    public class AuthService
    {
        private readonly IMongoCollection<User> _users;
        private readonly JwtSettings _jwtSettings; 

        private readonly List<string> _adminEmails = new() { "a@example.com, sinchuk_taras@knu.ua" }; // admin s list 


        public AuthService(IMongoClient mongoClient, JwtSettings jwtSettings)
        {
            var database = mongoClient.GetDatabase("DepartmentLibraryDb");
            _users = database.GetCollection<User>("users");
            _jwtSettings = jwtSettings;
        }

        /// <summary>
        /// Fetches user by email if hashed password from db does match with entered password generates 
        /// the jwt token 
        /// </summary>
        /// <param name="email">user email</param>
        /// <param name="password">password to check</param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public async Task<string> LoginAsync(string email, string password)
        {
            
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            System.Diagnostics.Debug.WriteLine($"UUUUUUUUUUUUUUUUUSER\n {user is null}");
            Console.WriteLine(user);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                throw new UnauthorizedAccessException("Invalid credentials");

            return GenerateJwt(user);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email">email of new user</param>
        /// <param name="password">password of new user</param>
        /// <param name="role">role of new use</param>
        /// <param name="adminEmail">email of the authorized user that are trying to register new user</param>
        /// <returns>Async insert into db users table</returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public async Task RegisterAsync(RegisterModel userDto, string adminEmail)
        {
            DateTime? thesisDefenseDate = null;

            if (!string.IsNullOrEmpty(userDto.ThesisDefenseDate))
            {
                // Convert the string to a DateTime object
                thesisDefenseDate = DateTime.ParseExact(userDto.ThesisDefenseDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
            }
            System.Diagnostics.Debug.WriteLine("ADMIN EMAIL", adminEmail);

            var user = new User
            {

                Name = userDto.Name,
                Position = userDto.Position,
                Email = userDto.Email,
                Phone = userDto.Phone,
                Password = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                Role = userDto.Role,
                ThesisDefenseDate = thesisDefenseDate,
            };

            await _users.InsertOneAsync(user);
        }
        /// <summary>
        /// Generates a JSON Web Token (JWT) for the specified user using their email and role.
        /// </summary>
        /// <param name="user">The user for whom to generate the JWT. Must contain Email and Role properties.</param>
        /// <returns>A string representing the generated JWT.</returns>
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
