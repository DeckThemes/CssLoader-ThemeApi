#region

using System.Text;
using DeckPersonalisationApi;
using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Middleware.CookieConverter;
using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Services;
using DeckPersonalisationApi.Services.Css;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

#endregion

AppConfiguration configuration = new();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = configuration.JwtIssuer,
        ValidAudience = configuration.JwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.ASCII.GetBytes(configuration.JwtKey)),
        ValidateIssuer = configuration.JwtValidateIssuer,
        ValidateAudience = configuration.JwtValidateAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
    };
});
builder.Services.AddSingleton(configuration);
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BlobService>();
builder.Services.AddScoped<CssThemeService>();
builder.Services.AddSingleton<TaskService>();
builder.Services.AddScoped<VnuCssVerifier>();
builder.Services.AddScoped<CssSubmissionService>();
builder.Services.AddHostedService<Startup>();
builder.Services.AddDbContext<ApplicationContext>(x =>
{
    string conn = configuration.DbPath;
    if (string.IsNullOrWhiteSpace(conn) || conn == "MEMORY")
        x.UseInMemoryDatabase("app");
    else
        x.UseSqlite(conn);
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme()
            {
                Reference = new OpenApiReference()
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
});
var app = builder.Build();

app.UseAuthTokenCookieToAuthHeader();

// Configure the HTTP request pipeline.
if (configuration.UseSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(x =>
{
    x.Run(async ctx =>
    {
        Exception? exception = ctx.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        
        if (exception is IHttpException httpException)
        {
            ctx.Response.StatusCode = httpException.StatusCode;
            await ctx.Response.WriteAsJsonAsync(new ErrorDto(httpException.Message));
        }
    });
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseJwtRoleAttributes();
app.MapControllers();
app.Run($"http://localhost:{configuration.Port}");