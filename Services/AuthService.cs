using System;
using System.Text.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using SonicWave8D.Models;

namespace SonicWave8D.Services
{
    public class AuthService
    {
        private readonly StorageService _storageService;
        private readonly ILocalStorageService _localStorage;
        private const string USER_STORAGE_KEY = "sonicwave_user";

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public event Action? OnAuthStateChanged;

        public User? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;
        public bool IsLoading { get; private set; } = true;

        public AuthService(StorageService storageService, ILocalStorageService localStorage)
        {
            _storageService = storageService;
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                // Check if user is stored in local storage
                var userJson = await _localStorage.GetItemAsStringAsync(USER_STORAGE_KEY);

                if (!string.IsNullOrEmpty(userJson))
                {
                    CurrentUser = JsonSerializer.Deserialize<User>(userJson, _jsonOptions);
                    Console.WriteLine($"[AUTH] User loaded from storage: Id={CurrentUser?.Id}, Email={CurrentUser?.Email}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user from storage: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                OnAuthStateChanged?.Invoke();
            }
        }

        public async Task<(bool success, string? error)> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _storageService.LoginUserAsync(email, password);

                if (user == null)
                {
                    return (false, "Invalid email or password");
                }

                CurrentUser = user;
                await _localStorage.SetItemAsStringAsync(USER_STORAGE_KEY, JsonSerializer.Serialize(user, _jsonOptions));
                Console.WriteLine($"[AUTH] User logged in: Id={user.Id}, Email={user.Email}");
                OnAuthStateChanged?.Invoke();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool success, string? error)> RegisterAsync(string email, string password)
        {
            try
            {
                // Validate email format
                if (!IsValidEmail(email))
                {
                    return (false, "Invalid email format");
                }

                // Validate password strength
                if (password.Length < 6)
                {
                    return (false, "Password must be at least 6 characters");
                }

                var user = await _storageService.RegisterUserAsync(email, password);

                if (user == null)
                {
                    return (false, "User already exists");
                }

                CurrentUser = user;
                await _localStorage.SetItemAsStringAsync(USER_STORAGE_KEY, JsonSerializer.Serialize(user, _jsonOptions));
                Console.WriteLine($"[AUTH] User registered: Id={user.Id}, Email={user.Email}");
                OnAuthStateChanged?.Invoke();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task LogoutAsync()
        {
            CurrentUser = null;
            await _localStorage.RemoveItemAsync(USER_STORAGE_KEY);
            OnAuthStateChanged?.Invoke();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
