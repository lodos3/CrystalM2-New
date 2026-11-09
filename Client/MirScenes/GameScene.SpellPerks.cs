using Client.MirNetwork;
using Client.MirObjects;
using Client.MirScenes.Dialogs;
using C = ClientPackets;
using S = ServerPackets;

namespace Client.MirScenes;

public sealed partial class GameScene
{
    private SpellPerkDialog _spellPerkDialog;

    internal void OpenSpellPerks(UserObject actor)
    {
        if (Observing || !SpellPerks.Supports(actor.Class)) return;
        _spellPerkDialog?.Dispose();
        _spellPerkDialog = new SpellPerkDialog(actor) { Parent = this };
        _spellPerkDialog.Show();
        Network.Enqueue(new C.SpellPerkDraft { ObjectID = actor.ObjectID });
    }

    private void ReceiveSpellPerks(S.SpellPerkState state)
    {
        UserObject actor = state.ObjectID == User?.ObjectID ? User : state.ObjectID == Hero?.ObjectID ? Hero : null;
        if (actor == null || state.Revision < actor.SpellPerkRevision) return;
        bool changed = actor.SpellPerkRevision != state.Revision || !actor.SpellPerkPicks.SequenceEqual(state.Picks);
        actor.SpellPerkRevision = state.Revision;
        actor.SpellPerkPicks = state.Picks;
        if (changed) actor.RefreshStats();
        if (_spellPerkDialog?.Actor == actor && _spellPerkDialog.Visible) _spellPerkDialog.Receive(state, changed);
    }
}
