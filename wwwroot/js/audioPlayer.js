// Audio Player Helper for Blazor
// Provides audio control functions via JSInterop

// Progress interval storage keyed by element id (stable across renders)
const progressIntervalsById = new Map();

export function playAudio(audioElement) {
  if (audioElement) {
    console.log(
      "Playing audio, src:",
      audioElement.src,
      "readyState:",
      audioElement.readyState,
    );
    const playPromise = audioElement.play();
    if (playPromise !== undefined) {
      return playPromise.catch((error) => {
        console.error("Play error:", error);
        throw error;
      });
    }
    return Promise.resolve();
  }
  return Promise.reject("Audio element not found");
}

export function pauseAudio(audioElement) {
  if (audioElement) {
    audioElement.pause();
  }
}

export function setCurrentTime(audioElement, time) {
  if (audioElement) {
    audioElement.currentTime = time;
  }
}

export function getCurrentTime(audioElement) {
  if (audioElement) {
    return audioElement.currentTime;
  }
  return 0;
}

export function getDuration(audioElement) {
  if (audioElement) {
    return audioElement.duration || 0;
  }
  return 0;
}

export function getAudioState(audioElement) {
  if (audioElement) {
    const state = {
      currentTime: audioElement.currentTime || 0,
      duration: audioElement.duration || 0,
      paused: audioElement.paused,
      ended: audioElement.ended,
    };
    // console.log('Audio state:', state); // Debug logging
    return state;
  }
  return {
    currentTime: 0,
    duration: 0,
    paused: true,
    ended: false,
  };
}

export function setVolume(audioElement, volume) {
  if (audioElement) {
    audioElement.volume = Math.max(0, Math.min(1, volume));
  }
}

export function getVolume(audioElement) {
  if (audioElement) {
    return audioElement.volume;
  }
  return 1;
}

// Setup event listeners for audio element
export function setupAudioEvents(audioElement, dotNetHelper, componentId) {
  if (!audioElement || !dotNetHelper) {
    console.error("Audio element or dotNetHelper not provided");
    return;
  }

  console.log("Setting up audio events for component:", componentId);

  // Time update event
  audioElement.addEventListener("timeupdate", () => {
    dotNetHelper.invokeMethodAsync("OnTimeUpdateJS", componentId);
  });

  // Ended event
  audioElement.addEventListener("ended", () => {
    dotNetHelper.invokeMethodAsync("OnAudioEndedJS", componentId);
  });

  // Can play event
  audioElement.addEventListener("canplay", () => {
    console.log("Audio can play");
    dotNetHelper.invokeMethodAsync("OnCanPlayJS", componentId);
  });

  // Error event
  audioElement.addEventListener("error", (e) => {
    console.error("Audio error:", e, audioElement.error);
    const errorMsg = audioElement.error
      ? `Code: ${audioElement.error.code}, Message: ${audioElement.error.message}`
      : "Unknown error";
    dotNetHelper.invokeMethodAsync("OnAudioErrorJS", componentId, errorMsg);
  });

  // Loaded metadata event
  audioElement.addEventListener("loadedmetadata", () => {
    console.log("Metadata loaded, duration:", audioElement.duration);
    dotNetHelper.invokeMethodAsync("OnLoadedMetadataJS", componentId);
  });

  // Play event
  audioElement.addEventListener("play", () => {
    console.log("Audio playing");
  });

  // Pause event
  audioElement.addEventListener("pause", () => {
    console.log("Audio paused");
  });
}

// Get element bounding rectangle
export function getBoundingRect(element) {
  if (element) {
    const rect = element.getBoundingClientRect();
    return {
      left: rect.left,
      top: rect.top,
      right: rect.right,
      bottom: rect.bottom,
      width: rect.width,
      height: rect.height,
      x: rect.x,
      y: rect.y,
    };
  }
  return {
    left: 0,
    top: 0,
    right: 0,
    bottom: 0,
    width: 0,
    height: 0,
    x: 0,
    y: 0,
  };
}

// Click input element (for file upload)
export function clickElement(element) {
  if (element) {
    element.click();
  }
}

// Check if audio element is ready
export function isAudioReady(audioElement) {
  if (audioElement) {
    return audioElement.readyState >= 2; // HAVE_CURRENT_DATA
  }
  return false;
}

// Load audio source
export function loadAudioSource(audioElement, src) {
  if (audioElement && src) {
    console.log("Loading audio source:", src.substring(0, 50) + "...");
    audioElement.src = src;
    audioElement.load();
  }
}

// Get audio ready state
export function getReadyState(audioElement) {
  if (audioElement) {
    return audioElement.readyState;
  }
  return 0;
}

// Progress tracking with setInterval for reliable updates in Blazor WASM
export function startProgressInterval(audioElement, dotNetHelper) {
  if (!audioElement || !dotNetHelper) {
    console.error(
      "startProgressInterval: Missing audioElement or dotNetHelper",
    );
    return;
  }

  const key = audioElement.id || null;
  if (!key) {
    console.error(
      "startProgressInterval: audioElement.id is missing. Set a stable id on the <audio> element.",
    );
    return;
  }

  // Stop any existing interval for this element id
  stopProgressIntervalById(key);

  console.log("Starting progress interval tracking for:", key);

  let tickCount = 0;

  const intervalId = setInterval(() => {
    // If element got detached/replaced, stop interval to avoid leaking timers
    const el = document.getElementById(key);
    if (!el) {
      console.warn(
        "Progress interval: element no longer exists, stopping:",
        key,
      );
      stopProgressIntervalById(key);
      return;
    }

    if (!el.paused && !el.ended) {
      const currentTime = el.currentTime || 0;
      const duration = el.duration || 0;

      // Log first few ticks to verify it runs
      if (tickCount < 5) {
        console.log(
          `[${key}] tick #${tickCount} ct=${currentTime.toFixed(3)} dur=${duration.toFixed(3)} paused=${el.paused}`,
        );
        tickCount++;
      }

      if (duration > 0) {
        dotNetHelper
          .invokeMethodAsync("UpdateProgressFromJS", currentTime, duration)
          .catch((error) => {
            console.error("Error invoking UpdateProgressFromJS:", error);
            stopProgressIntervalById(key);
          });
      }
    }
  }, 50); // Update every 50ms

  progressIntervalsById.set(key, intervalId);
  console.log("Progress interval started for:", key, "ID:", intervalId);
}

export function stopProgressInterval(audioElement) {
  if (!audioElement) return;
  const key = audioElement.id || null;
  if (!key) return;
  stopProgressIntervalById(key);
}

function stopProgressIntervalById(key) {
  const intervalId = progressIntervalsById.get(key);
  if (intervalId) {
    console.log("Stopping progress interval for:", key, "ID:", intervalId);
    clearInterval(intervalId);
    progressIntervalsById.delete(key);
  }
}

// Progress tracking with requestAnimationFrame for smooth updates
// (kept for reference / future use; current implementation uses setInterval by element id)
let progressTrackingMap = new Map();

export function startProgressTracking(audioElement, dotNetHelper) {
  if (!audioElement || !dotNetHelper) {
    console.error("Audio element or dotNetHelper not provided");
    return;
  }

  // Stop any existing tracking for this element
  stopProgressTracking(audioElement);

  console.log("Starting progress tracking");

  let animationFrameId = null;

  const updateProgress = () => {
    if (audioElement && !audioElement.paused && !audioElement.ended) {
      const currentTime = audioElement.currentTime || 0;
      const duration = audioElement.duration || 0;

      // Call .NET method to update UI
      try {
        dotNetHelper.invokeMethodAsync("UpdateProgress", currentTime, duration);
      } catch (error) {
        console.error("Error calling UpdateProgress:", error);
        stopProgressTracking(audioElement);
        return;
      }

      // Schedule next update
      animationFrameId = requestAnimationFrame(updateProgress);
    } else {
      // Audio stopped, clean up
      stopProgressTracking(audioElement);
    }
  };

  // Start the animation loop
  animationFrameId = requestAnimationFrame(updateProgress);

  // Store the animation frame ID so we can cancel it later
  progressTrackingMap.set(audioElement, animationFrameId);
}

export function stopProgressTracking(audioElement) {
  if (!audioElement) return;

  const animationFrameId = progressTrackingMap.get(audioElement);
  if (animationFrameId) {
    console.log("Stopping progress tracking");
    cancelAnimationFrame(animationFrameId);
    progressTrackingMap.delete(audioElement);
  }
}
