using Domain.Entities;
using Infrastructure.AI.Plugins;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.UnitTests.AI;

/// <summary>
/// Unit tests for DrugPlugin, the native SK plugin that queries the drug database.
///
/// DESIGN DECISION — SQLite in-memory with spatial + Identity:
///   DrugPlugin uses EF.Functions.Like() (SQL LIKE) and resolves AppDbContext which
///   extends IdentityDbContext and uses NetTopologySuite for spatial data.
///
///   We use SQLite in-memory with:
///   - UseNetTopologySuite() for spatial column support
///   - AddIdentityCore<AppUser>() for Identity schema
///
///   The SqliteConnection is kept open for the test class lifetime so the
///   in-memory database persists across the test methods.
/// </summary>
public class DrugPluginTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly AppDbContext _db;
    private readonly DrugPlugin _plugin;

    public DrugPluginTests()
    {
        // Shared connection — in-memory DB lives as long as this connection is open
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();

        // Register minimal Identity services required by IdentityDbContext<AppUser>
        services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        // Register AppDbContext with SQLite + NetTopologySuite spatial support
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite(_connection,
                o => o.UseNetTopologySuite()));

        _serviceProvider = services.BuildServiceProvider();
        _db = _serviceProvider.GetRequiredService<AppDbContext>();

        // Create all tables — SQLite will execute CREATE TABLE for the full model
        _db.Database.EnsureCreated();

        SeedTestData();

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = _serviceProvider.GetRequiredService<ILogger<DrugPlugin>>();
        _plugin = new DrugPlugin(scopeFactory, logger);
    }

    private void SeedTestData()
    {
        _db.Drugs.AddRange(
            new Drug
            {
                DrugId = Guid.NewGuid(),
                BrandName = "Amoxicillin 500mg",
                GenericName = "Amoxicillin",
                DrugClass = "Antibiotics",
                Form = "Capsule",
                Strength = "500mg",
                RequiresPrescription = true,
                ArabicName = string.Empty,
                DrugBankId = string.Empty,
                RxNormCui = string.Empty,
                NdcCode = string.Empty,
                Manufacturer = string.Empty,
                IsActive = true
            },
            new Drug
            {
                DrugId = Guid.NewGuid(),
                BrandName = "Augmentin 625mg",
                GenericName = "Amoxicillin/Clavulanate",
                DrugClass = "Antibiotics",
                Form = "Tablet",
                Strength = "625mg",
                RequiresPrescription = true,
                ArabicName = string.Empty,
                DrugBankId = string.Empty,
                RxNormCui = string.Empty,
                NdcCode = string.Empty,
                Manufacturer = string.Empty,
                IsActive = true
            },
            new Drug
            {
                DrugId = Guid.NewGuid(),
                BrandName = "Ibuprofen 400mg",
                GenericName = "Ibuprofen",
                DrugClass = "NSAIDs",
                Form = "Tablet",
                Strength = "400mg",
                RequiresPrescription = false,
                ArabicName = string.Empty,
                DrugBankId = string.Empty,
                RxNormCui = string.Empty,
                NdcCode = string.Empty,
                Manufacturer = string.Empty,
                IsActive = true
            },
            new Drug
            {
                DrugId = Guid.NewGuid(),
                BrandName = "Paracetamol 500mg",
                GenericName = "Acetaminophen",
                DrugClass = "Analgesics",
                Form = "Tablet",
                Strength = "500mg",
                RequiresPrescription = false,
                ArabicName = string.Empty,
                DrugBankId = string.Empty,
                RxNormCui = string.Empty,
                NdcCode = string.Empty,
                Manufacturer = string.Empty,
                IsActive = true
            }
        );
        _db.SaveChanges();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "AI")]
    public async Task GetDrugInfoAsync_WithExistingDrugName_ReturnsJsonResult()
    {
        var result = await _plugin.GetDrugInfoAsync("Amoxicillin");

        Assert.NotNull(result);
        Assert.DoesNotContain("not found", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Amoxicillin", result);
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task GetDrugInfoAsync_WithNonExistentDrug_ReturnsNotFoundMessage()
    {
        var result = await _plugin.GetDrugInfoAsync("NonExistentDrug12345");

        Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task SearchDrugsAsync_WithValidPrefix_ReturnsMatchingDrugs()
    {
        var result = await _plugin.SearchDrugsAsync("Ibuprofen");

        Assert.NotNull(result);
        Assert.Contains("Ibuprofen", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task SearchDrugsAsync_WithNoMatches_ReturnsNoMatchMessage()
    {
        var result = await _plugin.SearchDrugsAsync("ZzzNonExistent999");

        Assert.Contains("No drugs matching", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task GetDrugsByCategoryAsync_WithAntibiotics_ReturnsBothAntibiotics()
    {
        var result = await _plugin.GetDrugsByCategoryAsync("Antibiotics");

        Assert.NotNull(result);
        Assert.Contains("Amoxicillin", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Augmentin", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task GetDrugInfoAsync_ReturnsValidJson()
    {
        var result = await _plugin.GetDrugInfoAsync("Paracetamol");

        if (!result.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            var exception = Record.Exception(
                () => System.Text.Json.JsonDocument.Parse(result));
            Assert.Null(exception);
        }
    }

    public void Dispose()
    {
        _db.Dispose();
        _serviceProvider.Dispose();
        _connection.Dispose();
    }
}
