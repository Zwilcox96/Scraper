using HtmlAgilityPack;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.IO;
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

        public async Task fetchInfoAsync()
        {
            string ApiKey = "fae228e0fcd20c4676bf1ea0cc2a1514";
            string scraperLink = "http://api.scraperapi.com?api_key=" + ApiKey + "&url=" + getZillowURL();
            HttpClient scraperAPI = new HttpClient();
            HttpResponseMessage zillowInfo = await scraperAPI.GetAsync(scraperLink);
            int statusCode = (int)zillowInfo.StatusCode;
            //if we sucessfully got house info then read it and parse it(WHICH IS 98 PERCENT OF  THE TIME, according to the scraper)
            if (statusCode == 200)
            {
                string StrZillowInfo = await zillowInfo.Content.ReadAsStringAsync();
                parseHTML(StrZillowInfo);
            }
            else if (statusCode == 500) //if the api call fails the first time which is 2% OF THE TIME
            {
                var retrying = retryApi(statusCode, scraperLink); //retryAPI asks upto 10 times until it sucessfully gets the data
                retrying.Wait();
            }
            else //if status code is not 500 or 200 in the frist call, then we reached our api limit or concurrent limit
            {
              APIError(statusCode);
            }

        }

        private async Task retryApi(int statusCode, string scraperLink)
        {
            HttpClient scraperAPI = new HttpClient();
            HttpResponseMessage zillowInfo = await scraperAPI.GetAsync(scraperLink);

            for (int i = 0; statusCode != 200 && i < 10; i++) //call api until you sucessfully get info. from zillow or until you have tried 10 times
            {
                Console.WriteLine("api failed trying again!");
                zillowInfo = await scraperAPI.GetAsync(scraperLink); //grab data from api
                statusCode = (int)zillowInfo.StatusCode; //see if we sucessfully got it
                if (statusCode == 200)
                {
                    Console.WriteLine("api worked!");
                    string StrZillowInfo = await zillowInfo.Content.ReadAsStringAsync();
                    parseHTML(StrZillowInfo);
                }
            }
            //if we dont sucessfully get info from zillow after 10 api calls then assign negative value to baths,beds, zestimate to inform user
            if (statusCode != 200)
            {
                updateAPIErrorCode(-401); //assingn 401 as error msg to inform user that api calls kept on failing 10x in row
            }
        }
        //if error occurs 
        private void APIError(int errorCode)
        {
          Console.WriteLine("Error occured!!!");
          switch(errorCode)
           {
             case 429:
               Console.WriteLine("Exceeding concurrent api request calls in our plan");
               updateAPIErrorCode(-429);
               break;
             case 403:
               Console.WriteLine("Exceeding # of api request calls in our plan");
               updateAPIErrorCode(-403);
               break;
            }
        }

        //this method is only called if the scraperApi fails, and sets
        //all fields to either -403, -429
        private void updateAPIErrorCode(int err_code)
        {
            if (err_code == 429 || err_code == 403 || err_code == -401)
            {
                numberOfBaths = err_code;
                numberOfBeds = err_code;
                zestimate = err_code;
                areaInSqFt = err_code;
                houseAddress = "error code " + err_code;
            }
        }
        private void parseHTML(string StrZillowInfo)
        {
            //OLD HOUSE PROFILE HAS middle-dot as classname
            int hInfoStartIndex = StrZillowInfo.IndexOf("middle-dot"); //houseInfoStartIndex 
            int hInfoEndIndex = StrZillowInfo.IndexOf("</h3>");//houseInfoEndIndex
             
            //if middle-dot cant be found then the house's info. is organized in different template so we need to try different parsing
            if(hInfoStartIndex == -1)
            {
              Console.WriteLine("grabbed from updated profile page:\n");
              houseAddress = getHouseAddress(StrZillowInfo);
              
               //notice new parsing techniques have Updated at the end
              zestimate = getZestimateUpdated(StrZillowInfo);
              numberOfBeds = getNumberOfBedsUpdated(StrZillowInfo);
              numberOfBaths = getNumberOfBathsUpdated(StrZillowInfo);
              areaInSqFt = getAreaUpdated(StrZillowInfo);
            }
            //When hInfoStartIndex can be found, house's info. is organized in template that has middle-dot, so this parsing will work 
            else{
             //StrZillowInfo contained almost 10k characters, this will contain less than 5% thus removing searching in un-necessary area of the document and make it bit faster
            string hInfoReferenceStr = StrZillowInfo.Substring(hInfoStartIndex, hInfoEndIndex - hInfoStartIndex);
            houseAddress = getHouseAddress(StrZillowInfo);
            numberOfBeds = getNumberOfBeds(hInfoReferenceStr);
            numberOfBaths = getNumberOfBaths(hInfoReferenceStr);
            areaInSqFt = getAreaInfo(hInfoReferenceStr);
            zestimate = getZestimate(StrZillowInfo);
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
                Console.WriteLine("Error occured in grabbing houseAddress");
                return "error 101";
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
                Console.WriteLine("could not get zestimate!");
                Console.WriteLine(zestimateText);
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
                Console.WriteLine("Couldnot get getZestimateUpdated");
                Console.WriteLine(zestimateText);
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
                Console.WriteLine("Couldnot get getAreaInfo");
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
                Console.WriteLine("Couldnot get getAreaUpdated");
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
            }catch(Exception e)
            {
                Console.WriteLine("Couldnot get getNumberOfBeds");
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
            }catch(Exception e)
            {
                Console.WriteLine("Couldnot get getNumberOfBeds");
                return -101;
            }
            
        }

        static float getNumberOfBaths(string tHTMLDOM) //bed text has form like: <span>4 beds</span>
        {
            try
            {
                float numberOfBaths;
                int endIndex = tHTMLDOM.IndexOf(" bath");
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
                Console.WriteLine("Couldnot get getNumberOfBaths");
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

            }catch (Exception e)
            {
                Console.WriteLine("Couldnot get getNumberOfBaths");
                return -101;
            }

        }
       public void printInfo()
        {
            Console.WriteLine("Address: " + houseAddress);
            Console.WriteLine("beds: " + numberOfBeds);
            Console.WriteLine("baths: " + numberOfBaths);
            Console.WriteLine("Area: " + areaInSqFt);
            Console.WriteLine("Zestimate: " + zestimate);
        }
    }
}
