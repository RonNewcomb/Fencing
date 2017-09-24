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
    /// This is the game itself, the highest-level class.  Start here.
    // XNA uses a right-handed coordinate system: the positive x-axis points right, 
    // the positive y-axis points up, and the positive z-axis points toward the observer.
    // But the above perspective matrix negates Y, so Y is mirrored
    /// In this game: .
    /// -Y moves everything *downward*
    /// +Y moves everything *upward*
    // Camera is at (0,0,+1) "backward", looking toward (0,0,0) and is upright.
    // When camera changes x,y, I'll also change looking-at's x,y to match so that
    // the camera *always* looks down the Z-axis (decreasing Z, moving away from camera).
    // Moving the camera itself:
    /// -CameraAt.Z zooms *closer* to the action
    /// +CameraAt.Z zooms *away* from the action
    /// -CameraAt.X moves everthing *right*
    /// +CameraAt.X moves everything *left*
    /// </summary>
    public class Fencing : Microsoft.Xna.Framework.Game
    {
        public GraphicsDeviceManager graphics;
        public BasicEffect basicEffect;
        SamplerState ss = new SamplerState();
        Texture2D BackgroundImage;
        public Vector3 CameraAt = Vector3.Backward; // (0,0,+1)
        public Character left, right;
        PadController pad1, pad2;
        AIController AI1;
        KeyboardController key1;

        static public Nullable<Vector3> swordLinesCrossedAt = null;
        static public Character blade_taken = null;
        static public Vector3 wall_at;
//        static public float trapped_on;
        static public void VibrationFireworks(Character character, float heavy, float light)
        {
            if (character.myController is PadController)
                GamePad.SetVibration((character.myController as PadController).myPlayer, heavy, light);
        }

        /// <summary>Take is an unusual gameplay action because it ties both fencer's blades together 
        /// for an extended time.  Hence the game itself needs to be informed by the character classes
        /// when a fencer has initiated (successfully) this mode.  But the mode can be canceled in
        /// three ways. One, the aggressor can release the button.  Two, the other fencer can take 
        /// defensive action, such as a deceive to untangle himself.  Or three, the fencers may 
        /// drift far enough apart that the blades can no longer touch.</summary>
        static public void PriseDeFer(Character aggressor)
        {
            // RULE 1: There is always a Taker and a Takee. The mode is asymmetric.
            if (aggressor == null)
                return;
            // RULE 2: Take mode immediately ends if the blades are no longer crossed. 
            if (swordLinesCrossedAt == null)
                PriseDeFerFin(aggressor);
            // SUCCESS: Put Take into effect, remembering the Taker and the point on his blade which blocks.
            else
            {
                // DEFINITION: blade_taken is the person who is taking the other's blade.
                blade_taken = aggressor;
                // RULE (Solid Objects): wall_at mathematically ensures solid objects are solid in Character.Draw()
                wall_at = Vector3.Lerp(aggressor.jointsForCollision[(int)Character.JointNames.wrist], aggressor.jointsForCollision[(int)Character.JointNames.swordtip], 0.5f);
                // RULE: tied blades cause controller vibration. (So begin the vibration.)
                VibrationFireworks(aggressor, 0f, 0.5f);
                VibrationFireworks(aggressor.Target, 0f, 0.5f);
            }
        }

        /// <summary>When a Take ends either because the aggressor let off the button or because
        /// the blades no longer touch, this turns off the mode.</summary>
        static public void PriseDeFerFin(Character aggressor)
        {
            // RULE 1: Releasing your TAKE button when your opponent has taken your blade won't
            // cancel the mode, of course.
            if (aggressor == null || aggressor != blade_taken)
                return;
            // SUCCESS: turn off take mode. Three steps: 
            // RULE 1 (Solid Objects): if a fencer's blade is tied up by his opponent, and he tries to
            //   move his blade through the (solid) opposing blade, his wrist changes angle to 
            //   ensure solid objects don't pass through one another. This now no longer applies.
            aggressor.Target.deflected_wrist = false;
            // RULE 2: tied blades cause controller vibration. (So untied blades stop the vibration.)
            VibrationFireworks(aggressor, 0f, 0f);
            VibrationFireworks(aggressor.Target, 0f, 0f);
            // DEFINITION: blade_taken is the person who is taking the other's blade.
            blade_taken = null;
        }

        public Fencing()  // constructor
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
#if DEBUG
            IsMouseVisible = true;
#endif
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            ss.AddressU = ss.AddressV = TextureAddressMode.Clamp;
            graphics.GraphicsDevice.SamplerStates[0] = ss; // this paragraph to silence the compiler

            basicEffect = new BasicEffect(GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.World = Matrix.Identity;
            basicEffect.View = Matrix.CreateLookAt(Vector3.Backward, Vector3.Zero, Vector3.Up);
            basicEffect.Projection = Matrix.CreatePerspective(GraphicsDevice.Viewport.Width,
                -GraphicsDevice.Viewport.Height, 1f, 1000f);

            left = new Character(this,0);
            right = new Character(this,1);
            left.Target = right;  // for convenience's sake. 
            right.Target = left;
            Components.Add(left); // add to empty Draw() and Update() rulebooks
            Components.Add(right);

            key1 = new KeyboardController(this);
            Components.Add(key1);
            AI1 = new AIController(this, GameDifficulty.Normal, left);
            Components.Add(AI1);

            if (GamePad.GetState(PlayerIndex.One).IsConnected)
            {
                pad1 = new PadController(this, PlayerIndex.One, left);
                Components.Add(pad1);
                Bind(pad1, left);
            }
            else
                Bind(key1, left);

            if (GamePad.GetState(PlayerIndex.Two).IsConnected)
            {
                pad2 = new PadController(this, PlayerIndex.Two, right);
                Components.Add(pad2);
                Bind(pad2, right);
            }
            else
                Bind(AI1, right);

            base.Initialize(); // initialize other components
        }

        /// <summary>This function decides what device controls who. It does NOT assign 
        /// individual buttons to functions.  See XxxController.Bind() for that.</summary>
        /// <param name="input">Any derived instance: AIController, PadController, or KeyboardController.</param>
        /// <param name="character">"Left" and "right" are the two characters.</param>
        protected void Bind(Controller input, Character character)
        {
            character.myController = input;
            input.Pose += character.Pose;
            input.Advance += character.Advance;
            input.Parry += character.Parry;
            input.Deceive += character.Deceive;
            input.Invert += character.Invert;
            input.Take += character.Take;
            input.EndTake += character.EndTake;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) this.Exit();
#if DEBUG
            KeyboardState k = Keyboard.GetState();
            if (k.IsKeyDown(Keys.R)) { left.Initialize(); right.Initialize(); }
#endif
            CameraAt.X = (right.Location.X + left.Location.X) / 2f; // place camera at average of  
            // characters are drawn "standing on the origin", so move them down (the camera upward) by
            // 30% so their feet are visible a little above the edge of screen
            CameraAt.Y = (float)GraphicsDevice.Viewport.Height * -0.30f;
            CameraAt.Z = (right.Location.X - left.Location.X) / 700f; // characters start 600 apart...
            if (CameraAt.Z < 1f) CameraAt.Z = 1f;
            basicEffect.View = Matrix.CreateLookAt(CameraAt, new Vector3(CameraAt.X, CameraAt.Y, 0f), Vector3.Up); // actually only need to change View.Transform, but compiler can't do so directly. (Chained structs problem.)

            base.Update(gameTime);
        }

        /// <summary>This is called when the game should draw itself.</summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // clear the screen
            GraphicsDevice.Clear(Color.Gray);

            // render background
            basicEffect.TextureEnabled = true;
            basicEffect.Texture = BackgroundImage;
            basicEffect.World = Matrix.CreateTranslation(new Vector3(0f, GraphicsDevice.Viewport.Width * -0.90f, 0f));
            basicEffect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, backgroundQuad, 0, 2);

            // Calculate where the swords are crossed.
            BoundingBox l = left.SwordBox(), r = right.SwordBox(); // aliases required, it seems
            swordLinesCrossedAt = Geometry.LineSegmentIntersection(l, r);

            // Update or turn off TAKE mode, if applicable.
            if (blade_taken != null)
                PriseDeFer(blade_taken);
            DrawSparkAt(wall_at);

            // draw all other DrawableGameComponents (such as the 2 characters)
            base.Draw(gameTime);
        }
        private VertexPositionColorTexture[] backgroundQuad = { // 1st vertex in clockwise order!
            new VertexPositionColorTexture(new Vector3(-2048,   0, 0), Color.Tan, new Vector2(0f,0f)),
            new VertexPositionColorTexture(new Vector3( 2048,   0, 0), Color.Tan, new Vector2(1f,0f)), 
            new VertexPositionColorTexture(new Vector3(-2048,2048, 0), Color.Tan, new Vector2(0f,1f)), 
            new VertexPositionColorTexture(new Vector3( 2048,2048, 0), Color.Tan, new Vector2(1f,1f)) 
        };
        private VertexPositionColorTexture[] sparkQuad = { // 1st vertex in clockwise order!
            new VertexPositionColorTexture(new Vector3(-10,-10, 0), Color.Tan, new Vector2(0f,0f)),
            new VertexPositionColorTexture(new Vector3( 10,-10, 0), Color.Tan, new Vector2(1f,0f)), 
            new VertexPositionColorTexture(new Vector3(-10, 10, 0), Color.Tan, new Vector2(0f,1f)), 
            new VertexPositionColorTexture(new Vector3( 10, 10, 0), Color.Tan, new Vector2(1f,1f)) 
        };

        private void DrawSparkAt(Vector3 here)
        {
            basicEffect.TextureEnabled = true;
            basicEffect.Texture = BackgroundImage;
            basicEffect.World = Matrix.CreateTranslation(here);
            basicEffect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, sparkQuad, 0, 2);
        }


        /// <summary>The camera listens for a viewport-changed-size message.  Anything that depends 
        /// on the viewport size needs be re-initialized in here.</summary>
        /// <param name="sender">The object sending the window-resize message.</param>
        /// <param name="e">The parameters passed from the sender to this (or any) listener.</param>
        public void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            CameraAt.Y = (float)GraphicsDevice.Viewport.Height * -0.30f;
            basicEffect.Projection = Matrix.CreatePerspective(GraphicsDevice.Viewport.Width,
                -GraphicsDevice.Viewport.Height, 1f, 1000f);
        }


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            BackgroundImage = Content.Load<Texture2D>("background");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

    }
}
