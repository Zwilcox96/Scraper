
using System;
using System.Net;



namespace scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            //assume house info is given like this...
            int houseNumber = 1555;
            string streetname = "Squaw Valley Drive"; 
            string city = "Woodland";
            string state = "CA";
            int zip = 95776;
            string lastTag = "_rb/";
            
            House house = new House(houseNumber, streetname, city, state, zip);
            house.fetchInfo();
            printHouseInfo(house);
           
            //this code is moved to house classs
            /*
            string zillowURL = GenerateURL(houseNumber, streetname, city, state, zip, lastTag);
            string ApiKey = "fae228e0fcd20c4676bf1ea0cc2a1514";

            //we need to do get request to the scraperapi and thats the link
            string scraperLink = "http://api.scraperapi.com?api_key=" + ApiKey + "&url="+ zillowURL;
            WebRequest wrGETURL = WebRequest.Create(scraperLink);
            Stream outputStream = wrGETURL.GetResponse().GetResponseStream();
            StreamReader zillowInfo = new StreamReader(outputStream);
            string StrZillowInfo = zillowInfo.ReadToEnd();

            //beds,bath and area info. are really close to each other and to prevent 3 un-necessary searches to whole DOM we are going to trim it
            int hInfoStartIndex = StrZillowInfo.IndexOf("middle-dot"); //houseInfoStartIndex which contains index where useful(bed,rooms,sqft info are closely located)
            int hInfoEndIndex = StrZillowInfo.IndexOf("</h3>");//houseInfoEndIndex

            //StrZillowInfo contained almost 10k characters, this will contain less than 5% thus removing searching in un-necessary area of the document
            string hInfoReferenceStr = StrZillowInfo.Substring(hInfoStartIndex, hInfoEndIndex- hInfoStartIndex);
            string AddressText = getHouseAddress(StrZillowInfo);
            Console.WriteLine("Address is: " + AddressText);
            float numberOfBeds = getNumberOfBeds(hInfoReferenceStr);
            Console.WriteLine("Beds is: " + numberOfBeds);
            float numberOfBaths = getNumberOfBaths(hInfoReferenceStr);
            Console.WriteLine("Number of bath is " + numberOfBaths);
            int areaInSqFt = getAreaInfo(hInfoReferenceStr);
            Console.WriteLine(" Area is " + areaInSqFt);
            int zestimate = getZestimate(StrZillowInfo);
            Console.WriteLine("ZESTIMATE: " + zestimate);
            */
            Console.Read();
        }
        static void printHouseInfo(House home)
        {
            Console.WriteLine("Address: " + home.houseAddress);
            Console.WriteLine("beds: " + home.numberOfBeds);
            Console.WriteLine("baths: " + home.numberOfBaths);
            Console.WriteLine("Zestimate: " + home.zestimate);
            Console.WriteLine("Area: " + home.areaInSqFt);

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

        
        //return address without the tags
        static string getHouseAddress(string HTMLCode) 
        {
            int AddressTagLocation = HTMLCode.IndexOf("<title>");
            int endTagIndex = HTMLCode.IndexOf('|', AddressTagLocation); //stop reading when we see | after addresstag
            int NumCharToRead = endTagIndex - AddressTagLocation- 7; //7 b/c <title> is 7 char. long
            string address = HTMLCode.Substring(AddressTagLocation+7, NumCharToRead);
            return address;
        }

        
        static int getZestimate(string zestimateText)
        {
            int zestimation; //var to store zestimate value
            //find index of $ symbol, and find index of </div>
            int startIndex = zestimateText.IndexOf("Home Value: $") +13;
            int endOfZestimate = zestimateText.IndexOf(".", startIndex);
            zestimateText = zestimateText.Substring(startIndex, endOfZestimate- startIndex); //dollarsing+1 b/c we care about number after $ 
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
            return gotArea? area : -1;
        }

        static float getNumberOfBeds(string tHTMLDOM) //trimmed HTMLDOM
        {
            float numOfBeds;
            int startIndex = tHTMLDOM.IndexOf("<span>") + 6; //+6 b/c it returns begining point of '<' tag and we dont wanan read <span>
            int endIndex = tHTMLDOM.IndexOf(" beds");
            tHTMLDOM = tHTMLDOM.Substring(startIndex, endIndex-startIndex);
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
            client.Headers[HttpRequestHeader.Accept] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,q=0.8";
            client.Headers[HttpRequestHeader.AcceptEncoding] = "identity";
            client.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.9";
            client.Headers[HttpRequestHeader.Upgrade] = "1";
            client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            return client;
        }

        //this is to test indexOf method of string class
        static void FindNeighbors(string htmlDOM)
        {
            int currentIndex = 0; //where we are within the arr of data
            //zsg-photo-card-price">$
             string usefulClass = "\"hdpUrl\":\""; //this field is really close to useful info so we will use this as a reference point 
            int usefulClassLength = usefulClass.Length;
            int usefulClassIndex = 0; //index which holds where the 'usefulClass' is in the string

            //the first house info that comes after hdpURl is the main house for which we already grabbed info on so we can skip it.
            usefulClassIndex = htmlDOM.IndexOf(usefulClass, currentIndex); //find where usefulClass is in DOM
            currentIndex = usefulClassIndex + 1;

            //for each neighbor
            do
            {
                usefulClassIndex = htmlDOM.IndexOf(usefulClass, currentIndex); //find where usefulClass is in DOM
                if(usefulClassIndex != -1)
                {
                    float NumBeds = GetNeighborsBeds(htmlDOM, usefulClassIndex);
                    float NumBaths = GetNeighborsBaths(htmlDOM, usefulClassIndex);
                    int HouseArea = GetNeighborsArea(htmlDOM, usefulClassIndex);
                    string zestimate = GetNeighborsZestimate(htmlDOM, usefulClassIndex, usefulClassLength);
                    string Address = GetNeighborsAddress(htmlDOM, usefulClassIndex);
                    Console.WriteLine(Address + "  Beds:" + NumBeds + " Baths: " + NumBaths + " HouseArea: " + HouseArea + "Zestimate: " + zestimate);
                    currentIndex = usefulClassIndex + 1;
                }
            } while (usefulClassIndex != -1);
        }

        static string GetNeighborsAddress(string htmlDOM, int houseInfoIndex)
        {
            string AddressAttribute = "\"streetAddress\":\"";
            int AddressIndex = htmlDOM.IndexOf(AddressAttribute, houseInfoIndex); //find "Address" after "hdpUrl"
            int startIndex = AddressIndex + AddressAttribute.Length;

            int endQuoteIndex = htmlDOM.IndexOf("\"},", startIndex); //"}, seperates Address field from others
            int readUntilComma = endQuoteIndex - startIndex;

            string AddressString = htmlDOM.Substring(startIndex, readUntilComma);
            return AddressString;
        }

        static int GetNeighborsArea (string htmlDOM, int houseInfoIndex)
        {
            string areaAttribute = "\"livingArea\":";
            int areaIndex = htmlDOM.IndexOf(areaAttribute, houseInfoIndex); //find "area" after "hdpUrl"
            int startIndex = areaIndex + areaAttribute.Length;

            int commaIndex = htmlDOM.IndexOf(",", startIndex); //comma seperates area field from others
            int readUntilComma = commaIndex - startIndex;

            string AreaString = htmlDOM.Substring(startIndex, readUntilComma);
            int numareas;
            if (Int32.TryParse(AreaString, out numareas))
            {
                numareas = Int32.Parse(AreaString);
            }
            else
            {
                numareas = 0;
            }
            return numareas;
        }

        static float GetNeighborsBaths(string htmlDOM, int houseInfoIndex) 
        {
            string bathAttribute = "\"bathrooms\":";
            int bathIndex = htmlDOM.IndexOf(bathAttribute, houseInfoIndex); //find "bath" after "hdpUrl"
            int startIndex = bathIndex + bathAttribute.Length;

            int commaIndex = htmlDOM.IndexOf(",", startIndex); //comma seperates bath field from others
            int readUntilComma = commaIndex - startIndex;

            string numBathString = htmlDOM.Substring(startIndex, readUntilComma);
            float numbaths;
            if (float.TryParse(numBathString, out numbaths))
            {
                numbaths = float.Parse(numBathString);
            }
            else
            {
                numbaths = 0;
            }
            return numbaths;
        }

        static float GetNeighborsBeds(string htmlDOM, int houseInfoIndex)
        {
            string bedAttribute = "\"bedrooms\":";
            int bedIndex = htmlDOM.IndexOf(bedAttribute, houseInfoIndex); //find "bed" after "hdpUrl"
            int startIndex = bedIndex + bedAttribute.Length;

            int commaIndex = htmlDOM.IndexOf(",", startIndex); //comma seperates bed field from others
            int readUntilComma = commaIndex - startIndex;

            string numBedString = htmlDOM.Substring(startIndex, readUntilComma);
            float numBeds;
            if(float.TryParse(numBedString,out numBeds))
            {
                numBeds = float.Parse(numBedString);
            }
            else
            {
                numBeds = 0;
            }
            return numBeds;
        }
 
        static string GetNeighborsZestimate(string htmlDOM,int usefulClassIndex, int usefulClassLength)
        {
            string priceAttribute = "\"price\":";
            int priceIndex = htmlDOM.IndexOf(priceAttribute, usefulClassIndex); //find "price" after "hdpUrl"
            int startIndex = priceIndex + priceAttribute.Length;

            int commaIndex = htmlDOM.IndexOf(",", startIndex); //comma seperates price field from others
            int readUntilComma = commaIndex - startIndex;

            string zestimateString = htmlDOM.Substring(startIndex, readUntilComma);
            return zestimateString;
        }

    }

}
