using System.Text.Json;
using Domain.Entities;
using NetTopologySuite.Geometries;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seeders;

public class OsmPharmacySeeder(
    AppDbContext context,
    IHttpClientFactory httpClientFactory,
    ILogger<OsmPharmacySeeder> logger)
{
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";
    private const string OverpassQuery = @"
        [out:json][timeout:250];
        area[""ISO3166-1""=""EG""][""admin_level""=""2""]->.searchArea;
        nwr[""amenity""=""pharmacy""](area.searchArea);
        out center;
    ";

    public async Task SeedAsync()
    {
        if (context.Pharmacies.Any())
        {
            logger.LogDebug("Pharmacies already exist in the database. Skipping OSM Seeder.");
            return;
        }

        logger.LogInformation("Starting OpenStreetMap Pharmacy Seeder for Egypt...");
        
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("User-Agent", "PharmaLink-GraduationProject/1.0");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", OverpassQuery)
            });
            var response = await client.PostAsync(OverpassUrl, content);
            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(jsonStream);

            var elements = doc.RootElement.GetProperty("elements");

            var branchesByPharmacyName = new Dictionary<string, List<PharmacyBranch>>();
            int totalProcessed = 0;

            foreach (var element in elements.EnumerateArray())
            {
                if (element.TryGetProperty("tags", out var tags))
                {
                    double lat = 0, lon = 0;
                    bool hasGeo = false;

                    if (element.TryGetProperty("lat", out var latProp) && element.TryGetProperty("lon", out var lonProp))
                    {
                        lat = latProp.GetDouble();
                        lon = lonProp.GetDouble();
                        hasGeo = true;
                    }
                    else if (element.TryGetProperty("center", out var centerProp) && 
                             centerProp.TryGetProperty("lat", out var centerLatProp) && 
                             centerProp.TryGetProperty("lon", out var centerLonProp))
                    {
                        lat = centerLatProp.GetDouble();
                        lon = centerLonProp.GetDouble();
                        hasGeo = true;
                    }

                    if (!hasGeo) continue;

                    var name = "صيدلية غير معروفة";
                    if (tags.TryGetProperty("name:ar", out var nameArProp))
                        name = nameArProp.GetString() ?? name;
                    else if (tags.TryGetProperty("name", out var nameProp))
                        name = nameProp.GetString() ?? name;
                    else if (tags.TryGetProperty("name:en", out var nameEnProp))
                        name = nameEnProp.GetString() ?? name;

                    if (name == "صيدلية غير معروفة") 
                        continue;

                    if (!branchesByPharmacyName.TryGetValue(name, out var branches))
                    {
                        branches = new List<PharmacyBranch>();
                        branchesByPharmacyName[name] = branches;
                    }

                    var city = "";
                    if (tags.TryGetProperty("addr:city", out var cityProp))
                        city = cityProp.GetString() ?? "";

                    var street = "";
                    if (tags.TryGetProperty("addr:street", out var streetProp))
                        street = streetProp.GetString() ?? "";

                    var phone = "";
                    if (tags.TryGetProperty("contact:phone", out var cPhoneProp))
                        phone = cPhoneProp.GetString() ?? "";
                    else if (tags.TryGetProperty("phone", out var phoneProp))
                        phone = phoneProp.GetString() ?? "";

                    var website = "";
                    if (tags.TryGetProperty("contact:website", out var cWebProp))
                        website = cWebProp.GetString() ?? "";
                    else if (tags.TryGetProperty("website", out var webProp))
                        website = webProp.GetString() ?? "";

                    var openingHours = "";
                    if (tags.TryGetProperty("opening_hours", out var hoursProp))
                        openingHours = hoursProp.GetString() ?? "";

                    var governorate = "Egypt";
                    if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(street))
                    {
                        try
                        {
                            var geoUrl = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={lat}&longitude={lon}&localityLanguage=ar";
                            var geoResponse = await client.GetAsync(geoUrl);
                            if (geoResponse.IsSuccessStatusCode)
                            {
                                var geoJson = await geoResponse.Content.ReadAsStringAsync();
                                var geoDoc = JsonDocument.Parse(geoJson);
                                
                                if (string.IsNullOrWhiteSpace(city) && geoDoc.RootElement.TryGetProperty("city", out var gCityProp))
                                {
                                    var extractedCity = gCityProp.GetString();
                                    if (!string.IsNullOrEmpty(extractedCity)) city = extractedCity;
                                }
                                
                                if (geoDoc.RootElement.TryGetProperty("principalSubdivision", out var gGovProp))
                                {
                                    var extractedGov = gGovProp.GetString();
                                    if (!string.IsNullOrEmpty(extractedGov)) governorate = extractedGov;
                                }

                                if (string.IsNullOrWhiteSpace(street) && geoDoc.RootElement.TryGetProperty("locality", out var gLocProp))
                                {
                                    var extractedLoc = gLocProp.GetString();
                                    if (!string.IsNullOrEmpty(extractedLoc)) street = extractedLoc;
                                }
                            }
                        }
                        catch (Exception geox)
                        {
                            logger.LogWarning(geox, "Reverse geocoding failed for lat: {Lat}, lon: {Lon}", lat, lon);
                        }
                    }

                    string finalBranchName = name;
                    if (!string.IsNullOrWhiteSpace(street))
                        finalBranchName += " - " + street;
                    else if (!string.IsNullOrWhiteSpace(city))
                        finalBranchName += " - " + city;
                    else
                        finalBranchName += " - فرع " + (branches.Count + 1);

                    var branch = new PharmacyBranch
                    {
                        BranchId = Guid.NewGuid(),
                        BranchName = finalBranchName,
                        City = city,
                        Governorate = governorate,
                        AddressLine = street,
                        PhoneNumber = phone,
                        GeoLocation = new Point(lon, lat) { SRID = 4326 },
                        ServiceRadiusKm = 5,
                        SupportsDelivery = true,
                        SupportsPickup = true,
                        WorkingSchedule = new List<PharmacyBranchSchedule>()
                    };

                    bool is24Hours = openingHours.Contains("24/7", StringComparison.OrdinalIgnoreCase) || 
                                     new[] { "العزبي", "رشدي", "سيف", "مصر", "الطرشوبي", "19011", "دوائي", "نورماندي", "خليل", "كير" }
                                     .Any(c => name.Contains(c));
                    
                    for (int day = 0; day < 7; day++)
                    {
                        branch.WorkingSchedule.Add(new PharmacyBranchSchedule
                        {
                            Id = Guid.NewGuid(),
                            Day = (DayOfWeek)day,
                            OpenTime = is24Hours ? new TimeOnly(0, 0) : new TimeOnly(9, 0),
                            CloseTime = is24Hours ? new TimeOnly(23, 59) : new TimeOnly(22, 0),
                            IsClosed = false
                        });
                    }

                    branches.Add(branch);
                    totalProcessed++;
                }
            }

            logger.LogInformation("Parsed {Count} pharmacies from OSM. Creating entities...", totalProcessed);

            var pharmaciesToInsert = new List<Pharmacy>();
            
            foreach (var kvp in branchesByPharmacyName)
            {
                string logoUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(kvp.Key)}&background=random&color=fff";
                
                if (kvp.Key.Contains("العزبي")) logoUrl = "https://logo.clearbit.com/elezabypharmacy.com";
                else if (kvp.Key.Contains("رشدي")) logoUrl = "https://logo.clearbit.com/roshdypharmacies.com";
                else if (kvp.Key.Contains("سيف")) logoUrl = "https://logo.clearbit.com/seif-online.com";
                else if (kvp.Key.Contains("مصر")) logoUrl = "https://logo.clearbit.com/misrpharmacies.com";
                else if (kvp.Key.Contains("كير")) logoUrl = "https://logo.clearbit.com/carepharmacy.com";
                else if (kvp.Key.Contains("دوائي")) logoUrl = "https://logo.clearbit.com/dawaey.com";
                else if (kvp.Key.Contains("الطرشوبي")) logoUrl = "https://logo.clearbit.com/tarshoby.com";
                else if (kvp.Key.Contains("نورماندي")) logoUrl = "https://logo.clearbit.com/normandypharmacy.com";

                var pharmacy = new Pharmacy
                {
                    PharmacyId = Guid.NewGuid(),
                    LegalName = kvp.Key,
                    LicenseNumber = "OSM-" + Guid.NewGuid().ToString().Substring(0, 8),
                    VerificationStatus = VerificationStatus.Verified,
                    LogoUrl = logoUrl,
                    Branches = kvp.Value
                };
                
                pharmaciesToInsert.Add(pharmacy);
            }

            const int batchSize = 1000;
            for (int i = 0; i < pharmaciesToInsert.Count; i += batchSize)
            {
                var batch = pharmaciesToInsert.Skip(i).Take(batchSize).ToList();
                context.Pharmacies.AddRange(batch);
                await context.SaveChangesAsync();
                logger.LogInformation("Inserted batch of {BatchSize} pharmacies.", batch.Count);
            }

            logger.LogInformation("Successfully completed OSM Pharmacy Seeding. Inserted {Count} total branches across {PharmaciesCount} unique names.", 
                totalProcessed, pharmaciesToInsert.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding pharmacies from OpenStreetMap.");
        }
    }
}
