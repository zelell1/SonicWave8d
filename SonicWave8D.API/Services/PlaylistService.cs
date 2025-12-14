using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SonicWave8D.API.Data;
using SonicWave8D.Shared.DTOs;
using SonicWave8D.Shared.Models;

namespace SonicWave8D.API.Services
{
    public interface IPlaylistService
    {
        Task<PlaylistDto?> GetByIdAsync(Guid playlistId, Guid? userId = null);
        Task<PlaylistListResponse> GetUserPlaylistsAsync(Guid userId);
        Task<PlaylistListResponse> GetPublicPlaylistsAsync(PaginationParams pagination);
        Task<PlaylistDto> CreateAsync(Guid userId, CreatePlaylistRequest request);
        Task<PlaylistDto?> UpdateAsync(Guid playlistId, Guid userId, UpdatePlaylistRequest request);
        Task<bool> DeleteAsync(Guid playlistId, Guid userId);
        Task<bool> AddTrackAsync(Guid playlistId, Guid userId, AddTrackToPlaylistRequest request);
        Task<bool> RemoveTrackAsync(Guid playlistId, Guid userId, Guid trackId);
        Task<bool> ReorderTracksAsync(Guid playlistId, Guid userId, ReorderPlaylistRequest request);
        Task<PlaylistDto?> GetPlaylistWithTracksAsync(Guid playlistId, Guid? userId = null);
    }

    public class PlaylistService : IPlaylistService
    {
        private readonly AppDbContext _context;

        public PlaylistService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PlaylistDto?> GetByIdAsync(Guid playlistId, Guid? userId = null)
        {
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistTracks)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
                return null;

            // Проверяем доступ - можно смотреть свои или публичные плейлисты
            if (!playlist.IsPublic && (!userId.HasValue || playlist.UserId != userId.Value))
                return null;

            return MapToDto(playlist);
        }

        public async Task<PlaylistListResponse> GetUserPlaylistsAsync(Guid userId)
        {
            var playlists = await _context.Playlists
                .Include(p => p.PlaylistTracks)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();

            return new PlaylistListResponse
            {
                Playlists = playlists.Select(p => MapToDto(p)).ToList(),
                TotalCount = playlists.Count
            };
        }

        public async Task<PlaylistListResponse> GetPublicPlaylistsAsync(PaginationParams pagination)
        {
            var query = _context.Playlists
                .Include(p => p.PlaylistTracks)
                .Where(p => p.IsPublic)
                .OrderByDescending(p => p.PlaylistTracks.Count)
                .ThenByDescending(p => p.UpdatedAt);

            var totalCount = await query.CountAsync();

            var playlists = await query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PlaylistListResponse
            {
                Playlists = playlists.Select(p => MapToDto(p)).ToList(),
                TotalCount = totalCount
            };
        }

        public async Task<PlaylistDto> CreateAsync(Guid userId, CreatePlaylistRequest request)
        {
            var playlist = new Playlist
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name,
                Description = request.Description,
                CoverImageUrl = request.CoverImageUrl,
                IsPublic = request.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();

            return MapToDto(playlist);
        }

        public async Task<PlaylistDto?> UpdateAsync(Guid playlistId, Guid userId, UpdatePlaylistRequest request)
        {
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistTracks)
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);

            if (playlist == null)
                return null;

            if (!string.IsNullOrEmpty(request.Name))
                playlist.Name = request.Name;

            if (request.Description != null)
                playlist.Description = request.Description;

            if (request.CoverImageUrl != null)
                playlist.CoverImageUrl = request.CoverImageUrl;

            if (request.IsPublic.HasValue)
                playlist.IsPublic = request.IsPublic.Value;

            playlist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(playlist);
        }

        public async Task<bool> DeleteAsync(Guid playlistId, Guid userId)
        {
            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);

            if (playlist == null)
                return false;

            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AddTrackAsync(Guid playlistId, Guid userId, AddTrackToPlaylistRequest request)
        {
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistTracks)
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);

            if (playlist == null)
                return false;

            // Проверяем, существует ли трек
            var trackExists = await _context.Tracks.AnyAsync(t => t.Id == request.TrackId);
            if (!trackExists)
                return false;

            // Проверяем, не добавлен ли уже трек в плейлист
            var alreadyExists = await _context.PlaylistTracks
                .AnyAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == request.TrackId);

            if (alreadyExists)
                return true; // Уже добавлен

            // Определяем порядок
            var maxOrder = playlist.PlaylistTracks.Any()
                ? playlist.PlaylistTracks.Max(pt => pt.Order)
                : -1;

            var playlistTrack = new PlaylistTrack
            {
                Id = Guid.NewGuid(),
                PlaylistId = playlistId,
                TrackId = request.TrackId,
                Order = request.Order ?? maxOrder + 1,
                AddedAt = DateTime.UtcNow
            };

            _context.PlaylistTracks.Add(playlistTrack);

            playlist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveTrackAsync(Guid playlistId, Guid userId, Guid trackId)
        {
            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);

            if (playlist == null)
                return false;

            var playlistTrack = await _context.PlaylistTracks
                .FirstOrDefaultAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId);

            if (playlistTrack == null)
                return false;

            _context.PlaylistTracks.Remove(playlistTrack);

            playlist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Перенумеровываем оставшиеся треки
            var remainingTracks = await _context.PlaylistTracks
                .Where(pt => pt.PlaylistId == playlistId)
                .OrderBy(pt => pt.Order)
                .ToListAsync();

            for (int i = 0; i < remainingTracks.Count; i++)
            {
                remainingTracks[i].Order = i;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReorderTracksAsync(Guid playlistId, Guid userId, ReorderPlaylistRequest request)
        {
            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);

            if (playlist == null)
                return false;

            var playlistTracks = await _context.PlaylistTracks
                .Where(pt => pt.PlaylistId == playlistId)
                .ToListAsync();

            // Проверяем, что все переданные trackId существуют в плейлисте
            var existingTrackIds = playlistTracks.Select(pt => pt.TrackId).ToHashSet();
            if (!request.TrackIds.All(id => existingTrackIds.Contains(id)))
                return false;

            // Обновляем порядок
            for (int i = 0; i < request.TrackIds.Count; i++)
            {
                var playlistTrack = playlistTracks.FirstOrDefault(pt => pt.TrackId == request.TrackIds[i]);
                if (playlistTrack != null)
                {
                    playlistTrack.Order = i;
                }
            }

            playlist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<PlaylistDto?> GetPlaylistWithTracksAsync(Guid playlistId, Guid? userId = null)
        {
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistTracks)
                    .ThenInclude(pt => pt.Track)
                        .ThenInclude(t => t!.Preset)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
                return null;

            // Проверяем доступ
            if (!playlist.IsPublic && (!userId.HasValue || playlist.UserId != userId.Value))
                return null;

            var dto = MapToDto(playlist);

            // Добавляем треки с полной информацией
            dto.Tracks = playlist.PlaylistTracks
                .Where(pt => pt.Track != null)
                .OrderBy(pt => pt.Order)
                .Select(pt => MapTrackToDto(pt.Track!, userId))
                .ToList();

            return dto;
        }

        private static PlaylistDto MapToDto(Playlist playlist)
        {
            var totalDuration = 0.0;

            if (playlist.PlaylistTracks != null && playlist.PlaylistTracks.Any())
            {
                foreach (var pt in playlist.PlaylistTracks)
                {
                    if (pt.Track != null)
                    {
                        totalDuration += pt.Track.Duration;
                    }
                }
            }

            return new PlaylistDto
            {
                Id = playlist.Id,
                UserId = playlist.UserId,
                Name = playlist.Name,
                Description = playlist.Description,
                CoverImageUrl = playlist.CoverImageUrl,
                IsPublic = playlist.IsPublic,
                TrackCount = playlist.PlaylistTracks?.Count ?? 0,
                TotalDuration = totalDuration,
                CreatedAt = playlist.CreatedAt,
                UpdatedAt = playlist.UpdatedAt
            };
        }

        private static TrackDto MapTrackToDto(Track track, Guid? userId)
        {
            List<double>? equalizerSettings = null;
            if (!string.IsNullOrEmpty(track.EqualizerSettings))
            {
                try
                {
                    equalizerSettings = JsonSerializer.Deserialize<List<double>>(track.EqualizerSettings);
                }
                catch { }
            }

            return new TrackDto
            {
                Id = track.Id,
                UserId = track.UserId,
                Title = track.Title,
                Artist = track.Artist,
                Album = track.Album,
                Genre = track.Genre,
                Duration = track.Duration,
                FileType = track.FileType,
                FileSize = track.FileSize,
                CoverImageUrl = track.CoverImageUrl,
                EqualizerSettings = equalizerSettings,
                PresetId = track.PresetId,
                PresetName = track.Preset?.Name,
                Is8DEnabled = track.Is8DEnabled,
                Status = track.Status.ToString(),
                PlayCount = track.PlayCount,
                CreatedAt = track.CreatedAt,
                LastPlayedAt = track.LastPlayedAt,
                IsFavorite = false // Будет установлено отдельно при необходимости
            };
        }
    }
}
