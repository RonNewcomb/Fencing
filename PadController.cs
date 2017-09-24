using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace Fencing
{
    /// <summary>
    /// This is when the player uses a gamepad.  Gamepads may be plugged-in or not, sometimes 
    /// mid-match. But there's only so many buttons to use, and there are analog sticks.
    /// It's possible to expose an event for every button on the gamepad, for instance.
    /// Like KeyboardController, button-to-function mapping is needed.
    /// </summary>
    public class PadController : Controller
    {
        public PlayerIndex myPlayer;
        private Character me;
        public int LightVibrationTimer = -1, HeavyVibrationTimer = -1;
        private ButtonState[] ButtonPreviously = new ButtonState[(int)GameFunction.Count];
        public Buttons[] Bindings = new Buttons[(int)GameFunction.Count] 
        {   Buttons.RightTrigger,       // parry
            Buttons.RightShoulder,      // deceive
            Buttons.LeftTrigger,        // invert
            Buttons.LeftShoulder,       // take
            Buttons.LeftStick,          // advance
            Buttons.LeftStick,          // retreat
            Buttons.RightStick          // pose
        };  // KeyboardController needs advance & retreat separately, hence listing LeftStick twice.


        /// <summary>This binds a particular button to a particular gameplay function.  With the 
        /// exception of advance and retreat, a function cannot be bound to multiple buttons and 
        /// a button cannot be bound to multiple functions.  If tried, the bindings swap places.</summary>
        /// <param name="act">The gameplay action the button should do.</param>
        /// <param name="button">The button to assign to the action.</param>
        public void Bind(GameFunction act, Buttons button)
        {
            int previousBinding = (int)act; // safe default value
            for (int i = 0; i < (int)GameFunction.Count; i++)
                if (Bindings[i] == button) // if the button we're assigning is already used elsewhere,
                    previousBinding = i;
            Bindings[previousBinding] = Bindings[(int)act]; // then swap it with whatever we're overwriting
            Bindings[(int)act] = button;
            if (act == GameFunction.advance) Bindings[(int)GameFunction.retreat] = button;
            if (act == GameFunction.retreat) Bindings[(int)GameFunction.advance] = button;
        }

        public PadController(Game game, PlayerIndex pi, Character character) : base(game)
        {
            myPlayer = pi;
            me = character;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            for(int i = 0; i < (int)GameFunction.Count; i++)
                ButtonPreviously[i] = ButtonState.Released;
            base.Initialize();
        }

        /// <summary>Reads the hardware, triggers gameplay events, decreases vibration timers.</summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            GamePadState Pad = GamePad.GetState(myPlayer, GamePadDeadZone.None);
         
            if (PositiveEdge(Pad, GameFunction.take)) 
                OnTake(1f);
            else if (Fencing.blade_taken == me && Pad.IsButtonUp(Bindings[(int)GameFunction.take]))
                OnEndTake();
            if (PositiveEdge(Pad, GameFunction.deceive)) OnDeceive(1f);
            OnInvert(Pad.Triggers.Left);
            if (Pad.Triggers.Right > 0.3f) OnParry(Pad.Triggers.Right);
            if (Math.Abs(Pad.ThumbSticks.Left.X) > 0.1f) OnAdvance(Pad.ThumbSticks.Left.X); 
            OnPose(Geometry.SquareTheCircle(Pad.ThumbSticks.Right)); // diagonals need "extending" to 1.0 

            if (LightVibrationTimer > 0)
            {
                if (--LightVibrationTimer == 0)
                    GamePad.SetVibration(myPlayer, 0f, 0f);
            }

            base.Update(gameTime);
        }

        /// <summary>Returns true only when the corresponding button has just been pressed.</summary>
        /// <param name="GamePadState">The sample read from the hardware.</param>
        /// <param name="GameFunction">The gameplay feature that is mapped to the button.</param>
        private bool PositiveEdge(GamePadState Pad, GameFunction f)
        {
            if (Pad.IsButtonUp(Bindings[(int)f]))
                ButtonPreviously[(int)f] = ButtonState.Released;
            else if (ButtonPreviously[(int)f] != ButtonState.Pressed)
            {
                ButtonPreviously[(int)f] = ButtonState.Pressed;
                return true;
            }
            return false;
        }

    }
}
