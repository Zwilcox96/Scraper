using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.Xml;


namespace scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            //assume house info is given like this...
            int houseNumber = 1528;
            string streetname = "Hutchison Valley Dr";  //last term has to be abbrebiated(need to see if map api does that)
            string city = "Woodland";
            string state = "CA";
            int zip = 95776;
            string lastTag = "_rb/";

            string zillowURL = GenerateURL(houseNumber, streetname, city, state, zip, lastTag); //generate the URL string
            WebClient client = new WebClient();
            client = SetHeaders(client);
            string html = client.DownloadString("https://www.zillow.com/homes/2005-Birmingham,-Sacramento,-CA-95691_rb/");
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            
            string AddressText = htmlDoc.DocumentNode.SelectSingleNode("//head/title").OuterHtml; //contains address embeded in title tag
            string roomsText = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[2]").OuterHtml; //room info is embeded in this string
            string bathText = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[4]").OuterHtml;
            string areaText = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[6]").OuterHtml;
            string zestimateText = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'zestimate primary-quote')]/div").OuterHtml;
            string solarFactorText = htmlDoc.DocumentNode.SelectSingleNode("(//div[@class ='home-details-facts-category-group-container'])[4]").OuterHtml;

            Console.WriteLine(solarFactorText);
            //return numerical value of those fields
            string houseAddress = getHouseAddress(AddressText);
            int numberOfBaths = getNumberOfBaths(bathText); 
            int numberOfBeds = getNumberOfBeds(roomsText);
            int areaInSqFt = getAreaInfo(areaText);
            int Zestimate = getZestimate(zestimateText);

            Console.WriteLine("For house: " + houseAddress);
            Console.WriteLine(areaInSqFt + " is area");
            Console.WriteLine(numberOfBeds + " is number of beds");
            Console.WriteLine(numberOfBaths + " is number of baths");
            Console.WriteLine(Zestimate + " is the estimated price of home.");
           

            Console.Read();
        }
        static string GenerateURL(int houseNumber, string street,string city,string state, int zipcode, string lastTag)
        {
            string url = "https://www.zillow.com/homes/";
            url = url + houseNumber.ToString()+"-"; //add house number and dash
            //replace spaces(' ') in prefix with '-'
            street = street.Replace(" ", "-");
            url = url + street + ",-" + city + ",-" + state + "-" + zipcode + lastTag;
            return url;
        }

        //houseText format: <title>1528 Hutchison Valley Dr, Woodland, CA 95776 | Zillow</title>
        //return address without the tags
        static string getHouseAddress(string houseText) 
        {
            int endTagIndex = houseText.IndexOf('|'); //stop reading when we see |
            int NumCharToRead = endTagIndex - 7;
            string address = houseText.Substring(7,NumCharToRead); //starting @ index 7 read  NumCharToRead many characters
            return address;
        }
        static int getZestimate(string zestimateText)
        {
            //find index of $ symbol, and find index of </div>
            int dollarSign = zestimateText.IndexOf('$');
            int endOfZestimate = zestimateText.LastIndexOf('<');
            int charToRead = endOfZestimate - dollarSign-1;  //how many characters to read
            zestimateText = zestimateText.Substring(dollarSign + 1,charToRead); //dollarsing+1 b/c we care about number after $ 
            zestimateText = zestimateText.Replace(",", "");
            int zestimation = Int32.Parse(zestimateText);
            return zestimation;
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
            client.Headers[HttpRequestHeader.AcceptEncoding] = "identity";
            client.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.9";
            client.Headers[HttpRequestHeader.Upgrade] = "1";
            client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            return client;
        }
    }
}
