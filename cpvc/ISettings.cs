namespace CPvC
{
    /// <summary>
    /// Interface for application-wide configuration settings.
    /// </summary>
    public interface ISettings
    {
        string MachinesFolder { get; set; }
        string DiscsFolder { get; set; }
        string TapesFolder { get; set; }
        string RecentlyOpened { get; set; }
    }
}
