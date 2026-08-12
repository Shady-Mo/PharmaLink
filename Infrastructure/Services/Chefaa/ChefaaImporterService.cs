using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Domain.Entities;

namespace Infrastructure.Services.Chefaa;

public class ChefaaImporterService : IChefaaImporterService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChefaaApiClient _apiClient;
    private readonly ILogger<ChefaaImporterService> _logger;

    private int _totalImported = 0;
    private int _totalSkipped = 0;
    private int _totalFailed = 0;
    private int _totalCategoriesCreated = 0;
    private bool _isRunning = false;
    private Stopwatch _stopwatch = new();
    private decimal _lastPrice = 0;

    public ChefaaImporterService(
        IServiceScopeFactory scopeFactory,
        IChefaaApiClient apiClient,
        ILogger<ChefaaImporterService> logger)
    {
        _scopeFactory = scopeFactory;
        _apiClient = apiClient;
        _logger = logger;
    }

    public object GetStatus()
    {
        return new
        {
            IsRunning = _isRunning,
            TotalImported = _totalImported,
            TotalSkipped = _totalSkipped,
            TotalFailed = _totalFailed,
            TotalCategoriesCreated = _totalCategoriesCreated,
            LastPriceProcessed = _lastPrice,
            ExecutionTime = _stopwatch.Elapsed.ToString()
        };
    }

    public async Task StartImportAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Chefaa import is already running.");
            return;
        }

        _isRunning = true;
        _totalImported = 0;
        _totalSkipped = 0;
        _totalFailed = 0;
        _totalCategoriesCreated = 0;
        _lastPrice = 0;
        _stopwatch.Restart();

        _logger.LogInformation("Starting Chefaa import via Meilisearch API...");

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Load existing products for idempotency
                _logger.LogInformation("Loading existing Chefaa IDs from database...");
                var existingChefaaIds = new HashSet<int>(await dbContext.Drugs.Where(p => p.ChefaaId != null).Select(p => p.ChefaaId.Value).ToListAsync(cancellationToken));
                
                // Load existing categories to cache
                var categoriesList = await dbContext.DrugCategories.ToListAsync(cancellationToken);
                var categoryCache = new Dictionary<string, DrugCategory>();
                foreach (var cat in categoriesList)
                {
                    // Use a unique key based on NameEn + Level + ParentId to handle duplicate names at different levels
                    string key = GetCategoryKey(cat.NameEn, cat.Level, cat.ParentId);
                    categoryCache[key] = cat;
                }

                _logger.LogInformation($"Loaded {existingChefaaIds.Count} products and {categoryCache.Count} categories into cache.");

                dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

                int batchSize = 1000;
                bool hasMore = true;

                while (hasMore && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation($"Fetching next {batchSize} products. LastPrice: {_lastPrice}");
                    
                    var response = await _apiClient.FetchProductsAsync(batchSize, _lastPrice, cancellationToken);
                    var hits = response["hits"]?.AsArray();

                    if (hits == null || hits.Count == 0)
                    {
                        hasMore = false;
                        break;
                    }

                    int newItemsInBatch = 0;

                    foreach (var hitNode in hits)
                    {
                        var hit = hitNode.AsObject();
                        int chefaaId = hit["id"]?.GetValue<int>() ?? 0;
                        string titleEn = hit["title_en"]?.GetValue<string>() ?? "";
                        decimal currentPrice = GetDecimalSafe(hit, "price");

                        _lastPrice = currentPrice;

                        if (existingChefaaIds.Contains(chefaaId))
                        {
                            _totalSkipped++;
                            continue;
                        }

                        try
                        {
                            var l1Name = GetCategoryOrBrandNameSafe(hit, "level_one_category");
                            var l2Name = GetCategoryOrBrandNameSafe(hit, "level_two_category");
                            var l3Name = GetCategoryOrBrandNameSafe(hit, "level_three_category");

                            DrugCategory c1 = GetOrCreateCategory(dbContext, categoryCache, l1Name, 1, null);
                            DrugCategory c2 = GetOrCreateCategory(dbContext, categoryCache, l2Name, 2, c1?.Id);
                            DrugCategory c3 = GetOrCreateCategory(dbContext, categoryCache, l3Name, 3, c2?.Id ?? c1?.Id);

                            // The product belongs to the deepest category available
                            var finalCategory = c3 ?? c2 ?? c1;

                            var brandData = GetCategoryOrBrandNameSafe(hit, "brands");

                            var product = new Drug
                            {
                                ChefaaId = chefaaId,
                                BrandName = titleEn,
                                ArabicName = hit["title_ar"]?.GetValue<string>() ?? "",
                                Price = GetDecimalSafe(hit, "price"),
                                FinalPrice = GetDecimalSafe(hit, "final_price"),
                                Discount = GetDecimalSafe(hit, "discount"),
                                CostPrice = GetDecimalSafe(hit, "cost_price"),
                                ImageUrl = hit["image"]?.GetValue<string>() ?? "",
                                MetaDescriptionAr = hit["meta_description_ar"]?.GetValue<string>() ?? "",
                                Slug = hit["slug"]?.GetValue<string>() ?? "",
                                BrandEn = brandData?.NameEn ?? "",
                                BrandAr = brandData?.NameAr ?? "",
                                BrandSlug = brandData?.Slug ?? "",
                                BrandImageUrl = brandData?.ImageUrl ?? "",
                                Status = hit["status"]?.GetValue<string>() ?? "",
                                Type = hit["type"]?.GetValue<string>() ?? "",
                                FlowType = hit["flow_type"]?.GetValue<string>() ?? "",
                                FullUrl = hit["full_url"]?.GetValue<string>() ?? "",
                                MetaKeywordsEn = hit["meta_keywords_en"]?.GetValue<string>() ?? "",
                                MetaKeywordsAr = hit["meta_keywords_ar"]?.GetValue<string>() ?? "",
                                MetaDescriptionEn = hit["meta_description_en"]?.GetValue<string>() ?? "",
                                CategoryId = finalCategory?.Id,
                                InStock = hit["in_stock"]?.GetValue<bool>() ?? false,
                                OutOfStock = hit["out_of_stock"]?.GetValue<bool>() ?? false,
                                LowStock = hit["low_stock"]?.GetValue<bool>() ?? false,
                                RequiresPrescription = hit["need_prescription"]?.GetValue<bool>() ?? false,
                                IsActive = hit["active"]?.GetValue<bool>() ?? false,
                                MaxQuantity = hit["max_quantity"]?.GetValue<int>() ?? 0,
                                Quantity = hit["quantity"]?.GetValue<int>() ?? 0,
                                PurchaseCount = hit["purchase_count"]?.GetValue<int>() ?? 0
                            };

                            var suppliersNode = hit["suppliers_info"] as JsonArray;
                            if (suppliersNode != null)
                            {
                                foreach (var supp in suppliersNode)
                                {
                                    if (supp is JsonObject suppObj)
                                    {
                                        product.Suppliers.Add(new DrugSupplier
                                        {
                                            SupplierId = suppObj["id"]?.GetValue<int>() ?? 0,
                                            NameAr = suppObj["name_ar"]?.GetValue<string>() ?? "",
                                            NameEn = suppObj["name_en"]?.GetValue<string>() ?? "",
                                            Discount = GetDecimalSafe(suppObj, "discount"),
                                            CommercialPrice = GetDecimalSafe(suppObj, "commercial_price"),
                                            Price = GetDecimalSafe(suppObj, "price"),
                                            Quantity = suppObj["quantity"]?.GetValue<int>() ?? 0
                                        });
                                    }
                                }
                            }

                            var landingNode = hit["landing_pages"] as JsonArray;
                            if (landingNode != null)
                            {
                                foreach (var lp in landingNode)
                                {
                                    if (lp is JsonObject lpObj)
                                    {
                                        product.LandingPages.Add(new DrugLandingPage
                                        {
                                            TitleAr = lpObj["title_ar"]?.GetValue<string>() ?? "",
                                            TitleEn = lpObj["title_en"]?.GetValue<string>() ?? "",
                                            Slug = lpObj["slug"]?.GetValue<string>() ?? ""
                                        });
                                    }
                                }
                            }

                            dbContext.Drugs.Add(product);
                            existingChefaaIds.Add(chefaaId);
                            _totalImported++;
                            newItemsInBatch++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to parse/add Chefaa product ID {chefaaId}");
                            _totalFailed++;
                        }
                    }

                    if (newItemsInBatch > 0)
                    {
                        // Enable auto detect for relationships just during save if needed, 
                        // but since we explicitly set CategoryId it should be fine.
                        await dbContext.SaveChangesAsync(cancellationToken);
                        
                        // Clear the context to free memory, but keep categories in memory via dict
                        dbContext.ChangeTracker.Clear();
                        
                        // Re-attach categories to the context so we don't get detached entity exceptions
                        // actually we only rely on CategoryId (integer) so we don't need tracking!
                    }

                    if (hits.Count < batchSize)
                    {
                        hasMore = false;
                    }
                }
            }

            _logger.LogInformation("Chefaa import completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during Chefaa import.");
        }
        finally
        {
            _stopwatch.Stop();
            _isRunning = false;
            
            _logger.LogInformation($@"
Chefaa Import Summary:
---------------
Total Imported:         {_totalImported}
Total Skipped:          {_totalSkipped}
Total Failed:           {_totalFailed}
Total Categories Made:  {_totalCategoriesCreated}
Execution Time:         {_stopwatch.Elapsed}");
        }
    }

    private DrugCategory GetOrCreateCategory(AppDbContext dbContext, Dictionary<string, DrugCategory> cache, (string NameEn, string NameAr, string Slug, string ImageUrl)? data, int level, int? parentId)
    {
        if (data == null || (string.IsNullOrWhiteSpace(data.Value.NameEn) && string.IsNullOrWhiteSpace(data.Value.NameAr))) 
            return null;

        string key = GetCategoryKey(data.Value.NameEn ?? data.Value.NameAr, level, parentId);
        
        if (cache.TryGetValue(key, out var existing))
        {
            // If the existing category doesn't have a slug, update it.
            if (string.IsNullOrEmpty(existing.Slug) && !string.IsNullOrEmpty(data.Value.Slug))
            {
                existing.Slug = data.Value.Slug;
            }
            return existing;
        }

        var newCat = new DrugCategory
        {
            NameEn = data.Value.NameEn,
            NameAr = data.Value.NameAr,
            Slug = data.Value.Slug,
            ImageUrl = data.Value.ImageUrl,
            Level = level,
            ParentId = parentId
        };

        // We must save it immediately so it gets an ID that its children can reference
        dbContext.DrugCategories.Add(newCat);
        dbContext.SaveChanges(); // Synchronous save to get the ID right now
        
        cache[key] = newCat;
        _totalCategoriesCreated++;
        
        return newCat;
    }

    private string GetCategoryKey(string name, int level, int? parentId)
    {
        return $"{name}_{level}_{parentId ?? 0}";
    }

    private decimal GetDecimalSafe(JsonObject node, string propertyName)
    {
        if (node.TryGetPropertyValue(propertyName, out var val) && val != null)
        {
            var strVal = val.ToString();
            if (decimal.TryParse(strVal, out decimal result))
                return result;
        }
        return 0;
    }

    private (string NameEn, string NameAr, string Slug, string ImageUrl)? GetCategoryOrBrandNameSafe(JsonObject node, string propertyName)
    {
        if (!node.TryGetPropertyValue(propertyName, out var val) || val == null)
            return null;

        // Sometimes it's a JsonObject (e.g. level_one_category, brands)
        if (val is JsonObject obj)
        {
            return ExtractDataFromObject(obj);
        }
        
        // Sometimes it's a JsonArray (e.g. level_two_category, level_three_category)
        if (val is JsonArray arr && arr.Count > 0)
        {
            var firstNode = arr[0];
            if (firstNode is JsonObject firstObj)
            {
                return ExtractDataFromObject(firstObj);
            }
            if (firstNode != null && firstNode.GetValueKind() == JsonValueKind.String)
            {
                return (firstNode.ToString(), null, null, null);
            }
        }
        
        return null;
    }

    private (string NameEn, string NameAr, string Slug, string ImageUrl) ExtractDataFromObject(JsonObject obj)
    {
        string nameEn = null;
        string nameAr = null;
        string slug = null;
        string imageUrl = null;

        if (obj.TryGetPropertyValue("title_en", out var titleEn) && titleEn != null)
        {
            nameEn = titleEn.ToString();
        }
        if (obj.TryGetPropertyValue("title_ar", out var titleAr) && titleAr != null)
        {
            nameAr = titleAr.ToString();
        }
        if (string.IsNullOrWhiteSpace(nameEn) && string.IsNullOrWhiteSpace(nameAr) && obj.TryGetPropertyValue("name", out var nameNode) && nameNode != null)
        {
            nameEn = nameNode.ToString();
        }
        if (obj.TryGetPropertyValue("slug", out var slugNode) && slugNode != null)
        {
            slug = slugNode.ToString();
        }
        if (obj.TryGetPropertyValue("images", out var imagesNode) && imagesNode != null)
        {
            imageUrl = imagesNode.ToString();
        }
        
        return (nameEn, nameAr, slug, imageUrl);
    }
}
