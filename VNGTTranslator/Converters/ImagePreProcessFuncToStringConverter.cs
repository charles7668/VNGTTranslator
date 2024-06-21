using System.Globalization;
using System.Windows.Data;
using VNGTTranslator.Enums;

namespace VNGTTranslator.Converters
{
    internal class ImagePreProcessFuncToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IEnumerable<ImagePreProcessFunction> imagePreProcessFunctions)
            {
                var temp = new List<string>();
                foreach (ImagePreProcessFunction func in imagePreProcessFunctions)
                {
                    temp.Add(MappingPreProcessFunc(func));
                }

                return temp;
            }

            if (value is ImagePreProcessFunction imagePreProcessFunction)
            {
                return MappingPreProcessFunc(imagePreProcessFunction);
            }

            string MappingPreProcessFunc(ImagePreProcessFunction input)
            {
                switch (input)
                {
                    case ImagePreProcessFunction.OTSU_ALGORITHM:
                        return "Otsu Algorithm";
                    default:
                        return "None";
                }
            }

            return new List<ImagePreProcessFunction>();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IEnumerable<string> imagePreProcessFunctions)
            {
                var temp = new List<ImagePreProcessFunction>();
                foreach (string func in imagePreProcessFunctions)
                {
                    temp.Add(MappingPreProcessFunc(func));
                }

                return temp;
            }

            if (value is string imagePreProcessFunction)
            {
                return MappingPreProcessFunc(imagePreProcessFunction);
            }

            ImagePreProcessFunction MappingPreProcessFunc(string input)
            {
                switch (input)
                {
                    case "Otsu Algorithm":
                        return ImagePreProcessFunction.OTSU_ALGORITHM;
                    default:
                        return ImagePreProcessFunction.NONE;
                }
            }

            return new List<ImagePreProcessFunction>();
        }
    }
}