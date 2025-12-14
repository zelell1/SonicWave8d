// File Upload with Drag & Drop support for Blazor

export function initializeFileUpload(dropZoneElement, inputElement, dotnetHelper) {
  if (!dropZoneElement || !inputElement) {
    console.error("[FileUpload] Missing elements");
    return;
  }

  console.log("[FileUpload] Initializing drag & drop");

  // Prevent default drag behaviors on the entire document
  ["dragenter", "dragover", "dragleave", "drop"].forEach((eventName) => {
    document.body.addEventListener(eventName, preventDefaults, false);
  });

  function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
  }

  // Handle drop on the drop zone
  dropZoneElement.addEventListener(
    "drop",
    async (e) => {
      console.log("[FileUpload] Drop event triggered");
      const files = e.dataTransfer.files;

      if (files.length > 0) {
        const file = files[0];
        console.log(
          `[FileUpload] File dropped: ${file.name}, Type: ${file.type}, Size: ${file.size}`,
        );

        // Validate file type
        if (!file.type.startsWith("audio/")) {
          await dotnetHelper.invokeMethodAsync("OnDropError", {
            message: "Please upload an audio file (MP3, WAV, OGG, FLAC)",
          });
          return;
        }

        // Validate file size (200MB)
        const maxSize = 200 * 1024 * 1024;
        if (file.size > maxSize) {
          await dotnetHelper.invokeMethodAsync("OnDropError", {
            message: `File too large. Maximum size is 200MB (file is ${Math.round(file.size / 1024 / 1024)}MB)`,
          });
          return;
        }

        // Read file as data URL
        try {
          const dataUrl = await readFileAsDataUrl(file);
          console.log(
            `[FileUpload] File read successfully, data URL length: ${dataUrl.length}`,
          );

          // Send to Blazor
          await dotnetHelper.invokeMethodAsync("OnFileDropped", {
            dataUrl: dataUrl,
            fileName: file.name,
            fileType: file.type,
            fileSize: file.size,
          });
        } catch (error) {
          console.error("[FileUpload] Error reading file:", error);
          await dotnetHelper.invokeMethodAsync("OnDropError", {
            message: `Error reading file: ${error.message}`,
          });
        }
      }
    },
    false,
  );

  // Trigger the hidden input when clicking the drop zone
  dropZoneElement.addEventListener("click", (e) => {
    // Only trigger if not clicking on the input itself
    if (e.target !== inputElement) {
      inputElement.click();
    }
  });

  console.log("[FileUpload] Initialization complete");
}

function readFileAsDataUrl(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = (e) => {
      resolve(e.target.result);
    };

    reader.onerror = (e) => {
      reject(new Error("Failed to read file"));
    };

    reader.readAsDataURL(file);
  });
}

export function disposeFileUpload(dropZoneElement) {
  if (dropZoneElement) {
    console.log("[FileUpload] Disposing");
    // Remove event listeners if needed
  }
}
