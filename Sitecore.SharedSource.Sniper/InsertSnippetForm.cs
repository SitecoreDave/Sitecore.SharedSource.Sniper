using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using System;

namespace Sitecore.SharedSource.Sniper
{
    public class InsertSnippetForm : DialogForm
    {
        //protected DataContext MediaDataContext;
        protected DataContext SnippetsDataContext;
        protected DataContext DictionaryDataContext;
        //protected DataContext SystemDictionaryDataContext;
        
        protected TreeviewEx SnippetsTreeview;
        protected TreeviewEx DictionaryTreeview;

        protected Tabstrip Tabs;
        protected Tab SnippetsTab;
        protected Tab DictionaryTab;

        protected Frame SnippetSearchTabFrame;
        //protected Frame DictionarySearchTabFrame;

        // Fields
        protected Edit TextEdit;
        protected override void OnCancel(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            if (Mode != "webedit")
            {
                SheerResponse.Eval("scCancel()");
                return;
            }
            base.OnCancel(sender, args);
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);

            if (Context.ClientPage == null || Context.ClientPage.IsEvent) return;

            Mode = WebUtil.GetQueryString("mo");
            SnippetsDataContext.GetFromQueryString();
            DictionaryDataContext.GetFromQueryString();
            //const string minisearchPage = "/sitecore/shell/Applications/Buckets/MiniResults.aspx";
            //SnippetSearchTabFrame.SourceUri = String.Concat(minisearchPage, "?StartSearchLocation=", SnippetsDataContext.Root);
            //DictionarySearchTabFrame.SourceUri = String.Concat(minisearchPage, "?StartSearchLocation=", DictionaryDataContext.Root);
            //DictionarySearchTabFrame.SetSource(minisearchPage, "template=dictionary");
            string queryString = WebUtil.GetQueryString("fo");
            if (queryString == null || queryString.Length <= 0) return;

            if (!string.IsNullOrEmpty(WebUtil.GetQueryString("databasename")))
            {
                SnippetsDataContext.Parameters = string.Concat("databasename=", WebUtil.GetQueryString("databasename"));
                DictionaryDataContext.Parameters = string.Concat("databasename=", WebUtil.GetQueryString("databasename"));
            }
            if (queryString.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
            {
                if (!queryString.StartsWith("/sitecore", StringComparison.InvariantCulture))
                {
                    queryString = IO.FileUtil.MakePath("/sitecore/content", queryString, '/');
                }
                if (queryString.EndsWith(".aspx", StringComparison.InvariantCulture))
                {
                    queryString = StringUtil.Left(queryString, queryString.Length - 5);
                }
                SnippetsDataContext.Folder = queryString;
            }
            else if (!ShortID.IsShortID(queryString))
            {
                var item = SnippetsDataContext.GetDatabase().GetItem(queryString);
                if (item == null || String.IsNullOrEmpty(item.TemplateName)) return;

                SnippetsTab.Active = (item.TemplateName == "Snippet");
                //DictionaryTab.Active = (item.TemplateName == "Dictionary entry");
            }
            else
            {
                if (String.IsNullOrEmpty(queryString)) return;
                queryString = ShortID.Parse(queryString).ToID().ToString();
                if (Client.ContentDatabase == null) return;
                Item item1 = Client.ContentDatabase.GetItem(queryString);
                
                if (item1 != null)
                {
                    SnippetsDataContext.Folder = queryString;

                //    if (item1.Paths.IsMediaItem)
                //    {
                //        DictionaryDataContext.Folder = queryString;
                //        MediaTab.Active = true;
                //    }
                //    else
                //    {
                //        SnippetsDataContext.Folder = queryString;
                //    }
                }
            }
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Common.Log("Snippet - OnOk");
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            var selectedItem = GetCurrentItem();
            if (selectedItem == null || selectedItem.TemplateName == null)
            {
                Common.Log(selectedItem == null
                    ? "Snippet insert error - selectedItem is null"
                    : (!String.IsNullOrEmpty(selectedItem.TemplateName) ? String.Format("Snippet insert error - templateName: {0}", selectedItem.TemplateName) : ""));

                SheerResponse.Alert("Error inserting Snippet to item.", new string[0]);
                return;
            }
            var sniperTemplatesConfig = Configuration.Settings.GetAppSetting("SniperAllowedTemplates", "Snippet|Snippet,Dictionary Entry|Phrase,Sample Item|Text");
            var sniperTemplates = sniperTemplatesConfig.Split(',').Select(value => value.Split('|')).ToDictionary(pair => pair[0], pair => pair[1]);
            //(selectedItem.TemplateName != "Snippet" && selectedItem.TemplateName != "Dictionary entry")
            if (Configuration.Settings.GetAppSetting("SniperAllowedAllTypes", "") == "false" && !sniperTemplates.ContainsKey(selectedItem.TemplateName))
            {
                Common.Log(String.Format("Error inserting Snippet to item: {0}{1}", selectedItem.Name,
                    !String.IsNullOrEmpty(selectedItem.TemplateName) ? " - template: " + selectedItem.TemplateName : ""));

                SheerResponse.Alert("Error inserting Snippet to item.", new string[0]);
                return;
            }

            var displayName = selectedItem.DisplayName;
            var dynamicUrl = LinkManager.GetDynamicUrl(selectedItem, new LinkUrlOptions());
            dynamicUrl = dynamicUrl.Replace("~/link.aspx?", "~/sniper.aspx?");

            var javascriptArguments = string.Format("{0}, {1}", StringUtil.EscapeJavascriptString(dynamicUrl), StringUtil.EscapeJavascriptString(displayName));
 
            if (Mode == "webedit")
            {
                SheerResponse.SetDialogValue(javascriptArguments);
                base.OnOK(sender, args);
                return;
            }
            SheerResponse.Eval(string.Format("scClose({0})", javascriptArguments));
        }

        private Item GetCurrentItem()
        {
            //Assert.ArgumentNotNull(message, "message");
            //string str = message["id"];
            var item = (Tabs.Active == 0 ? SnippetsTreeview.GetSelectionItem() : DictionaryTreeview.GetSelectionItem());
            //var item = SnippetsTreeview.GetSelectionItem();
            if (item == null)
            {
                return null;
            }
            //if (string.IsNullOrEmpty(str) || !ID.IsID(str))
            //{
            //    return item;
            //}
            //return item.Database.GetItem(ID.Parse(str), item.Language, Sitecore.Data.Version.Latest);
            return item;
        }

        // Properties
        protected string Mode
        {
            get
            {
                if (ServerProperties == null) return "shell";
                var str = StringUtil.GetString(ServerProperties["Mode"]);
                return !string.IsNullOrEmpty(str) ? str : "shell";
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                ServerProperties["Mode"] = value;
            }
        }
    }
}