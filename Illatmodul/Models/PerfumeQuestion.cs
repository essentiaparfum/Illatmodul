using DotNetNuke.ComponentModel.DataAnnotations;

namespace Illamdul.Dnn.Illatmodul.Models
{
    [TableName("PerfumeQuestions")]
    [PrimaryKey("QuestionID", AutoIncrement = true)]
    [Scope("ModuleId")]
    public class PerfumeQuestion
    {
        public int QuestionID { get; set; }
        public string QuestionText { get; set; }
        public int SortOrder { get; set; }
    }
}
