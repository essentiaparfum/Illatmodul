using System.Collections.Generic;

namespace Illamdul.Dnn.Illatmodul.Models
{
    public class QuestionModel
    {
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; }
        // most már PerfumeAnswer-eket tartalmazunk
        public List<PerfumeAnswer> Answers { get; set; }
    }
}

