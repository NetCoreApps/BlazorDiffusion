using ServiceStack;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceModel;

public enum StatType
{
    Download,
}

[Icon(Svg = Icons.Stats)]
public class ArtifactStat
{
    [AutoIncrement]
    public int Id { get; set; }

    public StatType Type { get; set; }
    public int ArtifactId { get; set; }

    [References(typeof(AppUser))]
    public int? AppUserId { get; set; }
    public string RefId { get; set; }
    public string Source { get; set; }
    public string Version { get; set; }
    public string RawUrl { get; set; }
    public string RemoteIp { get; set; }
    public DateTime CreatedDate { get; set; }
}
