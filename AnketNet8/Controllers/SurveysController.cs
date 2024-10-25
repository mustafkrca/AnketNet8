using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AnketNet8.Data;
using AnketNet8.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AnketNet8.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SurveysController : Controller
    {
        private readonly SurveyContext _context;

        public SurveysController(SurveyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> AdminDashboard()
        {
            return View(await _context.Surveys.ToListAsync());
        }

        // GET: Surveys
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Surveys.ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> AdminLogin()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> AlreadySubmitted()
        {
            return View();
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
            survey.ParticipantCount = uniqueParticipants;

            // Her soru için yanıt yüzdelerini hesapla
            foreach (var question in survey.Questions)
            {
                question.Responses = question.Responses ?? new List<Response>();

                // Sadece çoktan seçmeli ve önceden tanımlı sorular için yüzdeleri hesapla
                if (question.Type == QuestionType.MultipleChoice || question.Type == QuestionType.PredefinedChoice)
                {
                    if (!string.IsNullOrEmpty(question.Options))
                    {
                        var options = question.Options.Split(';');
                        var totalResponses = question.Responses.Count();

                        question.Options = string.Join(";", options.Select(option =>
                        {
                            var responseCount = question.Responses.Count(r => r.SelectedOption == option);
                            var percentage = totalResponses > 0 ? (double)responseCount / totalResponses * 100 : 0;
                            return $"{option} - {percentage:F2}%"; // İki ondalık basamak ile göster
                        }));
                    }
                }
            }

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
                return RedirectToAction("AdminDashboard");
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
                return RedirectToAction("AdminDashboard");
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
            return RedirectToAction("AdminDashboard");
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

        [AllowAnonymous]
        public async Task<IActionResult> Solve(int id)
        {
            // Çerezi kontrol et: Eğer daha önce çözülmüşse bir hata veya uyarı sayfasına yönlendirin
            if (Request.Cookies.ContainsKey($"survey_{id}_completed"))
            {
                TempData["Message"] = "Bu anketi daha önce çözdünüz.";
                return RedirectToAction("AlreadySubmitted");
            }

            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .ThenInclude(q => q.Responses)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            return View(survey);
        }

        [AllowAnonymous]

        public async Task<IActionResult> Thanks(int id)
        {

            return View();
        }

        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> SubmitResponses(int SurveyId, Dictionary<string, string> responses)
        {
            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Id == SurveyId);

            if (survey == null)
            {
                return NotFound();
            }

            // Gelen yanıtları işle
            foreach (var response in responses)
            {
                if (response.Key.StartsWith("response_"))
                {
                    var questionIdStr = response.Key.Split('_')[1];
                    if (int.TryParse(questionIdStr, out int questionId))
                    {
                        var selectedOption = response.Value;
                        var answer = new Response
                        {
                            QuestionId = questionId,
                            SelectedOption = selectedOption
                        };
                        _context.Responses.Add(answer);
                    }
                }
                else if (response.Key.StartsWith("TextResponse_"))
                {
                    var questionIdStr = response.Key.Split('_')[1];
                    if (int.TryParse(questionIdStr, out int questionId))
                    {
                        var textResponse = response.Value;
                        var answer = new Response
                        {
                            QuestionId = questionId,
                            TextResponse = textResponse
                        };
                        _context.Responses.Add(answer);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Anket çözüldükten sonra çerezi oluştur
            Response.Cookies.Append(
                $"survey_{SurveyId}_completed",
                "true",
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMonths(1) // Çerezi bir ay boyunca saklar
                }
            );

            return RedirectToAction("Thanks");
        }

        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> AdminLogin(string username, string password)
        {
            var adminUser = await _context.Admins
                                           .FirstOrDefaultAsync(u => u.Username == username && u.Password == password); // Üretimde şifreyi hashleyin!

            if (adminUser != null)
            {
                // Kullanıcı bulundu, oturumu başlat
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, adminUser.Username),
            new Claim(ClaimTypes.Role, "Admin")
        };

                var claimsIdentity = new ClaimsIdentity(claims, "AdminAuth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Varsayılan şemayı belirtin
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                return RedirectToAction("AdminDashboard");
            }

            // Geçersiz giriş denemesi
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            return View();
        }



        [AllowAnonymous]

        private bool SurveyExists(int id)
        {
            return _context.Surveys.Any(e => e.Id == id);
        }
    }
}
