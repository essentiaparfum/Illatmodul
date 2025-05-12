using DotNetNuke.ComponentModel.DataAnnotations;

namespace Illamdul.Dnn.Illatmodul.Models
{
    [TableName("PerfumeAnswers")]
    [PrimaryKey("AnswerID", AutoIncrement = true)]
    [Scope("ModuleId")]
    public class PerfumeAnswer
    {
        public int AnswerID { get; set; }
        public int QuestionID { get; set; }
        public string AnswerText { get; set; }
        public string ValueCode { get; set; } // lehet null
        public int SortOrder { get; set; }
    }
}
