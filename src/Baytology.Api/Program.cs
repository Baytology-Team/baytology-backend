using Baytology.Infrastructure.Data.Seeders;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.None);

builder.Services
    .AddPresentation(builder.Configuration, builder.Environment)
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseCoreMiddlewares(builder.Configuration);

app.MapControllers();

app.Run();

public partial class Program;
