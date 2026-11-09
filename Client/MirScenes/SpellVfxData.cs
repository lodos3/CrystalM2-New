using System.Text.Json.Serialization;
using CrystalM2.Rendering.Core;

namespace Client.MirScenes;

// Typed bindings for the editable assets in Data/SpellVfx.
internal sealed record SpellVfxData
{
    internal DebrisNodes Debris { get; init; }
    internal FrameNodes Frame { get; init; }
    internal LegacyNodes Legacy { get; init; }
    internal SignatureNodes Signature { get; init; }
    internal TaoistNodes Taoist { get; init; }
    internal WarriorNodes Warrior { get; init; }
    internal WizardNodes Wizard { get; init; }

    internal sealed class DebrisNodes
    {
        public EmitNode Emit { get; init; }
        public PaletteNode Palette { get; init; }

        public sealed class EmitNode
        {
            public SpellParticleShape FlamingSwordShape { get; init; }
            public SpellParticleShape FlamingSwordShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlamingSwordHot { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlamingSwordCool { get; init; }
            public int FlamingSwordSpeed { get; init; }
            public int FlamingSwordLift { get; init; }
            public float FlamingSwordEnergy { get; init; }
            public SpellParticleShape ThunderBoltShape { get; init; }
            public SpellParticleShape ThunderBoltShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ThunderBoltHot { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ThunderBoltCool { get; init; }
            public int ThunderBoltSpeed { get; init; }
            public int ThunderBoltLift { get; init; }
            public float ThunderBoltEnergy { get; init; }
            public SpellParticleShape IceStormShape { get; init; }
            public SpellParticleShape IceStormShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int IceStormHot { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int IceStormCool { get; init; }
            public int IceStormSpeed { get; init; }
            public int IceStormLift { get; init; }
            public float IceStormEnergy { get; init; }
            public SpellParticleShape PlagueShape { get; init; }
            public SpellParticleShape PlagueShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int PlagueHot { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int PlagueCool { get; init; }
            public int PlagueSpeed { get; init; }
            public int PlagueLift { get; init; }
            public float PlagueEnergy { get; init; }
            public int Count { get; init; }
            public float CountDivisor { get; init; }
            public int Root { get; init; }
            public int Root2 { get; init; }
            public float Drift { get; init; }
            public float Drift2 { get; init; }
            public float Drift3 { get; init; }
            public float Lift { get; init; }
            public float Lift2 { get; init; }
            public float Wind { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public int Gravity { get; init; }
            public float Spin { get; init; }
            public int Spin2 { get; init; }
            public int Birth { get; init; }
            public int SpellParticleDuration { get; init; }
            public int SpellParticleSize { get; init; }
            public int SpellParticleSize2 { get; init; }
            public float Particle { get; init; }
            public float SpellParticleAspect { get; init; }
            public float SpellParticleGrowth { get; init; }
            public SpellParticleLayer SpellParticleLayer { get; init; }
            public SpellParticleLayer SpellParticleLayer2 { get; init; }
            public int SpellParticleImpactCount { get; init; }
            public int SpellParticleImpactSize { get; init; }
            public int SpellParticleImpactDuration { get; init; }
            public int SpellParticleImpactDuration2 { get; init; }
            public int SpellParticleImpactScatter { get; init; }
            public int SpellParticleColourCurveFullSpeed { get; init; }
        }

        public sealed class PaletteNode
        {
            [JsonConverter(typeof(VfxCurveConverter))]
            public SpellParticleColourCurve SoulEmber { get; init; }
        }
    }

    internal sealed class FrameNodes
    {
        public AddNode Add { get; init; }
        public EnvelopeNode Envelope { get; init; }
        public FloorSealNode FloorSeal { get; init; }
        public LineNode Line { get; init; }
        public OrbitNode Orbit { get; init; }
        public SparksNode Sparks { get; init; }
        public ThreadNode Thread { get; init; }
        public WindRibbonNode WindRibbon { get; init; }

        public sealed class AddNode
        {
            public SpellParticleLayer Layer { get; init; }
            public int SpellParticleDuration { get; init; }
        }

        public sealed class EnvelopeNode
        {
            public float TailOpacity { get; init; }
            public float FadeStart { get; init; }
            public float RiseBase { get; init; }
            public float RiseGain { get; init; }
            public int RiseSpeed { get; init; }
        }

        public sealed class FloorSealNode
        {
            public float Right { get; init; }
            public int SpellParticleDuration { get; init; }
            public SpellParticleLayer SpellParticleLayer { get; init; }
        }

        public sealed class LineNode
        {
            public float Amount { get; init; }
            public float WispWidth { get; init; }
            public float WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public float Edge { get; init; }
            public float Edge2 { get; init; }
            public float ThreadEndWidth { get; init; }
        }

        public sealed class OrbitNode
        {
            public int Segments { get; init; }
            public float Segments2 { get; init; }
            public float Flicker { get; init; }
            public float Flicker2 { get; init; }
            public float LineAlpha { get; init; }
            public float LineAlpha2 { get; init; }
            public int Angle { get; init; }
        }

        public sealed class SparksNode
        {
            public float Age { get; init; }
            public float Point { get; init; }
            public float Point2 { get; init; }
            public float Width { get; init; }
            public float Width2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ColourColour { get; init; }
            public float Colour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ColourColour2 { get; init; }
            public float Colour2 { get; init; }
            public float Fade { get; init; }
            public float Fade2 { get; init; }
            public float Fade3 { get; init; }
            public float Fade4 { get; init; }
            public int Fade5 { get; init; }
            public float ShardHeight { get; init; }
            public float ShardHeight2 { get; init; }
            public SpellParticleLayer ShardLayer { get; init; }
            public SpellParticleLayer ShardLayer2 { get; init; }
            public float ShardRotation { get; init; }
        }

        public sealed class ThreadNode
        {
            public float Centre { get; init; }
            public float Normal { get; init; }
            public float Overlap { get; init; }
            public float Overlap2 { get; init; }
            public int FilamentDuration { get; init; }
            public SpellParticleShape FilamentShape { get; init; }
        }

        public sealed class WindRibbonNode
        {
            public float Centre { get; init; }
            public int WindRibbonDuration { get; init; }
            public SpellParticleShape WindRibbonShape { get; init; }
            public SpellParticleLayer WindRibbonLayer { get; init; }
            public bool WindRibbonCastsLight { get; init; }
        }
    }

    internal sealed class LegacyNodes
    {
        public ProjectileTimingNode ProjectileTiming { get; init; }

        public sealed class ProjectileTimingNode
        {
            public int TravelPerCellMs { get; init; }
            public int WakeIntervalMs { get; init; }
        }

        public CueTimingNode CueTiming { get; init; }

        public sealed class CueTimingNode
        {
            public int BlinkArrivalDurationMs { get; init; }
            public int CatTongueImpactDurationMs { get; init; }
            public int DarkBodyImpactDurationMs { get; init; }
            public int ElectricShockImpactDurationMs { get; init; }
            public int ElementalBarrierExpireDurationMs { get; init; }
            public int ElementalBarrierImpactLongDurationMs { get; init; }
            public int ElementalBarrierImpactMediumDurationMs { get; init; }
            public int ElementalBarrierImpactShortDurationMs { get; init; }
            public int EntrapmentImpactDurationMs { get; init; }
            public int FatalSwordImpactDurationMs { get; init; }
            public int FireBallImpactDurationMs { get; init; }
            public int FireBangImpactDurationMs { get; init; }
            public int FireBounceImpactDurationMs { get; init; }
            public int FlameDisruptorImpactDurationMs { get; init; }
            public int FrostCrunchImpactDurationMs { get; init; }
            public int GreatFireBallImpactDurationMs { get; init; }
            public int HallucinationImpactDurationMs { get; init; }
            public int HealingCircleImpactDurationMs { get; init; }
            public int HealingImpactDurationMs { get; init; }
            public int HeavenlySwordImpactDurationMs { get; init; }
            public int HemorrhageImpactDurationMs { get; init; }
            public int IceStormImpactDurationMs { get; init; }
            public int MPEaterImpactLongDurationMs { get; init; }
            public int MPEaterImpactShortDurationMs { get; init; }
            public int MagicShieldHitDurationMs { get; init; }
            public int MagicShieldImpactDurationMs { get; init; }
            public int MassHealingImpactDurationMs { get; init; }
            public int MassHidingImpactDurationMs { get; init; }
            public int MeteorShowerImpactDurationMs { get; init; }
            public int MeteorStrikeImpactDurationMs { get; init; }
            public int MirroringExpireDurationMs { get; init; }
            public int MirroringImpactDurationMs { get; init; }
            public int MoonMistImpactLongDurationMs { get; init; }
            public int MoonMistImpactShortDurationMs { get; init; }
            public int NapalmShotImpactDurationMs { get; init; }
            public int OneWithNatureImpactDurationMs { get; init; }
            public int PlagueImpactDurationMs { get; init; }
            public int PoisoningImpactDurationMs { get; init; }
            public int PurificationImpactDurationMs { get; init; }
            public int ReincarnationImpactDurationMs { get; init; }
            public int RevelationImpactDurationMs { get; init; }
            public int SoulFireBallImpactDurationMs { get; init; }
            public int StormEscapeImpactDurationMs { get; init; }
            public int StraightShotImpactDurationMs { get; init; }
            public int TeleportArrivalDurationMs { get; init; }
            public int TeleportDepartureDurationMs { get; init; }
            public int ThunderBoltImpactLongDurationMs { get; init; }
            public int ThunderBoltImpactShortDurationMs { get; init; }
            public int TrapHexagonImpactDurationMs { get; init; }
            public int TurnUndeadImpactDurationMs { get; init; }
            public int TwinDrakeBladeImpactDurationMs { get; init; }
            public int UltimateEnhancerImpactDurationMs { get; init; }
            public int VampireShotImpactDurationMs { get; init; }
            public int VampirismImpactDurationMs { get; init; }
            public int VampirismReturnDurationMs { get; init; }
        }

        public AuraNode Aura { get; init; }
        public EmitNode Emit { get; init; }
        public FieldNode Field { get; init; }
        public FieldIntervalNode FieldInterval { get; init; }
        public ImpactNode Impact { get; init; }
        public MotifProfileNode MotifProfile { get; init; }
        public PrepareNode Prepare { get; init; }
        public ProfileNode Profile { get; init; }
        public ProjectileAnchorNode ProjectileAnchor { get; init; }
        public ProjectileFrameNode ProjectileFrame { get; init; }
        public SwingNode Swing { get; init; }
        public WeaponSweepNode WeaponSweep { get; init; }
        public WindupNode Windup { get; init; }

        public sealed class AuraNode
        {
            public int AddVelocity { get; init; }
            public int AddSize { get; init; }
            public int AddDuration { get; init; }
            public SpellParticleLayer AddLayer { get; init; }
            public int AddAlpha { get; init; }
            public float AddGrowth { get; init; }
            public int SparkVelocity { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public int SparkSize { get; init; }
            public int SparkDuration { get; init; }
            public SpellParticleLayer SparkLayer { get; init; }
            public bool SparkCore { get; init; }
            public SpellParticleShape HaloShape { get; init; }
            public int HaloDuration { get; init; }
            public SpellParticleLayer HaloLayer { get; init; }
            public float HaloAspect { get; init; }
            public int HaloAlpha { get; init; }
            public int AddVelocity2 { get; init; }
            public int AddSize2 { get; init; }
            public int AddDuration2 { get; init; }
            public SpellParticleLayer AddLayer2 { get; init; }
            public float AddAspect { get; init; }
            public int AddAlpha2 { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int X2 { get; init; }
            public int Y2 { get; init; }
            public SpellParticleShape SparkShape2 { get; init; }
            public int SparkSize2 { get; init; }
            public int SparkDuration2 { get; init; }
            public SpellParticleLayer SparkLayer2 { get; init; }
            public bool SparkCore2 { get; init; }
        }

        public sealed class EmitNode
        {
            public int PrepareFactor { get; init; }
            public int AuraFactor { get; init; }
            public int BurnFactor { get; init; }
            public int SwingFactor { get; init; }
            public int Factor { get; init; }
            public int Body { get; init; }
            public int PrepareHand { get; init; }
            public int FlightOrbVelocity { get; init; }
            public SpellParticleShape FlightOrbShape { get; init; }
            public int FlightOrbSize { get; init; }
            public int FlightOrbDuration { get; init; }
            public SpellParticleLayer FlightOrbLayer { get; init; }
            public float FlightOrbAspect { get; init; }
            public int FlightOrbAlpha { get; init; }
            public int FlightAddOffset { get; init; }
            public int FlightAddVelocity { get; init; }
            public int FlightAddDuration { get; init; }
            public SpellParticleLayer FlightAddLayer { get; init; }
            public float FlightAddAspect { get; init; }
            public bool FlightAddCore { get; init; }
            public int ExpireShardCount { get; init; }
            public SpellParticleShape ExpireShardShape { get; init; }
            public int ExpireShardSpread { get; init; }
            public int ExpireShardDuration { get; init; }
            public SpellParticleShape BurnHaloShape { get; init; }
            public int BurnHaloSize { get; init; }
            public int BurnHaloDuration { get; init; }
            public SpellParticleLayer BurnHaloLayer { get; init; }
            public float BurnHaloAspect { get; init; }
            public int BurnHaloAlpha { get; init; }
            public int BurnX { get; init; }
            public int BurnY { get; init; }
            public int BurnX2 { get; init; }
            public int BurnY2 { get; init; }
            public SpellParticleShape BurnWispShape { get; init; }
            public int BurnWispSize { get; init; }
            public int BurnWispDuration { get; init; }
            public SpellParticleLayer BurnWispLayer { get; init; }
            public float BurnWispAspect { get; init; }
            public float BurnWispGrowth { get; init; }
            public int BurnSparkCount { get; init; }
            public SpellParticleShape BurnSparkShape { get; init; }
            public int BurnSparkSpread { get; init; }
            public int BurnSparkDuration { get; init; }
            public int WardRingDuration { get; init; }
            public int WardShieldVelocity { get; init; }
            public SpellParticleShape WardShieldShape { get; init; }
            public int WardShieldSize { get; init; }
            public int WardShieldDuration { get; init; }
            public SpellParticleLayer WardShieldLayer { get; init; }
            public float WardShieldAspect { get; init; }
            public float WardShieldGrowth { get; init; }
            public int WardShieldAlpha { get; init; }
        }

        public sealed class FieldNode
        {
            public float Drift { get; init; }
            public float Drift2 { get; init; }
            public float Drift3 { get; init; }
            public int Drift4 { get; init; }
            public int PoisonCloudY { get; init; }
            public int PoisonCloudX { get; init; }
            public int PoisonCloudY2 { get; init; }
            public SpellParticleShape PoisonCloudWispShape { get; init; }
            public int PoisonCloudWispSize { get; init; }
            public int PoisonCloudWispDuration { get; init; }
            public SpellParticleLayer PoisonCloudWispLayer { get; init; }
            public float PoisonCloudWispAspect { get; init; }
            public int PoisonCloudWispAlpha { get; init; }
            public float PoisonCloudWispGrowth { get; init; }
            public int PoisonCloudY3 { get; init; }
            public int PoisonCloudX2 { get; init; }
            public int PoisonCloudY4 { get; init; }
            public SpellParticleShape PoisonCloudWispShape2 { get; init; }
            public int PoisonCloudWispSize2 { get; init; }
            public int PoisonCloudWispDuration2 { get; init; }
            public SpellParticleLayer PoisonCloudWispLayer2 { get; init; }
            public float PoisonCloudWispAspect2 { get; init; }
            public int PoisonCloudWispAlpha2 { get; init; }
            public float PoisonCloudWispGrowth2 { get; init; }
            public int TrapHexagonBoltCount { get; init; }
            public int TrapHexagonPoint { get; init; }
            public int TrapHexagonPoint2 { get; init; }
            public int TrapHexagonEnd { get; init; }
            public int TrapHexagonEnd2 { get; init; }
            public float TrapHexagonBoltOffset { get; init; }
            public SpellParticleShape TrapHexagonBoltShape { get; init; }
            public int TrapHexagonBoltSize { get; init; }
            public int TrapHexagonBoltDuration { get; init; }
            public SpellParticleLayer TrapHexagonBoltLayer { get; init; }
            public float TrapHexagonBoltAspect { get; init; }
            public int TrapHexagonBoltAlpha { get; init; }
        }

        public sealed class FieldIntervalNode
        {
            public int HealingCircleIntervalMs { get; init; }
            public int FireWallIntervalMs { get; init; }
            public int DefaultIntervalMs { get; init; }
        }

        public sealed class ImpactNode
        {
            public int CleaveAddVelocity { get; init; }
            public int CleaveVal1 { get; init; }
            public int CleaveAddDuration { get; init; }
            public SpellParticleLayer CleaveAddLayer { get; init; }
            public float CleaveAddAspect { get; init; }
            public float CleaveAddRotation { get; init; }
            public bool CleaveAddCore { get; init; }
            public int CleaveSparkCount { get; init; }
            public SpellParticleShape CleaveSparkShape { get; init; }
            public int CleaveSparkSpread { get; init; }
            public int CleaveSparkDuration { get; init; }
            public int RushBoltVelocity { get; init; }
            public SpellParticleShape RushBoltShape { get; init; }
            public int RushBoltDuration { get; init; }
            public SpellParticleLayer RushBoltLayer { get; init; }
            public float RushBoltAspect { get; init; }
            public int RushWispVelocity { get; init; }
            public SpellParticleShape RushWispShape { get; init; }
            public int RushWispSize { get; init; }
            public int RushWispDuration { get; init; }
            public SpellParticleLayer RushWispLayer { get; init; }
            public int RushWispAlpha { get; init; }
            public int RushShardCount { get; init; }
            public SpellParticleShape RushShardShape { get; init; }
            public int RushShardSpread { get; init; }
            public int RushShardDuration { get; init; }
            public int WardRingDuration { get; init; }
            public int WardAddVelocity { get; init; }
            public int WardAddSize { get; init; }
            public int WardAddDuration { get; init; }
            public SpellParticleLayer WardAddLayer { get; init; }
            public float WardAddAspect { get; init; }
            public float WardAddGrowth { get; init; }
            public int WardFlashVelocity { get; init; }
            public SpellParticleShape WardFlashShape { get; init; }
            public int WardFlashDuration { get; init; }
            public SpellParticleLayer WardFlashLayer { get; init; }
            public bool WardFlashCore { get; init; }
            public int WardFeatherCount { get; init; }
            public SpellParticleShape WardFeatherShape { get; init; }
            public float WardFeatherSpread { get; init; }
            public int WardFeatherDuration { get; init; }
            public int BindRingDuration { get; init; }
            public int BindBoltCount { get; init; }
            public int BindY { get; init; }
            public int BindY2 { get; init; }
            public SpellParticleShape BindBoltShape { get; init; }
            public int BindBoltSize { get; init; }
            public int BindBoltDuration { get; init; }
            public SpellParticleLayer BindBoltLayer { get; init; }
            public float BindBoltAspect { get; init; }
            public float BindBoltRotation { get; init; }
            public float BindBoltRotation2 { get; init; }
            public int BindBoltDelay { get; init; }
            public int BindAddVelocity { get; init; }
            public int BindAddSize { get; init; }
            public int BindAddDuration { get; init; }
            public SpellParticleLayer BindAddLayer { get; init; }
            public bool BindAddCore { get; init; }
            public int VeilRingDuration { get; init; }
            public int VeilWispCount { get; init; }
            public int VeilX { get; init; }
            public int VeilY { get; init; }
            public int VeilX2 { get; init; }
            public int VeilY2 { get; init; }
            public SpellParticleShape VeilWispShape { get; init; }
            public int VeilWispSize { get; init; }
            public int VeilWispDuration { get; init; }
            public SpellParticleLayer VeilWispLayer { get; init; }
            public float VeilWispGrowth { get; init; }
            public int VeilWispAlpha { get; init; }
            public int VeilAddVelocity { get; init; }
            public int VeilAddDuration { get; init; }
            public SpellParticleLayer VeilAddLayer { get; init; }
            public bool VeilAddCore { get; init; }
            public int BreakRingDuration { get; init; }
            public int BreakAddDuration { get; init; }
            public SpellParticleLayer BreakAddLayer { get; init; }
            public float BreakAddAspect { get; init; }
            public float BreakAddGrowth { get; init; }
            public int BreakAddAlpha { get; init; }
            public bool BreakAddCore { get; init; }
            public int BreakFeatherCount { get; init; }
            public SpellParticleShape BreakFeatherShape { get; init; }
            public float BreakFeatherSpread { get; init; }
            public int BreakFeatherDuration { get; init; }
            public int RingDuration { get; init; }
            public int PillarVelocity { get; init; }
            public int PillarSize { get; init; }
            public int PillarDuration { get; init; }
            public SpellParticleLayer PillarLayer { get; init; }
            public float PillarAspect { get; init; }
            public float PillarAspect2 { get; init; }
            public float PillarGrowth { get; init; }
            public int FlashVelocity { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public int FlashSize { get; init; }
            public int FlashDuration { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashCore { get; init; }
            public int SparkCount { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkSpread { get; init; }
            public int SparkDuration { get; init; }
        }

        public sealed class MotifProfileNode
        {
            public SignatureSpellEffects.Form CelebrationForm { get; init; }
            public SpellParticleShape CelebrationShape { get; init; }
            public int CelebrationSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CelebrationColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CelebrationCore { get; init; }
            public SignatureSpellEffects.Form RadianceForm { get; init; }
            public SpellParticleShape RadianceShape { get; init; }
            public int RadianceSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int RadianceColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int RadianceCore { get; init; }
            public SignatureSpellEffects.Form CraftSuccessForm { get; init; }
            public SpellParticleShape CraftSuccessShape { get; init; }
            public int CraftSuccessSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CraftSuccessColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CraftSuccessCore { get; init; }
            public SignatureSpellEffects.Form CraftFailForm { get; init; }
            public SpellParticleShape CraftFailShape { get; init; }
            public int CraftFailSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CraftFailColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CraftFailCore { get; init; }
            public SignatureSpellEffects.Form ClickForm { get; init; }
            public SpellParticleShape ClickShape { get; init; }
            public int ClickSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ClickColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ClickCore { get; init; }
        }

        public sealed class PrepareNode
        {
            public int AddVelocity { get; init; }
            public int Val1 { get; init; }
            public int Val2 { get; init; }
            public int AddDuration { get; init; }
            public SpellParticleLayer AddLayer { get; init; }
            public float AddAspect { get; init; }
            public float AddRotation { get; init; }
            public float AddGrowth { get; init; }
            public int AddAlpha { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public int Val12 { get; init; }
            public int OrbDuration { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public int OrbDelay { get; init; }
            public bool OrbCore { get; init; }
            public int SparkCount { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public int SparkSpread { get; init; }
            public int SparkDuration { get; init; }
        }

        public sealed class ProfileNode
        {
            public SignatureSpellEffects.Form FencingForm { get; init; }
            public SpellParticleShape FencingShape { get; init; }
            public int FencingSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FencingColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FencingCore { get; init; }
            public SignatureSpellEffects.Form SlayingForm { get; init; }
            public SpellParticleShape SlayingShape { get; init; }
            public int SlayingSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SlayingColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SlayingCore { get; init; }
            public SignatureSpellEffects.Form ThrustingForm { get; init; }
            public SpellParticleShape ThrustingShape { get; init; }
            public int ThrustingSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ThrustingColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ThrustingCore { get; init; }
            public SignatureSpellEffects.Form HalfMoonForm { get; init; }
            public SpellParticleShape HalfMoonShape { get; init; }
            public int HalfMoonSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int HalfMoonColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int HalfMoonCore { get; init; }
            public SignatureSpellEffects.Form ShoulderDashForm { get; init; }
            public SpellParticleShape ShoulderDashShape { get; init; }
            public int ShoulderDashSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ShoulderDashColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ShoulderDashCore { get; init; }
            public SignatureSpellEffects.Form TwinDrakeBladeForm { get; init; }
            public SpellParticleShape TwinDrakeBladeShape { get; init; }
            public int TwinDrakeBladeSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TwinDrakeBladeColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TwinDrakeBladeCore { get; init; }
            public SignatureSpellEffects.Form EntrapmentForm { get; init; }
            public SpellParticleShape EntrapmentShape { get; init; }
            public int EntrapmentSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int EntrapmentColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int EntrapmentCore { get; init; }
            public SignatureSpellEffects.Form FlamingSwordForm { get; init; }
            public SpellParticleShape FlamingSwordShape { get; init; }
            public int FlamingSwordSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlamingSwordColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlamingSwordCore { get; init; }
            public SignatureSpellEffects.Form LionRoarForm { get; init; }
            public SpellParticleShape LionRoarShape { get; init; }
            public int LionRoarSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int LionRoarColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int LionRoarCore { get; init; }
            public SignatureSpellEffects.Form CrossHalfMoonForm { get; init; }
            public SpellParticleShape CrossHalfMoonShape { get; init; }
            public int CrossHalfMoonSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CrossHalfMoonColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CrossHalfMoonCore { get; init; }
            public SignatureSpellEffects.Form BladeAvalancheForm { get; init; }
            public SpellParticleShape BladeAvalancheShape { get; init; }
            public int BladeAvalancheSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int BladeAvalancheColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int BladeAvalancheCore { get; init; }
            public SignatureSpellEffects.Form ProtectionFieldForm { get; init; }
            public SpellParticleShape ProtectionFieldShape { get; init; }
            public int ProtectionFieldSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ProtectionFieldColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ProtectionFieldCore { get; init; }
            public SignatureSpellEffects.Form RageForm { get; init; }
            public SpellParticleShape RageShape { get; init; }
            public int RageSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int RageColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int RageCore { get; init; }
            public SignatureSpellEffects.Form CounterAttackForm { get; init; }
            public SpellParticleShape CounterAttackShape { get; init; }
            public int CounterAttackSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CounterAttackColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CounterAttackCore { get; init; }
            public SignatureSpellEffects.Form SlashingBurstForm { get; init; }
            public SpellParticleShape SlashingBurstShape { get; init; }
            public int SlashingBurstSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SlashingBurstColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SlashingBurstCore { get; init; }
            public SignatureSpellEffects.Form FuryForm { get; init; }
            public SpellParticleShape FuryShape { get; init; }
            public int FurySize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FuryColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FuryCore { get; init; }
            public SignatureSpellEffects.Form ImmortalSkinForm { get; init; }
            public SpellParticleShape ImmortalSkinShape { get; init; }
            public int ImmortalSkinSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ImmortalSkinColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ImmortalSkinCore { get; init; }
            public SignatureSpellEffects.Form BattleCryForm { get; init; }
            public SpellParticleShape BattleCryShape { get; init; }
            public int BattleCrySize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int BattleCryColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int BattleCryCore { get; init; }
            public SignatureSpellEffects.Form FatalSwordForm { get; init; }
            public SpellParticleShape FatalSwordShape { get; init; }
            public int FatalSwordSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FatalSwordColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FatalSwordCore { get; init; }
            public SignatureSpellEffects.Form PoisonSwordForm { get; init; }
            public SpellParticleShape PoisonSwordShape { get; init; }
            public int PoisonSwordSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int PoisonSwordColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int PoisonSwordCore { get; init; }
            public SignatureSpellEffects.Form HemorrhageForm { get; init; }
            public SpellParticleShape HemorrhageShape { get; init; }
            public int HemorrhageSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int HemorrhageColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int HemorrhageCore { get; init; }
            public SignatureSpellEffects.Form NapalmShotForm { get; init; }
            public SpellParticleShape NapalmShotShape { get; init; }
            public int NapalmShotSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int NapalmShotColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int NapalmShotCore { get; init; }
            public SignatureSpellEffects.Form ElementalBarrierForm { get; init; }
            public SpellParticleShape ElementalBarrierShape { get; init; }
            public int ElementalBarrierSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ElementalBarrierColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ElementalBarrierCore { get; init; }
            public SignatureSpellEffects.Form TrapForm { get; init; }
            public SpellParticleShape TrapShape { get; init; }
            public int TrapSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TrapColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TrapCore { get; init; }
            public SignatureSpellEffects.Form MoonLightForm { get; init; }
            public SpellParticleShape MoonLightShape { get; init; }
            public int MoonLightSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MoonLightColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MoonLightCore { get; init; }
            public SignatureSpellEffects.Form SummonToadForm { get; init; }
            public SpellParticleShape SummonToadShape { get; init; }
            public int SummonToadSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SummonToadColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SummonToadCore { get; init; }
            public SignatureSpellEffects.Form HasteForm { get; init; }
            public SpellParticleShape HasteShape { get; init; }
            public int HasteSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int HasteColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int HasteCore { get; init; }
            public SignatureSpellEffects.Form ConcentrationForm { get; init; }
            public SpellParticleShape ConcentrationShape { get; init; }
            public int ConcentrationSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ConcentrationColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ConcentrationCore { get; init; }
            public SignatureSpellEffects.Form StraightShotForm { get; init; }
            public SpellParticleShape StraightShotShape { get; init; }
            public int StraightShotSize { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int StraightShotColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int StraightShotCore { get; init; }
        }

        public sealed class ProjectileAnchorNode
        {
            public int FireBallY { get; init; }
            public int SoulFireBallY { get; init; }
            public int Y { get; init; }
        }

        public sealed class ProjectileFrameNode
        {
            public float Turn { get; init; }
            public SpellParticleShape CatTongueBladeEdgeShape { get; init; }
            public SpellParticleLayer CatTongueBladeEdgeLayer { get; init; }
            public float CatTongueBladeEdgeAspect { get; init; }
            public bool CatTongueBladeEdgeCore { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public int OrbSize { get; init; }
            public int OrbSizeDivisor { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public int OrbAlpha { get; init; }
            public int AddSize { get; init; }
            public int AddSizeDivisor { get; init; }
            public SpellParticleLayer AddLayer { get; init; }
            public float AddAspect { get; init; }
            public bool AddCore { get; init; }
            public int Drift { get; init; }
            public int OrbOffset { get; init; }
            public int OrbVelocity { get; init; }
            public float OrbVelocity2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public int Val2 { get; init; }
            public SpellParticleLayer OrbLayer2 { get; init; }
            public float OrbAspect { get; init; }
            public float OrbGrowth { get; init; }
            public int OrbAlpha2 { get; init; }
            public int SparkOffset { get; init; }
            public int SparkVelocity { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public int SparkSize { get; init; }
            public int Val22 { get; init; }
            public SpellParticleLayer SparkLayer { get; init; }
            public bool SparkCore { get; init; }
        }

        public sealed class SwingNode
        {
            public int BoltOffset { get; init; }
            public int BoltVelocity { get; init; }
            public SpellParticleShape BoltShape { get; init; }
            public int BoltDuration { get; init; }
            public SpellParticleLayer BoltLayer { get; init; }
            public float BoltAspect { get; init; }
            public bool BoltCore { get; init; }
            public int SparkOffset { get; init; }
            public int SparkCount { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public int SparkSpread { get; init; }
            public int SparkDuration { get; init; }
            public int ShardCount { get; init; }
            public int ShardOffset { get; init; }
            public int ShardOffset2 { get; init; }
            public int ShardVelocity { get; init; }
            public SpellParticleShape ShardShape { get; init; }
            public int ShardSize { get; init; }
            public int ShardSize2 { get; init; }
            public int ShardDuration { get; init; }
            public SpellParticleLayer ShardLayer { get; init; }
            public float ShardAspect { get; init; }
            public float ShardRotation { get; init; }
            public int ShardDelay { get; init; }
            public int CrossHalfMoonFactor { get; init; }
            public int TwinDrakeBladeFactor { get; init; }
            public float Direction { get; init; }
            public float Direction2 { get; init; }
            public float Direction3 { get; init; }
            public int AddOffset { get; init; }
            public int AddVelocity { get; init; }
            public int AddDuration { get; init; }
            public SpellParticleLayer AddLayer { get; init; }
            public float AddAspect { get; init; }
            public int AddDelay { get; init; }
            public int RingDuration { get; init; }
            public int WispOffset { get; init; }
            public int WispVelocity { get; init; }
            public int WispVelocity2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public int WispSize { get; init; }
            public int WispDuration { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public float WispGrowth { get; init; }
            public int SparkOffset2 { get; init; }
            public int SparkCount2 { get; init; }
            public SpellParticleShape SparkShape2 { get; init; }
            public float SparkSpread2 { get; init; }
            public int SparkDuration2 { get; init; }
        }

        public sealed class WeaponSweepNode
        {
            public int Offset { get; init; }
            public int Life { get; init; }
            public float Drift { get; init; }
            public int PiecesDivisor { get; init; }
            public int Span { get; init; }
            public int Span2 { get; init; }
            public float BladeEdgeOffset { get; init; }
            public SpellParticleShape BladeEdgeShape { get; init; }
            public SpellParticleLayer BladeEdgeLayer { get; init; }
            public float BladeEdgeAspect { get; init; }
            public int BladeEdgeAlpha { get; init; }
            public int BladeEdgeAlpha2 { get; init; }
            public int TipSpan { get; init; }
            public float BladeEdgeOffset2 { get; init; }
            public SpellParticleShape BladeEdgeShape2 { get; init; }
            public SpellParticleLayer BladeEdgeLayer2 { get; init; }
            public float BladeEdgeAspect2 { get; init; }
            public bool BladeEdgeCore { get; init; }
            public int BladeEdgeAlpha3 { get; init; }
            public float FlashVelocity { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public int FlashSize { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashCore { get; init; }
        }

        public sealed class WindupNode
        {
            public int ThunderBoltDuration { get; init; }
            public int RevelationDuration { get; init; }
            public int LionRoarDuration { get; init; }
            public int FlameFieldDuration { get; init; }
            public int FuryDuration { get; init; }
            public int ImmortalSkinDuration { get; init; }
            public int UltimateEnhancerDuration { get; init; }
            public int MagicBoosterDuration { get; init; }
            public int PetEnhancerDuration { get; init; }
            public int TwinDrakeBladeDuration { get; init; }
            public int ThrustingDurationDivisor { get; init; }
            public int ThrustingDelayDivisor { get; init; }
            public int BladeAvalancheDuration { get; init; }
            public int SlashingBurstDuration { get; init; }
            public int SlashingBurstDelay { get; init; }
            public int CounterAttackDuration { get; init; }
        }
    }

    internal sealed class SignatureNodes
    {
        public CastNode Cast { get; init; }
        public CellNode Cell { get; init; }
        public EffectAddNode EffectAdd { get; init; }
        public EffectLightningNode EffectLightning { get; init; }
        public EffectProjectileNode EffectProjectile { get; init; }
        public EffectRingNode EffectRing { get; init; }
        public EffectScatterNode EffectScatter { get; init; }
        public ProfileNode Profile { get; init; }
        public RelayArcNode RelayArc { get; init; }

        public sealed class CastNode
        {
            public int Windup { get; init; }
            public int Angle { get; init; }
            public float Facing { get; init; }
            public int Hand { get; init; }
            public int Hand2 { get; init; }
            public int End { get; init; }
            public int CleaveSlashOffset { get; init; }
            public int CleaveSlashVelocity { get; init; }
            public SpellParticleShape CleaveSlashShape { get; init; }
            public int CleaveSlashDuration { get; init; }
            public SpellParticleLayer CleaveSlashLayer { get; init; }
            public float CleaveSlashAspect { get; init; }
            public int CleaveVal2 { get; init; }
            public int CleaveAddOffset { get; init; }
            public int CleaveAddVelocity { get; init; }
            public int CleaveAddSize { get; init; }
            public int CleaveAddDuration { get; init; }
            public SpellParticleLayer CleaveAddLayer { get; init; }
            public float CleaveAddAspect { get; init; }
            public int CleaveVal22 { get; init; }
            public bool CleaveAddCore { get; init; }
            public int HammerAddOffset { get; init; }
            public int HammerY { get; init; }
            public int HammerVal2 { get; init; }
            public SpellParticleLayer HammerAddLayer { get; init; }
            public float HammerAddAspect { get; init; }
            public float HammerAddRotation { get; init; }
            public float HammerAddRotation2 { get; init; }
            public int HammerVal22 { get; init; }
            public int RushSize { get; init; }
            public int WardAddVelocity { get; init; }
            public int WardAddSize { get; init; }
            public int WardAddDuration { get; init; }
            public SpellParticleLayer WardAddLayer { get; init; }
            public int WardSize { get; init; }
            public int GlyphVelocity { get; init; }
            public SpellParticleShape GlyphShape { get; init; }
            public int GlyphSize { get; init; }
            public int GlyphDuration { get; init; }
            public SpellParticleLayer GlyphLayer { get; init; }
            public float GlyphGrowth { get; init; }
            public int FlashVelocity { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public int FlashSize { get; init; }
            public int Val1 { get; init; }
            public int Val2 { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashCore { get; init; }
        }

        public sealed class CellNode
        {
            public SpellParticleShape CellMarkShape { get; init; }
            public int CellMarkSize { get; init; }
            public int CellMarkDuration { get; init; }
            public SpellParticleLayer CellMarkLayer { get; init; }
            public float CellMarkAspect { get; init; }
            public int CellMarkAlpha { get; init; }
            public int RingDuration { get; init; }
            public int AddOffset { get; init; }
            public int AddVelocity { get; init; }
            public int AddSize { get; init; }
            public int AddSizeDivisor { get; init; }
            public int AddDuration { get; init; }
            public int AddDuration2 { get; init; }
            public SpellParticleLayer AddLayer { get; init; }
            public int Body { get; init; }
            public int RushX { get; init; }
            public int RushY { get; init; }
            public int RushAddDuration { get; init; }
            public SpellParticleLayer RushAddLayer { get; init; }
            public byte RushAddDirection { get; init; }
            public float RushAddAspect { get; init; }
            public float RushAddRotation { get; init; }
            public int RushFlashVelocity { get; init; }
            public SpellParticleShape RushFlashShape { get; init; }
            public int RushFlashDuration { get; init; }
            public SpellParticleLayer RushFlashLayer { get; init; }
            public bool RushFlashCore { get; init; }
            public int RushSparkCount { get; init; }
            public SpellParticleShape RushSparkShape { get; init; }
            public float RushSparkSpread { get; init; }
            public int RushSparkDuration { get; init; }
            public int HammerRingSize { get; init; }
            public int HammerRingDuration { get; init; }
            public int HammerAddOffset { get; init; }
            public int HammerAddVelocity { get; init; }
            public int HammerAddSize { get; init; }
            public int HammerAddDuration { get; init; }
            public SpellParticleLayer HammerAddLayer { get; init; }
            public float HammerAddRotation { get; init; }
            public int HammerFlashVelocity { get; init; }
            public SpellParticleShape HammerFlashShape { get; init; }
            public int HammerFlashSize { get; init; }
            public int HammerFlashSizeDivisor { get; init; }
            public int HammerFlashDuration { get; init; }
            public SpellParticleLayer HammerFlashLayer { get; init; }
            public bool HammerFlashCore { get; init; }
            public int HammerShardCount { get; init; }
            public SpellParticleShape HammerShardShape { get; init; }
            public float HammerShardSpread { get; init; }
            public int HammerShardDuration { get; init; }
            public int ArcRingDuration { get; init; }
            public int ArcPillarOffset { get; init; }
            public int ArcY { get; init; }
            public SpellParticleShape ArcPillarShape { get; init; }
            public int ArcPillarSize { get; init; }
            public int ArcPillarDuration { get; init; }
            public SpellParticleLayer ArcPillarLayer { get; init; }
            public float ArcPillarAspect { get; init; }
            public float ArcPillarGrowth { get; init; }
            public int ArcAddVelocity { get; init; }
            public int ArcAddSize { get; init; }
            public int ArcAddDuration { get; init; }
            public SpellParticleLayer ArcAddLayer { get; init; }
            public bool ArcAddCore { get; init; }
            public int ArcShardCount { get; init; }
            public SpellParticleShape ArcShardShape { get; init; }
            public SpellParticleShape ArcShardShape2 { get; init; }
            public float ArcShardSpread { get; init; }
            public int ArcShardDuration { get; init; }
            public int IceRingDuration { get; init; }
            public int IceParticleCount { get; init; }
            public int IceX { get; init; }
            public int IceY { get; init; }
            public int IceX2 { get; init; }
            public int IceY2 { get; init; }
            public int IceAddSize { get; init; }
            public int IceAddSize2 { get; init; }
            public int IceAddDuration { get; init; }
            public SpellParticleLayer IceAddLayer { get; init; }
            public float IceAddAspect { get; init; }
            public float IceAddRotation { get; init; }
            public int IceShardCount { get; init; }
            public SpellParticleShape IceShardShape { get; init; }
            public int IceShardSpread { get; init; }
            public int IceShardDuration { get; init; }
            public int TotemRingSize { get; init; }
            public int TotemRingDuration { get; init; }
            public int TotemAddOffset { get; init; }
            public int TotemY { get; init; }
            public int TotemAddSize { get; init; }
            public int TotemAddDuration { get; init; }
            public SpellParticleLayer TotemAddLayer { get; init; }
            public float TotemAddAspect { get; init; }
            public float TotemAddGrowth { get; init; }
            public int TotemFlashOffset { get; init; }
            public int TotemFlashVelocity { get; init; }
            public SpellParticleShape TotemFlashShape { get; init; }
            public int TotemFlashDuration { get; init; }
            public SpellParticleLayer TotemFlashLayer { get; init; }
            public bool TotemFlashCore { get; init; }
            public int TotemFeatherCount { get; init; }
            public SpellParticleShape TotemFeatherShape { get; init; }
            public float TotemFeatherSpread { get; init; }
            public int TotemFeatherDuration { get; init; }
            public int BreakRingDuration { get; init; }
            public int BreakY { get; init; }
            public int BreakAddDuration { get; init; }
            public SpellParticleLayer BreakAddLayer { get; init; }
            public float BreakAddGrowth { get; init; }
            public bool BreakAddCore { get; init; }
            public int BreakShardCount { get; init; }
            public SpellParticleShape BreakShardShape { get; init; }
            public float BreakShardSpread { get; init; }
            public int BreakShardDuration { get; init; }
            public int VeilRingDuration { get; init; }
            public int VeilWispCount { get; init; }
            public int VeilX { get; init; }
            public int VeilY { get; init; }
            public int VeilX2 { get; init; }
            public int VeilY2 { get; init; }
            public SpellParticleShape VeilWispShape { get; init; }
            public int VeilWispDuration { get; init; }
            public SpellParticleLayer VeilWispLayer { get; init; }
            public float VeilWispGrowth { get; init; }
            public int VeilY3 { get; init; }
            public SpellParticleShape VeilGlyphShape { get; init; }
            public int VeilGlyphSize { get; init; }
            public int VeilGlyphSizeDivisor { get; init; }
            public int VeilGlyphDuration { get; init; }
            public SpellParticleLayer VeilGlyphLayer { get; init; }
            public int AddVelocity2 { get; init; }
            public int AddSize2 { get; init; }
            public int AddDuration3 { get; init; }
            public SpellParticleLayer AddLayer2 { get; init; }
            public float AddRotation { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public int FlashSize { get; init; }
            public int FlashDuration { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashCore { get; init; }
            public int ShardCount { get; init; }
            public SpellParticleShape ShardShape { get; init; }
            public float ShardSpread { get; init; }
            public int ShardDuration { get; init; }
        }

        public sealed class EffectAddNode
        {
            public int DefaultAlpha { get; init; }
            public int ColourDivisor { get; init; }
            public int ColourDivisor2 { get; init; }
            public int ColourDivisor3 { get; init; }
            public SpellParticleLayer SpellParticleLayer { get; init; }
        }

        public sealed class EffectLightningNode
        {
            public int SegmentsDivisor { get; init; }
            public int Factor { get; init; }
            public int Factor2 { get; init; }
            public int Span { get; init; }
            public float BoltOffset { get; init; }
            public SpellParticleShape BoltShape { get; init; }
            public int BoltDuration { get; init; }
            public SpellParticleLayer BoltLayer { get; init; }
            public float Val1 { get; init; }
            public float Val2 { get; init; }
            public int Val22 { get; init; }
            public int BoltDelay { get; init; }
            public bool BoltCore { get; init; }
        }

        public sealed class EffectProjectileNode
        {
            public int Duration { get; init; }
            public int Duration2 { get; init; }
            public SpellParticleShape Head { get; init; }
            public SpellParticleShape Head2 { get; init; }
            public SpellParticleShape BoltShape { get; init; }
            public int BoltSize { get; init; }
            public SpellParticleLayer BoltLayer { get; init; }
            public float BoltAspect { get; init; }
            public SpellParticleLayer AddLayer { get; init; }
            public float AddAspect { get; init; }
            public bool AddCore { get; init; }
            public int FeatherCount { get; init; }
            public float TrailDivisor { get; init; }
            public int FeatherVelocity { get; init; }
            public float FeatherVelocity2 { get; init; }
            public SpellParticleShape FeatherShape { get; init; }
            public SpellParticleShape FeatherShape2 { get; init; }
            public int FeatherSize { get; init; }
            public int FeatherDuration { get; init; }
            public int FeatherDuration2 { get; init; }
            public SpellParticleLayer FeatherLayer { get; init; }
            public float FeatherAspect { get; init; }
            public int FeatherDelayDivisor { get; init; }
            public int ClawOffset { get; init; }
            public int Y { get; init; }
            public SpellParticleShape ClawShape { get; init; }
            public int ClawDuration { get; init; }
            public SpellParticleLayer ClawLayer { get; init; }
            public float ClawAspect { get; init; }
            public int ClawDelayDivisor { get; init; }
            public int ClawDelay { get; init; }
            public int ClawAlpha { get; init; }
        }

        public sealed class EffectRingNode
        {
            public SpellParticleShape HaloShape { get; init; }
            public SpellParticleLayer HaloLayer { get; init; }
            public float HaloAspect { get; init; }
            public float HaloGrowth { get; init; }
            public int HaloAlpha { get; init; }
            public int GlyphOffset { get; init; }
            public int GlyphVelocity { get; init; }
            public SpellParticleShape GlyphShape { get; init; }
            public int GlyphSize { get; init; }
            public int GlyphDuration { get; init; }
            public SpellParticleLayer GlyphLayer { get; init; }
            public float GlyphAspect { get; init; }
            public float GlyphGrowth { get; init; }
            public int GlyphAlpha { get; init; }
        }

        public sealed class EffectScatterNode
        {
            public float Angle { get; init; }
            public float Angle2 { get; init; }
            public float Reach { get; init; }
            public float Reach2 { get; init; }
            public float Y { get; init; }
            public int Y2 { get; init; }
            public int Y3 { get; init; }
            public int AddSize { get; init; }
            public int AddSize2 { get; init; }
            public int AddDuration { get; init; }
            public SpellParticleLayer AddLayer { get; init; }
            public float AddAspect { get; init; }
            public float AddAspect2 { get; init; }
            public int AddDelay { get; init; }
            public int AddAlpha { get; init; }
        }

        public sealed class ProfileNode
        {
            public SignatureSpellEffects.Form SteelCutForm { get; init; }
            public SpellParticleShape SteelCutShape { get; init; }
            public int SteelCutSize { get; init; }
            public SignatureSpellEffects.Form NotchbreakerForm { get; init; }
            public SpellParticleShape NotchbreakerShape { get; init; }
            public int NotchbreakerSize { get; init; }
            public SignatureSpellEffects.Form OathbrandForm { get; init; }
            public SpellParticleShape OathbrandShape { get; init; }
            public int OathbrandSize { get; init; }
            public SignatureSpellEffects.Form VeilStrikeForm { get; init; }
            public SpellParticleShape VeilStrikeShape { get; init; }
            public int VeilStrikeSize { get; init; }
            public SignatureSpellEffects.Form GaleCleaverForm { get; init; }
            public SpellParticleShape GaleCleaverShape { get; init; }
            public int GaleCleaverSize { get; init; }
            public SignatureSpellEffects.Form RedWakeForm { get; init; }
            public SpellParticleShape RedWakeShape { get; init; }
            public int RedWakeSize { get; init; }
            public SignatureSpellEffects.Form DrakesEchoForm { get; init; }
            public SpellParticleShape DrakesEchoShape { get; init; }
            public int DrakesEchoSize { get; init; }
            public SignatureSpellEffects.Form DrakefallForm { get; init; }
            public SpellParticleShape DrakefallShape { get; init; }
            public int DrakefallSize { get; init; }
            public SignatureSpellEffects.Form BulwarkSlamForm { get; init; }
            public SpellParticleShape BulwarkSlamShape { get; init; }
            public int BulwarkSlamSize { get; init; }
            public SignatureSpellEffects.Form ArcaneHammerForm { get; init; }
            public SpellParticleShape ArcaneHammerShape { get; init; }
            public int ArcaneHammerSize { get; init; }
            public SignatureSpellEffects.Form ForgefallForm { get; init; }
            public SpellParticleShape ForgefallShape { get; init; }
            public int ForgefallSize { get; init; }
            public SignatureSpellEffects.Form GatebreakerForm { get; init; }
            public SpellParticleShape GatebreakerShape { get; init; }
            public int GatebreakerSize { get; init; }
            public SignatureSpellEffects.Form BloodrushForm { get; init; }
            public SpellParticleShape BloodrushShape { get; init; }
            public int BloodrushSize { get; init; }
            public SignatureSpellEffects.Form OathstepForm { get; init; }
            public SpellParticleShape OathstepShape { get; init; }
            public int OathstepSize { get; init; }
            public SignatureSpellEffects.Form PrismStepForm { get; init; }
            public SpellParticleShape PrismStepShape { get; init; }
            public int PrismStepSize { get; init; }
            public SignatureSpellEffects.Form SpiritPounceForm { get; init; }
            public SpellParticleShape SpiritPounceShape { get; init; }
            public int SpiritPounceSize { get; init; }
            public SignatureSpellEffects.Form AncestralRelayForm { get; init; }
            public SpellParticleShape AncestralRelayShape { get; init; }
            public int AncestralRelaySize { get; init; }
            public SignatureSpellEffects.Form CinderLanceForm { get; init; }
            public SpellParticleShape CinderLanceShape { get; init; }
            public int CinderLanceSize { get; init; }
            public SignatureSpellEffects.Form FractureSpearForm { get; init; }
            public SpellParticleShape FractureSpearShape { get; init; }
            public int FractureSpearSize { get; init; }
            public SignatureSpellEffects.Form PinpointForm { get; init; }
            public SpellParticleShape PinpointShape { get; init; }
            public int PinpointSize { get; init; }
            public SignatureSpellEffects.Form SerpentNeedleForm { get; init; }
            public SpellParticleShape SerpentNeedleShape { get; init; }
            public int SerpentNeedleSize { get; init; }
            public SignatureSpellEffects.Form SpiritSparkForm { get; init; }
            public SpellParticleShape SpiritSparkShape { get; init; }
            public int SpiritSparkSize { get; init; }
            public SignatureSpellEffects.Form AncestorsRendForm { get; init; }
            public SpellParticleShape AncestorsRendShape { get; init; }
            public int AncestorsRendSize { get; init; }
            public SignatureSpellEffects.Form ArcJavelinForm { get; init; }
            public SpellParticleShape ArcJavelinShape { get; init; }
            public int ArcJavelinSize { get; init; }
            public SignatureSpellEffects.Form StormRelayForm { get; init; }
            public SpellParticleShape StormRelayShape { get; init; }
            public int StormRelaySize { get; init; }
            public SignatureSpellEffects.Form SkywireForm { get; init; }
            public SpellParticleShape SkywireShape { get; init; }
            public int SkywireSize { get; init; }
            public SignatureSpellEffects.Form ThunderNailForm { get; init; }
            public SpellParticleShape ThunderNailShape { get; init; }
            public int ThunderNailSize { get; init; }
            public SignatureSpellEffects.Form AshSealForm { get; init; }
            public SpellParticleShape AshSealShape { get; init; }
            public int AshSealSize { get; init; }
            public SignatureSpellEffects.Form KilnBurstForm { get; init; }
            public SpellParticleShape KilnBurstShape { get; init; }
            public int KilnBurstSize { get; init; }
            public SignatureSpellEffects.Form FurnaceHeartForm { get; init; }
            public SpellParticleShape FurnaceHeartShape { get; init; }
            public int FurnaceHeartSize { get; init; }
            public SignatureSpellEffects.Form CinderMineForm { get; init; }
            public SpellParticleShape CinderMineShape { get; init; }
            public int CinderMineSize { get; init; }
            public SignatureSpellEffects.Form ScarletCrucibleForm { get; init; }
            public SpellParticleShape ScarletCrucibleShape { get; init; }
            public int ScarletCrucibleSize { get; init; }
            public SignatureSpellEffects.Form GlacialGateForm { get; init; }
            public SpellParticleShape GlacialGateShape { get; init; }
            public int GlacialGateSize { get; init; }
            public SignatureSpellEffects.Form WinterglassVolleyForm { get; init; }
            public SpellParticleShape WinterglassVolleyShape { get; init; }
            public int WinterglassVolleySize { get; init; }
            public SignatureSpellEffects.Form IceManacleForm { get; init; }
            public SpellParticleShape IceManacleShape { get; init; }
            public int IceManacleSize { get; init; }
            public SignatureSpellEffects.Form SleetSnareForm { get; init; }
            public SpellParticleShape SleetSnareShape { get; init; }
            public int SleetSnareSize { get; init; }
            public SignatureSpellEffects.Form InterposeForm { get; init; }
            public SpellParticleShape InterposeShape { get; init; }
            public int InterposeSize { get; init; }
            public SignatureSpellEffects.Form GuardianTetherForm { get; init; }
            public SpellParticleShape GuardianTetherShape { get; init; }
            public int GuardianTetherSize { get; init; }
            public SignatureSpellEffects.Form PaperWardForm { get; init; }
            public SpellParticleShape PaperWardShape { get; init; }
            public int PaperWardSize { get; init; }
            public SignatureSpellEffects.Form IceglassRefugeForm { get; init; }
            public SpellParticleShape IceglassRefugeShape { get; init; }
            public int IceglassRefugeSize { get; init; }
            public SignatureSpellEffects.Form PhoenixShroudForm { get; init; }
            public SpellParticleShape PhoenixShroudShape { get; init; }
            public int PhoenixShroudSize { get; init; }
            public SignatureSpellEffects.Form StaticMantleForm { get; init; }
            public SpellParticleShape StaticMantleShape { get; init; }
            public int StaticMantleSize { get; init; }
            public SignatureSpellEffects.Form EchoFangForm { get; init; }
            public SpellParticleShape EchoFangShape { get; init; }
            public int EchoFangSize { get; init; }
            public SignatureSpellEffects.Form LanternLinkForm { get; init; }
            public SpellParticleShape LanternLinkShape { get; init; }
            public int LanternLinkSize { get; init; }
            public SignatureSpellEffects.Form GuidingWickForm { get; init; }
            public SpellParticleShape GuidingWickShape { get; init; }
            public int GuidingWickSize { get; init; }
            public SignatureSpellEffects.Form TendingBellForm { get; init; }
            public SpellParticleShape TendingBellShape { get; init; }
            public int TendingBellSize { get; init; }
            public SignatureSpellEffects.Form DawnCascadeForm { get; init; }
            public SpellParticleShape DawnCascadeShape { get; init; }
            public int DawnCascadeSize { get; init; }
            public SignatureSpellEffects.Form SpiritCommandForm { get; init; }
            public SpellParticleShape SpiritCommandShape { get; init; }
            public int SpiritCommandSize { get; init; }
            public SignatureSpellEffects.Form PommelLockForm { get; init; }
            public SpellParticleShape PommelLockShape { get; init; }
            public int PommelLockSize { get; init; }
            public SignatureSpellEffects.Form HeelHookForm { get; init; }
            public SpellParticleShape HeelHookShape { get; init; }
            public int HeelHookSize { get; init; }
            public SignatureSpellEffects.Form ShieldLockForm { get; init; }
            public SpellParticleShape ShieldLockShape { get; init; }
            public int ShieldLockSize { get; init; }
            public SignatureSpellEffects.Form IronChallengeForm { get; init; }
            public SpellParticleShape IronChallengeShape { get; init; }
            public int IronChallengeSize { get; init; }
            public SignatureSpellEffects.Form LanternEdictForm { get; init; }
            public SpellParticleShape LanternEdictShape { get; init; }
            public int LanternEdictSize { get; init; }
            public SignatureSpellEffects.Form TendonSealForm { get; init; }
            public SpellParticleShape TendonSealShape { get; init; }
            public int TendonSealSize { get; init; }
            public SignatureSpellEffects.Form AncestorsGraspForm { get; init; }
            public SpellParticleShape AncestorsGraspShape { get; init; }
            public int AncestorsGraspSize { get; init; }
            public SignatureSpellEffects.Form CausticScriptForm { get; init; }
            public SpellParticleShape CausticScriptShape { get; init; }
            public int CausticScriptSize { get; init; }
            public SignatureSpellEffects.Form DistilForm { get; init; }
            public SpellParticleShape DistilShape { get; init; }
            public int DistilSize { get; init; }
            public SignatureSpellEffects.Form SmokeChokeForm { get; init; }
            public SpellParticleShape SmokeChokeShape { get; init; }
            public int SmokeChokeSize { get; init; }
            public SignatureSpellEffects.Form CrossguardBreakForm { get; init; }
            public SpellParticleShape CrossguardBreakShape { get; init; }
            public int CrossguardBreakSize { get; init; }
            public SignatureSpellEffects.Form RedSlipForm { get; init; }
            public SpellParticleShape RedSlipShape { get; init; }
            public int RedSlipSize { get; init; }
            public SignatureSpellEffects.Form BastionBreakForm { get; init; }
            public SpellParticleShape BastionBreakShape { get; init; }
            public int BastionBreakSize { get; init; }
            public SignatureSpellEffects.Form ShedCindersForm { get; init; }
            public SpellParticleShape ShedCindersShape { get; init; }
            public int ShedCindersSize { get; init; }
            public SignatureSpellEffects.Form ThawskinForm { get; init; }
            public SpellParticleShape ThawskinShape { get; init; }
            public int ThawskinSize { get; init; }
            public SignatureSpellEffects.Form GroundingStepForm { get; init; }
            public SpellParticleShape GroundingStepShape { get; init; }
            public int GroundingStepSize { get; init; }
            public SignatureSpellEffects.Form UnbindKnotForm { get; init; }
            public SpellParticleShape UnbindKnotShape { get; init; }
            public int UnbindKnotSize { get; init; }
            public SignatureSpellEffects.Form AntivenomVeilForm { get; init; }
            public SpellParticleShape AntivenomVeilShape { get; init; }
            public int AntivenomVeilSize { get; init; }
            public SignatureSpellEffects.Form SoulUnshackleForm { get; init; }
            public SpellParticleShape SoulUnshackleShape { get; init; }
            public int SoulUnshackleSize { get; init; }
            public SignatureSpellEffects.Form RallyingStandardForm { get; init; }
            public SpellParticleShape RallyingStandardShape { get; init; }
            public int RallyingStandardSize { get; init; }
            public SignatureSpellEffects.Form FluxPylonForm { get; init; }
            public SpellParticleShape FluxPylonShape { get; init; }
            public int FluxPylonSize { get; init; }
            public SignatureSpellEffects.Form MiasmaVesselForm { get; init; }
            public SpellParticleShape MiasmaVesselShape { get; init; }
            public int MiasmaVesselSize { get; init; }
            public SignatureSpellEffects.Form SanctuaryLanternForm { get; init; }
            public SpellParticleShape SanctuaryLanternShape { get; init; }
            public int SanctuaryLanternSize { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color SteelColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color SteelColour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color BloodColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color BloodColour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color WardColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color WardColour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color FlameColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color FlameColour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color FrostColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color FrostColour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color StormColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color StormColour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color SpiritColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color SpiritColour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color VenomColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color VenomColour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color ShadowColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color ShadowColour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color Colour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color Colour2 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color Colour3 { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color Colour4 { get; init; }
        }

        public sealed class RelayArcNode
        {
            public int Chest { get; init; }
            public int Size { get; init; }
        }
    }

    internal sealed class TaoistNodes
    {
        public BurningWardNode BurningWard { get; init; }
        public CleanseNode Cleanse { get; init; }
        public CloudNode Cloud { get; init; }
        public CurseRuneNode CurseRune { get; init; }
        public DrawFrameNode DrawFrame { get; init; }
        public DrawPoisonFieldNode DrawPoisonField { get; init; }
        public DrawProjectileNode DrawProjectile { get; init; }
        public EnhanceNode Enhance { get; init; }
        public HealNode Heal { get; init; }
        public HealMotesNode HealMotes { get; init; }
        public HealingCircleNode HealingCircle { get; init; }
        public MassHealNode MassHeal { get; init; }
        public PacketDurationNode PacketDuration { get; init; }
        public PaletteNode Palette { get; init; }
        public PaperBurstNode PaperBurst { get; init; }
        public PetNode Pet { get; init; }
        public PhantomNode Phantom { get; init; }
        public PlagueNode Plague { get; init; }
        public PoisonNode Poison { get; init; }
        public RepulseNode Repulse { get; init; }
        public RevealNode Reveal { get; init; }
        public ReviveNode Revive { get; init; }
        public ShieldNode Shield { get; init; }
        public ShinsuNode Shinsu { get; init; }
        public SpiritEdgeNode SpiritEdge { get; init; }
        public SummonStarsNode SummonStars { get; init; }
        public TrapNode Trap { get; init; }
        public VeilNode Veil { get; init; }

        public sealed class BurningWardNode
        {
            public int ArmourWidth { get; init; }
            public int SoulShieldWidth { get; init; }
            public int ArmourHeight { get; init; }
            public int SoulShieldHeight { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ArmourGlowColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SoulShieldGlowColour { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float Scale { get; init; }
            public float Scale2 { get; init; }
            public float Front { get; init; }
            public float Front2 { get; init; }
            public float Front3 { get; init; }
            public float Front4 { get; init; }
            public float Formed { get; init; }
            public float FormedDivisor { get; init; }
            public float Settling { get; init; }
            public float SettlingDivisor { get; init; }
            public float DissipationStart { get; init; }
            public float DissipationDuration { get; init; }
            public float SpriteWidth { get; init; }
            public float SpriteHeight { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ArmourSpriteColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SoulShieldSpriteColour { get; init; }
            public float SpriteAlpha { get; init; }
            public SpellParticleLayer SpriteLayer { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ArmourSpriteColour2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SoulShieldSpriteColour2 { get; init; }
            public float SpriteAlpha2 { get; init; }
            public float SpriteAlpha3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ArmourSpriteColour3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SoulShieldSpriteColour3 { get; init; }
            public float SpriteAlpha4 { get; init; }
            public float SpriteAlpha5 { get; init; }
            public SpellParticleLayer SpriteLayer2 { get; init; }
            public float SparkDivisor { get; init; }
            public int Source { get; init; }
            public int Source2 { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int UserFlameWidth { get; init; }
            public int UserFlameHeight { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public bool UserFlameLight { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int BrushKnotStride { get; init; }
            public int StrokeSeedStride { get; init; }
            public int KnotSeedStride { get; init; }
            public float Ignition { get; init; }
            public float Ignition2 { get; init; }
            public float Ignition3 { get; init; }
            public float Ignition4 { get; init; }
            public float Age { get; init; }
            public int PressDivisor { get; init; }
            public int Size { get; init; }
            public int ArmourSize { get; init; }
            public int SoulShieldSize { get; init; }
            public int Lift { get; init; }
            public int ArmourLift { get; init; }
            public int SoulShieldLift { get; init; }
            public float Lift2 { get; init; }
            public int Rise { get; init; }
            public int ArmourRise { get; init; }
            public int SoulShieldRise { get; init; }
            public float Tilt { get; init; }
            public float Alpha { get; init; }
            public float Alpha2 { get; init; }
            public float ArmourAlpha { get; init; }
            public float SoulShieldAlpha { get; init; }
            public float Alpha3 { get; init; }
            public float Alpha4 { get; init; }
            public float FlameSequenceOffset { get; init; }
            public float FlameSequenceOffset2 { get; init; }
            public SpellParticleShape FlameSequenceShape { get; init; }
            public float Offset { get; init; }
            public float Offset2 { get; init; }
            public float FlameSequenceAlpha { get; init; }
            public SpellParticleLayer FlameSequenceLayer { get; init; }
            public float UserFlameOffset { get; init; }
            public float UserFlameOffset2 { get; init; }
            public float UserFlameWidth2 { get; init; }
            public float UserFlameHeight2 { get; init; }
            public SpellParticleShape UserFlameShape2 { get; init; }
            public float Offset3 { get; init; }
            public SpellParticleLayer UserFlameLayer2 { get; init; }
            public float FlameTongueOffset { get; init; }
            public float FlameTongueWidth { get; init; }
            public SpellParticleShape FlameTongueShape { get; init; }
            public float Offset4 { get; init; }
            public float FlameTongueAlpha { get; init; }
            public SpellParticleLayer FlameTongueLayer { get; init; }
            public int X2 { get; init; }
            public float Y3 { get; init; }
            public float FlameSequenceWidth { get; init; }
            public float FlameSequenceHeight { get; init; }
            public SpellParticleShape FlameSequenceShape2 { get; init; }
            public float FlameSequencePhase { get; init; }
            public float Offset5 { get; init; }
            public float FlameSequenceAlpha2 { get; init; }
            public SpellParticleLayer FlameSequenceLayer2 { get; init; }
            public float SmokeReleaseStart { get; init; }
            public float SmokeReleaseDuration { get; init; }
            public float SmokeAge { get; init; }
            public float SmokeAge2 { get; init; }
            public int Sideways { get; init; }
            public int Sideways2 { get; init; }
            public int Sideways3 { get; init; }
            public int SmokeBaseOffsetY { get; init; }
            public int SmokeBaseRise { get; init; }
            public int SmokePressureRise { get; init; }
            public float SmokeBaseOpacity { get; init; }
            public float SmokeReleaseOpacity { get; init; }
            public float SmokeAgeFade { get; init; }
            public float SmokeEarlyOpacity { get; init; }
            public float SmokeSettlingOpacity { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ArmourSmokeSequenceColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SoulShieldSmokeSequenceColour { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public int KnotGlowMaxWidth { get; init; }
            public float ArmourKnotGlowWidthScale { get; init; }
            public float SoulShieldKnotGlowWidthScale { get; init; }
            public float WispHeight2 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            public float WispAlpha2 { get; init; }
            public SpellParticleLayer WispLayer2 { get; init; }
            public int UserEmberOffset { get; init; }
            public int UserEmberWidth { get; init; }
            public int UserEmberHeight { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public float UserEmberAlpha { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public float FlashWidth2 { get; init; }
            public float FlashHeight2 { get; init; }
            public SpellParticleShape FlashShape2 { get; init; }
            public float FlashAlpha2 { get; init; }
            public SpellParticleLayer FlashLayer2 { get; init; }
        }

        public sealed class CleanseNode
        {
            public int Head { get; init; }
            public int Head2 { get; init; }
            public int Head3 { get; init; }
            public int Head4 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public bool OrbLight { get; init; }
            public int OrbWidth3 { get; init; }
            public int OrbHeight3 { get; init; }
            public SpellParticleShape OrbShape3 { get; init; }
            public float OrbAlpha3 { get; init; }
            public SpellParticleLayer OrbLayer2 { get; init; }
            public int ParticleCount { get; init; }
            public float Age { get; init; }
            public float Offset { get; init; }
            public int Offset2 { get; init; }
            public int Offset3 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float X { get; init; }
            public int PrepareY { get; init; }
            public int ReleaseY { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public float ThreadAlpha2 { get; init; }
            public int SparksOffset { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
            public int WispCount { get; init; }
            public int TurnDivisor { get; init; }
            public float Turn { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float Top { get; init; }
            public int Top2 { get; init; }
            public int X2 { get; init; }
            public int Y3 { get; init; }
            public int Y4 { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha3 { get; init; }
            public float ThreadStartWidth3 { get; init; }
            public float ThreadEndWidth3 { get; init; }
            public float ThreadAlpha4 { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public float WispRotation { get; init; }
        }

        public sealed class CloudNode
        {
            public float Bloom { get; init; }
            public float Bloom2 { get; init; }
            public float Bloom3 { get; init; }
            public float Drift { get; init; }
            public float Drift2 { get; init; }
            public int Drift3 { get; init; }
            public float Drift4 { get; init; }
            public float Drift5 { get; init; }
            public int Drift6 { get; init; }
            public float Coverage { get; init; }
            public float Coverage2 { get; init; }
            public int MistFilamentWidth { get; init; }
            public int MistFilamentHeight { get; init; }
            public SpellParticleShape MistFilamentShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MistFilamentColour { get; init; }
            public float MistFilamentAlpha { get; init; }
            public SpellParticleLayer MistFilamentLayer { get; init; }
            public float X { get; init; }
            public float MistFilamentRotation { get; init; }
            public bool MistFilamentLight { get; init; }
            public int SmokeSequenceCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Turn { get; init; }
            public float Turn2 { get; init; }
            public float Turn3 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float Root { get; init; }
            public float Root2 { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public int Point3 { get; init; }
            public float Opacity { get; init; }
            public float Opacity2 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour3 { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public SpellParticleLayer SmokeSequenceLayer2 { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public int MistFilamentWidth2 { get; init; }
            public int MistFilamentWidth3 { get; init; }
            public int MistFilamentHeight2 { get; init; }
            public int MistFilamentHeight3 { get; init; }
            public SpellParticleShape MistFilamentShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MistFilamentColour2 { get; init; }
            public float MistFilamentAlpha2 { get; init; }
            public SpellParticleLayer MistFilamentLayer2 { get; init; }
            public float MistFilamentRotation2 { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public float SparksProgress { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SparksColour { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class CurseRuneNode
        {
            public int Width { get; init; }
            public int Height { get; init; }
            public int Centre { get; init; }
            public float Scale { get; init; }
            public float Scale2 { get; init; }
            public float Surge { get; init; }
            public float SurgeDivisor { get; init; }
            public float Surge2 { get; init; }
            public float SurgeDivisor2 { get; init; }
            public float Breaking { get; init; }
            public float BreakingDivisor { get; init; }
            public float CurseGlyphWidth { get; init; }
            public float CurseGlyphHeight { get; init; }
            public SpellParticleShape CurseGlyphShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CurseGlyphColour { get; init; }
            public float CurseGlyphAlpha { get; init; }
            public float CurseGlyphAlpha2 { get; init; }
            public SpellParticleLayer CurseGlyphLayer { get; init; }
            public SpellParticleShape CurseGlyphShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CurseGlyphColour2 { get; init; }
            public float CurseGlyphAlpha3 { get; init; }
            public float CurseGlyphAlpha4 { get; init; }
            public SpellParticleLayer CurseGlyphLayer2 { get; init; }
            public bool CurseGlyphLight { get; init; }
            public SpellParticleShape CurseGlyphShape3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int CurseGlyphColour3 { get; init; }
            public float CurseGlyphAlpha5 { get; init; }
            public float CurseGlyphAlpha6 { get; init; }
            public SpellParticleLayer CurseGlyphLayer3 { get; init; }
            public SpellParticleShape BrushShape { get; init; }
            public int BrushKnotStride { get; init; }
            public float Flicker { get; init; }
            public float Flicker2 { get; init; }
            public int Flicker3 { get; init; }
            public int PressureDivisor { get; init; }
            public float Flare { get; init; }
            public float Flare2 { get; init; }
            public int WispWidth { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public int FlashWidth { get; init; }
            public int FlashWidth2 { get; init; }
            public int FlashHeight { get; init; }
            public int FlashHeight2 { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlashColour { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public float X { get; init; }
            public float X2 { get; init; }
            public int Length { get; init; }
            public int Length2 { get; init; }
            public float Length3 { get; init; }
            public float Length4 { get; init; }
            public float PillarOffset { get; init; }
            public int PillarWidth { get; init; }
            public int PillarWidth2 { get; init; }
            public SpellParticleShape PillarShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int PillarColour { get; init; }
            public float PillarAlpha { get; init; }
            public SpellParticleLayer PillarLayer { get; init; }
            public float PillarRotation { get; init; }
            public float MistFilamentOffset { get; init; }
            public int MistFilamentHeight { get; init; }
            public int MistFilamentHeight2 { get; init; }
            public SpellParticleShape MistFilamentShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MistFilamentColour { get; init; }
            public float MistFilamentAlpha { get; init; }
            public SpellParticleLayer MistFilamentLayer { get; init; }
            public float MistFilamentOffset2 { get; init; }
            public float MistFilamentWidth { get; init; }
            public int MistFilamentHeight3 { get; init; }
            public int MistFilamentHeight4 { get; init; }
            public SpellParticleShape MistFilamentShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MistFilamentColour2 { get; init; }
            public float MistFilamentAlpha2 { get; init; }
            public SpellParticleLayer MistFilamentLayer2 { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Age3 { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY2 { get; init; }
            public int Remnant { get; init; }
            public float Remnant2 { get; init; }
            public float Remnant3 { get; init; }
            public float RemnantDivisor { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceWidth3 { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public int SmokeSequenceHeight3 { get; init; }
            public SpellParticleShape SmokeSequenceShape2 { get; init; }
            public float SmokeSequencePhase2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour3 { get; init; }
            public SpellParticleLayer SmokeSequenceLayer2 { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public int MistFilamentWidth2 { get; init; }
            public int MistFilamentWidth3 { get; init; }
            public int MistFilamentHeight5 { get; init; }
            public int MistFilamentHeight6 { get; init; }
            public SpellParticleShape MistFilamentShape3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MistFilamentColour3 { get; init; }
            public float MistFilamentAlpha3 { get; init; }
            public SpellParticleLayer MistFilamentLayer3 { get; init; }
            public int MistFilamentRotation { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SparkColour { get; init; }
            public float SparkAlpha { get; init; }
            public SpellParticleLayer SparkLayer { get; init; }
            public int X3 { get; init; }
            public int X4 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public int IonCoronaWidth { get; init; }
            public int IonCoronaHeight { get; init; }
            public SpellParticleShape IonCoronaShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int IonCoronaColour { get; init; }
            public float IonCoronaAlpha { get; init; }
            public SpellParticleLayer IonCoronaLayer { get; init; }
            public float IonCoronaRotation { get; init; }
            public int WispWidth3 { get; init; }
            public int WispHeight3 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            public float WispAlpha2 { get; init; }
            public SpellParticleLayer WispLayer2 { get; init; }
            public bool WispLight { get; init; }
            public int FlashWidth3 { get; init; }
            public int FlashHeight3 { get; init; }
            public SpellParticleShape FlashShape2 { get; init; }
            public float FlashAlpha2 { get; init; }
            public SpellParticleLayer FlashLayer2 { get; init; }
        }

        public sealed class DrawFrameNode
        {
            public float HealingCircleDiameter { get; init; }
            public float SoulShieldEnvelopeFadeStart { get; init; }
            public float SoulShieldEnvelopeTail { get; init; }
            public float BlessedArmourEnvelopeFadeStart { get; init; }
            public float BlessedArmourEnvelopeTail { get; init; }
            public float CurseEnvelopeFadeStart { get; init; }
            public float CurseEnvelopeTail { get; init; }
            public float SoulFireBallEnvelopeFadeStart { get; init; }
            public float SoulFireBallEnvelopeTail { get; init; }
            public float EnvelopeFadeStart { get; init; }
            public float EnvelopeTail { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
            public SpellParticleShape SoulShieldShape { get; init; }
            public SpellParticleShape BlessedArmourShape { get; init; }
            public float PoisonCloudTime { get; init; }
        }

        public sealed class DrawPoisonFieldNode
        {
            public float Time { get; init; }
        }

        public sealed class DrawProjectileNode
        {
            public int DepthY { get; init; }
            public float Angle { get; init; }
            public float Angle2 { get; init; }
            public int MistFilamentOffset { get; init; }
            public int MistFilamentWidth { get; init; }
            public int MistFilamentHeight { get; init; }
            public SpellParticleShape MistFilamentShape { get; init; }
            public float MistFilamentAlpha { get; init; }
            public SpellParticleLayer MistFilamentLayer { get; init; }
            public bool MistFilamentLight { get; init; }
            public int TalismanWidth { get; init; }
            public int TalismanHeight { get; init; }
            public SpellParticleShape TalismanShape { get; init; }
            public float TalismanAlpha { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int MistFilamentCount { get; init; }
            public float Drift { get; init; }
            public float Drift2 { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public int Point3 { get; init; }
            public int MistFilamentWidth2 { get; init; }
            public int MistFilamentWidth3 { get; init; }
            public int MistFilamentHeight2 { get; init; }
            public int MistFilamentHeight3 { get; init; }
            public SpellParticleShape MistFilamentShape2 { get; init; }
            public float MistFilamentAlpha2 { get; init; }
            public float MistFilamentAlpha3 { get; init; }
            public float MistFilamentRotation { get; init; }
            public int SparkOffset { get; init; }
            public int SparkOffset2 { get; init; }
            public int SparkWidth { get; init; }
            public int SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public float SparkAlpha2 { get; init; }
            public SpellParticleLayer SparkLayer { get; init; }
            public int Life { get; init; }
            public int Point4 { get; init; }
            public int WispVelocity { get; init; }
            public int WispVelocity2 { get; init; }
            public int WispSize { get; init; }
            public float Opacity { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAspect { get; init; }
            public float WispGrowth { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public bool WispCastsLight { get; init; }
            public int WispDepthY { get; init; }
        }

        public sealed class EnhanceNode
        {
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public float Open { get; init; }
            public float Open2 { get; init; }
            public int Open3 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public bool OrbLight2 { get; init; }
            public int OrbOffset { get; init; }
            public int OrbWidth3 { get; init; }
            public int OrbHeight3 { get; init; }
            public SpellParticleShape OrbShape3 { get; init; }
            public float OrbAlpha3 { get; init; }
            public SpellParticleLayer OrbLayer2 { get; init; }
            public int OrbOffset2 { get; init; }
            public int OrbWidth4 { get; init; }
            public int OrbHeight4 { get; init; }
            public SpellParticleShape OrbShape4 { get; init; }
            public float OrbAlpha4 { get; init; }
            public SpellParticleLayer OrbLayer3 { get; init; }
            public int WispCount { get; init; }
            public float Turn { get; init; }
            public int TurnDivisor { get; init; }
            public int Reach { get; init; }
            public int Reach2 { get; init; }
            public float EllipseY { get; init; }
            public float Point { get; init; }
            public float Point2 { get; init; }
            public int WispWidth { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            public float WispAlpha { get; init; }
            public float ThreadStart { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public int FireCurlWidth { get; init; }
            public int FireCurlHeight { get; init; }
            public SpellParticleShape FireCurlShape { get; init; }
            public float FireCurlAlpha { get; init; }
            public SpellParticleLayer FireCurlLayer { get; init; }
            public int ThreadStart2 { get; init; }
            public int ThreadEnd { get; init; }
            public float Value { get; init; }
            public float ValueDivisor { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha2 { get; init; }
            public SpellParticleLayer ThreadLayer { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class HealNode
        {
            public int Body { get; init; }
            public float PreparePhase { get; init; }
            public float Phase { get; init; }
            public int Head { get; init; }
            public int Head2 { get; init; }
            public float EllipseAngle { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            public float WispAlpha2 { get; init; }
            public SpellParticleLayer WispLayer2 { get; init; }
            public bool WispLight { get; init; }
            public int Radius { get; init; }
            public int Count { get; init; }
            public float TrailAngle { get; init; }
            public float TrailAngle2 { get; init; }
            public int WispWidth3 { get; init; }
            public int WispHeight3 { get; init; }
            public SpellParticleShape WispShape3 { get; init; }
            public float WispAlpha3 { get; init; }
            public SpellParticleLayer WispLayer3 { get; init; }
            public bool WispLight2 { get; init; }
            public int WispWidth4 { get; init; }
            public int WispHeight4 { get; init; }
            public SpellParticleShape WispShape4 { get; init; }
            public float WispAlpha4 { get; init; }
            public SpellParticleLayer WispLayer4 { get; init; }
            public bool WispLight3 { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public float FlashRotation { get; init; }
            public int SparkCount { get; init; }
            public float AgeDivisor { get; init; }
            public float Turn { get; init; }
            public int Point { get; init; }
            public float Point2 { get; init; }
            public int X { get; init; }
            public int X2 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float EllipseAngle2 { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseX3 { get; init; }
            public int EllipseY2 { get; init; }
            public int EllipseY3 { get; init; }
            public float X3 { get; init; }
            public int X4 { get; init; }
            public int Y3 { get; init; }
            public SpellParticleLayer PrepareDepth { get; init; }
            public float Depth { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public int SparkHeight2 { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public float SparkAlpha2 { get; init; }
            public int FlashCount { get; init; }
            public float Age { get; init; }
            public float Turn2 { get; init; }
            public float Turn3 { get; init; }
            public int EllipseX4 { get; init; }
            public int EllipseX5 { get; init; }
            public int EllipseY4 { get; init; }
            public int EllipseY5 { get; init; }
            public int Point3 { get; init; }
            public int Point4 { get; init; }
            public int FlashWidth2 { get; init; }
            public int FlashWidth3 { get; init; }
            public int FlashHeight2 { get; init; }
            public int FlashHeight3 { get; init; }
            public SpellParticleShape FlashShape2 { get; init; }
            public SpellParticleShape FlashShape3 { get; init; }
            public float FlashAlpha2 { get; init; }
            public SpellParticleLayer FlashLayer2 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
        }

        public sealed class HealMotesNode
        {
            public float Age { get; init; }
            public float Turn { get; init; }
            public float Turn2 { get; init; }
            public float Turn3 { get; init; }
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public float Spread3 { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public float Size { get; init; }
            public float Size2 { get; init; }
            public float Shimmer { get; init; }
            public float Shimmer2 { get; init; }
            public int Shimmer3 { get; init; }
            public int Shimmer4 { get; init; }
            public float FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public SpellParticleShape FlashShape2 { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public float OrbWidth { get; init; }
            public float OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
        }

        public sealed class HealingCircleNode
        {
            public int Opening { get; init; }
            public int Width { get; init; }
            public float Width2 { get; init; }
            public float Width3 { get; init; }
            public float Rotation { get; init; }
            public float Pulse { get; init; }
            public float Pulse2 { get; init; }
            public float PulseDivisor { get; init; }
            public float WispWidth { get; init; }
            public float WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public bool WispLight { get; init; }
            public float TrigramsWidth { get; init; }
            public SpellParticleShape TrigramsShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TrigramsColour { get; init; }
            public float TrigramsAlpha { get; init; }
            public SpellParticleShape TrigramsShape2 { get; init; }
            public float TrigramsAlpha2 { get; init; }
            public bool TrigramsLight { get; init; }
            public float TrigramsWidth2 { get; init; }
            public SpellParticleShape TrigramsShape3 { get; init; }
            public float TrigramsAlpha3 { get; init; }
            public float YinYangWidth { get; init; }
            public SpellParticleShape YinYangShape { get; init; }
            public float YinYangAlpha { get; init; }
            public bool YinYangLight { get; init; }
            public int SparkCount { get; init; }
            public int AngleDivisor { get; init; }
            public float Angle { get; init; }
            public float Age { get; init; }
            public float EllipseX { get; init; }
            public float EllipseY { get; init; }
            public float Point { get; init; }
            public float Point2 { get; init; }
            public int Point3 { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public float SparkAlpha2 { get; init; }
        }

        public sealed class MassHealNode
        {
            public float Bloom { get; init; }
            public float Bloom2 { get; init; }
            public float Bloom3 { get; init; }
            public int PrepareRadius { get; init; }
            public int ReleaseRadius { get; init; }
            public int Centre { get; init; }
            public int PrepareCentre { get; init; }
            public int ReleaseCentre { get; init; }
            public float OrbWidth { get; init; }
            public float OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public float WispWidth { get; init; }
            public float WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public float Value { get; init; }
            public float ValueDivisor { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public bool WispLight { get; init; }
            public float Radius { get; init; }
            public int PrepareCount { get; init; }
            public int ReleaseCount { get; init; }
            public int WispCount { get; init; }
            public float Angle { get; init; }
            public int Angle2 { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            public float WispAlpha2 { get; init; }
            public bool WispLight2 { get; init; }
            public float Flare { get; init; }
            public float Flare2 { get; init; }
            public int Flare3 { get; init; }
            public float Flare4 { get; init; }
            public int Flare5 { get; init; }
            public int WispWidth3 { get; init; }
            public int WispHeight3 { get; init; }
            public SpellParticleShape WispShape3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha3 { get; init; }
            public float WispRotation { get; init; }
            public bool WispLight3 { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int SparkCount { get; init; }
            public float Trail { get; init; }
            public float Spread { get; init; }
            public int Spread2 { get; init; }
            public float Spread3 { get; init; }
            public int Point { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public float SparkAlpha2 { get; init; }
            public float SparkRotation { get; init; }
        }

        public sealed class PacketDurationNode
        {
            public int SoulFireBallDurationMs { get; init; }
            public int HealingDurationMs { get; init; }
            public int SoulShieldDurationMs { get; init; }
            public int HealingCircleDurationMs { get; init; }
            public int CurseDurationMs { get; init; }
            public int EnergyShieldAuraDurationMs { get; init; }
            public int EnergyShieldImpactDurationMs { get; init; }
            public int PetEnhancerAuraDurationMs { get; init; }
            public int PetEnhancerImpactDurationMs { get; init; }
            public int RevelationDurationMs { get; init; }
            public int DefaultDurationMs { get; init; }
        }

        public sealed class PaletteNode
        {
            [JsonConverter(typeof(VfxRgbConverter))]
            public int White { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Jade { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Teal { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Mint { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int InkBlue { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Blue { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Ice { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Violet { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Ochre { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Amber { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Gold { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Ivory { get; init; }
            [JsonConverter(typeof(VfxCurveConverter))]
            public SpellParticleColourCurve PaperFlame { get; init; }
            [JsonConverter(typeof(VfxCurveConverter))]
            public SpellParticleColourCurve PaperCore { get; init; }
            [JsonConverter(typeof(VfxCurveConverter))]
            public SpellParticleColourCurve PaperHeat { get; init; }
            [JsonConverter(typeof(VfxCurveConverter))]
            public SpellParticleColourCurve PaperEmber { get; init; }
            [JsonConverter(typeof(VfxCurveConverter))]
            public SpellParticleColourCurve SoulFlame { get; init; }
            [JsonConverter(typeof(VfxCurveConverter))]
            public SpellParticleColourCurve ArmourFlame { get; init; }
        }

        public sealed class PaperBurstNode
        {
            public int Centre { get; init; }
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public float Spread3 { get; init; }
            public float Heat { get; init; }
            public float HeatDivisor { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public bool WispLight { get; init; }
            public int FireSequenceCount { get; init; }
            public float Angle { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public float Flare { get; init; }
            public float Flare2 { get; init; }
            public int Flare3 { get; init; }
            public float Flare4 { get; init; }
            public float Flare5 { get; init; }
            public float FlareDivisor { get; init; }
            public int FireSequenceWidth { get; init; }
            public int FireSequenceWidth2 { get; init; }
            public int FireSequenceHeight { get; init; }
            public int FireSequenceHeight2 { get; init; }
            public SpellParticleShape FireSequenceShape { get; init; }
            public float FireSequencePhase { get; init; }
            public float FireSequencePhase2 { get; init; }
            public float FireSequencePhase3 { get; init; }
            public float FireSequenceAlpha { get; init; }
            public float FireSequenceRotation { get; init; }
            public float Amount { get; init; }
            public int FireSequenceWidth3 { get; init; }
            public int FireSequenceHeight3 { get; init; }
            public SpellParticleShape FireSequenceShape2 { get; init; }
            public float FireSequencePhase4 { get; init; }
            public float FireSequencePhase5 { get; init; }
            public float FireSequencePhase6 { get; init; }
            public float FireSequenceAlpha2 { get; init; }
            public SpellParticleLayer FireSequenceLayer { get; init; }
            public float FireSequenceRotation2 { get; init; }
            public float Amount2 { get; init; }
            public int UserFlameWidth { get; init; }
            public int UserFlameHeight { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public float UserFlameRotation { get; init; }
            public int X2 { get; init; }
            public int X3 { get; init; }
            public int Y2 { get; init; }
            public int Y3 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            public float SmokeSequencePhase2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public float Value { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int FireSequenceWidth4 { get; init; }
            public int FireSequenceHeight4 { get; init; }
            public SpellParticleShape FireSequenceShape3 { get; init; }
            public float FireSequencePhase7 { get; init; }
            public float FireSequencePhase8 { get; init; }
            public SpellParticleLayer FireSequenceLayer2 { get; init; }
            public bool FireSequenceLight { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int UserEmberCount { get; init; }
            public float Birth { get; init; }
            public float Birth2 { get; init; }
            public float Age3 { get; init; }
            public float Age4 { get; init; }
            public float Angle2 { get; init; }
            public float Reach { get; init; }
            public float Reach2 { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY2 { get; init; }
            public float Position { get; init; }
            public float Position2 { get; init; }
            public float Position3 { get; init; }
            public int Position4 { get; init; }
            public int Position5 { get; init; }
            public float Size { get; init; }
            public float Size2 { get; init; }
            public float Alpha { get; init; }
            public float Alpha2 { get; init; }
            public int Alpha3 { get; init; }
            public float Alpha4 { get; init; }
            public float AlphaDivisor { get; init; }
            public float UserEmberHeight { get; init; }
            public float UserEmberHeight2 { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public float FlashWidth2 { get; init; }
            public float FlashHeight2 { get; init; }
            public SpellParticleShape FlashShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlashColour { get; init; }
            public float FlashAlpha2 { get; init; }
            public SpellParticleLayer FlashLayer2 { get; init; }
            public float SparkWidth { get; init; }
            public float SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float Offset { get; init; }
            public float SparkAlpha { get; init; }
            public SpellParticleLayer SparkLayer { get; init; }
        }

        public sealed class PetNode
        {
            public int AuraCentre { get; init; }
            public int ImpactCentre { get; init; }
            public float Open { get; init; }
            public float Open2 { get; init; }
            public float Open3 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public int FlameSequenceWidth { get; init; }
            public int FlameSequenceHeight { get; init; }
            public SpellParticleShape FlameSequenceShape { get; init; }
            public float FlameSequencePhase { get; init; }
            public float FlameSequencePhase2 { get; init; }
            public float FlameSequenceAlpha { get; init; }
            public bool FlameSequenceLight { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
            public float Flicker { get; init; }
            public float Flicker2 { get; init; }
            public float Flicker3 { get; init; }
            public int ArmourGlyphWidth { get; init; }
            public int ArmourGlyphHeight { get; init; }
            public SpellParticleShape ArmourGlyphShape { get; init; }
            public float ArmourGlyphAlpha { get; init; }
            public int ClawWidth { get; init; }
            public int ClawHeight { get; init; }
            public SpellParticleShape ClawShape { get; init; }
            public float ClawAlpha { get; init; }
            public SpellParticleLayer ClawLayer { get; init; }
            public float ClawRotation { get; init; }
            public float X { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int SparksCount2 { get; init; }
            public int SparksRx2 { get; init; }
            public int SparksRy2 { get; init; }
            public int SparksRise2 { get; init; }
        }

        public sealed class PhantomNode
        {
            public int Rise { get; init; }
            public int Centre { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int SpiritFigureWidth { get; init; }
            public int SpiritFigureHeight { get; init; }
            public SpellParticleShape SpiritFigureShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SpiritFigureColour { get; init; }
            public float SpiritFigureAlpha { get; init; }
            public int SpiritFigureOffset { get; init; }
            public int SpiritFigureWidth2 { get; init; }
            public int SpiritFigureHeight2 { get; init; }
            public SpellParticleShape SpiritFigureShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SpiritFigureColour2 { get; init; }
            public float SpiritFigureAlpha2 { get; init; }
            public bool SpiritFigureLight { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class PlagueNode
        {
            public float Open { get; init; }
            public float Open2 { get; init; }
            public float Open3 { get; init; }
            public int Centre { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public int FlameSequenceCount { get; init; }
            public float Turn { get; init; }
            public float Turn2 { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float Root { get; init; }
            public float Root2 { get; init; }
            public int X { get; init; }
            public float Y { get; init; }
            public float Y2 { get; init; }
            public float Lean { get; init; }
            public int FlameSequenceWidth { get; init; }
            public int FlameSequenceWidth2 { get; init; }
            public SpellParticleShape FlameSequenceShape { get; init; }
            public float FlameSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlameSequenceColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlameSequenceColour2 { get; init; }
            public float FlameSequenceAlpha { get; init; }
            public int X2 { get; init; }
            public float Y3 { get; init; }
            public int FireCurlWidth { get; init; }
            public int FireCurlHeight { get; init; }
            public SpellParticleShape FireCurlShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FireCurlColour { get; init; }
            public float FireCurlAlpha { get; init; }
            public float FireCurlAlpha2 { get; init; }
            public float FlameTongueOffset { get; init; }
            public int FlameTongueWidth { get; init; }
            public float FlameTongueHeight { get; init; }
            public SpellParticleShape FlameTongueShape { get; init; }
            public float FlameTongueAlpha { get; init; }
            public float FlameTongueAlpha2 { get; init; }
            public SpellParticleLayer FlameTongueLayer { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class PoisonNode
        {
            public int Body { get; init; }
            public int Body2 { get; init; }
            public int ReleaseBody { get; init; }
            public float Bloom { get; init; }
            public float Bloom2 { get; init; }
            public int Bloom3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int PrepareBodyTintColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ReleaseBodyTintColour { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int WispCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Angle { get; init; }
            public int Angle2 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float Point { get; init; }
            public float Point2 { get; init; }
            public int Point3 { get; init; }
            public int WispWidth { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public float WispAlpha2 { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public float SparkAlpha2 { get; init; }
            public SpellParticleLayer SparkLayer { get; init; }
            public int OrbitX { get; init; }
            public int OrbitY { get; init; }
            public int OrbitAngle { get; init; }
            public float OrbitSweep { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbitColour { get; init; }
            public float OrbitWidth { get; init; }
            public float OrbitAlpha { get; init; }
        }

        public sealed class RepulseNode
        {
            public float Open { get; init; }
            public int Body { get; init; }
            public int OrbWidth { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public int WardBackCount { get; init; }
            public float Wave { get; init; }
            public float Wave2 { get; init; }
            public int Width { get; init; }
            public int Width2 { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public SpellParticleShape WardBackShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WardBackColour { get; init; }
            public float WardBackAlpha { get; init; }
            public SpellParticleLayer WardBackLayer { get; init; }
            public SpellParticleShape WardFrontShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WardFrontColour { get; init; }
            public float WardFrontAlpha { get; init; }
            public float WardFrontAlpha2 { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRx2 { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class RevealNode
        {
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public int Centre3 { get; init; }
            public float Growth { get; init; }
            public float Growth2 { get; init; }
            public int Growth3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ReleaseTintColour { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public int Y { get; init; }
            public int SigilEyeWidth { get; init; }
            public int SigilEyeHeight { get; init; }
            public SpellParticleShape SigilEyeShape { get; init; }
            public float SigilEyeAlpha { get; init; }
            public SpellParticleLayer SigilEyeLayer { get; init; }
            public float SigilEyeRotation { get; init; }
            public int X { get; init; }
            public int X2 { get; init; }
            public int Y2 { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int SparkCount { get; init; }
            public int Angle { get; init; }
            public float Angle2 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float EllipseAngle { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY2 { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ReleaseThreadColour { get; init; }
            public float ThreadAlpha { get; init; }
            public int SparkWidth { get; init; }
            public int SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
        }

        public sealed class ReviveNode
        {
            public float Lift { get; init; }
            public float Lift2 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public int HaloWidth { get; init; }
            public int HaloHeight { get; init; }
            public SpellParticleShape HaloShape { get; init; }
            public float HaloAlpha { get; init; }
            public SpellParticleLayer HaloLayer { get; init; }
            public int PillarCount { get; init; }
            public int SampleStride { get; init; }
            public int X { get; init; }
            public int Centre { get; init; }
            public int X2 { get; init; }
            public int PillarWidth { get; init; }
            public int PillarHeight { get; init; }
            public SpellParticleShape PillarShape { get; init; }
            public float PillarAlpha { get; init; }
            public SpellParticleLayer PillarLayer { get; init; }
            public float PillarRotation { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public float WispRotation { get; init; }
            public bool WispLight { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public float FlashRotation { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class ShieldNode
        {
            public int Centre { get; init; }
            public int Spread { get; init; }
            public float Spread2 { get; init; }
            public int SideCount { get; init; }
            public int SampleStride { get; init; }
            public int Segments { get; init; }
            public float Wave { get; init; }
            public float Wave2 { get; init; }
            public int Wave3 { get; init; }
            public float Wave4 { get; init; }
            public float Pulse { get; init; }
            public float Pulse2 { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha { get; init; }
            public SpellParticleLayer ThreadLayer { get; init; }
            public float ThreadStartWidth3 { get; init; }
            public float ThreadStartWidth4 { get; init; }
            public float ThreadEndWidth3 { get; init; }
            public float ThreadAlpha2 { get; init; }
            public SpellParticleLayer ThreadLayer2 { get; init; }
            public float Midpoint { get; init; }
            public int WispWidth { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public float WispAlpha2 { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int UserLightningOffset { get; init; }
            public int UserLightningOffset2 { get; init; }
            public int UserLightningWidth { get; init; }
            public int UserLightningWidth2 { get; init; }
            public int UserLightningHeight { get; init; }
            public int UserLightningHeight2 { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public SpellParticleLayer UserLightningLayer { get; init; }
            public float UserLightningRotation { get; init; }
            public int MistFilamentOffset { get; init; }
            public int MistFilamentWidth { get; init; }
            public int MistFilamentWidth2 { get; init; }
            public int MistFilamentHeight { get; init; }
            public int MistFilamentHeight2 { get; init; }
            public SpellParticleShape MistFilamentShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MistFilamentColour { get; init; }
            public float MistFilamentAlpha { get; init; }
            public SpellParticleLayer MistFilamentLayer { get; init; }
            public int FlashCount { get; init; }
            public int Age { get; init; }
            public float T { get; init; }
            public float T2 { get; init; }
            public int Drift { get; init; }
            public int Drift2 { get; init; }
            public int X { get; init; }
            public int X2 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float Alpha { get; init; }
            public int FlashWidth { get; init; }
            public int FlashWidth2 { get; init; }
            public int FlashHeight { get; init; }
            public int FlashHeight2 { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public SpellParticleShape FlashShape2 { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public float FlashRotation { get; init; }
            public int WispWidth3 { get; init; }
            public int WispWidth4 { get; init; }
            public int WispHeight3 { get; init; }
            public int WispHeight4 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            public float WispAlpha3 { get; init; }
            public SpellParticleLayer WispLayer2 { get; init; }
            public float WispRotation { get; init; }
        }

        public sealed class ShinsuNode
        {
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public float Open { get; init; }
            public float Open2 { get; init; }
            public int Open3 { get; init; }
            public float Fade { get; init; }
            public float FadeDivisor { get; init; }
            public int HaloWidth { get; init; }
            public int HaloHeight { get; init; }
            public SpellParticleShape HaloShape { get; init; }
            public float HaloAlpha { get; init; }
            public SpellParticleLayer HaloLayer { get; init; }
            public bool HaloLight { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public bool WispLight { get; init; }
            public int TalismanCount { get; init; }
            public int TurnDivisor { get; init; }
            public int Turn { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public int TalismanWidth { get; init; }
            public int TalismanWidth2 { get; init; }
            public int TalismanHeight { get; init; }
            public int TalismanHeight2 { get; init; }
            public SpellParticleShape TalismanShape { get; init; }
            public float TalismanRotation { get; init; }
            public int FlameSequenceOffset { get; init; }
            public int FlameSequenceWidth { get; init; }
            public int FlameSequenceHeight { get; init; }
            public SpellParticleShape FlameSequenceShape { get; init; }
            public float FlameSequencePhase { get; init; }
            public float FlameSequencePhase2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlameSequenceColour { get; init; }
            public float FlameSequenceAlpha { get; init; }
            public float FlameSequenceRotation { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            public float WispAlpha2 { get; init; }
            public float Ignite { get; init; }
            public float Ignite2 { get; init; }
            public float Ignite3 { get; init; }
            public int Ignite4 { get; init; }
            public int UserFlameOffset { get; init; }
            public int UserFlameWidth { get; init; }
            public int UserFlameHeight { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public float UserFlameRotation { get; init; }
            public int FeatherCount { get; init; }
            public float EllipseAngle { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY2 { get; init; }
            public int FeatherWidth { get; init; }
            public int FeatherHeight { get; init; }
            public SpellParticleShape FeatherShape { get; init; }
            public float FeatherAlpha { get; init; }
            public float FeatherAlpha2 { get; init; }
            public float FeatherRotation { get; init; }
            public int SparkOffset { get; init; }
            public int SparkCount { get; init; }
            public int SparkRx { get; init; }
            public int SparkRy { get; init; }
            public int SparkRise { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public int FlashCount { get; init; }
            public float Age { get; init; }
            public float Turn2 { get; init; }
            public int Turn3 { get; init; }
            public int EllipseX3 { get; init; }
            public int EllipseX4 { get; init; }
            public int EllipseY3 { get; init; }
            public int EllipseY4 { get; init; }
            public int Point { get; init; }
            public int FlashWidth { get; init; }
            public int FlashWidth2 { get; init; }
            public int FlashHeight { get; init; }
            public int FlashHeight2 { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public float FlashAlpha2 { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
        }

        public sealed class SpiritEdgeNode
        {
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int ParticleCount { get; init; }
            public int Point { get; init; }
            public float Y { get; init; }
            public int Y2 { get; init; }
            public int ThreadEnd { get; init; }
            public int ThreadEnd2 { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class SummonStarsNode
        {
            [JsonConverter(typeof(VfxRgbConverter))]
            public int DuskColour { get; init; }
            public int Lift { get; init; }
            public float Opening { get; init; }
            public float Ascension { get; init; }
            public float AscensionDivisor { get; init; }
            public float Ascension2 { get; init; }
            public float Ascension3 { get; init; }
            public float AscensionDivisor2 { get; init; }
            public int HaloWidth { get; init; }
            public int HaloWidth2 { get; init; }
            public int HaloHeight { get; init; }
            public int HaloHeight2 { get; init; }
            public SpellParticleShape HaloShape { get; init; }
            public float HaloAlpha { get; init; }
            public float Value { get; init; }
            public float ValueDivisor { get; init; }
            public SpellParticleLayer HaloLayer { get; init; }
            public bool HaloLight { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int Crown { get; init; }
            public float Crown2 { get; init; }
            public int IonCoronaWidth { get; init; }
            public int IonCoronaHeight { get; init; }
            public SpellParticleShape IonCoronaShape { get; init; }
            public float IonCoronaAlpha { get; init; }
            public SpellParticleLayer IonCoronaLayer { get; init; }
            public float IonCoronaRotation { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public float FlashRotation { get; init; }
            public bool FlashLight { get; init; }
            public int WispCount { get; init; }
            public int WispCount2 { get; init; }
            public float T { get; init; }
            public float T2 { get; init; }
            public int Turn { get; init; }
            public float Turn2 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY { get; init; }
            public int Star { get; init; }
            public int Star2 { get; init; }
            public int WispWidth2 { get; init; }
            public int WispWidth3 { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            public float WispAlpha2 { get; init; }
            public float Flare { get; init; }
            public float Flare2 { get; init; }
            public float Flare3 { get; init; }
            public int Flare4 { get; init; }
            public int WispWidth4 { get; init; }
            public int WispHeight3 { get; init; }
            public SpellParticleShape WispShape3 { get; init; }
            public float WispAlpha3 { get; init; }
            public int FlashWidth2 { get; init; }
            public float FlashWidth3 { get; init; }
            public int FlashHeight2 { get; init; }
            public SpellParticleShape FlashShape2 { get; init; }
            public float FlashAlpha2 { get; init; }
            public int FlashWidth4 { get; init; }
            public int FlashHeight3 { get; init; }
            public SpellParticleShape FlashShape3 { get; init; }
            public float FlashAlpha3 { get; init; }
            public SpellParticleLayer FlashLayer2 { get; init; }
            public int FeatherOffset { get; init; }
            public int FeatherCount { get; init; }
            public int FeatherRx { get; init; }
            public int FeatherRy { get; init; }
            public int FeatherRise { get; init; }
            public SpellParticleShape FeatherShape { get; init; }
            public SpellParticleShape FeatherShape2 { get; init; }
            public int FlashCount { get; init; }
            public float Age { get; init; }
            public float Turn3 { get; init; }
            public int Turn4 { get; init; }
            public int EllipseX3 { get; init; }
            public int EllipseX4 { get; init; }
            public int EllipseY2 { get; init; }
            public int EllipseY3 { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public int FlashWidth5 { get; init; }
            public int FlashWidth6 { get; init; }
            public int FlashHeight4 { get; init; }
            public int FlashHeight5 { get; init; }
            public SpellParticleShape FlashShape4 { get; init; }
            public SpellParticleShape FlashShape5 { get; init; }
            public float FlashAlpha4 { get; init; }
            public SpellParticleLayer FlashLayer3 { get; init; }
        }

        public sealed class TrapNode
        {
            public float Open { get; init; }
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public bool WispLight { get; init; }
            public int FlashCount { get; init; }
            public int Turn { get; init; }
            public int FlashCount2 { get; init; }
            public float TDivisor { get; init; }
            public int EllipseAngle { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha { get; init; }
            public int Angle { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public float Pulse { get; init; }
            public float Pulse2 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public int Head { get; init; }
            public int Head2 { get; init; }
            public float WispOffset { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            public float WispAlpha2 { get; init; }
            public float ThreadStartWidth3 { get; init; }
            public float ThreadEndWidth3 { get; init; }
            public float ThreadAlpha2 { get; init; }
            public SpellParticleLayer ThreadLayer { get; init; }
            public int FlashWidth2 { get; init; }
            public int FlashHeight2 { get; init; }
            public SpellParticleShape FlashShape2 { get; init; }
            public float FlashAlpha2 { get; init; }
            public SpellParticleLayer FlashLayer2 { get; init; }
            public float FlashRotation { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class VeilNode
        {
            public float Width { get; init; }
            public int Puffs { get; init; }
            public int Puffs2 { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Age3 { get; init; }
            public float Age4 { get; init; }
            public float Hit { get; init; }
            public float HitDivisor { get; init; }
            public float Opacity { get; init; }
            public float Opacity2 { get; init; }
            public float OpacityDivisor { get; init; }
            public float Turn { get; init; }
            public float Turn2 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float BasePoint { get; init; }
            public float BasePoint2 { get; init; }
            public int Lift { get; init; }
            public int Lift2 { get; init; }
            public int Lift3 { get; init; }
            public int Point { get; init; }
            public int X { get; init; }
            public int X2 { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public SpellParticleLayer Layer2 { get; init; }
            public SpellParticleLayer Layer3 { get; init; }
            public float Tilt { get; init; }
            public float Tilt2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TintColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TintColour2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TintColour3 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceWidth3 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public int SmokeSequenceHeight3 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            public float SmokeSequencePhase2 { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public int X3 { get; init; }
            public int Y { get; init; }
            public int SmokeSequenceWidth4 { get; init; }
            public int SmokeSequenceWidth5 { get; init; }
            public int SmokeSequenceHeight4 { get; init; }
            public int SmokeSequenceHeight5 { get; init; }
            public SpellParticleShape SmokeSequenceShape2 { get; init; }
            public float SmokeSequencePhase3 { get; init; }
            public float SmokeSequencePhase4 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha2 { get; init; }
            public int WispWidth { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public float WispAlpha2 { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public bool WispLight { get; init; }
            public float Ground { get; init; }
            public float GroundDivisor { get; init; }
            public float DustFade { get; init; }
            public float DustFade2 { get; init; }
            public float DustFadeDivisor { get; init; }
            public int WispCount { get; init; }
            public int WispCount2 { get; init; }
            public float Turn3 { get; init; }
            public float Turn4 { get; init; }
            public float Age5 { get; init; }
            public int EllipseX2 { get; init; }
            public float EllipseX3 { get; init; }
            public int EllipseY2 { get; init; }
            public float EllipseY3 { get; init; }
            public int WispWidth3 { get; init; }
            public int WispWidth4 { get; init; }
            public int WispHeight3 { get; init; }
            public int WispHeight4 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour3 { get; init; }
            public float WispAlpha3 { get; init; }
            public float WispAlpha4 { get; init; }
            public SpellParticleLayer WispLayer2 { get; init; }
            public float WispRotation { get; init; }
            public int SparkOffset { get; init; }
            public int SparkOffset2 { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SparkColour { get; init; }
            public float SparkAlpha { get; init; }
            public SpellParticleLayer SparkLayer { get; init; }
        }
    }

    internal sealed class WarriorNodes
    {
        public ActorMasksNode ActorMasks { get; init; }

        public sealed class ActorMasksNode
        {
            public float ReadyIntensity { get; init; }
            public float SwingPulseGain { get; init; }
            public float SwingPulseBase { get; init; }
            public float FuryIntensity { get; init; }
            public float RageIntensity { get; init; }
            public float ImmortalSkinIntensity { get; init; }
            public float ProtectionFieldIntensity { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color FuryCore { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color RageCore { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color ImmortalSkinCore { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color ProtectionFieldCore { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color FuryColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color RageColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color ImmortalSkinColour { get; init; }
            [JsonConverter(typeof(VfxColorConverter))]
            public Color ProtectionFieldColour { get; init; }
        }

        public AddBladeFireNode AddBladeFire { get; init; }
        public AvalancheNode Avalanche { get; init; }
        public BladeFlameNode BladeFlame { get; init; }
        public BurstNode Burst { get; init; }
        public CounterNode Counter { get; init; }
        public DashNode Dash { get; init; }
        public DrakeChargeNode DrakeCharge { get; init; }
        public DrakeContactNode DrakeContact { get; init; }
        public DrakeEdgeSweepNode DrakeEdgeSweep { get; init; }
        public DrawFrameNode DrawFrame { get; init; }
        public DrawWeaponEnchantNode DrawWeaponEnchant { get; init; }
        public EngulfNode Engulf { get; init; }
        public FuryNode Fury { get; init; }
        public GroundDirectionNode GroundDirection { get; init; }
        public PacketDurationNode PacketDuration { get; init; }
        public PaletteNode Palette { get; init; }
        public ProjectCrescentNode ProjectCrescent { get; init; }
        public RoarNode Roar { get; init; }
        public SkinNode Skin { get; init; }
        public SnareNode Snare { get; init; }
        public StrikeNode Strike { get; init; }
        public SurgeNode Surge { get; init; }
        public SweepRibbonNode SweepRibbon { get; init; }
        public ThrustWindNode ThrustWind { get; init; }
        public WeaponSweepNode WeaponSweep { get; init; }

        public sealed class AddBladeFireNode
        {
            public int UserEmberCount { get; init; }
            public float Age { get; init; }
            public float Along { get; init; }
            public float Along2 { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float Alpha { get; init; }
            public float Alpha2 { get; init; }
            public int UserEmberWidth { get; init; }
            public int UserEmberWidth2 { get; init; }
            public int UserEmberHeight { get; init; }
            public int UserEmberHeight2 { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public SpellParticleShape UserEmberShape2 { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public SpellParticleLayer UserEmberLayer2 { get; init; }
            public float UserEmberRotation { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
        }

        public sealed class AvalancheNode
        {
            public float Turn { get; init; }
            public int Hand { get; init; }
            public int Hand2 { get; init; }
            public int Hand3 { get; init; }
            public float Windup { get; init; }
            public float Windup2 { get; init; }
            public int WarriorTripleCutWidth { get; init; }
            public int WarriorTripleCutHeight { get; init; }
            public SpellParticleShape WarriorTripleCutShape { get; init; }
            public float WarriorTripleCutAlpha { get; init; }
            public bool WarriorTripleCutLight { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public float Release { get; init; }
            public float ReleaseDivisor { get; init; }
            public float Release2 { get; init; }
            public float ReleaseDivisor2 { get; init; }
            public int WispCount { get; init; }
            public float Direction { get; init; }
            public float Direction2 { get; init; }
            public int Ray { get; init; }
            public int Ray2 { get; init; }
            public int Ray3 { get; init; }
            public float Centre { get; init; }
            public int Centre2 { get; init; }
            public int Centre3 { get; init; }
            public float WispWidth { get; init; }
            public int WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public float WispRotation { get; init; }
            public float ThreadStart { get; init; }
            public float ThreadEnd { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public SpellParticleLayer ThreadLayer { get; init; }
            public int LaneCount { get; init; }
            public int StepCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Age3 { get; init; }
            public int Index { get; init; }
            public int Rise { get; init; }
            public float Fade { get; init; }
            public float FadeDivisor { get; init; }
            public float Opacity { get; init; }
            public float Opacity2 { get; init; }
            public int Stride { get; init; }
            public int Stride2 { get; init; }
            public int Stride3 { get; init; }
            public float Stride4 { get; init; }
            public int Breadth { get; init; }
            public int Breadth2 { get; init; }
            public int Next { get; init; }
            public int Next2 { get; init; }
            public int Offset { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadStartWidth3 { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha2 { get; init; }
            public SpellParticleLayer ThreadLayer2 { get; init; }
            public int WarriorDustWidth { get; init; }
            public int WarriorDustWidth2 { get; init; }
            public int WarriorDustHeight { get; init; }
            public int WarriorDustHeight2 { get; init; }
            public SpellParticleShape WarriorDustShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WarriorDustColour { get; init; }
            public float WarriorDustAlpha { get; init; }
            public SpellParticleLayer WarriorDustLayer { get; init; }
            public int WispWidth2 { get; init; }
            public int WispWidth3 { get; init; }
            public int WispHeight3 { get; init; }
            public int WispHeight4 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            public float WispAlpha2 { get; init; }
            public SpellParticleLayer WispLayer2 { get; init; }
            public int BladeCount { get; init; }
            public int Foot { get; init; }
            public float Foot2 { get; init; }
            public int Foot3 { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public int Height3 { get; init; }
            public int Height4 { get; init; }
            public float Tip { get; init; }
            public float IceSpireOffset { get; init; }
            public int IceSpireWidth { get; init; }
            public int IceSpireWidth2 { get; init; }
            public SpellParticleShape IceSpireShape { get; init; }
            public float IceSpireAlpha { get; init; }
            public float Amount { get; init; }
            public int IceSpireWidth3 { get; init; }
            public float IceSpireHeight { get; init; }
            public SpellParticleShape IceSpireShape2 { get; init; }
            public float IceSpireAlpha2 { get; init; }
            public SpellParticleLayer IceSpireLayer { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int WarriorSparkWidth { get; init; }
            public int WarriorSparkHeight { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WarriorSparkColour { get; init; }
            public float WarriorSparkAlpha { get; init; }
            public SpellParticleLayer WarriorSparkLayer { get; init; }
            public float WarriorSparkRotation { get; init; }
        }

        public sealed class BladeFlameNode
        {
            public int FireSequenceOffset { get; init; }
            public float FireSequenceHeight { get; init; }
            public SpellParticleShape FireSequenceShape { get; init; }
            public float FireSequencePhase { get; init; }
            public float FireSequenceAlpha { get; init; }
            public bool FireSequenceLight { get; init; }
            public float FireSequenceWidth { get; init; }
            public float FireSequenceHeight2 { get; init; }
            public SpellParticleShape FireSequenceShape2 { get; init; }
            public float FireSequencePhase2 { get; init; }
            public float FireSequencePhase3 { get; init; }
            public float FireSequenceAlpha2 { get; init; }
            public SpellParticleLayer FireSequenceLayer { get; init; }
            public int WarriorSparkCount { get; init; }
            public float WarriorSparkRx { get; init; }
            public float WarriorSparkRy { get; init; }
            public int WarriorSparkRise { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
        }

        public sealed class BurstNode
        {
            public int Launch { get; init; }
            public int Tip { get; init; }
            public int Tip2 { get; init; }
            public float Tip3 { get; init; }
            public float Centre { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            public float SmokeSequencePhase2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int FlameSequenceWidth { get; init; }
            public int FlameSequenceHeight { get; init; }
            public SpellParticleShape FlameSequenceShape { get; init; }
            public float FlameSequencePhase { get; init; }
            public float FlameSequenceAlpha { get; init; }
            public bool FlameSequenceLight { get; init; }
            public int BladeEdgeOffset { get; init; }
            public int BladeEdgeWidth { get; init; }
            public int BladeEdgeHeight { get; init; }
            public SpellParticleShape BladeEdgeShape { get; init; }
            public float BladeEdgeAlpha { get; init; }
            public SpellParticleLayer BladeEdgeLayer { get; init; }
            public float BladeEdgeRotation { get; init; }
            public int WarriorImpactOffset { get; init; }
            public int WarriorImpactWidth { get; init; }
            public int WarriorImpactHeight { get; init; }
            public SpellParticleShape WarriorImpactShape { get; init; }
            public float WarriorImpactAlpha { get; init; }
            public float WarriorImpactRotation { get; init; }
            public bool WarriorImpactLight { get; init; }
            public int ParticleCount { get; init; }
            public float Lift { get; init; }
            public int Lift2 { get; init; }
            public float Tail { get; init; }
            public float LineEnd { get; init; }
            public int LineWidth { get; init; }
            public int LineWidth2 { get; init; }
            public float LineAlpha { get; init; }
            public int WarriorSparkOffset { get; init; }
            public int WarriorSparkCount { get; init; }
            public int WarriorSparkRx { get; init; }
            public int WarriorSparkRy { get; init; }
            public int WarriorSparkRise { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
        }

        public sealed class CounterNode
        {
            public int Flash { get; init; }
            public int Body { get; init; }
            public int FlameTongueWidth { get; init; }
            public int FlameTongueHeight { get; init; }
            public SpellParticleShape FlameTongueShape { get; init; }
            public float FlameTongueAlpha { get; init; }
            public bool FlameTongueLight { get; init; }
            public int WarriorImpactWidth { get; init; }
            public int WarriorImpactHeight { get; init; }
            public SpellParticleShape WarriorImpactShape { get; init; }
            public float WarriorImpactAlpha { get; init; }
            public SpellParticleLayer WarriorImpactLayer { get; init; }
            public float WarriorImpactRotation { get; init; }
            public int FlameSequenceOffset { get; init; }
            public int FlameSequenceWidth { get; init; }
            public int FlameSequenceHeight { get; init; }
            public SpellParticleShape FlameSequenceShape { get; init; }
            public float FlameSequencePhase { get; init; }
            public float FlameSequenceAlpha { get; init; }
            public float Trail { get; init; }
            public float TrailDivisor { get; init; }
            public float Afterglow { get; init; }
            public float Afterglow2 { get; init; }
            public float Afterglow3 { get; init; }
            public float AfterglowDivisor { get; init; }
            public int Edge { get; init; }
            public int Edge2 { get; init; }
            public int Edge3 { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public bool WispLight { get; init; }
            public int FireSequenceOffset { get; init; }
            public int FireSequenceWidth { get; init; }
            public int FireSequenceHeight { get; init; }
            public SpellParticleShape FireSequenceShape { get; init; }
            public float FireSequencePhase { get; init; }
            public float FireSequenceAlpha { get; init; }
            public int WarriorCrescentWidth { get; init; }
            public int WarriorCrescentHeight { get; init; }
            public SpellParticleShape WarriorCrescentShape { get; init; }
            public float WarriorCrescentAlpha { get; init; }
            public float WarriorCrescentRotation { get; init; }
            public bool WarriorCrescentLight { get; init; }
            public int WarriorSparkCount { get; init; }
            public int WarriorSparkRx { get; init; }
            public int WarriorSparkRy { get; init; }
            public int WarriorSparkRise { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
        }

        public sealed class DashNode
        {
            public int WarriorDustCount { get; init; }
            public int Edge { get; init; }
            public int Edge2 { get; init; }
            public int WarriorDustVelocity { get; init; }
            public int WarriorDustDuration { get; init; }
            public int WarriorDustDuration2 { get; init; }
            public int WarriorDustSize { get; init; }
            public int WarriorDustSize2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Colour { get; init; }
            public float Opacity { get; init; }
            public SpellParticleShape WarriorDustShape { get; init; }
            public float WarriorDustAspect { get; init; }
            public float WarriorDustGrowth { get; init; }
            public SpellParticleLayer WarriorDustLayer { get; init; }
            public bool WarriorDustCastsLight { get; init; }
            public int BladeEdgeOrigin { get; init; }
            public int BladeEdgeOrigin2 { get; init; }
            public int BladeEdgeVelocity { get; init; }
            public int BladeEdgeDuration { get; init; }
            public int BladeEdgeSize { get; init; }
            public int BladeEdgeSize2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Colour2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Colour3 { get; init; }
            public float Opacity2 { get; init; }
            public SpellParticleShape BladeEdgeShape { get; init; }
            public float BladeEdgeAspect { get; init; }
            public int BladeEdgeRotation { get; init; }
            public float BladeEdgeRotation2 { get; init; }
            public SpellParticleLayer BladeEdgeLayer { get; init; }
            public bool BladeEdgeCastsLight { get; init; }
        }

        public sealed class DrakeChargeNode
        {
            public int Body { get; init; }
            public int Overhead { get; init; }
            public int StrandCount { get; init; }
            public float Phase { get; init; }
            public float Phase2 { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Radius3 { get; init; }
            public float Radius4 { get; init; }
            public int Target { get; init; }
            public float Value { get; init; }
            public float Departure { get; init; }
            public float DepartureDivisor { get; init; }
            public float FinalPoint { get; init; }
            public float FinalPointDivisor { get; init; }
            public int UserLightningCount { get; init; }
            public float TDivisor { get; init; }
            public float Angle { get; init; }
            public float EllipseX { get; init; }
            public float EllipseY { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public SpellParticleLayer Layer2 { get; init; }
            public float Flicker { get; init; }
            public float Flicker2 { get; init; }
            public int Flicker3 { get; init; }
            public float Flicker4 { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public float UserLightningOffset { get; init; }
            public int UserLightningWidth { get; init; }
            public float UserLightningWidth2 { get; init; }
            public float UserLightningHeight { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int SparkCount { get; init; }
            public float Age { get; init; }
            public float Phase3 { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY2 { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public int Point3 { get; init; }
            public float Fade { get; init; }
            public float Fade2 { get; init; }
            public float Fade3 { get; init; }
            public float Fade4 { get; init; }
            public SpellParticleLayer Layer3 { get; init; }
            public SpellParticleLayer Layer4 { get; init; }
            public float SparkWidth { get; init; }
            public float SparkWidth2 { get; init; }
            public float SparkHeight { get; init; }
            public float SparkHeight2 { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkRotation { get; init; }
        }

        public sealed class DrakeContactNode
        {
            public int Centre { get; init; }
            public float Dissipate { get; init; }
            public float DissipateDivisor { get; init; }
            public float Impact { get; init; }
            public float Impact2 { get; init; }
            public int Impact3 { get; init; }
            public int UserLightningCount { get; init; }
            public float StartAngle { get; init; }
            public float StartAngle2 { get; init; }
            public float EndAngle { get; init; }
            public float EndAngle2 { get; init; }
            public int RadiusX { get; init; }
            public int RadiusX2 { get; init; }
            public int RadiusY { get; init; }
            public int RadiusY2 { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public SpellParticleLayer Layer2 { get; init; }
            public float Flicker { get; init; }
            public float Flicker2 { get; init; }
            public int Flicker3 { get; init; }
            public float Spread { get; init; }
            public float Width { get; init; }
            public float Alpha { get; init; }
            public int Offset { get; init; }
            public int Offset2 { get; init; }
            public int UserLightningCount2 { get; init; }
            public float Point { get; init; }
            public int UserLightningWidth { get; init; }
            public int UserLightningWidth2 { get; init; }
            public float UserLightningHeight { get; init; }
            public int UserLightningHeight2 { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public float UserLightningAlpha2 { get; init; }
            public int UserLightningCount3 { get; init; }
            public float Angle { get; init; }
            public int AngleDivisor { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public float Height3 { get; init; }
            public float Height4 { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public float Y { get; init; }
            public float X3 { get; init; }
            public int X4 { get; init; }
            public SpellParticleLayer Layer3 { get; init; }
            public SpellParticleLayer Layer4 { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha2 { get; init; }
            public float Fork { get; init; }
            public float X5 { get; init; }
            public int X6 { get; init; }
            public int Y2 { get; init; }
            public float ThreadStartWidth3 { get; init; }
            public float ThreadEndWidth3 { get; init; }
            public float ThreadAlpha3 { get; init; }
            public int UserLightningWidth3 { get; init; }
            public float UserLightningHeight3 { get; init; }
            public SpellParticleShape UserLightningShape2 { get; init; }
            public float UserLightningAlpha3 { get; init; }
            public int WarriorSparkCount { get; init; }
            public int AngleDivisor2 { get; init; }
            public float Age { get; init; }
            public int Range { get; init; }
            public int Range2 { get; init; }
            public float Range3 { get; init; }
            public float Range4 { get; init; }
            public int Range5 { get; init; }
            public int Point2 { get; init; }
            public int Point3 { get; init; }
            public int Point4 { get; init; }
            public SpellParticleLayer Layer5 { get; init; }
            public SpellParticleLayer Layer6 { get; init; }
            public float WarriorSparkWidth { get; init; }
            public float WarriorSparkWidth2 { get; init; }
            public int WarriorSparkHeight { get; init; }
            public float WarriorSparkHeight2 { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
            public SpellParticleShape WarriorSparkShape2 { get; init; }
            public float WarriorSparkAlpha { get; init; }
            public float WarriorSparkAlpha2 { get; init; }
            public int SmokeSequenceCount { get; init; }
            public float Age2 { get; init; }
            public float Age3 { get; init; }
            public int Radius3 { get; init; }
            public int Radius4 { get; init; }
            public int Radius5 { get; init; }
            public int Plume { get; init; }
            public int Plume2 { get; init; }
            public int Plume3 { get; init; }
            public SpellParticleLayer Layer7 { get; init; }
            public SpellParticleLayer Layer8 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour2 { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public float SmokeSequenceAlpha2 { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public float SmokeSequenceRotation2 { get; init; }
        }

        public sealed class DrakeEdgeSweepNode
        {
            public int PlaneCount { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public SpellParticleLayer Layer2 { get; init; }
            public int GroundY { get; init; }
            public int GroundY2 { get; init; }
            public int UserLightningCount { get; init; }
            public float Along { get; init; }
            public float Along2 { get; init; }
            public int AlongDivisor { get; init; }
            public float Offset { get; init; }
            public float Offset2 { get; init; }
            public float Offset3 { get; init; }
            public float Point { get; init; }
            public float Point2 { get; init; }
            public float BehindActive { get; init; }
            public float BehindInactive { get; init; }
            public float Factor { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public float ThreadAlpha2 { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha3 { get; init; }
            public float ThreadAlpha4 { get; init; }
            public float UserLightningOffset { get; init; }
            public int UserLightningWidth { get; init; }
            public float UserLightningHeight { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public float UserLightningAlpha2 { get; init; }
            public float SparkWidth { get; init; }
            public float SparkWidth2 { get; init; }
            public float SparkHeight { get; init; }
            public float SparkHeight2 { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public float SparkAlpha2 { get; init; }
        }

        public sealed class DrawFrameNode
        {
            public float WindStrength { get; init; }
            public float WindStrength2 { get; init; }
            public int WindStrength3 { get; init; }
            public float WindStrength4 { get; init; }
            public float WindStrength5 { get; init; }
            public float WindStrengthDivisor { get; init; }
            public float EnvelopeFadeStart { get; init; }
            public float EnvelopeTail { get; init; }
            public int BurnOffset { get; init; }
            public int BurnWidth { get; init; }
            public int FlamingSwordTip { get; init; }
            public int FlamingSwordTip2 { get; init; }
            public int FlamingSwordWidth { get; init; }
            public int TwinDrakeBladeHand { get; init; }
            public int TwinDrakeBladeHand2 { get; init; }
        }

        public sealed class DrawWeaponEnchantNode
        {
            public int DepthY { get; init; }
            public int UserEmberCount { get; init; }
            public int Sample { get; init; }
            public int Sample2 { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Along { get; init; }
            public float Along2 { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float Alpha { get; init; }
            public float Alpha2 { get; init; }
            public int UserEmberWidth { get; init; }
            public int UserEmberWidth2 { get; init; }
            public int UserEmberHeight { get; init; }
            public int UserEmberHeight2 { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public SpellParticleShape UserEmberShape2 { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public SpellParticleLayer UserEmberLayer2 { get; init; }
            public float UserEmberRotation { get; init; }
            public float UserEmberRotation2 { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int GroundBase { get; init; }
            public int Segments { get; init; }
            public int Segments2 { get; init; }
            public int PlaneCount { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public SpellParticleLayer Layer2 { get; init; }
            public int Depth { get; init; }
            public int Depth2 { get; init; }
            public float Previous { get; init; }
            public float Previous2 { get; init; }
            public float Previous3 { get; init; }
            public float Along3 { get; init; }
            public float Along4 { get; init; }
            public float Jitter { get; init; }
            public float Jitter2 { get; init; }
            public float Jitter3 { get; init; }
            public float Point { get; init; }
            public float Point2 { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public float ThreadAlpha2 { get; init; }
            public float UserLightningOffset { get; init; }
            public int UserLightningWidth { get; init; }
            public float UserLightningHeight { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public float UserLightningAlpha2 { get; init; }
            public int SparkCount { get; init; }
            public int SparkCount2 { get; init; }
            public int Sample3 { get; init; }
            public int Sample4 { get; init; }
            public float Age3 { get; init; }
            public float Age4 { get; init; }
            public float Along5 { get; init; }
            public float Along6 { get; init; }
            public float Point3 { get; init; }
            public int Point4 { get; init; }
            public int Point5 { get; init; }
            public int Point6 { get; init; }
            public int Depth3 { get; init; }
            public int Depth4 { get; init; }
            public SpellParticleLayer Layer3 { get; init; }
            public SpellParticleLayer Layer4 { get; init; }
            public float Fade { get; init; }
            public float Fade2 { get; init; }
            public float SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public float SparkHeight { get; init; }
            public float SparkHeight2 { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int ParticleCount { get; init; }
            public int Sample5 { get; init; }
            public int Sample6 { get; init; }
            public float Along7 { get; init; }
            public float Along8 { get; init; }
            public float Flick { get; init; }
            public int Flick2 { get; init; }
            public int Flick3 { get; init; }
            public int Flick4 { get; init; }
            public SpellParticleLayer Layer5 { get; init; }
            public SpellParticleLayer Layer6 { get; init; }
            public int Depth5 { get; init; }
            public int Depth6 { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha3 { get; init; }
        }

        public sealed class EngulfNode
        {
            public float DurationFade { get; init; }
            public float DurationFadeDivisor { get; init; }
            public int UserFlameCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Lateral { get; init; }
            public int Lateral2 { get; init; }
            public int Lateral3 { get; init; }
            public int Rise { get; init; }
            public int Rise2 { get; init; }
            public float Flicker { get; init; }
            public float Flicker2 { get; init; }
            public int Flicker3 { get; init; }
            public float Flicker4 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public int SmallWidth { get; init; }
            public int SmallWidth2 { get; init; }
            public int SmallHeight { get; init; }
            public int SmallHeight2 { get; init; }
            public float UserFlameOffset { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public SpellParticleLayer UserFlameLayer2 { get; init; }
            public float UserFlameRotation { get; init; }
            public float UserFlameRotation2 { get; init; }
            public SpellParticleShape FlameSequenceShape { get; init; }
            public float FlameSequencePhase { get; init; }
            public float FlameSequencePhase2 { get; init; }
            public float FlameSequenceAlpha { get; init; }
            public SpellParticleLayer FlameSequenceLayer { get; init; }
            public float FlameTongueWidth { get; init; }
            public float FlameTongueHeight { get; init; }
            public SpellParticleShape FlameTongueShape { get; init; }
            public float FlameTongueAlpha { get; init; }
            public SpellParticleLayer FlameTongueLayer { get; init; }
            public float X3 { get; init; }
            public float Y3 { get; init; }
            public int WarriorSparkWidth { get; init; }
            public int WarriorSparkWidth2 { get; init; }
            public int WarriorSparkHeight { get; init; }
            public int WarriorSparkHeight2 { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
            public SpellParticleShape WarriorSparkShape2 { get; init; }
            public float WarriorSparkAlpha { get; init; }
            public SpellParticleLayer WarriorSparkLayer { get; init; }
            public float SmokeAge { get; init; }
            public float X4 { get; init; }
            public int X5 { get; init; }
            public int Y4 { get; init; }
            public int Y5 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
        }

        public sealed class FuryNode
        {
            public int Eye { get; init; }
            public int WarriorRingWidth { get; init; }
            public int WarriorRingHeight { get; init; }
            public SpellParticleShape WarriorRingShape { get; init; }
            public float WarriorRingAlpha { get; init; }
            public bool WarriorRingLight { get; init; }
            public int SigilEyeWidth { get; init; }
            public int SigilEyeHeight { get; init; }
            public SpellParticleShape SigilEyeShape { get; init; }
            public float SigilEyeAlpha { get; init; }
            public float SigilEyeAlpha2 { get; init; }
            public bool SigilEyeLight { get; init; }
            public int WarriorSparkWidth { get; init; }
            public int WarriorSparkHeight { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
            public float WarriorSparkAlpha { get; init; }
            public SpellParticleLayer WarriorSparkLayer { get; init; }
            public int FireSequenceOffset { get; init; }
            public int FireSequenceWidth { get; init; }
            public int FireSequenceHeight { get; init; }
            public SpellParticleShape FireSequenceShape { get; init; }
            public float FireSequenceAlpha { get; init; }
            public int SideCount { get; init; }
            public int SampleStride { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int FlameSequenceWidth { get; init; }
            public int FlameSequenceHeight { get; init; }
            public SpellParticleShape FlameSequenceShape { get; init; }
            public float FlameSequencePhase { get; init; }
            public float FlameSequenceAlpha { get; init; }
            public SpellParticleLayer FlameSequenceLayer { get; init; }
            public SpellParticleLayer FlameSequenceLayer2 { get; init; }
            public float FlameSequenceRotation { get; init; }
            public bool FlameSequenceLight { get; init; }
            public int X2 { get; init; }
            public int Y2 { get; init; }
            public int FireSequenceWidth2 { get; init; }
            public int FireSequenceHeight2 { get; init; }
            public SpellParticleShape FireSequenceShape2 { get; init; }
            public float FireSequencePhase { get; init; }
            public float FireSequenceAlpha2 { get; init; }
            public SpellParticleLayer FireSequenceLayer { get; init; }
            public float FireSequenceRotation { get; init; }
            public int WarriorSparkOffset { get; init; }
            public int WarriorSparkCount { get; init; }
            public int WarriorSparkRx { get; init; }
            public int WarriorSparkRy { get; init; }
            public int WarriorSparkRise { get; init; }
            public SpellParticleShape WarriorSparkShape2 { get; init; }
        }

        public sealed class GroundDirectionNode
        {
            public float Y { get; init; }
        }

        public sealed class PacketDurationNode
        {
            public int PrepareDurationMs { get; init; }
            public int BurnDurationMs { get; init; }
            public int WardDurationMs { get; init; }
            public int FlamingSwordDurationMs { get; init; }
            public int TwinDrakeBladeDurationMs { get; init; }
            public int FencingOrDashDurationMs { get; init; }
            public int DefaultDurationMs { get; init; }
        }

        public sealed class PaletteNode
        {
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Steel { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Ice { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int White { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Flame { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Ember { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Heat { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Gold { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int PaleGold { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Smoke { get; init; }
        }

        public sealed class ProjectCrescentNode
        {
            public float CrestPower { get; init; }
            public float EdgeTipWidth { get; init; }
            public float EdgeWidthScale { get; init; }
            public float EdgeTaper { get; init; }
            public int CrestLift { get; init; }
            public int NearSideLift { get; init; }
            public float BevelBase { get; init; }
            public float BevelCrest { get; init; }
            public float BevelFloor { get; init; }
            public float BevelNearSide { get; init; }
            public float InnerRadiusScale { get; init; }
            public float OuterRadiusScale { get; init; }
            public SpellParticleShape RibbonShape { get; init; }
            public bool RibbonCastsLight { get; init; }
            public int Segments { get; init; }
            public float SegmentDepth { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public SpellParticleLayer Layer2 { get; init; }
            public int BodyDivisor { get; init; }
            public int CoreDivisor { get; init; }
            public float Amount { get; init; }
            public float Amount2 { get; init; }
            public float Amount3 { get; init; }
            public float Amount4 { get; init; }
            public int Alpha { get; init; }
            public int Alpha2 { get; init; }
            public int Alpha3 { get; init; }
            public float Amount5 { get; init; }
            public float Amount6 { get; init; }
            public float Amount7 { get; init; }
            public float Amount8 { get; init; }
            public SpellParticleLayer Layer3 { get; init; }
            public SpellParticleLayer Layer4 { get; init; }
            public int Alpha4 { get; init; }
            public int Alpha5 { get; init; }
            public int UserSlashCount { get; init; }
            public int SampleStride { get; init; }
            public float Tangent { get; init; }
            public float Tangent2 { get; init; }
            public float Accent { get; init; }
            public float Depth { get; init; }
            public int Length { get; init; }
            public int Length2 { get; init; }
            public SpellParticleLayer Layer5 { get; init; }
            public SpellParticleLayer Layer6 { get; init; }
            public int Val2 { get; init; }
            public int Alpha6 { get; init; }
            public int Alpha7 { get; init; }
            public SpellParticleShape UserSlashShape { get; init; }
            public float UserSlashAspect { get; init; }
            public bool UserSlashCastsLight { get; init; }
            public int I { get; init; }
            public int WarriorSparkCount { get; init; }
            public int SampleStride2 { get; init; }
            public float Edge { get; init; }
            public float Drift { get; init; }
            public int Drift2 { get; init; }
            public float Depth2 { get; init; }
            public SpellParticleLayer Layer7 { get; init; }
            public SpellParticleLayer Layer8 { get; init; }
            public int Val22 { get; init; }
            public int WarriorSparkSize { get; init; }
            public int Alpha8 { get; init; }
            public int Alpha9 { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
            public float WarriorSparkAspect { get; init; }
            public bool WarriorSparkCastsLight { get; init; }
            public int I2 { get; init; }
            public int WispCount { get; init; }
            public int SampleStride3 { get; init; }
            public float Sample { get; init; }
            public float T { get; init; }
            public float Point { get; init; }
            public float Tangent3 { get; init; }
            public SpellParticleLayer Layer9 { get; init; }
            public SpellParticleLayer Layer10 { get; init; }
            public float WispVelocity { get; init; }
            public int WispVelocity2 { get; init; }
            public int Val23 { get; init; }
            public int WispSize { get; init; }
            public int Alpha10 { get; init; }
            public int Alpha11 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAspect { get; init; }
            public bool WispCastsLight { get; init; }
            public int I3 { get; init; }
            public int UserSlashCount2 { get; init; }
            public int SampleStride4 { get; init; }
            public float T2 { get; init; }
            public float Y { get; init; }
            public float CentreStroke { get; init; }
            public float Tangent4 { get; init; }
            public float Depth3 { get; init; }
            public int TierCount { get; init; }
            public float Factor5 { get; init; }
            public float Factor6 { get; init; }
            public float Lateral { get; init; }
            public float Lateral2 { get; init; }
            public float Point2 { get; init; }
            public float StrokeDepth { get; init; }
            public int Length3 { get; init; }
            public int Length4 { get; init; }
            public int Length5 { get; init; }
            public int Alpha12 { get; init; }
            public int Alpha13 { get; init; }
            public int Alpha14 { get; init; }
            public float UserSlashVelocity { get; init; }
            public float UserSlashVelocity2 { get; init; }
            public int Val24 { get; init; }
            public SpellParticleShape UserSlashShape2 { get; init; }
            public float UserSlashAspect2 { get; init; }
            public float UserSlashAspect3 { get; init; }
            public float UserSlashRotation { get; init; }
            public SpellParticleLayer UserSlashLayer { get; init; }
            public SpellParticleLayer UserSlashLayer2 { get; init; }
            public bool UserSlashCastsLight2 { get; init; }
        }

        public sealed class RoarNode
        {
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TintColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int TintColour2 { get; init; }
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public float Spread3 { get; init; }
            public int Head { get; init; }
            public int WarriorRingWidth { get; init; }
            public int WarriorRingHeight { get; init; }
            public SpellParticleShape WarriorRingShape { get; init; }
            public float WarriorRingAlpha { get; init; }
            public SpellParticleLayer WarriorRingLayer { get; init; }
            public bool WarriorRingLight { get; init; }
            public int WarriorImpactWidth { get; init; }
            public int WarriorImpactHeight { get; init; }
            public SpellParticleShape WarriorImpactShape { get; init; }
            public float WarriorImpactAlpha { get; init; }
            public bool WarriorImpactLight { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public float FlashAlpha2 { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int WispCount { get; init; }
            public float Angle { get; init; }
            public int AngleDivisor { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float Ray { get; init; }
            public float Ray2 { get; init; }
            public float Bend { get; init; }
            public float EllipseAngle { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY2 { get; init; }
            public float Bend2 { get; init; }
            public float WispWidth2 { get; init; }
            public int WispHeight2 { get; init; }
            public int WispHeight3 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            public float WispAlpha2 { get; init; }
            public float ThreadStart { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha2 { get; init; }
            public SpellParticleLayer ThreadLayer { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
        }

        public sealed class SkinNode
        {
            public int Body { get; init; }
            public int WarriorRingWidth { get; init; }
            public int WarriorRingHeight { get; init; }
            public SpellParticleShape WarriorRingShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WarriorRingColour { get; init; }
            public float WarriorRingAlpha { get; init; }
            public SpellParticleLayer WarriorRingLayer { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public int I { get; init; }
            public int FlameTongueCount { get; init; }
            public float Wave { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float Tilt { get; init; }
            public float Tilt2 { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public int FlameTongueWidth { get; init; }
            public SpellParticleShape FlameTongueShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlameTongueColour { get; init; }
            public float FlameTongueAlpha { get; init; }
            public float FlameTongueAlpha2 { get; init; }
            public SpellParticleLayer FlameTongueLayer { get; init; }
            public SpellParticleLayer FlameTongueLayer2 { get; init; }
            public bool FlameTongueLight { get; init; }
            public int FlameTongueOffset { get; init; }
            public int FlameTongueWidth2 { get; init; }
            public float FlameTongueHeight { get; init; }
            public SpellParticleShape FlameTongueShape2 { get; init; }
            public float FlameTongueAlpha3 { get; init; }
            public float FlameTongueAlpha4 { get; init; }
            public SpellParticleLayer FlameTongueLayer3 { get; init; }
            public int Overhead { get; init; }
            public int Overhead2 { get; init; }
            public int Overhead3 { get; init; }
            public int WarriorImpactWidth { get; init; }
            public int WarriorImpactHeight { get; init; }
            public SpellParticleShape WarriorImpactShape { get; init; }
            public float WarriorImpactAlpha { get; init; }
            public float WarriorImpactRotation { get; init; }
            public bool WarriorImpactLight { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int ParticleCount { get; init; }
            public int Spread { get; init; }
            public int Spread2 { get; init; }
            public int Y3 { get; init; }
            public int Y4 { get; init; }
            public float X2 { get; init; }
            public int Y5 { get; init; }
            public int LineWidth { get; init; }
            public int LineWidth2 { get; init; }
            public float LineAlpha { get; init; }
            public SpellParticleLayer LineLayer { get; init; }
            public SpellParticleLayer LineLayer2 { get; init; }
            public int WarriorRingWidth2 { get; init; }
            public int WarriorRingHeight2 { get; init; }
            public SpellParticleShape WarriorRingShape2 { get; init; }
            public float WarriorRingAlpha2 { get; init; }
            public SpellParticleLayer WarriorRingLayer2 { get; init; }
        }

        public sealed class SnareNode
        {
            public float Sweep { get; init; }
            public float Sweep2 { get; init; }
            public float Extent { get; init; }
            public float Extent2 { get; init; }
            public float Extent3 { get; init; }
            public float Release { get; init; }
            public float ReleaseDivisor { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public bool WispLight { get; init; }
            public int SmokeSequenceCount { get; init; }
            public int AngleDivisor { get; init; }
            public float Edge { get; init; }
            public float Edge2 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public int Tangent { get; init; }
            public int Tangent2 { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour2 { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public float Bind { get; init; }
            public float BindDivisor { get; init; }
            public float Climb { get; init; }
            public float Climb2 { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseX3 { get; init; }
            public int EllipseY2 { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public SpellParticleLayer Layer2 { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceWidth3 { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public int SmokeSequenceHeight3 { get; init; }
            public SpellParticleShape SmokeSequenceShape2 { get; init; }
            public float SmokeSequencePhase2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour4 { get; init; }
            public float SmokeSequenceAlpha2 { get; init; }
            public float SmokeSequenceAlpha3 { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public int WispOffset { get; init; }
            public int WispWidth2 { get; init; }
            public int WispWidth3 { get; init; }
            public int WispHeight2 { get; init; }
            public int WispHeight3 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            public float WispAlpha2 { get; init; }
            public float WispRotation { get; init; }
        }

        public sealed class StrikeNode
        {
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ColourColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int ColourColour2 { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public int Radius3 { get; init; }
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public float Spread3 { get; init; }
            public float Fade { get; init; }
            public float FadeDivisor { get; init; }
            public float UserSlashOffset { get; init; }
            public float UserSlashWidth { get; init; }
            public float UserSlashHeight { get; init; }
            public SpellParticleShape UserSlashShape { get; init; }
            public float UserSlashAlpha { get; init; }
            public SpellParticleLayer UserSlashLayer { get; init; }
            public float UserSlashRotation { get; init; }
            public float UserSlashOffset2 { get; init; }
            public int UserSlashOffset3 { get; init; }
            public float UserSlashWidth2 { get; init; }
            public float UserSlashHeight2 { get; init; }
            public SpellParticleShape UserSlashShape2 { get; init; }
            public float UserSlashAlpha2 { get; init; }
            public SpellParticleLayer UserSlashLayer2 { get; init; }
            public float UserSlashRotation2 { get; init; }
            public bool UserSlashLight { get; init; }
            public int UserFlameOffset { get; init; }
            public float UserFlameWidth { get; init; }
            public float UserFlameHeight { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public float UserFlameRotation { get; init; }
            public int UserRockOffset { get; init; }
            public float UserRockWidth { get; init; }
            public float UserRockHeight { get; init; }
            public SpellParticleShape UserRockShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int UserRockColour { get; init; }
            public float UserRockAlpha { get; init; }
            public SpellParticleLayer UserRockLayer { get; init; }
            public int Fragments { get; init; }
            public int Fragments2 { get; init; }
            public int Fragments3 { get; init; }
            public float Age { get; init; }
            public float Scatter { get; init; }
            public float Scatter2 { get; init; }
            public float Point { get; init; }
            public float Point2 { get; init; }
            public float Point3 { get; init; }
            public float Point4 { get; init; }
            public int UserEmberWidth { get; init; }
            public int UserEmberWidth2 { get; init; }
            public int UserEmberHeight { get; init; }
            public int UserEmberHeight2 { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public SpellParticleShape UserEmberShape2 { get; init; }
            public SpellParticleShape UserEmberShape3 { get; init; }
            public float UserEmberAlpha { get; init; }
            public float UserEmberAlpha2 { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public SpellParticleLayer UserEmberLayer2 { get; init; }
            public float UserEmberRotation { get; init; }
            public float UserEmberRotation2 { get; init; }
        }

        public sealed class SurgeNode
        {
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public int Spread3 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight { get; init; }
            public int OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int BladeEdgeCount { get; init; }
            public float Angle { get; init; }
            public int AngleDivisor { get; init; }
            public float Length { get; init; }
            public float Length2 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY { get; init; }
            public int EllipseY2 { get; init; }
            public float BladeEdgeOffset { get; init; }
            public int BladeEdgeWidth { get; init; }
            public int BladeEdgeHeight { get; init; }
            public int BladeEdgeHeight2 { get; init; }
            public SpellParticleShape BladeEdgeShape { get; init; }
            public float BladeEdgeAlpha { get; init; }
            public int WarriorSparkCount { get; init; }
            public int WarriorSparkRx { get; init; }
            public int WarriorSparkRx2 { get; init; }
            public int WarriorSparkRy { get; init; }
            public int WarriorSparkRise { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
        }

        public sealed class SweepRibbonNode
        {
            public float StripsDivisor { get; init; }
            public float Centre { get; init; }
            public float Centre2 { get; init; }
            public SpellParticleShape BladeRibbonShape { get; init; }
            public bool BladeRibbonCastsLight { get; init; }
        }

        public sealed class ThrustWindNode
        {
            public float StrandTipThickness { get; init; }
            public float StrandMidThickness { get; init; }
            public int Source { get; init; }
            public int Direction { get; init; }
            public int Direction2 { get; init; }
            public int Factor { get; init; }
            public int Factor2 { get; init; }
            public int Factor3 { get; init; }
            public int Factor4 { get; init; }
            public float Unfold { get; init; }
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public int Travel { get; init; }
            public int Travel2 { get; init; }
            public int Y { get; init; }
            public float Travel3 { get; init; }
            public float Travel4 { get; init; }
            public int LaneCount { get; init; }
            public int Sway { get; init; }
            public float Sway2 { get; init; }
            public int Sway3 { get; init; }
            public int Sway4 { get; init; }
            public int Start { get; init; }
            public float Pull { get; init; }
            public float Pull2 { get; init; }
            public float Bend { get; init; }
            public float Bend2 { get; init; }
            public float Bend3 { get; init; }
            public float End { get; init; }
            public float End2 { get; init; }
            public float Thickness { get; init; }
            public float Thickness2 { get; init; }
            public float Alpha { get; init; }
            public float Alpha2 { get; init; }
            public int Segments { get; init; }
            public float Bank { get; init; }
            public float Bank2 { get; init; }
            public float Bank3 { get; init; }
            public float Factor5 { get; init; }
            public float UpActive { get; init; }
            public float UpInactive { get; init; }
            public float CurlStart { get; init; }
            public float CurlUpper { get; init; }
            public float CurlUpper2 { get; init; }
            public float CurlLower { get; init; }
            public float CurlLower2 { get; init; }
            public float CurlEnd { get; init; }
            public int Thickness3 { get; init; }
            public int Thickness4 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Colour { get; init; }
            public float Alpha3 { get; init; }
            public int Segments2 { get; init; }
            public float Thickness5 { get; init; }
            public int Thickness6 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Colour2 { get; init; }
            public float Alpha4 { get; init; }
            public int Segments3 { get; init; }
            public int ParticleCount { get; init; }
            public float T { get; init; }
            public float T2 { get; init; }
            public float T3 { get; init; }
            public int T4 { get; init; }
            public float T5 { get; init; }
            public int Length { get; init; }
            public float Length2 { get; init; }
            public float Length3 { get; init; }
            public float Length4 { get; init; }
            public float Roll { get; init; }
            public int Roll2 { get; init; }
            public float Roll3 { get; init; }
            public float Roll4 { get; init; }
            public float Pull3 { get; init; }
            public float Pull4 { get; init; }
            public float Bend4 { get; init; }
            public float Bend5 { get; init; }
            public int End3 { get; init; }
            public float Thickness7 { get; init; }
            public float Thickness8 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Colour3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Colour4 { get; init; }
            public float Alpha5 { get; init; }
            public int Segments4 { get; init; }
            public float WispOffset { get; init; }
            public int WispWidth { get; init; }
            public float WispWidth2 { get; init; }
            public int WispHeight { get; init; }
            public float WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public bool WispLight { get; init; }
        }

        public sealed class WeaponSweepNode
        {
            public int Life { get; init; }
            public int SparkCount { get; init; }
            public float Along { get; init; }
            public float Along2 { get; init; }
            public int End { get; init; }
            public int End2 { get; init; }
            public float End3 { get; init; }
            public int End4 { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public SpellParticleLayer Layer2 { get; init; }
            public int GroundY { get; init; }
            public int GroundY2 { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public int SparkHeight2 { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public float Inner { get; init; }
            public float Inner2 { get; init; }
            public float Inner3 { get; init; }
            public float Inner4 { get; init; }
            public float Outer { get; init; }
            public float Outer2 { get; init; }
            public float Outer3 { get; init; }
            public float Depth { get; init; }
            public int Depth2 { get; init; }
            public SpellParticleLayer Depth3 { get; init; }
            public SpellParticleLayer Depth4 { get; init; }
            public int Alpha { get; init; }
            public int Alpha2 { get; init; }
            public int Alpha3 { get; init; }
            public float Inner5 { get; init; }
            public float Outer4 { get; init; }
            public int Val2 { get; init; }
            public int Alpha4 { get; init; }
            public int ForwardTip { get; init; }
            public int SlayingOrbVelocity { get; init; }
            public SpellParticleShape SlayingOrbShape { get; init; }
            public int SlayingOrbSize { get; init; }
            public SpellParticleLayer SlayingOrbLayer { get; init; }
            public float SlayingOrbAspect { get; init; }
            public int SlayingOrbAlpha { get; init; }
            public int SlayingWarriorTripleCutOffset { get; init; }
            public int SlayingWarriorTripleCutVelocity { get; init; }
            public SpellParticleShape SlayingWarriorTripleCutShape { get; init; }
            public int SlayingWarriorTripleCutSize { get; init; }
            public SpellParticleLayer SlayingWarriorTripleCutLayer { get; init; }
            public float SlayingWarriorTripleCutAspect { get; init; }
            public float SlayingWarriorTripleCutRotation { get; init; }
            public bool SlayingWarriorTripleCutCore { get; init; }
            public int SlayingWarriorTripleCutAlpha { get; init; }
            public int SlayingSparkCount { get; init; }
            public SpellParticleShape SlayingSparkShape { get; init; }
            public int SlayingSparkSpread { get; init; }
            public int HalfMoonLift { get; init; }
            public int HalfMoonRadius { get; init; }
            public int HalfMoonWidth { get; init; }
            public float CrossHalfMoonYaw { get; init; }
            public int CrossHalfMoonLift { get; init; }
            public int CrossHalfMoonRadius { get; init; }
            public int CrossHalfMoonWidth { get; init; }
            public float CrossHalfMoonSweep { get; init; }
            public float CrossHalfMoonYaw2 { get; init; }
            public int CrossHalfMoonLift2 { get; init; }
            public int CrossHalfMoonRadius2 { get; init; }
            public int CrossHalfMoonWidth2 { get; init; }
            public float CrossHalfMoonSweep2 { get; init; }
            public int CrossHalfMoonWarriorSparkOffset { get; init; }
            public int CrossHalfMoonWarriorSparkOffset2 { get; init; }
            public int CrossHalfMoonWarriorSparkVelocity { get; init; }
            public SpellParticleShape CrossHalfMoonWarriorSparkShape { get; init; }
            public int CrossHalfMoonWarriorSparkSize { get; init; }
            public SpellParticleLayer CrossHalfMoonWarriorSparkLayer { get; init; }
            public float CrossHalfMoonWarriorSparkAspect { get; init; }
            public bool CrossHalfMoonWarriorSparkCore { get; init; }
            public int CrossHalfMoonWarriorSparkAlpha { get; init; }
            public int TwinDrakeBladeUserLightningCount { get; init; }
            public float TwinDrakeBladeAlong { get; init; }
            public float TwinDrakeBladeAlong2 { get; init; }
            public int TwinDrakeBladeAlongDivisor { get; init; }
            public int TwinDrakeBladeTendril { get; init; }
            public int TwinDrakeBladeTendril2 { get; init; }
            public float TwinDrakeBladeTendril3 { get; init; }
            public int TwinDrakeBladeTendril4 { get; init; }
            public SpellParticleLayer TwinDrakeBladeLayer { get; init; }
            public SpellParticleLayer TwinDrakeBladeLayer2 { get; init; }
            public int TwinDrakeBladeGroundY { get; init; }
            public int TwinDrakeBladeGroundY2 { get; init; }
            public float TwinDrakeBladeThreadStartWidth { get; init; }
            public float TwinDrakeBladeThreadEndWidth { get; init; }
            public float TwinDrakeBladeThreadAlpha { get; init; }
            public float TwinDrakeBladeAmount { get; init; }
            public int TwinDrakeBladeUserLightningWidth { get; init; }
            public float TwinDrakeBladeUserLightningHeight { get; init; }
            public SpellParticleShape TwinDrakeBladeUserLightningShape { get; init; }
            public float TwinDrakeBladeUserLightningAlpha { get; init; }
            public int CounterAttackWarriorCrescentVelocity { get; init; }
            public SpellParticleShape CounterAttackWarriorCrescentShape { get; init; }
            public int CounterAttackWarriorCrescentSize { get; init; }
            public SpellParticleLayer CounterAttackWarriorCrescentLayer { get; init; }
            public float CounterAttackWarriorCrescentAspect { get; init; }
            public float CounterAttackWarriorCrescentRotation { get; init; }
            public bool CounterAttackWarriorCrescentCore { get; init; }
            public int CounterAttackWarriorCrescentAlpha { get; init; }
            public int WarriorSparkVelocity { get; init; }
            public SpellParticleShape WarriorSparkShape { get; init; }
            public int WarriorSparkSize { get; init; }
            public int WarriorSparkSize2 { get; init; }
            public int Val22 { get; init; }
            public SpellParticleLayer WarriorSparkLayer { get; init; }
            public float WarriorSparkAspect { get; init; }
            public int WarriorSparkAlpha { get; init; }
            public bool WarriorSparkCore { get; init; }
        }
    }

    internal sealed class WizardNodes
    {
        public ArcTimingNode ArcTiming { get; init; }
        public BlinkRingNode BlinkRing { get; init; }
        public BlizzardNode Blizzard { get; init; }
        public BoosterNode Booster { get; init; }
        public ChannelNode Channel { get; init; }
        public DisplaceNode Displace { get; init; }
        public DisruptorNode Disruptor { get; init; }
        public DrawArcNode DrawArc { get; init; }
        public DrawFrameNode DrawFrame { get; init; }
        public DrawProjectileNode DrawProjectile { get; init; }
        public ElectricWrapNode ElectricWrap { get; init; }
        public FieldDropNode FieldDrop { get; init; }
        public FireBangNode FireBang { get; init; }
        public FireBurstNode FireBurst { get; init; }
        public FireChargeNode FireCharge { get; init; }
        public FireWallNode FireWall { get; init; }
        public FireWallChargeNode FireWallCharge { get; init; }
        public FlameFieldNode FlameField { get; init; }
        public FlamesNode Flames { get; init; }
        public ForkNode Fork { get; init; }
        public FrostChargeNode FrostCharge { get; init; }
        public FrostFireImpactNode FrostFireImpact { get; init; }
        public GreatFireChargeNode GreatFireCharge { get; init; }
        public HellfireChargeNode HellfireCharge { get; init; }
        public IceBreakNode IceBreak { get; init; }
        public IceStormNode IceStorm { get; init; }
        public IceThrustNode IceThrust { get; init; }
        public LightningNode Lightning { get; init; }
        public MeteorChargeNode MeteorCharge { get; init; }
        public MeteorSealNode MeteorSeal { get; init; }
        public MeteorStrikeNode MeteorStrike { get; init; }
        public MirroringNode Mirroring { get; init; }
        public PacketDurationNode PacketDuration { get; init; }
        public PaletteNode Palette { get; init; }
        public RepelNode Repel { get; init; }
        public SealNode Seal { get; init; }
        public SearingImpactNode SearingImpact { get; init; }
        public ShieldNode Shield { get; init; }
        public SnowContactNode SnowContact { get; init; }
        public SnowballWakeNode SnowballWake { get; init; }
        public StormArrivalNode StormArrival { get; init; }
        public StormEscapeNode StormEscape { get; init; }
        public TeleportLightNode TeleportLight { get; init; }
        public ThunderStormNode ThunderStorm { get; init; }
        public ThunderStrikeNode ThunderStrike { get; init; }
        public TurnUndeadNode TurnUndead { get; init; }
        public VampirismNode Vampirism { get; init; }

        public sealed class ArcTimingNode
        {
            public int RelayDurationMs { get; init; }
            public int VampirismDurationMs { get; init; }
            public int LightningDuration { get; init; }
        }

        public sealed class BlinkRingNode
        {
            public int Lift { get; init; }
            public int Lift2 { get; init; }
            public float Lift3 { get; init; }
            public float Lift4 { get; init; }
            public int Width { get; init; }
            public int Width2 { get; init; }
            public int OrbitY { get; init; }
            public int OrbitWidth { get; init; }
            public float OrbitAlpha { get; init; }
            public float OrbitX { get; init; }
            public int OrbitY2 { get; init; }
            public float OrbitAngle { get; init; }
            public float OrbitSweep { get; init; }
            public int OrbitWidth2 { get; init; }
            public float OrbitAlpha2 { get; init; }
            public float OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public float SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int ParticleCount { get; init; }
            public int AngleDivisor { get; init; }
            public float Angle { get; init; }
            public float EllipseX { get; init; }
            public int EllipseY { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public int LineWidth { get; init; }
            public float LineAlpha { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class BlizzardNode
        {
            public float Cycle { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public float Y { get; init; }
            public int Y2 { get; init; }
            public float Fall { get; init; }
            public float Lull { get; init; }
            public float LullDivisor { get; init; }
            public float WarningDivisor { get; init; }
            public int X3 { get; init; }
            public int Y3 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public int X4 { get; init; }
            public int Y4 { get; init; }
            public int SparkWidth { get; init; }
            public int SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public SpellParticleLayer SparkLayer { get; init; }
            public float SparkRotation { get; init; }
            public int X5 { get; init; }
            public int X6 { get; init; }
            public int Y5 { get; init; }
            public int Y6 { get; init; }
            public int X7 { get; init; }
            public int X8 { get; init; }
            public int Y7 { get; init; }
            public int Y8 { get; init; }
            public float Opacity { get; init; }
            public float Opacity2 { get; init; }
            public int LineWidth { get; init; }
            public bool LineLight { get; init; }
            public float LineStart { get; init; }
            public int LineWidth2 { get; init; }
            public SpellParticleLayer LineLayer { get; init; }
            public float SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public float SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape2 { get; init; }
            public float SmokeSequencePhase2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour2 { get; init; }
            public float SmokeSequenceAlpha2 { get; init; }
            public SpellParticleLayer SmokeSequenceLayer2 { get; init; }
            public float SmokeSequenceRotation2 { get; init; }
            public int FlameWidth { get; init; }
            public int FlameHeight { get; init; }
            public SpellParticleShape FlameShape { get; init; }
            public float FlameRotation { get; init; }
            public int FrostCoreWidth { get; init; }
            public int FrostCoreHeight { get; init; }
            public SpellParticleShape FrostCoreShape { get; init; }
            public SpellParticleLayer FrostCoreLayer { get; init; }
            public float FrostCoreRotation { get; init; }
            public float Land { get; init; }
            public float LandDivisor { get; init; }
            public int SmokeOffset { get; init; }
            public int SmokeWidth { get; init; }
            public int SmokeHeight { get; init; }
            public SpellParticleShape SmokeShape { get; init; }
            public float SmokeAlpha { get; init; }
            public float SmokeAlpha2 { get; init; }
            public float SmokeAlpha3 { get; init; }
            public SpellParticleLayer SmokeLayer { get; init; }
            public bool SmokeLight { get; init; }
            public float Spread { get; init; }
            public float SpreadDivisor { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int ShardCount { get; init; }
            public int ShardRx { get; init; }
            public int ShardRy { get; init; }
            public int ShardRise { get; init; }
            public SpellParticleShape ShardShape { get; init; }
        }

        public sealed class BoosterNode
        {
            public int Eye { get; init; }
            public int Eye2 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int SigilEyeWidth { get; init; }
            public int SigilEyeHeight { get; init; }
            public SpellParticleShape SigilEyeShape { get; init; }
            public float SigilEyeAlpha { get; init; }
            public int SigilEyeWidth2 { get; init; }
            public int SigilEyeHeight2 { get; init; }
            public SpellParticleShape SigilEyeShape2 { get; init; }
            public float SigilEyeAlpha2 { get; init; }
            public float SigilEyeAlpha3 { get; init; }
            public int XDivisor { get; init; }
            public SpellParticleLayer SigilEyeLayer { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public int SparksCount { get; init; }
            public int SparksCount2 { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public float SparksProgressDivisor { get; init; }
            public int SparksRise { get; init; }
            public int FlameTongueOffset { get; init; }
            public int FlameTongueWidth { get; init; }
            public int FlameTongueHeight { get; init; }
            public SpellParticleShape FlameTongueShape { get; init; }
            public float FlameTongueAlpha { get; init; }
            public bool FlameTongueLight { get; init; }
            public int LineEnd { get; init; }
            public int LineWidth { get; init; }
            public float LineAlpha { get; init; }
            public bool LineLight { get; init; }
            public int FeatherCount { get; init; }
            public float Offset { get; init; }
            public int Offset2 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float Y3 { get; init; }
            public int FeatherWidth { get; init; }
            public int FeatherHeight { get; init; }
            public SpellParticleShape FeatherShape { get; init; }
            public float FeatherAlpha { get; init; }
            public SpellParticleLayer FeatherLayer { get; init; }
            public float FeatherRotation { get; init; }
        }

        public sealed class ChannelNode
        {
            public int CountDivisor { get; init; }
            public int Flash { get; init; }
            public SpellParticleLayer Core { get; init; }
            public SpellParticleLayer Core2 { get; init; }
            public float Thread { get; init; }
            public float Decay { get; init; }
            public float Decay2 { get; init; }
            public float Decay3 { get; init; }
            public float SortY { get; init; }
            public float Centre { get; init; }
            public int GlowSize { get; init; }
            public float GlowSize2 { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight3 { get; init; }
            public int WispHeight4 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            public float WispAlpha2 { get; init; }
            public SpellParticleLayer WispLayer2 { get; init; }
            public float WispWidth3 { get; init; }
            public float WispHeight5 { get; init; }
            public SpellParticleShape WispShape3 { get; init; }
            public float WispAlpha3 { get; init; }
            public SpellParticleLayer WispLayer3 { get; init; }
            public float WispWidth4 { get; init; }
            public SpellParticleShape WispShape4 { get; init; }
            public float WispAlpha4 { get; init; }
            public float WispAlpha5 { get; init; }
            public float Thickness { get; init; }
            public float Thickness2 { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha { get; init; }
            public float ThreadAlpha2 { get; init; }
            public int UserLightningWidth { get; init; }
            public int UserLightningHeight { get; init; }
            public float UserLightningHeight2 { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public float Shift { get; init; }
            public int Shift2 { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public int SparkHeight2 { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public float SparkAlpha2 { get; init; }
            public float Distribution { get; init; }
            public float Distribution2 { get; init; }
            public float Forward { get; init; }
            public float Forward2 { get; init; }
            public float Reach { get; init; }
            public int Reach2 { get; init; }
            public float Width { get; init; }
            public float Alpha { get; init; }
        }

        public sealed class DisplaceNode
        {
            public float Chaos { get; init; }
            public int StrideCount { get; init; }
            public float Variation { get; init; }
            public float Variation2 { get; init; }
            public int Detail { get; init; }
            public float Factor { get; init; }
        }

        public sealed class DisruptorNode
        {
            public int PrepareHeight { get; init; }
            public int ReleaseHeight { get; init; }
            public float Height { get; init; }
            public float Height2 { get; init; }
            public int PrepareWidth { get; init; }
            public int ReleaseWidth { get; init; }
            public int OrbOffset { get; init; }
            public float OrbWidth { get; init; }
            public float OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public float Alpha { get; init; }
            public float SmokeSequenceOffset { get; init; }
            public float SmokeSequenceWidth { get; init; }
            public float SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            public float SmokeSequencePhase2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public int ParticleCount { get; init; }
            public int Angle { get; init; }
            public float Angle2 { get; init; }
            public int Centre { get; init; }
            public int CentreDivisor { get; init; }
            public float OrbitX { get; init; }
            public float OrbitX2 { get; init; }
            public int OrbitY { get; init; }
            public int OrbitY2 { get; init; }
            public float OrbitSweep { get; init; }
            public int OrbitWidth { get; init; }
            public float OrbitAlpha { get; init; }
            public int FlameCount { get; init; }
            public float EllipseX { get; init; }
            public float EllipseY { get; init; }
            public int FlameWidth { get; init; }
            public int FlameHeight { get; init; }
            public SpellParticleShape FlameShape { get; init; }
            public float FlameAlpha { get; init; }
            public float FlameRotation { get; init; }
            public int SparksOffset { get; init; }
            public int SparksCount { get; init; }
            public float SparksRx { get; init; }
            public float SparksRy { get; init; }
            public float SparksRise { get; init; }
        }

        public sealed class DrawArcNode
        {
            public int Source { get; init; }
            public int Destination { get; init; }
            public int Head { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int FireCurlWidth { get; init; }
            public int FireCurlHeight { get; init; }
            public SpellParticleShape FireCurlShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FireCurlColour { get; init; }
            public float FireCurlAlpha { get; init; }
            public int FireCurlRotation { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public int WispCount { get; init; }
            public float Age { get; init; }
            public int Tail { get; init; }
            public int WispWidth { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            public float WispAlpha { get; init; }
            public float WispAlpha2 { get; init; }
            public int Spread { get; init; }
            public float Width { get; init; }
            public float Alpha { get; init; }
            public int Forks { get; init; }
            public int Offset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
        }

        public sealed class DrawFrameNode
        {
            public float PrepareFactor { get; init; }
            public float PrepareFactor2 { get; init; }
            public float PrepareValue { get; init; }
            public float EnvelopeFadeStart { get; init; }
            public float EnvelopeTail { get; init; }
            public float EnvelopeFadeStart2 { get; init; }
            public float EnvelopeTail2 { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int SparksOffset { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
            public float Alpha { get; init; }
            public int FireBallX { get; init; }
            public int FireBallY { get; init; }
            public int FireBallDiameter { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MeteorShowerAccentColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MeteorShowerAccentColour2 { get; init; }
            public int MeteorShowerOffset { get; init; }
            public int HellFireX { get; init; }
            public int HellFireY { get; init; }
            public int HellFireDiameter { get; init; }
            public int LightningSource { get; init; }
            public int LightningDestination { get; init; }
            public int LightningDestination2 { get; init; }
            public int LightningY { get; init; }
            public int FrostCrunchOffset { get; init; }
            public int BlizzardOffset { get; init; }
            public int BlizzardRadius { get; init; }
            public int BlizzardCount { get; init; }
        }

        public sealed class DrawProjectileNode
        {
            public int DepthY { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int RimColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int RimColour2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MantleColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int MantleColour2 { get; init; }
            public float Phase { get; init; }
            public int UserFlameCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public int Along { get; init; }
            public int Along2 { get; init; }
            public int Along3 { get; init; }
            public float Point { get; init; }
            public int Point2 { get; init; }
            public int Point3 { get; init; }
            public int UserFlameWidth { get; init; }
            public int UserFlameWidth2 { get; init; }
            public int UserFlameHeight { get; init; }
            public int UserFlameHeight2 { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public SpellParticleLayer UserFlameLayer2 { get; init; }
            public float UserFlameRotation { get; init; }
            public float UserFlameRotation2 { get; init; }
            public int SparkOffset { get; init; }
            public int SparkWidth { get; init; }
            public int SparkWidth2 { get; init; }
            public int SparkHeight { get; init; }
            public int SparkHeight2 { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public SpellParticleShape SparkShape2 { get; init; }
            public float SparkAlpha { get; init; }
            public SpellParticleLayer SparkLayer { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour2 { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int UserFlameWidth3 { get; init; }
            public int UserFlameHeight3 { get; init; }
            public SpellParticleShape UserFlameShape2 { get; init; }
            public float UserFlameAlpha2 { get; init; }
            public SpellParticleLayer UserFlameLayer3 { get; init; }
            public bool UserFlameLight { get; init; }
            public int FrostCoreCount { get; init; }
            public float Theta { get; init; }
            public float Theta2 { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float EllipseY { get; init; }
            public float Pulse { get; init; }
            public float Pulse2 { get; init; }
            public float Pulse3 { get; init; }
            public int FrostCoreWidth { get; init; }
            public int FrostCoreWidth2 { get; init; }
            public int FrostCoreHeight { get; init; }
            public int FrostCoreHeight2 { get; init; }
            public SpellParticleShape FrostCoreShape { get; init; }
            public SpellParticleShape FrostCoreShape2 { get; init; }
            public float FrostCoreAlpha { get; init; }
            public SpellParticleLayer FrostCoreLayer { get; init; }
            public SpellParticleLayer FrostCoreLayer2 { get; init; }
            public int FrostCoreWidth3 { get; init; }
            public int FrostCoreWidth4 { get; init; }
            public int FrostCoreHeight3 { get; init; }
            public int FrostCoreHeight4 { get; init; }
            public SpellParticleShape FrostCoreShape3 { get; init; }
            public float FrostCoreAlpha2 { get; init; }
            public SpellParticleLayer FrostCoreLayer3 { get; init; }
            public int FrostCoreWidth5 { get; init; }
            public int FrostCoreHeight5 { get; init; }
            public SpellParticleShape FrostCoreShape4 { get; init; }
            public float FrostCoreAlpha3 { get; init; }
            public SpellParticleLayer FrostCoreLayer4 { get; init; }
            public bool FrostCoreLight { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int Drift { get; init; }
            public int Drift2 { get; init; }
            public int UserEmberOrigin { get; init; }
            public int UserEmberVelocity { get; init; }
            public int Val2 { get; init; }
            public int UserEmberSize { get; init; }
            public float Opacity { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public float UserEmberGrowth { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public bool UserEmberCastsLight { get; init; }
            public int UserEmberDepthY { get; init; }
        }

        public sealed class ElectricWrapNode
        {
            public int PrepareRadius { get; init; }
            public int ReleaseRadius { get; init; }
            public int PrepareHeight { get; init; }
            public int ReleaseHeight { get; init; }
            public int PrepareDrop { get; init; }
            public int ReleaseDrop { get; init; }
            public float Drop { get; init; }
            public int StartHeight { get; init; }
            public int Flash { get; init; }
            public int WispCount { get; init; }
            public float PrepareSweep { get; init; }
            public float ReleaseSweep { get; init; }
            public float Sweep { get; init; }
            public float Sweep2 { get; init; }
            public int PrepareSpin { get; init; }
            public float ReleaseSpin { get; init; }
            public float Spin { get; init; }
            public int WispCount2 { get; init; }
            public float StepDivisor { get; init; }
            public int LayerDivisor { get; init; }
            public float Opacity { get; init; }
            public float Opacity2 { get; init; }
            public float Opacity3 { get; init; }
            public float Opacity4 { get; init; }
            public float Middle { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public SpellParticleLayer ThreadLayer { get; init; }
            public int Length { get; init; }
            public float Width { get; init; }
            public float Alpha { get; init; }
            public int FlashWidth { get; init; }
            public int FlashWidth2 { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public float WispOffset { get; init; }
            public float WispWidth2 { get; init; }
            public float WispHeight3 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            public float WispAlpha2 { get; init; }
            public bool WispLight { get; init; }
            public float SparksOffset { get; init; }
            public int SparksCount { get; init; }
            public float SparksRx { get; init; }
            public float SparksRy { get; init; }
        }

        public sealed class FieldDropNode
        {
            public float Lead { get; init; }
            public float Lead2 { get; init; }
            public float Value { get; init; }
            public float Value2 { get; init; }
            public int Value3 { get; init; }
            public float Value4 { get; init; }
        }

        public sealed class FireBangNode
        {
            public int Centre { get; init; }
            public int FlameWidth { get; init; }
            public int FlameHeight { get; init; }
            public SpellParticleShape FlameShape { get; init; }
            public float FlameAlpha { get; init; }
            public SpellParticleLayer FlameLayer { get; init; }
            public bool FlameLight { get; init; }
            public int FlameCount { get; init; }
            public float Angle { get; init; }
            public int AngleDivisor { get; init; }
            public float Angle2 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float Ray { get; init; }
            public float Ray2 { get; init; }
            public float Ray3 { get; init; }
            public float LineStart { get; init; }
            public int LineWidth { get; init; }
            public float LineAlpha { get; init; }
            public float LineStart2 { get; init; }
            public float LineEnd { get; init; }
            public int LineWidth2 { get; init; }
            public float LineAlpha2 { get; init; }
            public float FlameOffset { get; init; }
            public int FlameWidth2 { get; init; }
            public int FlameHeight2 { get; init; }
            public SpellParticleShape FlameShape2 { get; init; }
            public float FlameAlpha2 { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public int Spread3 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public int FlameCount2 { get; init; }
            public int AngleDivisor2 { get; init; }
            public float Angle3 { get; init; }
            public float Distance { get; init; }
            public float Distance2 { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY2 { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public float Height3 { get; init; }
            public int Width { get; init; }
            public int Width2 { get; init; }
            public float Alpha { get; init; }
            public float FlameOffset2 { get; init; }
            public int FlameWidth3 { get; init; }
            public int FlameHeight3 { get; init; }
            public SpellParticleShape FlameShape3 { get; init; }
            public float FlameAlpha3 { get; init; }
            public SpellParticleLayer FlameLayer2 { get; init; }
            public SpellParticleLayer FlameLayer3 { get; init; }
            public float FlameRotation { get; init; }
            public int SparksOffset { get; init; }
            public int SparksCount2 { get; init; }
            public int SparksRx2 { get; init; }
            public int SparksRy2 { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class FireBurstNode
        {
            public float Radius { get; init; }
            public float Radius2 { get; init; }
            public float Radius3 { get; init; }
            public float Fade { get; init; }
            public float FadeDivisor { get; init; }
            public float Time { get; init; }
            public float Time2 { get; init; }
            public int Count { get; init; }
            public int Count2 { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public int Rise { get; init; }
            public int Rise2 { get; init; }
            public float Y { get; init; }
            public int Y2 { get; init; }
            public float UserFlameWidth { get; init; }
            public float UserFlameWidth2 { get; init; }
            public int UserFlameHeight { get; init; }
            public int UserFlameHeight2 { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public SpellParticleLayer UserFlameLayer2 { get; init; }
            public float UserFlameRotation { get; init; }
            public float UserFlameRotation2 { get; init; }
            public int UserEmberOffset { get; init; }
            public int UserEmberOffset2 { get; init; }
            public int UserEmberWidth { get; init; }
            public int UserEmberWidth2 { get; init; }
            public int UserEmberHeight { get; init; }
            public int UserEmberHeight2 { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public float UserEmberAlpha { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceOffset2 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour2 { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
        }

        public sealed class FireChargeNode
        {
            public int Hand { get; init; }
            public int Hand2 { get; init; }
            public int Hand3 { get; init; }
            public int UserEmberCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Theta { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Radius3 { get; init; }
            public float EllipseY { get; init; }
            public int Point { get; init; }
            public int UserEmberWidth { get; init; }
            public int UserEmberWidth2 { get; init; }
            public int UserEmberHeight { get; init; }
            public int UserEmberHeight2 { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public SpellParticleShape UserEmberShape2 { get; init; }
            public float UserEmberAlpha { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public SpellParticleLayer UserEmberLayer2 { get; init; }
            public float UserEmberRotation { get; init; }
            public float UserEmberRotation2 { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
        }

        public sealed class FireWallNode
        {
            public int Width { get; init; }
            public int Height { get; init; }
            public int Alpha { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int Width2 { get; init; }
            public int Height2 { get; init; }
            public float Alpha2 { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public float Smoke { get; init; }
            public float X2 { get; init; }
            public int X3 { get; init; }
            public int Y2 { get; init; }
            public int Y3 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public int SparksOffset { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public float SparksProgress { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class FireWallChargeNode
        {
            public float ClimbDivisor { get; init; }
            public int Overhead { get; init; }
            public float Ignite { get; init; }
            public float Burst { get; init; }
            public float BurstDivisor { get; init; }
            public int UserFlameCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Scatter { get; init; }
            public int Scatter2 { get; init; }
            public int Scatter3 { get; init; }
            public int Upward { get; init; }
            public int Upward2 { get; init; }
            public float X { get; init; }
            public int Y { get; init; }
            public float X2 { get; init; }
            public float Y2 { get; init; }
            public int Y3 { get; init; }
            public float Point { get; init; }
            public float Alpha { get; init; }
            public float Alpha2 { get; init; }
            public int UserFlameWidth { get; init; }
            public int UserFlameWidth2 { get; init; }
            public int UserFlameHeight { get; init; }
            public int UserFlameHeight2 { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public SpellParticleLayer UserFlameLayer2 { get; init; }
            public float UserFlameRotation { get; init; }
            public float UserFlameRotation2 { get; init; }
            public float Gather { get; init; }
            public float GatherDivisor { get; init; }
            public int UserFlameCount2 { get; init; }
            public int Angle { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Radius3 { get; init; }
            public float EllipseY { get; init; }
            public int UserFlameWidth3 { get; init; }
            public int UserFlameWidth4 { get; init; }
            public int UserFlameHeight3 { get; init; }
            public int UserFlameHeight4 { get; init; }
            public SpellParticleShape UserFlameShape2 { get; init; }
            public float UserFlameAlpha2 { get; init; }
            public SpellParticleLayer UserFlameLayer3 { get; init; }
            public SpellParticleLayer UserFlameLayer4 { get; init; }
            public float UserFlameRotation3 { get; init; }
            public int UserFlameCount3 { get; init; }
            public int ThetaDivisor { get; init; }
            public int EllipseX { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY2 { get; init; }
            public int EllipseY3 { get; init; }
            public int Point2 { get; init; }
            public int UserFlameWidth5 { get; init; }
            public int UserFlameWidth6 { get; init; }
            public int UserFlameHeight5 { get; init; }
            public int UserFlameHeight6 { get; init; }
            public SpellParticleShape UserFlameShape3 { get; init; }
            public SpellParticleShape UserFlameShape4 { get; init; }
            public float UserFlameAlpha3 { get; init; }
            public float UserFlameAlpha4 { get; init; }
            public SpellParticleLayer UserFlameLayer5 { get; init; }
            public int SmokeSequenceCount { get; init; }
            public int X3 { get; init; }
            public int X4 { get; init; }
            public int Y4 { get; init; }
            public int Y5 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public float Value { get; init; }
            public float ValueDivisor { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
        }

        public sealed class FlameFieldNode
        {
            public float Open { get; init; }
            public float Open2 { get; init; }
            public float Open3 { get; init; }
            public int Radius { get; init; }
            public float Rotation { get; init; }
            public float OrbWidth { get; init; }
            public float OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public float Alpha { get; init; }
            public int Spokes { get; init; }
            public float X { get; init; }
            public float Y { get; init; }
            public float Alpha2 { get; init; }
            public int Spokes2 { get; init; }
            public float OrbWidth2 { get; init; }
            public float OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public SpellParticleLayer OrbLayer2 { get; init; }
            public bool OrbLight2 { get; init; }
            public float SigilDiscWidth { get; init; }
            public float SigilDiscHeight { get; init; }
            public SpellParticleShape SigilDiscShape { get; init; }
            public float SigilDiscAlpha { get; init; }
            public SpellParticleLayer SigilDiscLayer { get; init; }
            public float Upper { get; init; }
            public float Upper2 { get; init; }
            public int Crown { get; init; }
            public int Crown2 { get; init; }
            public float X2 { get; init; }
            public float Y2 { get; init; }
            public float Alpha3 { get; init; }
            public int Spokes3 { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public float OrbWidth3 { get; init; }
            public float OrbHeight3 { get; init; }
            public SpellParticleShape OrbShape3 { get; init; }
            public float OrbAlpha3 { get; init; }
            public bool OrbLight3 { get; init; }
            public float OrbWidth4 { get; init; }
            public float OrbHeight4 { get; init; }
            public SpellParticleShape OrbShape4 { get; init; }
            public float OrbAlpha4 { get; init; }
            public SpellParticleLayer OrbLayer3 { get; init; }
            public bool OrbLight4 { get; init; }
            public int ParticleCount { get; init; }
            public int AngleDivisor { get; init; }
            public float EllipseX { get; init; }
            public float EllipseY { get; init; }
            public int Ray { get; init; }
            public float LineStart { get; init; }
            public float LineEnd { get; init; }
            public int LineWidth { get; init; }
            public int LineWidth2 { get; init; }
            public float LineAlpha { get; init; }
            public float LineAlpha2 { get; init; }
            public int ParticleCount2 { get; init; }
            public int AngleDivisor2 { get; init; }
            public float EllipseX2 { get; init; }
            public float EllipseX3 { get; init; }
            public float EllipseY2 { get; init; }
            public float Beat { get; init; }
            public float Beat2 { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public int LineStart2 { get; init; }
            public int LineStart3 { get; init; }
            public int LineWidth3 { get; init; }
            public float LineAlpha3 { get; init; }
            public bool LineLight { get; init; }
            public int Width { get; init; }
            public int Width2 { get; init; }
            public float Height3 { get; init; }
            public float Height4 { get; init; }
            public float Alpha4 { get; init; }
            public int SparksOffset { get; init; }
            public int SparksCount { get; init; }
            public float SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class FlamesNode
        {
            public float Phase { get; init; }
            public float Phase2 { get; init; }
            public int Sway { get; init; }
            public float Sway2 { get; init; }
            public float OrbWidth { get; init; }
            public float OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public float Column { get; init; }
            public float Centre { get; init; }
            public float FlameSequenceWidth { get; init; }
            public SpellParticleShape FlameSequenceShape { get; init; }
            public float FlameSequenceAlpha { get; init; }
            public bool FlameSequenceLight { get; init; }
            public float UserFlameOffset { get; init; }
            public float UserFlameWidth { get; init; }
            public float UserFlameHeight { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public float UserFlameAlpha2 { get; init; }
            public float X { get; init; }
            public bool UserFlameLight { get; init; }
            public float X2 { get; init; }
            public float Y { get; init; }
            public float FlameSequenceWidth2 { get; init; }
            public float FlameSequenceHeight { get; init; }
            public SpellParticleShape FlameSequenceShape2 { get; init; }
            public float FlameSequencePhase { get; init; }
            public float FlameSequenceAlpha2 { get; init; }
            public float FlameSequenceRotation { get; init; }
            public SpellParticleLayer Core { get; init; }
            public SpellParticleLayer Core2 { get; init; }
            public float X3 { get; init; }
            public float Y2 { get; init; }
            public float FlameSequenceWidth3 { get; init; }
            public float FlameSequenceHeight2 { get; init; }
            public SpellParticleShape FlameSequenceShape3 { get; init; }
            public float FlameSequencePhase2 { get; init; }
            public float FlameSequenceAlpha3 { get; init; }
            public float FlameSequenceRotation2 { get; init; }
            public float OrbOffset { get; init; }
            public float OrbWidth2 { get; init; }
            public float OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public float OrbAlpha2 { get; init; }
        }

        public sealed class ForkNode
        {
            public float Angle { get; init; }
            public float Angle2 { get; init; }
            public float Reach { get; init; }
            public float Reach2 { get; init; }
            public float Spread { get; init; }
            public float Progress { get; init; }
            public float Taper { get; init; }
            public float Thickness { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float Length { get; init; }
            public float Width { get; init; }
            public float Alpha { get; init; }
        }

        public sealed class FrostChargeNode
        {
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public float Centre3 { get; init; }
            public int UserFlameCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Angle { get; init; }
            public float Angle2 { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Radius3 { get; init; }
            public float Radius4 { get; init; }
            public int Radius5 { get; init; }
            public float EllipseY { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public int UserFlameWidth { get; init; }
            public int UserFlameWidth2 { get; init; }
            public int UserFlameHeight { get; init; }
            public int UserFlameHeight2 { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public SpellParticleLayer UserFlameLayer2 { get; init; }
            public float UserFlameRotation { get; init; }
            public float UserFlameRotation2 { get; init; }
            public int UserEmberOffset { get; init; }
            public int UserEmberWidth { get; init; }
            public int UserEmberWidth2 { get; init; }
            public int UserEmberHeight { get; init; }
            public int UserEmberHeight2 { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public float UserEmberAlpha { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int FrostCoreWidth { get; init; }
            public int FrostCoreHeight { get; init; }
            public SpellParticleShape FrostCoreShape { get; init; }
            public float FrostCoreAlpha { get; init; }
            public SpellParticleLayer FrostCoreLayer { get; init; }
            public int FrostCoreWidth2 { get; init; }
            public int FrostCoreHeight2 { get; init; }
            public SpellParticleShape FrostCoreShape2 { get; init; }
            public float FrostCoreAlpha2 { get; init; }
            public SpellParticleLayer FrostCoreLayer2 { get; init; }
            public bool FrostCoreLight { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int FrostCoreCount { get; init; }
            public float Angle3 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY2 { get; init; }
            public int EllipseY3 { get; init; }
            public int FrostCoreWidth3 { get; init; }
            public int FrostCoreWidth4 { get; init; }
            public int FrostCoreHeight3 { get; init; }
            public int FrostCoreHeight4 { get; init; }
            public SpellParticleShape FrostCoreShape3 { get; init; }
            public float FrostCoreAlpha3 { get; init; }
            public float FrostCoreAlpha4 { get; init; }
            public SpellParticleLayer FrostCoreLayer3 { get; init; }
        }

        public sealed class FrostFireImpactNode
        {
            public float Fade { get; init; }
            public float FadeDivisor { get; init; }
            public int UserFlameCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Angle { get; init; }
            public float Angle2 { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Radius3 { get; init; }
            public float Radius4 { get; init; }
            public float Radius5 { get; init; }
            public float EllipseY { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public int UserFlameWidth { get; init; }
            public int UserFlameWidth2 { get; init; }
            public int UserFlameHeight { get; init; }
            public int UserFlameHeight2 { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public SpellParticleLayer UserFlameLayer2 { get; init; }
            public float UserFlameRotation { get; init; }
            public float UserFlameRotation2 { get; init; }
            public int ShardOffset { get; init; }
            public int ShardWidth { get; init; }
            public int ShardWidth2 { get; init; }
            public int ShardHeight { get; init; }
            public int ShardHeight2 { get; init; }
            public SpellParticleShape ShardShape { get; init; }
            public float ShardAlpha { get; init; }
            public SpellParticleLayer ShardLayer { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int FrostCoreWidth { get; init; }
            public int FrostCoreHeight { get; init; }
            public SpellParticleShape FrostCoreShape { get; init; }
            public float FrostCoreAlpha { get; init; }
            public SpellParticleLayer FrostCoreLayer { get; init; }
            public int FrostCoreWidth2 { get; init; }
            public int FrostCoreHeight2 { get; init; }
            public SpellParticleShape FrostCoreShape2 { get; init; }
            public float FrostCoreAlpha2 { get; init; }
            public SpellParticleLayer FrostCoreLayer2 { get; init; }
            public bool FrostCoreLight { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int FrostCoreCount { get; init; }
            public float Angle3 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseX3 { get; init; }
            public int EllipseY2 { get; init; }
            public int EllipseY3 { get; init; }
            public int EllipseY4 { get; init; }
            public int FrostCoreWidth3 { get; init; }
            public int FrostCoreWidth4 { get; init; }
            public int FrostCoreHeight3 { get; init; }
            public int FrostCoreHeight4 { get; init; }
            public SpellParticleShape FrostCoreShape3 { get; init; }
            public float FrostCoreAlpha3 { get; init; }
            public float FrostCoreAlpha4 { get; init; }
            public SpellParticleLayer FrostCoreLayer3 { get; init; }
        }

        public sealed class GreatFireChargeNode
        {
            public int Focus { get; init; }
            public int Focus2 { get; init; }
            public int UserEmberCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Angle { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Radius3 { get; init; }
            public float EllipseY { get; init; }
            public int Point { get; init; }
            public int UserEmberWidth { get; init; }
            public int UserEmberWidth2 { get; init; }
            public int UserEmberHeight { get; init; }
            public int UserEmberHeight2 { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public SpellParticleShape UserEmberShape2 { get; init; }
            public float UserEmberAlpha { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public SpellParticleLayer UserEmberLayer2 { get; init; }
            public float UserEmberRotation { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
        }

        public sealed class HellfireChargeNode
        {
            public int Centre { get; init; }
            public int OrbitX { get; init; }
            public int OrbitY { get; init; }
            public float OrbitAngle { get; init; }
            public float OrbitSweep { get; init; }
            public int OrbitWidth { get; init; }
            public float OrbitAlpha { get; init; }
            public int OrbitX2 { get; init; }
            public int OrbitY2 { get; init; }
            public float OrbitAngle2 { get; init; }
            public float OrbitAngle3 { get; init; }
            public float OrbitSweep2 { get; init; }
            public int OrbitWidth2 { get; init; }
            public float OrbitAlpha2 { get; init; }
            public int FlameCount { get; init; }
            public float Angle { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public int FlameWidth { get; init; }
            public int FlameHeight { get; init; }
            public SpellParticleShape FlameShape { get; init; }
            public float FlameAlpha { get; init; }
            public bool FlameLight { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int FlameWidth2 { get; init; }
            public int FlameHeight2 { get; init; }
            public SpellParticleShape FlameShape2 { get; init; }
            public float FlameAlpha2 { get; init; }
            public float FlameRotation { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
        }

        public sealed class IceBreakNode
        {
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public float Spread3 { get; init; }
            public float OrbWidth { get; init; }
            public float OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public float SmokeSequenceWidth { get; init; }
            public float SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            public float SmokeSequencePhase2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public float EllipseY { get; init; }
            public int Length { get; init; }
            public int Length2 { get; init; }
            public float Length3 { get; init; }
            public float IceSpireWidth { get; init; }
            public SpellParticleShape IceSpireShape { get; init; }
            public float IceSpireAlpha { get; init; }
            public SpellParticleLayer IceSpireLayer { get; init; }
            public float FrostCoreWidth { get; init; }
            public float FrostCoreHeight { get; init; }
            public SpellParticleShape FrostCoreShape { get; init; }
            public float FrostCoreAlpha { get; init; }
            public SpellParticleLayer FrostCoreLayer { get; init; }
            public float FrostCoreWidth2 { get; init; }
            public float FrostCoreHeight2 { get; init; }
            public SpellParticleShape FrostCoreShape2 { get; init; }
            public float FrostCoreAlpha2 { get; init; }
            public SpellParticleLayer FrostCoreLayer2 { get; init; }
            public int FrostCoreRotation { get; init; }
            public bool FrostCoreLight { get; init; }
            public float ShardRx { get; init; }
            public float ShardRy { get; init; }
            public int ShardRise { get; init; }
            public SpellParticleShape ShardShape { get; init; }
        }

        public sealed class IceStormNode
        {
            public float PrepareSpread { get; init; }
            public float ReleaseSpread { get; init; }
            public float PrepareWind { get; init; }
            public float ReleaseWind { get; init; }
            public float PrepareRuneFade { get; init; }
            public float RuneFade { get; init; }
            public float RuneFade2 { get; init; }
            public float RuneFadeDivisor { get; init; }
            public int SigilDiscWidth { get; init; }
            public int SigilDiscHeight { get; init; }
            public SpellParticleShape SigilDiscShape { get; init; }
            public float SigilDiscAlpha { get; init; }
            public float SigilDiscAlpha2 { get; init; }
            public int X { get; init; }
            public SpellParticleLayer SigilDiscLayer { get; init; }
            public int SigilDiscRotation { get; init; }
            public bool SigilDiscLight { get; init; }
            public int PrepareRibbons { get; init; }
            public int ReleaseRibbons { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Angle { get; init; }
            public float Angle2 { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public int Radius3 { get; init; }
            public int Radius4 { get; init; }
            public float Radius5 { get; init; }
            public int Radius6 { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public int Centre { get; init; }
            public float Breathe { get; init; }
            public float Breathe2 { get; init; }
            public int Breathe3 { get; init; }
            public int Width { get; init; }
            public int Width2 { get; init; }
            public int Width3 { get; init; }
            public float Thickness { get; init; }
            public int Thickness2 { get; init; }
            public int Thickness3 { get; init; }
            public float Alpha { get; init; }
            public SpellParticleShape UserSlashShape { get; init; }
            public int TailCount { get; init; }
            public float BackAngle { get; init; }
            public float BackAngle2 { get; init; }
            public int Back { get; init; }
            public float Back2 { get; init; }
            public float TailFade { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadStartWidth2 { get; init; }
            public float ThreadStartWidth3 { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadEndWidth2 { get; init; }
            public float ThreadAlpha { get; init; }
            public float WispOffset { get; init; }
            public int WispWidth { get; init; }
            public float WispHeight { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape { get; init; }
            public float WispAlpha { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public float SmokeSequenceWidth { get; init; }
            public float SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public int SnowCount { get; init; }
            public int Flake { get; init; }
            public float DriftAge { get; init; }
            public float Point { get; init; }
            public float Point2 { get; init; }
            public float X2 { get; init; }
            public int X3 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public float SparkWidth { get; init; }
            public float SparkWidth2 { get; init; }
            public float SparkHeight { get; init; }
            public float SparkHeight2 { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            public float SparkAlpha { get; init; }
            public int PrepareDust { get; init; }
            public int ReleaseDust { get; init; }
            public float Age3 { get; init; }
            public float Angle3 { get; init; }
            public float Angle4 { get; init; }
            public int Radius7 { get; init; }
            public int Radius8 { get; init; }
            public int Point3 { get; init; }
            public int Point4 { get; init; }
            public float SparkWidth3 { get; init; }
            public float SparkWidth4 { get; init; }
            public float SparkHeight3 { get; init; }
            public float SparkHeight4 { get; init; }
            public SpellParticleShape SparkShape2 { get; init; }
            public float SparkAlpha2 { get; init; }
            public float SparkRotation { get; init; }
        }

        public sealed class IceThrustNode
        {
            public int Reach { get; init; }
            public int Reach2 { get; init; }
            public float Surge { get; init; }
            public float Surge2 { get; init; }
            public float Surge3 { get; init; }
            public int Chest { get; init; }
            public int OrbOffset { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public float Value { get; init; }
            public bool OrbLight { get; init; }
            public int Stream { get; init; }
            public int StreamCount { get; init; }
            public int Breadth { get; init; }
            public float Length { get; init; }
            public int SampleCount { get; init; }
            public float Roll { get; init; }
            public int Roll2 { get; init; }
            public float Roll3 { get; init; }
            public int Lane { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public float Taper { get; init; }
            public float Taper2 { get; init; }
            public float SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            public float SmokeSequencePhase2 { get; init; }
            public float SmokeSequencePhase3 { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public float ThreadStartWidth { get; init; }
            public float ThreadEndWidth { get; init; }
            public float ThreadAlpha { get; init; }
            public SpellParticleLayer ThreadLayer { get; init; }
            public int OrbOffset2 { get; init; }
            public int OrbOffset3 { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbWidth3 { get; init; }
            public int OrbHeight2 { get; init; }
            public int OrbHeight3 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public int SmokeWidth { get; init; }
            public int SmokeHeight { get; init; }
            public SpellParticleShape SmokeShape { get; init; }
            public float SmokeAlpha { get; init; }
            public SpellParticleLayer SmokeLayer { get; init; }
            public float Start { get; init; }
            public int End { get; init; }
            public int Spread { get; init; }
            public int Spread2 { get; init; }
            public float Width { get; init; }
            public float Width2 { get; init; }
            public float Alpha { get; init; }
            public float Alpha2 { get; init; }
            public float Time { get; init; }
            public int Forks { get; init; }
            public int Offset { get; init; }
            public int Offset2 { get; init; }
            public int OrbOffset4 { get; init; }
            public int OrbCount { get; init; }
            public int OrbRx { get; init; }
            public int OrbRy { get; init; }
            public int OrbRise { get; init; }
            public SpellParticleShape OrbShape3 { get; init; }
        }

        public sealed class LightningNode
        {
            public int Spread { get; init; }
            public float Width { get; init; }
            public int Alpha { get; init; }
            public int Forks { get; init; }
            public int Source { get; init; }
            public int End { get; init; }
            public int Spread2 { get; init; }
            public float Width2 { get; init; }
            public float Alpha2 { get; init; }
            public float Time { get; init; }
            public int Forks2 { get; init; }
            public int Offset { get; init; }
            public int Source2 { get; init; }
            public float Amount { get; init; }
            public int End2 { get; init; }
            public int Spread3 { get; init; }
            public float Width3 { get; init; }
            public float Alpha3 { get; init; }
            public float Time2 { get; init; }
            public int Forks3 { get; init; }
            public int Offset2 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
        }

        public sealed class MeteorChargeNode
        {
            public int Centre { get; init; }
            public int SigilDiscCount { get; init; }
            public float Angle { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public int SigilDiscWidth { get; init; }
            public int SigilDiscHeight { get; init; }
            public SpellParticleShape SigilDiscShape { get; init; }
            public float SigilDiscAlpha { get; init; }
            public bool SigilDiscLight { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
        }

        public sealed class MeteorSealNode
        {
            public float Open { get; init; }
            public float Open2 { get; init; }
            public int Open3 { get; init; }
            public float Pulse { get; init; }
            public float Pulse2 { get; init; }
            public int Pulse3 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public float Rotation { get; init; }
            public int Spokes { get; init; }
            public int GlyphWidth { get; init; }
            public int GlyphHeight { get; init; }
            public SpellParticleShape GlyphShape { get; init; }
            public float GlyphAlpha { get; init; }
            public SpellParticleLayer GlyphLayer { get; init; }
            public float GlyphRotation { get; init; }
        }

        public sealed class MeteorStrikeNode
        {
            public float Phase { get; init; }
            public float Warmup { get; init; }
            public float Warmup2 { get; init; }
            public float Warmup3 { get; init; }
            public float WarmupDivisor { get; init; }
            public int SmokeWidth { get; init; }
            public int SmokeHeight { get; init; }
            public SpellParticleShape SmokeShape { get; init; }
            public float SmokeAlpha { get; init; }
            public SpellParticleLayer SmokeLayer { get; init; }
            public bool SmokeLight { get; init; }
            public float Fall { get; init; }
            public int X { get; init; }
            public int X2 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public int Tail { get; init; }
            public int Tail2 { get; init; }
            public int Tail3 { get; init; }
            public int Tail4 { get; init; }
            public int LineWidth { get; init; }
            public float LineAlpha { get; init; }
            public bool LineLight { get; init; }
            public float LineStart { get; init; }
            public int LineWidth2 { get; init; }
            public float LineAlpha2 { get; init; }
            public SpellParticleLayer LineLayer { get; init; }
            public float SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public int FireSequenceWidth { get; init; }
            public int FireSequenceHeight { get; init; }
            public SpellParticleShape FireSequenceShape { get; init; }
            public float FireSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FireSequenceColour { get; init; }
            public float FireSequenceAlpha { get; init; }
            public int UserRockWidth { get; init; }
            public int UserRockHeight { get; init; }
            public SpellParticleShape UserRockShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int UserRockColour { get; init; }
            public float UserRockAlpha { get; init; }
            public SpellParticleLayer UserRockLayer { get; init; }
            public float UserRockRotation { get; init; }
            public float X3 { get; init; }
            public float UserRockRotation2 { get; init; }
            public int X4 { get; init; }
            public int Y3 { get; init; }
            public int UserFlameWidth { get; init; }
            public int UserFlameHeight { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public bool UserFlameLight { get; init; }
            public float Flash { get; init; }
            public float FlashDivisor { get; init; }
            public int FlameWidth { get; init; }
            public int FlameHeight { get; init; }
            public SpellParticleShape FlameShape { get; init; }
            public float FlameAlpha { get; init; }
            public SpellParticleLayer FlameLayer { get; init; }
            public bool FlameLight { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
            public float Height2 { get; init; }
            public float Alpha { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
            public int FragmentCount { get; init; }
            public float Age { get; init; }
            public float X5 { get; init; }
            public int X6 { get; init; }
            public int X7 { get; init; }
            public int Y4 { get; init; }
            public int Y5 { get; init; }
            public int Y6 { get; init; }
            public int UserRockWidth2 { get; init; }
            public int UserRockWidth3 { get; init; }
            public int UserRockHeight2 { get; init; }
            public int UserRockHeight3 { get; init; }
            public SpellParticleShape UserRockShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int UserRockColour2 { get; init; }
            public float UserRockAlpha2 { get; init; }
            public float UserRockAlpha3 { get; init; }
            public SpellParticleLayer UserRockLayer2 { get; init; }
            public float UserRockRotation3 { get; init; }
            public float UserRockRotation4 { get; init; }
        }

        public sealed class MirroringNode
        {
            public int Lift { get; init; }
            public int Lift2 { get; init; }
            public int Lift3 { get; init; }
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public float Spread3 { get; init; }
            public int SmokeWidth { get; init; }
            public int SmokeHeight { get; init; }
            public SpellParticleShape SmokeShape { get; init; }
            public float SmokeAlpha { get; init; }
            public SpellParticleLayer SmokeLayer { get; init; }
            public int ParticleCount { get; init; }
            public int OrbitOffset { get; init; }
            public int OrbitX { get; init; }
            public int OrbitX2 { get; init; }
            public int OrbitY { get; init; }
            public int OrbitAngle { get; init; }
            public float OrbitAngle2 { get; init; }
            public float OrbitSweep { get; init; }
            public int OrbitWidth { get; init; }
            public float OrbitAlpha { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int SpiritFigureOffset { get; init; }
            public int SpiritFigureWidth { get; init; }
            public int SpiritFigureHeight { get; init; }
            public SpellParticleShape SpiritFigureShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SpiritFigureColour { get; init; }
            public float SpiritFigureAlpha { get; init; }
            public float SpiritFigureAlpha2 { get; init; }
            public float SpiritFigureRotation { get; init; }
            public float SpiritFigureRotation2 { get; init; }
            public int SmokeCount { get; init; }
            public float Height { get; init; }
            public int HeightDivisor { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public int Y3 { get; init; }
            public int SmokeWidth2 { get; init; }
            public int SmokeWidth3 { get; init; }
            public int SmokeHeight2 { get; init; }
            public int SmokeHeight3 { get; init; }
            public SpellParticleShape SmokeShape2 { get; init; }
            public float SmokeAlpha2 { get; init; }
            public int OrbOffset { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public float OrbAlpha3 { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight2 { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
            public int SparksRise2 { get; init; }
        }

        public sealed class PacketDurationNode
        {
            public int ContactDurationMs { get; init; }
            public int WardDurationMs { get; init; }
            public int BurnDurationMs { get; init; }
            public int IceThrustDurationMs { get; init; }
            public int DefaultDurationMs { get; init; }
        }

        public sealed class PaletteNode
        {
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Gold { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Fire { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Ember { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Hot { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Blue { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int PaleBlue { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int White { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int Ice { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int IceWhite { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int StormColoursShadow { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int StormColoursGlow { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int StormColoursFilament { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int DrakeColoursShadow { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int DrakeColoursGlow { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int DrakeColoursFilament { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int DrakeColoursCore { get; init; }
        }

        public sealed class RepelNode
        {
            public int Centre { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Radius3 { get; init; }
            public float OrbWidth { get; init; }
            public float OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public float OrbitY { get; init; }
            public int OrbitWidth { get; init; }
            public float OrbitAlpha { get; init; }
            public int OrbitX { get; init; }
            public float OrbitY2 { get; init; }
            public float OrbitAngle { get; init; }
            public int OrbitWidth2 { get; init; }
            public float OrbitAlpha2 { get; init; }
            public float WispWidth { get; init; }
            public float WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public int FlameCount { get; init; }
            public int AngleDivisor { get; init; }
            public float Angle { get; init; }
            public float EllipseX { get; init; }
            public int EllipseX2 { get; init; }
            public float EllipseY { get; init; }
            public int FlameWidth { get; init; }
            public int FlameWidth2 { get; init; }
            public int FlameHeight { get; init; }
            public SpellParticleShape FlameShape { get; init; }
            public float FlameAlpha { get; init; }
            public int SparksCount { get; init; }
            public float SparksRy { get; init; }
        }

        public sealed class SealNode
        {
            public float SigilDiscWidth { get; init; }
            public float SigilDiscHeight { get; init; }
            public SpellParticleShape SigilDiscShape { get; init; }
            public bool SigilDiscLight { get; init; }
            public float HaloWidth { get; init; }
            public float HaloHeight { get; init; }
            public SpellParticleShape HaloShape { get; init; }
            public float HaloAlpha { get; init; }
            public float Inward { get; init; }
            public float Inward2 { get; init; }
            public float EllipseAngle { get; init; }
            public float Fork { get; init; }
            public float LineWidth { get; init; }
            public float LineWidth2 { get; init; }
            public float LineAlpha { get; init; }
            public float LineWidth3 { get; init; }
            public float LineAlpha2 { get; init; }
        }

        public sealed class SearingImpactNode
        {
            public float Open { get; init; }
            public float Open2 { get; init; }
            public float Open3 { get; init; }
            public float Heat { get; init; }
            public float HeatDivisor { get; init; }
            public int UserFlameCount { get; init; }
            public float Age { get; init; }
            public float Age2 { get; init; }
            public float Spread { get; init; }
            public int Spread2 { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public float Y { get; init; }
            public int Y2 { get; init; }
            public int Y3 { get; init; }
            public int Y4 { get; init; }
            public int UserFlameWidth { get; init; }
            public int UserFlameWidth2 { get; init; }
            public int UserFlameHeight { get; init; }
            public int UserFlameHeight2 { get; init; }
            public SpellParticleShape UserFlameShape { get; init; }
            public float UserFlameAlpha { get; init; }
            public SpellParticleLayer UserFlameLayer { get; init; }
            public SpellParticleLayer UserFlameLayer2 { get; init; }
            public float UserFlameRotation { get; init; }
            public float UserFlameRotation2 { get; init; }
            public int UserEmberOffset { get; init; }
            public int UserEmberWidth { get; init; }
            public int UserEmberWidth2 { get; init; }
            public int UserEmberHeight { get; init; }
            public int UserEmberHeight2 { get; init; }
            public SpellParticleShape UserEmberShape { get; init; }
            public float UserEmberAlpha { get; init; }
            public SpellParticleLayer UserEmberLayer { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceOffset2 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
        }

        public sealed class ShieldNode
        {
            public float Unfold { get; init; }
            public float Shimmer { get; init; }
            public float Shimmer2 { get; init; }
            public int ShimmerDivisor { get; init; }
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public int Width { get; init; }
            public float Width2 { get; init; }
            public float Width3 { get; init; }
            public int Width4 { get; init; }
            public int Height { get; init; }
            public float Height2 { get; init; }
            public float Height3 { get; init; }
            public int Height4 { get; init; }
            public SpellParticleShape WardBackShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WardBackColour { get; init; }
            public float WardBackAlpha { get; init; }
            public SpellParticleLayer WardBackLayer { get; init; }
            public SpellParticleShape WardFrontShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WardFrontColour { get; init; }
            public bool WardFrontLight { get; init; }
            public int WispOffset { get; init; }
            public float WispWidth { get; init; }
            public float WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public float WispAlpha2 { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public bool WispLight { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public float WispWidth2 { get; init; }
            public float WispHeight2 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            public float WispAlpha3 { get; init; }
            public SpellParticleLayer WispLayer2 { get; init; }
            public int X2 { get; init; }
            public int Y2 { get; init; }
            public int FlashWidth { get; init; }
            public int FlashWidth2 { get; init; }
            public int FlashHeight { get; init; }
            public int FlashHeight2 { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public float FlashAlpha2 { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public float FlashRotation { get; init; }
            public int WispOffset2 { get; init; }
            public float WispWidth3 { get; init; }
            public int WispHeight3 { get; init; }
            public SpellParticleShape WispShape3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour3 { get; init; }
            public float WispAlpha4 { get; init; }
            public SpellParticleLayer WispLayer3 { get; init; }
            public int SparkCount { get; init; }
            public float OrbitDivisor { get; init; }
            public int OrbitDivisor2 { get; init; }
            public float EllipseX { get; init; }
            public float EllipseY { get; init; }
            public float SparkWidth { get; init; }
            public float SparkHeight { get; init; }
            public SpellParticleShape SparkShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SparkColour { get; init; }
            public float SparkAlpha { get; init; }
            public int X3 { get; init; }
            public int Y3 { get; init; }
            public float WardFrontWidth { get; init; }
            public float WardFrontHeight { get; init; }
            public SpellParticleShape WardFrontShape2 { get; init; }
            public float WardFrontAlpha { get; init; }
            public SpellParticleLayer WardFrontLayer { get; init; }
            public int X4 { get; init; }
            public float WardFrontRotation { get; init; }
            public int OrbWidth { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight { get; init; }
            public int OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class SnowContactNode
        {
            public float Lift { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public int Y3 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceWidth2 { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public int SmokeSequenceHeight2 { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public float SmokeSequenceAlpha2 { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public SpellParticleLayer SmokeSequenceLayer2 { get; init; }
            public float SmokeSequenceRotation { get; init; }
            public bool SmokeSequenceLight { get; init; }
            public int OrbCount { get; init; }
            public int OrbCount2 { get; init; }
            public int OrbRx { get; init; }
            public int OrbRy { get; init; }
            public int OrbRise { get; init; }
            public SpellParticleShape OrbShape { get; init; }
        }

        public sealed class SnowballWakeNode
        {
            public float PhaseDivisor { get; init; }
            public int OrbCount { get; init; }
            public int Point { get; init; }
            public int Point2 { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public int Size { get; init; }
            public int Size2 { get; init; }
            public float OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public int OrbCount2 { get; init; }
            public int Point3 { get; init; }
            public int Point4 { get; init; }
            public int Point5 { get; init; }
            public int Point6 { get; init; }
            public float Drift { get; init; }
            public int Drift2 { get; init; }
            public int Drift3 { get; init; }
            public int Drift4 { get; init; }
            public int Val2 { get; init; }
            public int Val22 { get; init; }
            public int OrbSize { get; init; }
            public int OrbSize2 { get; init; }
            public float Opacity { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public float OrbAspect { get; init; }
            public float OrbGrowth { get; init; }
            public SpellParticleLayer OrbLayer2 { get; init; }
            public bool OrbCastsLight { get; init; }
            public int OrbDepthY { get; init; }
        }

        public sealed class StormArrivalNode
        {
            public int Centre { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Radius3 { get; init; }
            public int UserLightningWidth { get; init; }
            public int UserLightningHeight { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public SpellParticleLayer UserLightningLayer { get; init; }
            public float UserLightningRotation { get; init; }
            public bool UserLightningLight { get; init; }
            public int UserLightningWidth2 { get; init; }
            public int UserLightningHeight2 { get; init; }
            public SpellParticleShape UserLightningShape2 { get; init; }
            public float UserLightningAlpha2 { get; init; }
            public SpellParticleLayer UserLightningLayer2 { get; init; }
            public float UserLightningRotation2 { get; init; }
            public float UserLightningRotation3 { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int ParticleCount { get; init; }
            public float Angle { get; init; }
            public int AngleDivisor { get; init; }
            public float Angle2 { get; init; }
            public float EllipseY { get; init; }
            public int Spread { get; init; }
            public float Width { get; init; }
            public float Alpha { get; init; }
            public int Offset { get; init; }
        }

        public sealed class StormEscapeNode
        {
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Radius3 { get; init; }
            public int Centre { get; init; }
            public int UserLightningCount { get; init; }
            public int AngleDivisor { get; init; }
            public float Angle { get; init; }
            public float EllipseY { get; init; }
            public float EllipseAngle { get; init; }
            public float EllipseY2 { get; init; }
            public float Layer { get; init; }
            public int Spread { get; init; }
            public float Width { get; init; }
            public float Alpha { get; init; }
            public int Offset { get; init; }
            public int UserLightningWidth { get; init; }
            public int UserLightningHeight { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public float EllipseAngle2 { get; init; }
            public int EllipseX { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY3 { get; init; }
            public int Spread2 { get; init; }
            public float Width2 { get; init; }
            public float Alpha2 { get; init; }
            public int Offset2 { get; init; }
            public int Offset3 { get; init; }
            public int SparksCount { get; init; }
            public float SparksRx { get; init; }
            public float SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class TeleportLightNode
        {
            public float Spread { get; init; }
            public float Spread2 { get; init; }
            public float Spread3 { get; init; }
            public float Spread4 { get; init; }
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public float Pulse { get; init; }
            public float Pulse2 { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight2 { get; init; }
            public int OrbWidth3 { get; init; }
            public int OrbHeight3 { get; init; }
            public SpellParticleShape OrbShape3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public SpellParticleLayer OrbLayer2 { get; init; }
            public bool OrbLight3 { get; init; }
            public int ParticleCount { get; init; }
            public float X { get; init; }
            public int X2 { get; init; }
            public int Depth { get; init; }
            public int Depth2 { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public int LineWidth { get; init; }
            public int LineWidth2 { get; init; }
            public float LineAlpha { get; init; }
            public SpellParticleLayer LineLayer { get; init; }
            public SpellParticleLayer LineLayer2 { get; init; }
        }

        public sealed class ThunderStormNode
        {
            public int Radius { get; init; }
            public float Radius2 { get; init; }
            public float Radius3 { get; init; }
            public float Radius4 { get; init; }
            public int ParticleCount { get; init; }
            public int AngleDivisor { get; init; }
            public float Angle { get; init; }
            public float Source { get; init; }
            public float End { get; init; }
            public float End2 { get; init; }
            public int Spread { get; init; }
            public float Width { get; init; }
            public float Alpha { get; init; }
            public int Forks { get; init; }
            public int Offset { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public float Root { get; init; }
            public float Root2 { get; init; }
            public float EllipseAngle { get; init; }
            public float EllipseX { get; init; }
            public float EllipseY { get; init; }
            public int Flare { get; init; }
            public int Flare2 { get; init; }
            public int Spread2 { get; init; }
            public float Width2 { get; init; }
            public float Alpha2 { get; init; }
            public int Offset2 { get; init; }
            public int Offset3 { get; init; }
            public int Body { get; init; }
            public int UserLightningWidth { get; init; }
            public int UserLightningHeight { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public SpellParticleLayer UserLightningLayer { get; init; }
            public float UserLightningRotation { get; init; }
            public bool UserLightningLight { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int UserLightningWidth2 { get; init; }
            public int UserLightningHeight2 { get; init; }
            public SpellParticleShape UserLightningShape2 { get; init; }
            public float UserLightningAlpha2 { get; init; }
            public float UserLightningAlpha3 { get; init; }
            public SpellParticleLayer UserLightningLayer2 { get; init; }
            public int SparksOffset { get; init; }
            public int SparksCount { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class ThunderStrikeNode
        {
            public int Bottom { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int Spread { get; init; }
            public float Width { get; init; }
            public float Alpha { get; init; }
            public int Forks { get; init; }
            public int Offset { get; init; }
            public int UserLightningOffset { get; init; }
            public int UserLightningWidth { get; init; }
            public int UserLightningHeight { get; init; }
            public SpellParticleShape UserLightningShape { get; init; }
            public float UserLightningAlpha { get; init; }
            public SpellParticleLayer UserLightningLayer { get; init; }
            public bool UserLightningLight { get; init; }
            public int WispOffset { get; init; }
            public int WispWidth { get; init; }
            public int WispHeight { get; init; }
            public SpellParticleShape WispShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour { get; init; }
            public float WispAlpha { get; init; }
            public SpellParticleLayer WispLayer { get; init; }
            public float WispRotation { get; init; }
            public float Power { get; init; }
            public float Power2 { get; init; }
            public int X2 { get; init; }
            public int Y2 { get; init; }
            public float Peak { get; init; }
            public float Peak2 { get; init; }
            public int Source { get; init; }
            public int Spread2 { get; init; }
            public float Width2 { get; init; }
            public int Forks2 { get; init; }
            public int Offset2 { get; init; }
            public int X3 { get; init; }
            public int Y3 { get; init; }
            public int X4 { get; init; }
            public int Spread3 { get; init; }
            public float Width3 { get; init; }
            public float Alpha2 { get; init; }
            public float Time { get; init; }
            public int Forks3 { get; init; }
            public int Offset3 { get; init; }
            public int X5 { get; init; }
            public int Y4 { get; init; }
            public int X6 { get; init; }
            public int Spread4 { get; init; }
            public float Width4 { get; init; }
            public float Alpha3 { get; init; }
            public int Forks4 { get; init; }
            public int Offset4 { get; init; }
            public int IonCoronaCount { get; init; }
            public int Height { get; init; }
            public int Height2 { get; init; }
            public float X7 { get; init; }
            public int X8 { get; init; }
            public int IonCoronaWidth { get; init; }
            public int IonCoronaWidth2 { get; init; }
            public int IonCoronaHeight { get; init; }
            public int IonCoronaHeight2 { get; init; }
            public SpellParticleShape IonCoronaShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int IonCoronaColour { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int IonCoronaColour2 { get; init; }
            public float IonCoronaAlpha { get; init; }
            public float IonCoronaAlpha2 { get; init; }
            public SpellParticleLayer IonCoronaLayer { get; init; }
            public float IonCoronaRotation { get; init; }
            public float IonCoronaRotation2 { get; init; }
            public int X9 { get; init; }
            public int X10 { get; init; }
            public int UserLightningWidth2 { get; init; }
            public int UserLightningWidth3 { get; init; }
            public int UserLightningHeight2 { get; init; }
            public SpellParticleShape UserLightningShape2 { get; init; }
            public float UserLightningAlpha2 { get; init; }
            public float UserLightningAlpha3 { get; init; }
            public SpellParticleLayer UserLightningLayer2 { get; init; }
            public float UserLightningRotation { get; init; }
            public float UserLightningRotation2 { get; init; }
            public int UserLightningOffset2 { get; init; }
            public int UserLightningWidth4 { get; init; }
            public int UserLightningHeight3 { get; init; }
            public SpellParticleShape UserLightningShape3 { get; init; }
            public float UserLightningAlpha4 { get; init; }
            public SpellParticleLayer UserLightningLayer3 { get; init; }
            public bool UserLightningLight2 { get; init; }
            public int FlashOffset { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int UserLightningOffset3 { get; init; }
            public int UserLightningWidth5 { get; init; }
            public int UserLightningHeight4 { get; init; }
            public SpellParticleShape UserLightningShape4 { get; init; }
            public float UserLightningAlpha5 { get; init; }
            public SpellParticleLayer UserLightningLayer4 { get; init; }
            public bool UserLightningLight3 { get; init; }
            public int WispCount { get; init; }
            public int AngleDivisor { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY { get; init; }
            public float Target { get; init; }
            public float Target2 { get; init; }
            public int Spread5 { get; init; }
            public float Width5 { get; init; }
            public float Alpha4 { get; init; }
            public int Offset5 { get; init; }
            public SpellParticleLayer Layer { get; init; }
            public float Amount { get; init; }
            public int WispWidth2 { get; init; }
            public int WispHeight2 { get; init; }
            public SpellParticleShape WispShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int WispColour2 { get; init; }
            public float WispAlpha2 { get; init; }
            public float WispAlpha3 { get; init; }
            public float Value { get; init; }
            public SpellParticleLayer WispLayer2 { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int SparksRise { get; init; }
        }

        public sealed class TurnUndeadNode
        {
            public int PrepareCentre { get; init; }
            public int ReleaseCentre { get; init; }
            public int Size { get; init; }
            public int Size2 { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            public float OrbAlpha { get; init; }
            public bool OrbLight { get; init; }
            public float GlyphWidth { get; init; }
            public float GlyphHeight { get; init; }
            public SpellParticleShape GlyphShape { get; init; }
            public float GlyphAlpha { get; init; }
            public float GlyphRotation { get; init; }
            public float HaloWidth { get; init; }
            public float HaloHeight { get; init; }
            public SpellParticleShape HaloShape { get; init; }
            public float HaloAlpha { get; init; }
            public int FlashWidth { get; init; }
            public int FlashHeight { get; init; }
            public SpellParticleShape FlashShape { get; init; }
            public float FlashAlpha { get; init; }
            public SpellParticleLayer FlashLayer { get; init; }
            public bool FlashLight { get; init; }
            public int OrbitX { get; init; }
            public int OrbitY { get; init; }
            public float OrbitAngle { get; init; }
            public float OrbitSweep { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbitColour { get; init; }
            public float OrbitWidth { get; init; }
            public float OrbitAlpha { get; init; }
            public int LineStart { get; init; }
            public int LineEnd { get; init; }
            public float LineWidth { get; init; }
            public float LineAlpha { get; init; }
            public SpellParticleLayer LineLayer { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            public int Count { get; init; }
            public int Radius { get; init; }
            public int Radius2 { get; init; }
            public float Angle { get; init; }
            public float EllipseY { get; init; }
            public float Point { get; init; }
            public int Point2 { get; init; }
            public int Width { get; init; }
            public int Width2 { get; init; }
            public float OrbWidth { get; init; }
            public float OrbHeight { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public bool OrbLight2 { get; init; }
            public SpellParticleShape OrbShape3 { get; init; }
            public float OrbAlpha3 { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public float FlashWidth2 { get; init; }
            public float FlashHeight2 { get; init; }
            public SpellParticleShape FlashShape2 { get; init; }
            public float FlashAlpha2 { get; init; }
            public SpellParticleLayer FlashLayer2 { get; init; }
            public float EllipseAngle { get; init; }
            public int EllipseX { get; init; }
            public int EllipseY2 { get; init; }
            public int LineWidth2 { get; init; }
            public float LineAlpha2 { get; init; }
            public int SparksCount2 { get; init; }
            public int SparksRx2 { get; init; }
            public int SparksRy2 { get; init; }
            public int SmokeSequenceOffset { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
        }

        public sealed class VampirismNode
        {
            public int Centre { get; init; }
            public int Centre2 { get; init; }
            public int OrbOffset { get; init; }
            public int OrbWidth { get; init; }
            public int OrbHeight { get; init; }
            public SpellParticleShape OrbShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour { get; init; }
            public float OrbAlpha { get; init; }
            public SpellParticleLayer OrbLayer { get; init; }
            public bool OrbLight { get; init; }
            public int ParticleCount { get; init; }
            public int AngleDivisor { get; init; }
            public int Angle { get; init; }
            public int EllipseX { get; init; }
            public int EllipseX2 { get; init; }
            public int EllipseY { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int Y2 { get; init; }
            public int LineWidth { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int LineColour { get; init; }
            public float LineAlpha { get; init; }
            public int OrbOffset2 { get; init; }
            public int OrbWidth2 { get; init; }
            public int OrbHeight2 { get; init; }
            public SpellParticleShape OrbShape2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int OrbColour2 { get; init; }
            public float OrbAlpha2 { get; init; }
            public SpellParticleLayer OrbLayer2 { get; init; }
            public bool OrbLight2 { get; init; }
            public int FlameOffset { get; init; }
            public int FlameWidth { get; init; }
            public int FlameHeight { get; init; }
            public SpellParticleShape FlameShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlameColour { get; init; }
            public float FlameAlpha { get; init; }
            public float FlameRotation { get; init; }
            public bool FlameLight { get; init; }
            public float Spin { get; init; }
            public float Spin2 { get; init; }
            public int SmokeSequenceWidth { get; init; }
            public int SmokeSequenceHeight { get; init; }
            public SpellParticleShape SmokeSequenceShape { get; init; }
            public float SmokeSequencePhase { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SmokeSequenceColour { get; init; }
            public float SmokeSequenceAlpha { get; init; }
            public SpellParticleLayer SmokeSequenceLayer { get; init; }
            public int FlameTongueCount { get; init; }
            public int AngleDivisor2 { get; init; }
            public int EllipseX3 { get; init; }
            public int EllipseX4 { get; init; }
            public int EllipseY2 { get; init; }
            public int EllipseY3 { get; init; }
            public float EllipseAngle { get; init; }
            public int EllipseX5 { get; init; }
            public int EllipseY4 { get; init; }
            public int FlameTongueWidth { get; init; }
            public int FlameTongueHeight { get; init; }
            public SpellParticleShape FlameTongueShape { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int FlameTongueColour { get; init; }
            public float FlameTongueAlpha { get; init; }
            public float FlameTongueRotation { get; init; }
            public bool FlameTongueLight { get; init; }
            public int LineWidth2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int LineColour2 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int LineColour3 { get; init; }
            public float LineAlpha2 { get; init; }
            public float LineStart { get; init; }
            public float EllipseAngle2 { get; init; }
            public int EllipseX6 { get; init; }
            public int EllipseY5 { get; init; }
            public float LineWidth3 { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int LineColour4 { get; init; }
            public float LineAlpha3 { get; init; }
            public int SparksCount { get; init; }
            public int SparksRx { get; init; }
            public int SparksRy { get; init; }
            [JsonConverter(typeof(VfxRgbConverter))]
            public int SparksColour { get; init; }
            public int SparksRise { get; init; }
        }
    }
}
