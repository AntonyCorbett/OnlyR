using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OnlyR.VolumeMeter
{
    public class VduControl : Control
    {
        private readonly int _levelsCount = 14;

        private Image _image;
        private Border _innerBorder;
        private SolidColorBrush _backBrush;
        private SolidColorBrush _lightGreenBrush;
        private SolidColorBrush _yellowBrush;
        private SolidColorBrush _redBrush;

        private List<RenderTargetBitmap> _bitmaps;
        private readonly DrawingVisual _drawingVisual;


        static VduControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VduControl),
                new FrameworkPropertyMetadata(typeof(VduControl)));
        }

        public VduControl()
        {
            InitBitmaps();
            InitBrushes();

            _drawingVisual = new DrawingVisual();
        }

        private void InitBrushes()
        {
            _backBrush = new SolidColorBrush { Color = Colors.Black };
            _lightGreenBrush = new SolidColorBrush { Color = Colors.GreenYellow };
            _yellowBrush = new SolidColorBrush { Color = Colors.Yellow };
            _redBrush = new SolidColorBrush { Color = Colors.Red };
        }

        private void InitBitmaps()
        {
            _bitmaps = new List<RenderTargetBitmap>();

            for (int n = 0; n < _levelsCount + 1; ++n)
            {
                _bitmaps.Add(null);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("VolumeImage") is Image image)
            {
                _image = image;
            }

            if (GetTemplateChild("InnerBorder") is Border border)
            {
                _innerBorder = border;
            }
        }

        public static readonly DependencyProperty VolumeLevelProperty =
            DependencyProperty.Register("VolumeLevel", typeof(int), typeof(VduControl), 
                new PropertyMetadata(0, OnVolumeChanged));

        private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is VduControl c)
            {
                c.OnVolumeChanged((int)e.OldValue, (int)e.NewValue);
            }
        }

        private void OnVolumeChanged(int oldValue, int newValue)
        {
            Refresh();
        }

        public int VolumeLevel
        {
            // wrapper (no additional code in here!)
            get => (int) GetValue(VolumeLevelProperty);
            set => SetValue(VolumeLevelProperty, value);
        }

        private void Refresh()
        {
            int numBlocksLit = (VolumeLevel * _levelsCount) / 100;
            RenderTargetBitmap bmp = _bitmaps[numBlocksLit] ?? CreateBitmap(numBlocksLit);
            _image.Source = bmp;
        }

        private RenderTargetBitmap CreateBitmap(int numBlocksLit)
        {
            var w = _innerBorder.RenderSize.Height;
            int bmpHeight = (int)(_innerBorder.ActualHeight);
            int ySpaceBetweenBlocks = Math.Max(bmpHeight / 40, 1);

            int overallBlockHeight = bmpHeight / _levelsCount;
            int blockHeight = overallBlockHeight - ySpaceBetweenBlocks;

            // recalc...
            //bmpHeight = (overallBlockHeight * _levelsCount) - ySpaceBetweenBlocks;

            int bmpWidth = (int) (_innerBorder.ActualWidth);
            int blockWidth = bmpWidth;
            
            _bitmaps[numBlocksLit] = new RenderTargetBitmap(bmpWidth, bmpHeight, 96, 96, PixelFormats.Pbgra32);

            using (DrawingContext dc = _drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(_backBrush, null, new Rect(0, 0, blockWidth, blockHeight));
                
                for (int n = 0; n < numBlocksLit; ++n)
                {
                    SolidColorBrush b;
                    if (n > _levelsCount - 3)
                    {
                        b = _redBrush;
                    }
                    else if (n > _levelsCount - 6)
                    {
                        b = _yellowBrush;
                    }
                    else
                    {
                        b = _lightGreenBrush;
                    }
                    
                    dc.DrawRectangle(b, null, new Rect(
                        0, 
                        bmpHeight - ((n+1) * (blockHeight + ySpaceBetweenBlocks)), 
                        blockWidth, 
                        blockHeight));
                }
            }

            _bitmaps[numBlocksLit].Render(_drawingVisual);
            return _bitmaps[numBlocksLit];
        }

    }
}
