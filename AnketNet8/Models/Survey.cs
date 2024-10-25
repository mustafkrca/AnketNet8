using AnketNet8.Models;

public class Survey
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime CreatedDate { get; set; }

    // Anketin sorularý
    public ICollection<Question> Questions { get; set; } = new List<Question>();

    // Katýlýmcý sayýsýný tutmak için ek alan
    public int ParticipantCount { get; set; }
}
