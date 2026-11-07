using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Server;
using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirNetwork;
using Server.MirObjects;

static void Check(bool ok, string message)
{
    if (!ok) throw new InvalidOperationException(message);
}

var originalDirectory = Directory.GetCurrentDirectory();
var databaseDirectory = Directory.CreateTempSubdirectory("crystal-spec-tests-");
Directory.SetCurrentDirectory(databaseDirectory.FullName);
try
{
    Check(Envir.Edit.LoadDB(), "Create and read a versioned world database");
    var tuned = Envir.Edit.MagicInfoList.Single(m => m.Spell == Spell.SteelCut);
    tuned.PowerBase = 13;
    Envir.Edit.SaveDB();
    Check(Envir.Main.LoadDB(), "Read the editor's saved definitions on the server");
    Envir.Main.BuffInfoList.AddRange(BuffInfo.Load());
    foreach (var ability in SpecialisationAbilities.All)
    {
        var rows = Envir.Main.MagicInfoList.Where(m => m.Spell == ability.Spell).ToArray();
        Check(rows.Length == 1 && rows[0].Level1 > ability.UnlockLevel, $"Seed one definition with separate rank gates for {ability.Name}");
        Check(rows[0].BaseCost == ability.Mana && rows[0].DelayBase == ability.Cooldown,
            $"Keep cost and cooldown synchronized for {ability.Name}");
    }
    Check(Envir.Main.MagicInfoList.Single(m => m.Spell == Spell.SteelCut).PowerBase == 13,
        "Loading defaults again must preserve an edited power value");

    foreach (var ability in SpecialisationAbilities.All)
        ExerciseSpell(ability);
    foreach (var ability in SpecialisationAbilities.All.Where(a => SpellCombos.For(a.Spell) != null))
        ExerciseSpell(ability, exerciseCombo: true);
}
finally
{
    Directory.SetCurrentDirectory(originalDirectory);
    databaseDirectory.Delete(recursive: true);
}

Console.WriteLine("All 18 spell dispatches, nine original-spell combo rotations and the Magic database round-trip passed.");

static void ExerciseSpell(SpecialisationAbility ability, bool exerciseCombo = false)
{
    const int size = 12;
    var map = new Map(new MapInfo()) { Width = size, Height = size, Cells = new Cell[size, size], DoorIndex = new Door[size, size] };
    for (int x = 0; x < size; x++)
    for (int y = 0; y < size; y++)
        map.Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };

    // A real connection uses an ephemeral loopback socket. No external server is contacted.
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    using var client = new TcpClient();
    client.Connect((IPEndPoint)listener.LocalEndpoint);
    using var server = listener.AcceptTcpClient();
    var caster = new PlayerObject
    {
        Info = new CharacterInfo
        {
            Name = "Combat test", Class = ability.Class, Specialisation = ability.Spec, Level = 45, MP = 150, HP = 200
        },
        Stats = new Stats
        {
            [Stat.HP] = 500, [Stat.MP] = 150, [Stat.SkillGainMultiplier] = 1, [Stat.Accuracy] = 100,
            [Stat.MinDC] = 45, [Stat.MaxDC] = 45, [Stat.MinMC] = 45, [Stat.MaxMC] = 45, [Stat.MinSC] = 45, [Stat.MaxSC] = 45
        },
        Connection = new MirConnection(1, server),
        AMode = AttackMode.Peace,
        ArmourRate = 1,
        DamageRate = 1,
        CurrentLocation = new Point(4, 4)
    };
    caster.CurrentMap = map;
    caster.Node = new LinkedListNode<MapObject>(caster);
    caster.Info.Mount = new MountInfo(caster);
    map.AddObject(caster);
    var magic = new UserMagic(ability.Spell);
    caster.Info.Magics.Add(magic);

    var enemy = MakeMonster("Combat test enemy", map, new Point(5, 4));
    var chain = new List<MonsterObject>();
    if (ability.Spell == Spell.StormRelay)
        for (int i = 0; i < 4; i++)
            chain.Add(MakeMonster("Relay target", map, new Point(6 + i, 4)));
    if (ability.Spell == Spell.Distil)
        enemy.PoisonList.Add(new Poison { Owner = caster, PType = PoisonType.Green, Value = 6, Duration = 4, TickSpeed = 1000 });

    MonsterObject pet = null;
    if (ability.Target == AbilityTarget.Companion)
    {
        pet = MakeMonster(ability.Spell == Spell.SpiritCommand ? Settings.SkeletonName : Settings.VampireName,
            map, new Point(4, 5));
        pet.Master = caster;
        pet.HP -= 100;
        caster.Pets.Add(pet);
    }

    uint target = ability.Target is AbilityTarget.Enemy or AbilityTarget.Companion ? enemy.ObjectID : 0;
    if (ability.Spec != CombatSpecialisation.Unchosen)
    {
        caster.Info.Specialisation = CombatSpecialisation.Unchosen;
        caster.Magic(ability.Spell, MirDirection.Right, target, enemy.CurrentLocation);
        Check(caster.MP == 150 && caster.ActionList.Count == 0, $"{ability.Name} must require the selected specialisation");
        caster.Info.Specialisation = ability.Spec;
    }

    if (ability.Target == AbilityTarget.Enemy)
    {
        caster.Magic(ability.Spell, MirDirection.Right, 0, enemy.CurrentLocation);
        Check(caster.MP == 150 && caster.ActionList.Count == 0, $"{ability.Name} cannot spend mana without a valid target");
    }

    if (exerciseCombo)
    {
        PrimeCombo(ability.Spell, caster, enemy, map);
        SpellComboProgress before = ReadCombo(caster);
        uint key = SpellCombos.For(ability.Spell).TargetBound ? enemy.ObjectID : 0;
        Check(before.Count(Envir.Main.Time, key) == 1, $"{ability.Name} must get one confirmed combo stack");
        if (key != 0)
            Check(before.Count(Envir.Main.Time, key + 1) == 0, $"{ability.Name} cannot spend another target's mark");
    }

    int health = enemy.HP;
    caster.Magic(ability.Spell, MirDirection.Right, target, enemy.CurrentLocation);
    Check(caster.MP == 150 - magic.Info.BaseCost && caster.ActionList.Count == 1,
        $"{ability.Name} must schedule one paid cast");
    Check(caster.ActionList[0].Time - caster.ActionList[0].StartTime == ability.Windup && enemy.HP == health,
        $"{ability.Name} must wait for the authored wind-up");

    // Dispatch queued effects with the same concrete actor handler used by the server loop.
    int resolved = 0;
    if (ability.Spell == Spell.CinderMine)
    {
        map.GetCell(enemy.CurrentLocation).Remove(enemy);
        var arm = caster.ActionList[0];
        caster.ActionList.RemoveAt(0);
        caster.Process(arm);
        resolved++;
        Check(caster.ActionList.Count == 1 && enemy.HP == health, "An empty mine should arm and wait");
        map.GetCell(enemy.CurrentLocation).Add(enemy);
    }
    while (caster.ActionList.Count > 0 && resolved < 16)
    {
        var action = caster.ActionList[0];
        caster.ActionList.RemoveAt(0);
        caster.Process(action);
        resolved++;
    }

    Check(caster.ActionList.Count == 0, $"{ability.Name} must have a finite resolution");
    if (ability.Target == AbilityTarget.Companion)
        Check(pet.HP > pet.MaxHealth - 100 && pet.Target == enemy, $"{ability.Name} must heal and direct the owned companion");
    else if (ability.Target == AbilityTarget.Ally)
    {
        if (ability.Spell == Spell.LanternLink)
            Check(caster.HP > 200, "Lantern Link must heal missing health");
        int before = caster.HP;
        int remaining = caster.Attacked(enemy, 10, DefenceType.None);
        Check(remaining == 0 && caster.HP == before, $"{ability.Name} must absorb an actual incoming hit");
    }
    else
        Check(enemy.HP < health, $"{ability.Name} must damage a legal enemy");

    if (ability.Spell == Spell.AshSeal) Check(resolved == 3 && magic.Experience is > 0 and <= 3, "Ash Seal pulses three times and trains only once");
    if (ability.Spell == Spell.CinderMine) Check(resolved == 2, "Cinder Mine detonates once after a target enters");
    if (ability.Spell == Spell.Distil) Check(enemy.PoisonList.Count == 0, "Distil must consume its owner's poison");
    if (ability.Spell == Spell.StormRelay)
        Check(resolved == 4 && chain.Take(3).All(t => t.HP < t.MaxHealth) &&
            chain[3].HP == chain[3].MaxHealth && magic.Experience is > 0 and <= 3,
            "Storm Relay must hit at most four distinct targets and train once");
    if (exerciseCombo)
        Check(ReadCombo(caster).Count(Envir.Main.Time, enemy.ObjectID) == 0,
            $"{ability.Name} must consume its eligible combo on resolution");

    if (ability.Target == AbilityTarget.Companion && !exerciseCombo)
    {
        // Regression: selecting an owned companion used to fail an enemy-only target check.
        // A selected pet must receive the mend, without recalling it or changing focus.
        string petName = ability.Spell == Spell.SpiritCommand ? Settings.ShinsuName : Settings.ToadName;
        var selectedPet = MakeMonster(petName, map, new Point(4, 6));
        selectedPet.Master = caster;
        selectedPet.HP -= 150;
        caster.Pets.Add(selectedPet);
        int originalPetHealth = pet.HP;
        MapObject originalFocus = pet.Target;
        Point selectedLocation = selectedPet.CurrentLocation;
        magic.CastTime = 0;
        caster.Magic(ability.Spell, MirDirection.Right, selectedPet.ObjectID, selectedLocation);
        Check(caster.ActionList.Count == 1, $"{ability.Name} must accept an explicitly selected owned pet");
        var selectedCast = caster.ActionList[0];
        caster.ActionList.RemoveAt(0);
        caster.Process(selectedCast);
        Check(selectedPet.HP > selectedPet.MaxHealth - 150, $"{ability.Name} must mend the selected pet");
        Check(pet.HP == originalPetHealth && pet.Target == originalFocus,
            $"{ability.Name} must leave the other companion untouched");
        Check(selectedPet.Target == null && selectedPet.CurrentLocation == selectedLocation,
            $"{ability.Name} must not turn a direct mend into a focus or recall order");

        // A cosmetic cast on a healthy pet cannot be farmed for proficiency.
        selectedPet.HP = selectedPet.MaxHealth;
        magic.CastTime = 0;
        int priorExperience = magic.Experience;
        caster.Magic(ability.Spell, MirDirection.Right, selectedPet.ObjectID, selectedLocation);
        Check(caster.ActionList.Count == 1, $"{ability.Name} may target a full-health owned pet");
        var emptyMend = caster.ActionList[0];
        caster.ActionList.RemoveAt(0);
        caster.Process(emptyMend);
        Check(magic.Experience == priorExperience,
            $"{ability.Name} must not gain proficiency from a direct zero-heal mend");
    }
}

static SpellComboProgress ReadCombo(HumanObject caster) => (SpellComboProgress)typeof(HumanObject)
    .GetField("_combo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(caster)!;

static void PrimeCombo(Spell spender, PlayerObject caster, MonsterObject enemy, Map map)
{
    long now = Envir.Main.Time;
    switch (spender)
    {
        case Spell.Notchbreaker:
        case Spell.RedWake:
            Spell attack = spender == Spell.Notchbreaker ? Spell.FlamingSword : Spell.HalfMoon;
            caster.Process(new DelayedAction(DelayedType.Damage, now, enemy, 20, DefenceType.AC, false,
                new UserMagic(attack)));
            if (spender == Spell.RedWake)
            {
                var secondary = MakeMonster("Cleave target", map, new Point(4, 5));
                caster.Process(new DelayedAction(DelayedType.Damage, now, secondary, 20, DefenceType.AC, false,
                    new UserMagic(attack)));
            }
            break;
        case Spell.Interpose:
            caster.Stats[Stat.MinAC] = caster.Stats[Stat.MaxAC] = 12;
            caster.AddBuff(BuffType.ProtectionField, caster, 5000, new Stats(), refreshStats: false);
            caster.Attacked(enemy, 10, DefenceType.AC);
            break;
        case Spell.LanternLink:
            var heal = new DelayedAction(DelayedType.Magic, now, new UserMagic(Spell.Healing), 50, caster);
            var regen = typeof(HumanObject).GetMethod("ProcessRegen", BindingFlags.Instance | BindingFlags.NonPublic)!;
            caster.Process(heal);
            Check(ReadCombo(caster).Count(now) == 0, "Queueing a heal does not award Healing Reserve");
            caster.HP = caster.MaxHealth;
            caster.HealTime = now - 1;
            regen.Invoke(caster, null);
            Check(ReadCombo(caster).Count(now) == 0, "Overhealing does not award Healing Reserve");
            caster.HP = 200;
            caster.Process(heal);
            caster.HealTime = now - 1;
            regen.Invoke(caster, null);
            break;
        case Spell.Distil:
            var cloud = new SpellObject { Spell = Spell.PoisonCloud, Caster = caster, CurrentMap = map, TickSpeed = 1000, Value = 10 };
            cloud.ProcessSpell(enemy);
            break;
        default:
            Spell builder = spender switch
            {
                Spell.AshSeal => Spell.GreatFireBall,
                Spell.GlacialGate => Spell.FrostCrunch,
                Spell.StormRelay => Spell.ThunderBolt,
                Spell.SpiritCommand => Spell.SoulFireBall,
                _ => throw new InvalidOperationException()
            };
            var build = new DelayedAction(DelayedType.Magic, now, new UserMagic(builder), 20, enemy, enemy.CurrentLocation);
            caster.Process(build);
            if (spender == Spell.GlacialGate)
            {
                int health = enemy.HP;
                map.Process(new DelayedAction(DelayedType.Magic, now, caster, new UserMagic(Spell.IceThrust),
                    caster.CurrentLocation, MirDirection.Right, 20, 20));
                Check(health - enemy.HP == 25 && ReadCombo(caster).Count(now, enemy.ObjectID) == 0,
                    "Ice Thrust must shatter Rime once for 25% extra damage");
                caster.Process(build);
            }
            break;
    }
}

static MonsterObject MakeMonster(string name, Map map, Point location)
{
    var info = new MonsterInfo { Name = name, Level = 40 };
    info.Stats[Stat.HP] = 1000;
    MonsterObject monster = MonsterObject.GetMonster(info);
    monster.CurrentMap = map;
    monster.CurrentLocation = location;
    monster.Node = new LinkedListNode<MapObject>(monster);
    monster.Stats = new Stats(info.Stats);
    monster.ArmourRate = monster.DamageRate = 1;
    monster.SetHP(info.Stats[Stat.HP]);
    map.AddObject(monster);
    return monster;
}
