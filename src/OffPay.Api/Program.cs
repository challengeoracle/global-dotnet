using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OffPay.Api.Extensions;
using OffPay.Api.Middleware;
using OffPay.Infrastructure.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddMongoDB();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwagger();
builder.Services.AddApplicationServices();

builder.Services.AddHealthChecks()
    .AddOracle(
        builder.Configuration.GetConnectionString("Oracle")!,
        name: "oracle",
        tags: ["ready"])
    .AddCheck<MongoHealthCheck>("mongodb", tags: ["ready"]);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.Run();
