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
            var url = "https://www.zillow.com/homes/1528-Hutchison-Valley-Dr,-Woodland,-CA-95776_rb/";
            WebClient client = new WebClient();
            client = SetHeaders(client);
            string html = client.DownloadString(url);
            //Console.WriteLine(html);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var node = htmlDoc.DocumentNode.SelectSingleNode("//head/title");

            //indexing actally starts with 1 for webscraping and node2 grabs element that has # of bed info
            var node2 = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[2]"); 
            //node 4 grabs 4th span element which contains bath info
            var node4 = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[4]");
            //node 6 grabs 6th span element which has the sqaure feet info
            var node6 = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[6]");
            
            //save their tags
            string roomsInfo = node2.OuterHtml;
            string bathInfo = node4.OuterHtml;
            string areatext = node6.OuterHtml;

            int numberOfBaths = getNumberOfBaths(bathInfo);
            int numberOfBeds = getNumberOfBeds(roomsInfo);
            int areaInSqFt = getAreaInfo(areatext);

            Console.WriteLine(areaInSqFt + " is area");
            Console.WriteLine(numberOfBeds + "is number of beds");
            Console.WriteLine(numberOfBaths + "is number of baths");
            Console.Read();
        }
        static int getAreaInfo(string areaText) //areaText format: <span>2,247 sqft</span>
        {
            int spaceIndex = areaText.IndexOf(' '); //index used to determine end of square feet information
            int startIndex = 6;  //starting index is 6 b/c "<span>" takes indexes 0-5
            areaText = areaText.Replace(",", ""); // replace comma with nothing
            areaText = areaText.Substring(startIndex,spaceIndex-startIndex); //read the numerical number portion of string only
            int area = Int32.Parse(areaText);  //convert numerial string to number
            return area;
        }

        static int getNumberOfBeds(string bedText) //bed text has form like: <span>4 beds</span>
        {
          int bIndex = bedText.IndexOf(' '); //determine we are done reading numbers
          int startIndex = 6; 
          bedText = bedText.Substring(startIndex, bIndex-startIndex);
          int bedCount = Int32.Parse(bedText);
          return bedCount;
        }

        static int getNumberOfBaths(string bathText) //bed text has form like: <span>4 beds</span>
        {
            int bIndex = bathText.IndexOf('b'); //determine we are done reading numbers
            int startIndex = 6;  //starting index is 6 b/c <span> takes indexes 0-5
            bathText = bathText.Substring(startIndex, bIndex - 6);
            int bathCount = Int32.Parse(bathText);
            return bathCount;
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
