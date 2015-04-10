using System;
using System.Net;
using HtmlAgilityPack;
using System.Web;
using System.Text;

namespace GetCorporationWikiData
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var baseURL = "http://www.corporationwiki.com";

			var searchURL = "/search/results?term=";

			var maltego = new MaltegoTransformNet.Core.MaltegoResponseGenerator ();

			//Search for company on corporation wiki http://www.corporationwiki.com/search/results?term=
			var fullURL = baseURL + searchURL + HttpUtility.UrlEncode(args[0]);

			using (var client = new WebClient())
			{
				var searchResponse = client.DownloadString(fullURL);
				var doc = new HtmlDocument();
				doc.LoadHtml (searchResponse);
				var companyResults = doc.DocumentNode.SelectNodes("//div[@id='results-details']/table/tbody/tr/td[2]/a['href']");
				foreach (var node in companyResults)
				{
					var partialLink = node.Attributes["href"].Value;
					var companyLink = baseURL + partialLink;

					var companyPage = client.DownloadString (companyLink);

					var companyPageDoc = new HtmlDocument();
					companyPageDoc.LoadHtml(companyPage);

					//Get the company profile
					var profileTextNode = companyPageDoc.DocumentNode.SelectSingleNode("//div[@id='profile-text']");
					if(profileTextNode != null){
						maltego.AddPhraseEntity("CorporationWiki Profile", profileTextNode.InnerText.Trim ());
					}

					var people = profileTextNode.SelectNodes("./div/p");
					if(people != null && people.Count > 0){
						foreach(var person in people){
							var parts = person.InnerText.Trim ().Split ();
							maltego.AddPersonEntity(parts[0] + ' ' + parts[1], parts[0], parts[2], string.Join (" ", parts), null);
						}
					}

					//Get a list of addresses 
					var addresses = companyPageDoc.DocumentNode.SelectNodes ("//span[@itemprop='address']");

					if(addresses != null){
						foreach(var address in addresses){
							var addressParts = address.InnerText.Split (new char[] {'\r','\n'});
							var addressBuilder = new StringBuilder();
							foreach (var part in addressParts){
								if(!string.IsNullOrWhiteSpace(part)){
									addressBuilder.AppendLine(part);
								}
							}

							maltego.AddLocationEntity(addressBuilder.ToString());
						}
					}

					//Find the business documents add note node

				}
			}

			Console.WriteLine (maltego.GetMaltegoMessageText());
		}
	}
}
