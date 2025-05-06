// Models/QuestionModel.cs
using System.Collections.Generic;

namespace Illamdul.Dnn.Illatmodul.Models
{
    public class QuestionModel
    {
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; }
        public List<string> Answers { get; set; }
    }
}
