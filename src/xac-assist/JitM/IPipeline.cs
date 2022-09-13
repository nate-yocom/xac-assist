namespace XacAssist.JitM {
    public interface IPipeline {
        void Start();
        void Tick();
        void Stop();
    }
}