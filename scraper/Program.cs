using HtmlAgilityPack;
using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;

namespace scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            //string infoArr = "hey this is Amit the greatest. Amit is coolest person i know. He rocks at everything.Amit is so cool yar yall dont even know.";
           //FindString(infoArr);
            
            //assume house info is given like this...
            int houseNumber = 1528;
            string streetname = "Hutchison Valley Dr";  //last term has to be abbrebiated(need to see if map api does that)
            string city = "Woodland";
            string state = "CA";
            int zip = 95776;
            string lastTag = "_rb/";
            List<House> houses = new List<House>();

            House house = new House(houseNumber, streetname, city, state, zip);
            house.fetchInfo();

            House house2 = new House(5705, "Regan Hall Ln", "Carmichael", "CA", 95608);
            house2.fetchInfo();
            for(int i = 0; i < 10000; i++)
            {
                houses.Add(house);
                houses.Add(house2);

            }
            

            string zillowURL = GenerateURL(houseNumber, streetname, city, state, zip, lastTag); //generate the URL string
            WebClient client = new WebClient();
            client = SetHeaders(client);
            string html = client.DownloadString(zillowURL);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
          
            string AddressText = htmlDoc.DocumentNode.SelectSingleNode("//head/title").OuterHtml; //contains address embeded in title tag
            string roomsText = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[2]").OuterHtml; //room info is embeded in this string
            string bathText = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[4]").OuterHtml;
            string areaText = htmlDoc.DocumentNode.SelectSingleNode("//h3/span[6]").OuterHtml;
            string zestimateText = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'zestimate primary-quote')]/div").OuterHtml;


            //couldnt do this :( zillow uses script for generating solarNumber and cant get past that
            //string solarFactorText = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'truncated')]").OuterHtml;

            /*
             * AddRemainingHouses()
             * in html file:
             * 
             * find: zsg-photo-card-price
             * grab price by using $ to find recent index (zestimate, some zestimate values maynot be clean...)
             * grab property by searching for: property-beds 
             * grab address by using this: zsg-photo-card-address
             * 
             * 
             */
            
            //Console.WriteLine(solarFactorText);
            //return numerical value of those fields
            
            string houseAddress = getHouseAddress(AddressText);
            int numberOfBaths = getNumberOfBaths(bathText); 
            int numberOfBeds = getNumberOfBeds(roomsText);
            int areaInSqFt = getAreaInfo(areaText);
            int Zestimate = getZestimate(zestimateText);


            //FindString(html);
            //foreach (House curHouse in houses)
            //{
            //    Console.WriteLine("For house: " + curHouse.houseAddress);
            //    Console.WriteLine(curHouse.areaInSqFt + " is area");
            //    Console.WriteLine(curHouse.numberOfBeds + " is number of beds");
            //    Console.WriteLine(curHouse.numberOfBaths + " is number of baths");
            //    Console.WriteLine(curHouse.zestimate + " is the estimated price of home.");
            //}
            Console.WriteLine(Analytics.getAverageSquareFootage(houses) + " is average area");
            Console.WriteLine(Analytics.getAverageBedrooms(houses) + " is number of beds");
            Console.WriteLine(Analytics.getAverageBathrooms(houses) + " is number of baths");
            Console.WriteLine(Analytics.getAverageZestimate(houses) + " is the estimated price of home.");
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
            client.Headers[HttpRequestHeader.Accept] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,q=0.8";
            client.Headers[HttpRequestHeader.AcceptEncoding] = "identity";
            client.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.9";
            client.Headers[HttpRequestHeader.Upgrade] = "1";
            client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            return client;
        }

        //this is to test indexOf method of string class
        static void FindString(string htmlDOM)
        {
            int currentIndex = 0; //where we are within the arr of data
            //zsg-photo-card-price">$
             string usefulClass = "\"hdpUrl\":\""; //this field is really close to useful info so we will use this as a reference point 
            int usefulClassLength = usefulClass.Length;
            int usefulClassIndex = 0; //index which holds where the 'usefulClass' is in the string

            //the first house info that comes after hdpURl is the one we just grabbed info on so we can skip it.
            usefulClassIndex = htmlDOM.IndexOf(usefulClass, currentIndex); //find where usefulClass is in DOM
            currentIndex = usefulClassIndex + 1;
            do
            {
                usefulClassIndex = htmlDOM.IndexOf(usefulClass, currentIndex); //find where usefulClass is in DOM
                if(usefulClassIndex != -1)
                {
                    //address
                    double NumBeds = GetBeds(htmlDOM, usefulClassIndex);
                    double NumBaths = GetBaths(htmlDOM, usefulClassIndex);
                    int HouseArea = GetArea(htmlDOM, usefulClassIndex);
                    string zestimate = GetZestimate(htmlDOM, usefulClassIndex, usefulClassLength);
                    string Address = GetAddress(htmlDOM, usefulClassIndex);
                    Console.WriteLine(Address + "  Beds:" + NumBeds + " Baths: " + NumBaths + " HouseArea: " + HouseArea + "Zestimate: " + zestimate);
                    currentIndex = usefulClassIndex + 1;
                }
            } while (usefulClassIndex != -1);
            
            Console.ReadLine();
        }

        static string GetAddress(string htmlDOM, int houseInfoIndex)
        {
            string AddressAttribute = "\"streetAddress\":\"";
            int AddressIndex = htmlDOM.IndexOf(AddressAttribute, houseInfoIndex); //find "Address" after "hdpUrl"
            int startIndex = AddressIndex + AddressAttribute.Length;

            int endQuoteIndex = htmlDOM.IndexOf("\"},", startIndex); //"}, seperates Address field from others
            int readUntilComma = endQuoteIndex - startIndex;

            string AddressString = htmlDOM.Substring(startIndex, readUntilComma);
            return AddressString;
        }

        static int GetArea (string htmlDOM, int houseInfoIndex)
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

        static double GetBaths(string htmlDOM, int houseInfoIndex) 
        {
            string bathAttribute = "\"bathrooms\":";
            int bathIndex = htmlDOM.IndexOf(bathAttribute, houseInfoIndex); //find "bath" after "hdpUrl"
            int startIndex = bathIndex + bathAttribute.Length;

            int commaIndex = htmlDOM.IndexOf(",", startIndex); //comma seperates bath field from others
            int readUntilComma = commaIndex - startIndex;

            string numBathString = htmlDOM.Substring(startIndex, readUntilComma);
            double numbaths;
            if (Double.TryParse(numBathString, out numbaths))
            {
                numbaths = Double.Parse(numBathString);
            }
            else
            {
                numbaths = 0;
            }
            return numbaths;
        }

        static double GetBeds(string htmlDOM, int houseInfoIndex)
        {
            string bedAttribute = "\"bedrooms\":";
            int bedIndex = htmlDOM.IndexOf(bedAttribute, houseInfoIndex); //find "bed" after "hdpUrl"
            int startIndex = bedIndex + bedAttribute.Length;

            int commaIndex = htmlDOM.IndexOf(",", startIndex); //comma seperates bed field from others
            int readUntilComma = commaIndex - startIndex;

            string numBedString = htmlDOM.Substring(startIndex, readUntilComma);
            double numBeds;
            if(Double.TryParse(numBedString,out numBeds))
            {
                numBeds = Double.Parse(numBedString);
            }
            else
            {
                numBeds = 0;
            }
            return numBeds;
        }
 
        static string GetZestimate(string htmlDOM,int usefulClassIndex, int usefulClassLength)
        {
            string priceAttribute = "\"price\":";
            int priceIndex = htmlDOM.IndexOf(priceAttribute, usefulClassIndex); //find "price" after "hdpUrl"
            int startIndex = priceIndex + priceAttribute.Length;

            int commaIndex = htmlDOM.IndexOf(",", startIndex); //comma seperates price field from others
            int readUntilComma = commaIndex - startIndex;

            string zestimateString = htmlDOM.Substring(startIndex, readUntilComma);
            Console.WriteLine(zestimateString);
            return zestimateString;
        }

    }

}
