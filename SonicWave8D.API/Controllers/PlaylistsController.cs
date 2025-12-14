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
    public class PlaylistsController : ControllerBase
    {
        private readonly IPlaylistService _playlistService;

        public PlaylistsController(IPlaylistService playlistService)
        {
            _playlistService = playlistService;
        }

        /// <summary>
        /// Получить плейлист по ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PlaylistDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlaylistDto>> GetById(Guid id)
        {
            var userId = GetUserIdFromClaims();
            var playlist = await _playlistService.GetByIdAsync(id, userId);

            if (playlist == null)
                return NotFound(new { message = "Плейлист не найден или у вас нет доступа к нему" });

            return Ok(playlist);
        }

        /// <summary>
        /// Получить плейлист с треками
        /// </summary>
        [HttpGet("{id:guid}/tracks")]
        [ProducesResponseType(typeof(PlaylistDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlaylistDto>> GetWithTracks(Guid id)
        {
            var userId = GetUserIdFromClaims();
            var playlist = await _playlistService.GetPlaylistWithTracksAsync(id, userId);

            if (playlist == null)
                return NotFound(new { message = "Плейлист не найден или у вас нет доступа к нему" });

            return Ok(playlist);
        }

        /// <summary>
        /// Получить плейлисты текущего пользователя
        /// </summary>
        [HttpGet("my")]
        [ProducesResponseType(typeof(PlaylistListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PlaylistListResponse>> GetUserPlaylists()
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _playlistService.GetUserPlaylistsAsync(userId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Получить публичные плейлисты
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PlaylistListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PlaylistListResponse>> GetPublicPlaylists([FromQuery] PaginationParams pagination)
        {
            var result = await _playlistService.GetPublicPlaylistsAsync(pagination);
            return Ok(result);
        }

        /// <summary>
        /// Создать новый плейлист
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(PlaylistDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PlaylistDto>> Create([FromBody] CreatePlaylistRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var playlist = await _playlistService.CreateAsync(userId.Value, request);
            return CreatedAtAction(nameof(GetById), new { id = playlist.Id }, playlist);
        }

        /// <summary>
        /// Обновить плейлист
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(PlaylistDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlaylistDto>> Update(Guid id, [FromBody] UpdatePlaylistRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var playlist = await _playlistService.UpdateAsync(id, userId.Value, request);

            if (playlist == null)
                return NotFound(new { message = "Плейлист не найден или у вас нет прав на его редактирование" });

            return Ok(playlist);
        }

        /// <summary>
        /// Удалить плейлист
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _playlistService.DeleteAsync(id, userId.Value);

            if (!result)
                return NotFound(new { message = "Плейлист не найден или у вас нет прав на его удаление" });

            return NoContent();
        }

        /// <summary>
        /// Добавить трек в плейлист
        /// </summary>
        [HttpPost("{id:guid}/tracks")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> AddTrack(Guid id, [FromBody] AddTrackToPlaylistRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _playlistService.AddTrackAsync(id, userId.Value, request);

            if (!result)
                return NotFound(new { message = "Плейлист или трек не найден" });

            return Ok(new { message = "Трек добавлен в плейлист" });
        }

        /// <summary>
        /// Удалить трек из плейлиста
        /// </summary>
        [HttpDelete("{id:guid}/tracks/{trackId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RemoveTrack(Guid id, Guid trackId)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _playlistService.RemoveTrackAsync(id, userId.Value, trackId);

            if (!result)
                return NotFound(new { message = "Плейлист или трек не найден" });

            return Ok(new { message = "Трек удалён из плейлиста" });
        }

        /// <summary>
        /// Изменить порядок треков в плейлисте
        /// </summary>
        [HttpPut("{id:guid}/tracks/reorder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ReorderTracks(Guid id, [FromBody] ReorderPlaylistRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _playlistService.ReorderTracksAsync(id, userId.Value, request);

            if (!result)
                return NotFound(new { message = "Плейлист не найден или не все треки принадлежат этому плейлисту" });

            return Ok(new { message = "Порядок треков обновлён" });
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
