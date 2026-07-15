using System;
using NetTopologySuite.Geometries;

namespace Infrastructure.Persistence.Seeders.Fakers;

public class GeoLocationGenerator
{
    private readonly Random _random;

    private static readonly District[] CairoDistricts =
    [
        new("Nasr City", 30.0626, 31.3283),
        new("Heliopolis", 30.0911, 31.3218),
        new("Maadi", 29.9628, 31.2618),
        new("New Cairo", 30.0249, 31.4720),
        new("Shubra", 30.0713, 31.2427),
        new("Downtown", 30.0465, 31.2366)
    ];

    private static readonly District[] TantaDistricts =
    [
        new("El Bahr Street", 30.7885, 31.0016),
        new("Saeed Street", 30.7930, 31.0050),
        new("Stadium Area", 30.8010, 30.9980),
        new("El Galaa", 30.7800, 30.9900),
        new("El Geish Street", 30.7850, 30.9950),
        new("Kafr Essam", 30.7950, 30.9850),
        new("Sibirbay", 30.8100, 31.0000),
        new("Tanta University Area", 30.7960, 30.9960)
    ];

    public GeoLocationGenerator(int seed = 1337)
    {
        _random = new Random(seed);
    }

    public LocationResult GenerateLocation()
    {
        bool isCairo = _random.NextDouble() < 0.6;
        var governorate = isCairo ? "Cairo" : "Gharbia";
        var city = isCairo ? "Cairo" : "Tanta";
        var districts = isCairo ? CairoDistricts : TantaDistricts;

        var district = districts[_random.Next(districts.Length)];

        double offsetMultiplier;
        double roll = _random.NextDouble();
        if (roll < 0.4) 
            offsetMultiplier = GetRandomDouble(0.002, 0.005);
        else if (roll < 0.8) 
            offsetMultiplier = GetRandomDouble(0.02, 0.05);
        else 
            offsetMultiplier = GetRandomDouble(0.1, 0.2);

        double angle = _random.NextDouble() * 2 * Math.PI;
        double latOffset = Math.Sin(angle) * offsetMultiplier;
        double lngOffset = Math.Cos(angle) * offsetMultiplier;

        double finalLat = district.Latitude + latOffset;
        double finalLng = district.Longitude + lngOffset;

        return new LocationResult(
            governorate,
            city,
            district.Name,
            new Point(finalLng, finalLat) { SRID = 4326 }
        );
    }

    private double GetRandomDouble(double min, double max)
    {
        return min + (_random.NextDouble() * (max - min));
    }
}

public record District(string Name, double Latitude, double Longitude);
public record LocationResult(string Governorate, string City, string District, Point Point);
