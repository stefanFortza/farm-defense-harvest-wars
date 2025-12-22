using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurare SQLite
// Asigură-te că ai pachetul: Microsoft.EntityFrameworkCore.Sqlite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=game_dev.db"));

// 2. Configurare Identity (Login/Register)
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 3. Controllere
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

app.Run();
