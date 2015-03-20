var scEditor = null;
var scTool = null;

//Set the Id of your button into the RadEditorCommandList[]
RadEditorCommandList["SniperButton"] = function (commandName, editor, args) {
	var d = Telerik.Web.UI.Editor.CommandList._getLinkArgument(editor);
	Telerik.Web.UI.Editor.CommandList._getDialogArguments(d, "A", editor, "DocumentManager");

	//Retrieve the html selected in the editor
	var html = editor.getSelectionHtml();

	var id;

	// internal link in form of <a href="~/link.aspx?_id=110D559FDEA542EA9C1C8A5DF7E70EF9">...</a>
	// Snippet link: <a href="~/link.aspx?_id=110D559FDEA542EA9C1C8A5DF7E70EF9" type="snippet">...</snippet>
	// Dictionary link: <a href="~/link.aspx?_id=110D559FDEA542EA9C1C8A5DF7E70EF9" type="dictionary">...</snippet>
	if (html) {
		id = GetMediaID(html);
	}

	// link to media in form of <a href="~/media/CC2393E7CA004EADB4A155BE4761086B.ashx">...</a>
	if (!id) {
		var regex = /~\/media\/([\w\d]+)\.ashx/;
		var match = regex.exec(html);
		if (match && match.length >= 1 && match[1]) {
		  id = match[1];
		}
	}

	if (!id) {
		id = scItemID;
	}
	scEditor = editor;
  
  //"/sitecore/shell/default.aspx?xmlcontrol=RichText.InsertSnippet&la=" + scLanguage,
  
 //Call your custom dialog box
	editor.showExternalDialog(
	"/sitecore/shell/default.aspx?xmlcontrol=RichText.SniperButton&la=" + scLanguage + (id ? "&fo=" + id : "") + (scDatabase ? "&databasename=" + scDatabase : "") ,
	  null, //argument
	  1100, //Height
	  500, //Width
	  scInsertSniperLink, //callback
	  null, // callback args
	  "SnipperButton",
	  true, //modal
	  Telerik.Web.UI.WindowBehaviors.Close, // behaviors
	  false, //showStatusBar
	  false //showTitleBar
   );
};

function scInsertSniperLink(sender, returnValue) {
  if (!returnValue) {
	return;
  }

  var d = scEditor.getSelection().getParentElement();

  if ($telerik.isFirefox && d.tagName == "A") {
	d.parentNode.removeChild(d);
  } else {
	scEditor.fire("Unlink");
  }

  var text = scEditor.getSelectionHtml();
  
  if ($telerik.isIE) {
	text = scIEFixRTETextRange(scEditor);
  }
  if (text == "" || text == null || ((text != null) && (text.length == 15) && (text.substring(2, 15).toLowerCase() == "<p>&nbsp;</p>"))) {
	text = returnValue.text;
  }
  else {
	// if selected string is a full paragraph, we want to insert the link inside the paragraph, and not the other way around.
	var regex = /^[\s]*<p>(.+)<\/p>[\s]*$/i;
	var match = regex.exec(text);
	if (match && match.length >= 2) {
	  //scEditor.pasteHtml("<p><a type=\"snippet\" href=\"" + returnValue.url + "\">[" + match[1] + "]</a></p>", "DocumentManager");
	  scEditor.pasteHtml("<p><a href=\"" + returnValue.url + "\">[" + match[1] + "]</a></p>", "DocumentManager");
	  return;
	}
  }
  //scEditor.pasteHtml("<a type=\"snippet\" href=\"" + returnValue.url + "\">[" + text + "]</a>", "DocumentManager");
  scEditor.pasteHtml("<a href=\"" + returnValue.url + "\">[" + text + "]</a>", "DocumentManager");
}