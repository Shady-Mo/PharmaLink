using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services.OrderSplitting;
using Application.Services.OrderSplitting.Models;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderSplittingTester;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Live DB Pharmacy Order Splitting Test ===");

        var services = new ServiceCollection();
        services.AddHttpClient("OsrmClient");
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddTransient<IOsrmRoutingService, OsrmRoutingService>();
        var serviceProvider = services.BuildServiceProvider();
        var osrmRoutingService = serviceProvider.GetRequiredService<IOsrmRoutingService>();

        var connectionString = "Server=; Database=; User Id=; Password=; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString, x => x.UseNetTopologySuite());
        
        using var dbContext = new AppDbContext(optionsBuilder.Options);

        Console.WriteLine("Connecting to Live Database...");

        var drugNames = new[] { "1+1 اكس | مزيل العرق آيس تشيل 48 ساعة برائحة النعناع والليمون للرجال | 150مل",
                                "1+1 اوميجالوكس اوميجا 3 | 30 كبسولة"};
        var targetDrugs = dbContext.Drugs
            .Where(d => drugNames.Any(name => d.ArabicName.Contains(name) || d.BrandName.Contains(name)))
            .Distinct()
            .ToList();
            
        Console.WriteLine($"\nFound {targetDrugs.Count} target drugs in the live database:");

        var items = new List<PendingItem>();
        foreach (var drug in targetDrugs)
        {
            items.Add(new PendingItem(Guid.NewGuid(), drug.DrugId, 1));
            Console.WriteLine($"- {drug.ArabicName} ({drug.BrandName})");
        }

        if (items.Count == 0)
        {
            Console.WriteLine("Could not find the items in the DB. Test aborted.");
            return;
        }

        var drugIds = targetDrugs.Select(d => d.DrugId).ToList();
        
        var inventories = dbContext.PharmacyInventories
            .Include(i => i.Branch)
            .Where(i => drugIds.Contains(i.DrugId) && i.StockQuantity > 0)
            .ToList();

        var branchesGrouped = inventories.GroupBy(i => i.BranchId).ToList();
        var candidateBranches = new List<CandidateBranch>();
        
        var userLat = 30.12914915195177;
        var userLng = 31.27714977862994; 

        var coords = new List<(double Lat, double Lon)> { (userLat, userLng) };
        var branchIndexMap = new Dictionary<Guid, int>();

        foreach (var group in branchesGrouped)
        {
            var branch = group.First().Branch;
            double branchLat = branch.GeoLocation != null ? branch.GeoLocation.Y : 30.0;
            double branchLng = branch.GeoLocation != null ? branch.GeoLocation.X : 31.0;
            
            branchIndexMap[branch.BranchId] = coords.Count;
            coords.Add((branchLat, branchLng));
        }

        Console.WriteLine("\nCalculating driving distances matrix via OSRM...");
        var matrixResult = await osrmRoutingService.GetDistanceMatrixAsync(coords, CancellationToken.None);
        
        if (!matrixResult.IsSuccess)
        {
            Console.WriteLine("OSRM Matrix calculation failed: " + matrixResult.Message);
            return;
        }

        foreach (var group in branchesGrouped)
        {
            var branch = group.First().Branch;
            var idx = branchIndexMap[branch.BranchId];
            var distanceToPatient = matrixResult.DistancesKm[0][idx];
            
            var stockDict = new Dictionary<Guid, int>();
            foreach (var inv in group)
            {
                stockDict[inv.DrugId] = inv.StockQuantity;
            }

            candidateBranches.Add(new CandidateBranch(
                branch.BranchId, 
                branch.BranchName ?? "Unknown Branch", 
                distanceToPatient, 
                true, 
                true, 
                stockDict));
        }
        
        Console.WriteLine($"\nFound {candidateBranches.Count} candidate branches carrying at least one of these items in Egypt.");

        var context = new SplittingContext(Guid.NewGuid(), FulfillmentMode.Delivery, items, candidateBranches);

        var bruteForceAlgo = new BruteForceOrderSplittingAlgorithm(matrixResult.DistancesKm, branchIndexMap);
        
        Console.WriteLine("Running Brute Force Algorithm (with TSP Distance Matrix)...");
        var bruteForceResult = bruteForceAlgo.Execute(context);

        Console.WriteLine("\n--- Brute Force Results (Absolute Optimal) ---");
        PrintResult(bruteForceResult, candidateBranches);
    }

    static void PrintResult(SplittingResult result, List<CandidateBranch> allBranches)
    {
        var uniqueBranches = new HashSet<Guid>();
        
        if (!result.Assignments.Any())
        {
            Console.WriteLine("NO ASSIGNMENTS FOUND! The cart cannot be fulfilled with current DB stock.");
            return;
        }

        double finalTspDistance = 0;
        foreach (var assignment in result.Assignments)
        {
            var branchName = allBranches.Find(b => b.BranchId == assignment.BranchId)?.BranchName;
            Console.WriteLine($"Item Assigned -> {branchName}");
            uniqueBranches.Add(assignment.BranchId);
            finalTspDistance = assignment.Decision.DistanceKm;
        }
        
        Console.WriteLine($"\nTotal Unique Branches Used: {uniqueBranches.Count}");
        Console.WriteLine($"Total TSP Driving Distance: {Math.Round(finalTspDistance, 2)} KM");
    }
}
