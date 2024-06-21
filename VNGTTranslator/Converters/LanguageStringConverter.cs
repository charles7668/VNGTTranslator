using System.Globalization;
using System.Windows.Data;

namespace VNGTTranslator.Converters
{
    internal class LanguageStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string> languageList)
            {
                var temp = new List<string>();
                foreach (string lang in languageList)
                {
                    temp.Add(MappingLanguage(lang));
                }

                return temp;
            }

            if (value is string language)
            {
                return MappingLanguage(language);
            }

            return new List<string>();

            string MappingLanguage(string input)
            {
                switch (input)
                {
                    case "en":
                        return "English";
                    case "ja":
                        return "Japanese";
                    case "zh-tw":
                        return "Chinese (Simplified)";
                    case "zh-cn":
                        return "Chinese (Traditional)";
                    default:
                        return "Unknown";
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string> langList)
            {
                var temp = new List<string>();
                foreach (string lang in langList)
                {
                    temp.Add(MappingLanguage(lang));
                }

                return temp;
            }

            if (value is string language)
            {
                return MappingLanguage(language);
            }

            return new List<string>();

            string MappingLanguage(string input)
            {
                switch (input)
                {
                    case "English":
                        return "en";
                    case "Japanese":
                        return "ja";
                    case "Chinese (Simplified)":
                        return "zh-tw";
                    case "Chinese (Traditional)":
                        return "zh-cn";
                    default:
                        return "Unknown";
                }
            }
        }
    }
}