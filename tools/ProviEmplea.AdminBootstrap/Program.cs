using System.ComponentModel.DataAnnotations;
using System.Text;
using Devart.Data.Oracle;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProviEmplea.Web.Data;
using ProviEmplea.Web.Data.Entities;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(ProviEmpleaDbContext).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();
var connectionString = configuration.GetConnectionString("Oracle");
var licenseKey = configuration["Devart:LicenseKey"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("No está configurada ConnectionStrings:Oracle en User Secrets o en el entorno.");
    return 1;
}

if (!string.IsNullOrWhiteSpace(licenseKey))
{
    OracleConnection.LoadLicenseKey(licenseKey);
}

Console.Write("Correo del administrador inicial: ");
var emailInput = Console.ReadLine()?.Trim();
if (string.IsNullOrWhiteSpace(emailInput) ||
    emailInput.Length > 255 ||
    !new EmailAddressAttribute().IsValid(emailInput))
{
    Console.Error.WriteLine("El correo ingresado no es válido.");
    return 1;
}

Console.Write("Contraseña inicial (no se mostrará): ");
var password = ReadSecret();
if (password.Length is < 12 or > 128 ||
    !password.Any(char.IsUpper) ||
    !password.Any(char.IsLower) ||
    !password.Any(char.IsDigit) ||
    !password.Any(character => !char.IsLetterOrDigit(character)))
{
    Console.Error.WriteLine("La contraseña debe tener entre 12 y 128 caracteres, mayúscula, minúscula, número y símbolo.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<ProviEmpleaDbContext>();
optionsBuilder.UseOracle(connectionString);

await using var db = new ProviEmpleaDbContext(optionsBuilder.Options);
if (!await db.Database.CanConnectAsync())
{
    Console.Error.WriteLine("No fue posible conectar con Oracle.");
    return 1;
}

if (await db.Users.AnyAsync(user => user.RoleId == 1 && user.DeletedAt == null))
{
    Console.Error.WriteLine("Ya existe un administrador activo. No se creó otro.");
    return 1;
}

var normalizedEmail = emailInput.ToLowerInvariant();
if (await db.Users.AnyAsync(user => user.Email == normalizedEmail))
{
    Console.Error.WriteLine("El correo ya está registrado.");
    return 1;
}

if (!await db.Roles.AnyAsync(role => role.Id == 1))
{
    Console.Error.WriteLine("No existe el rol admin. Ejecuta primero el bootstrap del esquema Oracle.");
    return 1;
}

db.Users.Add(new User
{
    Id = Guid.NewGuid(),
    RoleId = 1,
    Email = normalizedEmail,
    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10),
    ValidationStatus = "Aprobado"
});
await db.SaveChangesAsync();

Console.WriteLine($"Administrador inicial creado: {normalizedEmail}");
return 0;

static string ReadSecret()
{
    var secret = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return secret.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (secret.Length > 0)
            {
                secret.Length--;
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            secret.Append(key.KeyChar);
        }
    }
}
