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

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
