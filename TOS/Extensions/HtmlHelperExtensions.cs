using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TOS.Extensions;

public static class HtmlHelperExtensions
{
    public static string SelectStringByLanguage(this IHtmlHelper htmlHelper, string en, string cz)
    {
        return CultureInfo.CurrentCulture.Name.Contains("cz") ? cz : en;
    }
}