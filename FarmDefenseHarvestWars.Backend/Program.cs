using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurare SQLite
// Asigură-te că ai pachetul: Microsoft.EntityFrameworkCore.Sqlite
builder.Services.AddScoped<IDefaultUnitUnlockService, DefaultUnitUnlockService>();
builder.Services.AddScoped<DevelopmentTestUserSeeder>();
builder.Services.AddSingleton<DefaultUnitUnlockCreationInterceptor>();
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
    options
    .UseSqlite("Data Source=game_dev.db")
    .AddInterceptors(serviceProvider.GetRequiredService<DefaultUnitUnlockCreationInterceptor>()));

// 2. Configurare Identity (Login/Register)
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.Configure<IdentityOptions>(options =>
{
    // Dezactivează cerințele de complexitate
    options.Password.RequireDigit = false;           // Nu cere cifre (0-9)
    options.Password.RequireLowercase = false;       // Nu cere litere mici
    options.Password.RequireUppercase = false;       // Nu cere litere mari
    options.Password.RequireNonAlphanumeric = false; // Nu cere caractere speciale (!@#)
    options.Password.RequiredUniqueChars = 0;        // Nu cere caractere unice

    // Setează lungimea minimă (Default e 6, poți pune 1 sau 3 pentru teste)
    options.Password.RequiredLength = 3;
});

// 3. Controllere
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IMatchServerOrchestrator, ProcessMatchServerOrchestrator>();
builder.Services.AddSingleton<IUnitRegistryProvider, UnitRegistryProvider>();

// 4. Configurare SWAGGER (Versiunea NOUĂ pentru .NET 10 / Swashbuckle v10+)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Farm Defense API", Version = "v1" });

    // A. Definim Schema de Securitate
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    // B. Aplicăm cerința de securitate (SINTAXA NOUĂ)
    // Aceasta leagă butonul de lacăt de schema definită mai sus
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// 5. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    using (IServiceScope scope = app.Services.CreateScope())
    {
        DevelopmentTestUserSeeder testUserSeeder = scope.ServiceProvider.GetRequiredService<DevelopmentTestUserSeeder>();
        await testUserSeeder.SeedAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Ordinea contează aici!
app.UseAuthorization();

// Mapăm rutele automate de Identity (/register, /login)
app.MapIdentityApi<ApplicationUser>();

// Mapăm controllerele tale (Joc, Inventar, etc.)
app.MapControllers();

await app.RunAsync();
