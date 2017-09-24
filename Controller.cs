using System;
using Microsoft.Xna.Framework;

namespace Fencing
{
    /// <summary>
    /// This is the parent of KeyboardController, PadController, and AIController. It publishes the 
    /// gameplay events Parry, Deceive, Advance, etc. that they use.  
    /// There's two kinds of bindings in effect:  binding a Character to a derived Controller, and 
    /// except for AIController, binding individual buttons (or thumbsticks) to gameplay functions.
    /// </summary>
    public class Controller : Microsoft.Xna.Framework.GameComponent
    {
        public enum GameFunction { parry, deceive, invert, take, advance, retreat, pose, Count };

        /// <summary>
        /// This is the parent of KeyboardController, PadController, and AIController. It publishes the 
        /// gameplay events Parry, Deceive, Advance, etc. that they use.  Please instantiate one of
        /// those classes.
        /// </summary>
        public Controller(Game game) : base(game) { }

        /// <summary>Thrust, Parry, Deceive: rock, paper, scissors.</summary>
        public event Action<float> Parry;
        public event Action<float> Deceive;
        /// <summary>Thumbstick can hold it at intermediate positions, keyboard picks a keyframe.</summary>
        public event Action<Vector2> Pose;
        /// <summary>L2 or R2 trigger button can keep this partly inverted.</summary>
        public event Action<float> Invert;
        /// <summary>Tries to catch the opposing blade so it can be pushed elsewhere.</summary>
        public event Action<float> Take;
        /// <summary>Tries to catch the opposing blade so it can be pushed elsewhere.</summary>
        public event Action EndTake;
        /// <summary>Thumbstick can hold it at intermediate speeds, but keyboard uses max values always.</summary>
        public event Action<float> Advance;

        /// <summary>This is how the derived classes fire the related events.</summary>
        protected void OnParry(float amount)    { if (Parry   != null) Parry(amount);   }
        protected void OnDeceive(float amount)  { if (Deceive != null) Deceive(amount); }
        protected void OnInvert(float amount)   { if (Invert  != null) Invert(amount);  }
        protected void OnTake(float amount)     { if (Take    != null) Take(amount);    }
        protected void OnEndTake()              { if (EndTake != null) EndTake();       }
        protected void OnAdvance(float amount)  { if (Advance != null) Advance(amount); }
        protected void OnPose(Vector2 keyframe) { if (Pose    != null) Pose(keyframe);  }

    }
}
