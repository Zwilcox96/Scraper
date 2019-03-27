using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace scraper
{
    class House
    {
        int houseNumber;
        string streetName;
        string city;
        string state;
        int zip;
        string lastTag = "_rb/";
        public HtmlDocument htmlDoc;
        //string zillowURL;

        public string houseAddress { get; private set; }
        public int numberOfBaths { get; private set; }
        public int numberOfBeds { get; private set; }
        public int areaInSqFt { get; private set; }
        public int zestimate { get; private set;}

        public House(int houseNumber, string streetName, string city, string state, int zip)
        {
            this.houseNumber = houseNumber;
            this.streetName = streetName;
            this.city = city;
            this.state = state;
            this.zip = zip;
            //zillowURL = getZillowURL();
        }



        public string getZillowURL()
        {
            string url = "https://www.zillow.com/homes/";
            url = url + houseNumber.ToString() + "-"; //add house number and dash
                                                        //replace spaces(' ') in prefix with '-'
            streetName = streetName.Replace(" ", "-");
            url = url + streetName + ",-" + city + ",-" + state + "-" + zip + lastTag;
            return url;
        }

        public void fetchInfo()
        {
            WebClient client = new WebClient();
            client = SetHeaders(client);
            string html = client.DownloadString(getZillowURL());
            htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            parseHTML(htmlDoc);
        }

        private void parseHTML(HtmlDocument htmlDoc)
        {
            string AddressText = htmlDoc.DocumentNode.SelectSingleNode("//head/title").OuterHtml; //contains address embeded in title tag
            string roomsText = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[2]").OuterHtml; //room info is embeded in this string
            string bathText = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[4]").OuterHtml;
            string areaText = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[6]").OuterHtml;
            string zestimateText = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'zestimate primary-quote')]/div").OuterHtml;
            houseAddress = getHouseAddress(AddressText);
            numberOfBeds = getNumberOfBeds(roomsText);
            numberOfBaths = getNumberOfBaths(bathText);
            areaInSqFt = getAreaInfo(areaText);
            zestimate = getZestimate(zestimateText);
        }

        //houseText format: <title>1528 Hutchison Valley Dr, Woodland, CA 95776 | Zillow</title>
        //return address without the tags
        static string getHouseAddress(string houseText)
        {
            int endTagIndex = houseText.IndexOf('|'); //stop reading when we see |
            int NumCharToRead = endTagIndex - 7;
            string address = houseText.Substring(7, NumCharToRead); //starting @ index 7 read  NumCharToRead many characters
            return address;
        }

        static int getZestimate(string zestimateText)
        {
            //find index of $ symbol, and find index of </div>
            int dollarSign = zestimateText.IndexOf('$');
            int endOfZestimate = zestimateText.LastIndexOf('<');
            int charToRead = endOfZestimate - dollarSign - 1;  //how many characters to read
            zestimateText = zestimateText.Substring(dollarSign + 1, charToRead); //dollarsing+1 b/c we care about number after $ 
            zestimateText = zestimateText.Replace(",", "");
            int zestimation = Int32.Parse(zestimateText);
            return zestimation;
        }
    
        static int getAreaInfo(string areaText) //areaText format: <span>2,247 sqft</span>
        {
            int spaceIndex = areaText.IndexOf(' '); //index used to determine end of square feet information
            int startIndex = 6;  //starting index is 6 b/c "<span>" takes indexes 0-5
            areaText = areaText.Replace(",", ""); // replace comma with nothing
            areaText = areaText.Substring(startIndex, spaceIndex - startIndex); //read the numerical number portion of string only
            int area = Int32.Parse(areaText);  //convert numerial string to number
            return area;
        }

        static int getNumberOfBeds(string bedText) //bed text has form like: <span>4 beds</span>
        {
            int bIndex = bedText.IndexOf(' '); //determine we are done reading numbers
            int startIndex = 6;
            bedText = bedText.Substring(startIndex, bIndex - startIndex);
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
