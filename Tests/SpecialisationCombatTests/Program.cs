using System.Drawing;
using System.Reflection;
using Server;
using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirObjects;
using C = ClientPackets;
using S = ServerPackets;

static void Check(bool ok, string message)
{
    if (!ok) throw new InvalidOperationException(message);
}

string original = Directory.GetCurrentDirectory();
string installed = args.Length == 1 ? Path.GetFullPath(args[0]) : null;
var scratch = Directory.CreateTempSubdirectory("crystal-spell-perk-tests-");
Directory.SetCurrentDirectory(scratch.FullName);
try
{
    CheckActorDatabaseVersions();
    Check(Envir.Main.LoadDB(), "Load default magic definitions");
    Envir.Main.BuffInfoList.AddRange(BuffInfo.Load());
    Check(!Envir.Main.MagicInfoList.Any(magic => SpellCurriculum.IsRetired(magic.Spell)), "New worlds seed legacy spells only");
    CheckAutoLearn();
    CheckWarriorPerks();
    CheckWizardPerks();
    CheckTaoistPerks();
    CheckRejectedDatabaseFilesArePreserved();
    if (installed != null) CheckInstalledDatabase(installed);
}
finally
{
    Directory.SetCurrentDirectory(original);
    if (scratch.FullName.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
        scratch.Delete(recursive: true);
}

Console.WriteLine("Spell auto-learning, perk combat, perk saves and database checks passed.");

static void CheckAutoLearn()
{
    foreach (MirClass actorClass in new[] { MirClass.Warrior, MirClass.Wizard, MirClass.Taoist })
    {
        var caster = new TestPlayer { Info = new CharacterInfo { Class = actorClass, Level = 1 } };
        caster.Learn();
        Check(caster.Info.Magics.Count == 1, $"Start {actorClass} with their first spell");
        caster.Info.Level = 100;
        caster.Learn();
        caster.Learn();
        Check(caster.Info.Magics.Select(m => m.Spell).SequenceEqual(SpellCurriculum.ForClass(actorClass)),
            $"{actorClass} auto-learns every native spell once");
    }

    // A previous class build may have stored its key on a spell replacement.
    Envir.Main.MagicInfoList.Add(new MagicInfo { Name = "Drake's Echo", Spell = Spell.DrakesEcho, Level1 = 30 });
    var migrated = new TestPlayer { Info = new CharacterInfo { Class = MirClass.Warrior, Level = 60, Specialisation = CombatSpecialisation.Second } };
    migrated.Info.Magics.Add(new UserMagic(Spell.DrakesEcho) { Key = 4 });
    migrated.Learn();
    Check(migrated.Info.Specialisation == CombatSpecialisation.Unchosen && migrated.GetMagic(Spell.DrakesEcho) == null &&
        migrated.Info.Magics.All(magic => !SpellCurriculum.IsRetired(magic.Spell)) && migrated.GetMagic(Spell.TwinDrakeBlade)?.Key == 4,
        "Convert replacement spell bindings when migrating a player");
    Envir.Main.MagicInfoList.RemoveAll(magic => magic.Spell == Spell.DrakesEcho);
}

static void CheckWarriorPerks()
{
    var (caster, map) = MakeCaster(MirClass.Warrior);
    var primary = MakeMonster("Primary", map, new Point(5, 4));
    var nearby = MakeMonster("Nearby", map, new Point(5, 5));
    var distant = MakeMonster("Distant", map, new Point(8, 8));
    SetTime(20000);
    caster.Apply([new(Spell.FlamingSword, SpellPerk.Splash, 2), new(Spell.FlamingSword, SpellPerk.Burn, 1),
        new(Spell.TwinDrakeBlade, SpellPerk.DrakeEcho, 1), new(Spell.ShoulderDash, SpellPerk.DashDistance, 1)]);
    Check(caster.Info.SpellPerks.Revision == 1 && caster.Sent.OfType<S.SpellPerkState>().Last().Message == "Spell perks applied.",
        "Server accepts the player-owned spell draft");
    caster.Apply([new(Spell.FlamingSword, SpellPerk.Burn, 1)]);
    Check(caster.Info.SpellPerks.Revision == 1, "Reject an invalid draft atomically");

    caster.SpellToggle(Spell.FlamingSword, SpellToggleState.True);
    Check(caster.FlamingSword, "Flaming Sword can arm through the legacy toggle");
    caster.Attack(MirDirection.Right, Spell.FlamingSword);
    Check(primary.HP == primary.MaxHealth && caster.ActionList.Any(action => action.Type == DelayedType.Damage), "Wait for the weapon hit");
    Drain(caster);
    Check(primary.HP < primary.MaxHealth && nearby.HP < nearby.MaxHealth && distant.HP == distant.MaxHealth,
        "Flaming Sword splash and delayed burn resolve against nearby targets");
    Check(caster.Sent.OfType<S.MapEffect>().Any(effect => effect.Effect == SpellEffect.SpellPerkBurn && effect.Location == nearby.CurrentLocation),
        "Burn sends its own particle event");

    int hp = primary.HP;
    caster.Process(new DelayedAction(DelayedType.Damage, Envir.Main.Time, primary, 20, DefenceType.AC, false, caster.GetMagic(Spell.TwinDrakeBlade), true));
    Drain(caster);
    Check(hp - primary.HP > 20, "The confirmed second drake hit schedules a third hit");

    caster.LogTime = Envir.Main.Time + 500;
    caster.Apply([]);
    Check(caster.Info.SpellPerks.Revision == 1 && caster.Sent.OfType<S.SpellPerkState>().Last().Message.Contains("combat"),
        "Reject perk changes in combat");
    SetTime(60000);
    caster.Magic(Spell.ShoulderDash, MirDirection.Up, 0, caster.CurrentLocation);
    Check(caster.Sent.OfType<S.UserDash>().Count() >= 1, "Shoulder Dash retains its movement packets");
}

static void CheckWizardPerks()
{
    var (caster, map) = MakeCaster(MirClass.Wizard);
    var target = MakeMonster("Strike target", map, new Point(6, 4));
    var neighbour = MakeMonster("Chain target", map, new Point(7, 4));
    SetTime(90000);
    caster.Apply([new(Spell.ThunderBolt, SpellPerk.Chain, 1), new(Spell.FireWall, SpellPerk.Radius, 1),
        new(Spell.FireWall, SpellPerk.Duration, 1), new(Spell.Teleport, SpellPerk.Economy, 1)]);
    caster.Magic(Spell.ThunderBolt, MirDirection.Right, target.ObjectID, target.CurrentLocation);
    Drain(caster);
    Check(target.HP < target.MaxHealth && neighbour.HP < neighbour.MaxHealth && caster.Sent.OfType<S.ObjectProjectile>().Any(p => p.Spell == Spell.ThunderBolt),
        "Thunder Bolt chains through the real cast and sends the arc endpoint IDs");
    var wall = caster.GetMagic(Spell.FireWall);
    map.Process(new DelayedAction(DelayedType.Magic, Envir.Main.Time, caster, wall, 20, new Point(8, 8)));
    Check(map.Spells.Count(s => s.Spell == Spell.FireWall) == 9 &&
        map.Spells.Where(s => s.Spell == Spell.FireWall).All(s => s.ExpireTime - Envir.Main.Time == 23000),
        "Fire Wall has its expanded cross and duration");
    var teleport = caster.GetMagic(Spell.Teleport);
    Check(teleport.ManaCost < teleport.Info.BaseCost + UserMagic.EffectiveRank * teleport.Info.LevelCost, "Efficiency changes real mana cost");
}

static void CheckTaoistPerks()
{
    var (caster, map) = MakeCaster(MirClass.Taoist);
    TestPlayer ally = AddCaster(MirClass.Warrior, map, new Point(5, 4));
    List<PlayerObject> group = [caster, ally];
    caster.GroupMembers = ally.GroupMembers = group;
    SetTime(120000);
    caster.Apply([new(Spell.Healing, SpellPerk.EchoHeal, 1), new(Spell.PoisonCloud, SpellPerk.Radius, 1),
        new(Spell.PoisonCloud, SpellPerk.Duration, 1)]);
    caster.Info.HP = caster.MaxHealth - 70;
    ally.Info.HP = ally.MaxHealth - 70;
    caster.Magic(Spell.Healing, MirDirection.Right, ally.ObjectID, ally.CurrentLocation);
    Drain(caster);
    Check(ally.HealAmount > caster.HealAmount && caster.HealAmount > 0,
        "Healing echoes at reduced strength to a second wounded ally");
    Check(caster.Sent.OfType<S.MapEffect>().Any(p => p.Value == (byte)Spell.Healing && p.Location == caster.CurrentLocation),
        "The echo has its own effect at the second recipient");
    var cloud = caster.GetMagic(Spell.PoisonCloud);
    map.Process(new DelayedAction(DelayedType.Magic, Envir.Main.Time, caster, cloud, 20, new Point(8, 8), (byte)0));
    var cells = map.Spells.Where(spell => spell.Spell == Spell.PoisonCloud).ToArray();
    Check(cells.Length == 25 && cells.All(spell => spell.ExpireTime - Envir.Main.Time == 6900 &&
        spell.GetInfo() is S.ObjectSpell art && art.Location == spell.CurrentLocation),
        "Poison Cloud sends particle fields for all expanded cells and extends their lifetime");
}

static (TestPlayer Caster, Map Map) MakeCaster(MirClass actorClass)
{
    const int size = 16;
    var map = new Map(new MapInfo()) { Width = size, Height = size, Cells = new Cell[size, size], DoorIndex = new Door[size, size] };
    for (int x = 0; x < size; x++)
    for (int y = 0; y < size; y++)
        map.Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
    return (AddCaster(actorClass, map, new Point(4, 4)), map);
}

static TestPlayer AddCaster(MirClass actorClass, Map map, Point location)
{
    var caster = new TestPlayer
    {
        Info = new CharacterInfo { Name = "Perk test", Level = 70, Class = actorClass, HP = 500, MP = 250 },
        Stats = new Stats { [Stat.HP] = 500, [Stat.MP] = 250, [Stat.MinDC] = 40, [Stat.MaxDC] = 40,
            [Stat.MinMC] = 40, [Stat.MaxMC] = 40, [Stat.Accuracy] = 100 },
        ArmourRate = 1, DamageRate = 1, AMode = AttackMode.Peace, CurrentLocation = location, CurrentMap = map
    };
    caster.Node = new LinkedListNode<MapObject>(caster);
    caster.Info.Mount = new MountInfo(caster);
    caster.Learn();
    map.AddObject(caster);
    return caster;
}

static void Drain(TestPlayer caster)
{
    int actions = 0;
    while (caster.ActionList.Count > 0)
    {
        Check(++actions < 20, "All perk hits finish in bounded time");
        var action = caster.ActionList.MinBy(entry => entry.Time);
        caster.ActionList.Remove(action);
        SetTime(Math.Max(Envir.Main.Time, action.Time));
        caster.Process(action);
    }
}

static void SetTime(long time) => typeof(Envir).GetProperty(nameof(Envir.Time))!.SetValue(Envir.Main, time);

static void CheckInstalledDatabase(string source)
{
    File.Copy(Path.Combine(source, "Server.MirDB"), Envir.DatabasePath, true);
    File.Copy(Path.Combine(source, "Server.MirADB"), Envir.AccountPath, true);
    int worldVersion = BitConverter.ToInt32(File.ReadAllBytes(Envir.DatabasePath));
    int accountVersion = BitConverter.ToInt32(File.ReadAllBytes(Envir.AccountPath));
    Check(Envir.Main.LoadDB() && Envir.Main.LoadAccounts(), "Open installed save copies");
    int mapCount = Envir.Main.MapInfoList.Count;
    var characters = Envir.Main.CharacterList.Select(character => (character.Index, character.Level, character.Class, character.Name)).ToArray();
    Envir.Main.SaveDB();
    Envir.Main.SaveAccounts();
    Check(BitConverter.ToInt32(File.ReadAllBytes(Envir.DatabasePath)) == Envir.Version &&
        BitConverter.ToInt32(File.ReadAllBytes(Envir.AccountPath)) == Envir.Version, "Write both installed save copies in the current format");
    Check(Envir.Main.LoadDB() && Envir.Main.LoadAccounts() && Envir.Main.MapInfoList.Count == mapCount &&
        Envir.Main.CharacterList.Select(character => (character.Index, character.Level, character.Class, character.Name)).SequenceEqual(characters),
        "Reload migrated copies with all map and character records");
    Console.WriteLine($"Migrated copies of world v{worldVersion} and accounts v{accountVersion} to v{Envir.Version} with {mapCount} maps and {characters.Length} characters.");
}

static void CheckRejectedDatabaseFilesArePreserved()
{
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    writer.Write(Envir.Version + 1);
    writer.Write(Envir.CustomVersion);
    byte[] unsupported = stream.ToArray();

    File.WriteAllBytes(Envir.DatabasePath, unsupported);
    File.WriteAllBytes(Envir.AccountPath, unsupported);
    Check(!Envir.Main.LoadDB(), "Reject a newer world database");
    Check(!Envir.Main.LoadAccounts(), "Reject a newer account database");
    Envir.Main.SaveDB();
    Envir.Main.SaveAccounts();
    Check(File.ReadAllBytes(Envir.DatabasePath).SequenceEqual(unsupported), "Preserve the rejected world database");
    Check(File.ReadAllBytes(Envir.AccountPath).SequenceEqual(unsupported), "Preserve the rejected account database");
}

static void CheckActorDatabaseVersions()
{
    CharacterInfo[] actors =
    [
        new CharacterInfo { Name = "Character test", CreationIP = string.Empty, Heroes = new HeroInfo[1] },
        new HeroInfo(new ClientPackets.NewHero { Name = "Hero test" })
    ];
    foreach (var actor in actors)
    {
        actor.Specialisation = CombatSpecialisation.Second;
        actor.Level = 45;
        var talent = new TalentPick(TalentCatalog.Tree(actor.Class).At(1, 0).Id, 1, 0);
        actor.Talents.Apply(actor.Specialisation, [talent], []);
        actor.Talents.SaveBindings(actor.Specialisation, [new TalentBinding(Spell.SteelCut, actor is HeroInfo ? (byte)17 : (byte)1)]);
        SpellPerkPick[] points = [new(Spell.FlamingSword, SpellPerk.Splash, 1), new(Spell.FlamingSword, SpellPerk.Burn, 2)];
        actor.SpellPerks.Apply(points);
        using var perkStream = new MemoryStream();
        using var perkWriter = new BinaryWriter(perkStream);
        actor.SpellPerks.Write(perkWriter);
        using var profileStream = new MemoryStream();
        using var profileWriter = new BinaryWriter(profileStream);
        actor.Talents.Write(profileWriter);
        foreach (var (version, customVersion, expected) in new[]
        {
            (118, 0, actor.Specialisation),
            (119, 1, CombatSpecialisation.Unchosen),
            (120, 1, actor.Specialisation),
            (121, 1, actor.Specialisation),
            (122, 1, actor.Specialisation)
        })
        {
            const int nextRecord = 0x12345678;
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            actor.Save(writer);
            if (version < 122) stream.Position -= perkStream.Length;
            if (version < 121) stream.Position -= profileStream.Length;
            // The legacy writer ended the actor record before the specialisation byte.
            if (version == 119) stream.Position--;
            writer.Write(nextRecord);
            stream.Position = 0;

            using var reader = new BinaryReader(stream);
            CharacterInfo loaded = actor is HeroInfo
                ? new HeroInfo(reader, version, customVersion)
                : new CharacterInfo(reader, version, customVersion);
            Check(loaded.Name == actor.Name && loaded.Specialisation == expected,
                $"Read {actor.Name} with the expected specialisation from schema {version}.{customVersion}");
            Check(loaded.Talents.Picks(CombatSpecialisation.Unchosen).Length == (version >= 121 ? 1 : 0), "Read versioned build without consuming neighbouring records");
            if (version >= 121) Check(loaded.Talents.Picks(CombatSpecialisation.Unchosen)[0] == talent &&
                loaded.Talents.Bindings(actor.Specialisation).SequenceEqual(actor.Talents.Bindings(actor.Specialisation)), "Preserve talent picks and bindings");
            Check(loaded.SpellPerks.Picks.SequenceEqual(version == 122 ? points : []), "Load versioned spell points and picks");
            Check(reader.ReadInt32() == nextRecord,
                $"Read exactly one {actor.Name} record from schema {version}.{customVersion}");
        }
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


sealed class TestPlayer : PlayerObject
{
    public readonly List<Packet> Sent = [];
    public override void Enqueue(Packet packet) => Sent.Add(packet);
    public override void Broadcast(Packet packet) { }
    public void Learn() => ReconcileCurriculumSpells(false);
    public void Apply(SpellPerkPick[] picks)
    {
        typeof(PlayerObject).GetMethod("ApplySpellPerkDraft", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(this,
            [new C.SpellPerkDraft { ObjectID = ObjectID, Revision = Info.SpellPerks.Revision, Apply = true, Picks = picks }]);
    }
}
