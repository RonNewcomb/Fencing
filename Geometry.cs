using System;
using Microsoft.Xna.Framework;

namespace Fencing
{
    /// <summary>For these BoundingBox methods, "min" and "max" are misnomers.
    /// They're just 2 points to define an axis-aligned box.
    /// If you use BoundingBox.CreateFromPoints(), for example,
    /// the points will be normalized to actually be a min/max, which
    /// destroys slope information.</summary>
    public class Geometry
    {
        public static float Width(BoundingBox b)  { return b.Max.X - b.Min.X; }
        public static float Height(BoundingBox b) { return b.Max.Y - b.Min.Y; }
        public static float Slope(BoundingBox b)  { return Geometry.Height(b) / Geometry.Width(b); }
        public static float Intercept(BoundingBox b) { return b.Min.Y - b.Min.X * Geometry.Slope(b); }

        /// <summary>This returns a value that's positive or negative depending on which side of the
        /// a-b line that point c is on. Or if point c is on the a-b line, returns zero.  It ignores
        /// the Z-component in every case.</summary>
        /// <param name="a">One end of the line.</param>
        /// <param name="b">The other end of the line.</param>
        /// <param name="c">The point to consider.</param>
        /// <returns>A number that's positive or negative depending upon which side of the line C is on.</returns>
        public static float Beside(Vector3 a, Vector3 b, Vector3 c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X));
        }

        /// <summary>Returns whether or not the first point is inside the quad defined by the 
        /// following four points.</summary>
        public static bool PointInPoly(Vector3 point, Vector3 corner1, Vector3 corner2, Vector3 corner3, Vector3 corner4)
        {
            float side_of1 = Geometry.Beside(corner1, corner2, Fencing.wall_at);
            float side_of2 = Geometry.Beside(corner2, corner3, Fencing.wall_at);
            float side_of3 = Geometry.Beside(corner3, corner4, Fencing.wall_at);
            float side_of4 = Geometry.Beside(corner4, corner1, Fencing.wall_at);
            return (4 == Math.Abs(Math.Sign(side_of1) + Math.Sign(side_of2) + Math.Sign(side_of3) + Math.Sign(side_of4)));
        }


        /// <summary>
        /// Answers if the 2D point is within the 2D box.
        /// </summary>
        /// <param name="b">It isn't assumed that this box's .Min and .Max actually are a minimum or maximum.</param>
        /// <param name="v">The z component is ignored, for a 2D point.</param>
        /// <returns>True if the point is within the box, ignoring all z components.</returns>
        public static bool Contains(BoundingBox b, Vector3 v)
        {  // the .Contains of BoundingBox does not work because .Min and .Max aren't actually min/max
            if (v.X < MathHelper.Min(b.Min.X, b.Max.X)) return false;
            if (v.X > MathHelper.Max(b.Min.X, b.Max.X)) return false;
            if (v.Y < MathHelper.Min(b.Min.Y, b.Max.Y)) return false;
            if (v.Y > MathHelper.Max(b.Min.Y, b.Max.Y)) return false;
            return true;
        }

        /// <summary>
        /// Returns the intersection of the 2D xy lines defined by the corners of the boxes.
        /// </summary>
        /// <param name="leftSword">Two points define both a bounding box as well as a line. The z component is ignored.</param>
        /// <param name="rightSword">Don't create with CreateFromPoints() because it doesn't preserve the two points (and hence the slope).</param>
        /// <returns>Right slope is nudged if needed to avoid div-by-0 with vertical parallel lines.</returns>
        public static Vector3 LineIntersection(BoundingBox leftSword, BoundingBox rightSword)
        {
            float leftslope      =  Geometry.Slope(leftSword);
            float leftintercept  =  Geometry.Intercept(leftSword);
            float rightslope     =  Geometry.Slope(rightSword);
            float rightintercept =  Geometry.Intercept(rightSword);
            if (leftslope - rightslope == 0f) rightslope += 0.0001f; 
            float intersectX = (rightintercept - leftintercept) / (leftslope - rightslope); 
            float intersectY = leftslope * intersectX + leftintercept;
            return new Vector3(intersectX, intersectY, 0f);
        }

        /// <summary>
        /// Lines are infinite in both directions, rays are infinite in one direction, but segments, like all physical objects, are finite in every way.
        /// </summary>
        /// <param name="leftSword">Two points define both a bounding box as well as a line. The z component is ignored.</param>
        /// <param name="rightSword">Don't create with CreateFromPoints() because it doesn't preserve the two points (and hence the slope).</param>
        /// <returns>Returns the intersection point, or null if they don't intersect.</returns>
        public static Nullable<Vector3> LineSegmentIntersection(BoundingBox leftSword, BoundingBox rightSword)
        {
            Vector3 li = Geometry.LineIntersection(leftSword, rightSword);
            if (!Geometry.Contains( leftSword, li)) return null;
            if (!Geometry.Contains(rightSword, li)) return null;
            return li;
        }

        public static Vector2 SquareTheCircle(Vector2 v)
        {
            float absX = Math.Abs(v.X);
            float absY = Math.Abs(v.Y);
            float dirLen = Math.Min(v.Length() * 1.25f, 1f);
            float scale = Math.Max(0.01f, absX > absY ? absX : absY);
            return v * (dirLen / scale);
        }

        /// <summary>Rotates the vector counter-clockwise within the xy plane.</summary>
        /// <param name="vector">The z component of the vector is ignored.</param>
        /// <returns>The returned vector preserves whatever the z component was.</returns>
        public static Vector3 Perpendicular(Vector3 vector)
        {
            return new Vector3(vector.Y, -vector.X, vector.Z);
        }

        /// <summary>Rotates the vector counter-clockwise within the xy plane.</summary>
        /// <param name="vector">The z component of the vector is ignored.</param>
        /// <returns>The returned vector preserves whatever the z component was.</returns>
        public static Vector2 Perpendicular(Vector2 vector)
        {
            return new Vector2(vector.Y, -vector.X);
        }
        //        Vector3 chord1, chord2;
/*
        public static float NormalizedDistanceFrom(Vector3 from, Vector3 to, float swordLength)
        {
            float a = to.X - from.X;
            float b = to.Y - from.Y;
            return (a * a + b * b) / (swordLength * swordLength);
        }*/

        public static float AngleFromPointAthruPointC(Vector3 from, Vector3 to)
        {
            float angle = (float)(Math.Atan((from.Y - to.Y) / (from.X - to.X))) * 180f / MathHelper.Pi;
            if (to.X - from.X < 0f && to.Y - from.Y > 0f)
                angle += 180f;
            else if (to.X - from.X < 0f && to.Y - from.Y < 0f)
                angle -= 180f;
            if (angle > 359.5f)
                angle -= 360f;
            else if (angle < -359.5f)
                angle += 360f;
            return angle;
        }

        /*        + (BOOL)IsPoint:(Vector3)p InCircleAt:(Vector3)center WithRadius:(float)radius  betweenAngle:(float)min andAngle:(float)max
                {
                    if ([Geometry distanceFrom:p to:center over:radius] > 1.0)
                    {
                        printf("short\n");
                        return NO;
                    }
                    float angle = [Geometry computeAngleFrom:center to:p];
                    if (max < min)
                    {
                        float temp = min;
                        min = max;
                        max = temp;
                    }
                    return (min <= angle && angle <= max);
                }*/

        /*        + (void)intersectionOfCircle:(float)radius  line:(NSRect)sword  vector:(Vector3)v
                {
                    float x1 = sword.origin.X - v.X;
                    float y1 = sword.origin.Y - v.Y;
                    float x2 = x1 + sword.size.width;
                    float y2 = y1 + sword.size.height;

                    float dx = x2 - x1;
                    float dy = y2 - y1;
                    float dr2 = dx*dx + dy*dy;
                    float det = x1 * y2 - x2 * y1;
                    float det2 = det * det;
                    float term = sqrt(radius*radius*dr2 - det2);
                    float intersectX1 = ( det * dy + [self sign:dy] * dx * term) / dr2 + v.X;
                    float intersectX2 = ( det * dy - [self sign:dy] * dx * term) / dr2 + v.X;
                    float intersectY1 = (-det * dx + abs(dy) * term) / dr2 + v.Y;
                    float intersectY2 = (-det * dx - abs(dy) * term) / dr2 + v.Y;
                    if (intersectY2 < intersectY1)
                    {   // chord1 shall always be higher (visually, on-screen) than chord2
                        chord1 = NSMakePoint(intersectX1, intersectY1);
                        chord2 = NSMakePoint(intersectX2, intersectY2);
                    }
                    else
                    {
                        chord1 = NSMakePoint(intersectX2, intersectY2);
                        chord2 = NSMakePoint(intersectX1, intersectY1);
                    }
                }*/


    }
}
