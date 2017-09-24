using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;

namespace Fencing
{
    /// <summary>
    /// This is when the computer controls a character.  Like KeyboardController, AI is always
    /// "plugged-in".  But unlike KeyboardController and PadController, button-to-function mapping
    /// is not needed.  Gameplay functions are called directly.  It does need to know which character
    /// it is controlling, however, because it has a lot of thinking to do.
    /// It may make use of the tutorial information to make decisions.
    /// </summary>
    public class AIController : Controller
    {
        private GameDifficulty Difficulty = GameDifficulty.Normal;
        private Character me;

        public AIController(Game game, GameDifficulty d, Character c) : base(game)
        {
            Difficulty = d;
            me = c;
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
#if DEBUG
            if (GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.A)) OnTake(1f);
#endif
            if (Fencing.blade_taken == me) OnTake(1f);
            if (oscillateDir == +1)
            {
                OnPose(new Vector2(1f, (float)Difficulty - 2f));
//                if (gameTime.TotalGameTime.Seconds % 5 == 0)
  //                  oscillateDir = -1;
            }
            else
            {
                OnPose(new Vector2(-1f,-1f));
                if (gameTime.TotalGameTime.Seconds % 4 == 0)
                {
                    oscillateDir = +1;
                    //OnTake(1f);
                }
            }
            base.Update(gameTime);
        }
        private int oscillateDir = +1;
    }
}
