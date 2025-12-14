// Audio Processing Module for SonicWave 8D
// Handles EQ, 8D Spatial Effects, and WAV encoding

const EQ_FREQUENCIES = [60, 170, 310, 600, 1000, 3000, 6000, 12000, 14000, 16000];
const ROTATION_SPEED_SECONDS = 10.0;

/**
 * Process audio with EQ and optional 8D spatial effects
 * @param {string} fileDataUrl - Base64 data URL of the audio file
 * @param {number[]} gains - Array of 10 gain values in dB
 * @param {boolean} enable8D - Whether to apply 8D spatial effect
 * @returns {Promise<object>} Result with processed audio data URL
 */
export async function process8DAudio(fileDataUrl, gains, enable8D) {
    try {
        console.log('Starting audio processing...');

        // 1. Convert data URL to ArrayBuffer
        const arrayBuffer = await dataUrlToArrayBuffer(fileDataUrl);

        // 2. Decode audio
        const tempContext = new AudioContext();
        const audioBuffer = await tempContext.decodeAudioData(arrayBuffer);
        const duration = audioBuffer.duration;
        await tempContext.close();

        console.log(`Audio decoded: ${duration}s, ${audioBuffer.sampleRate}Hz`);

        // 3. Setup offline context for rendering
        const offlineCtx = new OfflineAudioContext(
            2, // Stereo output
            audioBuffer.length,
            audioBuffer.sampleRate
        );

        // 4. Create source
        const source = offlineCtx.createBufferSource();
        source.buffer = audioBuffer;

        // 5. Build EQ chain
        let lastNode = source;

        for (let i = 0; i < EQ_FREQUENCIES.length; i++) {
            const filter = offlineCtx.createBiquadFilter();

            // Use appropriate filter types
            if (i === 0) {
                filter.type = 'lowshelf';
                filter.Q.value = 0.5;
            } else if (i === EQ_FREQUENCIES.length - 1) {
                filter.type = 'highshelf';
                filter.Q.value = 0.5;
            } else {
                filter.type = 'peaking';
                filter.Q.value = 0.5; // Wide, musical EQ
            }

            filter.frequency.setValueAtTime(EQ_FREQUENCIES[i], 0);
            filter.gain.setValueAtTime(gains[i] || 0, 0);

            lastNode.connect(filter);
            lastNode = filter;
        }

        // 6. Create spatial effects (8D)
        const panner = offlineCtx.createStereoPanner();
        const gainNode = offlineCtx.createGain();

        lastNode.connect(panner);
        panner.connect(gainNode);
        gainNode.connect(offlineCtx.destination);

        // 7. Apply 8D automation or keep static
        if (enable8D) {
            const pointsPerSecond = 50;
            const totalPoints = Math.ceil(duration * pointsPerSecond);
            const panCurve = new Float32Array(totalPoints);
            const gainCurve = new Float32Array(totalPoints);

            for (let i = 0; i < totalPoints; i++) {
                const time = i / pointsPerSecond;
                const angle = (time % ROTATION_SPEED_SECONDS) / ROTATION_SPEED_SECONDS * 2 * Math.PI;

                // Pan: -1 (Left) to 1 (Right)
                panCurve[i] = Math.sin(angle);

                // Depth/Volume: slight dip when "behind"
                const depth = 0.9 + 0.1 * Math.cos(angle);
                gainCurve[i] = depth;
            }

            panner.pan.setValueCurveAtTime(panCurve, 0, duration);
            gainNode.gain.setValueCurveAtTime(gainCurve, 0, duration);
        } else {
            // Static center panning
            panner.pan.setValueAtTime(0, 0);
            gainNode.gain.setValueAtTime(1.0, 0);
        }

        // 8. Start processing
        source.start(0);
        console.log('Rendering audio...');
        const renderedBuffer = await offlineCtx.startRendering();

        console.log('Audio rendered, encoding to WAV...');

        // 9. Convert to WAV blob
        const wavBlob = bufferToWav(renderedBuffer);

        // 10. Convert blob to data URL
        const processedDataUrl = await blobToDataUrl(wavBlob);

        console.log('Audio processing complete!');

        return {
            success: true,
            processedDataUrl: processedDataUrl,
            duration: duration,
            errorMessage: null
        };

    } catch (error) {
        console.error('Audio processing error:', error);
        return {
            success: false,
            processedDataUrl: null,
            duration: 0,
            errorMessage: error.message
        };
    }
}

/**
 * Get audio file duration
 * @param {string} fileDataUrl - Base64 data URL of the audio file
 * @returns {Promise<number>} Duration in seconds
 */
export async function getAudioDuration(fileDataUrl) {
    try {
        const arrayBuffer = await dataUrlToArrayBuffer(fileDataUrl);
        const tempContext = new AudioContext();
        const audioBuffer = await tempContext.decodeAudioData(arrayBuffer);
        const duration = audioBuffer.duration;
        await tempContext.close();
        return duration;
    } catch (error) {
        console.error('Error getting audio duration:', error);
        return 0;
    }
}

/**
 * Read audio file from input element
 * @param {HTMLInputElement} fileInput - File input element reference
 * @returns {Promise<object>} File information including data URL
 */
export async function readAudioFile(fileInput) {
    return new Promise((resolve, reject) => {
        const files = fileInput.files;

        if (!files || files.length === 0) {
            reject(new Error('No file selected'));
            return;
        }

        const file = files[0];

        if (!file.type.startsWith('audio/')) {
            reject(new Error('File is not an audio file'));
            return;
        }

        const reader = new FileReader();

        reader.onload = (e) => {
            resolve({
                dataUrl: e.target.result,
                fileName: file.name,
                fileType: file.type,
                fileSize: file.size
            });
        };

        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(file);
    });
}

/**
 * Convert File object to data URL
 * @param {File} file - File object
 * @returns {Promise<string>} Base64 data URL
 */
export async function fileToDataUrl(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (e) => resolve(e.target.result);
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(file);
    });
}

// ============== Helper Functions ==============

/**
 * Convert data URL to ArrayBuffer
 * @param {string} dataUrl - Base64 data URL
 * @returns {Promise<ArrayBuffer>}
 */
async function dataUrlToArrayBuffer(dataUrl) {
    const response = await fetch(dataUrl);
    return await response.arrayBuffer();
}

/**
 * Convert Blob to data URL
 * @param {Blob} blob
 * @returns {Promise<string>}
 */
function blobToDataUrl(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (e) => resolve(e.target.result);
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(blob);
    });
}

/**
 * Encode AudioBuffer to WAV Blob
 * @param {AudioBuffer} buffer
 * @returns {Blob}
 */
function bufferToWav(buffer) {
    const numOfChan = buffer.numberOfChannels;
    const length = buffer.length * numOfChan * 2 + 44;
    const bufferArray = new ArrayBuffer(length);
    const view = new DataView(bufferArray);
    const channels = [];
    let offset = 0;
    let pos = 0;

    // Helper to write string to DataView
    function writeString(view, offset, string) {
        for (let i = 0; i < string.length; i++) {
            view.setUint8(offset + i, string.charCodeAt(i));
        }
    }

    // Write WAV header
    writeString(view, 0, 'RIFF');
    view.setUint32(4, 36 + buffer.length * numOfChan * 2, true);
    writeString(view, 8, 'WAVE');
    writeString(view, 12, 'fmt ');
    view.setUint32(16, 16, true); // Format chunk size
    view.setUint16(20, 1, true); // PCM format
    view.setUint16(22, numOfChan, true);
    view.setUint32(24, buffer.sampleRate, true);
    view.setUint32(28, buffer.sampleRate * 2 * numOfChan, true); // Byte rate
    view.setUint16(32, numOfChan * 2, true); // Block align
    view.setUint16(34, 16, true); // Bits per sample
    writeString(view, 36, 'data');
    view.setUint32(40, buffer.length * numOfChan * 2, true);

    // Get channel data
    for (let i = 0; i < buffer.numberOfChannels; i++) {
        channels.push(buffer.getChannelData(i));
    }

    // Write interleaved audio data
    offset = 44;
    while (pos < buffer.length) {
        for (let i = 0; i < numOfChan; i++) {
            let sample = Math.max(-1, Math.min(1, channels[i][pos])); // Clamp
            sample = (0.5 + sample < 0 ? sample * 32768 : sample * 32767) | 0; // Scale to 16-bit
            view.setInt16(offset, sample, true);
            offset += 2;
        }
        pos++;
    }

    return new Blob([view], { type: 'audio/wav' });
}
