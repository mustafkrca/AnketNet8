using AnketNet8.Models;

public class Response
{
    public int Id { get; set; }
    public int QuestionId { get; set; } // Hangi soru
    public string? SelectedOption { get; set; } // Seçilen seçenek
    public string? TextResponse { get; set; } // Açıklama tipi

    // Cevaba ait soru
    public Question Question { get; set; }
}