using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using SixLabors.ImageSharp;

using System.Diagnostics;

namespace XacAssist.Features {

    public abstract class Feature {
        protected Joystick? _inputJoystick;
        protected SimpleJoystick? _outputJoystick;
        protected ILoggerFactory _loggerFactory;        

        private object _mutex = new object();
        private bool _dirty = false;

        public bool Dirty { 
            get { lock (_mutex) { return _dirty; } } 
            protected set { lock(_mutex) { _dirty = value; } }
        }

        // Feature current toggled on or off
        private bool _enabled = false;
        public bool Enabled { 
            get { lock(_mutex) { return _enabled; } } 
            set { lock(_mutex) { _enabled = value; _dirty = true; } }
        }

        // What button toggles this feature
        public int ToggleButton { get; set; }

        public abstract string Name { get; }
        
        public enum FeatureFilterAction {
            Continue        = 0x00, // Default, continue to the next feature, nothing to see here, move along.  Original event will flow.
            Stop            = 0x01, // Done, no more action, skip all remaining features, do NOT flow the original event
            Swallowed       = 0x02, // Continue on to remaining features, but do NOT flow the original event
        }

        public Feature(ILoggerFactory loggerFactory, Joystick inputJoystick, SimpleJoystick outputJoystick) {
            _loggerFactory = loggerFactory;
            _inputJoystick = inputJoystick;
            _outputJoystick = outputJoystick;
        }

        // Feature's Start, Tick, and Stop are called always.
        public abstract void Start();
        public abstract void Tick();
        
        public abstract void Stop();

        public virtual void TickFrame(Image frame) {}        
        
        // Button and Axis filters are only called if Enabled == True
        public abstract FeatureFilterAction ButtonFilter(ref byte buttonId, ButtonEventTypes eventType, ref bool pressed, TimeSpan elapsed);
                
        public abstract FeatureFilterAction AxisFilter(ref byte axisId, ref short value, TimeSpan elapsed);        
    }
}