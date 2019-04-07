using HtmlAgilityPack;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.IO;



/* What this program does: it makes api calls to zillow and parses the house's information. 
 * ========================================================================================
 * Because some houses' profile pageare different (found 2 different profile templates) i needed 2 different 
 * parsing methods: one for each template. 
 *  
 *  The parsing methods that parse newest profile template have the word Updated to the end of it.
 *  for example: getZestimateUpdated for newest template and getZestimate for the older template.
 * 
 * 
 *what error code means:ERRORCODE
         *  if houseAddress is error code 101 then something went wrong in grabbing data from api call
         * if zestimate,area,rooms,bath have -101 then the parsing is not working (most likely b/c the indexes are off) 
         * if error code is 500 then the api requests were retried but failed after trying for 60 secs.
         * 
*/

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
            //fetchInfo();
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

        public async void fetchInfo()
        {
            try
            {
                string ApiKey = "fae228e0fcd20c4676bf1ea0cc2a1514";
                string scraperLink = "http://api.scraperapi.com?api_key=" + ApiKey + "&url=" + getZillowURL();

                HttpClient scraperAPI = new HttpClient();
                HttpResponseMessage zillowInfo = await scraperAPI.GetAsync(scraperLink);

                int statusCode = (int)zillowInfo.StatusCode;
                if (statusCode == 200)
                {
                    string StrZillowInfo = await zillowInfo.Content.ReadAsStringAsync();
                    parseHTML(StrZillowInfo);
                }
                else PrintError(statusCode);
            }catch(Exception e){
                Console.WriteLine("Error in grabbing data from the web");
                Console.WriteLine(e);
                this.houseAddress = "error code 101"; //if any houseAddress is that then something went wrong

            }
            
        }

        private void PrintError(int errorCode)
        {
          Console.WriteLine("Error occured!!! Error Code: "+ errorCode);
          switch(errorCode)
           {
                //if the api calls fails try again
             case 500:
               Console.WriteLine("cannot get data from api call. Request failed despite retring for 60 seconds");
                    retryAPI();
               break;
             case 429:
               Console.WriteLine("Exceeding api request calls in our plan");
               break;
          }
        }

        //if the API Call fails the first time keep retrying it up to 5 times or
        private void retryAPI()
        {
            string ApiKey = "fae228e0fcd20c4676bf1ea0cc2a1514";
            string scraperLink = "http://api.scraperapi.com?api_key=" + ApiKey + "&url=" + getZillowURL();
            HttpClient scraperAPI = new HttpClient();
            int statusCode = 0;
            int count = 0;
            try
            {
                do
                {
                    HttpResponseMessage zillowInfo = await scraperAPI.GetAsync(scraperLink);
                    statusCode = (int)zillowInfo.StatusCode;
                    if (statusCode == 200)
                    {
                        string StrZillowInfo = await zillowInfo.Content.ReadAsStringAsync();
                        parseHTML(StrZillowInfo);
                    }
                    else count++;
                    Console.WriteLine("calling api again! count is: " + count);
                } while (statusCode != 200 || count <5); //retry calling api upto 5 times or when we sucessfully get data
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in grabbing data from the web");
                Console.WriteLine(e);
                this.houseAddress = "error code 101"; //if any houseAddress is that then something went wrong

            }
        }
        private void parseHTML(String StrZillowInfo)
        {
            //beds,bath and area info. are close to each other and to prevent 3 un-necessary searches to whole DOM
            //which is 10k characters long, we are going to trim it
            int hInfoStartIndex = StrZillowInfo.IndexOf("middle-dot"); //houseInfoStartIndex 
            int hInfoEndIndex = StrZillowInfo.IndexOf("</h3>");//houseInfoEndIndex
            
            //either zillow coudlnt find that house or 
            //it has the updated page(some pages in zillow are updated) and old parsing might not work
            if(hInfoStartIndex == -1)
            {
              Console.WriteLine("grabbed from updated profile page:\n");
              houseAddress = getHouseAddress(StrZillowInfo);
              zestimate = getZestimateUpdated(StrZillowInfo);
              numberOfBeds = getNumberOfBedsUpdated(StrZillowInfo);
              numberOfBaths = getNumberOfBathsUpdated(StrZillowInfo);
              areaInSqFt = getAreaUpdated(StrZillowInfo);
              
             Console.WriteLine(houseAddress + "\n BED: "+numberOfBeds+"\n BATH: "+numberOfBaths+ "\n Area: "+areaInSqFt+ "\n Zestimate: "+zestimate);
            }
            //old zillow formated house profile
            else{
             //StrZillowInfo contained almost 10k characters, this will contain less than 5% thus removing searching in un-necessary area of the document
            string hInfoReferenceStr = StrZillowInfo.Substring(hInfoStartIndex, hInfoEndIndex - hInfoStartIndex);
            houseAddress = getHouseAddress(StrZillowInfo);
            numberOfBeds = getNumberOfBeds(hInfoReferenceStr);
            numberOfBaths = getNumberOfBaths(hInfoReferenceStr);
            areaInSqFt = getAreaInfo(hInfoReferenceStr);
            zestimate = getZestimate(StrZillowInfo);
            Console.WriteLine("Address: " + houseAddress);
            Console.WriteLine("beds: " + numberOfBeds);
            Console.WriteLine("baths: " + numberOfBaths);
            Console.WriteLine("Area: " + areaInSqFt);
            Console.WriteLine("Zestimate: " + zestimate);
            }
        }
        

        //houseText format: <title>1528 Hutchison Valley Dr, Woodland, CA 95776 | Zillow</title>
        //return address without the tags
        static string getHouseAddress(string HTMLCode)
        {
            try
            {
                int AddressTagLocation = HTMLCode.IndexOf("<title>");
                int endTagIndex = HTMLCode.IndexOf('|', AddressTagLocation); //stop reading when we see | after addresstag
                int NumCharToRead = endTagIndex - AddressTagLocation - 7; //7 b/c <title> is 7 char. long
                string address = HTMLCode.Substring(AddressTagLocation + 7, NumCharToRead);

                //some homes contain that in title so if they do skip over it
                if (address.Contains("Real Estate &amp"))
                {
                    AddressTagLocation = HTMLCode.IndexOf("content=\"", AddressTagLocation) + 9;
                    endTagIndex = HTMLCode.IndexOf("is ", AddressTagLocation);
                    address = HTMLCode.Substring(AddressTagLocation, endTagIndex - AddressTagLocation);
                }
                return address;
            }catch(Exception e)
            {
                Console.WriteLine("Error occured in getHouseAddress");
                Console.WriteLine(e);
                return "error -101";
            }
             
        }

        static int getZestimate(string zestimateText)
        {
            try
            {
                int zestimation; //var to store zestimate value
                string zestimateTag = "zestimate primary-quote\"";
                int zestimateTagIndex = zestimateText.IndexOf(zestimateTag); //contains where useful info. is located

                //find index of $ symbol, and find index of </div>
                int startIndex = zestimateText.IndexOf("$", zestimateTagIndex); //find $ after primaryquote thing
                int endOfZestimate = zestimateText.IndexOf("</div>", startIndex);
                zestimateText = zestimateText.Substring(startIndex + 1, endOfZestimate - startIndex - 1); //dollarsing+1 b/c we care about number after $ 
                zestimateText = zestimateText.Replace(",", "");
                bool gotZestimation = Int32.TryParse(zestimateText, out zestimation);
                if (gotZestimation == false) Console.Write(zestimateText);
                return gotZestimation ? zestimation : -1;
            }catch(Exception e)
            {
                Console.WriteLine("Error occured in getZestimate");
                return -101;
            }
            
        }
        static int getZestimateUpdated(string zestimateText)
        {
            try
            {
                int zestimateReference = zestimateText.IndexOf("ds-price");
                zestimateReference = zestimateText.IndexOf("Zestimate", zestimateReference); //find index of dollar sign after ds price class
                zestimateReference = zestimateText.IndexOf("$", zestimateReference);
                int endZestimateIndex = zestimateText.IndexOf("<", zestimateReference); //find < after the dollarsign
                string zestimateStr = zestimateText.Substring(zestimateReference + 1, endZestimateIndex - zestimateReference - 1);
                zestimateStr = zestimateStr.Replace(",", "");
                int zestimate;
                bool convertedZestimate = Int32.TryParse(zestimateStr, out zestimate);
                return convertedZestimate ? zestimate : -2;
            }catch(Exception e)
            {
                Console.WriteLine("Error occured in getZestimateUpdated");
                return -101;
            }
            
        }
        static int getAreaInfo(string tHTMLDOM) //areaText format: <span>2,247 sqft</span>
        {
            try
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
            }catch(Exception e)
            {
                Console.WriteLine("Error occured in getAreaInfo");
                return -101;
            }
            
        }
        static int getAreaUpdated(string HTMLDOM){
            try
            {
                string usefulSelector = "\"ds-bed-bath-living-area\"><span>";
                int startIndex = HTMLDOM.IndexOf(usefulSelector) + 32;
                int endIndex = HTMLDOM.IndexOf("<", startIndex);
                string areaStr = HTMLDOM.Substring(startIndex, endIndex - startIndex);
                areaStr = areaStr.Replace(",", "");
                int area;
                bool convertedSucessfully = Int32.TryParse(areaStr, out area);
                return convertedSucessfully ? area : -1;
            }catch(Exception e)
            {
                Console.WriteLine("Error occured in getAreaUpdated");
                return -101;
            }
            
        }
        static float getNumberOfBeds(string tHTMLDOM) //bed text has form like: <span>4 beds</span>
        {
            try
            {
                float numOfBeds;
                int startIndex = tHTMLDOM.IndexOf("<span>") + 6; //+6 b/c it returns begining point of '<' tag and we dont wanan read <span>
                int endIndex = tHTMLDOM.IndexOf(" beds");
                tHTMLDOM = tHTMLDOM.Substring(startIndex, endIndex - startIndex);
                bool gotNumOfBeds = float.TryParse(tHTMLDOM, out numOfBeds);
                return gotNumOfBeds ? numOfBeds : -1; //if gotNumOfBeds true then return numOfBeds else -1
            }catch (Exception e)
            {
                Console.WriteLine("Error occured in getNumberOfBeds");
                return -101;
            }

        }

        static float getNumberOfBedsUpdated(string HTMLDOM){
            try
            {
                float numOfBeds;
                string usefulSelector = "ds-vertical-divider ds-bed-bath-living-area\"><span>";
                int startIndex = HTMLDOM.IndexOf(usefulSelector) + 51;
                int endIndex = HTMLDOM.IndexOf("<", startIndex);
                string bedStr = HTMLDOM.Substring(startIndex, endIndex - startIndex);
                bool convertedToFloat = float.TryParse(bedStr, out numOfBeds);
                return convertedToFloat ? numOfBeds : (float)-2.2;
            }catch (Exception e)
            {
                Console.WriteLine("Error occured in getNumberOfBedsUpdated");
                return -101;
            }

        }

        static float getNumberOfBaths(string tHTMLDOM) //bed text has form like: <span>4 beds</span>
        {
            try
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
            }catch (Exception e)
            {
                Console.WriteLine("Error occured in getNumberOfBaths");
                return -101;
            }

        }
        static float getNumberOfBathsUpdated(string HTMLDOM){
            try
            {
                float numOfBaths;
                string usefulSelector = "ds-vertical-divider ds-bed-bath-living-area\"><span>";
                int startIndex = HTMLDOM.IndexOf(usefulSelector) + 51;
                startIndex = HTMLDOM.IndexOf(usefulSelector, startIndex) + 51; //2nd occurance is for baths
                int endIndex = HTMLDOM.IndexOf("<", startIndex);
                string bathStr = HTMLDOM.Substring(startIndex, endIndex - startIndex);
                bool convertedToFloat = float.TryParse(bathStr, out numOfBaths);
                return convertedToFloat ? numOfBaths : (float)-2.2;
            }catch(Exception e)
            {
                Console.WriteLine("Error occured in getNumberOfBathsUpdated");
                return -101;
            }
            
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
