// IndexedDB Storage Module for SonicWave 8D
// Optimized version with Blob storage instead of Base64

const DB_NAME = "SonicWaveDB";
const DB_VERSION = 2; // Увеличиваем версию для новой схемы
const USERS_STORE = "users";
const TRACKS_STORE = "tracks";
const FILES_STORE = "files"; // Новое хранилище для Blob файлов

let dbInstance = null;

// Open and initialize the database
function openDB() {
  return new Promise((resolve, reject) => {
    if (dbInstance) {
      resolve(dbInstance);
      return;
    }

    const request = indexedDB.open(DB_NAME, DB_VERSION);

    request.onupgradeneeded = (event) => {
      const db = event.target.result;
      console.log("[DB] Upgrading database to version", DB_VERSION);

      // Create Users Store
      if (!db.objectStoreNames.contains(USERS_STORE)) {
        const userStore = db.createObjectStore(USERS_STORE, {
          keyPath: "email",
        });
        userStore.createIndex("id", "id", { unique: true });
      }

      // Create Tracks Store (metadata only)
      if (!db.objectStoreNames.contains(TRACKS_STORE)) {
        const trackStore = db.createObjectStore(TRACKS_STORE, {
          keyPath: "id",
        });
        trackStore.createIndex("userId", "userId", { unique: false });
      }

      // Create Files Store (Blob storage)
      if (!db.objectStoreNames.contains(FILES_STORE)) {
        const filesStore = db.createObjectStore(FILES_STORE, {
          keyPath: "trackId",
        });
        console.log("[DB] Created files store for Blob storage");
      }
    };

    request.onsuccess = () => {
      dbInstance = request.result;
      console.log("[DB] Database opened successfully");
      resolve(dbInstance);
    };

    request.onerror = () => {
      console.error("[DB] Failed to open database:", request.error);
      reject(request.error);
    };
  });
}

// Initialize DB (called from C#)
export async function initializeDB() {
  try {
    await openDB();
    console.log("[DB] IndexedDB initialized successfully");
  } catch (error) {
    console.error("[DB] Failed to initialize IndexedDB:", error);
    throw error;
  }
}

// User Registration
export async function registerUser(email, password) {
  const db = await openDB();

  return new Promise((resolve, reject) => {
    const tx = db.transaction(USERS_STORE, "readwrite");
    const store = tx.objectStore(USERS_STORE);

    const checkReq = store.get(email);

    checkReq.onsuccess = () => {
      if (checkReq.result) {
        reject(new Error("User already exists"));
      } else {
        const newUser = {
          id: generateId(),
          email: email,
          password: password,
          name: email.split("@")[0],
        };

        const addReq = store.add(newUser);

        addReq.onsuccess = () => {
          const userResult = {
            id: newUser.id,
            email: newUser.email,
            name: newUser.name,
            password: newUser.password,
          };
          console.log("[DB] User registered:", newUser.email);
          resolve(JSON.stringify(userResult));
        };

        addReq.onerror = () => reject(addReq.error);
      }
    };

    checkReq.onerror = () => reject(checkReq.error);
  });
}

// User Login
export async function loginUser(email, password) {
  const db = await openDB();

  return new Promise((resolve, reject) => {
    const tx = db.transaction(USERS_STORE, "readonly");
    const store = tx.objectStore(USERS_STORE);
    const req = store.get(email);

    req.onsuccess = () => {
      const user = req.result;
      if (user && user.password === password) {
        const userResult = {
          id: user.id,
          email: user.email,
          name: user.name,
          password: user.password,
        };
        console.log("[DB] User logged in:", user.email);
        resolve(JSON.stringify(userResult));
      } else {
        reject(new Error("Invalid credentials"));
      }
    };

    req.onerror = () => reject(req.error);
  });
}

// Convert data URL to Blob
function dataUrlToBlob(dataUrl) {
  const parts = dataUrl.split(",");
  const contentType = parts[0].match(/:(.*?);/)[1];
  const raw = atob(parts[1]);
  const rawLength = raw.length;
  const uInt8Array = new Uint8Array(rawLength);

  for (let i = 0; i < rawLength; ++i) {
    uInt8Array[i] = raw.charCodeAt(i);
  }

  return new Blob([uInt8Array], { type: contentType });
}

// Convert Blob to data URL
function blobToDataUrl(blob) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => resolve(reader.result);
    reader.onerror = reject;
    reader.readAsDataURL(blob);
  });
}

// Save Track (with Blob optimization)
export async function saveTrack(trackJson) {
  const db = await openDB();
  const track = JSON.parse(trackJson);

  console.log("[DB SAVE] Saving track:", track.id);

  // Validate
  if (!track.id) {
    throw new Error("Track ID is missing!");
  }
  if (!track.userId) {
    throw new Error("Track UserId is missing!");
  }

  try {
    // 1. Конвертируем Base64 в Blob для хранения
    let originalBlob = null;
    let processedBlob = null;

    if (track.fileDataUrl && track.fileDataUrl.startsWith("data:")) {
      console.log(
        "[DB SAVE] Converting original file Base64 to Blob (size reduction ~33%)",
      );
      originalBlob = dataUrlToBlob(track.fileDataUrl);
      console.log(
        `[DB SAVE] Original Blob size: ${(originalBlob.size / 1024 / 1024).toFixed(2)} MB`,
      );
    }

    if (track.processedDataUrl && track.processedDataUrl.startsWith("data:")) {
      console.log("[DB SAVE] Converting processed file to Blob");
      processedBlob = dataUrlToBlob(track.processedDataUrl);
    }

    // 2. Сохраняем Blob файлы отдельно
    const filesTx = db.transaction(FILES_STORE, "readwrite");
    const filesStore = filesTx.objectStore(FILES_STORE);

    const fileData = {
      trackId: track.id,
      originalBlob: originalBlob,
      processedBlob: processedBlob,
      savedAt: new Date().toISOString(),
    };

    await new Promise((resolve, reject) => {
      const fileReq = filesStore.put(fileData);
      fileReq.onsuccess = () => {
        console.log("[DB SAVE] Files saved as Blobs");
        resolve();
      };
      fileReq.onerror = () => reject(fileReq.error);
    });

    // 3. Сохраняем метаданные трека (БЕЗ Base64)
    const trackMetadata = {
      id: track.id,
      userId: track.userId,
      originalName: track.originalName,
      uploadDate: track.uploadDate,
      status: track.status,
      duration: track.duration,
      selectedPresetId: track.selectedPresetId,
      currentGains: track.currentGains,
      is8dEnabled: track.is8dEnabled,
      fileType: track.fileType,
      fileSize: track.fileSize,
    };

    const tracksTx = db.transaction(TRACKS_STORE, "readwrite");
    const tracksStore = tracksTx.objectStore(TRACKS_STORE);

    await new Promise((resolve, reject) => {
      const trackReq = tracksStore.put(trackMetadata);
      trackReq.onsuccess = () => {
        console.log("[DB SAVE] Track metadata saved successfully");
        resolve();
      };
      trackReq.onerror = () => reject(trackReq.error);
    });

    console.log("[DB SAVE] ✅ Track saved completely (Blob optimized)");
  } catch (error) {
    console.error("[DB SAVE ERROR]", error);
    throw error;
  }
}

// Get User's Tracks (with Blob loading)
export async function getUserTracks(userId) {
  const db = await openDB();
  console.log("[DB LOAD] Loading tracks for user:", userId);

  return new Promise(async (resolve, reject) => {
    try {
      // 1. Загружаем метаданные треков
      const tracksTx = db.transaction(TRACKS_STORE, "readonly");
      const tracksStore = tracksTx.objectStore(TRACKS_STORE);
      const index = tracksStore.index("userId");
      const tracksReq = index.getAll(userId);

      tracksReq.onsuccess = async () => {
        const tracks = tracksReq.result || [];
        console.log(`[DB LOAD] Found ${tracks.length} tracks (metadata only)`);

        // 2. Для каждого трека загружаем Blob файлы
        const filesTx = db.transaction(FILES_STORE, "readonly");
        const filesStore = filesTx.objectStore(FILES_STORE);

        const tracksWithFiles = await Promise.all(
          tracks.map(async (track) => {
            try {
              // Загружаем файлы для этого трека
              const fileReq = filesStore.get(track.id);

              const fileData = await new Promise((res, rej) => {
                fileReq.onsuccess = () => res(fileReq.result);
                fileReq.onerror = () => rej(fileReq.error);
              });

              if (fileData) {
                // Конвертируем Blob обратно в Data URL для использования в приложении
                if (fileData.originalBlob) {
                  track.fileDataUrl = await blobToDataUrl(
                    fileData.originalBlob,
                  );
                }
                if (fileData.processedBlob) {
                  track.processedDataUrl = await blobToDataUrl(
                    fileData.processedBlob,
                  );
                }
                console.log(`[DB LOAD] Loaded files for track: ${track.id}`);
              } else {
                console.warn(`[DB LOAD] No files found for track: ${track.id}`);
                track.fileDataUrl = "";
                track.processedDataUrl = null;
              }

              return track;
            } catch (error) {
              console.error(
                `[DB LOAD] Error loading files for track ${track.id}:`,
                error,
              );
              track.fileDataUrl = "";
              track.processedDataUrl = null;
              return track;
            }
          }),
        );

        console.log("[DB LOAD] ✅ All tracks loaded with files");
        resolve(JSON.stringify(tracksWithFiles));
      };

      tracksReq.onerror = () => {
        console.error("[DB LOAD ERROR]", tracksReq.error);
        reject(tracksReq.error);
      };
    } catch (error) {
      console.error("[DB LOAD ERROR]", error);
      reject(error);
    }
  });
}

// Get Track by ID
export async function getTrackById(trackId) {
  const db = await openDB();

  return new Promise(async (resolve, reject) => {
    try {
      // Загружаем метаданные
      const tracksTx = db.transaction(TRACKS_STORE, "readonly");
      const tracksStore = tracksTx.objectStore(TRACKS_STORE);
      const trackReq = tracksStore.get(trackId);

      trackReq.onsuccess = async () => {
        const track = trackReq.result;

        if (!track) {
          resolve(null);
          return;
        }

        // Загружаем файлы
        const filesTx = db.transaction(FILES_STORE, "readonly");
        const filesStore = filesTx.objectStore(FILES_STORE);
        const fileReq = filesStore.get(trackId);

        fileReq.onsuccess = async () => {
          const fileData = fileReq.result;

          if (fileData) {
            if (fileData.originalBlob) {
              track.fileDataUrl = await blobToDataUrl(fileData.originalBlob);
            }
            if (fileData.processedBlob) {
              track.processedDataUrl = await blobToDataUrl(
                fileData.processedBlob,
              );
            }
          }

          resolve(JSON.stringify(track));
        };

        fileReq.onerror = () => reject(fileReq.error);
      };

      trackReq.onerror = () => reject(trackReq.error);
    } catch (error) {
      console.error("[DB] Error getting track:", error);
      reject(error);
    }
  });
}

// Delete Track
export async function deleteTrack(trackId) {
  const db = await openDB();

  return new Promise((resolve, reject) => {
    try {
      // Удаляем метаданные
      const tracksTx = db.transaction(TRACKS_STORE, "readwrite");
      const tracksStore = tracksTx.objectStore(TRACKS_STORE);
      tracksStore.delete(trackId);

      tracksTx.oncomplete = () => {
        // Удаляем файлы
        const filesTx = db.transaction(FILES_STORE, "readwrite");
        const filesStore = filesTx.objectStore(FILES_STORE);
        filesStore.delete(trackId);

        filesTx.oncomplete = () => {
          console.log("[DB] Track and files deleted:", trackId);
          resolve();
        };
        filesTx.onerror = () => reject(filesTx.error);
      };

      tracksTx.onerror = () => reject(tracksTx.error);
    } catch (error) {
      console.error("[DB] Error deleting track:", error);
      reject(error);
    }
  });
}

// Helper function to generate unique IDs
function generateId() {
  return (
    Math.random().toString(36).substring(2, 15) +
    Math.random().toString(36).substring(2, 15)
  );
}

// Clear entire database (for troubleshooting)
export async function clearDatabase() {
  try {
    console.log("[DB] Clearing entire database...");

    if (dbInstance) {
      dbInstance.close();
      dbInstance = null;
    }

    return new Promise((resolve, reject) => {
      const deleteRequest = indexedDB.deleteDatabase(DB_NAME);

      deleteRequest.onsuccess = () => {
        console.log("[DB] ✅ Database cleared successfully");
        resolve(true);
      };

      deleteRequest.onerror = () => {
        console.error("[DB] ❌ Error clearing database:", deleteRequest.error);
        reject(deleteRequest.error);
      };

      deleteRequest.onblocked = () => {
        console.warn("[DB] ⚠️ Database deletion blocked - close all tabs");
        reject(new Error("Database deletion blocked"));
      };
    });
  } catch (error) {
    console.error("[DB] Error clearing database:", error);
    throw error;
  }
}
