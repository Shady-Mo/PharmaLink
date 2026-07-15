using Bogus;
using Infrastructure.Persistence.Seeders.Fakers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Persistence.Seeders;

public class DatabaseSeeder(
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleSeeder roleSeeder,
    DrugSeeder drugSeeder,
    ILogger<DatabaseSeeder> logger,
    IWebHostEnvironment env)
{
    public async Task SeedAllAsync()
    {
        if (!env.IsDevelopment())
        {
            logger.LogWarning("Database seeding is only allowed in Development environment.");
            return;
        }

        logger.LogInformation("Ensuring database is created and migrated...");
        await context.Database.MigrateAsync();

        logger.LogInformation("Seeding Roles...");
        await roleSeeder.SeedAsync();

        logger.LogInformation("Seeding Drugs...");
        string drugJsonPath = Path.Combine(env.WebRootPath, "Data", "egyptian-drugs.json");
        await drugSeeder.SeedAsync(drugJsonPath);
        
        var drugs = await context.Drugs.ToListAsync();
        if (drugs.Count == 0)
        {
            logger.LogWarning("No drugs available in the database. Cannot proceed with inventory/order seeding.");
            return;
        }

        if (await context.Pharmacies.AnyAsync())
        {
            logger.LogInformation("Database already seeded with Pharmacies. Skipping Bogus generation.");
            return;
        }

        logger.LogInformation("Starting Bogus Database Seeding (Seed: 1337)...");
        Randomizer.Seed = new Random(1337);
        var geoGen = new GeoLocationGenerator(1337);

        // --- USERS ---
        logger.LogInformation("Seeding Users (Admins, Pharmacists, Patients)...");

        var adminPassword = "Password123!";
        var admins = new List<SystemAdmin>();
        for (int i = 0; i < 3; i++)
        {
            var admin = new SystemAdmin
            {
                Id = Guid.NewGuid(),
                UserName = $"admin{i + 1}@pharmalink.com",
                Email = $"admin{i + 1}@pharmalink.com",
                FullName = new Faker().Name.FullName(),
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
            var existing = await userManager.FindByEmailAsync(admin.Email);
            if (existing == null)
            {
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                    admins.Add(admin);
                }
            }
            else
            {
                admins.Add((SystemAdmin)existing);
            }
        }

        var pharmacists = new List<Pharmacist>();
        for (int i = 0; i < 30; i++)
        {
            var pharmacist = new Pharmacist
            {
                Id = Guid.NewGuid(),
                UserName = $"pharmacist{i + 1}@pharmalink.com",
                Email = $"pharmacist{i + 1}@pharmalink.com",
                FullName = new Faker().Name.FullName(),
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
            var existing = await userManager.FindByEmailAsync(pharmacist.Email);
            if (existing == null)
            {
                var result = await userManager.CreateAsync(pharmacist, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(pharmacist, AppRoles.Pharmacist);
                    pharmacists.Add(pharmacist);
                }
            }
            else
            {
                pharmacists.Add((Pharmacist)existing);
            }
        }

        var patients = new List<Patient>();
        var patientAddresses = new List<Address>();
        for (int i = 0; i < 100; i++)
        {
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                UserName = $"patient{i + 1}@example.com",
                Email = $"patient{i + 1}@example.com",
                FullName = new Faker().Name.FullName(),
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
            var existing = await userManager.FindByEmailAsync(patient.Email);
            if (existing == null)
            {
                var result = await userManager.CreateAsync(patient, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(patient, AppRoles.Patient);
                    patients.Add(patient);

                    var geo = geoGen.GenerateLocation();
                    patientAddresses.Add(new Address
                    {
                        AddressId = Guid.NewGuid(),
                        UserId = patient.Id,
                        Governorate = geo.Governorate,
                        City = geo.City,
                        AddressLine = new Faker().Address.StreetAddress() + ", " + geo.District,
                        GeoLocation = geo.Point,
                        IsDefault = true
                    });
                }
            }
            else
            {
                patients.Add((Patient)existing);
                var existingAddress = await context.Addresses.FirstOrDefaultAsync(a => a.UserId == existing.Id);
                if (existingAddress != null)
                {
                    patientAddresses.Add(existingAddress);
                }
                else
                {
                    var geo = geoGen.GenerateLocation();
                    var addr = new Address
                    {
                        AddressId = Guid.NewGuid(),
                        UserId = existing.Id,
                        Governorate = geo.Governorate,
                        City = geo.City,
                        AddressLine = new Faker().Address.StreetAddress() + ", " + geo.District,
                        GeoLocation = geo.Point,
                        IsDefault = true
                    };
                    patientAddresses.Add(addr);
                    await context.Addresses.AddAsync(addr);
                }
            }
        }

        await context.Addresses.AddRangeAsync(
            patientAddresses.Where(a => context.Entry(a).State == EntityState.Detached));
        await context.SaveChangesAsync();

        // --- PHARMACIES & BRANCHES ---
        logger.LogInformation("Seeding 15 Pharmacies and 40-60 Branches...");
        var pharmacies = new List<Pharmacy>();
        for (int i = 0; i < 15; i++)
        {
            pharmacies.Add(new Pharmacy
            {
                PharmacyId = Guid.NewGuid(),
                LegalName = new Faker().Company.CompanyName() + " Pharmacy",
                LicenseNumber = new Faker().Random.Replace("###-###-###"),
                LogoUrl = "https://fakeimg.pl/200x200/?text=Logo",
                OwnerUserId = pharmacists[new Faker().Random.Number(0, pharmacists.Count - 1)].Id,
                VerificationStatus = VerificationStatus.Verified
            });
        }

        await context.Pharmacies.AddRangeAsync(pharmacies);
        await context.SaveChangesAsync();

        var branches = new List<PharmacyBranch>();
        int totalBranches = new Faker().Random.Number(40, 60);
        for (int i = 0; i < totalBranches; i++)
        {
            var geo = geoGen.GenerateLocation();
            var supportsDelivery = new Faker().Random.Bool(0.8f); // 80% support delivery
            var supportsPickup = new Faker().Random.Bool(0.9f); // 90% support pickup
            if (!supportsDelivery && !supportsPickup) supportsPickup = true;

            branches.Add(new PharmacyBranch
            {
                BranchId = Guid.NewGuid(),
                PharmacyId = pharmacies[new Faker().Random.Number(0, pharmacies.Count - 1)].PharmacyId,
                BranchName = new Faker().Address.CityPrefix() + " Branch",
                Governorate = geo.Governorate,
                City = geo.City,
                AddressLine = new Faker().Address.StreetAddress() + ", " + geo.District,
                PhoneNumber = new Faker().Phone.PhoneNumber("01#-###-####"),
                WorkingHours = "09:00 AM - 11:00 PM",
                ServiceRadiusKm = supportsDelivery ? new Faker().PickRandom(3.0m, 5.0m, 10.0m, 15.0m) : 0m,
                SupportsDelivery = supportsDelivery,
                SupportsPickup = supportsPickup,
                GeoLocation = geo.Point
            });
        }

        await context.PharmacyBranches.AddRangeAsync(branches);
        await context.SaveChangesAsync();

        // --- INVENTORY ---
        logger.LogInformation("Seeding Inventory across all branches...");
        var inventories = new List<PharmacyInventory>();
        var commonDrugs = drugs.Take(50).ToList();
        var rareDrugs = drugs.Skip(50).Take(50).ToList();

        var fk = new Faker();
        foreach (var branch in branches)
        {
            bool isBigBranch = fk.Random.Bool(0.3f);
            var branchDrugs = new List<Drug>();

            if (isBigBranch)
            {
                branchDrugs.AddRange(commonDrugs);
                branchDrugs.AddRange(rareDrugs);
            }
            else
            {
                branchDrugs.AddRange(fk.PickRandom(commonDrugs, fk.Random.Number(10, 30)));
                branchDrugs.AddRange(fk.PickRandom(rareDrugs, fk.Random.Number(0, 5)));
            }

            foreach (var drug in branchDrugs)
            {
                int stock = fk.Random.Number(5, 100);
                int reserved = fk.Random.Bool(0.2f) ? fk.Random.Number(0, stock) : 0;

                if (fk.Random.Bool(0.05f)) stock = 0;

                DateOnly expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(fk.Random.Number(3, 24)));
                if (fk.Random.Bool(0.05f)) expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));

                inventories.Add(new PharmacyInventory
                {
                    InventoryId = Guid.NewGuid(),
                    BranchId = branch.BranchId,
                    DrugId = drug.DrugId,
                    StockQuantity = stock,
                    ReservedQuantity = reserved,
                    UnitPrice = drug.Price,
                    ExpiryDate = expiry,
                    LastSyncedAt = DateTime.UtcNow
                });
            }
        }

        await context.PharmacyInventories.AddRangeAsync(inventories);
        await context.SaveChangesAsync();

        // --- ORDERS & PRESCRIPTION REVIEWS ---
        logger.LogInformation("Seeding 400 Orders and 200 Prescription Reviews...");

        var orders = new List<Order>();
        var orderItems = new List<OrderItem>();
        var prescriptionReviews = new List<PrescriptionReview>();
        var prescriptionMedicines = new List<PrescriptionReviewMedicine>();

        for (int i = 0; i < 400; i++)
        {
            var patient = fk.PickRandom(patients);
            var patientAddress = patientAddresses.First(a => a.UserId == patient.Id);
            var fMode = fk.PickRandom<FulfillmentMode>();
            var status = fk.PickRandom(OrderStatus.Pending, OrderStatus.Processing, OrderStatus.Shipped,
                OrderStatus.Completed, OrderStatus.Cancelled);

            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                PatientUserId = patient.Id,
                DeliveryAddressId = patientAddress.AddressId,
                OrderStatus = status,
                FulfillmentMode = fMode,
                TotalAmount = 0
            };
            orders.Add(order);

            int numItems = fk.Random.Number(1, 4);
            var selectedDrugs = fk.PickRandom(drugs, numItems);
            decimal total = 0;

            foreach (var d in selectedDrugs)
            {
                int qty = fk.Random.Number(1, 3);
                var item = new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    DrugId = d.DrugId,
                    QuantityNeeded = qty,
                    ItemStatus = ItemStatus.Pending
                };
                total += (d.Price * qty);
                orderItems.Add(item);
            }

            order.TotalAmount = total;

            if (i < 200)
            {
                var reviewStatus = fk.PickRandom(PrescriptionReviewStatus.PendingReview,
                    PrescriptionReviewStatus.Approved, PrescriptionReviewStatus.Rejected);

                var pr = new PrescriptionReview
                {
                    PrescriptionReviewId = Guid.NewGuid(),
                    PatientUserId = patient.Id,
                    CreatedOrderId = order.OrderId,
                    PrescriptionImagePath = $"https://fakeimg.pl/400x400/?text=Prescription+{i}",
                    OriginalFileName = $"prescription_{i}.jpg",
                    AIModel = "gemini-1.5-flash",
                    ReviewStatus = reviewStatus,
                    PharmacistUserId = reviewStatus != PrescriptionReviewStatus.PendingReview
                        ? fk.PickRandom(pharmacists).Id
                        : null,
                    ReviewNotes = reviewStatus == PrescriptionReviewStatus.Rejected
                        ? "Invalid prescription."
                        : "Looks good.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow
                };
                prescriptionReviews.Add(pr);

                foreach (var d in selectedDrugs)
                {
                    bool isEdited = reviewStatus == PrescriptionReviewStatus.Approved && fk.Random.Bool(0.2f);
                    prescriptionMedicines.Add(new PrescriptionReviewMedicine
                    {
                        PrescriptionReviewMedicineId = Guid.NewGuid(),
                        PrescriptionReviewId = pr.PrescriptionReviewId,
                        MedicineName = d.BrandName,
                        OriginalMedicineName = d.BrandName,
                        Quantity = fk.Random.Number(1, 3),
                        IsEdited = isEdited,
                        Confidence = 0.95
                    });
                }
            }
        }

        await context.Orders.AddRangeAsync(orders);
        await context.OrderItems.AddRangeAsync(orderItems);
        await context.PrescriptionReviews.AddRangeAsync(prescriptionReviews);
        await context.PrescriptionReviewMedicines.AddRangeAsync(prescriptionMedicines);
        await context.SaveChangesAsync();

        logger.LogInformation("Database seeding completed successfully!");
    }
}