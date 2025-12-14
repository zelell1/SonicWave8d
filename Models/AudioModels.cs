using System;
using System.Collections.Generic;

namespace SonicWave8D.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // In production, this should be hashed
    }

    public class AudioTrack
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public string FileDataUrl { get; set; } = string.Empty; // Base64 or Blob URL
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public TrackStatus Status { get; set; } = TrackStatus.Pending;
        public string? ProcessedDataUrl { get; set; }
        public double Duration { get; set; }
        public string SelectedPresetId { get; set; } = "flat";
        public List<double> CurrentGains { get; set; } = new List<double> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public bool Is8dEnabled { get; set; } = true;
        public string FileType { get; set; } = "audio/mpeg";
        public long FileSize { get; set; }
    }

    public enum TrackStatus
    {
        Pending,
        Processing,
        Completed,
        Error
    }

    public class EqualizerPreset
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<double> Gains { get; set; } = new List<double>();

        public EqualizerPreset(string id, string name, params double[] gains)
        {
            Id = id;
            Name = name;
            Gains = new List<double>(gains);
        }
    }

    public static class AudioConstants
    {
        // Consumer-Friendly 10-band equalizer frequencies
        public static readonly int[] EQ_FREQUENCIES = { 60, 170, 310, 600, 1000, 3000, 6000, 12000, 14000, 16000 };

        public static readonly List<EqualizerPreset> EQ_PRESETS = new()
        {
            new EqualizerPreset("flat", "Flat (Default)", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            new EqualizerPreset("bass_boost", "Bass Boost", 12, 9, 6, 3, 0, 0, 0, 0, 0, 0),
            new EqualizerPreset("bass_cut", "Bass Cut", -12, -9, -6, -3, 0, 0, 0, 0, 0, 0),
            new EqualizerPreset("treble_boost", "Treble Boost", 0, 0, 0, 0, 0, 3, 6, 9, 12, 12),
            new EqualizerPreset("electronic", "Electronic", 8, 7, 3, 0, -3, 0, 2, 6, 8, 8),
            new EqualizerPreset("vocal", "Vocal Booster", -3, -3, -3, 2, 6, 6, 4, 2, 0, -2),
            new EqualizerPreset("pop", "Pop", 4, 3, 0, -3, -3, -1, 2, 4, 5, 5),
            new EqualizerPreset("rock", "Rock", 7, 5, 2, 0, -2, 0, 3, 6, 8, 8),
            new EqualizerPreset("jazz", "Jazz", 4, 3, 1, 3, -3, -3, 0, 2, 4, 5),
            new EqualizerPreset("custom", "Custom", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
        };

        public const double ROTATION_SPEED_SECONDS = 10.0;
    }

    public class ProcessAudioRequest
    {
        public string FileDataUrl { get; set; } = string.Empty;
        public List<double> Gains { get; set; } = new();
        public bool Enable8D { get; set; } = true;
    }

    public class ProcessAudioResult
    {
        public bool Success { get; set; }
        public string? ProcessedDataUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public double Duration { get; set; }
    }
}
