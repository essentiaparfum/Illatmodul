using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DotNetNuke.Common;
using DotNetNuke.Data;
using DotNetNuke.Entities.Profile;
using DotNetNuke.Entities.Users;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using Illamdul.Dnn.Illatmodul.Models;


namespace Illamdul.Dnn.Illatmodul.Controllers
{
    public class ItemController : DnnController
    {
        // 1) Alap nézet
        public ActionResult Index()
        {
            return View();
        }

        // 2) GET Kérdés
        public ActionResult Kerdes(int id = 1)
        {
            using (var ctx = DataContext.Instance())
            {
                var qRepo = ctx.GetRepository<PerfumeQuestion>();
                var aRepo = ctx.GetRepository<PerfumeAnswer>();

                // 🔹 Dinamikus kérdésszám
                int totalCount = qRepo.Get().Count();
                ViewBag.TotalQuestions = totalCount;

                // 🔹 Aktuális kérdés
                var question = qRepo.Find("WHERE SortOrder = @0", id).FirstOrDefault();
                if (question == null)
                    return RedirectToAction("Eredmeny");

                // 🔹 Mindegyik válaszlehetőség betöltése
                var answers = aRepo
                    .Find("WHERE QuestionID = @0 ORDER BY SortOrder", question.QuestionID)
                    .ToList();

                var model = new QuestionModel
                {
                    QuestionNumber = id,
                    QuestionText = question.QuestionText,
                    Answers = answers
                };

                return View("Kerdes", model);
            }
        }

        [HttpPost]
        public ActionResult Kerdes(int id = 1, string valasz = null)
        {
            const string sessionKey = "Illatmodul_Valaszok";
            var userId = UserController.Instance.GetCurrentUserInfo().UserID;

            using (var ctx = DataContext.Instance())
            {
                var answerRepo = ctx.GetRepository<PerfumeUserAnswer>();

                // Ha ez az 1. kérdés, töröljük a korábbi válaszokat (DB-ből és Session-ből)
                if (id == 1)
                {
                    answerRepo.Delete("WHERE ModuleId = @0 AND UserID = @1",
                                      ModuleContext.ModuleId, userId);
                    Session.Remove(sessionKey);
                }

                // Ha van új válasz, elmentjük Session-be és DB-be
                if (!string.IsNullOrEmpty(valasz))
                {
                    // 1.a) Session
                    var list = Session[sessionKey] as List<string> ?? new List<string>();
                    list.Add(valasz);
                    Session[sessionKey] = list;

                    // 1.b) DB-be
                    answerRepo.Insert(new PerfumeUserAnswer
                    {
                        ModuleId = ModuleContext.ModuleId,
                        UserID = userId,
                        QuestionID = id,
                        AnswerValue = valasz,
                        CreatedOn = DateTime.Now
                    });
                }
            }

            // 2) Következő kérdés számítása
            int next = id + 1;
            using (var ctx = DataContext.Instance())
            {
                int total = ctx.GetRepository<PerfumeQuestion>().Get().Count();
                if (next <= total)
                {
                    // Ha még maradt kérdés, irány a következő
                    string url = Globals.NavigateURL(
                        PortalSettings.ActiveTab.TabID,
                        "Kerdes",
                        "mid=" + ModuleContext.ModuleId,
                        "id=" + next
                    );
                    return Redirect(url);
                }
            }

            // 3) Ha elfogytak a kérdések, eredményre ugrunk
            string eredmenyUrl = Globals.NavigateURL(
                PortalSettings.ActiveTab.TabID,
                "Eredmeny",
                "mid=" + ModuleContext.ModuleId
            );
            return Redirect(eredmenyUrl);
        }



        // 4) Eredmény – pontozás a ValueCode alapján + profilba mentés
        public ActionResult Eredmeny()
        {
            try
            {
                // 1) Aktuális felhasználó
                var userId = UserController.Instance.GetCurrentUserInfo().UserID;
                List<string> answerCodes;

                using (var ctx = DataContext.Instance())
                {
                    // 2) Lekérdezzük a PerfumeUserAnswer táblából a válaszokat
                    var answerRepo = ctx.GetRepository<PerfumeUserAnswer>();
                    var answers = answerRepo
                        .Find("WHERE ModuleId = @0 AND UserID = @1 ORDER BY QuestionID",
                              ModuleContext.ModuleId, userId)
                        .ToList();

                    answerCodes = answers.Select(a => a.AnswerValue).ToList();

                    // 3) Pontozás előre definiált kategóriák szerint
                    var categories = new[]
                    {
                "Fás illatok","Friss illatok","Fűszeres illatok","Púderes illatok",
                "Édes illatok","Gyümölcsös illatok","Orientális illatok","Virágos illatok"
            };
                    var scores = categories
                        .ToDictionary(c => c, c => 0, StringComparer.InvariantCultureIgnoreCase);
                    foreach (var code in answerCodes)
                    {
                        if (!string.IsNullOrEmpty(code) && scores.ContainsKey(code))
                            scores[code]++;
                    }

                    // 4) A két legtöbb pontot kapott kategória
                    var topCategories = scores
                        .OrderByDescending(kvp => kvp.Value)
                        .Take(2)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    // 5) Mentés a felhasználó profiljába
                    var profileRepo = ctx.GetRepository<UserProfileData>();
                    SaveProfileValue(profileRepo, userId, 67, topCategories.ElementAtOrDefault(0));
                    SaveProfileValue(profileRepo, userId, 68, topCategories.ElementAtOrDefault(1));

                    
                    ViewBag.TopCategories = topCategories;
                }

                // 6) (Opcionális) ürítjük a sessiont is
                Session.Remove("Illatmodul_Valaszok");

                // 7) Nézetnek átadott teljes válaszlista (ha szükséges)
                ViewBag.AllAnswers = answerCodes;

                return View("Eredmeny");
            }
            catch (Exception ex)
            {
                return Content(
                    "❌ Hiba történt:<br/><strong>" + ex.Message + "</strong>" +
                    "<br/><pre>" + ex.StackTrace + "</pre>",
                    "text/html"
                );
            }
        }

        // Segédfüggvény a profilérték mentéséhez (marad változatlan)
        private void SaveProfileValue(
            IRepository<UserProfileData> repo,
            int userId,
            int propertyId,
            string value
        )
        {
            if (string.IsNullOrEmpty(value)) return;

            var existing = repo
                .Find("WHERE UserID = @0 AND PropertyDefinitionID = @1", userId, propertyId)
                .FirstOrDefault();

            if (existing == null)
            {
                repo.Insert(new UserProfileData
                {
                    UserID = userId,
                    PropertyDefinitionID = propertyId,
                    PropertyValue = value,
                    PropertyText = value,
                    Visibility = 2,
                    LastUpdatedDate = DateTime.Now
                });
            }
            else
            {
                existing.PropertyValue = value;
                existing.PropertyText = value;
                existing.LastUpdatedDate = DateTime.Now;
                repo.Update(existing);
            }
        }
    }

}
