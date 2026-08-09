namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<AppUser>().OwnsMany(u => u.RefreshTokens, a =>
        {
            a.WithOwner().HasForeignKey("UserId");
            a.Property(r => r.Token).IsRequired().HasMaxLength(200);
            a.ToTable("RefreshTokens");
        });

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<Pharmacist> Pharmacists { get; set; }
    public DbSet<SystemAdmin> SystemAdmins { get; set; }
    public DbSet<PharmacyAdmin> PharmacyAdmins { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Pharmacy> Pharmacies { get; set; }
    public DbSet<PharmacyBranch> PharmacyBranches { get; set; }
    public DbSet<PharmacyBranchSchedule> PharmacyBranchSchedules { get; set; }
    public DbSet<Drug> Drugs { get; set; }
    public DbSet<PharmacyInventory> PharmacyInventories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderFulfillmentLeg> OrderFulfillmentLegs { get; set; }
    public DbSet<PhoneVerificationOtp> PhoneVerificationOtps { get; set; }
    public DbSet<OrderFulfillmentLegStatusAudit> OrderFulfillmentLegStatusAudits { get;  set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<PrescriptionReview> PrescriptionReviews { get; set; }
    public DbSet<PrescriptionReviewMedicine> PrescriptionReviewMedicines { get; set; }
    public DbSet<MedicalInquiry> MedicalInquiries { get; set; }
    public DbSet<PharmacistAssignment> PharmacistAssignments { get; set; }

    public DbSet<PharmacyMissingStockLog> PharmacyMissingStockLog { get;    set; }  

    public DbSet<PharmacyReport> PharmacyReport     { get; set; }   

}
