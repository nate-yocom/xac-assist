using System.Collections.ObjectModel;

using XacAssist.Features;

namespace XacAssist.Pipeline {
    public interface IPipeline {
        void Start();
        void Tick();
        void Stop();

        ReadOnlyCollection<Feature> Features { get; }
    }
}