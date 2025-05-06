namespace Illamdul.Dnn.Illatmodul.Models
{
    public class PerfumeRecommendation
    {
        public string Category { get; set; }  // pl. "Friss, citrusos"
        public string Name { get; set; }  // pl. "Citrus Breeze"
        public string ImageUrl { get; set; }  // relatív URL a DesktopModules/MVC/Illatmodul/Images alól
        public string ProductUrl { get; set; }  // link a webshop termékoldalára
    }
}
