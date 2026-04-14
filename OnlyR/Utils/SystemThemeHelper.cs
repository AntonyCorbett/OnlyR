using Microsoft.Win32;
using Serilog;
using System;

namespace OnlyR.Utils
{
    internal static class SystemThemeHelper
    {
        private const string PersonalizeKeyPath =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        private const string AppsUseLightThemeValue = "AppsUseLightTheme";

        public static bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
                var value = key?.GetValue(AppsUseLightThemeValue);
                if (value is int intValue)
                {
                    return intValue == 0;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Could not read system theme from registry");
            }

            return false;
        }
    }
}
