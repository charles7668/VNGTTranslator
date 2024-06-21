using System.Drawing;
using VNGTTranslator.Enums;

namespace VNGTTranslator.Models
{
    public class OCRSetting
    {
        public bool IsUseScreen { get; set; }

        public Rectangle? OCRArea { get; set; }

        public IntPtr WinHandle { get; set; }

        public string OCRLang { get; set; } = "en";

        public ImagePreProcessFunction PreProcessFunc { get; set; } = ImagePreProcessFunction.NONE;
    }
}