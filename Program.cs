using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MediCare.Models.Data;            
using System.Text;
using MediCare;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration: JWT ----
builder.Configuration["Jwt:Key"] ??= "ReplaceThisWithASecretKeyLongEnoughForHS256";
builder.Configuration["Jwt:Issuer"] ??= "MedicareApi";
builder.Configuration["Jwt:Audience"] ??= "MedicareApiClients";

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Medicare API", Version = "v1" });

    // Add Bearer token support
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer' [space] and then your token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    });
});

// PostgreSQL Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add password hasher
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<User>, Microsoft.AspNetCore.Identity.PasswordHasher<User>>();

// Email service

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


// JWT Auth
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization(options =>
{
   
    options.AddPolicy("RequireAdminRole", policy => 
        policy.RequireRole("SuperAdmin"));
    
    options.AddPolicy("RequireDoctorRole", policy => 
        policy.RequireRole("Doctor", "SuperAdmin"));
    
    options.AddPolicy("RequireNurseRole", policy => 
        policy.RequireRole("Nurse", "SuperAdmin"));
    
    options.AddPolicy("RequireStaffRole", policy => 
        policy.RequireRole("Doctor", "Nurse", "SuperAdmin"));

    // Resource-based policies (for patient-specific access)
    options.AddPolicy("CanViewPatient", policy =>
        policy.Requirements.Add(new PatientAuthorizationRequirement(PatientAuthorizationOperation.View)));
        
    options.AddPolicy("CanUpdatePatient", policy =>
        policy.Requirements.Add(new PatientAuthorizationRequirement(PatientAuthorizationOperation.Update)));
        
    options.AddPolicy("CanPrescribe", policy =>
        policy.Requirements.Add(new PatientAuthorizationRequirement(PatientAuthorizationOperation.Prescribe)));
});


builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();


// Logging
builder.Services.AddLogging();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<User>>();
    DataSeeder.Seed(ctx, hasher);
}

// Configure app
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
