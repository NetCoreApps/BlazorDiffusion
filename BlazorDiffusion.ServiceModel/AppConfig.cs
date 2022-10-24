using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceModel;

public class AppConfig
{
    public static AppConfig Instance = new();

    public string ArtifactBucket { get; set; }
    public string R2Account { get; set; }
    public string AssetsBasePath { get; set; }

    public static AppConfig Set(AppConfig instance) => Instance = instance;
}
