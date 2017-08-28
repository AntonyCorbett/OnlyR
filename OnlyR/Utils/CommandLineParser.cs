using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlyR.Utils
{
    public class CommandLineParser
    {
        private readonly List<string> _rawItems;
        private readonly List<string> _switches;
        private readonly Dictionary<string, string> _parameters;
        private bool _parsed;

        private const string ID_KEY = "/id";

        private static CommandLineParser _instance;
        public static CommandLineParser Instance => _instance ?? (_instance = new CommandLineParser());

        private readonly string[] _switchValues =
        {
            "-nogpu"
        };

        private readonly string[] _paramKeys =
        {
            ID_KEY
        };

        public string GetId()
        {
            return GetParamValue(ID_KEY);
        }

        public CommandLineParser(IEnumerable<string> args = null)
        {
            _rawItems = args?.ToList() ?? Environment.GetCommandLineArgs().ToList();
            _switches = new List<string>();
            _parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> Switches
        {
            get
            {
                Parse();
                return _switches;
            }
        }

        public IReadOnlyDictionary<string, string> Parameters
        {
            get
            {
                Parse();
                return _parameters;
            }
        }

        // ReSharper disable once UnusedMember.Global
        public bool IsSwitchSet(string key)
        {
            return Switches.Contains(key);
        }

        public string GetParamValue(string key, string defValue = null)
        {
            string result = defValue;

            if (Parameters.ContainsKey(key))
            {
                result = Parameters[key];
            }

            return result;
        }

        private void Parse()
        {
            if (!_parsed)
            {
                StringBuilder currentParamValue = new StringBuilder();
                string currentKey = null;

                // skip the app path (item 0)...
                int startIndex = 1;
                for (int n = startIndex; n < _rawItems.Count; ++n)
                {
                    var item = _rawItems[n];

                    if (!string.IsNullOrWhiteSpace(item) && !item.Equals("="))
                    {
                        if (_switchValues.Contains(item, StringComparer.OrdinalIgnoreCase))
                        {
                            _switches.Add(item.ToLower());
                            SaveParam(currentKey, currentParamValue);
                        }
                        else
                        {
                            var key = _paramKeys.FirstOrDefault(x => item.StartsWith(x, StringComparison.OrdinalIgnoreCase));
                            if (key != null)
                            {
                                SaveParam(currentKey, currentParamValue);

                                currentKey = key;
                                currentParamValue.Clear();

                                if (item.Length > key.Length)
                                {
                                    if (currentParamValue.Length > 0)
                                    {
                                        currentParamValue.Append(" ");
                                    }
                                    currentParamValue.Append(item.Substring(key.Length).Replace("=", "").Trim());
                                }
                            }
                            else
                            {
                                // add to the param value...
                                if (currentParamValue.Length > 0)
                                {
                                    currentParamValue.Append(" ");
                                }
                                currentParamValue.Append(item);
                            }
                        }
                    }
                }

                SaveParam(currentKey, currentParamValue);
                _parsed = true;
            }
        }

        private void SaveParam(string currentKey, StringBuilder currentParamValue)
        {
            if (!string.IsNullOrEmpty(currentKey) && currentParamValue.Length > 0)
            {
                if (!_parameters.ContainsKey(currentKey))
                {
                    _parameters.Add(currentKey, currentParamValue.ToString());
                }
            }
        }
    }

}
