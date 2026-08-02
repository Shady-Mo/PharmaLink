using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string[] files = { 
            @"D:\ITI Graduation Project\PharmaLink-Backend\Infrastructure\AI\Plugins\DrugPlugin.cs",
            @"D:\ITI Graduation Project\PharmaLink-Backend\Infrastructure\AI\Plugins\InventoryPlugin.cs",
            @"D:\ITI Graduation Project\PharmaLink-Backend\Infrastructure\AI\Plugins\CartOrderPlugin.cs",
            @"D:\ITI Graduation Project\PharmaLink-Backend\Infrastructure\AI\Plugins\OrderPlugin.cs"
        };
        
        foreach (var file in files)
        {
            if (!File.Exists(file)) continue;
            var content = File.ReadAllText(file);
            
            // Replace Return Types
            content = Regex.Replace(content, @"public async Task<(DrugInfoResult|DrugSearchResult|InventoryCheckResult|BranchInventoryResult|CartResult|CartTotalsResult|OrderTrackingResult|OrderFulfillmentResult|OrderStatusResult)>", "public async Task<string>");
            
            // Replace 'return new XResult(...);' with 'return System.Text.Json.JsonSerializer.Serialize(new XResult(...));'
            content = Regex.Replace(content, @"return new (DrugInfoResult|DrugSearchResult|InventoryCheckResult|BranchInventoryResult|CartResult|CartTotalsResult|OrderTrackingResult|OrderFulfillmentResult|OrderStatusResult)\s*\(([\s\S]*?)\);", "return System.Text.Json.JsonSerializer.Serialize(new ());");
            
            File.WriteAllText(file, content);
            Console.WriteLine($"Updated {file}");
        }
    }
}
