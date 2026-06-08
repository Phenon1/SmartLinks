
using RedirectService.SmartLinks.Rules;
using System.Diagnostics.CodeAnalysis;

namespace RedirectService;

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSmartLinks(builder.Configuration);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapGet("/", () => Results.Ok(new
        {
            service = "Сервис редиректа SmartLinks",
            redirectTemplate = "/s/{code}"
        }));

        app.MapControllers();

        app.Run();
    }
}
