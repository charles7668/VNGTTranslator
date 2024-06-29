using System.Globalization;
using System.Windows.Data;
using VNGTTranslator.Enums;

namespace VNGTTranslator.Converters
{
    /// <summary>
    /// convert <see cref="Language" /> enum to string
    /// </summary>
    internal class LanguageEnumToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                IEnumerable<Language> languageList => languageList.Select(MappingLanguage).ToList(),
                Language language => MappingLanguage(language),
                _ => ""
            };

            static string MappingLanguage(Language input)
            {
                return input switch
                {
                    Language.ENGLISH => "English",
                    Language.CHINESE_SIMPLIFIED => "Chinese Simplified",
                    Language.CHINESE_TRADITIONAL => "Chinese Traditional",
                    _ => "Unknown"
                };
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                IEnumerable<string> langList => langList.Select(MappingLanguage).ToList(),
                string language => MappingLanguage(language),
                _ => new List<Language>()
            };

            static Language MappingLanguage(string input)
            {
                return input switch
                {
                    "English" => Language.ENGLISH,
                    "Chinese Simplified" => Language.CHINESE_SIMPLIFIED,
                    "Chinese Traditional" => Language.CHINESE_TRADITIONAL,
                    _ => Language.ENGLISH
                };
            }
        }
    }
}