using dotenv.net;
using Microsoft.EntityFrameworkCore;
using PostCommentApi;
using PostCommentApi.Middlewares;
using PostCommentApi.Services;
using PostCommentApi.Utilities;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Linq;


var builder = WebApplication.CreateBuilder(args);
// set up env var
DotEnv.Load();
var config = builder.Configuration.AddEnvironmentVariables().Build();

// Configure Serilog early so startup logs are captured (read from configuration)
Log.Logger = new LoggerConfiguration()
  .ReadFrom.Configuration(config)
  .CreateLogger();

builder.Host.UseSerilog((ctx, services, lc) =>
  lc.ReadFrom.Configuration(ctx.Configuration)
);

// optional debug dump
// Console.WriteLine(config.GetDebugView());
bool isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
//mysql db service setup
var connectionString = isRunningInContainer ? config["CONNECTIONSTRINGS:MYSQLDOCKER"] : config["CONNECTIONSTRINGS:MYSQL"];
Console.WriteLine(connectionString);
builder.Services.AddDbContext<AppDb>(options =>
{
  options.UseMySql(
      connectionString,
      new MySqlServerVersion(new Version(8, 0, 21)));
});
// Redis db service setup 
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
  var redisConfig = isRunningInContainer ? config["REDIS:CONNECTIONSTRINGDOCKER"] : config["REDIS:CONNECTIONSTRING"];
  if (string.IsNullOrEmpty(redisConfig)) throw new InvalidOperationException("Redis connection string is not configured.");
  var options = ConfigurationOptions.Parse(redisConfig);
  options.AbortOnConnectFail = false;
  return ConnectionMultiplexer.Connect(options);
});
builder.Services.AddScoped<IDatabase>(sp =>
  sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase()
);

builder.Services.AddSingleton<MinioService>();
builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithAuth();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IPostService, PostServiceNoCache>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<TestService>();
builder.Services.AddHttpContextAccessor();

// Correlation Id services removed

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.RequireHttpsMetadata = true; // true in prod; can be false in dev
    var jwtSettings = config.GetSection("JWT");
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer = jwtSettings["ISSUER"],
      ValidAudience = jwtSettings["AUDIENCE"],
      IssuerSigningKey = new SymmetricSecurityKey(
        System.Text.Encoding.UTF8.GetBytes(jwtSettings["SECRET"] ?? throw new InvalidOperationException("JWT Key is not configured."))
      )
    };
  });

var app = builder.Build();

// Ensure routing is enabled so middleware can read route values
app.UseRouting();

// Correlation Id middleware removed

// Default Serilog request logging
app.UseSerilogRequestLogging();

// Db Seeding
// Ensure database created
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDb>();

  try
  {
    db.Database.Migrate();
    await Seeder.SeedUserPostComment(db);
  }
  catch (Exception ex)
  {
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
  }

  // Ensure MinIO bucket exists
  var minioService = scope.ServiceProvider.GetRequiredService<MinioService>();
  await minioService.EnsureBucketExistsAsync();
}


if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}
// Exception handling middleware should be registered early in the pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TestMiddleWare>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Start the app. Do not register a terminal middleware that handles every request
// (app.Run(async ctx => ...)) because that will short-circuit the pipeline and
// prevent requests from reaching controllers. If you want a simple root health
// endpoint, use `app.MapGet("/", ...)` instead.
try
{
  Log.Information("Starting web host");
  app.Run();
}
catch (Exception ex)
{
  Log.Fatal(ex, "Host terminated unexpectedly");
  throw;
}
finally
{
  Log.Information("Shutting down web host");
  Log.CloseAndFlush();
}