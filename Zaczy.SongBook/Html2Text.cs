using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook;

public class Html2Text
{
	public Html2Text(string htmlContent)
	{
		_htmlContent = htmlContent;
	}

    private string _htmlContent;

	public string HtmlContent
    {
		get { return _htmlContent; }
		set { _htmlContent = value; }
	}

	public string ConvertToText()
	{
		if (string.IsNullOrEmpty(_htmlContent))
		{
			return string.Empty;
		}
		var text = System.Text.RegularExpressions.Regex.Replace(_htmlContent, "<.*?>", string.Empty);
		return System.Net.WebUtility.HtmlDecode(text);
    }

}
