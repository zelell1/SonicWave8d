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
    public class TracksController : ControllerBase
    {
        private readonly ITrackService _trackService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public TracksController(
            ITrackService trackService,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _trackService = trackService;
            _environment = environment;
            _configuration = configuration;
        }

        /// <summary>
        /// Получить список треков пользователя
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(TrackListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TrackListResponse>> GetUserTracks([FromQuery] PaginationParams pagination)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _trackService.GetUserTracksAsync(userId.Value, pagination);
            return Ok(result);
        }

        /// <summary>
        /// Получить трек по ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(TrackDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrackDto>> GetById(Guid id)
        {
            var userId = GetUserIdFromClaims();
            var track = await _trackService.GetByIdAsync(id, userId);

            if (track == null)
                return NotFound(new { message = "Трек не найден" });

            return Ok(track);
        }

        /// <summary>
        /// Загрузить новый трек
        /// </summary>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(TrackDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<ActionResult<TrackDto>> Upload([FromForm] IFormFile file, [FromForm] CreateTrackRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл не выбран" });

            // Проверка расширения файла
            var allowedExtensions = _configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
                ?? new[] { ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".aac" };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = $"Недопустимый формат файла. Разрешены: {string.Join(", ", allowedExtensions)}" });

            // Проверка размера файла
            var maxSizeMb = _configuration.GetValue<int>("FileStorage:MaxFileSizeMB", 100);
            if (file.Length > maxSizeMb * 1024 * 1024)
                return BadRequest(new { message = $"Размер файла превышает {maxSizeMb} МБ" });

            // Создаём директорию для загрузок
            var basePath = _configuration["FileStorage:BasePath"] ?? "./uploads";
            var userFolder = Path.Combine(basePath, userId.Value.ToString());

            if (!Directory.Exists(userFolder))
                Directory.CreateDirectory(userFolder);

            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(userFolder, fileName);

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Обновляем request с данными файла
            request.FileSize = file.Length;
            request.FileType = file.ContentType;

            // Если название не указано, используем имя файла
            if (string.IsNullOrEmpty(request.Title))
                request.Title = Path.GetFileNameWithoutExtension(file.FileName);

            // Создаём запись в БД
            var track = await _trackService.CreateAsync(userId.Value, request, filePath);

            return CreatedAtAction(nameof(GetById), new { id = track.Id }, track);
        }

        /// <summary>
        /// Создать трек (без загрузки файла - для внешних источников)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(TrackDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TrackDto>> Create([FromBody] CreateTrackRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var track = await _trackService.CreateAsync(userId.Value, request, "external");

            return CreatedAtAction(nameof(GetById), new { id = track.Id }, track);
        }

        /// <summary>
        /// Обновить трек
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(TrackDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrackDto>> Update(Guid id, [FromBody] UpdateTrackRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var track = await _trackService.UpdateAsync(id, userId.Value, request);

            if (track == null)
                return NotFound(new { message = "Трек не найден или у вас нет прав на его редактирование" });

            return Ok(track);
        }

        /// <summary>
        /// Удалить трек
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _trackService.DeleteAsync(id, userId.Value);

            if (!result)
                return NotFound(new { message = "Трек не найден или у вас нет прав на его удаление" });

            return NoContent();
        }

        /// <summary>
        /// Увеличить счётчик воспроизведений
        /// </summary>
        [HttpPost("{id:guid}/play")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> IncrementPlayCount(Guid id)
        {
            var result = await _trackService.IncrementPlayCountAsync(id);

            if (!result)
                return NotFound(new { message = "Трек не найден" });

            return Ok(new { message = "Счётчик воспроизведений обновлён" });
        }

        /// <summary>
        /// Добавить трек в избранное
        /// </summary>
        [HttpPost("{id:guid}/favorite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> AddToFavorites(Guid id)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _trackService.AddToFavoritesAsync(userId.Value, id);

            if (!result)
                return NotFound(new { message = "Трек не найден" });

            return Ok(new { message = "Трек добавлен в избранное" });
        }

        /// <summary>
        /// Удалить трек из избранного
        /// </summary>
        [HttpDelete("{id:guid}/favorite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RemoveFromFavorites(Guid id)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _trackService.RemoveFromFavoritesAsync(userId.Value, id);

            if (!result)
                return NotFound(new { message = "Трек не найден в избранном" });

            return Ok(new { message = "Трек удалён из избранного" });
        }

        /// <summary>
        /// Получить избранные треки
        /// </summary>
        [HttpGet("favorites")]
        [ProducesResponseType(typeof(TrackListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TrackListResponse>> GetFavorites([FromQuery] PaginationParams pagination)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _trackService.GetFavoriteTracksAsync(userId.Value, pagination);
            return Ok(result);
        }

        /// <summary>
        /// Поиск треков
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(TrackListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<TrackListResponse>> Search([FromQuery] string q, [FromQuery] PaginationParams pagination)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Поисковый запрос не может быть пустым" });

            var result = await _trackService.SearchTracksAsync(userId.Value, q, pagination);
            return Ok(result);
        }

        /// <summary>
        /// Скачать файл трека
        /// </summary>
        [HttpGet("{id:guid}/download")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Download(Guid id)
        {
            var userId = GetUserIdFromClaims();
            var track = await _trackService.GetByIdAsync(id, userId);

            if (track == null)
                return NotFound(new { message = "Трек не найден" });

            // Проверяем, что это трек пользователя
            if (track.UserId != userId)
                return Forbid();

            // Здесь нужно получить путь к файлу из БД
            // Пока возвращаем заглушку
            return NotFound(new { message = "Файл не найден" });
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
