using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Fencing
{
    /// <summary>
    /// This is when the player uses the keyboard.  Keyboards are always plugged-in, but there's
    /// no analog sticks. Like PadController, button-to-function mapping is needed, but anywhere from
    /// two to nine buttons will substitute for each thumbstick. There are too many buttons to expose
    /// each with an event.
    /// </summary>
    public class KeyboardController : Controller
    {
        public Keys[] Bindings = new Keys[(int)GameFunction.Count] 
        {   Keys.Space,     // parry
            Keys.S,         // deceive
            Keys.W,         // invert
            Keys.D2,        // take
            Keys.D,         // advance
            Keys.A,         // retreat
            Keys.NumPad5    // pose 
        };

        public KeyboardController(Game game) : base(game) { }

        /// <summary>
        /// This binds a particular keyboard button to a gameplay function.  A button can 
        /// only be unbound by binding a different button to that function.  The same button can be
        /// bound to multiple functions.
        /// </summary>
        /// <param name="act">Bind pose to Keys.Up or Keys.NumPad8 so that only one or the other can
        /// use that set of directions.  Anything else, and the numpad and arrows both are bound.</param>
        /// <param name="button">Bind pose to Keys.Up or Keys.NumPad8 so that only one or the other can
        /// use that set of directions.  Anything else, and the numpad and arrows both are bound.</param>
        public void Bind(GameFunction act, Keys button)
        {
            Bindings[(int)act] = button;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            KeyboardState k = Keyboard.GetState();

            // Bindable functions
            if (k.IsKeyDown(Bindings[(int)GameFunction.retreat])) OnAdvance(-1f);
            if (k.IsKeyDown(Bindings[(int)GameFunction.advance])) OnAdvance(+1f);
            if (k.IsKeyDown(Bindings[(int)GameFunction.parry  ])) OnParry(1f);
            if (k.IsKeyDown(Bindings[(int)GameFunction.invert ])) OnInvert(1f);
            if (k.IsKeyDown(Bindings[(int)GameFunction.take   ])) OnTake(1f);
            if (k.IsKeyDown(Bindings[(int)GameFunction.deceive])) OnDeceive(1f);

            Vector2 pose = new Vector2(0f, 0f);

            // Numpad to direct sword
            if (Bindings[(int)GameFunction.pose] != Keys.Up)
            {
                if (k.IsKeyDown(Keys.NumPad1)) pose += new Vector2(-1f,  1f);
                if (k.IsKeyDown(Keys.NumPad2)) pose += new Vector2( 0f,  1f);
                if (k.IsKeyDown(Keys.NumPad3)) pose += new Vector2( 1f,  1f);
                if (k.IsKeyDown(Keys.NumPad4)) pose += new Vector2(-1f,  0f);
                if (k.IsKeyDown(Keys.NumPad5)) pose += new Vector2( 0f,  0f);
                if (k.IsKeyDown(Keys.NumPad6)) pose += new Vector2( 1f,  0f);
                if (k.IsKeyDown(Keys.NumPad7)) pose += new Vector2(-1f, -1f);
                if (k.IsKeyDown(Keys.NumPad8)) pose += new Vector2( 0f, -1f);
                if (k.IsKeyDown(Keys.NumPad9)) pose += new Vector2( 1f, -1f);
/*                if (k.IsKeyDown(Keys.NumPad1)) OnPose(Character.PoseNames.close_low);
                if (k.IsKeyDown(Keys.NumPad2)) OnPose(Character.PoseNames.low);
                if (k.IsKeyDown(Keys.NumPad3)) OnPose(Character.PoseNames.far_low);
                if (k.IsKeyDown(Keys.NumPad4)) OnPose(Character.PoseNames.close_mid);
                if (k.IsKeyDown(Keys.NumPad5)) OnPose(Character.PoseNames.mid);
                if (k.IsKeyDown(Keys.NumPad6)) OnPose(Character.PoseNames.far_mid);
                if (k.IsKeyDown(Keys.NumPad7)) OnPose(Character.PoseNames.close_high);
                if (k.IsKeyDown(Keys.NumPad8)) OnPose(Character.PoseNames.high);
                if (k.IsKeyDown(Keys.NumPad9)) OnPose(Character.PoseNames.far_high);*/
            }

            // Arrow keys to direct sword
            if (Bindings[(int)GameFunction.pose] != Keys.NumPad8)
            {
                if (k.IsKeyDown(Keys.Up   )) pose += new Vector2( 0f, -1f);
                if (k.IsKeyDown(Keys.Down )) pose += new Vector2( 0f,  1f);
                if (k.IsKeyDown(Keys.Right)) pose += new Vector2( 1f,  0f);
                if (k.IsKeyDown(Keys.Left )) pose += new Vector2(-1f,  0f);

/*                int pose = 5;
                if (k.IsKeyDown(Keys.Up)) pose += 3;
                if (k.IsKeyDown(Keys.Down)) pose -= 3;
                if (k.IsKeyDown(Keys.Right)) pose += 1;
                if (k.IsKeyDown(Keys.Left)) pose -= 1;
                OnPose((Character.PoseNames)pose);*/
            }
            pose.Normalize();
            OnPose(pose);

            base.Update(gameTime);
        }
    }
}
