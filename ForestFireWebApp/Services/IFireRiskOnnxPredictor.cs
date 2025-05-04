namespace ForestFireWebApp.Services
{
    public interface IFireRiskOnnxPredictor
    {
        float Predict(float[] features);
    }
}
