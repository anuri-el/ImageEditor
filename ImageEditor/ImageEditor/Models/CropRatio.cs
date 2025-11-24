namespace ImageEditor.Models
{
    public class CropRatio
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsFreeform { get; set; }

        public override string ToString() => Name;

        public static CropRatio[] GetPredefinedRatios()
        {
            return new[]
            {
                new CropRatio { Name = "Freeform", Width = 0, Height = 0, IsFreeform = true },
                new CropRatio { Name = "1:1 (Square)", Width = 1, Height = 1 },
                new CropRatio { Name = "4:3", Width = 4, Height = 3 },
                new CropRatio { Name = "3:4 (Portrait)", Width = 3, Height = 4 },
                new CropRatio { Name = "16:9 (Widescreen)", Width = 16, Height = 9 },
                new CropRatio { Name = "9:16 (Vertical)", Width = 9, Height = 16 },
                new CropRatio { Name = "21:9 (Cinema)", Width = 21, Height = 9 },
                new CropRatio { Name = "Custom", Width = 0, Height = 0, IsFreeform = false }
            };
        }
    }
}
