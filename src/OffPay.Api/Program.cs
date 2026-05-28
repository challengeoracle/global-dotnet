using Microsoft.EntityFrameworkCore;
using OffPay.Infrastructure.Persistencia.Oracle;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<OffPayDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("Oracle")));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
