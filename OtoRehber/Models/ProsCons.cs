namespace OtoRehber.Models
{
    public class ProsCons
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        
        // "Pro" (Artı) veya "Con" (Eksi) değerlerini alacak
        public string Type { get; set; } 
        
        public string Description { get; set; }

        public Car Car { get; set; }
    }
}
