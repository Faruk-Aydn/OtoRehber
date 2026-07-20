using Microsoft.EntityFrameworkCore;
namespace OtoRehber.Models
{
    public class OtoRehberDbContext : DbContext
    {
        public OtoRehberDbContext(DbContextOptions<OtoRehberDbContext> options) : base(options)
        {
        }
        public DbSet<Car> Cars { get; set; }
        public DbSet<ChronicIssue> ChronicIssues { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
                    ReliabilityScore = 8,
                    PriceRange = "800k - 1.2M TL",
                    EstimatedMaintenanceCostEUR = 400,
                    Pros = ["Kaliteli iç mekan", "Tok sürüş hissi", "İyi 2. el değeri"],
                    Cons = ["DSG şanzıman riski", "Dizel motor partikül filtresi", "Yüksek servis maliyeti"]
                },
                new Car
                {
                    Id = 2,
                    Brand = "Toyota",
                    ModelName = "Corolla",
                    Engine = "1.6 Valvematic",
                    Segment = "C",
                    ExpertSummary = "Sorunsuzluk dendiğinde akla ilk gelen model. Konfor odaklı, aile kullanımına çok uygun fakat performans beklentisi olanları üzebilir.",
                    ReliabilityScore = 9.5,
                    PriceRange = "700k - 1.1M TL",
                    EstimatedMaintenanceCostEUR = 200,
                    Pros = ["Mükemmel sorunsuzluk", "Geniş iç hacim", "Düşük işletme maliyeti"],
                    Cons = ["Zayıf yalıtım", "Vasat performans", "Demode iç tasarım"]
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
        }
    }
    }

