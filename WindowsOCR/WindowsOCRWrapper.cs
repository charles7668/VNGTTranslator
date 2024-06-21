using Windows.Globalization;
using Windows.Media.Ocr;

namespace WindowsOCR
{
    public class WindowsOCRWrapper
    {
        public WindowsOCRWrapper()
        {
            InitOCREngine();
        }

        private OcrEngine _ocrEngine;
        private Language _ocrLanguage = new("en");

        public Language OcrLanguage
        {
            get => _ocrLanguage;
            set
            {
                _ocrLanguage = value;
                InitOCREngine();
            }
        }

        private void InitOCREngine()
        {
            _ocrEngine = OcrEngine.TryCreateFromLanguage(_ocrLanguage);
            if (_ocrEngine == null)
            {
                throw new Exception("Failed to initialize OCR engine");
            }
        }
    }
}