﻿
using System;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            //assume house info is given like this...
            int houseNumber = 164;
            string streetname = "Ardmore Way";
            string city = "Benicia";
            string state = "CA";
            int zip = 94510;

            List<House> housesList = new List<House>();

            Stopwatch timer = new Stopwatch(); //to see how long it takes to grab data
            timer.Start();

            //add houses to the houses
            housesList.Add(new House(houseNumber, streetname, city, state, zip));
            //housesList.Add(new House(438, "Greenbrier Ct", "Benicia", "ca", 94510));
            //housesList.Add(new House(414, "12th Ave", "San Francisco", "ca", 94118));
            //housesList.Add(new House(730, "Funston Ave", "San Francisco", "ca", 94118));
            housesList.Add(new House(233, "Del Loma Ct", "Fairfield", "ca", 94533));
            housesList.Add(new House(123, "Tustin Ct", "Benicia", "ca", 94510));

            int numOfHouses = housesList.Count;
            Task[] GrabHouseInfo = new Task[numOfHouses];

            //call the api which grabs info from zillow
            for (int i = 0; i < housesList.Count; i++)
            {
                GrabHouseInfo[i] = housesList[i].fetchInfoAsync();
            }

            //wait for all houses info to arrive
            for (int i = 0; i < housesList.Count; i++)
            {
                GrabHouseInfo[i].Wait();
            }
            timer.Stop();
            TimeSpan ts = timer.Elapsed;
            Console.WriteLine("min: " + ts.Minutes + "sec: " + ts.Seconds);

            //print all houses info
            for (int i = 0; i < housesList.Count; i++)
            {
                housesList[i].printInfo();
            }


            Console.WriteLine("AVG HOUSE INFO: ");
            Console.WriteLine("=======================================================================");
            double avgZestimate = Analytics.getAverageZestimate(housesList);
            double avgBath = Analytics.getAverageBathrooms(housesList);
            double avgBeds = Analytics.getAverageBedrooms(housesList);
            double avgSqFT = Analytics.getAverageSquareFootage(housesList);

            Console.WriteLine("Avg zestiamte: " + avgZestimate);
            Console.WriteLine("Avg bath: " + avgBath);
            Console.WriteLine("Avg beds: " + avgBeds);
            Console.WriteLine("Avg sqft: " + avgSqFT);

            Console.Read();
        }
    }
}
