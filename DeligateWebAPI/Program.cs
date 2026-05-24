using DeligateWebAPI.Data;
using DeligateWebAPI.Hubs;
using DeligateWebAPI.Interfaces;
using DeligateWebAPI.Models;
using DeligateWebAPI.Repository;
using DeligateWebAPI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using AspNet.Security.OAuth.Apple;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using Xabe.FFmpeg;

var builder = WebApplication.CreateBuilder(args);

// 1. DATABASE & REPOSITORIES
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(defaultConnection));

builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// 2. AUTHENTICATION (JWT + GOOGLE + APPLE + COOKIES)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme) // Single cookie scheme for both Google and Apple
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
})
.AddGoogle(options =>
{
    // Read from appsettings.json
    options.ClientId = "YOUR_GOOGLE_CLIENT_ID";
    options.ClientSecret = "YOUR_GOOGLE_CLIENT_SECRET";
    options.CallbackPath = "/signin-google";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})

.AddApple(options =>
{
    options.ClientId = "YOUR_GOOGLE_CLIENT_ID"; 
    options.TeamId = "YOUR_TEAM_ID";
    options.KeyId = "DVV8YR533P";

    options.UsePrivateKey(keyId =>
        builder.Environment.ContentRootFileProvider.GetFileInfo("Keys/AuthKey_DVV8YR533P.p8")); // Added 'Keys/'

    options.CallbackPath = "/signin-apple";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});


builder.Services.AddAuthorization();

// 3. CONTROLLERS & JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new TimeSpanConverter());
    });

// 4. SIGNALR & SYSTEM SERVICES
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1073741824; // 1GB
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<MessageCleanupService>();

// 5. CONFIGURATIONS (Email, Smtp, Uploads)
builder.Services.Configure<EmailConfiguration>(builder.Configuration.GetSection("EmailConfiguration"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = 1073741824; });

// 6. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("PasswordReset", opt =>
    {
        opt.PermitLimit = 3;              // max 3 requests
        opt.Window = TimeSpan.FromMinutes(15); // per 15 minutes
        opt.QueueLimit = 0;
    });
});


var app = builder.Build();

// 7. DATABASE MIGRATIONS & FFMPEG
FFmpeg.SetExecutablesPath(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg-binaries"));

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}



// 8. MIDDLEWARE PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles(); // If you serve images/files
app.UseRouting();
app.UseCors("AllowAllOrigins");

app.MapHub<ChatHub>("/chathub");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
