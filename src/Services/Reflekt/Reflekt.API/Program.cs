var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReflektPersistence(builder.Configuration);
builder.Services.AddReflektApi(builder.Configuration);

var app = builder.Build();

app.UseReflektPersistence();
app.UseReflektApi();

app.Run();

public partial class Program { }
