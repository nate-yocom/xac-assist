namespace XacAssist.JitM {
    public interface IPipelineConfig {
        string InputDevice { get; set; }
        string OutputDevice { get; set; }
        void ReadConfiguration();
        void FromJSON(string jsonText);
        void Save();
    }
}