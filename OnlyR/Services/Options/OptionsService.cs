using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OnlyR.Model;
using OnlyR.Utils;
using Serilog;

namespace OnlyR.Services.Options
{
    // todo: UI for setting and saving options

    // ReSharper disable once UnusedMember.Global
    public class OptionsService : IOptionsService
    {
        private Options _options;
        private readonly int _optionsVersion = 1;
        private string _optionsFilePath;

        public Options Options
        {
            get
            {
                Init();
                return _options;
            }
        }

        private void Init()
        {
            if (_options == null)
            {
                try
                {
                    string commandLineIdentifier = CommandLineParser.Instance.GetId();
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
                }
                catch(Exception ex)
                {
                    Log.Logger.Error(ex, "Could not read options file");
                    _options = new Options();
                }
            }
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
                    _options = (Options) serializer.Deserialize(file, typeof(Options));
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
                }
            }
        }

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

        public IEnumerable<SampleRateItem> GetSupportedSampleRates()
        {
            var result = new List<SampleRateItem>();

            var validSampleRates = Options.GetSupportedSampleRates();
            foreach(var rate in validSampleRates)
            {
                result.Add(new SampleRateItem { Name = rate.ToString(), ActualSampleRate = rate });
            }

            return result;
        }

        public IEnumerable<ChannelItem> GetSupportedChannels()
        {
            var result = new List<ChannelItem>();

            var channels = Options.GetSupportedChannels();
            foreach(var c in channels)
            {
                result.Add(new ChannelItem { Name = GetChannelName(c), ChannelCount = c });
            }

            return result;
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

        public void Save()
        {
            WriteOptions();
        }
    }
}
