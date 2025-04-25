using System;
using DotNetNuke.ComponentModel.DataAnnotations;

namespace Illamdul.Dnn.Illatmodul.Models
{
    [TableName("UserProfile")]
    [PrimaryKey("ProfileID", AutoIncrement = true)]
    [Scope("UserID")]
    public class UserProfileData
    {
        public int ProfileID { get; set; } // <-- ez kell, nem UserProfileID!
        public int UserID { get; set; }
        public int PropertyDefinitionID { get; set; }
        public string PropertyValue { get; set; }
        public string PropertyText { get; set; } // optional, ha nem használsz ilyet, kihagyható
        public int? Visibility { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public int? ExtendedVisibility { get; set; }
    }
}
