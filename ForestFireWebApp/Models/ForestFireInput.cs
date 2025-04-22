using Microsoft.ML.Data;

namespace ForestFireWebApp.Models;
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
