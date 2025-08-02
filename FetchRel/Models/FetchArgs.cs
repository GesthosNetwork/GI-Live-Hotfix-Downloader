namespace Models;

public class FetchArgs
{
    public string Command { get; set; } = "";
    public string Branch { get; set; } = "";
    public string Revision { get; set; } = "";
    public bool IsBase { get; set; }
    public string? AudioDiff { get; set; }
    public List<string> Clients { get; set; } = [];
    public string Url { get; set; } = "";
    public string OutDir { get; set; } = "";
    public bool Verbose { get; set; }

    public FetchArgs Clone()
    {
        return new FetchArgs
        {
            Command = this.Command,
            Branch = this.Branch,
            Revision = this.Revision,
            IsBase = this.IsBase,
            AudioDiff = this.AudioDiff,
            Clients = new List<string>(this.Clients),
            Url = this.Url,
            OutDir = this.OutDir,
            Verbose = this.Verbose
        };
    }
}
