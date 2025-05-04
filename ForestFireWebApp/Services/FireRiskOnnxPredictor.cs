using ForestFireWebApp.Models;
using Microsoft.ML;

namespace ForestFireWebApp.Services
{
    public class FireRiskOnnxPredictor: IFireRiskOnnxPredictor
    {
        private readonly PredictionEngine<ForestFireInput, ForestFirePrediction> _predictionEngine;

        public FireRiskOnnxPredictor()
        {
            var mlContext = new MLContext();
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModels", "fire_risk.onnx");
            var pipeline = mlContext.Transforms.ApplyOnnxModel(
                modelFile: modelPath,
                inputColumnNames: new[] { "float_input" },
                outputColumnNames: new[] { "variable" }
            );

            var emptyData = mlContext.Data.LoadFromEnumerable(new List<ForestFireInput>());
            var model = pipeline.Fit(emptyData);

            _predictionEngine = mlContext.Model.CreatePredictionEngine<ForestFireInput, ForestFirePrediction>(model);
        }

        public float Predict(float[] features)
        {
            var input = new ForestFireInput { Features = features };
            var prediction = _predictionEngine.Predict(input);
            return prediction.PredictedFires[0];
        }
    }
}
