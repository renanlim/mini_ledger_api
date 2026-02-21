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
using Microsoft.AspNetCore.Authorization; // NOVO USING

namespace BtgLedger.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountQueryRepository _queryRepository;
        private readonly IAccountCommandRepository _commandRepository;
        private readonly IMessageBusService _messageBus;
        private readonly IMemoryCache _cache; // Injetamos o Cache para o 2FA

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

        // 1. CRIAR CONTA (Agora com BCrypt)
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            string agency = "0001";
            string accountNumber = new Random().Next(100000, 999999).ToString();
            
            // Criptografa a senha antes de salvar no banco!
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password); 

            var account = new Account(request.OwnerName, request.InitialBalance, agency, accountNumber, passwordHash, request.PhoneNumber);

            await _commandRepository.CreateAsync(account);
            
            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
        }

        // 2. LOGIN (Verifica a senha e envia o PIN via RabbitMQ)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var account = await _queryRepository.GetByAgencyAndNumberAsync(request.Agency, request.Number);
            
            if (account == null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
                return Unauthorized(new { error = "Agência, conta ou senha inválidos." });

            // Gera um PIN de 6 dígitos
            string pin = new Random().Next(100000, 999999).ToString();

            // Salva o PIN no Cache por 5 minutos (evita sujar o banco de dados)
            _cache.Set($"2FA_{account.Id}", pin, TimeSpan.FromMinutes(5));

            // Simula o envio do SMS publicando na fila do RabbitMQ
            var smsEvent = new { 
                PhoneNumber = account.PhoneNumber, 
                Message = $"BTG Ledger: Seu codigo de acesso: {pin}. Nao compartilhe com ninguem." 
            };
            await _messageBus.PublishEventAsync("sms_notifications", smsEvent);

            // Retorna o ID da conta para o React saber de quem é o PIN que ele vai validar
            return Ok(new { 
                message = "PIN de validação enviado por SMS.", 
                accountId = account.Id,
                requires2FA = true 
            });
        }

        // 3. VALIDAR PIN E GERAR TOKEN JWT
        [HttpPost("validate-pin")]
        public async Task<IActionResult> ValidatePin([FromBody] ValidatePinRequest request)
        {
            // Busca o PIN no cache
            if (!_cache.TryGetValue($"2FA_{request.AccountId}", out string? savedPin) || savedPin != request.Pin)
            {
                return Unauthorized(new { error = "PIN inválido ou expirado." });
            }

            // PIN correto! Remove do cache para não ser usado de novo
            _cache.Remove($"2FA_{request.AccountId}");

            // Gera o Token JWT Fake (Para o React usar na sessão)
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

        // ... OS ENDPOINTS GET E TRANSACTION CONTINUAM IGUAIS AQUI ...
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccount(Guid id)
        {
            var account = await _queryRepository.GetByIdAsync(id);
            if (account == null) return NotFound("Account not found.");
            return Ok(new { account.Id, account.OwnerName, account.Balance, account.Agency, account.Number }); // Retornamos agência e conta agora
        }

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