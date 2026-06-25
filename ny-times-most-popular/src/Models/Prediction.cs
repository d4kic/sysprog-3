using Microsoft.ML.Data;

namespace ny_times_most_popular.src.Models
{
    internal class Prediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedLabel { get; set; }    
    }
}
