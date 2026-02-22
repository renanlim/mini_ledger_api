using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BtgLedger.Domain.Interfaces;
using BtgLedger.Domain.Entities;
using BtgLedger.API.DTOs;
using BtgLedger.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authorization;

namespace BtgLedger.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountQueryRepository _queryRepository;
        private readonly IAccountCommandRepository _commandRepository;
        private readonly IMessageBusService _messageBus;
        private readonly IMemoryCache _cache;

        public AccountsController(
            IAccountQueryRepository queryRepository, 
            IAccountCommandRepository commandRepository,
            IMessageBusService messageBus,
            IMemoryCache cache)
        {
            _queryRepository = queryRepository;
            _commandRepository = commandRepository;
            _messageBus = messageBus;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            string agency = "0001";
            string accountNumber = new Random().Next(100000, 999999).ToString();
            
            // Criptografa a senha com BCrypt antes de persistir no banco, 
            // garantindo que a credencial nunca seja armazenada em texto plano.
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password); 

            var account = new Account(request.OwnerName, request.InitialBalance, agency, accountNumber, passwordHash, request.PhoneNumber);

            await _commandRepository.CreateAsync(account);
            
            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var account = await _queryRepository.GetByAgencyAndNumberAsync(request.Agency, request.Number);
            
            // Valida a existência da conta e verifica a senha informada contra o hash armazenado
            if (account == null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
                return Unauthorized(new { error = "Agência, conta ou senha inválidos." });

            string pin = new Random().Next(100000, 999999).ToString();

            // Usa o MemoryCache com TTL (Time-To-Live) de 5 minutos para o PIN de 2FA.
            // Isso evita onerar o banco de dados principal com dados efêmeros e voláteis.
            _cache.Set($"2FA_{account.Id}", pin, TimeSpan.FromMinutes(5));

            var smsEvent = new { 
                PhoneNumber = account.PhoneNumber, 
                Message = $"BTG Ledger: Seu codigo de acesso: {pin}. Nao compartilhe com ninguem." 
            };
            
            // Publica o evento de SMS de forma assíncrona via mensageria (RabbitMQ), 
            // liberando a requisição HTTP sem precisar aguardar o serviço de telefonia.
            await _messageBus.PublishEventAsync("sms_notifications", smsEvent);

            return Ok(new { 
                message = "PIN de validação enviado por SMS.", 
                accountId = account.Id,
                requires2FA = true 
            });
        }

        [HttpPost("validate-pin")]
        public async Task<IActionResult> ValidatePin([FromBody] ValidatePinRequest request)
        {
            if (!_cache.TryGetValue($"2FA_{request.AccountId}", out string? savedPin) || savedPin != request.Pin)
            {
                return Unauthorized(new { error = "PIN inválido ou expirado." });
            }

            // Invalida e remove o PIN do cache imediatamente após o uso bem-sucedido 
            // para prevenir ataques de repetição (Replay Attacks).
            _cache.Remove($"2FA_{request.AccountId}");

            // Gera um token JWT com a Claim do AccountId, permitindo a autenticação
            // stateless (sem estado) para as próximas requisições da sessão.
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("MinhaChaveSuperSecretaDoBtgLedger2026!!"); 
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, request.AccountId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new { 
                token = tokenHandler.WriteToken(token),
                accountId = request.AccountId
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccount(Guid id)
        {
            var account = await _queryRepository.GetByIdAsync(id);
            if (account == null) return NotFound("Account not found.");
            
            return Ok(new { account.Id, account.OwnerName, account.Balance, account.Agency, account.Number });
        }

        // A anotação [Authorize] bloqueia o endpoint, garantindo que a transação 
        // só seja processada se a requisição contiver um Token JWT válido no Header.
        [Authorize]
        [HttpPost("{id}/transaction")]
        public async Task<IActionResult> ProcessTransaction(Guid id, [FromBody] TransactionRequest request)
        {
            try
            {
                await _commandRepository.AddTransactionAsync(id, request.Amount, request.Type);
                return Ok(new { message = "Transaction processed successfully." });
            }
            catch (Exception ex) 
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}