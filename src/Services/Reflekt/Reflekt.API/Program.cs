var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReflektPersistence(builder.Configuration);
builder.Services.AddReflektApi(builder.Configuration);

var app = builder.Build();

await app.Services.MigrateAndSeedAsync(app.Lifetime.ApplicationStopping);
app.UseReflektApi();

await app.RunAsync();

public partial class Program { }
