using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SonicWave8D.API.Services;
using SonicWave8D.Shared.DTOs;
using System.Security.Claims;

namespace SonicWave8D.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PresetsController : ControllerBase
    {
        private readonly IPresetService _presetService;

        public PresetsController(IPresetService presetService)
        {
            _presetService = presetService;
        }

        /// <summary>
        /// Получить пресет по ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PresetDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PresetDto>> GetById(Guid id)
        {
            var preset = await _presetService.GetByIdAsync(id);

            if (preset == null)
                return NotFound(new { message = "Пресет не найден" });

            return Ok(preset);
        }

        /// <summary>
        /// Получить пресеты текущего пользователя
        /// </summary>
        [HttpGet("my")]
        [ProducesResponseType(typeof(PresetListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PresetListResponse>> GetUserPresets()
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _presetService.GetUserPresetsAsync(userId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Получить системные пресеты
        /// </summary>
        [HttpGet("system")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PresetListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PresetListResponse>> GetSystemPresets()
        {
            var result = await _presetService.GetSystemPresetsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Получить публичные пресеты
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PresetListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PresetListResponse>> GetPublicPresets([FromQuery] PaginationParams pagination)
        {
            var result = await _presetService.GetPublicPresetsAsync(pagination);
            return Ok(result);
        }

        /// <summary>
        /// Получить все доступные пресеты (системные + свои + публичные)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PresetListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PresetListResponse>> GetAllAvailable()
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            // Получаем системные пресеты
            var systemPresets = await _presetService.GetSystemPresetsAsync();

            // Получаем пресеты пользователя
            var userPresets = await _presetService.GetUserPresetsAsync(userId.Value);

            // Объединяем
            var allPresets = systemPresets.Presets
                .Concat(userPresets.Presets)
                .ToList();

            return Ok(new PresetListResponse
            {
                Presets = allPresets,
                TotalCount = allPresets.Count
            });
        }

        /// <summary>
        /// Создать новый пресет
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(PresetDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PresetDto>> Create([FromBody] CreatePresetRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var preset = await _presetService.CreateAsync(userId.Value, request);
                return CreatedAtAction(nameof(GetById), new { id = preset.Id }, preset);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Обновить пресет
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(PresetDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PresetDto>> Update(Guid id, [FromBody] UpdatePresetRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var preset = await _presetService.UpdateAsync(id, userId.Value, request);

                if (preset == null)
                    return NotFound(new { message = "Пресет не найден или у вас нет прав на его редактирование" });

                return Ok(preset);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Удалить пресет
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _presetService.DeleteAsync(id, userId.Value);

            if (!result)
                return NotFound(new { message = "Пресет не найден или у вас нет прав на его удаление" });

            return NoContent();
        }

        /// <summary>
        /// Увеличить счётчик использований пресета
        /// </summary>
        [HttpPost("{id:guid}/use")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> IncrementUsageCount(Guid id)
        {
            var result = await _presetService.IncrementUsageCountAsync(id);

            if (!result)
                return NotFound(new { message = "Пресет не найден" });

            return Ok(new { message = "Счётчик использований обновлён" });
        }

        /// <summary>
        /// Добавить/убрать пресет из избранного
        /// </summary>
        [HttpPost("{id:guid}/favorite")]
        [ProducesResponseType(typeof(PresetDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PresetDto>> ToggleFavorite(Guid id)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            // Получаем текущий пресет
            var currentPreset = await _presetService.GetByIdAsync(id);
            if (currentPreset == null)
                return NotFound(new { message = "Пресет не найден" });

            // Переключаем избранное
            var updateRequest = new UpdatePresetRequest
            {
                IsFavorite = !currentPreset.IsFavorite
            };

            var preset = await _presetService.UpdateAsync(id, userId.Value, updateRequest);

            if (preset == null)
                return NotFound(new { message = "Пресет не найден или у вас нет прав на его редактирование" });

            return Ok(preset);
        }

        /// <summary>
        /// Дублировать пресет (создать копию)
        /// </summary>
        [HttpPost("{id:guid}/duplicate")]
        [ProducesResponseType(typeof(PresetDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PresetDto>> Duplicate(Guid id, [FromQuery] string? name = null)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var preset = await _presetService.DuplicatePresetAsync(id, userId.Value, name);

            if (preset == null)
                return NotFound(new { message = "Пресет не найден или у вас нет доступа к нему" });

            return CreatedAtAction(nameof(GetById), new { id = preset.Id }, preset);
        }

        /// <summary>
        /// Поиск пресетов
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PresetListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PresetListResponse>> Search([FromQuery] string q, [FromQuery] PaginationParams pagination)
        {
            var userId = GetUserIdFromClaims();

            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Поисковый запрос не может быть пустым" });

            var result = await _presetService.SearchPresetsAsync(q, userId, pagination);
            return Ok(result);
        }

        private Guid? GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}
