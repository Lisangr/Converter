namespace Converter.Application.Models
{
    public struct VideoAdjustments
    {
        public float Brightness { get; set; }
        public float Contrast { get; set; }
        public float Saturation { get; set; }
        public float Gamma { get; set; }

        public bool IsGrayscale { get; set; }
        public bool IsSepia { get; set; } 
        public bool IsBlur { get; set; }
        public bool IsVignette { get; set; }
    }
}