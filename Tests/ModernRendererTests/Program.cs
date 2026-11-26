using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Drawing;
using System.Numerics;
using CrystalM2.Rendering.Core;

static class Assertions
{
    internal static void True(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }

    internal static void Equal<T>(T expected, T actual, string reason) where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
            throw new InvalidOperationException(reason + ": expected " + expected + ", got " + actual);
    }
}

static class Program
{
    private static void CheckWingBodyTracking()
    {
        static void Check(int frame, int bank, int facing, bool mounted, Vector2 expected)
        {
            Assertions.True(WingBodyPose.TryGetDelta(frame, bank, facing, mounted, out Vector2 actual),
                $"Missing baked shoulder for body frame {frame}.");
            Assertions.True(Vector2.Distance(actual, expected) < .025f,
                $"Body frame {frame} wing translation differs from the baked pose: {actual} vs {expected}.");
        }

        // Right-facing male, idle, walk, run, attack and struck, from poses.json.
        Check(8, 0, 2, false, Vector2.Zero);
        Check(44, 0, 2, false, new(.975f, 1.325f));
        Check(92, 0, 2, false, new(.65f, 1.325f));
        Check(149, 0, 2, false, new(-14.05f, 7f));
        Check(152, 0, 2, false, new(18.025f, 18f));
        Check(366, 0, 2, false, new(-16.75f, 7.675f));
        Check(957, 808, 2, false, new(-11.65f, 6.35f));
        Check(424, 0, 2, true, Vector2.Zero);
        // Validate every actual bank entry and all eight direction baselines.
        foreach (int bank in new[] { 0, 808 })
        {
            for (int direction = 0; direction < 8; direction++)
            {
                Check(bank + direction * 4, bank, direction, false, Vector2.Zero);
                Check(bank + 416 + direction * 4, bank, direction, true, Vector2.Zero);
            }
            for (int i = 0; i < 808; i++)
                Assertions.True(WingBodyPose.TryGetDelta(bank + i, bank, 0, false, out var delta) &&
                    float.IsFinite(delta.X) && float.IsFinite(delta.Y), $"Invalid shoulder data for {bank+i}.");
        }

        Assertions.True(!WingBodyPose.TryGetDelta(808, 0, 0, false, out _),
            "Never read the other gender's frame as a male pose.");
        Assertions.True(!WingBodyPose.TryGetDelta(0, 512, 0, false, out _),
            "Alternate animation bank cannot be mistaken for classic CArmour.");
    }

    private static void CheckWingPoseMotion()
    {
        Assertions.True(WingBodyPose.TryGetMotion(8, 0, 2, false, out var idle) &&
            idle == WingPoseMotion.Zero, "Idle pose is the motion reference.");
        Assertions.True(WingBodyPose.TryGetMotion(149, 0, 2, false, out var windup) &&
            windup.Roll < -.02f && windup.Yaw > .02f && windup.Pitch < 0, "Right attack windup must lean and turn.");
        Assertions.True(WingBodyPose.TryGetMotion(152, 0, 2, false, out var follow) &&
            follow.Roll > .08f && follow.Yaw < -.02f && follow.Pitch > .1f, "Right attack follow-through must lean and turn.");
        for (int direction = 0; direction < 8; direction++)
        for (int index = 0; index < 4; index++)
            Assertions.True(WingBodyPose.TryGetMotion(direction * 4 + index, 0, direction, false, out var rest) &&
                rest.Roll == 0 && rest.Yaw == 0 && rest.Pitch == 0,
                "Standing frames track the socket without introducing extra rotation.");
        var bounded = WingPoseMotion.LimitOffset(new Vector2(25, -10), follow.Offset, 1.5f);
        Assertions.True(Vector2.Distance(bounded, follow.Offset) <= 1.501f, "Smoothing must not detach from the current torso.");
        var smooth = new WingPoseSmoother();
        smooth.Advance(idle, 100, true);
        var first = smooth.Advance(follow, 116, false);
        Assertions.True(first.Offset == follow.Offset &&
            first.Roll > 0 && first.Roll < follow.Roll &&
            first.Pitch > 0 && first.Pitch < follow.Pitch,
            "The socket must track the body immediately while the wing angles ease.");
        Assertions.True(smooth.Advance(follow, 116, false) == first, "Draw passes share a sample.");
        var later = smooth.Advance(follow, 216, false);
        Assertions.True(later.Offset == follow.Offset && later.Roll > first.Roll && later.Roll <= follow.Roll,
            "The angles converge without introducing positional lag.");
        var reverse = smooth.Advance(windup, 232, false);
        Assertions.True(reverse.Offset == windup.Offset &&
            MathF.Abs(reverse.Roll - later.Roll) <= .9f * .016f + .0001f &&
            MathF.Abs(reverse.Yaw - later.Yaw) <= .9f * .016f + .0001f &&
            MathF.Abs(reverse.Pitch - later.Pitch) <= .9f * .016f + .0001f,
            "An abrupt strike reversal must not snap any wing angle.");
        var sameTime = smooth.Advance(follow, 232, false);
        Assertions.True(sameTime.Offset == follow.Offset && sameTime.Roll == reverse.Roll,
            "A body frame change at the same clock tick still pins the socket.");
        var slow = new WingPoseSmoother();
        var fast = new WingPoseSmoother();
        slow.Advance(idle, 0, true);
        fast.Advance(idle, 0, true);
        var small = new WingPoseMotion(new Vector2(2, 1), .015f, -.015f, .01f);
        for (int time = 10; time <= 100; time += 10) fast.Advance(small, time, false);
        slow.Advance(small, 100, false);
        Assertions.True(MathF.Abs(fast.Current.Roll - slow.Current.Roll) < .0002f &&
            fast.Current.Offset == slow.Current.Offset, "The unclamped spring is frame-rate independent.");
        for (int time = 116; time <= 600; time += 16)
        {
            var old = fast.Current;
            var next = fast.Advance(follow, time, false, .060f, .30f);
            Assertions.True(next.Offset == follow.Offset &&
                MathF.Abs(next.Roll - old.Roll) <= .30f * .016f + .0001f &&
                MathF.Abs(next.Yaw - old.Yaw) <= .30f * .016f + .0001f &&
                MathF.Abs(next.Pitch - old.Pitch) <= .30f * .016f + .0001f,
                "Locomotion angular speed remains bounded independently of the socket.");
        }
        Assertions.True(smooth.Advance(idle, 217, true) == idle, "Reset must not retain the old pose.");
        foreach (int bank in new[] {0, 808})
        for (int frame = 0; frame < 808; frame++)
            Assertions.True(WingBodyPose.TryGetMotion(bank + frame, bank, 2, false, out var motion) &&
                float.IsFinite(motion.Roll) && float.IsFinite(motion.Yaw) &&
                MathF.Abs(motion.Roll) <= .181f && MathF.Abs(motion.Yaw) <= .141f &&
                WingBodyPose.TryGetAttachmentDelta(bank + frame, bank, 2, false, out var socket) &&
                motion.Offset == socket, $"Invalid or detached motion frame {bank + frame}.");
    }

    private static void CheckUpperBackTracking()
    {
        // Frame 152: neck (44,-20), shoulders (36,-24)/(47,-10).
        // The upper shoulder must pull the anchor upwards and forwards,
        // rather than letting the lower waist drag the socket behind.
        Assertions.True(WingBodyPose.TryGetUpperBack(152, 0, 2, out var strike) &&
            Vector2.Distance(strike, new Vector2(40.0625f, -20.375f)) < .001f,
            "The right-facing follow-through must attach to the visible upper back.");
        var smoother = new WingPoseSmoother();
        foreach (int bank in new[] { 0, 808 })
        for (int direction = 0; direction < 8; direction++)
        {
            // Classic Attack4 uses 416 + direction * 6; the same pixels
            // are also used by other actions. Do not infer mounting here.
            foreach (int start in new[] { 136, 184, 416, 584 })
            for (int frame = 0; frame < 6; frame++)
            {
                int index = bank + start + direction * 6 + frame;
                Assertions.True(WingBodyPose.TryGetUpperBack(index, bank, direction, out var upper) &&
                    float.IsFinite(upper.X) && float.IsFinite(upper.Y),
                    $"Missing upper-back anchor for frame {index}.");
                var pose = smoother.Advance(new WingPoseMotion(upper, .1f, .1f, .2f), index * 16L, false);
                Assertions.True(pose.Offset == upper, $"The attachment must not lag at frame {index}.");
            }
        }
        Assertions.True(!WingBodyPose.TryGetUpperBack(808, 0, 0, out _),
            "Do not sample the other gender's atlas bank.");
    }

    private static void CheckTorsoFrameTracking()
    {
        Assertions.True(WingBodyPose.TryGetTorsoFrame(8, 0, 2, false, out var idle) &&
            Vector2.Distance(idle.Across, Vector2.Normalize(new Vector2(10, -2))) < .0001f,
            "The reference must use the actual shoulder line.");
        Assertions.True(WingBodyPose.TryGetTorsoFrame(149, 0, 2, false, out var windup) &&
            Vector2.Distance(windup.Across, Vector2.Normalize(new Vector2(14, -8))) < .0001f,
            "Windup shoulder orientation must come from the displayed frame.");
        Assertions.True(WingBodyPose.TryGetTorsoFrame(152, 0, 2, false, out var follow) &&
            Vector2.Distance(follow.Across, Vector2.Normalize(new Vector2(11, 14))) < .0001f,
            "Follow-through must twist with the shoulder line, not the facing.");
        Assertions.True(Vector2.Distance(follow.Transform(follow.ReferenceAcross), follow.Across) < .0001f &&
            Vector2.Distance(windup.Transform(windup.ReferenceAcross), windup.Across) < .0001f &&
            idle.Transform(Vector2.Zero) == Vector2.Zero,
            "The rigid torso transform must map the reference shoulder axis to the current shoulder axis around the socket.");
        var smoother = new WingPoseSmoother();
        smoother.Advance(new WingPoseMotion(Vector2.Zero, 0, 0) { TorsoFrame = idle }, 100, true);
        var changed = smoother.Advance(new WingPoseMotion(Vector2.One, .15f, .1f) { TorsoFrame = follow }, 100, false);
        Assertions.True(changed.TorsoFrame == follow && changed.Offset == Vector2.One && changed.Yaw == 0,
            "Rigid orientation and attachment update with the body even at the same tick; distal inertia stays separate.");
        foreach (int bank in new[] { 0, 808 })
        for (int frame = 0; frame < 808; frame++)
        for (int direction = 0; direction < 8; direction++)
        {
            Assertions.True(WingBodyPose.TryGetTorsoFrame(bank + frame, bank, direction, false, out var pose) &&
                MathF.Abs(pose.Across.Length() - 1) < .0001f &&
                MathF.Abs(pose.Transform(Vector2.UnitY).Length() - 1) < .0001f &&
                Vector2.Distance(pose.Transform(pose.ReferenceAcross), pose.Across) < .0001f,
                $"Invalid torso basis for {bank + frame}.");
        }
    }

    private static void CheckIndependentWingSockets()
    {
        Assertions.True(WingBodyPose.TryGetTorsoFrame(152, 0, 2, false, out var release) &&
            release.NegativeShoulder == new Vector2(36, -24) &&
            release.PositiveShoulder == new Vector2(47, -10),
            "The actual right-facing follow-through uses independent shoulder landmarks.");
        foreach (int bank in new[] { 0, 808 })
        foreach (var (start, count, mounted) in new (int, int, bool)[]
        {
            (32, 6, false), (80, 6, false), (136, 6, false), (184, 6, false),
            (232, 8, false), (296, 6, false), (416, 6, false),
            (448, 8, true), (512, 6, true), (584, 6, true)
        })
        for (int direction = 0; direction < 8; direction++)
        for (int index = 0; index < count; index++)
        {
            int frame = bank + start + direction * count + index;
            Assertions.True(WingBodyPose.TryGetTorsoFrame(frame, bank, direction, mounted, out var body),
                $"Missing shoulder pair for body frame {frame}.");
            float angle = direction * MathF.PI / 4;
            Vector2 nominal = new(MathF.Cos(angle), MathF.Sin(angle) * .49f);
            Vector2 rear = new Vector2(-MathF.Sin(angle), MathF.Cos(angle) * .49f) * 1.5f;
            float handedness = Vector2.Dot(nominal, body.ReferenceAcross) < 0 ? -1 : 1;
            Vector2[] roots = new Vector2[2];
            foreach (int side in new[] { -1, 1 })
            {
                bool positive = side * handedness > 0;
                Vector2 referenceShoulder = positive ? body.ReferencePositiveShoulder : body.ReferenceNegativeShoulder;
                Vector2 currentShoulder = positive ? body.PositiveShoulder : body.NegativeShoulder;
                Vector2 rest = body.ReferenceCentre + rear +
                    body.ReferenceAcross * (side * handedness * 5.5f * nominal.Length()) - referenceShoulder;
                Vector2 actual = body.Centre + body.SocketOffset(nominal, side, 5.5f, rear) - currentShoulder;
                Vector2 expected = body.Transform(rest);
                float width = Vector2.Distance(body.NegativeShoulder, body.PositiveShoulder);
                float refWidth = Vector2.Distance(body.ReferenceNegativeShoulder, body.ReferencePositiveShoulder);
                if (width - refWidth + 11 * nominal.Length() >= 1)
                    Assertions.True(Vector2.Distance(actual, expected) < .001f,
                        $"Wing {side} lost its own shoulder attachment at frame {frame}.");
                Vector2 perpendicular = new(-body.Across.Y, body.Across.X);
                Assertions.True(MathF.Abs(Vector2.Dot(actual - expected, perpendicular)) < .001f,
                    $"Wing {side} changed its back/height offset at {frame}.");
                roots[(side + 1) / 2] = body.SocketOffset(nominal, side, 5.5f, rear);
                Vector2 idleRoot = body.AtRest().SocketOffset(nominal, side, 5.5f, rear);
                Assertions.True(Vector2.Distance(idleRoot, rear + body.ReferenceAcross *
                    (side * handedness * 5.5f * nominal.Length())) < .001f,
                    "Idle must retain its established root spacing.");
            }
            Assertions.True(Vector2.Dot(roots[1] - roots[0], body.Across) * handedness >= .999f,
                $"Wing sockets crossed at {frame}.");
        }
    }

    private static void CheckInverseWingTwist()
    {
        // Down-right attack lunge: the forward/back shoulder projection in
        // the 2D proxy has the opposite sign to the desired wing twist.
        Assertions.True(WingBodyPose.TryGetTorsoFrame(158, 0, 3, false, out var lunge),
            "Missing down-right lunge.");
        var corrected = lunge.ForWingTwist();
        Assertions.True(lunge.Across.Y > 0 && corrected.Across.Y < 0 && corrected.Across.X > 0,
            "The lunge must lower the previously raised wing root and raise its counterpart.");
        foreach (int bank in new[] { 0, 808 })
        foreach (var (start, count, mounted) in new (int, int, bool)[]
        {
            (0, 4, false), (32, 6, false), (80, 6, false),
            (136, 6, false), (184, 6, false), (232, 8, false),
            (416, 6, false), (448, 8, true), (512, 6, true), (584, 6, true)
        })
        for (int direction = 0; direction < 8; direction++)
        for (int i = 0; i < count; i++)
        {
            int index = bank + start + direction * count + i;
            Assertions.True(WingBodyPose.TryGetTorsoFrame(index, bank, direction, mounted, out var body),
                $"Missing body frame {index}.");
            var wing = body.ForWingTwist();
            float originalCross = body.ReferenceAcross.X * body.Across.Y - body.ReferenceAcross.Y * body.Across.X;
            float correctedCross = wing.ReferenceAcross.X * wing.Across.Y - wing.ReferenceAcross.Y * wing.Across.X;
            Assertions.True(MathF.Abs(originalCross + correctedCross) < .0001f &&
                MathF.Abs(Vector2.Dot(body.ReferenceAcross, body.Across) -
                    Vector2.Dot(wing.ReferenceAcross, wing.Across)) < .0001f,
                $"Incorrect twist sign at {index}.");
            Assertions.True(wing.Centre == body.Centre &&
                Vector2.Distance(wing.NegativeShoulder + wing.PositiveShoulder,
                    body.NegativeShoulder + body.PositiveShoulder) < .0001f &&
                MathF.Abs(Vector2.Distance(wing.NegativeShoulder, wing.PositiveShoulder) -
                    Vector2.Distance(body.NegativeShoulder, body.PositiveShoulder)) < .0001f &&
                Vector2.Dot(Vector2.Normalize(wing.PositiveShoulder - wing.NegativeShoulder), wing.Across) > .9999f,
                $"Twist changed the rig centre or shoulder separation at {index}.");
            float angle = direction * MathF.PI / 4;
            Vector2 nominal = new(MathF.Cos(angle), MathF.Sin(angle) * .49f);
            Vector2 rear = new Vector2(-MathF.Sin(angle), MathF.Cos(angle) * .49f) * 1.5f;
            float handedness = Vector2.Dot(nominal, wing.ReferenceAcross) < 0 ? -1 : 1;
            var left = wing.SocketOffset(nominal, -1, 5.5f, rear);
            var right = wing.SocketOffset(nominal, 1, 5.5f, rear);
            Assertions.True(Vector2.Dot(right - left, wing.Across) * handedness >= .999f,
                $"Corrected roots crossed at {index}.");
            foreach (int side in new[] { -1, 1 })
                Assertions.True(Vector2.Distance(wing.AtRest().SocketOffset(nominal, side, 5.5f, rear),
                    body.AtRest().SocketOffset(nominal, side, 5.5f, rear)) < .0001f,
                    "Standing attachment must remain unchanged.");
        }
    }

    private static void CheckLegacyWingLift()
    {
        // The reference measures art relative to the body, not screen position.
        Assertions.True(WingPoseAtlas.LiftFromRelativeCentres(-20, -28) == 8 &&
            WingPoseAtlas.LiftFromRelativeCentres(-20, -12) == 0 &&
            WingPoseAtlas.LiftFromRelativeCentres(-20, -90) == 12,
            "Reference lift is upward only and bounded.");
        var smoother = new WingPoseSmoother();
        smoother.Advance(WingPoseMotion.Zero, 0, true);
        var target = WingPoseMotion.Zero with { LegacyLift = 12 };
        var first = smoother.Advance(target, 16, false);
        Assertions.True(first.LegacyLift > 0 && first.LegacyLift <= .641f,
            "Lift must ease instead of snapping between effect frames.");
        Assertions.True(WingPoseMotion.Stabilize(target, true, false).LegacyLift == 0,
            "Standing has no extra lift.");
        foreach (int bank in new[] { 0, 808 })
        foreach (int direction in Enumerable.Range(0, 8))
        foreach (var (bodyStart, effectStart, count) in new[] { (32,64,6), (80,112,6),
            (136,168,6), (184,216,6), (232,264,8), (416,448,6) })
        for (int frame = 0; frame < count; frame++)
        {
            int index = bank + bodyStart + direction * count + frame;
            Assertions.True(WingBodyPose.TryGetTorsoFrame(index, bank, direction, false, out var body),
                $"Missing pose {index}.");
            int offset = bank == 808 ? 840 : 0;
            float lift = WingPoseAtlas.ReferenceLift(offset + effectStart + direction * count + frame,
                offset + direction * 8, 8, body.Centre.Y, body.ReferenceCentre.Y);
            Assertions.True(float.IsFinite(lift) && lift >= 0 && lift <= 12,
                $"Invalid legacy lift at {index}.");
        }
    }

    
    private static void CheckLiveArmourSplit()
    {
        // Test-only authored 24x36 sprite pixels: naked head, two hands,
        // coloured torso, legs and disconnected checker shadow.
        var pixels = new byte[24 * 36 * 4];
        var dressed = new byte[pixels.Length];
        void Rect(byte[] data, int x, int y, int w, int h, byte r, byte g, byte b)
        {
            for (int j=y; j<y+h; j++) for(int i=x; i<x+w; i++)
            {
                int p=(j*24+i)*4;
                data[p]=b; data[p+1]=g; data[p+2]=r; data[p+3]=255;
            }
        }
        foreach(var data in new[]{pixels,dressed})
        {
            Rect(data,10,1,5,6,202,160,119);
            Rect(data,2,17,3,3,195,149,106);
            Rect(data,19,17,3,3,195,149,106);
            Rect(data,7,21,4,13,176,132,92);
            Rect(data,14,21,4,13,176,132,92);
        }
        Rect(pixels,7,7,11,14,190,146,109);
        Rect(dressed,7,7,11,14,67,99,155);
        Rect(dressed,7,21,4,13,61,65,75);
        Rect(dressed,14,21,4,13,61,65,75);
        Rect(dressed,5,9,3,8,90,100,130);
        Rect(pixels,2,10,3,7,195,149,106);
        Rect(dressed,2,10,3,7,195,149,106);
        Rect(pixels,19,10,3,7,195,149,106);
        Rect(dressed,19,10,3,7,195,149,106);
        var sourceCopy = (byte[])dressed.Clone();
        Vector2[] anchors=[new(12,3),new(12,8),new(7,10),new(18,10),new(12,12),
            new(12,17),new(12,21),new(9,27),new(16,27),new(9,33),new(16,33)];
        var split = LiveArmourCoverage.Split(24,36,pixels,dressed,anchors);
        Assertions.True(split.Skin[(3*24+12)*4+3]==255 &&
            split.Skin[(18*24+3)*4+3]==255 && split.Skin[(18*24+20)*4+3]==255,
            "The visible face and both hands must be preserved.");
        Assertions.True(split.Skin[(12*24+12)*4+3]==0 &&
            split.Armour[(12*24+12)*4+3]==255 &&
            split.Skin[(32*24+9)*4+3]==0 && split.Armour[(32*24+9)*4+3]==255,
            "Torso and boots belong to generated armour, never the skin layer.");
        Assertions.True(split.Skin[(10*24+3)*4+3]==0 &&
            split.Skin[(10*24+20)*4+3]==0,
            "Sleeveless reference art must not preserve whole arms.");
        for(int p=3;p<pixels.Length;p+=4)
            Assertions.True(split.Skin[p]==0 || split.Armour[p]==0,"Skin and armour must be disjoint.");
        Assertions.True(dressed.AsSpan().SequenceEqual(sourceCopy),"Never modify the original library pixels.");
    }

private static void CheckArmourPatternTransfer()
    {
        const int w=32,h=44;
        byte[] mask=new byte[w*h*4], naked=new byte[mask.Length], fitted=new byte[mask.Length];
        var donors=Enumerable.Range(0,4).Select(_=>new byte[mask.Length]).ToArray();
        Vector2[] a=[new(16,3),new(16,8),new(8,10),new(24,10),new(16,14),
            new(16,22),new(16,25),new(11,33),new(21,33),new(11,40),new(21,40)];
        // Test-only synthetic donor art with different irregular patterns.
        for(int y=8;y<42;y++)for(int x=6;x<26;x++)
        {
            int p=(y*w+x)*4;
            mask[p+3]=fitted[p+3]=255;
            fitted[p]=fitted[p+1]=fitted[p+2]=70;
            for(int k=0;k<4;k++)
            {
                var d=donors[k];d[p+3]=255;
                d[p]=(byte)(20+k*40+(x*y)%29);
                d[p+1]=(byte)(40+k*25+(x*x+y)%45);
                d[p+2]=(byte)(80+k*30+(y*y+x)%30);
            }
        }
        int exposed=(3*w+16)*4, missing=(14*w+16)*4;
        // Missing donor pixel must fall back without opening a hole.
        donors[0][missing+3]=0;
        var before=donors.Select(d=>(byte[])d.Clone()).ToArray();
        var result=ArmourPatternTransfer.Compose(w,h,a,naked,fitted,mask,donors);
        var repeat=ArmourPatternTransfer.Compose(w,h,a,naked,fitted,mask,donors);
        Assertions.True(result.AsSpan().SequenceEqual(repeat),"Pattern transfer must be deterministic.");
        Assertions.True(result[exposed+3]==0&&result[missing+3]==255,"Preserve coverage, including donor gaps.");
        for(int i=3;i<result.Length;i+=4)
            Assertions.True(result[i]==mask[i],"No donor may extend the target silhouette.");
        for(int i=0;i<4;i++)
            Assertions.True(donors[i].AsSpan().SequenceEqual(before[i]),"Donor artwork is read-only.");
        int shoulder=(10*w+8)*4, chest=(16*w+16)*4, boot=(39*w+11)*4;
        Assertions.True(result[shoulder]>result[chest]&&result[boot]>result[shoulder],
            "Shoulder, chest and lower armour must select different donor patterns.");
        var varied=new HashSet<byte>();
        for(int y=12;y<19;y++)for(int x=14;x<19;x++)varied.Add(result[(y*w+x)*4]);
        Assertions.True(varied.Count>5,"Retain donor microdetail instead of flat bands.");
        for(int id=1;id<=11;id++)
        {
            var recipe=ArmourPatternTransfer.Recipe(id);
            Assertions.True(recipe.Length==4&&recipe.All(x=>x is >=1 and <=11)&&
                recipe.Distinct().Count()>=3,"Each design needs a stable multi-source recipe.");
        }
    }

    private static void CheckArmourDesigns()
    {
        var keys=new HashSet<string>();
        for(int id=2;id<=99;id++)
        {
            var design=ArmourDesign.Resolve(id);
            var again=ArmourDesign.Resolve(id);
            Assertions.True(design==again && design.Source is >=1 and <=11,
                "Design identity must be deterministic and independent of frame.");
            int[] donors=design.Donors;
            Assertions.True(donors.Distinct().Count()==4 &&
                donors.All(d=>ArmourDesign.IsRobe(d)==ArmourDesign.IsRobe(design.Source)),
                "Do not mix incompatible silhouettes or repeat donor regions.");
            Assertions.True(keys.Add($"{design.Source}:{string.Join(",",donors)}"),
                "Each runtime ID must select a distinct structural recipe.");
            Assertions.True(design.Palette is >=0 and <11 && design.Material is >=0 and <4,
                "Material/palette indices must be bounded.");
        }
        Assertions.True(keys.Count==98,"All appearance IDs must have a distinct recipe.");
        Assertions.True(!ArmourDesign.Supports(1)&&!ArmourDesign.Supports(100),
            "Preserve original g01 and reject invalid IDs.");
    }

    private static void CheckArmourFeatureSeams()
    {
        const int w=32,h=40;
        byte[] naked=new byte[w*h*4],mask=new byte[naked.Length],fitted=new byte[naked.Length];
        byte[][] donors=Enumerable.Range(0,4).Select(_=>new byte[naked.Length]).ToArray();
        Vector2[] a=[new(16,2),new(16,6),new(8,10),new(24,10),new(16,14),
            new(16,20),new(16,24),new(11,31),new(21,31),new(11,38),new(21,38)];
        // Test-only pixel fixtures: mutually opposed engraved patterns.
        // Ordinary RGB averaging destroys contrast in their overlap.
        for(int y=6;y<39;y++)for(int x=5;x<27;x++)
        {
            int p=(y*w+x)*4;mask[p+3]=fitted[p+3]=255;
            fitted[p]=fitted[p+1]=fitted[p+2]=100;
            for(int j=0;j<4;j++)
            {
                byte v=(byte)(((x+j)%2==0)?190:50);
                donors[j][p]=donors[j][p+1]=donors[j][p+2]=v;donors[j][p+3]=255;
            }
        }
        var surface=ArmourPatternTransfer.Compose(w,h,a,naked,fitted,mask,donors);
        int strong=0, samples=0;
        for(int y=11;y<18;y++)for(int x=10;x<22;x++)
        {
            int p=(y*w+x)*4;samples++;
            if(Math.Abs(surface[p]-120)>=35)strong++;
        }
        Assertions.True(strong>=samples*.8f,"Seam blending must preserve one strong engraving, not average opposite detail.");
        for(int p=3;p<mask.Length;p+=4)
            Assertions.True(surface[p]==mask[p],"Feature synthesis must preserve exact alpha.");
    }

    private static void CheckOriginalArmourRecolour()
    {
        // Authored test pixels: neutral metal, warm trim, blue panel,
        // dark cloth, black contour, white highlight, partial alpha, skin.
        byte[] source=[120,120,120,255, 40,140,190,255, 160,80,40,255,
            40,40,40,255, 0,0,0,255, 255,255,255,255, 70,100,140,113,
            90,150,202,255, 10,20,30,0, 70,90,120,255];
        byte[] naked=new byte[source.Length],skin=new byte[source.Length];
        source.AsSpan(28,4).CopyTo(naked.AsSpan(28,4));
        source.AsSpan(28,4).CopyTo(skin.AsSpan(28,4));
        // A bad isolation mask must never add pixels outside the source
        // silhouette or replace a covered metal pixel with naked flesh.
        skin[35]=255;skin[39]=255;
        naked[36]=60;naked[37]=140;naked[38]=210;naked[39]=255;
        var before=(byte[])source.Clone();
        var variants=new HashSet<(int,int)>();
        for(int id=2;id<=99;id++)
        {
            var design=ArmourRecolourDesign.Resolve(id);
            Assertions.True(design.Source==(id-2)%11+1 &&
                variants.Add((design.Source,design.Profile)),
                "Each appearance uses one original source and a unique palette.");
            var result=ArmourRecolour.Apply(source,naked,skin,design.Profile);
            var again=ArmourRecolour.Apply(source,naked,skin,design.Profile);
            Assertions.True(result.Surface.AsSpan().SequenceEqual(again.Surface),"Recolour is deterministic.");
            for(int p=0;p<source.Length;p+=4)
            {
                Assertions.True(result.Surface[p+3]+result.Skin[p+3]==source[p+3],
                    "Recolour and overlay together preserve every source alpha exactly.");
                Assertions.True(result.Surface[p+3]==0||result.Skin[p+3]==0,
                    "No double-drawn pixels.");
            }
            Assertions.True(result.Skin.AsSpan(28,4).SequenceEqual(source.AsSpan(28,4)) &&
                result.Skin[35]==0 && result.Skin[39]==0,
                "Only source-supported skin pixels are restored, never extra geometry.");
            Assertions.True(result.Surface.AsSpan(16,8).SequenceEqual(source.AsSpan(16,8)),
                "Black contours and white highlights must remain intact.");
            Assertions.True(!result.Surface.AsSpan(0,12).SequenceEqual(source.AsSpan(0,12)),
                "Palette must actually recolour the original material.");
            // A neutral ramp uses continuous monotone tone curves. This
            // catches collapsed shadows, inverted highlights and clipping.
            float last=-1;
            for(int v=0;v<=255;v++)
            {
                Vector3 rgb=ArmourRecolour.Colour(new Vector3(v/255f),design.Profile);
                float luma=ArmourRecolour.Luma(rgb);
                Assertions.True(luma>=last-.00001f && rgb.X>=0 && rgb.Y>=0 && rgb.Z>=0 &&
                    rgb.X<=1 && rgb.Y<=1 && rgb.Z<=1,"Preserve tonal order and gamut.");
                last=luma;
            }
        }
        Assertions.True(source.AsSpan().SequenceEqual(before),"Original sprite is immutable.");
    }

    private static void CheckLayeredArmourColours()
    {
        // Four distinct original colour families at several authored shades.
        byte[] art=[30,50,150,255, 50,80,220,255, 35,140,45,255, 55,210,65,255,
            170,70,30,255, 235,100,45,255, 45,45,45,255, 130,130,130,255,
            90,150,202,255, 0,0,0,255, 255,255,255,255, 0,0,0,0];
        byte[] naked=new byte[art.Length],isolated=new byte[art.Length];
        art.AsSpan(32,4).CopyTo(naked.AsSpan(32,4));art.AsSpan(32,4).CopyTo(isolated.AsSpan(32,4));
        var parts=ArmourLayeredRecolour.Separate(art,naked,isolated);
        var histogram=new ArmourColourHistogram();
        histogram.Add(parts.Surface);
        var model=histogram.Build();
        var layers=ArmourLayeredRecolour.Apply(parts,model,0);
        var repeated=ArmourLayeredRecolour.Apply(parts,model,0);
        Assertions.True(model.Count==4 && layers.Layers.Length==4,"Learn four independent original colour masks.");
        var occupied=new HashSet<int>();
        for(int p=0;p<art.Length;p+=4)
        {
            int alpha=layers.Skin[p+3],owner=-1;
            for(int j=0;j<4;j++)
            {
                alpha+=layers.Layers[j][p+3];
                if(layers.Layers[j][p+3]>0)
                {
                    Assertions.True(owner<0,"Colour masks cannot overlap.");
                    owner=j;occupied.Add(j);
                }
                Assertions.True(layers.Layers[j].AsSpan(p,4).SequenceEqual(repeated.Layers[j].AsSpan(p,4)),
                    "Fixed source palette gives deterministic frame assignment.");
            }
            Assertions.True(alpha==art[p+3],"Keep the source silhouette and alpha.");
            if(p==32)Assertions.True(layers.Skin.AsSpan(p,4).SequenceEqual(art.AsSpan(p,4)),"Skin remains original.");
            if(p==36||p==40)Assertions.True(layers.Surface.AsSpan(p,4).SequenceEqual(art.AsSpan(p,4)),"Preserve black and white.");
        }
        Assertions.True(occupied.Count==4,"All four original colour families should be populated.");
        for(int p=0;p<24;p+=8)
            Assertions.True(model.Classify(new Vector3(art[p+2],art[p+1],art[p])/255f)==
                model.Classify(new Vector3(art[p+6],art[p+5],art[p+4])/255f),
                "A material's lighter shade must not switch palette slot.");
        var one=new ArmourColourHistogram();one.Add(new byte[]{20,40,100,255});
        Assertions.True(one.Build().Count==1,"A genuinely single-colour source must not invent regions.");
    }

    private static void CheckSideWingProjection()
    {
        foreach(int direction in new[]{2,6})
        {
            foreach(float y in new[]{-9f,-1f,0f,2f,3f,9f})
            {
                var root=WingProfilePlacement.Socket(new Vector2(2,y),direction);
                Assertions.True(root.Y>=.5f&&root.Y<=3.5f,
                    "Profile roots sit close to the upper back without rising above it.");
                float previous=Math.Clamp(y,-1,3)+2.5f;
                Assertions.True(previous-root.Y>=.999f&&previous-root.Y<=2.001f,
                    "Raise the previous profile socket by only one to two pixels.");
            }
            float across=direction==2?.49f:-.49f;
            foreach(float lateral in new[]{-60f,60f})
            {
                float vertical=WingProfilePlacement.SpanY(direction,across,lateral);
                Assertions.True(Math.Abs(vertical)<=8.401f,"Limit excessive profile span.");
                Assertions.True(Math.Abs(vertical-WingProfilePlacement.SpanY(8-direction,-across,-lateral))<.001f,
                    "Left and right retain mirrored projection.");
            }
            Assertions.True(WingProfilePlacement.FeatherHeight(direction,-20)==-20 &&
                WingProfilePlacement.FeatherHeight(direction,0)==0 &&
                Math.Abs(WingProfilePlacement.FeatherHeight(direction,40)-34)<.001f,
                "Shorten lower feathers only, preserving the upper crown and root.");
            Assertions.True(WingProfilePlacement.Rise(direction,12)<=2,"Legacy lift must not stack on side facings.");
        }
        foreach(int direction in new[]{0,1,3,4,5,7})
            Assertions.True(WingProfilePlacement.Socket(new Vector2(2,-9),direction)==new Vector2(2,-9) &&
                WingProfilePlacement.SpanY(direction,.49f,60)==.49f*60 &&
                WingProfilePlacement.FeatherHeight(direction,40)==40 && WingProfilePlacement.Rise(direction,12)==12,
                "Unaffected facings retain their original projection.");
    }

    private static void CheckArmourAnatomicalProtection()
    {
        const int w=24,h=36;
        byte[] art=new byte[w*h*4],naked=new byte[art.Length],isolated=new byte[art.Length];
        void Pixel(byte[] a,int x,int y,byte r,byte g,byte b,byte alpha=255)
        {
            int p=(y*w+x)*4;a[p]=b;a[p+1]=g;a[p+2]=r;a[p+3]=alpha;
        }
        Vector2[] anchors=[new(12,3),new(12,8),new(7,10),new(18,10),new(12,12),
            new(12,17),new(12,21),new(9,27),new(16,27),new(9,33),new(16,33)];
        Pixel(art,12,2,22,25,33);Pixel(naked,12,2,130,115,100); // dark head detail
        Pixel(art,13,4,192,142,98);Pixel(naked,13,4,180,135,93);
        Pixel(art,13,5,94,85,89,120);Pixel(naked,13,5,120,100,90); // shaded jaw
        Pixel(art,3,18,184,130,92);Pixel(naked,3,18,184,130,92);
        Pixel(isolated,3,18,184,130,92);
        Pixel(art,4,18,90,79,82);Pixel(naked,4,18,116,91,72); // adjacent finger
        Pixel(art,20,18,184,130,92);Pixel(naked,20,18,184,130,92);
        Pixel(art,12,12,192,142,98);Pixel(naked,12,12,170,125,88); // copper breastplate
        Pixel(art,12,14,35,80,130);Pixel(naked,12,14,180,135,93);
        Pixel(isolated,23,35,200,140,100); // stray mask outside source
        var parts=ArmourLayeredRecolour.Separate(w,h,art,naked,isolated,anchors);
        var protectedPixels=new[]{(12,2),(13,4),(13,5),(3,18),(4,18),(20,18)};
        foreach(var (x,y) in protectedPixels)
        {
            int p=(y*w+x)*4;
            Assertions.True(parts.Skin.AsSpan(p,4).SequenceEqual(art.AsSpan(p,4))&&parts.Surface[p+3]==0,
                "Head, hands and shaded boundaries are excluded before learning the armour palette.");
        }
        int chest=(12*w+12)*4;
        Assertions.True(parts.Surface.AsSpan(chest,4).SequenceEqual(art.AsSpan(chest,4)),
            "A similar warm armour colour outside the head/hand region must remain available for recolouring.");
        var histogram=new ArmourColourHistogram();histogram.Add(parts.Surface);
        var model=histogram.Build();
        for(int profile=0;profile<9;profile++)
        {
            var result=ArmourLayeredRecolour.Apply(parts,model,profile);
            for(int p=0;p<art.Length;p+=4)
            {
                int alpha=result.Skin[p+3];
                for(int j=0;j<4;j++)alpha+=result.Layers[j][p+3];
                Assertions.True(alpha==art[p+3],"Protected skin and four masks conserve authored alpha.");
            }
            foreach(var (x,y) in protectedPixels)
            {
                int p=(y*w+x)*4;
                Assertions.True(result.Skin.AsSpan(p,4).SequenceEqual(art.AsSpan(p,4)) &&
                    result.Layers.All(layer=>layer[p+3]==0),"No material or effect layer can contain protected pixels.");
            }
        }
    }

    private static void CheckArmourMaterialRamps()
    {
        // Each target is a different hue. Shadows and highlights must retain
        // the source value ordering rather than becoming flat colour blocks.
        foreach(uint dye in new uint[]{0x376BC8,0xD1A355,0xB9D9F5,0x65324D,0x353942})
        for(int slot=0;slot<4;slot++)
        {
            float last=-1;
            for(int i=0;i<=255;i++)
            {
                float y=i/255f;
                var c=ArmourLayeredRecolour.MaterialColour(new Vector3(y),dye,slot);
                float value=ArmourRecolour.Luma(c);
                Assertions.True(value>=last-.00001f && c.X>=0&&c.X<=1&&c.Y>=0&&c.Y<=1&&c.Z>=0&&c.Z<=1,
                    "Material gradient must preserve relief order and stay in gamut.");
                Assertions.True(Math.Abs(value-y)<.09f,"Do not erase native shading.");
                last=value;
            }
        }
        var dark=ArmourLayeredRecolour.MaterialColour(new Vector3(.25f),0x376BC8,0);
        var light=ArmourLayeredRecolour.MaterialColour(new Vector3(.72f),0x376BC8,0);
        Vector3 a=dark-new Vector3(ArmourRecolour.Luma(dark));
        Vector3 b=light-new Vector3(ArmourRecolour.Luma(light));
        Assertions.True(Vector3.Distance(Vector3.Normalize(a),Vector3.Normalize(b))>.04f,
            "A material has a multi-hue shadow/highlight response, not a single colour wash.");
    }

    private static void CheckReferenceArmourBlend()
    {
        for(int profile=0;profile<9;profile++)
        for(int slot=0;slot<4;slot++)
        {
            var ramp=ArmourLayeredRecolour.ReferenceRamp(profile,slot);
            float previous=-1;
            for(int i=0;i<=255;i++)
            {
                float y=i/255f;
                Vector3 output=ArmourLayeredRecolour.BlendReference(new Vector3(y),ramp);
                float value=ArmourRecolour.Luma(output);
                Assertions.True(value>=previous-.00001f,"Soft-light value blend must remain monotonic.");
                Assertions.True(Math.Abs(value-y)<.12f && output.X>=0&&output.Y>=0&&output.Z>=0&&
                    output.X<=1&&output.Y<=1&&output.Z<=1,"Keep authored values and gamut.");
                previous=value;
            }
            Assertions.True(ArmourLayeredRecolour.BlendReference(Vector3.Zero,ramp)==Vector3.Zero &&
                ArmourLayeredRecolour.BlendReference(Vector3.One,ramp)==Vector3.One,
                "No additive wash over original black or white.");
        }
        var muted=ArmourLayeredRecolour.BlendReference(new Vector3(.5f),
            ArmourLayeredRecolour.ReferenceRamp(2,0));
        Assertions.True(Math.Abs(muted.X-muted.Z)<.14f,"Dark neutral reference should remain a subdued material.");
    }

    private static void CheckMaterialReconstruction()
    {
        const int w=16,h=16;
        byte[] art=new byte[w*h*4],skin=new byte[art.Length];
        for(int y=2;y<14;y++)for(int x=2;x<14;x++)
        {
            int p=(y*w+x)*4;
            byte v=(byte)(x<8?80:125);
            if(y==8)v=200; // authored engraved ridge
            if(y==9)v=40;  // authored recess
            art[p]=(byte)(x<8?v:Math.Min(255,v+35));art[p+1]=v;
            art[p+2]=(byte)(x<8?Math.Min(255,v+28):v);
            art[p+3]=(byte)(x==2?113:255);
        }
        int head=(3*w+7)*4;art.AsSpan(head,4).CopyTo(skin.AsSpan(head,4));art.AsSpan(head,4).Clear();
        var source=new ArmourSourceParts(art,skin);
        var histogram=new ArmourColourHistogram();histogram.Add(art);
        var model=histogram.Build();
        var map=ArmourMaterialMap.Analyse(w,h,source,model);
        var again=ArmourMaterialMap.Analyse(w,h,source,model);
        Assertions.True(map.Features.SequenceEqual(again.Features)&&map.Weights.SequenceEqual(again.Weights),
            "Material extraction is deterministic.");
        Assertions.True(map.Features[head+3]==0 && map.Weights[head/4]==Vector4.Zero,
            "Protected anatomy must not participate in material analysis.");
        int ridge=(8*w+5)*4,recess=(9*w+5)*4;
        Assertions.True(map.Features[ridge+2]>map.Features[(5*w+5)*4+2] &&
            map.Features[recess+1]>map.Features[(5*w+5)*4+1],
            "Ridges and recesses must respond to original image structure.");
        int soft=0;
        foreach(Vector4 v in map.Weights)
        {
            float sum=v.X+v.Y+v.Z+v.W;
            Assertions.True(sum==0||Math.Abs(sum-1)<.00001f,"Material coverage is normalized.");
            if(v.X>.01f&&v.Y>.01f)soft++;
        }
        Assertions.True(soft>0,"Colour decomposition supports more than one contributing material.");
        var ids=new HashSet<int>();
        for(int id=2;id<=99;id++)
        {
            var design=ArmourRecolourDesign.Resolve(id);
            var result=ArmourMaterialReconstruction.Render(source,map,design.Profile);
            Assertions.True(ids.Add(id)&&result.Skin.AsSpan().SequenceEqual(skin),"Every generated appearance preserves skin.");
            for(int p=0;p<art.Length;p+=4)
            {
                int alpha=0;for(int j=0;j<4;j++)alpha+=result.Layers[j][p+3];
                Assertions.True(alpha==art[p+3]&&result.Surface[p+3]==art[p+3],
                    "Combined and export layers preserve the original coverage.");
                if(art[p+3]==0)Assertions.True(result.Surface.AsSpan(p,4).SequenceEqual(new byte[4]),
                    "No effects or colour outside the material mask.");
            }
        }
        var flat=new byte[art.Length];for(int p=0;p<flat.Length;p+=4)
        {flat[p]=flat[p+1]=flat[p+2]=100;flat[p+3]=255;}
        var flatSource=new ArmourSourceParts(flat,new byte[flat.Length]);
        var flatMap=ArmourMaterialMap.Analyse(w,h,flatSource,model);
        Assertions.True(flatMap.Features[(8*w+8)*4+2]==0&&flatMap.Features[(8*w+8)*4+1]==0,
            "Do not invent relief on flat artwork.");
    }

    private static void Main()
    {
        CheckLiveArmourSplit();
        CheckOriginalArmourRecolour();
        CheckLayeredArmourColours();
        CheckArmourAnatomicalProtection();
        CheckArmourMaterialRamps();
        CheckReferenceArmourBlend();
        CheckMaterialReconstruction();
        CheckSideWingProjection();
        CheckArmourPatternTransfer();
        CheckArmourDesigns();
        CheckArmourFeatureSeams();
        CheckWingBodyTracking();
        CheckWingPoseMotion();
        CheckUpperBackTracking();
        CheckTorsoFrameTracking();
        CheckIndependentWingSockets();
        CheckInverseWingTwist();
        CheckLegacyWingLift();

        Span<SpriteVertex> vertices = stackalloc SpriteVertex[6];
        Assertions.True(SpriteGeometry.WriteQuad(vertices, new Rectangle(16, 8, 32, 24),
            64, 64, new Vector2(7, -3), Matrix4x4.Identity, Vector4.One, 100, 100),
            "A partly visible offset frame must survive viewport culling.");
        Assertions.True(vertices[0].Position == new Vector2(7, -3) &&
                        vertices[5].Position == new Vector2(7, 21),
            "Offset and crop size must survive vertex generation.");
        Assertions.True(vertices[0].Uv == new Vector2(0.25f, 0.125f) &&
                        vertices[4].Uv == new Vector2(0.75f, 0.5f),
            "Source rectangles must sample the original atlas region.");
        Assertions.True(!SpriteGeometry.WriteQuad(vertices, new Rectangle(0, 0, 20, 20),
            20, 20, new Vector2(101, 0), Matrix4x4.Identity, Vector4.One, 100, 100),
            "Fully offscreen sprites must not consume a batch slot.");
        var shear = Matrix4x4.Identity;
        shear.M21 = -2;
        Assertions.True(SpriteGeometry.WriteQuad(vertices, new Rectangle(0, 0, 20, 20),
            20, 20, new Vector2(110, 5), shear, Vector4.One, 100, 100),
            "Projected shadows intersecting the viewport must not be culled by their origin.");
        Assertions.True(!SpriteGeometry.WriteQuad(vertices, new Rectangle(0, 0, 20, 20),
            20, 20, new Vector2(float.NaN, 0), Matrix4x4.Identity, Vector4.One, 100, 100),
            "Non-finite vertices must never reach the GPU.");
        Assertions.True(!new ShadowSourcePolicy("").Allows("Data/Human/000000.Lib"),
            "Baked artwork must not acquire replacement shadows by default.");
        var unshadowed = new ShadowSourcePolicy("Data/Human/000000.Lib;Data/Monster/01.Lib");
        Assertions.True(unshadowed.Allows(@".\data\human\000000.lib") &&
                        !unshadowed.Allows("Data/NPC/000000.Lib") &&
                        !unshadowed.Allows("Data/Human/../NPC/000000.Lib"),
            "Shadow permission must be explicit for the exact library, without filename collisions.");

        // The Mir sprite already contains its original artist-painted lighting
        // and shadows. Raw BGRA pixels, offsets and alpha must be preserved.
        byte[] source = { 13, 24, 230, 255, 2, 60, 87, 128, 0, 0, 0, 0, 90, 45, 22, 90 };
        byte[] upload = BakedArtPolicy.CopyBgraExact(source, 2, 2);
        Assertions.True(upload.SequenceEqual(source),
            "Baked sprite colours and alpha must remain byte-for-byte identical.");
        upload[0] = 0;
        Assertions.True(source[0] == 13, "Upload must never mutate decoded sprite source.");

        foreach (int phase in new[] { 0, 1 })
        {
            const int width = 12, height = 10, pitch = 52;
            byte[] world = new byte[pitch * height];
            for (int y = 2; y < 8; y++)
            for (int x = 3; x < 10; x++)
            {
                if (((x + y) & 1) != phase) continue;
                int p = y * pitch + x * 4;
                world[p + 2] = 8;
                world[p + 3] = 255;
            }
            world[4] = 77;
            world[7] = 255;
            world[9] = 4;
            world[11] = 255;
            Assertions.True(BakedShadowRepair.Smooth(world, width, height, pitch),
                "Repair checker shadows in either parity with a padded row pitch.");
            for (int x = 5; x < 8; x++)
            {
                int p = 4 * pitch + x * 4;
                Assertions.True(world[p + 2] == 0 && world[p + 3] is >= 120 and <= 128,
                    "Replace the opaque dots and transparent holes with stable translucent coverage.");
            }
            Assertions.True(world[4] == 77 && world[7] == 255 && world[9] == 4 && world[11] == 255,
                "Preserve nearby colour and solid dark details.");
            byte[] softened = world.ToArray();
            Assertions.True(!BakedShadowRepair.Smooth(world, width, height, pitch) && world.SequenceEqual(softened),
                "Do not reprocess already-softened artwork.");
        }
        byte[] darkTiles = Enumerable.Repeat(new byte[] { 0, 0, 8, 255, 30, 40, 50, 255 }, 32)
            .SelectMany(pair => pair).ToArray();
        Assertions.True(!BakedShadowRepair.Smooth(darkTiles, 8, 8, 32),
            "Opaque dark tile patterns are not transparent checker shadows.");
        CheckShadowTileEdges();

        BakedSpriteCommand command = BakedArtPolicy.ForArtwork(7, 10, 20, 2, 2);
        Assertions.True(command.TintR == 255 && command.TintG == 255 &&
                        command.TintB == 255 && command.TintA == 255,
            "Default sprite path must never darken or colour baked artwork.");
        Assertions.True(command.Blend == SpriteBlend.NonPremultiplied,
            "Legacy straight-alpha BGRA must use non-premultiplied blend.");
        Assertions.True(!command.RecalculateNormals && !command.CastNewShadow,
            "Baked light and shadow must never be replaced by default.");
        var glow = BakedArtPolicy.ForExplicitGlow(7, 10, 20, 2, 2, 80, 120, 240, 42);
        Assertions.True(glow.Blend == SpriteBlend.Additive && glow.TintB == 240,
            "Additional light must be a separately requested overlay.");

        var commandList = new[]
        {
            BakedArtPolicy.ForArtwork(2, 0, 0, 48, 32),
            BakedArtPolicy.ForArtwork(2, 48, 0, 48, 32),
            BakedArtPolicy.ForArtwork(3, 0, 0, 48, 32),
            BakedArtPolicy.ForArtwork(2, 0, 32, 48, 32)
        };
        var runs = SpriteRunPlanner.GroupAdjacent(commandList);
        Assertions.Equal(3, runs.Count, "Batching may merge only adjacent sprites.");
        Assertions.Equal(2, runs[0].Count, "Adjacent matching frames should batch.");
        Assertions.Equal(1, runs[1].Count, "Do not reorder overlapping layers.");
        Assertions.Equal(1, runs[2].Count, "Do not reorder later frames back into first run.");

        // Test-only encoded fixture using the exact v3, 17-byte header and
        // gzip layout observed in the current MLibrary implementation.
        byte[] fixture = MakeTestLib(source);
        using var file = new MemoryStream(fixture, writable: false);
        using var lib = new LegacyMirLibReader(file, leaveOpen: true);
        Assertions.Equal(1, lib.Count, "Legacy version-three frame count.");
        LegacySpriteFrame frame = lib.ReadFrame(0);
        Assertions.Equal(2, frame.Width, "Width must match original LIB metadata.");
        Assertions.Equal(2, frame.Height, "Height must match original LIB metadata.");
        Assertions.Equal(7, frame.OffsetX, "Original sprite draw offset must be preserved.");
        Assertions.Equal(-3, frame.OffsetY, "Original sprite draw offset must be preserved.");
        Assertions.True(frame.Bgra.SequenceEqual(source), "Gzip BGRA pixels must be preserved.");
        Assertions.True(frame.Mask == null, "Unmasked frames stay unmasked.");

        // Mir3 two-layer sprites carry an independent authored auxiliary mask.
        // The new renderer must decode it without altering the baked base RGB.
        using var maskedArchive = new MemoryStream(MakeTestLib(source, includeMask: true));
        using var maskedLib = new LegacyMirLibReader(maskedArchive);
        LegacySpriteFrame layered = maskedLib.ReadFrame(0, includeMask: true);
        Assertions.True(layered.Bgra.SequenceEqual(source),
            "Reading a mask must not rewrite the base image.");
        Assertions.True(layered.Mask != null && layered.Mask.Bgra.SequenceEqual(source),
            "Second-layer mask BGRA must decompress independently.");
        Assertions.Equal(4, layered.Mask!.OffsetX,
            "Authored mask offsets must be preserved.");
        Assertions.Equal(-6, layered.Mask.OffsetY,
            "Authored mask offsets must be preserved.");
        Assertions.True(maskedLib.ReadFrame(0).Mask == null,
            "Mask decoding must remain opt-in.");
        Assertions.True(file.CanRead, "LeaveOpen must respect stream ownership.");
        Assertions.True(!lib.TryReadFrame(-1, out _) &&
                        !lib.TryReadFrame(4, out _), "Invalid image indices must fail safely.");

        byte[] malformed = (byte[])fixture.Clone();
        // Corrupt compressed payload; decoder must not silently return garbage.
        malformed[33] = 0; // GZip magic, after the 17-byte frame header.
        using var badStream = new MemoryStream(malformed);
        using var bad = new LegacyMirLibReader(badStream);
        bool rejected = false;
        try { bad.ReadFrame(0); }
        catch (InvalidDataException) { rejected = true; }
        Assertions.True(rejected, "Malformed compressed frame must be rejected.");

        CheckSpellParticles();
        Console.WriteLine("Baked-art fidelity, batching, LIB layout and spell particle checks passed.");
    }

    private static void CheckSpellParticles()
    {
        // F12 spell compositing must behave consistently for authored coverage
        // particles and for legacy spell textures. Keying is unnecessary for an
        // alpha coverage atlas, but glow-only and cutoff still matter.
        var authoredParticles = new SpellParticleGlowPolicy(0, .44f, .7f, false);
        var keyedParticles = new SpellParticleGlowPolicy(1, .44f, .7f, false);
        var enhancedParticles = new SpellParticleGlowPolicy(2, .44f, .7f, false);
        var glowOnlyParticles = new SpellParticleGlowPolicy(3, .44f, .7f, true);
        Assertions.True(authoredParticles.ShowAuthored && !authoredParticles.ShowGlow &&
                        keyedParticles.ShowAuthored && !keyedParticles.ShowGlow,
            "Authored and black-key particle modes must not add a glow pass.");
        Assertions.True(enhancedParticles.ShowAuthored && enhancedParticles.ShowGlow &&
                        !glowOnlyParticles.ShowAuthored && glowOnlyParticles.ShowGlow &&
                        glowOnlyParticles.ScreenBlend,
            "Glow-plus-base and glow-only must select the correct passes and blend mode.");
        Assertions.True(enhancedParticles.GlowAmount(new Vector3(.2f, .1f, .08f)) == 0f &&
                        enhancedParticles.GlowAmount(Vector3.One) > .69f,
            "The F12 glow cutoff should reject dark particles and retain bright sparks.");
        Assertions.True(new SpellParticleGlowPolicy(2, 1f, 2f, false).GlowAmount(Vector3.One) == 0f,
            "A maximum cutoff must suppress glow, even for the brightest particle.");
        Assertions.True(new SpellParticleGlowPolicy(2, 0f, 0f, false).GlowAmount(Vector3.One) == 0f &&
                        !new SpellParticleGlowPolicy(3, 0f, 0f, true).ShowGlow,
            "Zero glow power must disable the glow layer, including glow-only mode.");
        Assertions.True(new SpellParticleGlowPolicy(2, 0f, 2f, true).GlowAmount(Vector3.One) > 1.9f,
            "Glow power up to 200 percent must scale particle highlights.");

        var pool = new SpellParticlePool(512);
        Vector2 origin = new(120, 96);
        var particle = new SpellParticle(origin, new Vector2(32, 0), 100, 200, 12,
            Color.OrangeRed, SpellParticleShape.Spark, 0);
        Assertions.True(!pool.TryAdd(particle with { Duration = 0 }), "Reject invalid particle lifetimes.");
        Assertions.True(pool.TryAdd(particle), "An active spell should occupy one pool slot.");
        Assertions.Equal(new Vector2(144, 96), particle.Position(200), "World motion must use the game clock.");
        Assertions.True(pool.Burst(origin, Vector2.Zero, Color.CornflowerBlue, 100, 700, SpellParticleShape.Shard),
            "Accept an oversized burst up to the fixed scene budget.");
        Assertions.Equal(512, pool.Active.Length, "Pool capacity must remain fixed.");
        Assertions.True(!pool.Burst(origin, Vector2.Zero, Color.White, 100, 1), "Reject overflow in a full scene.");
        Assertions.True(pool.Advance(300) && pool.Active[0] != particle, "Remove expired particles in place.");
        Assertions.True(pool.Advance(730) && pool.Active.IsEmpty && !pool.Advance(731),
            "Redraw once when the last particle expires.");
        pool.TryAdd(particle);
        pool.Clear();
        Assertions.True(pool.Active.IsEmpty, "A map reset must drop every active particle.");

        var later = particle with { Start = 400, Shape = SpellParticleShape.Halo };
        var sooner = particle with { Start = 200, Shape = SpellParticleShape.Shard };
        Assertions.True(pool.Schedule(later) && pool.Schedule(sooner), "Schedule layers out of order.");
        Assertions.True(!pool.Advance(199) && pool.Active.IsEmpty, "Future layers must stay hidden.");
        Assertions.True(pool.Advance(200) && pool.Active.Length == 1 && pool.Active[0] == sooner,
            "An earlier layer must start even when a later one was queued first.");
        Assertions.True(pool.Advance(450) && pool.Active.Length == 1 && pool.Active[0] == later &&
            pool.Active[0].Age(450) == .25f, "Preserve layer age when a frame arrives late.");
        pool.Schedule(later with { Start = 700 });
        pool.Clear();
        Assertions.True(!pool.Advance(750), "A map reset must cancel queued layers.");
        pool.Schedule(sooner);
        Assertions.True(!pool.Advance(900), "Do not replay expired layers after a frame stall.");

        var motion = new SpellParticleMotion(new Vector2(48, 12), new Vector2(-12, 4), 10, 64, 290, 5);
        var airborne = particle with
        {
            Duration = 900, Motion = motion, FieldObjectID = 93,
            Impact = new SpellParticleImpact(SpellParticleShape.Spark, 3, 6, 300, 40)
        };
        long landing = airborne.Start + motion.LandingMilliseconds;
        Assertions.True(airborne.Position(200).Y < origin.Y && airborne.DepthAt(200) > origin.Y &&
            airborne.Angle(200) > airborne.Rotation, "Height, ground depth and spin advance on separate axes.");
        Assertions.True(pool.TryAdd(airborne) && pool.Advance(landing - 1) && pool.Active.Length == 1,
            "Keep the parent visible until it reaches the projected ground.");
        Assertions.True(pool.Advance(landing + 40) && pool.Active.Length == 3 &&
            pool.Active.ToArray().All(p => p.Start == landing && p.FieldObjectID == 93 &&
                Vector2.Distance(p.Origin, airborne.Position(landing)) < .01f),
            "Landing spawns one group of children at the physical contact point and backdates their age.");
        Assertions.True(pool.Advance(landing + 41) && pool.Active.Length == 3, "The contact must fire once.");
        Assertions.True(pool.RemoveField(93) && pool.Active.IsEmpty, "Removing a field clears its children.");
        pool.TryAdd(airborne);
        Assertions.True(pool.Advance(landing + 500) && pool.Active.IsEmpty, "Do not spawn expired contact debris after a stall.");
        pool.Schedule(airborne);
        Assertions.True(pool.Advance(landing + 40) && pool.Active.Length == 3 &&
            pool.Active.ToArray().All(p => p.Start == landing), "A late scheduled parent should enter at the child stage immediately.");
        pool.Clear();

        var gradient = new SpellParticleColourCurve((0, 0x801000), (.5f, 0xff8000), (1, 0xfffffe));
        var ember = particle with { Colour = Color.FromArgb(170, Color.White), ColourCurve = gradient };
        Assertions.True(ember.ColourAt(.75f).ToArgb() == Color.FromArgb(170, 255, 192, 127).ToArgb(),
            "An age curve interpolates its colour keys while preserving authored opacity.");
        var fastEmber = ember with { Duration = 1000, Velocity = new Vector2(100, 0), ColourCurveFullSpeed = 200 };
        Assertions.True(fastEmber.ColourAt(0).ToArgb() == Color.FromArgb(170, 255, 255, 254).ToArgb() &&
            fastEmber.ColourAt(1).ToArgb() == Color.FromArgb(170, 128, 16, 0).ToArgb(),
            "A speed curve tracks the instantaneous velocity of a decelerating particle.");
        fastEmber = fastEmber with { Motion = new SpellParticleMotion(new Vector2(100, 0), new Vector2(-100, 0), 0, 0, 0) };
        Assertions.True(fastEmber.ColourAt(0).ToArgb() == Color.FromArgb(170, 255, 128, 0).ToArgb(),
            "Simulated particles sample the colour curve from projected physics velocity.");

        byte[] atlas = SpellParticleAtlas.Generate();
        Assertions.Equal(SpellParticleAtlas.Width * SpellParticleAtlas.Height, atlas.Length,
            "All particle masks share one single-channel coverage texture.");
        Assertions.True(atlas.SequenceEqual(SpellParticleAtlas.Generate()), "Particle masks must be deterministic.");
        foreach (SpellParticleShape ribbon in new[] { SpellParticleShape.BladeRibbon, SpellParticleShape.MoonRibbon, SpellParticleShape.WindRibbon })
        {
            Rectangle part = SpellParticleAtlas.Part(ribbon);
            int row = (part.Top + 3 * part.Height / 4) * SpellParticleAtlas.Width;
            byte centre = atlas[row + part.Left + part.Width / 2];
            Assertions.True(centre > 0 && atlas[row + part.Left - 1] == centre && atlas[row + part.Right] == centre,
                "Ribbon coverage must continue across both joins when the GPU filters it.");
        }
        // Five source masks are packed into the Core assembly, not generated from
        // primitive geometry or loaded from missing runtime texture folders.
        foreach (var textured in new[]
            {
                SpellParticleShape.UserFlame, SpellParticleShape.UserLightning,
                SpellParticleShape.UserEmber, SpellParticleShape.UserSlash,
                SpellParticleShape.UserRock
            })
        {
            Rectangle part = SpellParticleAtlas.Part(textured);
            Assertions.True(part.Width == SpellParticleAtlas.Resolution &&
                part.Height == SpellParticleAtlas.Resolution,
                $"Packed texture {textured} must have a full atlas cell.");
            int covered = 0, transitions = 0;
            for (int y = part.Top; y < part.Bottom; y++)
            for (int x = part.Left; x < part.Right; x++)
            {
                byte a = atlas[y * SpellParticleAtlas.Width + x];
                if (a != 0) covered++;
                if (x > part.Left && a != atlas[y * SpellParticleAtlas.Width + x - 1])
                    transitions++;
            }
            Assertions.True(covered > 120 && transitions > 100,
                $"Packed user texture {textured} must contain detailed coverage rather than a blank primitive.");
        }
        var shapes = Enum.GetValues<SpellParticleShape>().Where(shape => shape != SpellParticleShape.Slash).ToArray();
        var distinct = new HashSet<Rectangle>();
        foreach (SpellParticleShape shape in shapes)
        {
            bool animated = shape is SpellParticleShape.FlameSequence or SpellParticleShape.FireSequence or SpellParticleShape.SmokeSequence;
            int count = animated ? SpellParticleAtlas.ClipFrames : 1;
            for (byte frame = 0; frame < count; frame++)
            {
                Rectangle region = SpellParticleAtlas.Part(shape, frame: frame);
                int minimum = animated ? 128 : 256;
                Assertions.True(distinct.Add(region) && region.Right <= SpellParticleAtlas.Width && region.Bottom <= SpellParticleAtlas.Height &&
                    region.Width >= minimum && region.Height >= minimum, $"{shape} frame {frame} needs its own full-resolution atlas cell.");
                bool hasAlpha = false;
                for (int y = region.Top; y < region.Bottom; y++)
                for (int x = region.Left; x < region.Right; x++)
                    hasAlpha |= atlas[y * SpellParticleAtlas.Width + x] != 0;
                Assertions.True(hasAlpha, $"{shape} frame {frame} has a visible mask.");
            }
        }
        var projectile = particle with { Linear = true, Layer = SpellParticleLayer.Emissive, Size = 80, Aspect = .25f, Rotation = MathF.PI / 2 };
        Assertions.Equal(new Vector2(136, 96), projectile.Position(200), "Projectiles arrive on the authored timeline.");
        Rectangle rotated = projectile.Bounds(projectile.Position(200), projectile.Dimensions(.5f, 1));
        Assertions.True(rotated.Height >= 80 && rotated.Width <= 22, "Light and viewport bounds cover the rotated projectile.");
        for (byte direction = 0; direction < 8; direction++)
        {
            Rectangle part = SpellParticleAtlas.Part(SpellParticleShape.Slash, direction);
            Assertions.True(part.Right <= SpellParticleAtlas.Width && part.Bottom <= SpellParticleAtlas.Height,
                "Every directional slash must fit inside the shared atlas.");
            bool hasAlpha = false;
            for (int y = part.Top; y < part.Bottom; y++)
            for (int x = part.Left; x < part.Right; x++)
                hasAlpha |= atlas[y * SpellParticleAtlas.Width + x] > 0;
            Assertions.True(hasAlpha, "Each facing must have a visible slash mask.");
        }
    }

    private static void CheckShadowTileEdges()
    {
        const int width = 7, height = 8, pitch = width * 4;
        byte[] left = CheckerTile(width, height, 0);
        byte[] right = CheckerTile(width, height, 1);
        byte[] previousRepair = left.ToArray();
        Assertions.True(BakedShadowRepair.Smooth(previousRepair, width, height, pitch), "Reproduce the old per-frame fade.");
        int edge = 3 * pitch + (width - 1) * 4 + 3;
        Assertions.True(previousRepair[edge] < 110, "Missing samples used to fade the edge of a cut tile.");
        Assertions.True(BakedShadowRepair.Smooth(left, width, height, pitch, joinTileEdges: true) &&
                        BakedShadowRepair.Smooth(right, width, height, pitch, joinTileEdges: true), "Repair both sides of the tile join.");
        for (int row = 0; row < height; row++)
        {
            byte leftAlpha = left[row * pitch + (width - 1) * 4 + 3];
            byte rightAlpha = right[row * pitch + 3];
            Assertions.True(leftAlpha >= 126 && Math.Abs(leftAlpha - rightAlpha) <= 2,
                "Connected map shadows must retain coverage across opposite phases at the cut.");
        }

        byte[] thinTile = CheckerTile(2, 6, 0, alpha: 144, red: 16);
        Assertions.True(BakedShadowRepair.Smooth(thinTile, 2, 6, 8, joinTileEdges: true) && thinTile[3] is >= 70 and <= 74,
            "Recognise narrow, darker-tinted shadow strips and preserve their lower opacity.");

        byte[] raw = CheckerTile(width, height, 0);
        byte[] converted = raw.ToArray();
        for (int i = 0; i < converted.Length; i += 4)
        {
            converted[i + 2] = 0;
            converted[i + 3] = 112;
        }
        byte[] edited = converted.ToArray();
        edited[2] = 127;
        byte[] editedBefore = edited.ToArray();
        Assertions.True(!BakedShadowRepair.TryUseOriginal(edited, width, height, pitch, raw.ToArray()) && edited.SequenceEqual(editedBefore),
            "Reject the older source if the current artwork differs.");
        Assertions.True(BakedShadowRepair.HasOfflineRepair(converted, width, height, pitch) &&
                        BakedShadowRepair.TryUseOriginal(converted, width, height, pitch, raw) && converted.SequenceEqual(left),
            "Rebuild an offline-converted tile from a matching backup using the same edge rules as its neighbour.");
    }

    private static byte[] CheckerTile(int width, int height, int phase, byte alpha = 255, byte red = 8)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (((x + y) & 1) != phase) continue;
            int p = (y * width + x) * 4;
            pixels[p + 2] = red;
            pixels[p + 3] = alpha;
        }
        return pixels;
    }

    private static byte[] MakeTestLib(byte[] bgra, bool includeMask = false)
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.NoCompression, leaveOpen: true))
            gzip.Write(bgra);
        using var compressedMask = new MemoryStream();
        if (includeMask)
        {
            using var gzip = new GZipStream(compressedMask, CompressionLevel.NoCompression, leaveOpen: true);
            gzip.Write(bgra);
        }

        using var raw = new MemoryStream();
        using (var writer = new BinaryWriter(raw, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(3); // library version
            writer.Write(1); // frame count
            writer.Write(0); // frame index metadata offset, ignored for pixel decoding
            writer.Write(16); // offset of frame header
            writer.Write((short)2);
            writer.Write((short)2);
            writer.Write((short)7);
            writer.Write((short)-3);
            writer.Write((short)0); // baked shadow offset x
            writer.Write((short)0); // baked shadow offset y
            writer.Write(includeMask ? (byte)0x80 : (byte)0);
            writer.Write(checked((int)compressed.Length));
            writer.Write(compressed.ToArray());
            if (includeMask)
            {
                writer.Write((short)2); // mask width
                writer.Write((short)2); // mask height
                writer.Write((short)4); // authored mask x
                writer.Write((short)-6); // authored mask y
                writer.Write(checked((int)compressedMask.Length));
                writer.Write(compressedMask.ToArray());
            }
        }
        return raw.ToArray();
    }
}
