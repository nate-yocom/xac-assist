using System.Collections.ObjectModel;
using System.Diagnostics;

using XacAssist.Features;


namespace XacAssist.Pipeline {
    public interface IPipeline {
        void Start();
        void Tick();
        void Stop();

        Stopwatch TimeSinceLastInput();

        ReadOnlyCollection<Feature> Features { get; }
    }
}