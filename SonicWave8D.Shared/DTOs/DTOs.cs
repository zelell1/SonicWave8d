using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SonicWave8D.Shared.DTOs
{
    #region Auth DTOs

    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public UserDto? User { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class UpdateUserRequest
    {
        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(255)]
        public string? AvatarUrl { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    #endregion

    #region Track DTOs

    public class TrackDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? Genre { get; set; }
        public double Duration { get; set; }
        public string FileType { get; set; } = "audio/mpeg";
        public long FileSize { get; set; }
        public string? CoverImageUrl { get; set; }
        public List<double>? EqualizerSettings { get; set; }
        public Guid? PresetId { get; set; }
        public string? PresetName { get; set; }
        public bool Is8DEnabled { get; set; }
        public string Status { get; set; } = "Pending";
        public int PlayCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastPlayedAt { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class CreateTrackRequest
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Artist { get; set; }

        [MaxLength(255)]
        public string? Album { get; set; }

        [MaxLength(50)]
        public string? Genre { get; set; }

        public double Duration { get; set; }

        [Required]
        public string FileType { get; set; } = "audio/mpeg";

        public long FileSize { get; set; }

        public string? CoverImageUrl { get; set; }

        public bool Is8DEnabled { get; set; } = true;
    }

    public class UpdateTrackRequest
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(255)]
        public string? Artist { get; set; }

        [MaxLength(255)]
        public string? Album { get; set; }

        [MaxLength(50)]
        public string? Genre { get; set; }

        public string? CoverImageUrl { get; set; }

        public List<double>? EqualizerSettings { get; set; }

        public Guid? PresetId { get; set; }

        public bool? Is8DEnabled { get; set; }
    }

    public class TrackListResponse
    {
        public List<TrackDto> Tracks { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class TrackUploadResponse
    {
        public bool Success { get; set; }
        public Guid? TrackId { get; set; }
        public string? UploadUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion

    #region Preset DTOs

    public class PresetDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<double> Gains { get; set; } = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public bool IsPublic { get; set; }
        public bool IsSystem { get; set; }
        public bool IsFavorite { get; set; }
        public int UsageCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatorUsername { get; set; }
    }

    public class CreatePresetRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public List<double> Gains { get; set; } = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public bool IsPublic { get; set; } = false;
    }

    public class UpdatePresetRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public List<double>? Gains { get; set; }

        public bool? IsPublic { get; set; }

        public bool? IsFavorite { get; set; }
    }

    public class PresetListResponse
    {
        public List<PresetDto> Presets { get; set; } = new();
        public int TotalCount { get; set; }
    }

    #endregion

    #region Playlist DTOs

    public class PlaylistDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImageUrl { get; set; }
        public bool IsPublic { get; set; }
        public int TrackCount { get; set; }
        public double TotalDuration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<TrackDto>? Tracks { get; set; }
    }

    public class CreatePlaylistRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        public bool IsPublic { get; set; } = false;
    }

    public class UpdatePlaylistRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        public bool? IsPublic { get; set; }
    }

    public class AddTrackToPlaylistRequest
    {
        [Required]
        public Guid TrackId { get; set; }

        public int? Order { get; set; }
    }

    public class ReorderPlaylistRequest
    {
        [Required]
        public List<Guid> TrackIds { get; set; } = new();
    }

    public class PlaylistListResponse
    {
        public List<PlaylistDto> Playlists { get; set; } = new();
        public int TotalCount { get; set; }
    }

    #endregion

    #region Common DTOs

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
        public static ApiResponse<T> Fail(string error) => new() { Success = false, ErrorMessage = error };
        public static ApiResponse<T> Fail(List<string> errors) => new() { Success = false, Errors = errors };
    }

    public class PaginationParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        public string? Search { get; set; }
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? Url { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion
}
