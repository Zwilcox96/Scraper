using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;

namespace scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = "https://www.zillow.com/homes/5705-Regan-Hall-Ln-Carmichael,-CA,-95608_rb/";
            WebClient client = new WebClient();
            client = SetHeaders(client);
            string html = client.DownloadString(url);
            Console.WriteLine(html);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var node = htmlDoc.DocumentNode.SelectSingleNode("//head/title");
            //var bedrooms = htmlDoc.DocumentNode.Descendants("meta").Select(y => y.Descendants().Where(x => x.Attributes["Property"].Value == "zillow_fb:beds"));
            //foreach(HtmlNode nodeBed in bedrooms)
            //{
            //    Console.WriteLine(nodeBed.Attributes["content"].Value);
            //}
            
            Console.WriteLine("Node Name: " + node.Name + "\n" + node.OuterHtml);
            Console.Read();
        }

        static WebClient SetHeaders(WebClient client)
        {
            client.Headers[HttpRequestHeader.Accept] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            client.Headers[HttpRequestHeader.AcceptEncoding] = "";
            client.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.9";
            client.Headers[HttpRequestHeader.Upgrade] = "1";
            client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.81 Safari/537.36";
            return client;
        }
    }
}
