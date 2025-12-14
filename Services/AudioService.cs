using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using SonicWave8D.Models;

namespace SonicWave8D.Services
{
    public class AudioService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

        public AudioService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/audioProcessor.js").AsTask());
        }

        /// <summary>
        /// Process audio file with EQ and optional 8D spatial effects
        /// </summary>
        public async Task<ProcessAudioResult> Process8DAudioAsync(string fileDataUrl, List<double> gains, bool enable8D)
        {
            try
            {
                var module = await _moduleTask.Value;

                var result = await module.InvokeAsync<ProcessAudioResult>(
                    "process8DAudio",
                    fileDataUrl,
                    gains.ToArray(),
                    enable8D
                );

                return result;
            }
            catch (JSException ex)
            {
                Console.WriteLine($"Audio processing error: {ex.Message}");
                return new ProcessAudioResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return new ProcessAudioResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during audio processing"
                };
            }
        }

        /// <summary>
        /// Get audio file duration
        /// </summary>
        public async Task<double> GetAudioDurationAsync(string fileDataUrl)
        {
            try
            {
                var module = await _moduleTask.Value;
                return await module.InvokeAsync<double>("getAudioDuration", fileDataUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting audio duration: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Convert file to base64 data URL
        /// </summary>
        public async Task<string> ConvertFileToDataUrlAsync(string inputElementId)
        {
            try
            {
                var module = await _moduleTask.Value;
                return await module.InvokeAsync<string>("convertFileToDataUrl", inputElementId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting file: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Read file from input element
        /// </summary>
        public async Task<(string dataUrl, string fileName, string fileType, long fileSize)> ReadAudioFileAsync(IJSObjectReference fileInputRef)
        {
            try
            {
                var module = await _moduleTask.Value;
                var result = await module.InvokeAsync<FileReadResult>("readAudioFile", fileInputRef);

                return (result.DataUrl, result.FileName, result.FileType, result.FileSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                return (string.Empty, string.Empty, string.Empty, 0);
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

        private class FileReadResult
        {
            public string DataUrl { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string FileType { get; set; } = string.Empty;
            public long FileSize { get; set; }
        }
    }
}
