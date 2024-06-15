using System.Collections;
using System.Reflection;
using System.Speech.Synthesis;

namespace VNGTTranslator.TTSProviders.Microsoft
{
    // this class to help install OneCore voices to SpeechSynthesizer
    // https://stackoverflow.com/questions/51811901/speechsynthesizer-doesnt-get-all-installed-voices-3
    public static class WindowSpeechHelper
    {
        private const string PROP_VOICE_SYNTHESIZER = "VoiceSynthesizer";
        private const string FIELD_INSTALLED_VOICES = "_installedVoices";

        private const string ONE_CORE_VOICES_REGISTRY = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices";

        private static readonly Type _ObjectTokenCategoryType = typeof(SpeechSynthesizer).Assembly
            .GetType("System.Speech.Internal.ObjectTokens.ObjectTokenCategory")!;

        private static readonly Type _VoiceInfoType = typeof(SpeechSynthesizer).Assembly
            .GetType("System.Speech.Synthesis.VoiceInfo")!;

        private static readonly Type _InstalledVoiceType = typeof(SpeechSynthesizer).Assembly
            .GetType("System.Speech.Synthesis.InstalledVoice")!;


        public static void InjectOneCoreVoices(this SpeechSynthesizer synthesizer)
        {
            object? voiceSynthesizer = GetProperty(synthesizer, PROP_VOICE_SYNTHESIZER);
            if (voiceSynthesizer == null)
                throw new NotSupportedException($"Property not found: {PROP_VOICE_SYNTHESIZER}");

            var installedVoices = GetField(voiceSynthesizer, FIELD_INSTALLED_VOICES) as IList;
            if (installedVoices == null)
                throw new NotSupportedException($"Field not found or null: {FIELD_INSTALLED_VOICES}");

            if (_ObjectTokenCategoryType
                    .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)?
                    .Invoke(null, new object?[] { ONE_CORE_VOICES_REGISTRY }) is not IDisposable otc)
                throw new NotSupportedException($"Failed to call Create on {_ObjectTokenCategoryType} instance");

            using (otc)
            {
                if (_ObjectTokenCategoryType
                        .GetMethod("FindMatchingTokens", BindingFlags.Instance | BindingFlags.NonPublic)?
                        .Invoke(otc, new object?[] { null, null }) is not IList tokens)
                    throw new NotSupportedException("Failed to list matching tokens");

                foreach (object? token in tokens)
                {
                    if (token == null || GetProperty(token, "Attributes") == null) continue;

                    object? voiceInfo =
                        typeof(SpeechSynthesizer).Assembly
                            .CreateInstance(_VoiceInfoType.FullName!, true,
                                BindingFlags.Instance | BindingFlags.NonPublic, null,
                                new[] { token }, null, null);

                    if (voiceInfo == null)
                        throw new NotSupportedException($"Failed to instantiate {_VoiceInfoType}");

                    object? installedVoice =
                        typeof(SpeechSynthesizer).Assembly
                            .CreateInstance(_InstalledVoiceType.FullName!, true,
                                BindingFlags.Instance | BindingFlags.NonPublic, null,
                                new[] { voiceSynthesizer, voiceInfo }, null, null);

                    if (installedVoice == null)
                        throw new NotSupportedException($"Failed to instantiate {_InstalledVoiceType}");

                    installedVoices.Add(installedVoice);
                }
            }
        }

        private static object? GetProperty(object target, string propName)
        {
            return target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(target);
        }

        private static object? GetField(object target, string propName)
        {
            return target.GetType().GetField(propName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(target);
        }
    }
}