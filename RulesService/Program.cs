using RulesService.SmartLinks;
using RulesService.SmartLinks.Interfaces;
using SmartLinks.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace RulesService;

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddRulesEngine(builder.Configuration);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapPost("/rules/handle", (RuleHandlerRequest request, IRuleHandler handler) =>
        {
            return Results.Ok(handler.Handle(request));
        });

        app.MapGet("/", () => Results.Ok(new { service = "Сервис правил SmartLinks" }));

        app.Run();
    }
}
