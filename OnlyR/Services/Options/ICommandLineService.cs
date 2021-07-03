namespace OnlyR.Services.Options
{
    public interface ICommandLineService
    {
        bool NoGpu { get; set; }

        string? OptionsIdentifier { get; set; }

        bool NoSettings { get; set; }

        bool NoFolder { get; set; }

        bool NoSave { get; set; }
    }
}
