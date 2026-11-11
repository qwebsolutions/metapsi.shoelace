using Metapsi.Html;
using System.Reflection;

namespace Metapsi.Shoelace;

public class AutoRegister : IAutoRegisterModule
{
    public void Register(WebResourceProvider webResourceProvider)
    {
        var assembly = typeof(AutoRegister).Assembly;
        foreach (var rn in assembly.GetManifestResourceNames())
        {
            webResourceProvider.RegisterEmbeddedResource(EmbeddedResource.Versioned(assembly, Cdn.Version, rn));
        }
    }
}
