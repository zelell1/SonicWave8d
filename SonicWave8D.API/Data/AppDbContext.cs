using Microsoft.EntityFrameworkCore;
using SonicWave8D.Shared.Models;

namespace SonicWave8D.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<CustomPreset> CustomPresets { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistTrack> PlaylistTracks { get; set; }
        public DbSet<FavoriteTrack> FavoriteTracks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
                entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            });

            // Track configuration
            modelBuilder.Entity<Track>(entity =>
            {
                entity.ToTable("tracks");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Title);
                entity.HasIndex(e => e.Artist);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Artist).HasColumnName("artist").HasMaxLength(255);
                entity.Property(e => e.Album).HasColumnName("album").HasMaxLength(255);
                entity.Property(e => e.Genre).HasColumnName("genre").HasMaxLength(50);
                entity.Property(e => e.Duration).HasColumnName("duration");
                entity.Property(e => e.OriginalFilePath).HasColumnName("original_file_path").HasMaxLength(500).IsRequired();
                entity.Property(e => e.ProcessedFilePath).HasColumnName("processed_file_path").HasMaxLength(500);
                entity.Property(e => e.FileType).HasColumnName("file_type").HasMaxLength(50).HasDefaultValue("audio/mpeg");
                entity.Property(e => e.FileSize).HasColumnName("file_size");
                entity.Property(e => e.CoverImageUrl).HasColumnName("cover_image_url").HasMaxLength(500);
                entity.Property(e => e.EqualizerSettings).HasColumnName("equalizer_settings").HasColumnType("jsonb");
                entity.Property(e => e.PresetId).HasColumnName("preset_id");
                entity.Property(e => e.Is8DEnabled).HasColumnName("is_8d_enabled").HasDefaultValue(true);
                entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
                entity.Property(e => e.PlayCount).HasColumnName("play_count").HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LastPlayedAt).HasColumnName("last_played_at");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Tracks)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Preset)
                    .WithMany(p => p.Tracks)
                    .HasForeignKey(e => e.PresetId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // CustomPreset configuration
            modelBuilder.Entity<CustomPreset>(entity =>
            {
                entity.ToTable("custom_presets");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IsPublic);
                entity.HasIndex(e => e.IsSystem);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
                entity.Property(e => e.Gains).HasColumnName("gains").HasColumnType("jsonb").IsRequired();
                entity.Property(e => e.IsPublic).HasColumnName("is_public").HasDefaultValue(false);
                entity.Property(e => e.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
                entity.Property(e => e.IsFavorite).HasColumnName("is_favorite").HasDefaultValue(false);
                entity.Property(e => e.UsageCount).HasColumnName("usage_count").HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.CustomPresets)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Playlist configuration
            modelBuilder.Entity<Playlist>(entity =>
            {
                entity.ToTable("playlists");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
                entity.Property(e => e.CoverImageUrl).HasColumnName("cover_image_url").HasMaxLength(500);
                entity.Property(e => e.IsPublic).HasColumnName("is_public").HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Playlists)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PlaylistTrack configuration (many-to-many)
            modelBuilder.Entity<PlaylistTrack>(entity =>
            {
                entity.ToTable("playlist_tracks");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.PlaylistId, e.TrackId }).IsUnique();
                entity.HasIndex(e => e.PlaylistId);
                entity.HasIndex(e => e.TrackId);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PlaylistId).HasColumnName("playlist_id").IsRequired();
                entity.Property(e => e.TrackId).HasColumnName("track_id").IsRequired();
                entity.Property(e => e.Order).HasColumnName("order").HasDefaultValue(0);
                entity.Property(e => e.AddedAt).HasColumnName("added_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Playlist)
                    .WithMany(p => p.PlaylistTracks)
                    .HasForeignKey(e => e.PlaylistId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Track)
                    .WithMany(t => t.PlaylistTracks)
                    .HasForeignKey(e => e.TrackId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // FavoriteTrack configuration
            modelBuilder.Entity<FavoriteTrack>(entity =>
            {
                entity.ToTable("favorite_tracks");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.TrackId }).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TrackId);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.TrackId).HasColumnName("track_id").IsRequired();
                entity.Property(e => e.AddedAt).HasColumnName("added_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.FavoriteTracks)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Track)
                    .WithMany(t => t.FavoritedBy)
                    .HasForeignKey(e => e.TrackId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed system presets
            SeedSystemPresets(modelBuilder);
        }

        private void SeedSystemPresets(ModelBuilder modelBuilder)
        {
            var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            // Создаём системного пользователя для системных пресетов
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = systemUserId,
                Email = "system@sonicwave8d.local",
                Username = "System",
                PasswordHash = "SYSTEM_USER_NO_LOGIN",
                IsActive = false,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            // Системные пресеты эквалайзера
            modelBuilder.Entity<CustomPreset>().HasData(
                new CustomPreset
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    UserId = systemUserId,
                    Name = "Flat (Default)",
                    Description = "Ровная АЧХ без изменений",
                    Gains = "[0,0,0,0,0,0,0,0,0,0]",
                    IsPublic = true,
                    IsSystem = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CustomPreset
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    UserId = systemUserId,
                    Name = "Bass Boost",
                    Description = "Усиление низких частот",
                    Gains = "[12,9,6,3,0,0,0,0,0,0]",
                    IsPublic = true,
                    IsSystem = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CustomPreset
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    UserId = systemUserId,
                    Name = "Bass Cut",
                    Description = "Ослабление низких частот",
                    Gains = "[-12,-9,-6,-3,0,0,0,0,0,0]",
                    IsPublic = true,
                    IsSystem = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CustomPreset
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                    UserId = systemUserId,
                    Name = "Treble Boost",
                    Description = "Усиление высоких частот",
                    Gains = "[0,0,0,0,0,3,6,9,12,12]",
                    IsPublic = true,
                    IsSystem = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CustomPreset
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                    UserId = systemUserId,
                    Name = "Electronic",
                    Description = "Для электронной музыки",
                    Gains = "[8,7,3,0,-3,0,2,6,8,8]",
                    IsPublic = true,
                    IsSystem = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CustomPreset
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                    UserId = systemUserId,
                    Name = "Vocal Booster",
                    Description = "Усиление вокала",
                    Gains = "[-3,-3,-3,2,6,6,4,2,0,-2]",
                    IsPublic = true,
                    IsSystem = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CustomPreset
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
                    UserId = systemUserId,
                    Name = "Pop",
                    Description = "Для поп-музыки",
                    Gains = "[4,3,0,-3,-3,-1,2,4,5,5]",
                    IsPublic = true,
                    IsSystem = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CustomPreset
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                    UserId = systemUserId,
                    Name = "Rock",
                    Description = "Для рок-музыки",
                    Gains = "[7,5,2,0,-2,0,3,6,8,8]",
                    IsPublic = true,
                    IsSystem = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new CustomPreset
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000009"),
                    UserId = systemUserId,
                    Name = "Jazz",
                    Description = "Для джаза",
                    Gains = "[4,3,1,3,-3,-3,0,2,4,5]",
                    IsPublic = true,
                    IsSystem = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
