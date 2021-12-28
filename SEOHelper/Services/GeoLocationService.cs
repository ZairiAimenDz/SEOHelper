using CsvHelper;
using SEOHelper.Models;

namespace SEOHelper.Service
{
    public class GeoLocationService
    {
        public static long[] GetLocationId(string City)
        {
            TextReader reader = new StreamReader("Resources/geodata.csv");
            var csvReader = new CsvReader(reader,System.Globalization.CultureInfo.InvariantCulture);
            var records = csvReader.GetRecords<Location>();
            var loc = records.Where(l => l.CannonicalName.ToLower().Contains(City.ToLower()) 
                || l.CountryCode.ToLower().Contains(City.ToLower())).First();
            return new long[] {long.Parse(loc.CriteriaID),long.Parse(loc.ParentID)};
        }
    }
}
