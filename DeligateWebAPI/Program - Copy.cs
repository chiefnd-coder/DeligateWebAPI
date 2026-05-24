
using DeligateWebAPI.Controllers;
using DeligateWebAPI.Data;
using DeligateWebAPI.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Xabe.FFmpeg;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Add the custom TimeSpan converter to the JsonSerializerOptions
        options.JsonSerializerOptions.Converters.Add(new TimeSpanConverter());
    });

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero, // No grace period
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddAuthorization();

// Add SignalR
builder.Services.AddSignalR();

// Get connection string
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not found.");

// Configure ApplicationDbContext (Identity) with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(defaultConnection));

// Configure ChatContext with PostgreSQL (FIXED: Changed from UseSqlServer to UseNpgsql)
builder.Services.AddDbContext<ChatContext>(options =>
    options.UseNpgsql(defaultConnection));

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure file upload limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1073741824; // 1GB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 1073741824; // 1GB
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });

    // Keep your specific origins policy if needed
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("https://example.com") // Specify the allowed origin(s)
              .AllowAnyHeader()                   // Allow any header
              .AllowAnyMethod();                  // Allow any method (GET, POST, etc.)
    });
});

var app = builder.Build();

// Set FFmpeg path
FFmpeg.SetExecutablesPath(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg-binaries"));

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    // Migrate ApplicationDbContext
    var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    applicationDbContext.Database.Migrate();

    // Migrate ChatContext
    var chatContext = scope.ServiceProvider.GetRequiredService<ChatContext>();
    chatContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    // Gerald: Make sure to disable this for development!
    app.UseHttpsRedirection();
}


//app.UseHttpsRedirection();

// Enable CORS middleware
app.UseCors("AllowAllOrigins"); // Changed to use AllowAllOrigins for SignalR compatibility

app.UseRouting();

// Serve static files from wwwroot
app.UseStaticFiles();
app.UseDefaultFiles();

app.UseAuthentication(); // Add this before UseAuthorization
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR Hub
//app.MapHub<ChatHub>("/chathub");
app.MapHub<ChatHub>("/chathub")
   .RequireAuthorization();

// Map other endpoints
app.MapItemEndpoints();

// Configure static file directories
var uploadsPathvendorprofile = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "Uploads");
Directory.CreateDirectory(uploadsPathvendorprofile);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPathvendorprofile),
    RequestPath = "/Uploads"
});

// Configure MediaUploads directory
if (!string.IsNullOrEmpty(app.Environment.WebRootPath))
{
    var mediaUploadsPath = Path.Combine(app.Environment.WebRootPath, "MediaUploads");
    Directory.CreateDirectory(mediaUploadsPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mediaUploadsPath),
        RequestPath = "/MediaUploads"
    });
}
else
{
    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "MediaUploads");
    Directory.CreateDirectory(uploadsPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/MediaUploads"
    });
}

app.Run();