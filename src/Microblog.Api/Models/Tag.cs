namespace Microblog.Api.Models;

public class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TagHash { get; set; } = string.Empty;
}