using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
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
        //string zillowURL;

        public string houseAddress { get; private set; }
        public float numberOfBaths { get; private set; } //some houses have half baths
        public float numberOfBeds { get; private set; } //making it float for the case that there is a house with half a room
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
            string ApiKey = "fae228e0fcd20c4676bf1ea0cc2a1514";
            //we need to do get request to the scraperapi and thats the link
            string scraperLink = "http://api.scraperapi.com?api_key=" + ApiKey + "&url=" + getZillowURL();
            WebRequest wrGETURL = WebRequest.Create(scraperLink);
            Stream outputStream = wrGETURL.GetResponse().GetResponseStream();
            StreamReader zillowInfo = new StreamReader(outputStream);
            string StrZillowInfo = zillowInfo.ReadToEnd();
            parseHTML(StrZillowInfo);
        }

        private void parseHTML(String StrZillowInfo)
        {
            //beds,bath and area info. are close to each other and to prevent 3 un-necessary searches to whole DOM
            //which is 10k characters long, we are going to trim it
            int hInfoStartIndex = StrZillowInfo.IndexOf("middle-dot"); //houseInfoStartIndex 
            int hInfoEndIndex = StrZillowInfo.IndexOf("</h3>");//houseInfoEndIndex
            //StrZillowInfo contained almost 10k characters, this will contain less than 5% thus removing searching in un-necessary area of the document
            string hInfoReferenceStr = StrZillowInfo.Substring(hInfoStartIndex, hInfoEndIndex - hInfoStartIndex);
            houseAddress = getHouseAddress(StrZillowInfo);
            numberOfBeds = getNumberOfBeds(hInfoReferenceStr);
            numberOfBaths = getNumberOfBaths(hInfoReferenceStr);
            areaInSqFt = getAreaInfo(hInfoReferenceStr);
            zestimate = getZestimate(StrZillowInfo);
        }

        //houseText format: <title>1528 Hutchison Valley Dr, Woodland, CA 95776 | Zillow</title>
        //return address without the tags
        static string getHouseAddress(string HTMLCode)
        {
            int AddressTagLocation = HTMLCode.IndexOf("<title>");
            int endTagIndex = HTMLCode.IndexOf('|', AddressTagLocation); //stop reading when we see | after addresstag
            int NumCharToRead = endTagIndex - AddressTagLocation - 7; //7 b/c <title> is 7 char. long
            string address = HTMLCode.Substring(AddressTagLocation + 7, NumCharToRead);
            return address;
        }

        static int getZestimate(string zestimateText)
        {
            int zestimation; //var to store zestimate value
            //find index of $ symbol, and find index of </div>
            int startIndex = zestimateText.IndexOf("The Zestimate for this house is $") + 33;
            int endOfZestimate = zestimateText.IndexOf(", which", startIndex);
            zestimateText = zestimateText.Substring(startIndex, endOfZestimate - startIndex); //dollarsing+1 b/c we care about number after $ 
            zestimateText = zestimateText.Replace(",", "");
            bool gotZestimation = Int32.TryParse(zestimateText, out zestimation);
            return gotZestimation ? zestimation : -1;
        }
    
        static int getAreaInfo(string tHTMLDOM) //areaText format: <span>2,247 sqft</span>
        {
            int area;
            tHTMLDOM = tHTMLDOM.Replace(",", ""); // replace comma with nothing
            int endIndex = tHTMLDOM.IndexOf("sqft</span>");
            int startIndex = endIndex;
            while (tHTMLDOM[startIndex] != '>')
            {
                startIndex--;
            }
            startIndex++; //startIndex points to > when we want it to point to the next character which is part of house #
            tHTMLDOM = tHTMLDOM.Substring(startIndex, endIndex - startIndex); //read the numerical number portion of string only
            bool gotArea = Int32.TryParse(tHTMLDOM, out area);  //convert numerial string to number
            return gotArea ? area : -1;
        }

        static float getNumberOfBeds(string tHTMLDOM) //bed text has form like: <span>4 beds</span>
        {
            float numOfBeds;
            int startIndex = tHTMLDOM.IndexOf("<span>") + 6; //+6 b/c it returns begining point of '<' tag and we dont wanan read <span>
            int endIndex = tHTMLDOM.IndexOf(" beds");
            tHTMLDOM = tHTMLDOM.Substring(startIndex, endIndex - startIndex);
            bool gotNumOfBeds = float.TryParse(tHTMLDOM, out numOfBeds);
            return gotNumOfBeds ? numOfBeds : -1; //if gotNumOfBeds true then return numOfBeds else -1
        }

        static float getNumberOfBaths(string tHTMLDOM) //bed text has form like: <span>4 beds</span>
        {
            float numberOfBaths;
            int endIndex = tHTMLDOM.IndexOf(" baths");
            int startIndex = endIndex;

            //# of bath comes after <span> tag so keep reading until u come across '>' (we are reading from the end to begining thats why we are looking for '>')
            while (tHTMLDOM[startIndex] != '>')
            {
                startIndex--; //using the string " baths" work backwards in determining # of baths
            }
            startIndex++; //b.c startIndex points to the index of > and the number comes right after that
            tHTMLDOM = tHTMLDOM.Substring(startIndex, endIndex - startIndex);
            bool getBath = float.TryParse(tHTMLDOM, out numberOfBaths); //if sucess then getBath is true
            return getBath ? numberOfBaths : -1;
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
