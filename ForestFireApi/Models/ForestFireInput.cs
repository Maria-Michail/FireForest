using Microsoft.ML.Data;

namespace ForestFireApi.Models
{
    public class ForestFireInput
    {
        [ColumnName("float_input")]
        [VectorType(7)]
        public float[] Features { get; set; }
    }


    public class ForestFirePrediction
    {
        [ColumnName("variable")]
        public float[] PredictedFires { get; set; }
    }

    public class FireRiskResult
    {
        public string Date { get; set; }
        public float Probability { get; set; }
        public string RiskLevel { get; set; }
    }
}
