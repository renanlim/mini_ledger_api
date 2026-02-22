using Microsoft.EntityFrameworkCore;
using BtgLedger.Infrastructure.Persistence;
using BtgLedger.Domain.Interfaces;
using BtgLedger.Infrastructure.Repositories;
using BtgLedger.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuração de Segurança: Define a chave simétrica para validar a assinatura dos Tokens JWT
var key = Encoding.ASCII.GetBytes("MinhaChaveSuperSecretaDoBtgLedger2026!!");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; // Apenas para ambiente de desenvolvimento
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddControllers();

// Habilita o cache volátil (RAM) para armazenar os códigos de 2FA temporariamente
builder.Services.AddMemoryCache(); 

builder.Services.AddOpenApi();

// Configura o CORS para permitir que o Front-end (React/Vite) acesse a API em portas diferentes
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Injeção da String de Conexão (O .NET buscará aqui ou no User Secrets conforme configuramos)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Injeção de Dependência (DI): Define o tempo de vida dos serviços e repositórios
builder.Services.AddScoped<IAccountQueryRepository, AccountRepository>();
builder.Services.AddScoped<IAccountCommandRepository, AccountRepository>();
builder.Services.AddSingleton<IMessageBusService, RabbitMqService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Ordem do Middleware: CORS deve vir antes da Autenticação/Autorização para evitar erros 403 no pre-flight
app.UseCors("AllowAll");
app.UseAuthentication(); 
app.UseAuthorization();  

app.UseHttpsRedirection();
app.MapControllers();

// Automatização de Infraestrutura: Garante que o banco de dados esteja atualizado com o schema mais recente ao iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();