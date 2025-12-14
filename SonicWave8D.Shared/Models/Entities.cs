using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SonicWave8D.Shared.Models
{
    /// <summary>
    /// Пользователь системы
    /// </summary>
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
        public virtual ICollection<CustomPreset> CustomPresets { get; set; } = new List<CustomPreset>();
        public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
        public virtual ICollection<FavoriteTrack> FavoriteTracks { get; set; } = new List<FavoriteTrack>();
    }

    /// <summary>
    /// Музыкальный трек
    /// </summary>
    public class Track
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Artist { get; set; }

        [MaxLength(255)]
        public string? Album { get; set; }

        [MaxLength(50)]
        public string? Genre { get; set; }

        /// <summary>
        /// Длительность трека в секундах
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// Путь к оригинальному файлу
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string OriginalFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Путь к обработанному файлу (8D)
        /// </summary>
        [MaxLength(500)]
        public string? ProcessedFilePath { get; set; }

        /// <summary>
        /// MIME тип файла
        /// </summary>
        [MaxLength(50)]
        public string FileType { get; set; } = "audio/mpeg";

        /// <summary>
        /// Размер файла в байтах
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// URL обложки
        /// </summary>
        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        /// <summary>
        /// Текущие настройки эквалайзера (JSON)
        /// </summary>
        public string? EqualizerSettings { get; set; }

        /// <summary>
        /// ID выбранного пресета
        /// </summary>
        public Guid? PresetId { get; set; }

        /// <summary>
        /// Включён ли 8D эффект
        /// </summary>
        public bool Is8DEnabled { get; set; } = true;

        public TrackStatus Status { get; set; } = TrackStatus.Pending;

        public int PlayCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastPlayedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("PresetId")]
        public virtual CustomPreset? Preset { get; set; }

        public virtual ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
        public virtual ICollection<FavoriteTrack> FavoritedBy { get; set; } = new List<FavoriteTrack>();
    }

    /// <summary>
    /// Статус обработки трека
    /// </summary>
    public enum TrackStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Error = 3
    }

    /// <summary>
    /// Кастомный пресет эквалайзера
    /// </summary>
    public class CustomPreset
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Настройки эквалайзера (10 значений от -12 до +12 дБ)
        /// Частоты: 60, 170, 310, 600, 1000, 3000, 6000, 12000, 14000, 16000 Hz
        /// Хранится как JSON массив
        /// </summary>
        [Required]
        public string Gains { get; set; } = "[0,0,0,0,0,0,0,0,0,0]";

        /// <summary>
        /// Публичный пресет (виден всем)
        /// </summary>
        public bool IsPublic { get; set; } = false;

        /// <summary>
        /// Системный пресет (создан администратором)
        /// </summary>
        public bool IsSystem { get; set; } = false;

        /// <summary>
        /// В избранном у пользователя
        /// </summary>
        public bool IsFavorite { get; set; } = false;

        /// <summary>
        /// Количество использований
        /// </summary>
        public int UsageCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
    }

    /// <summary>
    /// Плейлист пользователя
    /// </summary>
    public class Playlist
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        /// <summary>
        /// Публичный плейлист
        /// </summary>
        public bool IsPublic { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
    }

    /// <summary>
    /// Связь многие-ко-многим между плейлистами и треками
    /// </summary>
    public class PlaylistTrack
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PlaylistId { get; set; }

        [Required]
        public Guid TrackId { get; set; }

        /// <summary>
        /// Порядок трека в плейлисте
        /// </summary>
        public int Order { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PlaylistId")]
        public virtual Playlist? Playlist { get; set; }

        [ForeignKey("TrackId")]
        public virtual Track? Track { get; set; }
    }

    /// <summary>
    /// Избранные треки пользователя
    /// </summary>
    public class FavoriteTrack
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid TrackId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("TrackId")]
        public virtual Track? Track { get; set; }
    }
}
