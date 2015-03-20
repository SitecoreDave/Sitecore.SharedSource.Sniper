using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.RenderField;
using System;
using System.Text.RegularExpressions;

namespace Sitecore.SharedSource.Sniper
{
    public class RichTextSniperProcessor
    {
        public virtual void Process(RenderFieldArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (Context.Site.DisplayMode == Sites.DisplayMode.Edit) return;
            if (args == null || args.FieldTypeKey != "rich text") return;

            Common.Log("Sniper running", "verbose");
            if (String.IsNullOrEmpty(args.Result.FirstPart)) return;

            args.Result.FirstPart = GetSnipedValueRegEx(args.Result.FirstPart);
        }

        private static string GetSnipedValueRegEx(string text)
        {
            text = GetEval(text);

            if (!text.Contains(@"href=""~/sniper.aspx?_id=", StringComparison.OrdinalIgnoreCase)) return text;

            Common.Log("Sniper found", "verbose");

            //grab all sniper links
            //var pattern = @"<a\s+(?=[^<>]*type=[""\'])([^<>]*href=[""'].+?[""'])[^<>]+>[^<>]+</a>";
            //var pattern = @"<a\s+(?=[^<>]*type=[""'][""'])href=[""'](.+?)[""'][^<>]*>.*?</a>";
            //var pattern = @"href=""~/sniper.aspx([^""]*""[^>]*>";
            const string pattern = @"<a href=""(~/sniper.aspx([^""]*))""[^>]*>.*?</a>";

            var m1 = Regex.Matches(text, pattern, RegexOptions.Singleline);

            foreach (Match m in m1)
            {
                var fullItemText = m.Groups[0].Value;
                var itemPath = m.Groups[1].Value.Replace("href=", "");
                if (itemPath.StartsWith("\"")) itemPath = itemPath.Remove(0, 1);
                if (itemPath.EndsWith("\"")) itemPath = itemPath.Remove(itemPath.Length - 1, 1);
                if (itemPath.Contains("~/link.aspx?_id=", StringComparison.OrdinalIgnoreCase)) itemPath = itemPath.Replace("~/link.aspx?_id=", "");
                if (itemPath.Contains("~/sniper.aspx?_id=", StringComparison.OrdinalIgnoreCase)) itemPath = itemPath.Replace("~/sniper.aspx?_id=", "");
                if (itemPath.Contains("&amp;_z=z", StringComparison.OrdinalIgnoreCase)) itemPath = itemPath.Replace("&amp;_z=z", "");
                
                if (String.IsNullOrEmpty(itemPath)) continue;
                Common.Log(String.Format("Sniper - Getting item for itemPath: {0}", itemPath));
                var itemId = new ID(itemPath);

                var sniperItem = Context.Database.GetItem(itemId);
                if (sniperItem == null)
                {
                    Common.Log(String.Concat("Sniper Couldn't find item:", itemId));
                }
                if (sniperItem == null || String.IsNullOrEmpty(sniperItem.TemplateName)) continue;

                var fieldValue = GetSnipedFieldValue(sniperItem);
                text = text.Replace(fullItemText, fieldValue);
            }
            return text;
        }

        private static string GetSnipedFieldValue(Item sniperItem)
        {
            var sniperTemplatesConfig = Configuration.Settings.GetAppSetting("SniperAllowedTemplates", "Snippet|Snippet,Dictionary entry|Phrase,Sample Item|Text");
            var sniperTemplates = sniperTemplatesConfig.Split(',').Select(value => value.Split('|')).ToDictionary(pair => pair[0], pair => pair[1]);

            if (!sniperTemplates.ContainsKey(sniperItem.TemplateName)) return null;

            var fieldName = sniperTemplates[sniperItem.TemplateName];
            if (fieldName == null) return null;

            var fieldValue = sniperItem[fieldName];
            fieldValue = GetEval(fieldValue);
            if (!String.IsNullOrEmpty(fieldValue)) Common.Log(String.Concat("Sniper Snippet item:", sniperItem.ID.ToString()));
            return GetSnipedValueRegEx(fieldValue);
        }

        private static string GetEval(string text)
        {
            if (!text.StartsWith("{") || !text.EndsWith("}")) return text;
            return Common.Eval(text.Substring(1, text.Length - 2));
        }
    }
}
