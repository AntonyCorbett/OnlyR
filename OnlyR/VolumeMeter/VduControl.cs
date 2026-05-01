using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Automation.Peers;
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
    [ExcludeFromCodeCoverage]
    public class VduControl : Control
    {
        /// <summary>
        /// VolumeLevel DP (value range 0 - 100)
        /// </summary>
        public static readonly DependencyProperty VolumeLevelProperty =
            DependencyProperty.Register(
                nameof(VolumeLevel), typeof(int), typeof(VduControl), new PropertyMetadata(0, OnVolumeChanged, CoerceVolumeLevel));

#pragma warning disable U2U1011
#pragma warning disable CA1859
        private static object CoerceVolumeLevel(DependencyObject d, object baseValue)
#pragma warning restore CA1859
#pragma warning restore U2U1011
        {
            var value = (int)baseValue;
            return Math.Clamp(value, 0, 100);
        }

        // change number of levels to add or remove display "blocks"
        private readonly int _levelsCount = 14;
        private const int RedBlocksDivisor = 7;
        private const int YellowBlocksDivisor = 4;

        private int _cachedNumRedBlocks;
        private int _cachedNumYellowBlocks;

        private readonly DrawingVisual _drawingVisual;

        private Image? _image;
        private Border? _innerBorder;
        private SolidColorBrush _backBrush;
        private SolidColorBrush _lightGreenBrush;
        private SolidColorBrush _yellowBrush;
        private SolidColorBrush _redBrush;

        private List<RenderTargetBitmap?> _bitmaps;
        private Size _lastSize;

        static VduControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(VduControl), new FrameworkPropertyMetadata(typeof(VduControl)));

            // Display-only control — exclude from keyboard navigation
            IsTabStopProperty.OverrideMetadata(typeof(VduControl), new FrameworkPropertyMetadata(false));
            FocusableProperty.OverrideMetadata(typeof(VduControl), new FrameworkPropertyMetadata(false));
        }

        public VduControl()
        {
            Debug.Assert(_levelsCount >= 7, "_levelsCount >= 7");

            InitBitmaps();
            InitBrushes();

            _drawingVisual = new DrawingVisual();

            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Wraps the VolumeLevel DP (value range 0 - 100)
        /// </summary>
        public int VolumeLevel
        {
            get => (int)GetValue(VolumeLevelProperty);
            set => SetValue(VolumeLevelProperty, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new VduControlAutomationPeer(this);

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
                _innerBorder.SizeChanged += OnSizeChanged;
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_innerBorder != null)
            {
                var newSize = new Size(_innerBorder.ActualWidth, _innerBorder.ActualHeight);
                if (_lastSize != newSize)
                {
                    InvalidateBitmaps();
                    _lastSize = newSize;
                    Refresh();
                }
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_innerBorder != null)
            {
                _innerBorder.SizeChanged -= OnSizeChanged;
            }

            Unloaded -= OnUnloaded;
            Cleanup();
        }

        private void Cleanup()
        {
            InvalidateBitmaps();

            _backBrush?.Freeze();
            _lightGreenBrush?.Freeze();
            _yellowBrush?.Freeze();
            _redBrush?.Freeze();
        }

        private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VduControl c)
            {
                c.OnVolumeChanged((int)e.OldValue, (int)e.NewValue);
            }
        }

        private void OnVolumeChanged(int oldValue, int newValue)
        {
            Refresh();

            if (UIElementAutomationPeer.FromElement(this) is VduControlAutomationPeer peer)
            {
                peer.RaiseVolumeChangedEvent(oldValue, newValue);
            }
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
            if (bmpHeight == 0)
            {
                return null;
            }

            var overallBlockHeight = bmpHeight / _levelsCount;
            if (overallBlockHeight == 0)
            {
                return null;
            }

            bmpHeight = overallBlockHeight * _levelsCount;  // normalise

            var ySpaceBetweenBlocks = Math.Max(overallBlockHeight / 3, 1);
            var blockHeight = overallBlockHeight - ySpaceBetweenBlocks;

            var bmpWidth = (int)_innerBorder.ActualWidth;
            if (bmpWidth == 0)
            {
                return null;
            }

            var blockWidth = bmpWidth;

            _bitmaps[numBlocksLit] = new RenderTargetBitmap(bmpWidth, bmpHeight, 96, 96, PixelFormats.Pbgra32);

            var numRedBlocks = _cachedNumRedBlocks;
            var numYellowBlocks = _cachedNumYellowBlocks;

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

        private void InvalidateBitmaps()
        {
            for (var n = 0; n < _bitmaps.Count; ++n)
            {
                _bitmaps[n] = null;
            }

            InitBitmaps();

            _cachedNumRedBlocks = _levelsCount / RedBlocksDivisor;
            _cachedNumYellowBlocks = _levelsCount / YellowBlocksDivisor;
        }

        [MemberNotNull(nameof(_backBrush), nameof(_lightGreenBrush), nameof(_yellowBrush), nameof(_redBrush))]
        private void InitBrushes()
        {
            _backBrush = new SolidColorBrush { Color = Colors.Black };
            _backBrush.Freeze();

            _lightGreenBrush = new SolidColorBrush { Color = Colors.GreenYellow };
            _lightGreenBrush.Freeze();

            _yellowBrush = new SolidColorBrush { Color = Colors.Yellow };
            _yellowBrush.Freeze();

            _redBrush = new SolidColorBrush { Color = Colors.Red };
            _redBrush.Freeze();
        }

        [MemberNotNull(nameof(_bitmaps))]
        private void InitBitmaps()
        {
            _bitmaps = new List<RenderTargetBitmap?>(_levelsCount + 1);

            for (var n = 0; n < _levelsCount + 1; ++n)
            {
                _bitmaps.Add(null);
            }
        }
    }
}