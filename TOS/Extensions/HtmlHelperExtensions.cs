using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TOS.Extensions;

public static class HtmlHelperExtensions
{
    public static string SelectStringByLanguage(this IHtmlHelper htmlHelper, string en, string cz)
    {
        return CultureInfo.CurrentCulture.Name.Contains("cz") ? cz : en;
    }

    public static HtmlString SelectOption(this IHtmlHelper htmlHelper, string value, string text, string? selectedValue)
    { 
        var selected = selectedValue == value ? "selected" : string.Empty;
        var output = $"<option value=\"{value}\" {selected}>{text}</option>";
        
        return new(output);
    }
}