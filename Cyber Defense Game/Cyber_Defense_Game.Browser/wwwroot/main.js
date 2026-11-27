import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

let currentAudio = null;

function playTrack(src) {
    stopHostMusic();
    currentAudio = new Audio(src);
    currentAudio.loop = true;
    currentAudio.play().catch(err => console.warn('Audio play blocked:', err));
}

export function playHostMusic() {
    playTrack('./audio/easy.mp3');
}

export function playDifficultyMusic(difficulty) {
    const tracks = {
        'Easy': './audio/easy.mp3',
        'Normal': './audio/normal.mp3',
        'Hard': './audio/hard.mp3',
        'IMPOSSIBLE': './audio/imposible.mp3'
    };

    const src = tracks[difficulty] || tracks['Normal'];
    playTrack(src);
}

export function stopHostMusic() {
    if (currentAudio) {
        currentAudio.pause();
        currentAudio.currentTime = 0;
        currentAudio = null;
    }
}


globalThis.getLobbyCodeFromUrl = function () {
    const params = new URLSearchParams(window.location.search);
    const code = params.get("code");
    return code ? code.replace(/\D/g, "").slice(0, 6) : "";
};

globalThis.scanLobbyQrCode = async function () {
    if (!("BarcodeDetector" in window)) {
        throw new Error("In-app QR scanner not supported in this browser; scan with your phone camera instead");
    }

    const detector = new BarcodeDetector({ formats: ["qr_code"] });
    const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: "environment" }
    });

    return new Promise((resolve) => {
        const overlay = document.createElement("div");
        overlay.style.cssText = "position:fixed;inset:0;background:rgba(0,0,0,0.85);z-index:99999;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:16px;";

        const video = document.createElement("video");
        video.style.cssText = "width:300px;height:300px;object-fit:cover;border:3px solid #00E5FF;border-radius:12px;";
        video.srcObject = stream;
        video.setAttribute("playsinline", "true");
        video.play();

        const cancelBtn = document.createElement("button");
        cancelBtn.textContent = "Cancel";
        cancelBtn.style.cssText = "padding:10px 24px;font-size:16px;border-radius:8px;cursor:pointer;";

        overlay.appendChild(video);
        overlay.appendChild(cancelBtn);
        document.body.appendChild(overlay);

        let active = true;
        const cleanup = (result) => {
            active = false;
            stream.getTracks().forEach(t => t.stop());
            overlay.remove();
            resolve(result);
        };

        cancelBtn.onclick = () => cleanup("");

        const scanLoop = async () => {
            if (!active) return;
            try {
                const codes = await detector.detect(video);
                if (codes.length > 0 && codes[0].rawValue) {
                    cleanup(codes[0].rawValue);
                    return;
                }
            } catch (_) { }
            requestAnimationFrame(scanLoop);
        };
        requestAnimationFrame(scanLoop);
    });
};


const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
