namespace DatabaseUpdater;

internal class Database
{
    public string Name { get; set; }
    public string ConnectionString { get; set; }
    public bool NeedChange { get; set; }
}
