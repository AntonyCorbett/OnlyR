namespace OnlyR.Services.Options
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
        private Options _options;
        private string _optionsFilePath;
        private string _originalOptionsSignature;

        public OptionsService(ICommandLineService commandLineService)
        {
            _commandLineService = commandLineService;
        }

        public Options Options
        {
            get
            {
                Init();
                return _options;
            }
        }

        /// <summary>
        /// Gets a list of supported MP3 bit rates
        /// </summary>
        /// <returns>Collection of BitRateItem</returns>
        public IEnumerable<BitRateItem> GetSupportedMp3BitRates()
        {
            var result = new List<BitRateItem>();

            var validBitRates = Options.GetSupportedMp3BitRates();
            foreach (var rate in validBitRates)
            {
                result.Add(new BitRateItem { Name = rate.ToString(), ActualBitRate = rate });
            }

            return result;
        }

        /// <summary>
        /// Gets a list of supported sample rates (for recording)
        /// </summary>
        /// <returns>Collection of SampleRateItem</returns>
        public IEnumerable<SampleRateItem> GetSupportedSampleRates()
        {
            var result = new List<SampleRateItem>();

            var validSampleRates = Options.GetSupportedSampleRates();
            foreach (var rate in validSampleRates)
            {
                result.Add(new SampleRateItem { Name = rate.ToString(), ActualSampleRate = rate });
            }

            return result;
        }

        /// <summary>
        /// Gets a list of supported channel counts
        /// </summary>
        /// <returns>Collection of ChannelItem</returns>
        public IEnumerable<ChannelItem> GetSupportedChannels()
        {
            var result = new List<ChannelItem>();

            var channels = Options.GetSupportedChannels();
            foreach (var c in channels)
            {
                result.Add(new ChannelItem { Name = GetChannelName(c), ChannelCount = c });
            }

            return result;
        }

        /// <summary>
        /// Saves the settings (if they have changed since they were last read)
        /// </summary>
        public void Save()
        {
            try
            {
                var newSignature = GetOptionsSignature(_options);
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
            if (_options == null)
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

                    if (_options == null)
                    {
                        _options = new Options();
                    }

                    // store the original settings so that we can determine if they have changed
                    // when we come to save them
                    _originalOptionsSignature = GetOptionsSignature(_options);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Could not read options file");
                    _options = new Options();
                }
            }
        }

        private string GetOptionsSignature(Options options)
        {
            // config data is small so simple solution is best...
            return JsonConvert.SerializeObject(options);
        }

        private void ReadOptions()
        {
            if (!File.Exists(_optionsFilePath))
            {
                WriteDefaultOptions();
            }
            else
            {
                using (StreamReader file = File.OpenText(_optionsFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    _options = (Options)serializer.Deserialize(file, typeof(Options));
                    _options.Sanitize();
                }
            }
        }

        private void WriteDefaultOptions()
        {
            _options = new Options();
            WriteOptions();
        }

        private void WriteOptions()
        {
            if (_options != null)
            {
                using (StreamWriter file = File.CreateText(_optionsFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, _options);

                    _originalOptionsSignature = GetOptionsSignature(_options);
                }
            }
        }
    }
}
