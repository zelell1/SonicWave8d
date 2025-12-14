using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SonicWave8D.API.Data;
using SonicWave8D.Shared.DTOs;
using SonicWave8D.Shared.Models;

namespace SonicWave8D.API.Services
{
    public interface ITrackService
    {
        Task<TrackDto?> GetByIdAsync(Guid trackId, Guid? userId = null);
        Task<TrackListResponse> GetUserTracksAsync(Guid userId, PaginationParams pagination);
        Task<TrackDto> CreateAsync(Guid userId, CreateTrackRequest request, string filePath);
        Task<TrackDto?> UpdateAsync(Guid trackId, Guid userId, UpdateTrackRequest request);
        Task<bool> DeleteAsync(Guid trackId, Guid userId);
        Task<bool> IncrementPlayCountAsync(Guid trackId);
        Task<bool> AddToFavoritesAsync(Guid userId, Guid trackId);
        Task<bool> RemoveFromFavoritesAsync(Guid userId, Guid trackId);
        Task<TrackListResponse> GetFavoriteTracksAsync(Guid userId, PaginationParams pagination);
        Task<TrackListResponse> SearchTracksAsync(Guid userId, string query, PaginationParams pagination);
    }

    public class TrackService : ITrackService
    {
        private readonly AppDbContext _context;

        public TrackService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TrackDto?> GetByIdAsync(Guid trackId, Guid? userId = null)
        {
            var track = await _context.Tracks
                .Include(t => t.Preset)
                .FirstOrDefaultAsync(t => t.Id == trackId);

            if (track == null)
                return null;

            var isFavorite = userId.HasValue && await _context.FavoriteTracks
                .AnyAsync(f => f.UserId == userId.Value && f.TrackId == trackId);

            return MapToDto(track, isFavorite);
        }

        public async Task<TrackListResponse> GetUserTracksAsync(Guid userId, PaginationParams pagination)
        {
            var query = _context.Tracks
                .Include(t => t.Preset)
                .Where(t => t.UserId == userId);

            // Поиск
            if (!string.IsNullOrEmpty(pagination.Search))
            {
                var search = pagination.Search.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(search) ||
                    (t.Artist != null && t.Artist.ToLower().Contains(search)) ||
                    (t.Album != null && t.Album.ToLower().Contains(search)));
            }

            // Сортировка
            query = pagination.SortBy?.ToLower() switch
            {
                "title" => pagination.SortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
                "artist" => pagination.SortDescending ? query.OrderByDescending(t => t.Artist) : query.OrderBy(t => t.Artist),
                "duration" => pagination.SortDescending ? query.OrderByDescending(t => t.Duration) : query.OrderBy(t => t.Duration),
                "playcount" => pagination.SortDescending ? query.OrderByDescending(t => t.PlayCount) : query.OrderBy(t => t.PlayCount),
                "lastplayed" => pagination.SortDescending ? query.OrderByDescending(t => t.LastPlayedAt) : query.OrderBy(t => t.LastPlayedAt),
                _ => query.OrderByDescending(t => t.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var tracks = await query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            // Получаем список избранных треков
            var trackIds = tracks.Select(t => t.Id).ToList();
            var favoriteTrackIds = await _context.FavoriteTracks
                .Where(f => f.UserId == userId && trackIds.Contains(f.TrackId))
                .Select(f => f.TrackId)
                .ToListAsync();

            return new TrackListResponse
            {
                Tracks = tracks.Select(t => MapToDto(t, favoriteTrackIds.Contains(t.Id))).ToList(),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }

        public async Task<TrackDto> CreateAsync(Guid userId, CreateTrackRequest request, string filePath)
        {
            var track = new Track
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title,
                Artist = request.Artist,
                Album = request.Album,
                Genre = request.Genre,
                Duration = request.Duration,
                OriginalFilePath = filePath,
                FileType = request.FileType,
                FileSize = request.FileSize,
                CoverImageUrl = request.CoverImageUrl,
                Is8DEnabled = request.Is8DEnabled,
                Status = TrackStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tracks.Add(track);
            await _context.SaveChangesAsync();

            return MapToDto(track, false);
        }

        public async Task<TrackDto?> UpdateAsync(Guid trackId, Guid userId, UpdateTrackRequest request)
        {
            var track = await _context.Tracks
                .Include(t => t.Preset)
                .FirstOrDefaultAsync(t => t.Id == trackId && t.UserId == userId);

            if (track == null)
                return null;

            // Обновляем только те поля, которые переданы
            if (!string.IsNullOrEmpty(request.Title))
                track.Title = request.Title;

            if (request.Artist != null)
                track.Artist = request.Artist;

            if (request.Album != null)
                track.Album = request.Album;

            if (request.Genre != null)
                track.Genre = request.Genre;

            if (request.CoverImageUrl != null)
                track.CoverImageUrl = request.CoverImageUrl;

            if (request.EqualizerSettings != null)
                track.EqualizerSettings = JsonSerializer.Serialize(request.EqualizerSettings);

            if (request.PresetId.HasValue)
                track.PresetId = request.PresetId.Value;

            if (request.Is8DEnabled.HasValue)
                track.Is8DEnabled = request.Is8DEnabled.Value;

            track.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var isFavorite = await _context.FavoriteTracks
                .AnyAsync(f => f.UserId == userId && f.TrackId == trackId);

            return MapToDto(track, isFavorite);
        }

        public async Task<bool> DeleteAsync(Guid trackId, Guid userId)
        {
            var track = await _context.Tracks
                .FirstOrDefaultAsync(t => t.Id == trackId && t.UserId == userId);

            if (track == null)
                return false;

            _context.Tracks.Remove(track);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IncrementPlayCountAsync(Guid trackId)
        {
            var track = await _context.Tracks.FindAsync(trackId);
            if (track == null)
                return false;

            track.PlayCount++;
            track.LastPlayedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AddToFavoritesAsync(Guid userId, Guid trackId)
        {
            // Проверяем, существует ли трек
            var trackExists = await _context.Tracks.AnyAsync(t => t.Id == trackId);
            if (!trackExists)
                return false;

            // Проверяем, не добавлен ли уже в избранное
            var exists = await _context.FavoriteTracks
                .AnyAsync(f => f.UserId == userId && f.TrackId == trackId);

            if (exists)
                return true; // Уже в избранном

            var favorite = new FavoriteTrack
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TrackId = trackId,
                AddedAt = DateTime.UtcNow
            };

            _context.FavoriteTracks.Add(favorite);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveFromFavoritesAsync(Guid userId, Guid trackId)
        {
            var favorite = await _context.FavoriteTracks
                .FirstOrDefaultAsync(f => f.UserId == userId && f.TrackId == trackId);

            if (favorite == null)
                return false;

            _context.FavoriteTracks.Remove(favorite);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<TrackListResponse> GetFavoriteTracksAsync(Guid userId, PaginationParams pagination)
        {
            var query = _context.FavoriteTracks
                .Include(f => f.Track)
                .ThenInclude(t => t!.Preset)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.AddedAt);

            var totalCount = await query.CountAsync();

            var favorites = await query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new TrackListResponse
            {
                Tracks = favorites
                    .Where(f => f.Track != null)
                    .Select(f => MapToDto(f.Track!, true))
                    .ToList(),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }

        public async Task<TrackListResponse> SearchTracksAsync(Guid userId, string query, PaginationParams pagination)
        {
            var searchQuery = query.ToLower();

            var tracksQuery = _context.Tracks
                .Include(t => t.Preset)
                .Where(t => t.UserId == userId &&
                    (t.Title.ToLower().Contains(searchQuery) ||
                     (t.Artist != null && t.Artist.ToLower().Contains(searchQuery)) ||
                     (t.Album != null && t.Album.ToLower().Contains(searchQuery)) ||
                     (t.Genre != null && t.Genre.ToLower().Contains(searchQuery))))
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await tracksQuery.CountAsync();

            var tracks = await tracksQuery
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            // Получаем список избранных треков
            var trackIds = tracks.Select(t => t.Id).ToList();
            var favoriteTrackIds = await _context.FavoriteTracks
                .Where(f => f.UserId == userId && trackIds.Contains(f.TrackId))
                .Select(f => f.TrackId)
                .ToListAsync();

            return new TrackListResponse
            {
                Tracks = tracks.Select(t => MapToDto(t, favoriteTrackIds.Contains(t.Id))).ToList(),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }

        private static TrackDto MapToDto(Track track, bool isFavorite)
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
                IsFavorite = isFavorite
            };
        }
    }
}
