using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace VNGTTranslator
{
    /// <summary>
    /// ScreenCaptureWindow.xaml 的互動邏輯
    /// </summary>
    public partial class ScreenCaptureWindow : INotifyPropertyChanged
    {
        public ScreenCaptureWindow(BitmapImage image)
        {
            InitializeComponent();
            _image = image;
            _scale = GetScale();
            DataContext = this;
            _inkDrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Red,
                Width = 2,
                Height = 2,
                StylusTip = StylusTip.Rectangle,
                IsHighlighter = false,
                IgnorePressure = true
            };
        }

        private readonly double _scale;
        private BitmapImage _image;

        private string _info =
            "Hold down the left mouse button and drag the mouse to draw the area to be identified. Right-click to exit when finished.\n";

        private DrawingAttributes _inkDrawingAttributes;
        private StrokeCollection _inkStrokes = new();

        private Rect _selectRect;

        private Point _startPoint;

        public Rectangle OCRArea;

        public string Info
        {
            get => _info;
            set => SetField(ref _info, value);
        }

        public BitmapImage Image
        {
            get => _image;
            set => SetField(ref _image, value);
        }

        public DrawingAttributes InkDrawingAttributes
        {
            get => _inkDrawingAttributes;
            set => SetField(ref _inkDrawingAttributes, value);
        }

        public StrokeCollection InkStrokes
        {
            get => _inkStrokes;
            set => SetField(ref _inkStrokes, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private double GetScale()
        {
            if (_scale == 0)
                return Graphics.FromHwnd(new WindowInteropHelper(this).Handle).DpiX / 96;
            return _scale;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            OnPropertyChanged(propertyName);
        }

        private void InkCanvasMeasure_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = e.GetPosition(InkCanvasMeasure);
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                e.Handled = true;
                Capture();
                Dispatcher.Invoke(async () =>
                {
                    await Task.Delay(100);
                    Close();
                });
            }
        }

        private void Capture()
        {
            OCRArea = new Rectangle((int)_selectRect.X, (int)_selectRect.Y, (int)_selectRect.Width,
                (int)_selectRect.Height);
        }

        private void InkCanvasMeasure_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            // Draw square
            Point endP = e.GetPosition(InkCanvasMeasure);
            Point[] pointList =
            {
                new(_startPoint.X, _startPoint.Y), new(_startPoint.X, endP.Y), new(endP.X, endP.Y),
                new(endP.X, _startPoint.Y), new(_startPoint.X, _startPoint.Y)
            };
            var point = new StylusPointCollection(pointList);
            var stroke = new Stroke(point)
            {
                DrawingAttributes = InkCanvasMeasure.DefaultDrawingAttributes.Clone()
            };
            InkStrokes.Clear();
            InkStrokes.Add(stroke);

            _selectRect = new Rect(new Point(_startPoint.X * _scale, _startPoint.Y * _scale),
                new Point(endP.X * _scale, endP.Y * _scale));
        }
    }
}