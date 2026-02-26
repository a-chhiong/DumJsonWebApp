using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace WebApp.Formatters;

public class PlainTextInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextInputFormatter
{
    public PlainTextInputFormatter()
    {
        SupportedMediaTypes.Add("text/plain");
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context, Encoding encoding)
    {
        using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
        var content = await reader.ReadToEndAsync();
        return await InputFormatterResult.SuccessAsync(content);
    }
}