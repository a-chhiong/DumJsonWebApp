using System.Reflection;
using System.Xml.Linq;

namespace WebAPI.Swagger.Extensions;

public static class SwaggerSummaryExtensions
{
    private static readonly Dictionary<string, XDocument?> XmlCache = new();

    public static string GetSummary(this MemberInfo memberInfo)
    {
        try
        {
            var type = memberInfo.DeclaringType ?? (memberInfo as Type);
            if (type == null) return string.Empty;

            var assemblyName = type.Assembly.GetName().Name;
            if (string.IsNullOrEmpty(assemblyName)) return string.Empty;

            if (!XmlCache.TryGetValue(assemblyName, out var xmlDoc))
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
                xmlDoc = File.Exists(xmlPath) ? XDocument.Load(xmlPath) : null;
                XmlCache[assemblyName] = xmlDoc;
            }

            if (xmlDoc == null) return string.Empty;

            // P: for Property, T: for Type
            var prefix = memberInfo is PropertyInfo ? "P" : "T";
            var memberName = $"{prefix}:{type.FullName?.Replace("+", ".")}.{memberInfo.Name}";
            
            var xmlValue = xmlDoc.Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName)
                ?.Element("summary")?.Value;

            return CleanSummary(xmlValue);
        }
        catch { return string.Empty; }
    }

    private static string CleanSummary(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        
        return string.Join("\n", input.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l)));
    }
}