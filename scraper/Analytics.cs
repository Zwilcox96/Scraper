using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scraper
{
    class Analytics
    {

        public static double getAverageZestimate(List<House> houses)
        {
            var housePrice = from house in houses
                             select house.zestimate;
            return housePrice.DefaultIfEmpty(0).Average();
        }

        public static double getAverageSquareFootage(List<House> houses)
        {
            var houseArea = from house in houses
                            select house.areaInSqFt;
            return houseArea.DefaultIfEmpty(0).Average();
        }

        public static double getAverageBedrooms(List<House> houses)
        {
            var houseBedrooms = from house in houses
                                select house.numberOfBeds;
            return houseBedrooms.DefaultIfEmpty(0).Average();
        }

        public static double getAverageBathrooms(List<House> houses)
        {
            var houseBathrooms = from house in houses
                                 select house.numberOfBaths;
            return houseBathrooms.DefaultIfEmpty(0).Average();
        }

        public static List<House> filterHouses(List<House> houses, int minPrice, int maxPrice, int minArea, int maxArea, int minBedrooms, int maxBedrooms, int minBathrooms, int maxBathrooms)
        {
            var filteredHouses = from house in houses
                                 where house.zestimate >= minPrice && house.zestimate <= maxPrice
                                 && house.areaInSqFt >= minArea && house.areaInSqFt <= maxArea
                                 && Convert.ToInt32(house.numberOfBeds) >= minBedrooms && Convert.ToInt32(house.numberOfBeds) <= maxBedrooms
                                 && Convert.ToInt32(house.numberOfBaths) >= minBathrooms && Convert.ToInt32(house.numberOfBaths) <= maxBathrooms
                                 select house;
            return filteredHouses.ToList();
        }
    }
}
