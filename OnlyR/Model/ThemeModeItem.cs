namespace OnlyR.Model
{
    /// <summary>
    /// Represents a theme mode option in the settings
    /// </summary>
    public class ThemeModeItem
    {
        public ThemeModeItem(string name, AppTheme theme)
        {
            Name = name;
            Theme = theme;
        }

        public string Name { get; }

        public AppTheme Theme { get; }
    }
}
