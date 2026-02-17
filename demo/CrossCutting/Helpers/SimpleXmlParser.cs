using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using CrossCutting.Logger;

namespace CrossCutting.Helpers;

public static class SimpleXmlParser
{
    /// <summary>
    /// 反序列化登機證的物件之用
    /// ---
    /// 預設會清空所有 Namespace
    /// </summary>
    /// <param name="base64Text"></param>
    /// <typeparam name="TData"></typeparam>
    /// <returns></returns>
    public static TData? Deserialize<TData>(string? base64Text) where TData : class
    {
        if (string.IsNullOrEmpty(base64Text)) return null;
        
        try
        {
            var decodedData = Convert.FromBase64String(base64Text);
            var decodeText = Encoding.UTF8.GetString(decodedData);

            var xml = XElement.Parse(decodeText);
            var xmlCleaned = RemoveAllNamespaces(xml);

            var serializer = new XmlSerializer(typeof(TData));
            using var reader = xmlCleaned.CreateReader();

            return serializer.Deserialize(reader) as TData;
        }
        catch (Exception ex)
        {
            AppLog.Error($"deserialization fail : {ex}");
            return null;
        }
    }

    private static XElement RemoveAllNamespaces(XElement element) =>
        new XElement(element.Name.LocalName, element.HasElements
                ? element.Elements().Select(RemoveAllNamespaces)
                : (object?)element.Value);
}