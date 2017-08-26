using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using OnlyR.Utils;

namespace OnlyR.Services.Options
{
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
                string commandLineIdentifier = CommandLineParser.Instance.GetId();
                _optionsFilePath = FileUtils.GetUserOptionsFilePath(commandLineIdentifier, _optionsVersion);
                ReadOptions();
            }
        }

        private void ReadOptions()
        {
            using (StreamReader file = File.OpenText(_optionsFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                _options = (Options)serializer.Deserialize(file, typeof(Options));
            }
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
