namespace OnlyR.Services.Options
{
    public interface ICommandLineService
    {
        bool NoGpu { get; set; }

        string OptionsIdentifier { get; set; }
    }
}
