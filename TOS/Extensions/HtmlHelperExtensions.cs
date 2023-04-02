using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using TOS.Models;

namespace TOS.Extensions;

public static class HtmlHelperExtensions
{
    public static string SelectStringByLanguage(this IHtmlHelper htmlHelper, string? en, string? cz)
    {
        if (en is null) return cz ?? "";
        if (cz is null) return en;

        return CultureInfo.CurrentCulture.Name.Contains("cz") ? cz : en;
    }

    public static HtmlString SelectOption(this IHtmlHelper htmlHelper, string value, string text, string? selectedValue)
    { 
        var selected = selectedValue == value ? "selected" : string.Empty;
        var output = $"<option value=\"{value}\" {selected}>{text}</option>";
        
        return new(output);
    }

    public static bool CanEditTopic(this IHtmlHelper htmlHelper, Topic topicObj, string? username, bool topic, bool anyTopic, bool proposeTopic)
    {
        if (username is null) return false;
        if (anyTopic) return true;
        if (topicObj.Group.Creator.UserName == username) return true;
        if (topic && (topicObj.Proposed || topicObj.SupervisorId == null)) return true;
        if (topic && (topicObj.Creator.UserName == username || (topicObj.Supervisor != null && topicObj.Supervisor.UserName == username))) return true;
        if ( proposeTopic && topicObj.Proposed && topicObj.Creator.UserName == username) return true;
        
        return false;
    }
    

    public static bool CanDeleteComment(this IHtmlHelper htmlHelper, Comment comment, string? username, bool commentRole, bool anyCommentRole)
    {
        if (username is null) return false;
        if (anyCommentRole) return true;
        if (comment.Author.UserName == username && commentRole) return true;

        return false;
    }
}