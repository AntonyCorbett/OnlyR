using CommunityToolkit.Mvvm.Messaging;
using OnlyR.Services.Options;
using OnlyR.Utils;
using OnlyR.ViewModel;
using OnlyR.ViewModel.Messages;
using Serilog;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using Forms = System.Windows.Forms;

namespace OnlyR.Services.Tray;

/// <summary>
/// Notification area (tray) icon. Lets the window live in the notification area
/// only, and offers start/stop recording without showing the window.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TrayIconService : ITrayIconService, IDisposable
{
    private readonly MainViewModel _mainViewModel;
    private readonly IOptionsService _optionsService;

    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ToolStripMenuItem? _recordMenuItem;
    private Icon? _idleIcon;
    private Icon? _recordingIcon;
    private Window? _mainWindow;
    private bool _disposed;

    public TrayIconService(MainViewModel mainViewModel, IOptionsService optionsService)
    {
        _mainViewModel = mainViewModel;
        _optionsService = optionsService;

        _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
        WeakReferenceMessenger.Default.Register<MinimizeToTrayChanged>(this, (_, _) => UpdateIconVisibility());
        WeakReferenceMessenger.Default.Register<ShutDownApplicationMessage>(this, (_, _) => Dispose());
    }

    public bool IsExiting { get; private set; }

    public void Initialize(Window mainWindow)
    {
        if (_disposed || _notifyIcon != null)
        {
            return;
        }

        _mainWindow = mainWindow;
        CreateTrayIcon();
        UpdateIconVisibility();
        UpdateMenuState();
    }

    public void RequestExit()
    {
        IsExiting = true;
        _mainViewModel.RequestExit();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);

        if (_notifyIcon != null)
        {
            var menu = _notifyIcon.ContextMenuStrip;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
            menu?.Dispose();
        }

        _recordingIcon?.Dispose();
        _idleIcon?.Dispose();
        _recordingIcon = null;
        _idleIcon = null;
    }

    private static Icon? LoadApplicationIcon()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                return Icon.ExtractAssociatedIcon(processPath);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or System.IO.IOException)
        {
            Log.Logger.Warning(ex, "Could not load the application icon for the tray");
        }

        return null;
    }

    private static Icon? CreateRecordingIcon(Icon? baseIcon)
    {
        if (baseIcon == null)
        {
            return null;
        }

        using var bitmap = baseIcon.ToBitmap();
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var diameter = Math.Max(6, bitmap.Width / 2);
        var left = bitmap.Width - diameter;
        var top = bitmap.Height - diameter;

        using var brush = new SolidBrush(Color.FromArgb(230, 200, 30, 30));
        graphics.FillEllipse(brush, left, top, diameter, diameter);

        var iconHandle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(iconHandle);
            return (Icon)icon.Clone();
        }
        finally
        {
            _ = NativeMethods.DestroyIcon(iconHandle);
        }
    }

    private void CreateTrayIcon()
    {
        _idleIcon = LoadApplicationIcon();
        _recordingIcon = CreateRecordingIcon(_idleIcon);

        var menu = new Forms.ContextMenuStrip();

        var showItem = new Forms.ToolStripMenuItem(Properties.Resources.TRAY_SHOW_APP);
        showItem.Click += (_, _) => ShowWindow();
        menu.Items.Add(showItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        _recordMenuItem = new Forms.ToolStripMenuItem(Properties.Resources.TRAY_START_RECORDING);
        _recordMenuItem.Click += (_, _) => ToggleRecording();
        menu.Items.Add(_recordMenuItem);

        var folderItem = new Forms.ToolStripMenuItem(Properties.Resources.TRAY_OPEN_RECORDINGS);
        folderItem.Click += (_, _) => _mainViewModel.OpenRecordingsFolder();
        menu.Items.Add(folderItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem(Properties.Resources.TRAY_EXIT);
        exitItem.Click += (_, _) => RequestExit();
        menu.Items.Add(exitItem);

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _idleIcon ?? SystemIcons.Application,
            Text = "OnlyR",
            ContextMenuStrip = menu,
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsRecording) or nameof(MainViewModel.CanRecord))
        {
            // recording state changes can arrive on the audio capture thread
            RunOnUiThread(UpdateMenuState);
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _ = dispatcher.BeginInvoke(action);
        }
    }

    private void UpdateIconVisibility()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = _optionsService.Options.MinimizeToTray;
        }
    }

    private void UpdateMenuState()
    {
        if (_notifyIcon == null || _recordMenuItem == null)
        {
            return;
        }

        var isRecording = _mainViewModel.IsRecording;

        _recordMenuItem.Text = isRecording
            ? Properties.Resources.TRAY_STOP_RECORDING
            : Properties.Resources.TRAY_START_RECORDING;
        _recordMenuItem.Enabled = isRecording || _mainViewModel.CanRecord;

        _notifyIcon.Icon = isRecording
            ? _recordingIcon ?? _idleIcon ?? SystemIcons.Application
            : _idleIcon ?? SystemIcons.Application;

        _notifyIcon.Text = isRecording
            ? Properties.Resources.TRAY_TOOLTIP_RECORDING
            : "OnlyR";
    }

    private void ToggleRecording()
    {
        if (_mainViewModel.IsRecording)
        {
            _mainViewModel.StopRecording();
        }
        else if (_mainViewModel.CanRecord)
        {
            _mainViewModel.StartRecording();
        }
    }

    private void ShowWindow()
    {
        if (_mainWindow == null)
        {
            return;
        }

        _mainWindow.Show();

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _ = _mainWindow.Activate();
    }
}
