using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SonicWave8D.API.Data;
using SonicWave8D.Shared.DTOs;
using SonicWave8D.Shared.Models;

namespace SonicWave8D.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<UserDto?> GetUserByIdAsync(Guid userId);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserRequest request);
        string GenerateJwtToken(User user);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Проверяем, существует ли пользователь с таким email
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "Пользователь с таким email уже существует"
                };
            }

            // Проверяем, существует ли пользователь с таким username
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "Пользователь с таким именем уже существует"
                };
            }

            // Создаём нового пользователя
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLower(),
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Генерируем токен
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            return new AuthResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                User = MapToDto(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpireMinutes())
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Ищем пользователя по email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "Неверный email или пароль"
                };
            }

            // Проверяем пароль
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "Неверный email или пароль"
                };
            }

            // Проверяем, активен ли пользователь
            if (!user.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "Аккаунт деактивирован"
                };
            }

            // Обновляем время последнего входа
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Генерируем токен
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            return new AuthResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                User = MapToDto(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpireMinutes())
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            // В простой реализации просто проверяем, что токен не пустой
            // В продакшене нужно хранить refresh токены в БД
            if (string.IsNullOrEmpty(refreshToken))
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid refresh token"
                };
            }

            // Для демо возвращаем ошибку - нужна полная реализация с хранением токенов
            return new AuthResponse
            {
                Success = false,
                ErrorMessage = "Refresh token validation not implemented"
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user != null ? MapToDto(user) : null;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Проверяем текущий пароль
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return false;

            // Устанавливаем новый пароль
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            if (!string.IsNullOrEmpty(request.Username))
            {
                // Проверяем уникальность нового username
                if (await _context.Users.AnyAsync(u => u.Id != userId && u.Username.ToLower() == request.Username.ToLower()))
                    return null;

                user.Username = request.Username;
            }

            if (request.AvatarUrl != null)
                user.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync();

            return MapToDto(user);
        }

        public string GenerateJwtToken(User user)
        {
            var key = _configuration["Jwt:Key"] ?? "SonicWave8D_SuperSecretKey_ChangeInProduction_MinLength32Chars!";
            var issuer = _configuration["Jwt:Issuer"] ?? "SonicWave8D.API";
            var audience = _configuration["Jwt:Audience"] ?? "SonicWave8D.Client";
            var expireMinutes = GetTokenExpireMinutes();

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private int GetTokenExpireMinutes()
        {
            return int.TryParse(_configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 60;
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
