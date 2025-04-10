using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using DepartmentLibrary.Services;
using DepartmentLibrary.Settings;

var builder = WebApplication.CreateBuilder(args);

// ====== CONFIG & SETTINGS ======
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
// only one object of class jwtsettings
builder.Services.AddSingleton(jwtSettings);

// ====== MONGO CLIENT ======
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDb"))); // string in appsettings.json  (u need to put in your own)

// ====== YOUR SERVICES ======
builder.Services.AddSingleton<AuthService>();

// ====== CONTROLLERS + VIEWS ======
builder.Services.AddControllersWithViews();

// ====== AUTH ======

/// "for every request that needs auth search for jwt token"
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => // checks only by secret key 
{
    var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


/// midlleware grabs token from cookie and puts into header auth of current request so we can 
/// use User.Identity on the server if view requires authentication 
app.Use(async (context, next) =>
{
    var token = context.Request.Cookies["jwt"];
    if (!string.IsNullOrEmpty(token))
    {
        context.Request.Headers.Authorization = "Bearer " + token;
    }

    await next();
});



app.UseAuthentication();
app.UseAuthorization();

// ====== ROUTING ======
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var hash = BCrypt.Net.BCrypt.HashPassword("test");

System.Diagnostics.Debug.WriteLine(hash); 
app.Run();
