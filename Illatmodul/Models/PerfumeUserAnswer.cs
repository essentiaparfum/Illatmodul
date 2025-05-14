using System;
using DotNetNuke.ComponentModel.DataAnnotations;

namespace Illamdul.Dnn.Illatmodul.Models
{
    [TableName("PerfumeUserAnswer")]
    [PrimaryKey("AnswerID", AutoIncrement = true)]
    [Scope("ModuleId")]
    public class PerfumeUserAnswer
    {
        public int AnswerID { get; set; }
        public int ModuleId { get; set; }
        public int UserID { get; set; }
        public int QuestionID { get; set; }
        public string AnswerValue { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
