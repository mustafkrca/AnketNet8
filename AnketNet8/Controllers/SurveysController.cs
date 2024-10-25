using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AnketNet8.Data;
using AnketNet8.Models;

namespace AnketNet8.Controllers
{
    public class SurveysController : Controller
    {
        private readonly SurveyContext _context;

        public SurveysController(SurveyContext context)
        {
            _context = context;
        }

        // GET: Surveys
        public async Task<IActionResult> Index()
        {
            return View(await _context.Surveys.ToListAsync());
        }

        // GET: Surveys/Details/5
        public IActionResult Details(int id)
        {
            // Anketi ve sorularını dahil et
            var survey = _context.Surveys
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Responses)
                .FirstOrDefault(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            // Katılımcı sayısını hesapla
            var uniqueParticipants = survey.Questions
                .SelectMany(q => q.Responses)
                .Select(r => r.QuestionId)
                .Distinct()
                .Count();

            // Katılımcı sayısını ata
            survey.ParticipantCount = uniqueParticipants; // Survey modeline bu alanı eklemeyi unutmayın

            return View(survey);
        }

        // GET: Surveys/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Surveys/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,CreatedDate")] Survey survey)
        {
            if (ModelState.IsValid)
            {
                _context.Add(survey);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(survey);
        }

        // GET: Surveys/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the survey with its related questions using Include
            var survey = await _context.Surveys
                .Include(s => s.Questions) // Include related questions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            return View(survey);
        }


        // POST: Surveys/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,CreatedDate")] Survey survey)
        {
            if (id != survey.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(survey);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SurveyExists(survey.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(survey);
        }

        // GET: Surveys/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var survey = await _context.Surveys
                .FirstOrDefaultAsync(m => m.Id == id);
            if (survey == null)
            {
                return NotFound();
            }

            return View(survey);
        }

        // POST: Surveys/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var survey = await _context.Surveys.FindAsync(id);
            if (survey != null)
            {
                _context.Surveys.Remove(survey);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int id, string questionText, int questionType, List<string> options)
        {
            if (ModelState.IsValid)
            {
                // Yeni bir soru oluştur
                var newQuestion = new Question
                {
                    SurveyId = id,
                    Text = questionText,
                    Type = (QuestionType)questionType,
                    Options = (questionType == (int)QuestionType.MultipleChoice || questionType == (int)QuestionType.PredefinedChoice) ? string.Join(";", options) : null
                };

                // Veritabanına ekle ve kaydet
                _context.Questions.Add(newQuestion);
                await _context.SaveChangesAsync();

                // Başarılı işlem sonrası geri dönüş
                return RedirectToAction("Edit", new { id = id });
            }

            // Model geçerli değilse, hata durumunda geriye dön
            return RedirectToAction("Edit", new { id = id });
        }



        // POST: Surveys/DeleteQuestion/5
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound(); // Soru bulunamadıysa hata döndür
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            // Kullanıcıyı düzenleme sayfasına yönlendir, aynı anket ile
            return RedirectToAction("Edit", new { id = question.SurveyId }); // Belirli bir anketin düzenleme sayfasına yönlendirme
        }
        public async Task<IActionResult> Solve(int id)
        {
            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .ThenInclude(q => q.Responses) // Eğer ihtiyaç varsa yanıtları da dahil et
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            return View(survey);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitResponses(Dictionary<string, string> responses)
        {
            foreach (var response in responses)
            {
                // Anahtarın doğru formatta olup olmadığını kontrol edin
                if (response.Key.StartsWith("response_"))
                {
                    // response.Key = "response_1", bu yüzden '_' karakterinden sonra gelen kısmı alıyoruz
                    var questionIdStr = response.Key.Split('_')[1];
                    if (int.TryParse(questionIdStr, out int questionId))
                    {
                        var selectedOption = response.Value;

                        // Yeni bir yanıt oluşturun ve veritabanına kaydedin
                        var answer = new Response
                        {
                            QuestionId = questionId,
                            SelectedOption = selectedOption
                            // Eğer metin yanıtı varsa, buraya ekleyebilirsiniz
                        };

                        _context.Responses.Add(answer);
                    }
                    else
                    {
                        // Hatalı soru ID'si durumu için hata işlemleri
                        ModelState.AddModelError(string.Empty, "Geçersiz soru ID'si: " + questionIdStr);
                    }
                }
                if (response.Key.StartsWith("TextResponse_"))
                {
                    // response.Key = "response_1", bu yüzden '_' karakterinden sonra gelen kısmı alıyoruz
                    var questionIdStr = response.Key.Split('_')[1];
                    if (int.TryParse(questionIdStr, out int questionId))
                    {
                        var textResponse = response.Value;

                        // Yeni bir yanıt oluşturun ve veritabanına kaydedin
                        var answer = new Response
                        {
                            QuestionId = questionId,
                            TextResponse = textResponse
                            // Eğer metin yanıtı varsa, buraya ekleyebilirsiniz
                        };

                        _context.Responses.Add(answer);
                    }
                    else
                    {
                        // Hatalı soru ID'si durumu için hata işlemleri
                        ModelState.AddModelError(string.Empty, "Geçersiz soru ID'si: " + questionIdStr);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index)); // Yanıtlar gönderildikten sonra liste sayfasına yönlendirin
        }

        private bool SurveyExists(int id)
        {
            return _context.Surveys.Any(e => e.Id == id);
        }
    }
}
