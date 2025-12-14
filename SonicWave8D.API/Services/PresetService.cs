using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SonicWave8D.API.Data;
using SonicWave8D.Shared.DTOs;
using SonicWave8D.Shared.Models;

namespace SonicWave8D.API.Services
{
    public interface IPresetService
    {
        Task<PresetDto?> GetByIdAsync(Guid presetId);
        Task<PresetListResponse> GetUserPresetsAsync(Guid userId);
        Task<PresetListResponse> GetSystemPresetsAsync();
        Task<PresetListResponse> GetPublicPresetsAsync(PaginationParams pagination);
        Task<PresetDto> CreateAsync(Guid userId, CreatePresetRequest request);
        Task<PresetDto?> UpdateAsync(Guid presetId, Guid userId, UpdatePresetRequest request);
        Task<bool> DeleteAsync(Guid presetId, Guid userId);
        Task<bool> IncrementUsageCountAsync(Guid presetId);
        Task<PresetListResponse> SearchPresetsAsync(string query, Guid? userId, PaginationParams pagination);
        Task<PresetDto?> DuplicatePresetAsync(Guid presetId, Guid userId, string? newName = null);
    }

    public class PresetService : IPresetService
    {
        private readonly AppDbContext _context;

        public PresetService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PresetDto?> GetByIdAsync(Guid presetId)
        {
            var preset = await _context.CustomPresets
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == presetId);

            return preset != null ? MapToDto(preset) : null;
        }

        public async Task<PresetListResponse> GetUserPresetsAsync(Guid userId)
        {
            var presets = await _context.CustomPresets
                .Include(p => p.User)
                .Where(p => p.UserId == userId && !p.IsSystem)
                .OrderByDescending(p => p.IsFavorite)
                .ThenByDescending(p => p.UsageCount)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return new PresetListResponse
            {
                Presets = presets.Select(MapToDto).ToList(),
                TotalCount = presets.Count
            };
        }

        public async Task<PresetListResponse> GetSystemPresetsAsync()
        {
            var presets = await _context.CustomPresets
                .Include(p => p.User)
                .Where(p => p.IsSystem)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return new PresetListResponse
            {
                Presets = presets.Select(MapToDto).ToList(),
                TotalCount = presets.Count
            };
        }

        public async Task<PresetListResponse> GetPublicPresetsAsync(PaginationParams pagination)
        {
            var query = _context.CustomPresets
                .Include(p => p.User)
                .Where(p => p.IsPublic && !p.IsSystem)
                .OrderByDescending(p => p.UsageCount)
                .ThenBy(p => p.Name);

            var totalCount = await query.CountAsync();

            var presets = await query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PresetListResponse
            {
                Presets = presets.Select(MapToDto).ToList(),
                TotalCount = totalCount
            };
        }

        public async Task<PresetDto> CreateAsync(Guid userId, CreatePresetRequest request)
        {
            // Валидация gains - должно быть ровно 10 значений от -12 до +12
            if (request.Gains.Count != 10)
            {
                throw new ArgumentException("Gains must contain exactly 10 values");
            }

            foreach (var gain in request.Gains)
            {
                if (gain < -12 || gain > 12)
                {
                    throw new ArgumentException("Each gain value must be between -12 and +12 dB");
                }
            }

            var preset = new CustomPreset
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name,
                Description = request.Description,
                Gains = JsonSerializer.Serialize(request.Gains),
                IsPublic = request.IsPublic,
                IsSystem = false,
                IsFavorite = false,
                UsageCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CustomPresets.Add(preset);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные для DTO
            await _context.Entry(preset).Reference(p => p.User).LoadAsync();

            return MapToDto(preset);
        }

        public async Task<PresetDto?> UpdateAsync(Guid presetId, Guid userId, UpdatePresetRequest request)
        {
            var preset = await _context.CustomPresets
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == presetId && p.UserId == userId && !p.IsSystem);

            if (preset == null)
                return null;

            // Обновляем только переданные поля
            if (!string.IsNullOrEmpty(request.Name))
                preset.Name = request.Name;

            if (request.Description != null)
                preset.Description = request.Description;

            if (request.Gains != null)
            {
                // Валидация gains
                if (request.Gains.Count != 10)
                {
                    throw new ArgumentException("Gains must contain exactly 10 values");
                }

                foreach (var gain in request.Gains)
                {
                    if (gain < -12 || gain > 12)
                    {
                        throw new ArgumentException("Each gain value must be between -12 and +12 dB");
                    }
                }

                preset.Gains = JsonSerializer.Serialize(request.Gains);
            }

            if (request.IsPublic.HasValue)
                preset.IsPublic = request.IsPublic.Value;

            if (request.IsFavorite.HasValue)
                preset.IsFavorite = request.IsFavorite.Value;

            preset.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(preset);
        }

        public async Task<bool> DeleteAsync(Guid presetId, Guid userId)
        {
            var preset = await _context.CustomPresets
                .FirstOrDefaultAsync(p => p.Id == presetId && p.UserId == userId && !p.IsSystem);

            if (preset == null)
                return false;

            // Убираем пресет у всех треков, которые его используют
            var tracksWithPreset = await _context.Tracks
                .Where(t => t.PresetId == presetId)
                .ToListAsync();

            foreach (var track in tracksWithPreset)
            {
                track.PresetId = null;
            }

            _context.CustomPresets.Remove(preset);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IncrementUsageCountAsync(Guid presetId)
        {
            var preset = await _context.CustomPresets.FindAsync(presetId);
            if (preset == null)
                return false;

            preset.UsageCount++;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<PresetListResponse> SearchPresetsAsync(string query, Guid? userId, PaginationParams pagination)
        {
            var searchQuery = query.ToLower();

            var presetsQuery = _context.CustomPresets
                .Include(p => p.User)
                .Where(p =>
                    // Пользователь видит свои пресеты, системные и публичные
                    (userId.HasValue && p.UserId == userId.Value) ||
                    p.IsSystem ||
                    p.IsPublic)
                .Where(p =>
                    p.Name.ToLower().Contains(searchQuery) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchQuery)))
                .OrderByDescending(p => p.IsSystem)
                .ThenByDescending(p => p.UsageCount)
                .ThenBy(p => p.Name);

            var totalCount = await presetsQuery.CountAsync();

            var presets = await presetsQuery
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PresetListResponse
            {
                Presets = presets.Select(MapToDto).ToList(),
                TotalCount = totalCount
            };
        }

        public async Task<PresetDto?> DuplicatePresetAsync(Guid presetId, Guid userId, string? newName = null)
        {
            var originalPreset = await _context.CustomPresets
                .FirstOrDefaultAsync(p => p.Id == presetId);

            if (originalPreset == null)
                return null;

            // Проверяем доступ - можно копировать свои, системные или публичные пресеты
            if (originalPreset.UserId != userId && !originalPreset.IsSystem && !originalPreset.IsPublic)
                return null;

            var duplicatedPreset = new CustomPreset
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = newName ?? $"{originalPreset.Name} (копия)",
                Description = originalPreset.Description,
                Gains = originalPreset.Gains,
                IsPublic = false,
                IsSystem = false,
                IsFavorite = false,
                UsageCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CustomPresets.Add(duplicatedPreset);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные для DTO
            await _context.Entry(duplicatedPreset).Reference(p => p.User).LoadAsync();

            return MapToDto(duplicatedPreset);
        }

        private static PresetDto MapToDto(CustomPreset preset)
        {
            List<double> gains = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            if (!string.IsNullOrEmpty(preset.Gains))
            {
                try
                {
                    gains = JsonSerializer.Deserialize<List<double>>(preset.Gains) ?? gains;
                }
                catch
                {
                    // Оставляем значения по умолчанию при ошибке парсинга
                }
            }

            return new PresetDto
            {
                Id = preset.Id,
                UserId = preset.UserId,
                Name = preset.Name,
                Description = preset.Description,
                Gains = gains,
                IsPublic = preset.IsPublic,
                IsSystem = preset.IsSystem,
                IsFavorite = preset.IsFavorite,
                UsageCount = preset.UsageCount,
                CreatedAt = preset.CreatedAt,
                CreatorUsername = preset.User?.Username
            };
        }
    }
}
