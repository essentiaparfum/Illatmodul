// Controllers/ItemController.cs


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Illamdul.Dnn.Illatmodul.Models;
using DotNetNuke.Common;
using DotNetNuke.Entities.Users;
using DotNetNuke.Entities.Profile;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Web.Mvc.Framework.Controllers;

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
            var questions = new List<QuestionModel>
            {
                new QuestionModel {
                    QuestionNumber = 1,
                    QuestionText   = "Milyen alkalomra keresel illatot?",
                    Answers        = new List<string>
                    {
                        "Minden napra", "Elegáns eseményre", "Munkahelyre",
                        "Esti programra", "Ajándéknak"
                    }
                },
                new QuestionModel {
                    QuestionNumber = 2,
                    QuestionText   = "Milyen hatást szeretnél kelteni az illattal?",
                    Answers        = new List<string>
                    {
                        "Enyhe, friss", "Kellemesen észrevehető",
                        "Erőteljes, karakteres"
                    }
                },
                new QuestionModel {
                    QuestionNumber = 3,
                    QuestionText   = "Melyik illatok tetszenek inkább?",
                    Answers        = new List<string>
                    {
                        "Friss, citrusos", "Virágos", "Édes (vanília, karamell)",
                        "Meleg, fűszeres", "Nem tudom / segítsetek választani"
                    }
                },
                new QuestionModel {
                    QuestionNumber = 4,
                    QuestionText   = "Mikor használnád leginkább?",
                    Answers        = new List<string>
                    {
                        "Tavasz", "Nyár", "Ősz", "Tél", "Egész évben"
                    }
                },
                new QuestionModel {
                    QuestionNumber = 5,
                    QuestionText   = "Melyik stílus áll hozzád közel?",
                    Answers        = new List<string>
                    {
                        "Sportos", "Elegáns", "Romantikus", "Fiatalos", "Letisztult"
                    }
                },
                new QuestionModel {
                    QuestionNumber = 6,
                    QuestionText   = "Szereted, ha mások megjegyzik az illatodat?",
                    Answers        = new List<string>
                    {
                        "Igen, szeretem, ha feltűnő",
                        "Nem, inkább csak én érezzem",
                        "Valami a kettő között lenne jó"
                    }
                },
                new QuestionModel {
                    QuestionNumber = 7,
                    QuestionText   = "Mennyi ideig tartson az illat?",
                    Answers        = new List<string>
                    {
                        "Nem baj, ha hamar elillan, csak legyen friss",
                        "Fontos, hogy egész nap tartson",
                        "Nincs preferenciám"
                    }
                },
                
                
            };

            if (id < 1 || id > questions.Count)
                return RedirectToAction("Eredmeny");

            return View("Kerdes", questions[id - 1]);
        }

        // 3) POST Kérdés – válasz gyűjtés és lapozás a DNN URL-generátorral
        [HttpPost]
        public ActionResult Kerdes(int id = 1, string valasz = null)
        {
            const string key = "IllatValaszok";
            var list = Session[key] as List<string> ?? new List<string>();

            if (!string.IsNullOrEmpty(valasz))
            {
                list.Add(valasz);
                Session[key] = list;
            }

            var next = id + 1;
            if (next <= 7)
            {
                string url = Globals.NavigateURL(
                    PortalSettings.ActiveTab.TabID,
                    "Kerdes",
                    $"mid={ModuleContext.ModuleId}",
                    $"id={next}"
                );
                return Redirect(url);
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
                var answers = Session["IllatValaszok"] as List<string>;
                if (answers == null)
                {
                    answers = new List<string>();
                }

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

                var scores = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var cat in categories) scores[cat] = 0;

                foreach (var answer in answers)
                {
                    if (map.ContainsKey(answer))
                    {
                        var matched = map[answer];
                        foreach (var c in matched)
                        {
                            if (scores.ContainsKey(c)) scores[c]++;
                        }
                    }
                }

                string top1 = null;
                string top2 = null;
                int max1 = -1, max2 = -1;

                foreach (var kvp in scores)
                {
                    if (kvp.Value > max1)
                    {
                        max2 = max1;
                        top2 = top1;
                        max1 = kvp.Value;
                        top1 = kvp.Key;
                    }
                    else if (kvp.Value > max2)
                    {
                        max2 = kvp.Value;
                        top2 = kvp.Key;
                    }
                }

                var user = UserController.Instance.GetCurrentUserInfo();
                var userId = user.UserID;

                var ctx = DotNetNuke.Data.DataContext.Instance();
                var repo = ctx.GetRepository<Illamdul.Dnn.Illatmodul.Models.UserProfileData>();

                int topId = 45; // TopIllatkategoria
                int secId = 46; // SecIllatkategoria

                if (!string.IsNullOrEmpty(top1))
                {
                    var top = repo.Find("WHERE UserID = @0 AND PropertyDefinitionID = @1", userId, topId).FirstOrDefault();
                    if (top == null)
                    {
                        top = new Illamdul.Dnn.Illatmodul.Models.UserProfileData
                        {
                            UserID = userId,
                            PropertyDefinitionID = topId,
                            PropertyValue = top1,
                            PropertyText = top1, // új!
                            Visibility = 2,
                            LastUpdatedDate = DateTime.Now
                        };
                        repo.Insert(top);
                    }
                    else
                    {
                        top.PropertyValue = top1;
                        top.PropertyText = top1; // új!
                        top.LastUpdatedDate = DateTime.Now;
                        repo.Update(top);
                    }
                }

                if (!string.IsNullOrEmpty(top2))
                {
                    var sec = repo.Find("WHERE UserID = @0 AND PropertyDefinitionID = @1", userId, secId).FirstOrDefault();
                    if (sec == null)
                    {
                        sec = new Illamdul.Dnn.Illatmodul.Models.UserProfileData
                        {
                            UserID = userId,
                            PropertyDefinitionID = secId,
                            PropertyValue = top2,
                            PropertyText = top2, // új!
                            Visibility = 2,
                            LastUpdatedDate = DateTime.Now
                        };
                        repo.Insert(sec);
                    }
                    else
                    {
                        sec.PropertyValue = top2;
                        sec.PropertyText = top2; // új!
                        sec.LastUpdatedDate = DateTime.Now;
                        repo.Update(sec);
                    }
                }

                var resultList = new List<string>();
                if (!string.IsNullOrEmpty(top1)) resultList.Add(top1);
                if (!string.IsNullOrEmpty(top2)) resultList.Add(top2);
                ViewBag.TopCategories = resultList;

                Session.Remove("IllatValaszok");
                return View("Eredmeny");
            }
            catch (Exception ex)
            {
                return Content("❌ Hiba történt:<br/><strong>" + ex.Message + "</strong><br/><pre>" + ex.StackTrace + "</pre>", "text/html");
            }
        }


    }
}
