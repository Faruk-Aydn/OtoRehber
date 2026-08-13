namespace OtoRehber.Domain.Entities
{
    public class ReviewLike
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ReviewId { get; set; }
        public bool IsHelpful { get; set; }

        public AppUser User { get; set; }
        public CarReview Review { get; set; }
    }
}
