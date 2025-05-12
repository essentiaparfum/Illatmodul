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

        // 3) POST Kérdés – ValueCode-okat gyűjtünk Session-be, aztán lapozunk
        [HttpPost]
        public ActionResult Kerdes(int id = 1, string valasz = null)
        {
            const string key = "Illatmodul_Valaszok";
            var list = Session[key] as List<string> ?? new List<string>();

            if (!string.IsNullOrEmpty(valasz))
            {
                list.Add(valasz);
                Session[key] = list;
            }

            var next = id + 1;
            using (var ctx = DataContext.Instance())
            {
                var total = ctx.GetRepository<PerfumeQuestion>().Get().Count();
                if (next <= total)
                {
                    // IDE KELL A REDIRECT
                    string url = Globals.NavigateURL(
                        PortalSettings.ActiveTab.TabID,
                        "Kerdes",
                        "mid=" + ModuleContext.ModuleId,
                        "id=" + next
                    );
                    return Redirect(url);
                }
            }

            // ha nincs több kérdés
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
                // 1) Sessionből előzőleg gyűjtött ValueCode-ok
                var answers = Session["Illatmodul_Valaszok"] as List<string>
                              ?? new List<string>();

                // 2) Előre definiált kategóriák
                var categories = new[]
                {
                    "Fás illatok","Friss illatok","Fűszeres illatok","Púderes illatok",
                    "Édes illatok","Gyümölcsös illatok","Orientális illatok","Virágos illatok"
                };

                // 3) Inicializáljuk a pontszámlálót
                var scores = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var cat in categories)
                    scores[cat] = 0;

                // 4) Csak akkor adunk +1-et, ha a beküldött kód megegyezik egy kategóriával
                foreach (var code in answers)
                {
                    if (!string.IsNullOrEmpty(code) && scores.ContainsKey(code))
                        scores[code]++;
                }

                // 5) Két legtöbb pontot kapott kategória kiválasztása
                string top1 = null, top2 = null;
                int max1 = -1, max2 = -1;
                foreach (var kvp in scores)
                {
                    int pts = kvp.Value;
                    if (pts > max1)
                    {
                        max2 = max1; top2 = top1;
                        max1 = pts; top1 = kvp.Key;
                    }
                    else if (pts > max2)
                    {
                        max2 = pts; top2 = kvp.Key;
                    }
                }

                // 6) Mentés a felhasználó profiljába
                var user = UserController.Instance.GetCurrentUserInfo();
                var userId = user.UserID;
                using (var db = DataContext.Instance())
                {
                    var repo = db.GetRepository<UserProfileData>();
                    SaveProfileValue(repo, userId, 45, top1);
                    SaveProfileValue(repo, userId, 46, top2);
                }

                // 7) Nézetnek átadott lista
                var result = new List<string>();
                if (!string.IsNullOrEmpty(top1)) result.Add(top1);
                if (!string.IsNullOrEmpty(top2)) result.Add(top2);
                ViewBag.TopCategories = result;

                // Session törlése
                Session.Remove("Illatmodul_Valaszok");
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

        // Segédfüggvény a profilérték mentéséhez
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
