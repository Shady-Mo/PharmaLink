
var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Domain.Entities.AppUser>();
var dummyUser = new Domain.Entities.AppUser { Email = "test@test.com" };
var hash = hasher.HashPassword(dummyUser, "P@ssword123");
Console.WriteLine($"HASH_VALUE_START:{hash}:HASH_VALUE_END");

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiServices()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddJwtAuthorization();
    

var app = builder.Build();

app.UseSwaggerDocs();

app.UseScalarDocs();

app.UseHttpsRedirection();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();