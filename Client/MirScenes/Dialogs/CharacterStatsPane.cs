using Client.MirControls;
using Client.MirObjects;

namespace Client.MirScenes.Dialogs;

internal sealed class CharacterStatsPane : MirImageControl
{
    private enum Format { Number, Signed, Percent, Range, Ratio, Progress }

    private sealed class Row(MirLabel label, Func<(long Value, long Limit)> read, Format format)
    {
        private (long Value, long Limit)? _last;

        public void Refresh()
        {
            var current = read();
            if (_last == current) return;
            _last = current;
            label.Text = format switch
            {
                Format.Signed => $"{current.Value:+0;-0;+0}",
                Format.Percent => $"{current.Value}%",
                Format.Range => $"{current.Value}–{current.Limit}",
                Format.Ratio => $"{current.Value} / {current.Limit}",
                Format.Progress => current.Limit > 0 ? $"{current.Value / (double)current.Limit:0.##%}" : "0%",
                _ => current.Value.ToString()
            };
        }
    }

    private readonly List<Row> _rows = new(13);

    public CharacterStatsPane(UserObject actor, Rectangle bounds, bool attributes)
    {
        AutoSize = false;
        Size = bounds.Size;
        Location = bounds.Location;
        Skin = UiSurface.Inset;
        NotControl = true;

        int y = 12;
        if (attributes)
        {
            Add("Experience", () => (actor.Experience, actor.MaxExperience), Format.Progress);
            Add("Bag weight", () => (actor.CurrentBagWeight, actor.Stats[Stat.BagWeight]), Format.Ratio);
            Add("Wear weight", () => (actor.CurrentWearWeight, actor.Stats[Stat.WearWeight]), Format.Ratio);
            Add("Hand weight", () => (actor.CurrentHandWeight, actor.Stats[Stat.HandWeight]), Format.Ratio);
            y += 6;
            StatValue("Magic resistance", Stat.MagicResist, Format.Signed);
            StatValue("Poison resistance", Stat.PoisonResist, Format.Signed);
            StatValue("Health recovery", Stat.HealthRecovery, Format.Signed);
            StatValue("Mana recovery", Stat.SpellRecovery, Format.Signed);
            StatValue("Poison recovery", Stat.PoisonRecovery, Format.Signed);
            y += 6;
            StatValue("Holy", Stat.Holy, Format.Signed);
            StatValue("Freezing", Stat.Freezing, Format.Signed);
            StatValue("Poison attack", Stat.PoisonAttack, Format.Signed);
        }
        else
        {
            Add("Health", () => (actor.HP, actor.Stats[Stat.HP]), Format.Ratio);
            Add("Mana", () => (actor.MP, actor.Stats[Stat.MP]), Format.Ratio);
            y += 6;
            Range("Armour", Stat.MinAC, Stat.MaxAC);
            Range("Magic armour", Stat.MinMAC, Stat.MaxMAC);
            Range("Attack", Stat.MinDC, Stat.MaxDC);
            Range("Magic", Stat.MinMC, Stat.MaxMC);
            Range("Spirit", Stat.MinSC, Stat.MaxSC);
            y += 6;
            StatValue("Critical chance", Stat.CriticalRate, Format.Percent);
            StatValue("Critical damage", Stat.CriticalDamage);
            StatValue("Attack speed", Stat.AttackSpeed);
            StatValue("Accuracy", Stat.Accuracy, Format.Signed);
            StatValue("Agility", Stat.Agility, Format.Signed);
            StatValue("Luck", Stat.Luck);
        }

        void StatValue(string name, Stat stat, Format format = Format.Number) =>
            Add(name, () => (actor.Stats[stat], 0), format);

        void Range(string name, Stat min, Stat max) =>
            Add(name, () => (actor.Stats[min], actor.Stats[max]), Format.Range);

        void Add(string name, Func<(long Value, long Limit)> read, Format format)
        {
            const int LabelWidth = 105, Padding = 10, RowHeight = 18;
            MirPanel.AddCaption(this, name, new Rectangle(Padding, y, LabelWidth, RowHeight));
            MirLabel value = MirPanel.AddCaption(this, "",
                new Rectangle(Padding + LabelWidth, y, Size.Width - Padding * 2 - LabelWidth, RowHeight), false);
            value.DrawFormat |= TextFormatFlags.Right;
            _rows.Add(new Row(value, read, format));
            y += RowHeight;
        }
    }

    public void RefreshValues()
    {
        if (!Visible) return;
        foreach (Row row in _rows) row.Refresh();
    }
}
