using System;
using System.IO;
using Newtonsoft.Json;
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
    }
}
