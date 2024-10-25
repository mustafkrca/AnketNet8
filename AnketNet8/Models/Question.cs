namespace AnketNet8.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public QuestionType Type { get; set; } // Soru türü
        public int SurveyId { get; set; } // Hangi ankete ait
        public string? Options { get; set; } // Seçenekler

        // Soruya ait anket
        public Survey Survey { get; set; }

        // Cevaba ait olan yanıtları tutmak için
        public ICollection<Response> Responses { get; set; } = new List<Response>();

    }

    public enum QuestionType
    {
        MultipleChoice = 1,  // Seçmeli sorular
        PredefinedChoice = 2, // Hazır cevaplı sorular
        TextResponse = 3     // Açıklama soruları
    }
}