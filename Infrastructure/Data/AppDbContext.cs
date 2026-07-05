using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid> {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<Pharmacist> Pharmacists { get; set; }
    public DbSet<SystemAdmin> SystemAdmins { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Pharmacy> Pharmacies { get; set; }
    public DbSet<PharmacyBranch> PharmacyBranches { get; set; }
    public DbSet<Drug> Drugs { get; set; }
    public DbSet<PharmacyInventory> PharmacyInventories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderFulfillmentLeg> OrderFulfillmentLegs { get; set; }
}
