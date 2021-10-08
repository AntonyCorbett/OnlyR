using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OnlyR.VolumeMeter
{
    /// <summary>
    /// Volume meter custom control using bitmaps on a DrawingVisual.
    /// See also Themes\Generic.xaml
    /// </summary>
    public class VduControl : Control
    {
        /// <summary>
        /// VolumeLevel DP (value range 0 - 100)
        /// </summary>
        public static readonly DependencyProperty VolumeLevelProperty =
            DependencyProperty.Register(
                "VolumeLevel", typeof(int), typeof(VduControl), new PropertyMetadata(0, OnVolumeChanged));

        // change number of levels to add or remove display "blocks"
        private readonly int _levelsCount = 14;
        private readonly DrawingVisual _drawingVisual;

        private Image? _image;
        private Border? _innerBorder;
        private SolidColorBrush _backBrush;
        private SolidColorBrush _lightGreenBrush;
        private SolidColorBrush _yellowBrush;
        private SolidColorBrush _redBrush;

        private List<RenderTargetBitmap?> _bitmaps;

        static VduControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(VduControl), new FrameworkPropertyMetadata(typeof(VduControl)));
        }

        public VduControl()
        {
            Debug.Assert(_levelsCount >= 7, "_levelsCount >= 7");

            InitBitmaps();
            InitBrushes();

            _drawingVisual = new DrawingVisual();
        }

        /// <summary>
        /// Wraps the VolumeLevel DP (value range 0 - 100)
        /// </summary>
        public int VolumeLevel
        {
            // wrapper (no additional code in here!)

            // ReSharper disable once PossibleNullReferenceException
            get => (int)GetValue(VolumeLevelProperty);
            set => SetValue(VolumeLevelProperty, value);
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

        private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VduControl c)
            {
                c.OnVolumeChanged();
            }
        }

        private void OnVolumeChanged()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_image == null)
            {
                return;
            }

            var numBlocksLit = VolumeLevel * _levelsCount / 100;
            _image.Source = _bitmaps[numBlocksLit] ?? CreateBitmap(numBlocksLit);
        }

        private RenderTargetBitmap? CreateBitmap(int numBlocksLit)
        {
            if (_innerBorder == null)
            {
                return null;
            }

            var bmpHeight = (int)_innerBorder.ActualHeight;
            var overallBlockHeight = bmpHeight / _levelsCount;

            bmpHeight = overallBlockHeight * _levelsCount;  // normalise

            var ySpaceBetweenBlocks = Math.Max(overallBlockHeight / 3, 1);
            var blockHeight = overallBlockHeight - ySpaceBetweenBlocks;

            var bmpWidth = (int)_innerBorder.ActualWidth;
            var blockWidth = bmpWidth;

            _bitmaps[numBlocksLit] = new RenderTargetBitmap(bmpWidth, bmpHeight, 96, 96, PixelFormats.Pbgra32);

            var numRedBlocks = _levelsCount / 7;
            var numYellowBlocks = _levelsCount / 4;

            using (DrawingContext dc = _drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(_backBrush, null, new Rect(0, 0, blockWidth, blockHeight));

                for (int n = 0; n < numBlocksLit; ++n)
                {
                    SolidColorBrush b;
                    if (n >= _levelsCount - numRedBlocks)
                    {
                        b = _redBrush;
                    }
                    else if (n >= _levelsCount - numRedBlocks - numYellowBlocks)
                    {
                        b = _yellowBrush;
                    }
                    else
                    {
                        b = _lightGreenBrush;
                    }

                    dc.DrawRectangle(
                        b, 
                        null, 
                        new Rect(
                        0,
                        bmpHeight - ((n + 1) * (blockHeight + ySpaceBetweenBlocks)),
                        blockWidth,
                        blockHeight));
                }
            }

            _bitmaps[numBlocksLit]?.Render(_drawingVisual);
            return _bitmaps[numBlocksLit];
        }

        [MemberNotNull(nameof(_backBrush), nameof(_lightGreenBrush), nameof(_yellowBrush), nameof(_redBrush))]
        private void InitBrushes()
        {
            _backBrush = new SolidColorBrush { Color = Colors.Black };
            _lightGreenBrush = new SolidColorBrush { Color = Colors.GreenYellow };
            _yellowBrush = new SolidColorBrush { Color = Colors.Yellow };
            _redBrush = new SolidColorBrush { Color = Colors.Red };
        }

        [MemberNotNull(nameof(_bitmaps))]
        private void InitBitmaps()
        {
            _bitmaps = new List<RenderTargetBitmap?>();

            for (int n = 0; n < _levelsCount + 1; ++n)
            {
                _bitmaps.Add(null);
            }
        }
    }
}
