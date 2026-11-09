using Client.MirObjects;
using Client.MirControls;
using Client.MirNetwork;
using Client.MirScenes.Dialogs;
using C = ClientPackets;
using S = ServerPackets;

namespace Client.MirScenes;

public sealed partial class GameScene
{
    private TalentDialog _talentDialog;
    private MirLabel _rotationCaption;

    internal void OpenTalents(UserObject actor, CombatSpecialisation? selected = null)
    {
        if (actor == null || Observing || !TalentCatalog.Supports(actor.Class)) return;
        _talentDialog?.Dispose();
        _talentDialog = new TalentDialog(actor, selected) { Parent = this };
        _talentDialog.Show();
        Network.Enqueue(new C.TalentDraft { ObjectID = actor.ObjectID, Query = true });
    }

    private void ReceiveTalentState(S.TalentState state)
    {
        UserObject actor = state.ObjectID == User?.ObjectID ? User : state.ObjectID == Hero?.ObjectID ? Hero : null;
        if (actor == null) return;
        bool changed = actor.Talents.Revision != state.Profile.Revision || actor.Specialisation != state.Spec;
        actor.Talents = state.Profile;
        actor.Specialisation = state.Spec;
        foreach (ClientMagic magic in actor.Magics) magic.Key = 0;
        foreach (TalentBinding binding in state.ActiveBindings)
        {
            ClientMagic magic = actor.Magics.Find(magic => magic.Spell == binding.Spell);
            if (magic != null && actor.CanUseBuildSpell(binding.Spell)) magic.Key = binding.Slot;
        }
        MoveOldFinisherKeys(actor);
        if (changed)
        {
            actor.Thrusting = actor.HalfMoon = actor.CrossHalfMoon = actor.DoubleSlash = false;
            actor.Slaying = actor.TwinDrakeBlade = actor.FlamingSword = false;
            actor.RotationReady = false;
            actor.RotationStep = 0;
            actor.PendingSignature = Spell.None;
            actor.SignatureRecoveryUntil = 0;
            actor.ClearMagic();
            actor.RefreshStats();
        }
        if (_talentDialog?.Actor == actor && !_talentDialog.IsDisposed) _talentDialog.Receive(state);
        if (state.Message.Length != 0) OutputMessage(state.Message);
        RefreshActionBars();
    }

    private static void MoveOldFinisherKeys(UserObject actor)
    {
        foreach (ClientMagic old in actor.Magics)
        {
            if (old.Key == 0 || !TalentRotations.IsFinisher(old.Spell) || actor.FinisherAction(old.Spell) is not { } action) continue;
            byte previous = old.Key;
            SendAssignment(old, 0, previous);
            if (action.Key == 0) SendAssignment(action, previous, 0);
        }
    }

    private void ReceiveRotationState(S.RotationState state)
    {
        UserObject actor = state.ObjectID == User?.ObjectID ? User : state.ObjectID == Hero?.ObjectID ? Hero : null;
        if (actor == null) return;
        actor.RotationTarget = state.TargetID;
        actor.RotationReady = state.Ready;
        actor.RotationStep = state.Step;
        actor.RotationExpires = CMain.Time + state.Remaining;
    }

    private void UpdateRotationCaption()
    {
        if (User?.Rotation is not { } recipe || User.Level < 29 || Observing)
        {
            if (_rotationCaption != null) _rotationCaption.Visible = false;
            return;
        }
        if (_rotationCaption == null)
        {
            _rotationCaption = MirPanel.AddCaption(this, "", new Rectangle(0, 0, 390, 18), false);
            _rotationCaption.DrawFormat |= TextFormatFlags.HorizontalCenter;
            _rotationCaption.OutLine = true;
        }
        Point bar = SkillBarDialogs[0].Location;
        _rotationCaption.Location = new Point(Math.Max(0, bar.X - 15), Math.Max(0, bar.Y - 20));
        _rotationCaption.Visible = SkillBarDialogs[0].Visible;
        bool ready = User.IsFinisherReady(recipe.Finisher);
        int step = CMain.Time < User.RotationExpires ? Math.Min(User.RotationStep, recipe.Steps.Length - 1) : 0;
        ClientMagic action = User.FinisherAction(recipe.Finisher);
        string key = action?.Key > 0 ? CompactActionKey(CMain.InputKeys.GetKey(ActionKey(action.Key))) : "";
        _rotationCaption.Text = ready && SpecialisationAbilities.TryGet(recipe.Finisher, out var finisher)
            ? $"{finisher.Name} ready · {(key.Length > 0 ? key : action?.Name ?? "")}" 
            : $"{step + 1}/{recipe.Steps.Length} · {recipe.Steps[step].Hint}";
        _rotationCaption.ForeColour = ready ? Color.Gold : Color.Wheat;
    }

    private void CastTalentCounter(UserObject actor, ClientMagic magic, TalentAbility ability)
    {
        if (magic.CastTime > 0 && CMain.Time < magic.CastTime + magic.Delay) return;
        MapObject hovered = MapObject.MouseObject;
        uint targetId = (ability.Spell.Target is AbilityTarget.Ally or AbilityTarget.Companion && hovered != null)
            ? hovered.ObjectID : actor.ObjectID;
        SendSignatureRequest(actor, magic.Spell, actor.Direction, targetId, MapControl.MapLocation);
    }

    internal static void SendSignatureRequest(UserObject actor, Spell spell, MirDirection direction, uint target, Point location)
    {
        if (actor.PendingSignature == spell && CMain.Time < actor.PendingSignatureUntil) return;
        actor.PendingSignature = spell;
        actor.PendingSignatureDirection = direction;
        actor.PendingSignatureUntil = CMain.Time + 1000;
        Network.Enqueue(new C.Magic { ObjectID = actor.ObjectID, Spell = spell, TargetID = target, Direction = direction,
            Location = location, SpellTargetLock = CMain.SpellTargetLock });
    }

    private void PresentSignatureCast(MapObject actor, SpecialisationAbility ability, Point origin, Point target,
        MirDirection direction, bool accepted)
    {
        bool counter = TalentAbilities.TryGet(ability.Spell, out var talent) && talent.Kind == TalentAbilityKind.Counter;
        if (actor is UserObject user)
        {
            if (user.PendingSignature == ability.Spell) user.PendingSignature = Spell.None;
            if (accepted)
            {
                if (user.GetMagic(ability.Spell) is { } learned) learned.CastTime = CMain.Time;
                if (!counter) user.SignatureRecoveryUntil = CMain.Time + ability.CastRecovery;
            }
        }
        if (!accepted) return;

        MapControl.EmitSpellCast(ability.Spell, origin, target, direction);
        if (actor is not PlayerObject player || actor.Dead || counter || ability.Spell == Spell.PrismStep) return;
        player.ActionFeed.Add(new QueuedAction
        {
            Action = MirAction.Spell,
            Direction = direction,
            Location = origin,
            SignatureDuration = (ushort)Math.Clamp(ability.Windup + 75, 190, 720),
            Params = [ability.Spell]
        });
    }
}
