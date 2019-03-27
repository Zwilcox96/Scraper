using HtmlAgilityPack;
using System;
using System.Net;


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

            House house = new House(houseNumber, streetname, city, state, zip);
            house.fetchInfo();


            string zillowURL = GenerateURL(houseNumber, streetname, city, state, zip, lastTag); //generate the URL string
            WebClient client = new WebClient();
            client = SetHeaders(client);
            string html = client.DownloadString(zillowURL);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            //find # of neighboring houses and create that many house objs
            //for each house create obj and store that info 
            /*
            Console.WriteLine("For house: " + house.houseAddress);
            Console.WriteLine(house.areaInSqFt + " is area");
            Console.WriteLine(house.numberOfBeds + " is number of beds");
            Console.WriteLine(house.numberOfBaths + " is number of baths");
            Console.WriteLine(house.zestimate + " is the estimated price of home.");
            */
            //find neighboring houses
            FindHousesInfo(html);
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
        static WebClient SetHeaders(WebClient client)
        {
            client.Headers[HttpRequestHeader.Accept] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,q=0.8";
            client.Headers[HttpRequestHeader.AcceptEncoding] = "identity";
            client.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.9";
            client.Headers[HttpRequestHeader.Upgrade] = "1";
            client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            return client;
        }

        //this method finds neighrboring homes surrounding the home that we are currently looking at
        //find where ,"description":" occurs and grab main house's info. based of that.
        static void FindHousesInfo(string htmlDOM)
        {
            int currentIndex = 0; //index to track where we are in HtmlDOM

            //find main house's info:
            string mainHouseInfoStartReference = ",\"description\":\"";
            string mainHouseInfoEndReference = "\"homeTags\":";

            //grab where the useful info exists
            int startIndex = htmlDOM.IndexOf(mainHouseInfoStartReference, currentIndex);
            int endIndex = htmlDOM.IndexOf(mainHouseInfoEndReference, startIndex); //determine index where we stop reading

            int length = endIndex - startIndex;
            string mainInfo = htmlDOM.Substring(startIndex, length);
            Console.WriteLine("main info is:");
            Console.WriteLine(mainInfo);
            //need to work to grab specific info out of it like rooms, beds, baths,zestimate
            //this field is close to all the useful info so we will use this as a reference point 
            string usefulClass = "\"hdpUrl\":\""; 
            int usefulClassLength = usefulClass.Length;
            int usefulClassIndex = 0; //index which holds where the 'usefulClass' is in the string, initally 0

            //the first house info that comes after hdpURl is the one we just grabbed info on so we can skip it.
            //usefulClassIndex = htmlDOM.IndexOf(usefulClass, currentIndex); //find where usefulClass is in DOM
            //currentIndex = usefulClassIndex + 1;
            int neighborsCount = 0; //to track how many neighboring houses 

            //loops for each house as usefulClass occurs before house therefore if 
            //usefulClass exist then there is a neighboring house's info. that we can parse
            do
            {
                usefulClassIndex = htmlDOM.IndexOf(usefulClass, currentIndex); //find where usefulClass is in DOM from currentIndex
                if(usefulClassIndex != -1)
                {
                    //address
                    neighborsCount++; 
                    double NumBeds = GetBeds(htmlDOM, usefulClassIndex);
                    double NumBaths = GetBaths(htmlDOM, usefulClassIndex);
                    int HouseArea = GetArea(htmlDOM, usefulClassIndex);
                    string zestimate = GetZestimate(htmlDOM, usefulClassIndex, usefulClassLength);
                    string Address = GetAddress(htmlDOM, usefulClassIndex);
                    Console.WriteLine(neighborsCount + ". "+ Address + "  Beds:" + NumBeds + " Baths: " + NumBaths + " HouseArea: " + HouseArea + "Zestimate: " + zestimate);
                    currentIndex = usefulClassIndex + 1;
                }
            } while (usefulClassIndex != -1);
            Console.ReadLine();
        }
        //method that parses neigbors home address
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

        //method that parses neighbors area in sqft
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
        //grabs the bath for neighboring home
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
        //grabs bed for neighboring home
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
        //gets zestimate for neighboring home
        static string GetZestimate(string htmlDOM,int usefulClassIndex, int usefulClassLength)
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
