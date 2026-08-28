using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Infrastructure.Data
{
    // IDataProtectionKeyContext → cookie/antiforgery anahtarları DB'de saklanır
    // (deploy'da konteyner değişse de oturumlar düşmez).
    public class OtoRehberDbContext : IdentityDbContext<AppUser>, IDataProtectionKeyContext
    {
        public OtoRehberDbContext(DbContextOptions<OtoRehberDbContext> options) : base(options)
        {
        }
        public DbSet<Car> Cars { get; set; }
        public DbSet<ChronicIssue> ChronicIssues { get; set; }
        public DbSet<ProsCons> ProsCons { get; set; }
        public DbSet<MileageMilestone> MileageMilestones { get; set; }
        public DbSet<CarReview> CarReviews { get; set; }
        public DbSet<UserGarage> UserGarages { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Yorum → Kullanıcı ilişkisi; bir kullanıcı bir araca en fazla 1 yorum.
            modelBuilder.Entity<CarReview>(b =>
            {
                b.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(r => new { r.CarId, r.UserId }).IsUnique();
            });

            // Garaj: aynı araç bir kullanıcıda tek kayıt.
            modelBuilder.Entity<UserGarage>()
                .HasIndex(g => new { g.UserId, g.CarId }).IsUnique();

            // Filtre / sıralama için index'ler.
            modelBuilder.Entity<Car>(b =>
            {
                b.HasIndex(c => c.Brand);
                b.HasIndex(c => c.Segment);
                b.HasIndex(c => c.ReliabilityScore);
            });

            // Başlangıç verisi (Seeding)
            modelBuilder.Entity<Car>().HasData(
                new Car
                {
                    Id = 1,
                    Brand = "Volkswagen",
                    ModelName = "Golf",
                    Engine = "1.6 TDI",
                    Segment = "C",
                    ExpertSummary = "C segmentinin referans modeli, kaliteli iç mekan ve tok sürüş hissi sunar. Ancak DSG şanzıman ve dizel motor bakım maliyetlerine dikkat edilmelidir.",
                    UserFeedbackSummary = "Forumlarda Golf kalitesi övülse de, DSG şanzıman arızaları ve dizel partikül filtresi (DPF) tıkanıklığı en çok şikayet edilen konulardır.",
                    ReliabilityScore = 8,
                    MinPrice = 800000,
                    MaxPrice = 1200000,
                    EstimatedMaintenanceCostEUR = 400,
                    ProductionYears = "2012-2020"
                },
                new Car
                {
                    Id = 2,
                    Brand = "Toyota",
                    ModelName = "Corolla",
                    Engine = "1.6 Valvematic",
                    Segment = "C",
                    ExpertSummary = "Sorunsuzluk dendiğinde akla ilk gelen model. Konfor odaklı, aile kullanımına çok uygun fakat performans beklentisi olanları üzebilir.",
                    UserFeedbackSummary = "Kullanıcılar genel olarak sorunsuzluğundan ve düşük yakıt tüketiminden çok memnun. Ancak yüksek hızlarda içeri yol sesi alması klasik bir şikayettir.",
                    ReliabilityScore = 9.5,
                    MinPrice = 700000,
                    MaxPrice = 1100000,
                    EstimatedMaintenanceCostEUR = 200,
                    ProductionYears = "2013-2018"
                }
            );

            modelBuilder.Entity<ChronicIssue>().HasData(
                new ChronicIssue
                {
                    Id = 1,
                    CarId = 1,
                    IssueTitle = "DSG Şanzıman Mekatronik Arızası",
                    Description = "Özellikle kuru kavramalı 7 ileri DSG şanzımanlarda dur-kalk trafikte ısınma ve mekatronik arızası.",
                    Severity = "Kritik",
                    EstimatedCostEUR = 1200,
                    AffectedYears = "2013-2018"
                },
                new ChronicIssue
                {
                    Id = 2,
                    CarId = 1,
                    IssueTitle = "EGR ve Partikül Filtresi",
                    Description = "Düşük devirlerde şehir içi kullanımda kurum bağlaması ve tıkanıklık.",
                    Severity = "Orta",
                    EstimatedCostEUR = 500,
                    AffectedYears = "2013-2020"
                }
            );

            modelBuilder.Entity<ProsCons>().HasData(
                // Golf Pros
                new ProsCons { Id = 1, CarId = 1, Type = "Pro", Description = "Kaliteli iç mekan" },
                new ProsCons { Id = 2, CarId = 1, Type = "Pro", Description = "Tok sürüş hissi" },
                new ProsCons { Id = 3, CarId = 1, Type = "Pro", Description = "İyi 2. el değeri" },
                // Golf Cons
                new ProsCons { Id = 4, CarId = 1, Type = "Con", Description = "DSG şanzıman riski" },
                new ProsCons { Id = 5, CarId = 1, Type = "Con", Description = "Dizel motor partikül filtresi" },
                new ProsCons { Id = 6, CarId = 1, Type = "Con", Description = "Yüksek servis maliyeti" },

                // Corolla Pros
                new ProsCons { Id = 7, CarId = 2, Type = "Pro", Description = "Mükemmel sorunsuzluk" },
                new ProsCons { Id = 8, CarId = 2, Type = "Pro", Description = "Geniş iç hacim" },
                new ProsCons { Id = 9, CarId = 2, Type = "Pro", Description = "Düşük işletme maliyeti" },
                // Corolla Cons
                new ProsCons { Id = 10, CarId = 2, Type = "Con", Description = "Zayıf yalıtım" },
                new ProsCons { Id = 11, CarId = 2, Type = "Con", Description = "Vasat performans" },
                new ProsCons { Id = 12, CarId = 2, Type = "Con", Description = "Demode iç tasarım" }
            );
        }
    }
    }

