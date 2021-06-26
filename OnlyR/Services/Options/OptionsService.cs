namespace OnlyR.Services.Options
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Windows;
    using System.Windows.Markup;
    using Model;
    using Newtonsoft.Json;
    using Serilog;
    using Utils;

    /// <summary>
    /// The Option service is used to store and retrieve program settings
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class OptionsService : IOptionsService
    {
        private readonly ICommandLineService _commandLineService;
        private readonly int _optionsVersion = 1;
        private string _optionsFilePath;
        private string _originalOptionsSignature;

        public OptionsService(ICommandLineService commandLineService)
        {
            _commandLineService = commandLineService;

            Init();
        }

        public Options Options { get; private set; }

        /// <summary>
        /// Gets or sets the culture.
        /// </summary>
        /// <value>
        /// The culture.
        /// </value>
        public string Culture
        {
            get => Options.Culture;
            set
            {
                if (Options.Culture == null || Options.Culture != value)
                {
                    Options.Culture = value;
                }
            }
        }

        /// <summary>
        /// Gets a list of supported MP3 bit rates
        /// </summary>
        /// <returns>Collection of BitRateItem</returns>
        public BitRateItem[] GetSupportedMp3BitRates()
        {
            var result = new List<BitRateItem>();

            var validBitRates = Options.GetSupportedMp3BitRates();
            foreach (var rate in validBitRates)
            {
                result.Add(new BitRateItem { Name = rate.ToString(), ActualBitRate = rate });
            }

            return result.ToArray();
        }

        /// <summary>
        /// Gets a list of supported sample rates (for recording)
        /// </summary>
        /// <returns>Collection of SampleRateItem</returns>
        public SampleRateItem[] GetSupportedSampleRates()
        {
            var result = new List<SampleRateItem>();

            var validSampleRates = Options.GetSupportedSampleRates();
            foreach (var rate in validSampleRates)
            {
                result.Add(new SampleRateItem { Name = rate.ToString(), ActualSampleRate = rate });
            }

            return result.ToArray();
        }

        /// <summary>
        /// Gets a list of supported channel counts
        /// </summary>
        /// <returns>Collection of ChannelItem</returns>
        public ChannelItem[] GetSupportedChannels()
        {
            var result = new List<ChannelItem>();

            var channels = Options.GetSupportedChannels();
            foreach (var c in channels)
            {
                result.Add(new ChannelItem { Name = GetChannelName(c), ChannelCount = c });
            }

            return result.ToArray();
        }

        /// <summary>
        /// Saves the settings (if they have changed since they were last read)
        /// </summary>
        public void Save()
        {
            try
            {
                var newSignature = GetOptionsSignature(Options);
                if (_originalOptionsSignature != newSignature)
                {
                    // changed...
                    WriteOptions();
                    Log.Logger.Information("Settings changed and saved");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Could not save settings");
            }
        }

        private static string GetChannelName(int channelsCount)
        {
            switch (channelsCount)
            {
                case 1:
                    return Properties.Resources.MONO;
                case 2:
                    return Properties.Resources.STEREO;
                default:
                    return "Unknown";
            }
        }

        private void Init()
        {
            if (Options == null)
            {
                try
                {
                    string commandLineIdentifier = _commandLineService.OptionsIdentifier;
                    _optionsFilePath = FileUtils.GetUserOptionsFilePath(commandLineIdentifier, _optionsVersion);
                    var path = Path.GetDirectoryName(_optionsFilePath);
                    if (path != null)
                    {
                        Directory.CreateDirectory(path);
                        ReadOptions();
                    }

                    if (Options == null)
                    {
                        Options = new Options();
                    }

                    // store the original settings so that we can determine if they have changed
                    // when we come to save them
                    _originalOptionsSignature = GetOptionsSignature(Options);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Could not read options file");
                    Options = new Options();
                }
            }
        }

        private static string GetOptionsSignature(Options options)
        {
            var originalGenre = options.Genre;

            if (originalGenre != null && originalGenre.Trim() == Properties.Resources.SPEECH)
            {
                // denotes default for the language.
                options.Genre = null;
            }

            // config data is small so simple solution is best...
            var signature = JsonConvert.SerializeObject(options);

            options.Genre = originalGenre;

            return signature;
        }

        private void ReadOptions()
        {
            if (!File.Exists(_optionsFilePath))
            {
                WriteDefaultOptions();
            }
            else
            {
                ReadOptionsInternal();
                if (Options == null)
                {
                    WriteDefaultOptions();
                }
            }

            if (Options == null)
            {
#pragma warning disable S112 // General exceptions should never be thrown
                throw new Exception($"Could not read options file: {_optionsFilePath}");
#pragma warning restore S112 // General exceptions should never be thrown
            }
            
            SetCulture();
            Options.Sanitize();
        }

        private void ReadOptionsInternal()
        {
            using (var file = File.OpenText(_optionsFilePath))
            {
                var serializer = new JsonSerializer();
                Options = (Options)serializer.Deserialize(file, typeof(Options));
            }
        }

        private void SetCulture()
        {
            var culture = Options.Culture;

            if (string.IsNullOrEmpty(culture))
            {
                culture = CultureInfo.CurrentCulture.Name;
            }

            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
                FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(
                        XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Could not set culture");
            }
        }

        private void WriteDefaultOptions()
        {
            Options = new Options();
            WriteOptions();
        }

        private void WriteOptions()
        {
            if (Options != null)
            {
                using (StreamWriter file = File.CreateText(_optionsFilePath))
                {
                    var originalGenre = Options.Genre;

                    if (originalGenre != null && originalGenre.Trim() == Properties.Resources.SPEECH)
                    {
                        // denotes default for the language.
                        Options.Genre = null;
                    }
                    
                    var serializer = new JsonSerializer();
                    serializer.Serialize(file, Options);

                    Options.Genre = originalGenre;

                    _originalOptionsSignature = GetOptionsSignature(Options);
                }
            }
        }
    }
}
