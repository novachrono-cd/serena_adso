using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1;
using WebApplication1.interfaces;
using WebApplication1.models;
using WebApplication1.repositories;
using WebApplication1.services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("SERENA_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection o SERENA_CONNECTION_STRING antes de iniciar la API.");

var jwtKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("SERENA_JWT_KEY");
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Configure Jwt:Key con al menos 32 bytes mediante SERENA_JWT_KEY.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Serena";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Serena";

builder.Services.AddDbContext<SerenaDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.Services.AddScoped<ICita, CitaRepository>();
builder.Services.AddScoped<IDiario, DiarioRepository>();
builder.Services.AddScoped<IEmergencia, EmergenciaRepository>();
builder.Services.AddScoped<IEstadoDeAnimo, EstadoDeAnimoRepository>();
builder.Services.AddScoped<IEstadoAnimoUsuario, EstadoAnimoUsuarioRepository>();
builder.Services.AddScoped<IFormulario, FormularioRepository>();
builder.Services.AddScoped<IHistorialCita, HistorialCitaRepository>();
builder.Services.AddScoped<IHistorialClinico, HistorialClinicoRepository>();
builder.Services.AddScoped<IMenu, MenuRepository>();
builder.Services.AddScoped<IMenuRol, MenuRolRepository>();
builder.Services.AddScoped<IDisponibilidad, DisponibilidadRepository>();
builder.Services.AddScoped<IFicha, FichaRepository>();
builder.Services.AddScoped<IUsuarioFicha, UsuarioFichaRepository>();
builder.Services.AddScoped<IPublicaciones, PublicacionesRepository>();
builder.Services.AddScoped<IRol, RolRepository>();
builder.Services.AddScoped<IUsuario, UsuarioRepository>();
builder.Services.AddScoped<ILoginService, UsuarioRepository>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options => options.AddPolicy("DevelopmentFrontend", policy =>
    policy.WithOrigins(
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
            [
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:5173",
                "http://127.0.0.1:5173"
            ])
          .AllowAnyHeader()
          .AllowAnyMethod()));

builder.Services.AddSwaggerGen(c =>
{
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.CustomSchemaIds(type => type.FullName);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SerenaDbContext>();
    await db.Database.MigrateAsync();

    var rolesIniciales = new[] { "Aprendiz", "Psicólogo", "Administrador" };
    var rolesExistentes = (await db.Rol.Select(role => role.nombre_rol).ToListAsync()).ToHashSet();
    foreach (var role in rolesIniciales.Where(role => !rolesExistentes.Contains(role)))
    {
        db.Rol.Add(new Rol { nombre_rol = role });
    }
    await db.SaveChangesAsync();

    var estadosIniciales = new[] { "Feliz", "Calmado", "Ansioso", "Triste", "Motivado" };
    var estadosExistentes = (await db.EstadoDeAnimo.Select(state => state.nombre_estado).ToListAsync()).ToHashSet();
    foreach (var state in estadosIniciales.Where(state => !estadosExistentes.Contains(state)))
    {
        db.EstadoDeAnimo.Add(new EstadoDeAnimo { nombre_estado = state });
    }
    await db.SaveChangesAsync();

    if (!await db.Comunidad.AnyAsync(comunidad => comunidad.id == "s/CMTC"))
    {
        db.Comunidad.Add(new Comunidad
        {
            id = "s/CMTC",
            centro = "CMTC",
            sub_nombre = "General",
            nombre_completo = "Comunidad CMTC",
            descripcion = "Espacio general de la comunidad CMTC.",
            icono = "Building2",
            es_oficial = true
        });
        await db.SaveChangesAsync();
    }
}

app.MapGet("/health", async (SerenaDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "healthy" })
        : Results.Problem("No se pudo conectar con SQL Server.", statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    });
    app.UseCors("DevelopmentFrontend");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();