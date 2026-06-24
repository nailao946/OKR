namespace ME.Models
{
    public class TimeTag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string Notes { get; set; }
        public int SortOrder { get; set; }
        public bool IsPreset { get; set; }

        public bool IsDefault => Name == "闲时";
    }
}
