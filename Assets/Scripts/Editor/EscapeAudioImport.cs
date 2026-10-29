using UnityEditor;
using UnityEngine;

/// Import settings for Assets/Resources/Audio (SOUND-PLAN.md section 6, iOS):
/// music streams as Vorbis; short P0 foley and Kenney UI clips are ADPCM, mono, decompressed on load and preloaded.
public class EscapeAudioImport : AssetPostprocessor
{
    const string Root = "Assets/Resources/Audio/";
    const float MusicQuality = 0.65f;
    const uint SampleRate = 44100;

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Root)) return;
        var imp = (AudioImporter)assetImporter;
        bool music = assetPath.StartsWith(Root + "Music/");
        var s = imp.defaultSampleSettings;
        s.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        s.sampleRateOverride = SampleRate;
        if (music)
        {
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = MusicQuality;
            s.preloadAudioData = false;
            imp.forceToMono = false;
            imp.loadInBackground = true;
        }
        else
        {
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.ADPCM;
            s.preloadAudioData = true;
            imp.forceToMono = true;
            imp.loadInBackground = false;
        }
        imp.defaultSampleSettings = s;
    }
}
