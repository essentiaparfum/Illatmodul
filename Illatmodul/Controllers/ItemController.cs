using DotNetNuke.Common;
using DotNetNuke.Data;
using DotNetNuke.Entities.Profile;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using Illamdul.Dnn.Illatmodul.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Illamdul.Dnn.Illatmodul.Controllers
{
    public class ItemController : DnnController
    {
        // 1) Alap nézet (Index)
        public ActionResult Index()
        {
            return View();
        }

        // 2) GET Kérdés
        public ActionResult Kerdes(int id = 1)
        {
            using (var ctx = DotNetNuke.Data.DataContext.Instance())
            {
                var questionRepo = ctx.GetRepository<PerfumeQuestion>();
                var answerRepo = ctx.GetRepository<PerfumeAnswer>();

                // 🔹 Dinamikus kérdésszám meghatározása és átadása a View-nak
                var totalCount = questionRepo.Get().Count();
                ViewBag.TotalQuestions = totalCount;

                // 🔹 Az aktuális kérdés lekérdezése SortOrder alapján
                var question = questionRepo.Find("WHERE SortOrder = @0", id).FirstOrDefault();

                if (question == null)
                {
                    return RedirectToAction("Eredmeny");
                }

                // 🔹 A válaszlehetőségek lekérése ehhez a kérdéshez
                var answers = answerRepo.Find("WHERE QuestionID = @0 ORDER BY SortOrder", question.QuestionID)
                                        .Select(a => a.AnswerText)
                                        .ToList();

                // 🔹 Modell összeállítása és átadása a View-nak
                var model = new QuestionModel
                {
                    QuestionNumber = id,
                    QuestionText = question.QuestionText,
                    Answers = answers
                };

                return View("Kerdes", model);
            }
        }

        // 3) POST Kérdés – válasz gyűjtés és lapozás a DNN URL-generátorral
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

            // 🔁 Dinamikus kérdésszám lekérése
            using (var ctx = DotNetNuke.Data.DataContext.Instance())
            {
                var questionRepo = ctx.GetRepository<PerfumeQuestion>();
                var totalCount = questionRepo.Get().Count();

                if (next <= totalCount)
                {
                    string url = Globals.NavigateURL(
                        PortalSettings.ActiveTab.TabID,
                        "Kerdes",
                        $"mid={ModuleContext.ModuleId}",
                        $"id={next}"
                    );
                    return Redirect(url);
                }
            }

            string eredmenyUrl = Globals.NavigateURL(
                PortalSettings.ActiveTab.TabID,
                "Eredmeny",
                $"mid={ModuleContext.ModuleId}"
            );
            return Redirect(eredmenyUrl);
        }

        // 4) Eredmény oldal – két legtöbb pontot elért alkategória + profilba mentés
        public ActionResult Eredmeny()
        {
            try
            {
                var answers = Session["Illatmodul_Valaszok"] as List<string> ?? new List<string>();

                var categories = new[]
                {
                    "Fás illatok", "Friss illatok", "Fűszeres illatok", "Púderes illatok",
                    "Édes illatok", "Gyümölcsös illatok", "Orientális illatok", "Virágos illatok"
                };

                var map = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "Friss, citrusos", new[]{ "Friss illatok" } },
                    { "Virágos", new[]{ "Virágos illatok" } },
                    { "Édes (vanília, karamell)", new[]{ "Édes illatok" } },
                    { "Meleg, fűszeres", new[]{ "Fűszeres illatok" } },
                    { "Sportos", new[]{ "Friss illatok", "Fás illatok" } },
                    { "Elegáns", new[]{ "Púderes illatok", "Orientális illatok" } },
                    { "Romantikus", new[]{ "Virágos illatok", "Édes illatok" } },
                    { "Fiatalos", new[]{ "Gyümölcsös illatok", "Friss illatok" } },
                    { "Letisztult", new[]{ "Púderes illatok", "Fás illatok" } },
                    { "Igen, szeretem, ha feltűnő", new[]{ "Orientális illatok" } },
                    { "Nem, inkább csak én érezzem", new[]{ "Friss illatok" } },
                    { "Valami a kettő között lenne jó", new[]{ "Gyümölcsös illatok", "Édes illatok" } },
                    { "Nem baj, ha hamar elillan, csak legyen friss", new[]{ "Friss illatok" } },
                    { "Fontos, hogy egész nap tartson", new[]{ "Orientális illatok" } },
                    { "Nincs preferenciám", new[]{ "Gyümölcsös illatok", "Édes illatok" } },
                };

                var scores = categories.ToDictionary(c => c, c => 0, StringComparer.InvariantCultureIgnoreCase);

                foreach (var answer in answers)
                {
                    if (map.TryGetValue(answer, out var matched))
                    {
                        foreach (var c in matched)
                        {
                            if (scores.ContainsKey(c)) scores[c]++;
                        }
                    }
                }

                string top1 = null, top2 = null;
                int max1 = -1, max2 = -1;

                foreach (var kvp in scores)
                {
                    if (kvp.Value > max1)
                    {
                        max2 = max1; top2 = top1;
                        max1 = kvp.Value; top1 = kvp.Key;
                    }
                    else if (kvp.Value > max2)
                    {
                        max2 = kvp.Value; top2 = kvp.Key;
                    }
                }

                var user = UserController.Instance.GetCurrentUserInfo();
                var userId = user.UserID;
                var ctx = DotNetNuke.Data.DataContext.Instance();
                var repo = ctx.GetRepository<UserProfileData>();

                int topId = 45;
                int secId = 46;

                SaveProfileValue(repo, userId, topId, top1);
                SaveProfileValue(repo, userId, secId, top2);

                ViewBag.TopCategories = new List<string> { top1, top2 }.Where(s => !string.IsNullOrEmpty(s)).ToList();
                Session.Remove("Illatmodul_Valaszok");

                return View("Eredmeny");
            }
            catch (Exception ex)
            {
                return Content("❌ Hiba történt:<br/><strong>" + ex.Message + "</strong><br/><pre>" + ex.StackTrace + "</pre>", "text/html");
            }
        }

        private void SaveProfileValue(IRepository<UserProfileData> repo, int userId, int propertyId, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            var existing = repo.Find("WHERE UserID = @0 AND PropertyDefinitionID = @1", userId, propertyId).FirstOrDefault();
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
