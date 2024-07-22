using HandyControl.Controls;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using VNGTTranslator.Enums;
using VNGTTranslator.Helper;
using VNGTTranslator.Models;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Language = Windows.Globalization.Language;
using Window = System.Windows.Window;

namespace VNGTTranslator.OCRProviders
{
    [Export(typeof(IOCRProvider))]
    public class WindowOCRProvider : IOCRProvider
    {
        public WindowOCRProvider()
        {
            try
            {
                _ocrEngine = InitOCREngine(_ocrLanguage);
            }
            catch (Exception)
            {
                _ocrEngine = null;
            }
        }

        private OcrEngine? _ocrEngine;
        private Language _ocrLanguage = new("en");
        public string ProviderName => "WindowOCR";
        public bool SupportSetting { get; } = false;

        public async Task<Result<string>> RecognizeTextAsync(Bitmap originalImage,
            ImagePreProcessFunction preProcessFunc)
        {
            if (_ocrEngine == null)
            {
                return Result<string>.Fail("OCR engine is not initialized");
            }

            Bitmap image = originalImage;
            switch (preProcessFunc)
            {
                case ImagePreProcessFunction.OTSU_ALGORITHM:
                    image = ImageHelper.OTSUAlgorithm(originalImage);
                    break;
            }

            try
            {
                using var stream = new InMemoryRandomAccessStream();
                image.Save(stream.AsStream(), ImageFormat.Bmp);
                BitmapDecoder? decoder = await BitmapDecoder.CreateAsync(stream);
                SoftwareBitmap? bitmap = await decoder.GetSoftwareBitmapAsync();
                OcrResult? res = await _ocrEngine.RecognizeAsync(bitmap);
                return Result<string>.Success(res.Text);
            }
            catch (Exception ex)
            {
                return Result<string>.Fail(ex.Message);
            }
        }

        public Result SetOcrLanguage(string lang)
        {
            _ocrLanguage = new Language(lang);
            try
            {
                _ocrEngine = InitOCREngine(_ocrLanguage);
            }
            catch (Exception e)
            {
                _ocrEngine = null;
                return Result.Fail(e.Message);
            }

            return Result.Success();
        }

        public Task<PopupWindow> GetSettingWindowAsync(Window parent)
        {
            throw new NotSupportedException();
        }

        private static OcrEngine InitOCREngine(Language lang)
        {
            var engine = OcrEngine.TryCreateFromLanguage(lang);
            if (engine == null)
            {
                throw new Exception("Failed to initialize OCR engine");
            }

            return engine;
        }
    }
}