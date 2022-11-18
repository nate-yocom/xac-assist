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
        bool AllowAxisHoldToFlow { get; set; }
        int AxisHoldToFlowHoldTimeMilliseconds { get; set; }
        void ReadConfiguration();
        void FromJSON(string jsonText);
        void Save();
    }
}