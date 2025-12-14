using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using SonicWave8D.Models;

namespace SonicWave8D.Services
{
    public class StorageService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public StorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/storage.js").AsTask());
        }

        // Initialize the database
        public async Task InitializeAsync()
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("initializeDB");
        }

        // User Authentication Methods
        public async Task<User?> RegisterUserAsync(string email, string password)
        {
            try
            {
                var module = await _moduleTask.Value;
                var userJson = await module.InvokeAsync<string>("registerUser", email, password);

                if (string.IsNullOrEmpty(userJson))
                    return null;

                var user = JsonSerializer.Deserialize<User>(userJson, _jsonOptions);
                Console.WriteLine($"[STORAGE] RegisterUser deserialized: Id={user?.Id}, Email={user?.Email}");
                return user;
            }
            catch (JSException ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public async Task<User?> LoginUserAsync(string email, string password)
        {
            try
            {
                var module = await _moduleTask.Value;
                var userJson = await module.InvokeAsync<string>("loginUser", email, password);

                if (string.IsNullOrEmpty(userJson))
                    return null;

                var user = JsonSerializer.Deserialize<User>(userJson, _jsonOptions);
                Console.WriteLine($"[STORAGE] LoginUser deserialized: Id={user?.Id}, Email={user?.Email}");
                return user;
            }
            catch (JSException ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        // Track Management Methods
        public async Task SaveTrackAsync(AudioTrack track)
        {
            try
            {
                var module = await _moduleTask.Value;
                var trackJson = JsonSerializer.Serialize(track, _jsonOptions);
                Console.WriteLine($"[STORAGE] SaveTrack serializing: Id={track.Id}, UserId={track.UserId}");
                await module.InvokeVoidAsync("saveTrack", trackJson);
            }
            catch (JSException ex)
            {
                Console.WriteLine($"Save track error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<AudioTrack>> GetUserTracksAsync(string userId)
        {
            try
            {
                var module = await _moduleTask.Value;
                var tracksJson = await module.InvokeAsync<string>("getUserTracks", userId);

                if (string.IsNullOrEmpty(tracksJson))
                    return new List<AudioTrack>();

                var tracks = JsonSerializer.Deserialize<List<AudioTrack>>(tracksJson, _jsonOptions);
                Console.WriteLine($"[STORAGE] GetUserTracks for userId={userId}: found {tracks?.Count ?? 0} tracks");
                return tracks ?? new List<AudioTrack>();
            }
            catch (JSException ex)
            {
                Console.WriteLine($"Get tracks error: {ex.Message}");
                return new List<AudioTrack>();
            }
        }

        public async Task DeleteTrackAsync(string trackId)
        {
            try
            {
                var module = await _moduleTask.Value;
                await module.InvokeVoidAsync("deleteTrack", trackId);
            }
            catch (JSException ex)
            {
                Console.WriteLine($"Delete track error: {ex.Message}");
                throw;
            }
        }

        public async Task<AudioTrack?> GetTrackByIdAsync(string trackId)
        {
            try
            {
                var module = await _moduleTask.Value;
                var trackJson = await module.InvokeAsync<string>("getTrackById", trackId);

                if (string.IsNullOrEmpty(trackJson))
                    return null;

                return JsonSerializer.Deserialize<AudioTrack>(trackJson, _jsonOptions);
            }
            catch (JSException ex)
            {
                Console.WriteLine($"Get track error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ClearDatabaseAsync()
        {
            try
            {
                var module = await _moduleTask.Value;
                await module.InvokeVoidAsync("clearDatabase");
                Console.WriteLine("[STORAGE] Database cleared successfully");
                return true;
            }
            catch (JSException ex)
            {
                Console.WriteLine($"[STORAGE] Error clearing database: {ex.Message}");
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
