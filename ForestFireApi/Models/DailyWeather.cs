namespace ForestFireApi.Models
{
    public class DailyWeather
    {
        public double RainMax { get; set; } = 0;
        public double TempAvgSum { get; set; } = 0;
        public int TempAvgCount { get; set; } = 0;
        public double TempMax { get; set; } = double.MinValue;
        public double TempMin { get; set; } = double.MaxValue;
        public int HumMax { get; set; } = int.MinValue;
        public int HumMin { get; set; } = int.MaxValue;
        public double WindMax { get; set; } = 0;
        public double WindAvgSum { get; set; } = 0;
        public int WindCount { get; set; } = 0;
    }

    public class DailyWeatherSummary
    {
        public double RainMax { get; set; }
        public double TempAvg { get; set; }
        public double TempMax { get; set; }
        public double TempMin { get; set; }
        public int HumMax { get; set; }
        public int HumMin { get; set; }
        public double WindMax { get; set; }
        public double WindAvg { get; set; }
    }
}
