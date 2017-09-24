using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Fencing
{
    public class Character : DrawableGameComponent, IUpdateable
    {
        public enum PoseNames { none, close_low, low, far_low, close_mid, mid, far_mid, close_high, high, far_high, Count };
        public enum JointNames { hip, shoulder, elbow, wrist, swordtip, Count };  //sternum, shoulder, weapon_elbow, weapon_hand, weapon_tip, sternum_again, off_shoulder, off_elbow, off_hand

        public Controller myController;
        private int rightside;
        private Vector2 currentPose;
        public Vector3[] jointsForCollision = new Vector3[(int)JointNames.Count];
        private Vector3 oldwrist, oldsword;
        public bool deflected_wrist = false;
        private float oldwristangle;
        public float[] jointAngles = new float[(int)JointNames.swordtip] { 0f, 30f, 0f, -15f };
        public Character Target { get; set; }
        public Vector3 Location;
        private BasicEffect matrices;
        private GraphicsDevice device;
        public int parry_timer = 0;
        public int deception_timer = 0;
        const int MAX_PARRY_TIMER = 64;
        const int MAX_DECEIVE_TIMER = 32;
        const int MAX_TAKE_TIMER = 40;
        const float limb_length = 100f;
        const float MAX_SPEED = 3f; // running speed
        const float CORPS_A_CORPS_DISTANCE = 50f; // "body-to-body" distance, the minimum dist that must separate the fencers

        public Character(Game g, int side_as_array_index) : base(g)
        {
            rightside = side_as_array_index;
            DrawOrder = side_as_array_index + 1; // the background has 0
            Visible = true;
            matrices = (g as Fencing).basicEffect;
            device = (g as Fencing).GraphicsDevice;
        }

        public override void Initialize()
        {
            Location = new Vector3(300f, 0f, 0f);
            if (rightside == 0) Location.X = -300f;
            currentPose = new Vector2(0f, 0f);
            oldwristangle = jointAngles[(int)JointNames.wrist];
        }

        public override void Update(GameTime gameTime)
        {
            // RULE 1: decrease the parry timer until it hits zero.
            // RULE 2: Shade the swordtip & blade in proportion to the remaining timer.
            //   (This is in accordance with Game Design Law #37: show the player
            //   what's going on.)
            if (parry_timer == 0) ColorizeLimb(JointNames.swordtip, 255, JointNames.wrist, 255);
            else                  ColorizeLimb(JointNames.swordtip, 128 - (--parry_timer) * 2, JointNames.wrist, 128);

            // RULE 1: decrease the deception timer until it hits zero.
            // RULE 2: Shade the forearm & blade base in proportion to the remaining timer.
            if (deception_timer == 0) ColorizeLimb(JointNames.wrist, 211, JointNames.wrist, 255);
            else                      ColorizeLimb(JointNames.wrist, 128 - (--deception_timer) * 2, JointNames.wrist, 128 - deception_timer * 2);

            // Adjust the character model one step closer to the intended keyframe.
            float DesiredJointAngle, difference;
            for (int the_joint = 0; the_joint < (int)JointNames.swordtip; the_joint++)
            {
                DesiredJointAngle = InterpolatedJointAngle(the_joint);
                difference = jointAngles[the_joint] - DesiredJointAngle;
                if (Math.Abs(difference) <= the_joint + 1) 
                    jointAngles[the_joint] = DesiredJointAngle;
                else if (difference < 0) 
                    jointAngles[the_joint] += the_joint + 1; // BALANCE THIS
                else if (difference > 0) 
                    jointAngles[the_joint] -= the_joint + 1; // BALANCE THIS // wrist faster than shoulder
            }

            // RULE 1 (Solid Objects): if a fencer's blade is tied up by his opponent, and he tries to
            //   move his blade through the (solid) opposing blade, his wrist changes angle to 
            //   ensure solid objects don't pass through one another.
            // DEFINITION of deflected_wrist: true when a fencer's blade is tied up by his opponent.
            if (deflected_wrist == true) 
            {   // Overrides our desired wrist angle with the angle that points toward the blade's contact point.
#if DEBUG
                Fencing.VibrationFireworks(this, 0f, 0f);
#endif
                float perfect_angle = Geometry.AngleFromPointAthruPointC(oldwrist, Fencing.wall_at);
                jointAngles[(int)JointNames.wrist] = MathHelper.Lerp(perfect_angle, oldwristangle, 0.5f);
            }

            oldwristangle = jointAngles[(int)JointNames.wrist]; // save for the next frame's Update()
            base.Update(gameTime);
        }

        /// <summary>Otherwise known as walking, it kills interactive narrative. Subscribes to the Controller event Advance. Also handles retreating.</summary>
        /// <param name="amount">A number from 1.0 (advance) to -1.0 (full retreat).</param>
        public void Advance(float amount)
        {
            // RULE 1: fencers cannot walk through each other. See minimum distance.
            if (amount > 0)  // if advancing,
                if (Math.Abs(Location.X - Target.Location.X) <= CORPS_A_CORPS_DISTANCE) // BALANCE THIS 50f
                    return; // prevent advancing past each other

            // RULE 2: fencers can move left and right, per the thumbstick.
            if (rightside == 1) amount = -amount;  // now + is go right, - is go left
            Location.X += amount * MAX_SPEED; // BALANCE THIS

            // RULE 3: fencers cannot move past the end of the stage.
            if (Location.X < -2000f) Location.X = -2000;
            if (Location.X > +2000f) Location.X = +2000;
        }

        /// <summary>After thrust, it's the second component of rock-paper-scissors. It listens to the Controller event Parry.</summary>
        /// <param name="amount">A number from 1.0 (full power) down to deadzone (0.3, least committed).</param>
        public void Parry(float amount)
        {
            // RULE 1: a fencer can't parry if he recently missed a deceive attempt.
            if (deception_timer > 0)
                return;
            // RULE 2: Since parry is on an analog button (xbox trigger), then allow the parry
            //   to commit more fully if the button is still on its way to the fully down position.
            else if (parry_timer > 0 && parry_timer < (int)(MAX_PARRY_TIMER * amount))
                parry_timer = (int)(MAX_PARRY_TIMER * amount);  
            // RULE 3: a fencer cannot parry if he recently missed a parry attempt. This must come 
            //    after the above rule.
            else if (parry_timer > 0) 
                return; 
            // RULE 4: if a fencer attempts to parry when the blades aren't crossed, then
            //    he has "missed a parry attempt".  
            else if (Fencing.swordLinesCrossedAt == null) 
                parry_timer = (int)(MAX_PARRY_TIMER * amount);  // BALANCE THIS
            // RULE 5: assuming the swords are crossed, if a fencer attempts to parry when his 
            //   opponent has recently initiated a Deceive action, then the fencer has not only
            //   "missed a parry attempt" but receives an additional penalty to reward his
            //   opponent for anticipating the parry.
            else if (Target.deception_timer > 0) 
                parry_timer = (int)(MAX_PARRY_TIMER * amount * 2); // BALANCE THIS
            // SUCCESS: The opponent is parried, and cannot do many actions for a limited time.
            else 
                Target.parry_timer = (int)(MAX_PARRY_TIMER * amount);  // BALANCE THIS
        }

        /// <summary>With thrust and beat-parry, deceive completes the rock-paper-scissors triad.</summary>
        /// <param name="amount">A number from 1.0 (full power) down to deadzone (0.3, least committed).</param>
        public void Deceive(float amount)
        {
            // RULE 1: a fencer cannot deceive if he recently missed a parry attempt
            if (parry_timer != 0)
                return;
            // SUCCESS: the fencer is now deceiving, a kind of passive defense or counter-move.
            deception_timer = (int)(MAX_DECEIVE_TIMER * amount); // BALANCE THIS
            // RULE 2: a fencer can unlock his tied-up blade after being the victim of a Take action.
            deflected_wrist = false;
        }

        /// <summary>Changes to a set of keyframes for in-close fighting. It listens to the Controller event Invert.</summary>
        /// <param name="amount">A number from 1.0 (elbow points skyward) to 0 (normal).</param>
        public void Invert(float amount)
        {
            // uh...  my drawing code doesn't make this one easy anymore
        }

        /// <summary>Puts the game into a quasi-mode to interfere with the opponent's inputs. It listens to the Controller event Take.</summary>
        /// <param name="amount">A currently unused number from 1 (on) to 0 (off).</param>
        public void Take(float amount)
        {
            // RULE 1: a fencer can't Take/Block when out of line due to a missed parry.
            if (parry_timer != 0)
                return;
            // RULE 2: a fencer can't Take/Block during his own deceive attempt.
            else if (deception_timer != 0)
                return;
            // RULE 3: a fencer can't Take/Block when his blade's been taken by his opponent already.
            else if (Fencing.blade_taken == Target)
                return;
            // RULE 4: Taking/Blocking involves turning the blade slightly out of line.
            //    So if the opposing blade isn't present (meaning we whiffed) the blocking blade is,
            //    well, out of line.
            else if (Fencing.swordLinesCrossedAt == null) 
                parry_timer = MAX_TAKE_TIMER;  // BALANCE THIS
            // SUCCESS: Carry out the TAKE action by informing the game that the blades are tied
            //   together, with this fencer as the aggressor.
            else  
                Fencing.PriseDeFer(this);
            // RULE 5: When the Take button is released, untie the blades (see EndTake()).
            // RULE 6: If the tied blades ever uncross (such as by walking away, etc.) the blades are 
            //   immediately untied (see PriseDeFerFin()).
        }

        /// <summary>Informs the game that this fencer has released the TAKE button on his controller.
        /// This will end the Take quasi-mode assuming of course he was the initiator to begin with.
        /// It listens to the Controller event EndTake.</summary>
        public void EndTake()
        {
            // RULE 5: When the Take button is released, untie the blades (see EndTake()).
            Fencing.PriseDeFerFin(this);
            // RULE 6: If the tied blades ever uncross (such as by walking away, etc.) the blades are 
            //   immediately untied (see PriseDeFerFin()).
        }

        /// <summary>This selects which (interpolated) keyframe the fencer desires. It's kinda crude 
        /// for something as precise as fencing, but with a keypad it's the best possible.</summary>
        /// <param name="keyframe">An x,y coordinate between -1,-1 and +1,+1 representing a thumbstick.</param>
        public void Pose(Vector2 between_keyframes)
        {
            currentPose = between_keyframes;
            if (Fencing.blade_taken == this)
                Fencing.PriseDeFer(this); // update Game on my position so it can inform victim.
        }

        /// <summary>Renders the character and, as a side effect, fills in the jointsForCollision array
        /// with world coordinates.</summary>
        public override void Draw(GameTime gameTime)
        {
            oldwrist = jointsForCollision[(int)JointNames.wrist];
            oldsword = jointsForCollision[(int)JointNames.swordtip];
            Matrix temp;  // stupid structs can't be passed by ref except when in a lone variable
            matrices.TextureEnabled = false; // stick figures, no textures

            jointsForCollision[(int)JointNames.hip] = Location;
            for (int i = 0; i < (int)JointNames.swordtip; )
            {
                matrices.World = Matrix.CreateTranslation(-mylinelist[i*2].Position); // translate from local to world
                matrices.World *= Matrix.CreateRotationZ(MathHelper.ToRadians(jointAngles[i])); // rotate about origin
                if (rightside == 1) matrices.World *= Matrix.CreateScale(-1f, 1f, 1f); // player 2 mirrored
                matrices.World *= Matrix.CreateTranslation(jointsForCollision[i]); // move model to world-location of hip
                matrices.CurrentTechnique.Passes[0].Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, mylinelist, i*2, 1); // draw torso
                i++;
                temp = matrices.World; 
                Vector3.Transform(ref mylinelist[i*2].Position, ref temp, out jointsForCollision[i]); // local to world (shoulder)
            }

            // RULE (Solid objects): if my opponent creates an impassable "block point" via his TAKE 
            // button, and I try to pass it, then my sword is going to be deflected (by my
            // wrist changing angle).
            if (Fencing.blade_taken == Target)
                if (true == (Geometry.PointInPoly(Fencing.wall_at, jointsForCollision[(int)JointNames.wrist], jointsForCollision[(int)JointNames.swordtip], oldsword, oldwrist)))
                    deflected_wrist = true;

            base.Draw(gameTime);
        }

        /// <summary>Note that b.Min and b.Max aren't actually min/max, so careful what methods to call on it.</summary>
        /// <returns>A box defined by the wrist joint and sword tip.</returns>
        public BoundingBox SwordBox()
        {  // note:  b.Min and b.Max aren't actually min/max, so careful what methods to call on it
            return new BoundingBox(jointsForCollision[(int)JointNames.wrist], jointsForCollision[(int)JointNames.swordtip]);
        }

        /// <summary>Given a JointName and the currentPose, calculates the joint angle.</summary>
        /// <param name="i">A JointName casted to int.</param>
        /// <returns>The desired (by the player) angle of the passed-in joint.</returns>
        private float InterpolatedJointAngle(int i)
        {
            float lerp1 = 0f, lerp2 = 0f;
            if (currentPose.Y > 0)
            {
                lerp1 = MathHelper.Lerp(keyframes[5, i], keyframes[8, i], +currentPose.Y);
                if (currentPose.X > 0)
                {
                    lerp2 = MathHelper.Lerp(keyframes[6, i], keyframes[9, i], +currentPose.Y);
                    return MathHelper.Lerp(lerp1, lerp2, +currentPose.X);
                }
                else
                {
                    lerp2 = MathHelper.Lerp(keyframes[4, i], keyframes[7, i], +currentPose.Y);
                    return MathHelper.Lerp(lerp1, lerp2, -currentPose.X);
                }
            }
            else
            {
                lerp1 = MathHelper.Lerp(keyframes[5, i], keyframes[2, i], -currentPose.Y);
                if (currentPose.X > 0)
                {
                    lerp2 = MathHelper.Lerp(keyframes[6, i], keyframes[3, i], -currentPose.Y);
                    return MathHelper.Lerp(lerp1, lerp2, +currentPose.X);
                }
                else
                {
                    lerp2 = MathHelper.Lerp(keyframes[4, i], keyframes[1, i], -currentPose.Y);
                    return MathHelper.Lerp(lerp1, lerp2, -currentPose.X);
                }
            }
        }

        // stickman has no meshes, just bones, and we need to render the bones with GraphicsDevice.DrawUserPrimitives
        public VertexPositionColor[] mylinelist = new VertexPositionColor[(int)JointNames.Count * 2 - 1] { 
            new VertexPositionColor(new Vector3(          0,   0, 0), Color.LightGray), // From hip starting position,
            new VertexPositionColor(new Vector3(          0,-200, 0), Color.LightGray), // draw torso to shoulder.
            new VertexPositionColor(new Vector3(          0,-200, 0), Color.LightGray), // From shoulder, 
            new VertexPositionColor(new Vector3(limb_length,-200, 0), Color.LightGray), // the upper arm.
            new VertexPositionColor(new Vector3(limb_length,-200, 0), Color.LightGray), // From elbow, 
            new VertexPositionColor(new Vector3(2.0f*limb_length,-200, 0), Color.LightGray), // the lower arm.
            new VertexPositionColor(new Vector3(2.0f*limb_length,-200, 0), Color.LightGray), // From wrist,
            new VertexPositionColor(new Vector3(4.5f*limb_length,-200, 0), Color.White), // the sword.
            new VertexPositionColor(new Vector3(4.5f*limb_length,-200, 0), Color.White), // unused, but makes the loop easier
        };  // Character "stands on the x-axis", and -y is upward.
        
        public float[,] keyframes = new float[(int)PoseNames.Count, (int)JointNames.swordtip] {
            {0f,  30f,   0f, -15f},   // 0 None
            {0f, 115f,  55f, -80f},   // 1 close_low
            {0f,  50f,  15f,  40f},   // 2 low
            {0f,  25f,  20f,  15f},   // 3 far_low
            {0f, 150f,  25f, -70f},   // 4 close_mid
            {0f,  50f,  15f, -55f},   // 5 mid
            {0f,  15f,  10f,   0f},   // 6 far_mid
            {0f,  80f, -85f, -91f},   // 7 close_high
            {0f,  50f, -40f, -55f},   // 8 high
            {0f,   5f,  -5f, -10f},   // 9 far_high
        };   // off-hand was at 0f, -180f, -150f, -30f
        
        /// <summary>Shades a limb from one monochrome to another monochrome. A gradient.</summary>
        private void ColorizeLimb(JointNames joint1, int color1, JointNames joint2, int color2)
        {
            mylinelist[(int)joint1 * 2 - 1].Color = new Color(color1, color1, color1);
            mylinelist[(int)joint2 * 2    ].Color = new Color(color2, color2, color2);
        }

    }
}
