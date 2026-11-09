public sealed class TalentProfile
{
    private readonly TalentPick[][] _picks = [[], [], [], []];
    private readonly TalentBinding[][] _bindings = [[], [], [], []];
    private readonly bool[] _hasBindings = new bool[4];
    private TalentLoadout _active;

    public int Revision { get; private set; }
    public TalentPick[] Picks(CombatSpecialisation spec) => _picks[(int)spec];
    public TalentBinding[] Bindings(CombatSpecialisation spec) => _bindings[(int)spec];

    public TalentLoadout Active(MirClass @class, CombatSpecialisation spec)
    {
        if (_active == null || _active.Class != @class || _active.Spec != spec)
            _active = new TalentLoadout(@class, spec, _picks[0], Picks(spec));
        return _active;
    }

    public void Apply(CombatSpecialisation spec, TalentPick[] shared, TalentPick[] specialised)
    {
        _picks[0] = shared;
        if (spec != CombatSpecialisation.Unchosen) _picks[(int)spec] = specialised;
        Revision++;
        _active = null;
    }

    public bool HasBindings(CombatSpecialisation spec) => _hasBindings[(int)spec];

    public void SaveBindings(CombatSpecialisation spec, TalentBinding[] bindings)
    {
        _bindings[(int)spec] = bindings;
        _hasBindings[(int)spec] = true;
    }

    public TalentProfile Snapshot()
    {
        var copy = new TalentProfile { Revision = Revision };
        Array.Copy(_picks, copy._picks, _picks.Length);
        Array.Copy(_bindings, copy._bindings, _bindings.Length);
        Array.Copy(_hasBindings, copy._hasBindings, _hasBindings.Length);
        return copy;
    }

    public void ValidateSaved(MirClass @class, ushort level)
    {
        _active = null;
        for (int i = 0; i < _picks.Length; i++)
            if (!TalentCatalog.Validate(TalentCatalog.Tree(@class, (CombatSpecialisation)i), level, _picks[i], out _))
                _picks[i] = [];
    }

    public static TalentProfile Read(BinaryReader reader)
    {
        var profile = new TalentProfile { Revision = reader.ReadInt32() };
        if (profile.Revision < 0) throw new InvalidDataException("Invalid talent revision.");
        for (int i = 0; i < profile._picks.Length; i++) profile._picks[i] = ReadPicks(reader);
        for (int i = 0; i < profile._bindings.Length; i++)
        {
            profile._hasBindings[i] = reader.ReadBoolean();
            profile._bindings[i] = ReadBindings(reader);
        }
        return profile;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Revision);
        foreach (TalentPick[] picks in _picks) WritePicks(writer, picks);
        for (int i = 0; i < _bindings.Length; i++)
        {
            writer.Write(_hasBindings[i]);
            WriteBindings(writer, _bindings[i]);
        }
    }

    public static TalentPick[] ReadPicks(BinaryReader reader)
    {
        int count = reader.ReadByte();
        if (count > TalentCatalog.MaxNodes) throw new InvalidDataException("Too many talent picks.");
        var picks = new TalentPick[count];
        for (int i = 0; i < count; i++) picks[i] = new(reader.ReadUInt16(), reader.ReadByte(), reader.ReadByte());
        return picks;
    }

    public static void WritePicks(BinaryWriter writer, TalentPick[] picks)
    {
        if (picks.Length > TalentCatalog.MaxNodes) throw new InvalidDataException("Too many talent picks.");
        writer.Write((byte)picks.Length);
        foreach (TalentPick pick in picks)
        {
            writer.Write(pick.Id);
            writer.Write(pick.Rank);
            writer.Write(pick.Option);
        }
    }

    public static TalentBinding[] ReadBindings(BinaryReader reader)
    {
        int count = reader.ReadByte();
        if (count > 24) throw new InvalidDataException("Too many talent bindings.");
        var bindings = new TalentBinding[count];
        for (int i = 0; i < count; i++) bindings[i] = new((Spell)reader.ReadByte(), reader.ReadByte());
        return bindings;
    }

    public static void WriteBindings(BinaryWriter writer, TalentBinding[] bindings)
    {
        if (bindings.Length > 24) throw new InvalidDataException("Too many talent bindings.");
        writer.Write((byte)bindings.Length);
        foreach (TalentBinding binding in bindings)
        {
            writer.Write((byte)binding.Spell);
            writer.Write(binding.Slot);
        }
    }
}
