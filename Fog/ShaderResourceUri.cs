namespace Fog;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) => new($"pack://application:,,,/Fog;component/Shaders/{shaderName}.cso", UriKind.Absolute);
}
