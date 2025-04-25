// Controllers/ItemController.cs
using DotNetNuke.Common;
using DotNetNuke.Entities.Tabs;
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
            // Ha van még kérdés, navigáljunk a következőre
            if (next <= 7)  // 4 helyett a kérdések számát is használhatod
            {
                string url = Globals.NavigateURL(
                    PortalSettings.ActiveTab.TabID,
                    "Kerdes",                                // controlKey
                    $"mid={ModuleContext.ModuleId}",         // modul azonosító
                    $"id={next}"                             // következő kérdés
                );
                return Redirect(url);
            }

            // Ha vége, eredmény oldal
            string eredmenyUrl = Globals.NavigateURL(
                PortalSettings.ActiveTab.TabID,
                "Eredmeny",
                $"mid={ModuleContext.ModuleId}"
            );
            return Redirect(eredmenyUrl);
        }

        // 4) Eredmény oldal – két legtöbb pontot elért alkategória
        public ActionResult Eredmeny()
        {
            // A) Válaszok előkeresése
            var answers = Session["IllatValaszok"] as List<string> ?? new List<string>();


            // B) Alkategóriák listája
            var categories = new[]
            {
                "Fás illatok",
                "Friss illatok",
                "Fűszeres illatok",
                "Púderes illatok",
                "Édes illatok",
                "Gyümölcsös illatok",
                "Orientális illatok",
                "Virágos illatok"
            };

            // C) Válasz → kategória(ák) mapping
            var map = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
            {
                // 3. kérdés válaszai
                { "Friss, citrusos",          new[]{ "Friss illatok" } },
                { "Virágos",                  new[]{ "Virágos illatok" } },
                { "Édes (vanília, karamell)", new[]{ "Édes illatok" } },
                { "Meleg, fűszeres",          new[]{ "Fűszeres illatok" } },

                // 5. kérdés stílus válaszok például
                { "Sportos",   new[]{ "Friss illatok", "Fás illatok" } },
                { "Elegáns",   new[]{ "Púderes illatok", "Orientális illatok" } },
                { "Romantikus",new[]{ "Virágos illatok", "Édes illatok" } },
                { "Fiatalos",  new[]{ "Gyümölcsös illatok", "Friss illatok" } },
                { "Letisztult",new[]{ "Púderes illatok", "Fás illatok" } },

                // 6. kérdés
                { "Igen, szeretem, ha feltűnő",   new[]{ "Orientális illatok" } },
                { "Nem, inkább csak én érezzem",  new[]{ "Friss illatok" } },
                { "Valami a kettő között lenne jó", new[]{ "Gyümölcsös illatok", "Édes illatok" } },

                // 7. kérdés
                { "Nem baj, ha hamar elillan, csak legyen friss", new[]{ "Friss illatok" } },
                { "Fontos, hogy egész nap tartson",               new[]{ "Orientális illatok" } },
                { "Nincs preferenciám",                           new[]{ "Gyümölcsös illatok", "Édes illatok" } },

                
            };

            // D) Pontszámok inicializálása
            var scores = categories.ToDictionary(cat => cat, cat => 0, StringComparer.InvariantCultureIgnoreCase);

            // E) Válaszok pontozása
            foreach (var answer in answers)
            {
                if (map.TryGetValue(answer, out var cats))
                {
                    foreach (var c in cats)
                        if (scores.ContainsKey(c))
                            scores[c]++;
                }
            }

            // F) Top 2 kiválasztása
            var top2 = scores
                .OrderByDescending(kvp => kvp.Value)
                .Take(2)
                .Select(kvp => kvp.Key)
                .ToList();

            ViewBag.TopCategories = top2;
            Session.Remove("IllatValaszok");
            return View("Eredmeny");
        }
    }
}

