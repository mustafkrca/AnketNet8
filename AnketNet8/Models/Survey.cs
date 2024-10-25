using AnketNet8.Models;

public class Survey
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime CreatedDate { get; set; }

    // Anketin sorular�
    public ICollection<Question> Questions { get; set; } = new List<Question>();

    // Kat�l�mc� say�s�n� tutmak i�in ek alan
    public int ParticipantCount { get; set; }
}
