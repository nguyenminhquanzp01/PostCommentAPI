using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


var builder = WebApplication.CreateBuilder(args);
var ConnectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDb>(options =>
{
  options.UseMySql(
      ConnectionString,
      new MySqlServerVersion(new Version(8, 0, 21)));
});

builder.Services.AddStackExchangeRedisCache(options =>
{
  options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Db Seeding
// Ensure database created
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDb>();
  // db.Database.Migrate();
  db.Database.EnsureCreated();
  try
  {
    await Seeder.SeedUserPostComment(db);
  }
  catch (Exception ex)
  {
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
  }
}


if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Start the app. Do not register a terminal middleware that handles every request
// (app.Run(async ctx => ...)) because that will short-circuit the pipeline and
// prevent requests from reaching controllers. If you want a simple root health
// endpoint, use `app.MapGet("/", ...)` instead.
app.Run();
