using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BbfFirmasPoderes.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:Issuer", TestAuth.Issuer);
        builder.UseSetting("Jwt:Audience", TestAuth.Audience);
        builder.UseSetting("Jwt:SigningKey", TestAuth.SigningKey);
        builder.UseSetting(
            "ConnectionStrings:Postgres",
            "Host=127.0.0.1;Port=5432;Database=bbf_firmas_test;Username=bbf;Password=bbf");
    }
}
