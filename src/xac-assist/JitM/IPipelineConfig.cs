namespace XacAssist.JitM {
    public interface IPipelineConfig {
        string InputDevice { get; set; }
        string OutputDevice { get; set; }
        HashSet<byte> FireAndResetAxes { get; set; }          
        int WaitToReset { get; set; }
        float FireThreshold { get; set; }
        float ResetThreshold { get; set; }
        bool IgnoreAllButtons { get; set; }
        bool IgnoreAllAxes { get; set; }
        HashSet<byte> IgnoredButtons { get; set; }
        HashSet<byte> IgnoredAxes { get; set; }        
        Dictionary<byte, byte> MappedButtons { get; set; }
        Dictionary<byte, byte> MappedAxes { get; set; }

        byte MapButtonIfMapped(byte inputButton);
        byte MapAxisIfMapped(byte inputAxis);
        bool IsIgnoreButton(byte inputButton);
        bool IsIgnoreAxis(byte inputAxis);

        void ReadConfiguration();
        void FromJSON(string jsonText);
        void Save();
    }
}