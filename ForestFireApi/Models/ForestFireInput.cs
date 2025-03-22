using Microsoft.ML.Data;

namespace ForestFireApi.Models
{
    public class ForestFireInput
    {
        [ColumnName("float_input")]
        [VectorType(10)]
        public float[] Features { get; set; }
    }


    public class ForestFirePrediction
    {
        [ColumnName("probabilities")]
        public float[] Probabilities { get; set; }
    }
}
